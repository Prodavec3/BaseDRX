using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Shared.Job;

namespace Sungero.Exchange.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Получение сообщений".
    /// </summary>
    public static void GetMessages()
    {
      Functions.Module.Remote.RequeueMessagesGet();
    }
    
    /// <summary>
    /// Получить сертификат сервиса обмена для текущего сотрудника, используя системный диалог выбора сертификата.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификат.</returns>
    public static ICertificate GetCurrentUserExchangeCertificate(ExchangeCore.IBoxBase box, Company.IEmployee employee)
    {
      var businessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      var certificates = businessUnitBox.HasExchangeServiceCertificates == true
        ? businessUnitBox.ExchangeServiceCertificates.Where(x => Equals(x.Certificate.Owner, employee) && x.Certificate.Enabled == true).Select(x => x.Certificate)
        : Functions.Module.Remote.GetCertificates(employee).AsEnumerable();
      
      certificates = certificates.GroupBy(x => x.Thumbprint).Select(x => x.First());
      
      if (certificates.Count() > 1)
        return certificates.ShowSelectCertificate();
      
      return certificates.FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сертификат сервиса обмена для сотрудника, используя системный диалог выбора сертификата.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификат.</returns>
    [Public]
    public virtual ICertificate GetUserExchangeCertificate(ExchangeCore.IBoxBase box, Company.IEmployee employee)
    {
      return GetCurrentUserExchangeCertificate(box, employee);
    }
    
    /// <summary>
    /// Отправить уведомление об уточнении документа.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="note">Комментарий.</param>
    /// <param name="throwError">Не гасить ошибку.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания УОУ.</param>
    /// <param name="isInvoiceAmendmentRequest">True для УОУ, False для отказа.</param>
    /// <returns>Строка с ошибкой отправки уведомления. Пусто - если отправка успешная.</returns>
    [Public]
    public static string SendAmendmentRequest(List<Docflow.IOfficialDocument> documents, Parties.ICounterparty receiver, string note, bool throwError,
                                              ExchangeCore.IBoxBase box, ICertificate certificate, bool isInvoiceAmendmentRequest)
    {
      if (!documents.Any())
        return string.Empty;
      
      var error = Resources.AmendmentRequestError;
      var serviceDocs = new List<Structures.Module.ReglamentDocumentWithCertificate>();
      
      try
      {
        serviceDocs.AddRange(Functions.Module.Remote.GenerateAmendmentRequestDocuments(documents.ToList(), box, note, throwError, certificate, isInvoiceAmendmentRequest));
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return ex.Message;
      }
      
      if (!serviceDocs.Any())
        return Resources.AllAnswersIsAlreadySent;
      
      try
      {
        var signs = ExternalSignatures.Sign(certificate, serviceDocs.ToDictionary(d => d.ParentDocumentId, d => d.Content));
        
        foreach (var doc in serviceDocs)
          doc.Signature = signs[doc.ParentDocumentId];
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return Resources.DocumentEndorseError;
      }

      try
      {
        var serviceCounterpartyId = string.Empty;
        var externalDocumentInfo = Functions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(documents.FirstOrDefault());
        if (externalDocumentInfo != null)
          serviceCounterpartyId = externalDocumentInfo.ServiceCounterpartyId;
        
        Functions.Module.Remote.SendAmendmentRequest(serviceDocs, receiver, box, note);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(error, ex);
        return string.Format("{0}: {1}", error, ex.Message.ToString().ToLower());
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Отправить извещения о получении документа.
    /// </summary>
    [Public]
    public virtual void SignAndSendDeliveryConfirmation()
    {
      var userBoxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote
        .GetConnectedBoxes()
        .Where(b => b.CertificateReceiptNotifications != null && Equals(b.CertificateReceiptNotifications.Owner, Users.Current))
        .ToList();
      
      if (!userBoxes.Any())
      {
        var error = Resources.SendDeliveryConfirmationBoxesNotFoundFormat(Users.Current.Name);
        throw AppliedCodeException.Create(error);
      }
      
      var aggregate = new List<Exception>();
      foreach (var box in userBoxes)
      {
        try
        {
          var error = this.SendDeliveryConfirmation(box, box.CertificateReceiptNotifications, true);
          if (!string.IsNullOrWhiteSpace(error))
            aggregate.Add(AppliedCodeException.Create(error));
        }
        catch (Exception ex)
        {
          Logger.Error(ReceiptNotificationSendingTasks.Resources.ReceiptNotificationAssignmentError, ex);
          aggregate.Add(ex);
        }
      }
      if (aggregate.Any())
      {
        var result = aggregate.Count == 1 ? aggregate.Single() : new AggregateException(aggregate);
        throw result;
      }
    }
    
    /// <summary>
    /// Отправить извещение о получении документа.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания ИОП.</param>
    /// <param name="bulkMode">Режим для большой нагрузки.
    /// Если true - будут выполняться генерации ИОП в сервисе обмена и подписываться все доступные ИОП-ы.
    /// Если false - только одна пачка ИОП будет подписана, если совсем нечего подписывать - будет сгенерирована.</param>
    /// <returns>Строка с ошибкой отправки извещения. Пусто - если отправка успешная.</returns>
    [Public]
    public virtual string SendDeliveryConfirmation(ExchangeCore.IBoxBase box,
                                                   ICertificate certificate,
                                                   bool bulkMode)
    {
      var partSize = 25;
      var skip = 0;
      var isSendJobEnabled = PublicFunctions.Module.Remote.IsJobEnabled(PublicConstants.Module.SendSignedReceiptNotificationsId);
      var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      var documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
      if (!documentInfos.Any())
        return string.Empty;
      
      if (certificate == null)
        certificate = Functions.Module.GetCurrentUserExchangeCertificate(box, Company.Employees.Current);
      
      var isJobEnabled = PublicFunctions.Module.Remote.IsJobEnabled(Constants.Module.CreateReceiptNotifications);
      
      // Если в системе настроена автоматическая схема работы с иопами, и выбранный сертификат не попадает под неё - не делаем ничего.
      if (!bulkMode &&
          rootBox.CertificateReceiptNotifications != null &&
          !Equals(rootBox.CertificateReceiptNotifications, certificate) &&
          isJobEnabled)
        return string.Empty;
      
      while (documentInfos.Any())
      {
        // Если bulkMode выключен - разрешаем только один прогон.
        if (!bulkMode && skip >= partSize)
          break;
        
        var serviceDocs = new List<Structures.Module.ReglamentDocumentWithCertificate>();
        var error = Resources.DeliveryConfirmationError;
        try
        {
          serviceDocs = Functions.Module.Remote.GetGeneratedDeliveryConfirmationDocuments(documentInfos, rootBox, certificate, bulkMode);
          
          // Если снаружи пришел параметр, что генерировать не надо, но сгенерированных совсем нет - генерируем хотя бы одну пачку ИОП.
          // Так на небольших объемах спасаемся от лишней задачки на отправку ИОП.
          if (!bulkMode && (!serviceDocs.Any() || !isJobEnabled))
            serviceDocs = Functions.Module.Remote.GetGeneratedDeliveryConfirmationDocuments(documentInfos, rootBox, certificate, true);

          // Проставляем NotRequired в двух случаях:
          // 1. Это массовая обработка и ИОПы не сгенерировались.
          // 2. Это единичная обработка, ФП отключен, а ИОПы пытались генерироваться, но не сгенерировались.
          var documentsToFix = documentInfos.Where(x => !serviceDocs.Any(s => Equals(s.LinkedDocument, x.Document))).ToList();
          if (documentsToFix.Any() && (bulkMode == true || !bulkMode && !isJobEnabled))
            Functions.Module.Remote.FixReceiptNotification(documentsToFix, string.Empty);

          if (!serviceDocs.Any())
          {
            skip += partSize;
            documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
            continue;
          }
          var documentsToSign = serviceDocs.Where(d => d.Signature == null).ToList();

          try
          {
            Logger.DebugFormat("Try sign {0} documents", documentsToSign.Count());
            
            var signs = ExternalSignatures.Sign(certificate, documentsToSign.ToDictionary(d => d.ParentDocumentId, d => d.Content));
            Logger.DebugFormat("Sign {0} documents", signs.Count());
            foreach (var document in documentsToSign)
            {
              Logger.DebugFormat("Get signatory for parent document id {0}", document.ParentDocumentId);
              document.Signature = signs[document.ParentDocumentId];
              var formalizedPoAUnifiedRegNo = Docflow.PublicFunctions.OfficialDocument.Remote.GetFormalizedPoAUnifiedRegNo(document.LinkedDocument, Company.Employees.Current, certificate);
              document.FormalizedPoAUnifiedRegNumber = formalizedPoAUnifiedRegNo;
              Logger.Debug(string.Format("Sign receipt notification with ExchangeDocumentInfoId = {0}, DocumentType = {1}, ServiceCounterpartyId = {2}," +
                                         " ParentDocumentId = {3} LinkedDocumentId = {4}, ServiceMessageId = {5}, FormalizedPoAUnifiedRegNo = {6}",
                                         document.Info.Id, document.ReglamentDocumentType, document.ServiceCounterpartyId, document.ParentDocumentId,
                                         document.LinkedDocument.Id, document.ServiceMessageId, formalizedPoAUnifiedRegNo));
            }

            if (isSendJobEnabled)
              Functions.Module.Remote.SaveDeliveryConfirmationSigns(documentsToSign);
          }
          catch (Sungero.Domain.Shared.Exceptions.EntitySigningException ex)
          {
            Logger.ErrorFormat(error, ex);
            return ex.Message;
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat(error, ex);
            return Resources.DocumentEndorseError;
          }
          
          if (!isSendJobEnabled)
            Functions.Module.Remote.SendDeliveryConfirmation(serviceDocs, rootBox);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat(error, ex);
          return string.Format("{0}: {1}", error, ex.Message.ToString().ToLower());
        }

        if (Functions.Module.Remote.IsReglamentDocumentsNotSent(documentInfos))
          skip += partSize;
        documentInfos = Functions.Module.Remote.GetDocumentInfosWithoutReceiptNotificationPart(rootBox, skip, partSize, false);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Отправка документа, либо ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа или отправка самого документа.</param>
    [Public]
    public static void SendResultToCounterparty(Docflow.IOfficialDocument document)
    {
      SendResultToCounterparty(document, null, new List<Docflow.IOfficialDocument>());
    }
    
    /// <summary>
    /// Отправка документа, либо ответа контрагенту с учетом выбранного сервиса обмена и приложений в задаче на согласование.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа или отправка самого документа.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="addenda">Приложения.</param>
    [Public]
    public static void SendResultToCounterparty(Docflow.IOfficialDocument document,
                                                ExchangeCore.IExchangeService service,
                                                List<Docflow.IOfficialDocument> addenda)
    {
      var isNonformalizedTaxInvoice = false;
      if (FinancialArchive.IncomingTaxInvoices.Is(document) || FinancialArchive.OutgoingTaxInvoices.Is(document))
        isNonformalizedTaxInvoice = Docflow.AccountingDocumentBases.As(document).IsFormalized != true;

      if (isNonformalizedTaxInvoice)
      {
        Dialogs.NotifyMessage(Resources.NonFormalizedTaxInvoiceSendingError);
        return;
      }
      
      var lockInfo = document != null ? Locks.GetLockInfo(document) : null;
      if (lockInfo != null && lockInfo.IsLockedByOther)
      {
        Dialogs.NotifyMessage(lockInfo.LockedMessage);
        return;
      }
      
      var documentInfo = Functions.Module.Remote.GetInfoForSendToCounterparty(document);
      if (documentInfo.HasError)
      {
        Dialogs.NotifyMessage(documentInfo.Error);
        return;
      }
      
      if (documentInfo.AnswerIsSent)
      {
        Dialogs.NotifyMessage(Resources.AnswerIsAlreadySent);
        return;
      }
      
      if (document.LastVersion.Body.Size >= Constants.Module.ExchangeDocumentMaxSize)
      {
        Dialogs.NotifyMessage(Resources.DocumentOversized);
        return;
      }
      
      if (!document.AccessRights.CanUpdate())
      {
        Dialogs.NotifyMessage(Resources.NoRightsForDocument);
        return;
      }
      
      var isForcedLocked = false;
      if (!lockInfo.IsLocked)
        isForcedLocked = Locks.TryLock(document);
      if (!lockInfo.IsLocked && !isForcedLocked)
      {
        var lockInfoError = document != null ? Locks.GetLockInfo(document) : null;
        Dialogs.NotifyMessage(lockInfoError.LockedMessage);
        return;
      }
      
      if (documentInfo.IsSignedByCounterparty)
        SendAnswerToCounterparty(document, documentInfo, addenda);
      else
        SendDocumentToCounterparty(document, documentInfo, service, addenda);
      
      if (isForcedLocked)
        Locks.Unlock(document);
    }
    
    /// <summary>
    /// Отправка последней версии документа контрагенту.
    /// </summary>
    /// <param name="document">Документ для отправки.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="addenda">Приложения.</param>
    public static void SendDocumentToCounterparty(Docflow.IOfficialDocument document,
                                                  Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo,
                                                  ExchangeCore.IExchangeService service,
                                                  List<Docflow.IOfficialDocument> addenda)
    {
      if (!documentInfo.CanApprove && !documentInfo.HasApprovalSignature)
      {
        Dialogs.NotifyMessage(Resources.SendCounterpartyNotApproved);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(Resources.SendCounterpartyDialogTitle);
      dialog.HelpCode = Constants.Module.HelpCodes.SendDocument;

      var counterparty = dialog.AddSelect(Resources.SendCounterpartyReceiver, true, documentInfo.DefaultCounterparty)
        .From(documentInfo.Counterparties)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      var defaultBox = documentInfo.Boxes.FirstOrDefault(x => Equals(x.ExchangeService, service)) ?? documentInfo.DefaultBox;
      
      var box = dialog.AddSelect(Resources.SendCounterpartySender, true, defaultBox)
        .From(documentInfo.Boxes)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      box.IsEnabled = !(Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true);
      var users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
      
      var signatureOwner = dialog.AddSelect(Docflow.OfficialDocuments.Info.Properties.OurSignatory.LocalizedName, false, users.FirstOrDefault())
        .From(users);
      signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
      
      var allowedAddenda = documentInfo.Addenda.Select(a => a.Addendum).ToList();
      var defaultSelectedAddenda = addenda.Intersect(allowedAddenda).ToArray();
      var selectedAddenda = dialog.AddSelectMany(Resources.SendCounterpartyAddenda, false, defaultSelectedAddenda)
        .From(allowedAddenda);
      selectedAddenda.IsEnabled = documentInfo.HasAddendaToSend;
      
      var needSign = dialog.AddBoolean(Resources.SendCounterpartyNeedSign, false);
      if (Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true)
      {
        var accDocument = Docflow.AccountingDocumentBases.As(document);
        var isSF = accDocument.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        var isWaybill = FinancialArchive.Waybills.Is(document);
        var isContractStatement = FinancialArchive.ContractStatements.Is(document);
        var isUPD = FinancialArchive.UniversalTransferDocuments.Is(document);
        var isSbis = accDocument.BusinessUnitBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
        var isSbisSF = isSF && isSbis;
        needSign.Value = isWaybill || isUPD || isContractStatement || isSbisSF;
        needSign.IsEnabled = !(isWaybill || isSF || isUPD || isContractStatement || isSbisSF);
      }
      else
        needSign.Value = Docflow.ContractualDocumentBases.Is(document);
      
      var comment = dialog.AddMultilineString(Resources.SendCounterpartyComment, false);
      
      var sendButton = dialog.Buttons.AddCustom(Resources.SendCounterpartySendButton);
      dialog.Buttons.Default = sendButton;
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(x =>
                          {
                            if (x.IsValid && documentInfo.Certificates.CanSign && signatureOwner.Value == null)
                              x.AddInformation(Resources.SendCounterpartySignAndSendHint);
                            
                            if (selectedAddenda.Value != null)
                            {
                              var exchangeDocumentsSize = selectedAddenda.Value.Select(s => s.LastVersion.Body.Size).Sum() + document.LastVersion.Body.Size;
                              if (exchangeDocumentsSize >= Constants.Module.ExchangeDocumentMaxSize)
                                x.AddError(Resources.AddendaOversized);
                            }
                            if (counterparty.Value != null)
                            {
                              var boxes = counterparty.Value.ExchangeBoxes.Where(b => b.Box.Status == ExchangeCore.BusinessUnitBox.Status.Active &&
                                                                                 b.Box.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected &&
                                                                                 b.Status == Parties.CounterpartyExchangeBoxes.Status.Active && b.IsDefault == true);
                              if (!boxes.Any())
                                x.AddError(Resources.BoxesNotFound);
                            }
                          });
      
      counterparty.SetOnValueChanged(x =>
                                     {
                                       if (x.NewValue != x.OldValue)
                                       {
                                         var newCounterpartyList = documentInfo.Counterparties;
                                         if (x.NewValue != null)
                                           newCounterpartyList = documentInfo.Counterparties.Where(c => c.Equals(x.NewValue)).ToList();
                                         
                                         documentInfo.Boxes = Functions.Module.Remote.GetConnectedExchangeBoxesToCounterparty(document, newCounterpartyList);
                                         box = box.From(documentInfo.Boxes);
                                         
                                         // Если ящик не был выбран или был выбран ящик, через который нет обмена с выбранным контрагентом, заполнить поле первым подходящим ящиком.
                                         if (box.Value == null || !documentInfo.Boxes.Contains(box.Value))
                                           box.Value = documentInfo.Boxes.FirstOrDefault();
                                       }
                                     });
      
      box.SetOnValueChanged(x =>
                            {
                              if (x.NewValue != x.OldValue)
                              {
                                if (x.NewValue != null)
                                {
                                  documentInfo.Certificates = Functions.Module.Remote.GetDocumentCertificatesToBox(document, x.NewValue);
                                  documentInfo.IsSignedByUs = documentInfo.Certificates.Certificates.Any();
                                }
                                else
                                {
                                  documentInfo.Certificates = Structures.Module.DocumentCertificatesInfo
                                    .Create(new List<ICertificate>(), false, new List<ICertificate>());
                                  documentInfo.IsSignedByUs = false;
                                }
                                
                                users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
                                signatureOwner = signatureOwner.From(users);
                                signatureOwner.Value = users.FirstOrDefault();
                                signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
                              }
                            });
      
      dialog.SetOnButtonClick(x =>
                              {
                                if (x.Button == sendButton && x.IsValid)
                                {
                                  var hasExchangeWithCounterparty = counterparty.Value.ExchangeBoxes
                                    .Any(c => Equals(c.Box, box.Value) &&
                                         Equals(c.Status, Parties.CounterpartyExchangeBoxes.Status.Active) &&
                                         c.IsDefault == true);
                                  if (!hasExchangeWithCounterparty)
                                  {
                                    x.AddError(Exchange.Resources.NoExchangeThroughThisService);
                                    return;
                                  }

                                  if (signatureOwner.Value == null && documentInfo.IsSignedByUs && !documentInfo.Certificates.CanSign)
                                  {
                                    x.AddError(Resources.SendCounterpartyCertificateNotSelected);
                                    return;
                                  }
                                  
                                  if (IsLocked(document, x))
                                    return;
                                  
                                  foreach (var addendumDocument in selectedAddenda.Value)
                                    if (IsLocked(addendumDocument, x))
                                      return;
                                  
                                  if (Functions.ExchangeDocumentInfo.Remote.LastVersionSended(document, box.Value, counterparty.Value))
                                  {
                                    x.AddError(Resources.DocumentIsAlreadySentToCounterparty);
                                    return;
                                  }
                                  
                                  var noSign = signatureOwner.Value == null;
                                  var certificate = documentInfo.Certificates.Certificates.FirstOrDefault(c => Equals(c.Owner, signatureOwner.Value));
                                  ICertificate certificateToRejectFirstVersion = null;
                                  
                                  if (noSign)
                                  {
                                    if (!documentInfo.Certificates.CanSign)
                                    {
                                      if (selectedAddenda.Value.Any())
                                        x.AddError(Resources.SendCounterpartyWithAddendaWhenDocumentNotSigned);
                                      else
                                        x.AddError(Resources.SendCounterpartyCanNotSign);
                                      return;
                                    }
                                    
                                    var signDialog = Dialogs.CreateTaskDialog(Resources.SendCounterpartySignAndSendQuestion);
                                    var signButtons = signDialog.Buttons.AddCustom(Resources.SendCounterpartySignAndSendButton);
                                    signDialog.Buttons.AddCancel();
                                    
                                    if (signDialog.Show() == signButtons)
                                    {
                                      try
                                      {
                                        if (IsLocked(document, x))
                                          return;
                                        
                                        certificate = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                        
                                        certificateToRejectFirstVersion = certificate;
                                        
                                        var selectedAddendaList = new List<Docflow.IOfficialDocument>();
                                        if (selectedAddenda.Value != null)
                                          selectedAddendaList = selectedAddenda.Value.ToList();
                                        
                                        if (certificate == null || !Docflow.PublicFunctions.Module
                                            .ApproveWithAddenda(document, selectedAddendaList, certificate, null, true, true, string.Empty))
                                        {
                                          x.AddError(Resources.SendCounterpartyCanNotSign);
                                          return;
                                        }
                                      }
                                      catch (Exception ex)
                                      {
                                        x.AddError(ex.Message);
                                        return;
                                      }
                                    }
                                    else
                                    {
                                      return;
                                    }
                                  }
                                  
                                  try
                                  {
                                    Functions.Module.Remote.SendDocuments(document, selectedAddenda.Value.ToList(), counterparty.Value, box.Value,
                                                                          certificate, needSign.Value.Value, comment.Value);
                                    if (Equals(certificate.Owner, Company.Employees.Current))
                                      Functions.Module.SendDeliveryConfirmation(box.Value, certificate, false);
                                    else
                                      Functions.Module.SendDeliveryConfirmation(box.Value, null, false);
                                  }
                                  catch (AppliedCodeException ex)
                                  {
                                    x.AddError(ex.Message);
                                    return;
                                  }
                                  catch (Exception ex)
                                  {
                                    Logger.ErrorFormat("Error sending document: ", ex);
                                    x.AddError(Resources.ErrorWhileSendingDocToCounterparty);
                                    return;
                                  }
                                  
                                  if (documentInfo.NeedRejectFirstVersion)
                                  {
                                    if (certificateToRejectFirstVersion == null)
                                      certificateToRejectFirstVersion = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                    
                                    TryRejectCounterpartyVersion(document, counterparty.Value, box.Value, certificateToRejectFirstVersion);
                                  }

                                  var addendaToReject =
                                    documentInfo.Addenda
                                    .Where(a => a.NeedRejectFirstVersion)
                                    .Select(a => a.Addendum)
                                    .Where(a => selectedAddenda.Value.Contains(a))
                                    .ToList();
                                  foreach (var addendum in addendaToReject)
                                  {
                                    if (certificateToRejectFirstVersion == null)
                                      certificateToRejectFirstVersion = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                    
                                    TryRejectCounterpartyVersion(addendum, counterparty.Value, box.Value, certificateToRejectFirstVersion);
                                  }
                                  
                                  Dialogs.NotifyMessage(Resources.SendCounterpartySuccessfully);
                                }
                              });
      dialog.Show();
    }
    
    private static bool IsLocked(Docflow.IOfficialDocument document, CommonLibrary.BaseInputDialogEventArgs x)
    {
      var lockInfo = document != null ? Locks.GetLockInfo(document) : null;
      if (lockInfo != null && lockInfo.IsLockedByOther)
      {
        x.AddError(lockInfo.LockedMessage);
        return true;
      }
      return false;
    }
    
    private static void UnlockAddenda(List<Docflow.IOfficialDocument> selectedAddenda)
    {
      foreach (Docflow.IOfficialDocument docAddendum in selectedAddenda)
      {
        if (docAddendum.LastVersion != null)
        {
          var lockInfoAddendum = Locks.GetLockInfo(docAddendum.LastVersion.PublicBody);
          if (lockInfoAddendum != null && lockInfoAddendum.IsLockedByMe)
          {
            Locks.Unlock(docAddendum.LastVersion.PublicBody);
          }
        }
      }
    }
    
    /// <summary>
    /// Попытаться отказать контрагенту по первой версии, когда отправляем вторую.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="party">Контрагент.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="certificate">Сертификат.</param>
    private static void TryRejectCounterpartyVersion(Docflow.IOfficialDocument document, Parties.ICounterparty party,
                                                     ExchangeCore.IBusinessUnitBox box, ICertificate certificate)
    {
      try
      {
        if (document.Versions.Count < 2)
          return;
        
        if (certificate != null)
          SendAmendmentRequest(new List<Docflow.IOfficialDocument>() { document }, party, string.Empty, false, box, certificate, false);
      }
      catch (Exception)
      {
        // Мягкая попытка отправки, не удалось - и не надо.
      }
    }
    
    /// <summary>
    /// Отправка ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ, по которому требуется отправка ответа.</param>
    /// <param name="documentInfo">Информация о документе, связанная с коммуникацией с контрагентом.</param>
    /// <param name="addenda">Приложения.</param>
    public static void SendAnswerToCounterparty(Docflow.IOfficialDocument document, Sungero.Exchange.Structures.Module.SendToCounterpartyInfo documentInfo, List<Docflow.IOfficialDocument> addenda)
    {
      var dialog = Dialogs.CreateInputDialog(Resources.SendAnswerToCounterpartyDialogTitle);
      dialog.HelpCode = Constants.Module.HelpCodes.SendAnswerOnDocument;
      
      var positiveResult = Resources.SendCounterpartyPositiveResult;
      var amendmentResult = Resources.SendCounterpartyNegativeResult;
      var invoiceAmendmentResult = Resources.SendCounterpartyRejectResult;
      
      var allowedResults = new List<string>();
      if (documentInfo.CanSendSignAsAnswer)
        allowedResults.Add(positiveResult);
      if (documentInfo.CanSendInvoiceAmendmentRequestAsAnswer)
        allowedResults.Add(invoiceAmendmentResult);
      if (documentInfo.CanSendAmendmentRequestAsAnswer)
        allowedResults.Add(amendmentResult);
      
      var formParams = ((Domain.Shared.IExtendedEntity)document).Params;
      var signAndSend = formParams.ContainsKey(Exchange.PublicConstants.Module.DefaultSignResult) &&
        (bool)formParams[Exchange.PublicConstants.Module.DefaultSignResult];
      var signResult = dialog.AddSelect(Resources.SendCounterpartyResult, true, allowedResults.FirstOrDefault())
        .From(allowedResults.ToArray());
      
      var counterparty = dialog.AddSelect(Resources.SendCounterpartyReceiver, true, documentInfo.DefaultCounterparty);
      counterparty.IsEnabled = false;
      
      var box = dialog.AddSelect(Resources.SendCounterpartySender, true, documentInfo.DefaultBox);
      box.IsEnabled = false;
      
      var users = new List<Sungero.CoreEntities.IUser>();
      if (documentInfo.Certificates.Certificates != null)
        users = documentInfo.Certificates.Certificates.Select(c => c.Owner).ToList();
      
      var signatureOwner = dialog.AddSelect(Docflow.OfficialDocuments.Info.Properties.OurSignatory.LocalizedName, false, users.FirstOrDefault())
        .From(users);
      signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
      
      var allowedAddenda = documentInfo.Addenda.Select(a => a.Addendum).ToList();
      var defaultSelectedAddenda = addenda.Intersect(allowedAddenda).ToArray();
      var selectedAddenda = dialog.AddSelectMany(Resources.SendCounterpartyAddenda, false, defaultSelectedAddenda)
        .From(allowedAddenda);
      selectedAddenda.IsEnabled = documentInfo.HasAddendaToSend;
      
      var comment = dialog.AddMultilineString(Resources.SendCounterpartyComment, false);
      comment.IsEnabled = false;
      
      var sendButton = dialog.Buttons.AddCustom(Resources.SendCounterpartySendButton);
      dialog.Buttons.Default = sendButton;
      dialog.Buttons.AddCancel();
      
      var currentUserSelectedCertificate = Certificates.Null;
      
      if (!documentInfo.IsSignedByUs && !documentInfo.Certificates.MyCertificates.Any())
      {
        Dialogs.NotifyMessage(Resources.NotificationNoCertificates);
        return;
      }
      
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      var isSbis = exchangeDocumentInfo.RootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
      comment.IsRequired = isSbis && comment.IsEnabled;
      
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      
      signResult.SetOnValueChanged(x =>
                                   {
                                     if (x.NewValue != x.OldValue)
                                     {
                                       if (x.NewValue != positiveResult)
                                       {
                                         signatureOwner.Value = null;
                                         signatureOwner.IsEnabled = false;
                                         
                                         comment.IsEnabled = true;
                                         comment.IsRequired = isSbis;
                                       }
                                       else
                                       {
                                         signatureOwner.Value = users.FirstOrDefault();
                                         signatureOwner.IsEnabled = documentInfo.IsSignedByUs;
                                         comment.Value = string.Empty;
                                         
                                         comment.IsEnabled = false;
                                         comment.IsRequired = false;
                                       }
                                     }
                                   });
      
      dialog.SetOnRefresh(x =>
                          {
                            if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusAccepted);
                            else if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusPartiallyAccepted);
                            else if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerAcceptanceStatusRejected);
                            else if (accountingDocument != null && accountingDocument.IsFormalized == true &&
                                     accountingDocument.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired && accountingDocument.BuyerTitleId == null &&
                                     !(isSbis && accountingDocument.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf))
                              x.AddInformation(Sungero.Exchange.Resources.SendToCounterpartyDialog_BuyerTitleIsEmpty);
                            
                            if (selectedAddenda.Value != null && selectedAddenda.Value.Any() &&
                                documentInfo.Addenda.Where(a => a.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
                                .Any(ad => selectedAddenda.Value.Contains(ad.Addendum)))
                              x.AddWarning(Sungero.Exchange.Resources.SendToCounterpartyDialog_AddendaWithBuyerAcceptanceStatusRejected);

                          });
      
      dialog.SetOnButtonClick(x =>
                              {
                                if (x.Button == sendButton && x.IsValid)
                                {
                                  var hasExchangeWithCounterparty = counterparty.Value.ExchangeBoxes
                                    .Any(c => Equals(c.Box, box.Value) &&
                                         Equals(c.Status, Parties.CounterpartyExchangeBoxes.Status.Active) &&
                                         c.IsDefault == true);
                                  if (!hasExchangeWithCounterparty)
                                  {
                                    x.AddError(Exchange.Resources.NoExchangeThroughThisService);
                                    return;
                                  }
                                  
                                  if (IsLocked(document, x))
                                    return;
                                  
                                  if (comment.Value != null && comment.Value.Length > 1000)
                                  {
                                    x.AddError(Exchange.ExchangeDocumentProcessingAssignments.Resources.TextOverlong, comment);
                                    return;
                                  }
                                  
                                  if (signResult.Value == Resources.SendCounterpartyPositiveResult)
                                  {
                                    #region Отправка подписи
                                    
                                    if (signatureOwner.Value == null && documentInfo.IsSignedByUs && !documentInfo.Certificates.CanSign)
                                    {
                                      x.AddError(Resources.SendCounterpartyCertificateNotSelected);
                                      return;
                                    }
                                    
                                    var noSign = signatureOwner.Value == null;
                                    if (noSign)
                                    {
                                      if (!documentInfo.Certificates.CanSign)
                                      {
                                        x.AddError(Resources.NotificationNoCertificates);
                                        return;
                                      }
                                      
                                      var signDialog = Dialogs.CreateTaskDialog(Resources.SendCounterpartySignAndSendQuestion);
                                      var signButtons = signDialog.Buttons.AddCustom(Resources.SendCounterpartySignAndSendButton);
                                      signDialog.Buttons.AddCancel();
                                      
                                      if (signDialog.Show() == signButtons)
                                      {
                                        try
                                        {
                                          if (IsLocked(document, x))
                                            return;

                                          currentUserSelectedCertificate = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                          
                                          List<Docflow.IOfficialDocument> lockedDocuments = new List<Docflow.IOfficialDocument>();
                                          foreach (Docflow.IOfficialDocument docAddendum in selectedAddenda.Value)
                                          {
                                            if (docAddendum.LastVersion != null)
                                            {
                                              var bodyAddendum = docAddendum.LastVersion.PublicBody;
                                              var lockInfoAddendum = Locks.GetLockInfo(bodyAddendum);
                                              if (lockInfoAddendum != null)
                                              {
                                                if (lockInfoAddendum.IsLockedByOther)
                                                {
                                                  x.AddError(lockInfoAddendum.LockedMessage);
                                                  return;
                                                }
                                                else if (!lockInfoAddendum.IsLocked)
                                                {
                                                  Locks.TryLock(bodyAddendum);
                                                  lockedDocuments.Add(docAddendum);
                                                }
                                              }
                                            }
                                          }
                                          
                                          if (currentUserSelectedCertificate == null || !Docflow.PublicFunctions.Module
                                              .ApproveWithAddenda(document, selectedAddenda.Value.ToList(), currentUserSelectedCertificate, null, true, true, string.Empty))
                                          {
                                            UnlockAddenda(lockedDocuments);
                                            x.AddError(Resources.NotificationNoCertificates);
                                            return;
                                          }
                                          UnlockAddenda(lockedDocuments);
                                          
                                          signatureOwner.Value = Users.Current;
                                          documentInfo.Certificates = Functions.Module.Remote.GetDocumentCertificatesToBox(document, box.Value);
                                        }
                                        catch (Exception ex)
                                        {
                                          x.AddError(ex.Message);
                                          return;
                                        }
                                      }
                                      else
                                      {
                                        return;
                                      }
                                    }
                                    
                                    try
                                    {
                                      var certificate = signatureOwner.Value == null ? null :
                                        documentInfo.Certificates.Certificates.Single(c => Equals(c.Owner, signatureOwner.Value));
                                      
                                      var documentsToSend = new List<Docflow.IOfficialDocument>() { document };
                                      documentsToSend.AddRange(selectedAddenda.Value);
                                      Functions.Module.Remote.SendAnswers(documentsToSend, counterparty.Value, box.Value, certificate, false);
                                      if (Equals(certificate.Owner, Company.Employees.Current))
                                        Functions.Module.SendDeliveryConfirmation(box.Value, certificate, false);
                                      else
                                        Functions.Module.SendDeliveryConfirmation(box.Value, null, false);
                                    }
                                    catch (AppliedCodeException ex)
                                    {
                                      x.AddError(ex.Message);
                                      return;
                                    }
                                    catch (Exception ex)
                                    {
                                      Logger.ErrorFormat("Error sending sign: ", ex);
                                      x.AddError(Resources.ErrorWhileSendingSignToCounterparty);
                                      return;
                                    }
                                    
                                    Dialogs.NotifyMessage(Resources.SendAnswerCounterpartySuccessfully);
                                    
                                    #endregion
                                  }
                                  else
                                  {
                                    #region Отправка отказа в подписании

                                    currentUserSelectedCertificate = GetCurrentUserExchangeCertificate(box.Value, Company.Employees.Current);
                                    
                                    if (currentUserSelectedCertificate != null)
                                    {
                                      var documents = selectedAddenda.Value.ToList();
                                      documents.Add(document);
                                      
                                      var error = SendAmendmentRequest(documents, counterparty.Value, comment.Value, false, box.Value, currentUserSelectedCertificate, signResult.Value == invoiceAmendmentResult);
                                      Functions.Module.SendDeliveryConfirmation(box.Value, currentUserSelectedCertificate, false);
                                      if (!string.IsNullOrWhiteSpace(error))
                                      {
                                        x.AddError(error);
                                      }
                                      else
                                      {
                                        Dialogs.NotifyMessage(Resources.SendAnswerCounterpartySuccessfully);
                                      }
                                    }
                                    else
                                    {
                                      x.AddError(Resources.RejectCertificateNotFound);
                                    }
                                    
                                    #endregion
                                  }
                                }
                              });
      dialog.Show();
    }

    /// <summary>
    /// Обработать комплект документов для Сбис.
    /// </summary>
    /// <param name="exchangeDocumentInfo">Информация о документе обмена из комплекта.</param>
    /// <param name="x">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <returns>Список документов комплекта.</returns>
    [Obsolete("Теперь функция не актуальна, т.к. реализована поддержка частичного подписания.")]
    private static List<Docflow.IOfficialDocument> ProcessSbisDocumentsPackage(IExchangeDocumentInfo exchangeDocumentInfo, CommonLibrary.InputDialogButtonClickEventArgs x,
                                                                               ICertificate certificate = null)
    {
      var documentsFromPackage = new List<Docflow.IOfficialDocument>();
      var packageDocumentsExchangeInfos = Functions.Module.Remote.GetPackageDocumentsExchangeInfos(exchangeDocumentInfo.ServiceMessageId);
      if (packageDocumentsExchangeInfos.Count() > 1)
      {
        // Проверить наличие прав на все документы комплекта у отправителя.
        if (!Functions.Module.Remote.HasRightsToPackageExchangeDocuments(packageDocumentsExchangeInfos))
        {
          x.AddError(Sungero.Exchange.Resources.NotFoundOrNoRightsToDocument);
          return documentsFromPackage;
        }
        
        foreach (Exchange.IExchangeDocumentInfo exchangeInfo in packageDocumentsExchangeInfos)
        {
          if (certificate != null)
          {
            var packageDocumentInfo = Functions.Module.Remote.GetInfoForSendToCounterparty(exchangeInfo.Document);
            if (!packageDocumentInfo.Certificates.Certificates.Any(c => c.Id == certificate.Id))
            {
              x.AddError(Sungero.Exchange.Resources.SendSetSignaturesToSbis);
              return new List<Docflow.IOfficialDocument>();
            }
          }
          documentsFromPackage.Add(exchangeInfo.Document);
        }
      }
      else
      {
        if (packageDocumentsExchangeInfos.Any())
          documentsFromPackage.Add(packageDocumentsExchangeInfos.First().Document);
      }
      return documentsFromPackage;
    }
    
    /// <summary>
    /// Перегенерировать Public Body.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    public static void GeneratePublicBody(string documentId)
    {
      Functions.Module.Remote.GeneratePublicBody(int.Parse(documentId));
    }
    
    /// <summary>
    /// Создать QueueItem.
    /// </summary>
    /// <param name="businessUnitBoxId">ИД абонентского ящика.</param>
    /// <param name="messageId">ИД сообщения.</param>
    public static void CreateQueueItem(string businessUnitBoxId, string messageId)
    {
      Functions.Module.Remote.CreateQueueItem(int.Parse(businessUnitBoxId), messageId);
    }
  }
}
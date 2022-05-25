using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ExchangeDocumentProcessingAssignmentFunctions
  {

    /// <summary>
    /// Показать диалог выбора главного документа.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="needSignDocuments">Документы, требующие подписания.</param>
    /// <param name="currentAction">Действие по отправке.</param>
    /// <returns>Документ.</returns>
    public static Sungero.Domain.Shared.IEntity ShowMainDocumentChoosingDialog(List<Content.IElectronicDocument> documents,
                                                                               List<Content.IElectronicDocument> needSignDocuments,
                                                                               Domain.Shared.IActionInfo currentAction)
    {
      // Определить подходящие документы.
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(documents, currentAction);
      var needSignDocumentList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(needSignDocuments, currentAction);
      if (needSignDocumentList.Any())
        documentsList = needSignDocumentList;
      
      return Docflow.PublicFunctions.OfficialDocument.ChooseMainDocument(documentsList, needSignDocuments);
    }
    
    /// <summary>
    /// Отправить извещения о получении.
    /// </summary>
    /// <param name="certificate">Сертификат для подписания ИОП. Чтобы не запрашивать повторно, при одновременной отправке ИОП и УОУ. Может быть null - тогда подберется автоматически.</param>
    /// <returns>Результат отправки или подтверждения выполнения без отправки ИОП.</returns>
    public bool SendDeliveryConfirmation(ICertificate certificate)
    {
      var result = Exchange.Functions.Module.SendDeliveryConfirmation(_obj.Box, certificate, false);
      if (!string.IsNullOrEmpty(result))
      {
        // Если в ящике указан сертификат для автоматической работы с ИОПами, то диалог тут лишний.
        // Исключение - ситуация, когда подписание как раз и было выполнено указанным сертификатом.
        var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(_obj.Box);
        if (rootBox.CertificateReceiptNotifications == null || Equals(rootBox.CertificateReceiptNotifications, certificate))
        {
          var dialog = Dialogs.CreateTaskDialog(ExchangeDocumentProcessingAssignments.Resources.CompleteAsgWithoutSendDeliveryConfirmation, result, MessageType.Question);
          dialog.Buttons.AddYesNo();
          
          return dialog.Show() == DialogButtons.Yes;
        }
      }
      
      return true;
    }
  }
}
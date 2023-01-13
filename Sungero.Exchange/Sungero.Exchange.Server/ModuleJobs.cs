using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentInfoServiceDocuments;
using DcxClient = NpoComputer.DCX.ClientApi.Client;

namespace Sungero.Exchange.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Отправка подписанных ИОП.
    /// </summary>
    public virtual void SendSignedReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        SendSignedReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания ИОП.
    /// </summary>
    public virtual void CreateReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Where(x => x.CertificateReceiptNotifications != null).ToList();
      foreach (var box in boxes)
      {
        CreateReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания задач на отправку извещений о получении документов.
    /// </summary>
    public virtual void SendReceiptNotificationTasks()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Select(b => b.Id).ToList();
      foreach (var box in boxes)
      {
        SendReceiptNotificationTask(box);
      }
    }
    
    /// <summary>
    /// Реализация агента для конкретного ящика, чтобы можно было выполнить в транзакции.
    /// </summary>
    /// <param name="boxId">Id ящика.</param>
    private static void SendReceiptNotificationTask(int boxId)
    {
      var box = ExchangeCore.BusinessUnitBoxes.Get(boxId);
      var hasCertificate = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(box, box.Responsible);
      if (!hasCertificate)
        Logger.DebugFormat("Can't start Receipt Notification Sending Task. No certificates for responsible");
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      
      // Если отправить ИОПы нельзя, то новая задача не создается.
      var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(box) as NpoComputer.DCX.ClientApi.Client;
      var documentsToFix = new List<IExchangeDocumentInfo>();
      foreach (var documentInfo in documentInfos)
      {
        var canSendDeliveryConfirmation = true;
        try
        {
          canSendDeliveryConfirmation = client.CanSendDeliveryConfirmation(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Error while getting document from the service to generate delivery confirmation: {0}. ServiceMessageId: {1}, ServiceDocumentId: {2}",
                             ex.Message, documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        if (!canSendDeliveryConfirmation)
          documentsToFix.Add(documentInfo);
      }

      if (documentsToFix.Any())
      {
        foreach (var info in documentsToFix)
          Transactions.Execute(() =>
                               {
                                 var exchangeInfo = ExchangeDocumentInfos.Get(info.Id);
                                 Functions.Module.FixReceiptNotification(exchangeInfo, string.Empty, false);
                               });
      }
      
      var tasks = ReceiptNotificationSendingTasks.GetAll()
        .Where(x => Equals(x.Box, box) && Equals(x.Status, Exchange.ReceiptNotificationSendingTask.Status.InProcess));
      foreach (var task in tasks)
        try
      {
        task.Abort();
        Logger.DebugFormat("Aborted Receipt Notification Sending Task {0} for box {1}", task.Id, box.Id);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Abort task {0} failed, box {1}, exception \r\n {2}", task.Id, box.Id, ex);
      }
      
      var responsible = box.Responsible;
      documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      var previousDay = Calendar.Today.PreviousWorkingDay().EndOfDay();
      var previousDayDocumentInfos = documentInfos.Where(x => x.MessageDate <= previousDay).ToList();
      if (previousDayDocumentInfos.Any() && hasCertificate)
      {
        Logger.DebugFormat("Document infos ids without receipt notification: {0}",  string.Join(", ", previousDayDocumentInfos.Select(x => x.Id)));
        
        // Выдать права на чтение документам. Без прав ИОП не отправить.
        foreach (var documentInfo in documentInfos)
        {
          var document = documentInfo.Document;
          if (!document.AccessRights.CanRead(responsible))
          {
            document.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Read);
            document.Save();
          }
        }
        
        var receiptNotificationSendingTask = Functions.Module.CreateReceiptNotificationSendingTask(box);
        receiptNotificationSendingTask.Start();
        Logger.DebugFormat("Started Receipt Notification Sending Task {0} for box {1}", receiptNotificationSendingTask.Id, box.Id);
      }
    }
    
    /// <summary>
    /// Агент получения сообщений.
    /// </summary>
    public virtual void GetMessages()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("GetMessages. Start. Queue items count: '{0}'.", ExchangeCore.MessageQueueItems.GetAll().Count()));
      
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        var incomingMessageId = Functions.Module.GetLastIncomingMessageId(box);
        var outgoingMessageId = Functions.Module.GetLastOutgoingMessageId(box);
        
        // Если это первый запуск - явно вызываем синхронизацию контрагентов, у неё таймаут большой.
        if (string.IsNullOrWhiteSpace(incomingMessageId) && string.IsNullOrWhiteSpace(outgoingMessageId))
        {
          Exchange.PublicFunctions.Module.LogDebugFormat("GetMessages. Is first execute job. Requeue counterparty sync.");
          ExchangeCore.PublicFunctions.Module.Remote.RequeueCounterpartySync();
        }
        
        Functions.Module.SyncMessages(box, incomingMessageId, outgoingMessageId);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat("GetMessages. Run exchange checkup.");
      foreach (var box in boxes)
      {
        Functions.Module.RunExchangeCheckup(box);
      }
      
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("GetMessages. Done. Queue items count: '{0}'.", ExchangeCore.MessageQueueItems.GetAll().Count()));
    }

    /// <summary>
    /// Агент конвертации тел документов.
    /// </summary>
    public virtual void BodyConverterJob()
    {
      Exchange.PublicFunctions.Module.LogDebugFormat("BodyConverterJob. Start.");
      // Берем только ИД, не вытягиваем сущности чтобы кеш хибера был чистый, нужно для избежания потери данных при сохранении измененной сущности.
      var queueItemIds = this.GetActualBodyConverterQueueItems();
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("BodyConverterJob. Queue items count: {0}.", queueItemIds.Count));
      
      var queueItemsForDelete = new List<int>();
      
      foreach (var queueItemId in queueItemIds)
      {
        if (this.IsObsoleteBodyConverterQueueItem(queueItemId))
        {
          Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("BodyConverterJob. Queue item: {0} is obsolete.", queueItemId));
          queueItemsForDelete.Add(queueItemId);
          continue;
        }
        
        var startTime = Calendar.Now;
        var queueItem = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => x.Id == queueItemId).SingleOrDefault();
        
        if (queueItem == null)
          continue;
        
        if (queueItem.Document == null)
          continue;

        var lockInfo = Locks.GetLockInfo(queueItem.Document);
        if (lockInfo.IsLocked)
        {
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "BodyConverterJob. Document is locked.");
          continue;
        }
        
        var generated = false;
        
        var transactionSuccess = Transactions.Execute(
          () =>
          {
            // Dmitriev_IA:
            // Переполучение queueItem, т.к. при выполнении Transactions.Execute сбрасывается сессия NHibernate и полученные ранее сущности "забываются".
            // см. User Story 199135.
            queueItem = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => x.Id == queueItemId).SingleOrDefault();
            if (queueItem == null || queueItem.Document == null)
              return;
            generated = Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(queueItem.Document, queueItem.VersionId.Value, queueItem.ExchangeState, startTime);
          });
        
        if (generated && transactionSuccess)
        {
          queueItemsForDelete.Add(queueItemId);
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "BodyConverterJob. Document success converted, queue item will be deleted.");
        }
        else
        {
          Transactions.Execute(
            () =>
            {
              // Dmitriev_IA:
              // Переполучение queueItem, т.к. при выполнении Transactions.Execute сбрасывается сессия NHibernate и полученные ранее сущности "забываются".
              // см. User Story 199135.
              queueItem = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => x.Id == queueItemId).SingleOrDefault();
              if (queueItem == null)
                return;
              ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, Resources.GeneratePublicBodyFailed);
            });
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, string.Format("BodyConverterJob. {0}.", Resources.GeneratePublicBodyFailed));
        }
      }
      
      // Оставляем удаление завершенных элементов очереди для старых записей.
      var processedQueueItems = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(q => Equals(q.ProcessingStatus, ExchangeCore.BodyConverterQueueItem.ProcessingStatus.Processed))
        .Select(q => q.Id)
        .ToList();
      
      queueItemsForDelete.AddRange(processedQueueItems);
      this.ClearBodyConverterQueueItems(queueItemsForDelete);
      
      Exchange.PublicFunctions.Module.LogDebugFormat("BodyConverterJob. Done.");
    }
    
    /// <summary>
    /// Получить элементы очереди конвертации.
    /// </summary>
    /// <returns>Список ид элементов очереди ковертации.</returns>
    public virtual List<int> GetActualBodyConverterQueueItems()
    {
      var queueItemIds = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.Retries == 0 && x.ProcessingStatus != ExchangeCore.BodyConverterQueueItem.ProcessingStatus.Processed)
        .Select(x => x.Id)
        .ToList();
      
      // Ошибочные документы обрабатываются последними пачкой по 25.
      var repeatedQueueItemIds = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.Retries > 0)
        .OrderBy(y => y.Retries)
        .Take(25)
        .Select(x => x.Id)
        .ToList();
      
      queueItemIds.AddRange(repeatedQueueItemIds);
      
      return queueItemIds;
    }
    
    /// <summary>
    /// Признак устаревшего элемента очереди конвертации.
    /// </summary>
    /// <param name="queueItemId">Ид элемента очереди.</param>
    /// <returns>True, если очередь устарела.</returns>
    public virtual bool IsObsoleteBodyConverterQueueItem(int queueItemId)
    {
      // Вернет 0 если документ или очередь не найдена.
      var documentId = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.Id == queueItemId && x.Document != null)
        .Select(x => x.Document.Id)
        .SingleOrDefault();

      if (documentId == 0)
        return true;
      
      var versionId = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.Id == queueItemId)
        .Select(x => x.VersionId)
        .SingleOrDefault();
      
      if (versionId == null)
        return true;

      var equalsQueueItems = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => Equals(x.Document.Id, documentId) &&
                                                                                 Equals(x.VersionId, versionId) &&
                                                                                 x.Id != queueItemId);

      if (equalsQueueItems.Any() && queueItemId < equalsQueueItems.Max(x => x.Id))
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Удалить элементы очереди конвертации.
    /// </summary>
    /// <param name="queueItemIds">Ид элементов очереди.</param>
    public virtual void ClearBodyConverterQueueItems(List<int> queueItemIds)
    {
      var queueItemsForDelete = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => queueItemIds.Contains(x.Id)).ToList();
      
      foreach (var queueItem in queueItemsForDelete)
      {
        Transactions.Execute(
          () =>
          {
            ExchangeCore.BodyConverterQueueItems.Delete(queueItem);
          });
      }
    }

    /// <summary>
    /// Реализация агента создания ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void CreateReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(box, "Execute CreateReceiptNotifications.");
      var partSize = 25;
      var skip = 0;
      var certificate = box.CertificateReceiptNotifications;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          var serviceDocs = Functions.Module.GetGeneratedDeliveryConfirmationDocuments(documentInfos, box, box.CertificateReceiptNotifications, true);
          
          foreach (var doc in serviceDocs)
          {
            var info = doc.Info;
            if (info.ServiceDocuments.Any(d => d.DocumentType == doc.ReglamentDocumentType))
              continue;
            
            var serviceDocument = info.ServiceDocuments.AddNew();
            serviceDocument.DocumentType = doc.ReglamentDocumentType;
            serviceDocument.Body = doc.Content;
            serviceDocument.GeneratedName = doc.Name;
            serviceDocument.DocumentId = doc.ServiceDocumentId;
            serviceDocument.StageId = doc.ServiceDocumentStageId;
            serviceDocument.Certificate = doc.Certificate;
            serviceDocument.ParentDocumentId = doc.ParentDocumentId;
            
            // Выдать права на документ подписывающему ИОП.
            var document = info.Document;
            if (!document.AccessRights.CanRead(certificate.Owner))
            {
              document.AccessRights.Grant(certificate.Owner, DefaultAccessRightsTypes.Read);
              document.Save();
            }
            
            info.Save();
          }
          
          var documentsToFix = documentInfos.Where(x => !serviceDocs.Any(s => Equals(s.LinkedDocument, x.Document))).ToList();
          if (documentsToFix.Any())
            Functions.Module.FixReceiptNotification(documentsToFix, string.Empty);
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Exchange.PublicFunctions.Module.LogErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
        }
      }

    }
    
    /// <summary>
    /// Реализация отправки подписанных ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void SendSignedReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      Exchange.PublicFunctions.Module.LogDebugFormat(box, "Execute SendSignedReceiptNotifications.");
      var partSize = 25;
      var skip = 0;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          var documentsToSend = new List<Structures.Module.ReglamentDocumentWithCertificate>();
          
          foreach (var info in documentInfos)
          {
            Func<Enumeration?, bool> isRootDocumentReceipt = x => x == DocumentType.Receipt || x == DocumentType.IReceipt;
            var isInvoiceFlow = Functions.Module.IsInvoiceFlowDocument(info.Document);
            var reglamentDocument = info.ServiceDocuments
              .Where(d => d.Sign != null && d.Date == null)
              .Select(d =>
                      {
                        var parentId = info.ServiceDocumentId;
                        var counterpartyId = info.ServiceCounterpartyId;
                        return Structures.Module.ReglamentDocumentWithCertificate.Create(d.GeneratedName, d.Body, d.Certificate,
                                                                                         d.Sign, parentId, box, info.Document,
                                                                                         info.ServiceMessageId, d.DocumentId, d.StageId,
                                                                                         counterpartyId,
                                                                                         isRootDocumentReceipt(d.DocumentType), info,
                                                                                         isInvoiceFlow, d.DocumentType, 
                                                                                         d.FormalizedPoAUnifiedRegNo);
                      })
              .ToList();
            documentsToSend.AddRange(reglamentDocument);
          }
          if (documentsToSend.Any())
          {
            Exchange.PublicFunctions.Module.LogDebugFormat(box,
                                                           string.Format("Execute SendSignedReceiptNotifications. Processing document infos: {0}", string.Join(", ", documentsToSend.Select(d => d.Info.Id.ToString()).ToList())));
            Sungero.Exchange.Functions.Module.SendDeliveryConfirmation(documentsToSend, box);
          }
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Exchange.PublicFunctions.Module.LogErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
        }
      }
    }
  }
}
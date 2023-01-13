using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommonLibrary;
using NpoComputer.DCX.Common;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Exchange.Structures.Module;
using Sungero.ExchangeCore;
using Sungero.Metadata;
using Sungero.Parties;
using Sungero.Workflow;
using Calendar = Sungero.Core.Calendar;
using DcxClient = NpoComputer.DCX.ClientApi.Client;
using ExchDocumentType = Sungero.Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType;
using FinancialFunction = Sungero.FinancialArchive.PublicFunctions;
using Signature = NpoComputer.DCX.Common.Signature;

namespace Sungero.Exchange.Server
{
  public class ModuleFunctions
  {
    #region Обработка входящих сообщений

    #region Разбор сообщений из сервиса

    /// <summary>
    /// Обработка входящих сообщений.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик.</param>
    /// <param name="lastIncomingId">Id входящего сообщения, с которого нужно начать обработку.</param>
    /// <param name="lastOutgoingId">Id исходящего сообщения, с которого нужно начать обработку.</param>
    public virtual void SyncMessages(ExchangeCore.IBusinessUnitBox businessUnitBox, string lastIncomingId, string lastOutgoingId)
    {
      try
      {
        var client = GetClient(businessUnitBox);
        var lastIncomingEventId = string.Empty;
        var lastOutgoingEventId = string.Empty;
        
        var lastIncomingDefaultId = string.Empty;
        var lastOutgoingDefaultId = string.Empty;
        
        var existLastId = !string.IsNullOrWhiteSpace(lastIncomingId) || !string.IsNullOrWhiteSpace(lastOutgoingId);
        
        if (existLastId)
        {
          lastIncomingDefaultId = string.IsNullOrWhiteSpace(lastIncomingId) ? lastOutgoingId : lastIncomingId;
          lastOutgoingDefaultId = string.IsNullOrWhiteSpace(lastOutgoingId) ? lastIncomingId : lastOutgoingId;
        }
        
        this.LogDebugFormat(businessUnitBox, "Start receiving messages from the service.");
        var messages = (existLastId ? client.GetMessages(lastIncomingDefaultId, lastOutgoingDefaultId, out lastIncomingEventId, out lastOutgoingEventId) :
                        client.GetMessages(Calendar.Today.ToUtcTime().Value, out lastIncomingEventId, out lastOutgoingEventId)).ToList();
        this.LogDebugFormat(businessUnitBox, "Done receiving messages from the service. Count messages = '{0}'.", messages.Count);
        
        foreach (var message in messages)
        {
          // Добавление в очередь.
          this.CreateQueueItem(businessUnitBox, message);
        }
        
        if (!string.IsNullOrEmpty(lastIncomingEventId) && !Equals(lastIncomingEventId, lastIncomingId))
          this.UpdateLastIncomingMessageId(businessUnitBox, lastIncomingEventId);
        
        if (!string.IsNullOrEmpty(lastOutgoingEventId) && !Equals(lastOutgoingEventId, lastOutgoingId))
          this.UpdateLastOutgoingMessageId(businessUnitBox, lastOutgoingEventId);
        
        var queueItems = ExchangeCore.MessageQueueItems.GetAll(q => Equals(q.RootBox, businessUnitBox) &&
                                                               !Equals(q.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Suspended)).ToList();
        
        // Дозагрузка сообщений из очереди.
        this.LoadMessagesFromQueueItems(messages, queueItems, client);

        // Обработка сообщений из очереди.
        this.ProcessMessages(messages, queueItems, client);

        // Удаление из очереди обработанных сообщений.
        this.DeleteProcessedQueueItems(businessUnitBox, client);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(ExchangeCore.BusinessUnitBoxes.Resources.BoxError, ex, businessUnitBox.Id);
      }
    }

    /// <summary>
    /// Обработка сообщений очереди.
    /// </summary>
    /// <param name="messages">Сообщения.</param>
    /// <param name="queueItems">Элементы очереди.</param>
    /// <param name="client">Клиент.</param>
    protected virtual void ProcessMessages(List<IMessage> messages, List<IMessageQueueItem> queueItems, DcxClient client)
    {
      this.LogDebugFormat("Start process messages.");
      
      var queueItemsIds = queueItems.Select(q => q.Id).ToList();
      foreach (var message in messages)
      {
        if (message.HasErrors == true)
        {
          Logger.ErrorFormat("Message Id {0} not processed, service error {1}", message.ServiceMessageId, message.ErrorText);
          continue;
        }
        
        // Сообщение из сервиса.
        this.LogDebugFormat(message, "Start process message.");
        this.LogFullMessage(message);

        Transactions.Execute(
          () =>
          {
            var transactionQueueItems = ExchangeCore.MessageQueueItems.GetAll(q => queueItemsIds.Contains(q.Id)).ToList();
            if (this.ProcessMessage(message, transactionQueueItems, client))
            {
              var queueItem = transactionQueueItems.Single(x => x.ExternalId == message.ServiceMessageId);
              queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Processed;
              queueItem.Save();
              this.LogDebugFormat(message, "Message processed successfully.");
            }
            else
            {
              var queueItem = transactionQueueItems.SingleOrDefault(x => x.ExternalId == message.ServiceMessageId);
              if (queueItem != null)
              {
                queueItem.Retries += 1;
                queueItem.Save();
                this.LogDebugFormat(message, "Process message failed. Retries: '{0}'.", queueItem.Retries);
              }
            }
          });

        this.LogDebugFormat(message, "End process message.");
      }
      
      this.LogDebugFormat("End process messages.");
    }
    
    /// <summary>
    /// Добавление сообщений в очередь.
    /// </summary>
    /// <param name="businessUnitBoxId">ИД ящика НОР.</param>
    /// <param name="serviceMessageId">ИД Сообщение.</param>
    [Remote]
    public virtual void CreateQueueItem(int businessUnitBoxId, string serviceMessageId)
    {
      var businessUnitBox = BusinessUnitBoxes.Get(businessUnitBoxId);
      var client = GetClient(businessUnitBox);
      var serviceMessage = client.GetMessage(serviceMessageId);
      this.CreateQueueItem(businessUnitBox, serviceMessage);
      RequeueMessagesGet();
    }
    
    /// <summary>
    /// Получить ящик из сообщения.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик нашей организации.</param>
    /// <param name="message">Сообщение.</param>
    /// <returns>Ящик.</returns>
    protected virtual IBoxBase GetMessageBox(IBusinessUnitBox businessUnitBox, IMessage message)
    {
      if (message.Sender == null)
        return businessUnitBox;
      
      var organizationId = message.Sender.Organization.OrganizationId;
      bool isIncoming = organizationId != businessUnitBox.OrganizationId;
      var box = ExchangeCore.BoxBases.Null;
      if (isIncoming && message.ToDepartment != null && ExchangeCore.DepartmentBoxes
          .GetAll(x => x.ServiceId == message.ToDepartment.Id && x.Status == CoreEntities.DatabookEntry.Status.Active).Any())
        box = ExchangeCore.DepartmentBoxes.GetAll(x => x.ServiceId == message.ToDepartment.Id && x.Status == CoreEntities.DatabookEntry.Status.Active)
          .Single();
      else if (!isIncoming && message.FromDepartment != null && ExchangeCore.DepartmentBoxes
               .GetAll(x => x.ServiceId == message.FromDepartment.Id && x.Status == CoreEntities.DatabookEntry.Status.Active).Any())
        box = ExchangeCore.DepartmentBoxes.GetAll(x => x.ServiceId == message.FromDepartment.Id && x.Status == CoreEntities.DatabookEntry.Status.Active)
          .Single();
      else
        box = businessUnitBox;
      
      return box;
    }
    
    /// <summary>
    /// Добавление сообщений в очередь.
    /// </summary>
    /// <param name="businessUnitBox">Ящик НОР.</param>
    /// <param name="message">Сообщение.</param>
    protected virtual void CreateQueueItem(IBusinessUnitBox businessUnitBox, IMessage message)
    {
      if (ExchangeCore.MessageQueueItems.GetAll(q => Equals(q.RootBox, businessUnitBox) && q.ExternalId == message.ServiceMessageId).Any())
        return;
      
      var queueItem = ExchangeCore.MessageQueueItems.Create();
      queueItem.ExternalId = message.ServiceMessageId;
      queueItem.Box = this.GetMessageBox(businessUnitBox, message);
      queueItem.RootBox = businessUnitBox;
      queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.NotProcessed;
      queueItem.Created = Calendar.Now;
      queueItem.Name = message.ServiceMessageId;
      
      if (!message.HasErrors)
      {
        var organizationId = message.Sender.Organization.OrganizationId;
        bool isIncoming = organizationId != businessUnitBox.OrganizationId;
        queueItem.CounterpartyExternalId = isIncoming ? organizationId : message.Receiver.Organization.OrganizationId;
      }
      
      if (message.HasErrors)
      {
        var note = string.Format("{0}{1}{2}", queueItem.Note, Environment.NewLine, message.ErrorText);
        if (note.Length > 1000)
          note = note.Substring(0, 1000);
        queueItem.Note = note;
      }
      
      foreach (var primary in message.PrimaryDocuments)
      {
        var itemDocument = queueItem.Documents.AddNew();
        itemDocument.ExternalId = primary.ServiceEntityId;
        itemDocument.Type = ExchangeCore.MessageQueueItemDocuments.Type.Primary;
      }

      foreach (var reglament in message.ReglamentDocuments)
      {
        var itemDocument = queueItem.Documents.AddNew();
        itemDocument.ExternalId = reglament.ServiceEntityId;
        itemDocument.Type = ExchangeCore.MessageQueueItemDocuments.Type.Reglament;
      }

      queueItem.Save();
      this.LogDebugFormat(message, queueItem, businessUnitBox, "CreateQueueItem. Add message to queue item.");
    }

    /// <summary>
    /// Дозагрузка сообщений из очереди.
    /// </summary>
    /// <param name="messages">Сообщения.</param>
    /// <param name="queueItems">Элементы очереди.</param>
    /// <param name="client">Клиент.</param>
    protected virtual void LoadMessagesFromQueueItems(List<IMessage> messages, List<IMessageQueueItem> queueItems, DcxClient client)
    {
      var addedItems = queueItems.Where(x => messages.All(c => c.ServiceMessageId != x.ExternalId) &&
                                        !Equals(x.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Processed))
        .ToList();
      
      this.LogDebugFormat(string.Format("LoadMessagesFromQueueItems. Added queue items count: {0}.", addedItems.Count));
      
      foreach (var queueItem in addedItems)
      {
        var added = Transactions.Execute(
          () =>
          {
            var message = client.GetMessage(queueItem.ExternalId);
            if (message != null)
              messages.Add(message);
            else
            {
              queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Processed;
              queueItem.Save();
              Logger.DebugFormat("Exchange. Not found service message with Id = '{0}'.", queueItem.ExternalId);
            }
          });
        if (!added)
        {
          Transactions.Execute(
            () => { ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, Resources.GetMessageFailed); });
          this.LogDebugFormat(queueItem, Resources.GetMessageFailed);
        }
      }
    }
    
    /// <summary>
    /// Удалить обработанные элементы очереди.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик.</param>
    /// <param name="client">Dcx клиент.</param>
    protected virtual void DeleteProcessedQueueItems(IBusinessUnitBox businessUnitBox, DcxClient client)
    {
      var processedQueueItems = ExchangeCore.MessageQueueItems.GetAll(q => Equals(q.RootBox, businessUnitBox))
        .Where(q => Equals(q.ProcessingStatus, ExchangeCore.MessageQueueItem.ProcessingStatus.Processed))
        .ToList();
      foreach (var queueItem in processedQueueItems)
      {
        Transactions.Execute(
          () =>
          {
            var useRetry = false;
            var itemDocuments = queueItem.Documents.Select(d => d.ExternalId).ToList();
            var documentInfos = ExchangeDocumentInfos
              .GetAll(i => Equals(i.RootBox, businessUnitBox)
                      && Equals(i.ServiceMessageId, queueItem.ExternalId)
                      && itemDocuments.Contains(i.ServiceDocumentId)
                      && i.Document != null)
              .ToList();

            foreach (var documentInfo in documentInfos)
            {
              var canSendDeliveryConfirmation = true;
              try
              {
                canSendDeliveryConfirmation = client.CanSendDeliveryConfirmation(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
              }
              catch (Exception ex)
              {
                useRetry = true;
                this.LogDebugFormat(documentInfo, "Error while getting document from the service to generate delivery confirmation: {0}.", ex.Message);
              }
              
              if (!canSendDeliveryConfirmation && !useRetry)
                FixReceiptNotification(documentInfo, string.Empty, false);
            }

            if (!useRetry)
              ExchangeCore.MessageQueueItems.Delete(queueItem);
          });
      }
    }
    
    /// <summary>
    /// Получить id последнего входящего сообщения.
    /// </summary>
    /// <param name="box">Ящик.</param>
    /// <returns>Id сообщения.</returns>
    [Public, Remote]
    public virtual string GetLastIncomingMessageId(ExchangeCore.IBusinessUnitBox box)
    {
      var key = string.Format(Constants.Module.LastBoxIncomingMessageId, box.Id);
      var command = string.Format(Queries.Module.GetLastMessageId, key);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var result = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          result = executionResult.ToString();
        this.LogDebugFormat(box, "Get messages. Last incoming message id in DB is {0}", result);
        return result;
      }
      catch (Exception ex)
      {
        this.LogDebugFormat(box, "Error while getting incoming message id. No messages in box. {0}", ex);
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Получить id последнего исходящего сообщения.
    /// </summary>
    /// <param name="box">Ящик.</param>
    /// <returns>Id сообщения.</returns>
    [Public, Remote]
    public virtual string GetLastOutgoingMessageId(ExchangeCore.IBusinessUnitBox box)
    {
      var key = string.Format(Constants.Module.LastBoxOutgoingMessageId, box.Id);
      var command = string.Format(Queries.Module.GetLastMessageId, key);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var result = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          result = executionResult.ToString();
        this.LogDebugFormat(box, "Get messages. Last outgoing message id in DB is {0}", result);
        return result;
      }
      catch (Exception ex)
      {
        this.LogDebugFormat(box, "Error while getting outgoing message id. No messages in box. {0}", ex);
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Обновить id полученного входящего сообщения.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="incomingMessageId">Новый id.</param>
    [Public, Remote]
    public virtual void UpdateLastIncomingMessageId(ExchangeCore.IBusinessUnitBox box, string incomingMessageId)
    {
      var key = string.Format(Constants.Module.LastBoxIncomingMessageId, box.Id);
      
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.UpdateLastMessageId, new[] { key, incomingMessageId });
      this.LogDebugFormat(box, "Last box incoming message id is set to {0}", incomingMessageId);
    }
    
    /// <summary>
    /// Обновить id полученного исходящего сообщения.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="outgoingMessageId">Новый id.</param>
    [Public, Remote]
    public virtual void UpdateLastOutgoingMessageId(ExchangeCore.IBusinessUnitBox box, string outgoingMessageId)
    {
      var key = string.Format(Constants.Module.LastBoxOutgoingMessageId, box.Id);
      
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.UpdateLastMessageId, new[] { key, outgoingMessageId });
      this.LogDebugFormat(box, "Last box outgoing message id is set to {0}", outgoingMessageId);
    }

    #endregion

    #region Обработка одного сообщения

    /// <summary>
    /// Обработать сообщение.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItems">Обрабатываемые элементы очереди.</param>
    /// <param name="client">Клиент.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessMessage(IMessage message, List<IMessageQueueItem> queueItems, DcxClient client)
    {
      var queueItem = queueItems.Single(x => x.ExternalId == message.ServiceMessageId);
      var box = this.GetMessageBox(queueItem.RootBox, message);
      var businessUnitBox = queueItem.RootBox;
      
      this.LogDebugFormat(message, queueItem, businessUnitBox, "Execute ProcessMessage.");
      
      var organizationId = message.Sender.Organization.OrganizationId;
      var isIncoming = true;
      
      // Обрабатываем исходящие сообщения для поддержки параллельных действий.
      if (organizationId == businessUnitBox.OrganizationId)
      {
        organizationId = message.Receiver.Organization.OrganizationId;
        isIncoming = false;
      }
      
      var sender = Parties.Counterparties.GetAll(c => c.ExchangeBoxes.Any(e => Equals(e.OrganizationId, organizationId) && Equals(businessUnitBox, e.Box))).SingleOrDefault();
      if (sender == null)
      {
        /* Список контактов с сервиса для СБИСа всегда пустой, но для автоматического создания КА
         * необходимо сохранить сообщение в очереди, чтобы оно не потерялось, а также сохранить информацию
         * о КА для последующей синхронизации в RX.
         */
        var counterparties = client.GetContacts();
        if (!counterparties.Any(x => x.Organization.OrganizationId == organizationId) && client.CanSynchronizeContacts)
        {
          this.ProcessInvoiceConfirmation(message, queueItem, organizationId, businessUnitBox);
          this.ProcessReceiptNotice(message, queueItem, null, isIncoming, businessUnitBox);
          return true;
        }
        else if (!client.CanSynchronizeContacts)
        {
          this.AddCounterpartyQueueItem(businessUnitBox, organizationId);
        }
        
        this.LogDebugFormat(message, queueItem, businessUnitBox, "Unknown counterparty with OrganizationId: '{0}'. It is necessary to synchronize counterparties.", organizationId);
        return false;
      }
      
      if (message.PrimaryDocuments.Any() && message.PrimaryDocuments.Any(x => x.DocumentType == DocumentType.RevocationOffer))
      {
        this.ProcessAnnulmentOrCancellation(message, queueItems, sender, isIncoming, box);
        if (!message.ReglamentDocuments.Any() && message.PrimaryDocuments.All(x => x.DocumentType == DocumentType.RevocationOffer))
          return true;
      }
      
      if (!message.IsReply)
      {
        return this.ProcessNewMessage(message, queueItem, box, businessUnitBox, sender, organizationId, isIncoming);
      }
      else
      {
        return this.ProcessReplyMessage(message, queueItem, queueItems, client, box, businessUnitBox, sender, organizationId, isIncoming);
      }
    }

    /// <summary>
    /// Добавить контрагента из сообщения в очередь синхронизации.
    /// </summary>
    /// <param name="businessUnitBox">Абонентский ящик НОР.</param>
    /// <param name="organizationId">ИД организации контрагента.</param>
    protected virtual void AddCounterpartyQueueItem(IBusinessUnitBox businessUnitBox, string organizationId)
    {
      this.LogDebugFormat(businessUnitBox, "Execute AddCounterpartyQueueItem.");
      if (!CounterpartyQueueItems.GetAll(c => c.ExternalId == organizationId && Equals(c.Box, businessUnitBox)).Any())
      {
        var counterpartyQueueItem = CounterpartyQueueItems.Create();
        counterpartyQueueItem.ExternalId = organizationId;
        counterpartyQueueItem.Box = businessUnitBox;
        counterpartyQueueItem.RootBox = businessUnitBox;
        counterpartyQueueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.NotProcessed;
        counterpartyQueueItem.Save();
        this.LogDebugFormat(businessUnitBox, "Create queue item for counterparty OrganizationId {0}.", organizationId);
      }
    }
    
    /// <summary>
    /// Обработать новое сообщение.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="box">Ящик, на который получено сообщение.</param>
    /// <param name="businessUnitBox">Ящик нашей организации.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="organizationId">Идентификатор отправителя в сервисе обмена.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessNewMessage(IMessage message, IMessageQueueItem queueItem, IBoxBase box,
                                             IBusinessUnitBox businessUnitBox, ICounterparty sender,
                                             string organizationId, bool isIncoming)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessNewMessage.");
      if (isIncoming && message.ReglamentDocuments.Any(x => x.DocumentType == NpoComputer.DCX.Common.ReglamentDocumentType.DeliveryFailureNotification))
      {
        return this.ProcessDeliveryFailureNotification(message, box);
      }

      if (message.PrimaryDocuments.Any())
      {
        // Требование УОП.
        if (message.PrimaryDocuments.Any(x => x.NeedReceipt && x.Content == null))
        {
          if (isIncoming && ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box))
          {
            return this.ProcessReceiptRequire(message, queueItem, sender, box);
          }
        }
        else if (Functions.Module.IsMessageWithUnsupportedDocuments(message))
        {
          this.LogDebugFormat(message, queueItem, box, "Message contains unsupported documents.");
          // Некоторые документы не поддерживаются в системе.
          return this.ProcessMessageWithUnsupportedDocuments(message, sender, isIncoming, box);
        }
        else
        {
          // Создание новых документов.
          var processed = this.ProcessNewIncomingMessage(message, queueItem, sender, isIncoming, box);
          
          // Загрузка сервисных документов.
          return processed && this.ProcessInvoiceConfirmation(message, queueItem, organizationId, businessUnitBox);
        }
      }
      else
      {
        // Обработка ИОП.
        return this.ProcessReceiptNotice(message, queueItem, sender, isIncoming, businessUnitBox);
      }

      // Не смогли обработать - пропускаем и помечаем как обработанное.
      this.LogDebugFormat(message, queueItem, box, "Message processing was skipped. Message is marked as processed.");
      
      return true;
    }

    /// <summary>
    /// Обработать ответное сообщение.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="queueItems">Прочие необработанные элементы очереди.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="box">Ящик, на который получено сообщение.</param>
    /// <param name="businessUnitBox">Ящик нашей организации.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="organizationId">Идентификатор отправителя в сервисе обмена.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessReplyMessage(IMessage message,
                                               IMessageQueueItem queueItem, List<IMessageQueueItem> queueItems, DcxClient client, IBoxBase box,
                                               IBusinessUnitBox businessUnitBox, ICounterparty sender, string organizationId, bool isIncoming)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessReplyMessage.");
      var historyComment = string.Format("{0}|{1}", sender.Name, businessUnitBox.ExchangeService.Name);
      var historyOperation = new Enumeration(isIncoming ? Constants.Module.Exchange.GetAnswer : Constants.Module.Exchange.SendAnswer);
      
      var processResult = true;
      
      // Обработка УОП.
      if (message.ReglamentDocuments.Any(r => r.DocumentType == ReglamentDocumentType.NotificationReceipt))
        processResult = this.ProcessNoteReceipt(message, queueItem, sender, isIncoming, businessUnitBox);
      
      // Подпись неформализованного документа.
      if (message.PrimaryDocuments.Any(x => x.SignStatus == NpoComputer.DCX.Common.SignStatus.Signed &&
                                       x.DocumentType == NpoComputer.DCX.Common.DocumentType.Nonformalized &&
                                       message.Signatures.Any(s => s.DocumentId == x.ServiceEntityId)) && processResult)
        processResult = this.ProcessNonformalizedSign(message, queueItem, client, box, sender, isIncoming, historyOperation, historyComment) && processResult;

      // Отказ в подписании.
      if (message.ReglamentDocuments.Any(r => r.DocumentType == ReglamentDocumentType.AmendmentRequest ||
                                         r.DocumentType == ReglamentDocumentType.InvoiceAmendmentRequest ||
                                         r.DocumentType == ReglamentDocumentType.Rejection) && processResult)
        processResult = this.ProcessReject(message, queueItem, isIncoming, box, historyOperation, historyComment);
      
      // Обработка ИОП.
      if (message.ReglamentDocuments.Any(r => r.DocumentType == ReglamentDocumentType.Receipt ||
                                         r.DocumentType == ReglamentDocumentType.InvoiceReceipt ||
                                         r.DocumentType == ReglamentDocumentType.NotificationOnReceiptOfNotificationReceipt) && processResult)
        processResult = this.ProcessReceiptNotice(message, queueItem, sender, isIncoming, businessUnitBox) && processResult;

      // Обработка ИОП на УОП.
      if (message.ReglamentDocuments.Any(r => r.DocumentType == ReglamentDocumentType.NotificationOnReceiptOfNotificationReceipt) && processResult)
        processResult = this.ProcessReceiptOfNoteReceipt(message, queueItem, sender, isIncoming, businessUnitBox) && processResult;

      // Обработка подтверждения доставки.
      if (message.ReglamentDocuments.Any(r => r.DocumentType == ReglamentDocumentType.InvoiceConfirmation) && processResult)
        processResult = this.ProcessInvoiceConfirmation(message, queueItem, organizationId, businessUnitBox) && processResult;
      
      // Титулы формализованных документов и подпись на СЧФ СБИС.
      if ((message.ReglamentDocuments.Any(x => this.GetSupportedReglamentDocumentTypes().Contains(x.DocumentType)) ||
           (message.PrimaryDocuments.Any(d => d.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfSeller) &&
            businessUnitBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)) && processResult)
        processResult = this.ProcessFormalizedSign(message, queueItem, queueItems, isIncoming, box, historyOperation, historyComment) && processResult;
      
      // Если все регламентные документы в сообщении не поддерживаются - пропускаем и удаляем из очереди.
      if (message.ReglamentDocuments.All(x => !this.GetSupportedReglamentDocumentTypes().Contains(x.DocumentType)) &&
          !message.ReglamentDocuments.Any(x => this.GetSupportedServiceDocumentTypes().Contains(x.DocumentType)))
      {
        this.LogDebugFormat(message, queueItem, box, "Message processing was skipped. Message is marked as processed.");
        processResult = true;
      }
      
      return processResult;
    }

    #endregion

    #region Обработка ошибочных и непринимаемых сообщений

    /// <summary>
    /// Обработка ошибок подписания из диадока.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessDeliveryFailureNotification(IMessage message, IBoxBase box)
    {
      this.LogDebugFormat(message, box, "Execute ProcessDeliveryFailureNotification.");
      var parentMessageId = message.ReglamentDocuments.First(x => x.DocumentType == NpoComputer.DCX.Common.ReglamentDocumentType.DeliveryFailureNotification).ParentServiceEntityId;
      var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      var exchangeDocumentInfo = ExchangeDocumentInfos.GetAll().Where(x => Equals(x.RootBox, rootBox) &&
                                                                      x.ServiceMessageId == parentMessageId).FirstOrDefault();
      
      if (exchangeDocumentInfo != null && exchangeDocumentInfo.Document != null)
      {
        var tracking = exchangeDocumentInfo.Document.Tracking.Where(x => x.ExternalLinkId == exchangeDocumentInfo.Id).FirstOrDefault();
        
        var performer = tracking != null
          ? tracking.DeliveredTo
          : ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, exchangeDocumentInfo.Counterparty, new List<IExchangeDocumentInfo>() { exchangeDocumentInfo });
        
        this.SendCannotDeliveryDocumentTask(exchangeDocumentInfo, performer, box);

        exchangeDocumentInfo.Document.ExchangeState = null;
        exchangeDocumentInfo.ExchangeState = null;
        exchangeDocumentInfo.Document.ExternalApprovalState = null;
        
        if (tracking != null)
        {
          // HACK: нельзя удалять запись выдачи с действием "Согласование с контрагентом", но любую другую можно.
          tracking.Action = Docflow.OfficialDocumentTracking.Action.Delivery;
          exchangeDocumentInfo.Document.Tracking.Remove(tracking);
        }
        
        exchangeDocumentInfo.Document.Save();
        exchangeDocumentInfo.Save();
        ExchangeDocumentInfos.Delete(exchangeDocumentInfo);
      }

      return true;
    }

    /// <summary>
    /// Отправка задачи о том, что документ не был доставлен КА, т.к. подпись не прошла проверку.
    /// </summary>
    /// <param name="exchangeDocumentInfo">Информация о документе.</param>
    /// <param name="performer">Исполнитель задания.</param>
    /// <param name="box">Абонентский ящик, на который получено сообщение.</param>
    protected virtual void SendCannotDeliveryDocumentTask(IExchangeDocumentInfo exchangeDocumentInfo, IEmployee performer, IBoxBase box)
    {
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
      if (needReceive)
      {
        var task = Workflow.SimpleTasks.Create();
        task.NeedsReview = false;
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = performer;

        this.GrantAccessRightsForUpperBoxResponsibles(exchangeDocumentInfo.Document, box);
        task.Attachments.Add(exchangeDocumentInfo.Document);

        var hyperlink = Hyperlinks.Get(exchangeDocumentInfo.Document);

        task.ActiveText =
          Resources.CannotDeliveryDocumentToCounterpartyFormat(hyperlink, ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name);
        task.Subject = CutText(Resources.ErrorSendingDocumentToCounterpartyFormat(exchangeDocumentInfo.Document.Name),
                               task.Info.Properties.Subject.Length);
        task.Start();
      }
    }

    /// <summary>
    /// Обработка требования УОП.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessReceiptRequire(IMessage message, IMessageQueueItem queueItem, ICounterparty sender, IBoxBase box)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessReceiptRequire.");
      if (queueItem.NoticeTask == null || queueItem.NoticeTask.Status != Workflow.Task.Status.InProcess)
      {
        var task = Workflow.SimpleTasks.Create();
        var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
        var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(message.TimeStamp);
        task.Subject = Resources.ConfirmReceiptSubjectFormat(sender, ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(box), dateWithUTC,
                                                             exchangeService);
        task.Subject = CutText(task.Subject, task.Info.Properties.Subject.Length);
        task.ThreadSubject = Resources.ConfirmReceiptThreadSubject;
        var route = task.RouteSteps.AddNew();
        route.AssignmentType = Workflow.SimpleTaskRouteSteps.AssignmentType.Assignment;
        
        route.Performer = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, sender, null);
        route.Deadline = Calendar.Now.AddWorkingHours(route.Performer, 4);
        task.ActiveText = Resources.MessageHasDocumentWithReceiptRequire;
        
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Environment.NewLine;
        
        var counterPartyLink = Hyperlinks.Get(sender);
        
        task.ActiveText += Resources.ReceiptNeededActiveTextFormat(counterPartyLink, message.TimeStamp.ToShortDateString(), message.TimeStamp.ToShortTimeString());
        
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Environment.NewLine;
        
        task.ActiveText += Resources.LinkToPersonalDataFormat(exchangeService.Name, exchangeService.LogonUrl);

        task.NeedsReview = false;
        task.Start();
        
        queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Error;
        queueItem.Note = Resources.MessageHasDocumentWithReceiptRequireFormat(exchangeService.Name);
        queueItem.NoticeTask = task;
        queueItem.Save();
        return false;
      }
      
      return true;
    }

    /// <summary>
    /// Обработать входящее сообщение, в котором содержатся только неподдерживаемые документы.
    /// </summary>
    /// <param name="message">Сообщение сервиса обмена.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessMessageWithUnsupportedDocuments(IMessage message, ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      this.LogDebugFormat(message, box, "Execute ProcessMessageWithUnsupportedDocuments.");
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
      if (needReceive && isIncoming)
      {
        var simpleTask = Sungero.Workflow.SimpleTasks.Create();
        var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(message.TimeStamp);
        simpleTask.Subject = Resources.NoticeSubjectFormat(sender, ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(box), dateWithUTC,
                                                           ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box));
        simpleTask.Subject = CutText(simpleTask.Subject, simpleTask.Info.Properties.Subject.Length);
        simpleTask.ThreadSubject = Sungero.Exchange.Resources.NoticeThreadSubject;
        simpleTask.ActiveText = this.GenerateActiveTextFromUnsupportedDocuments(message.PrimaryDocuments, sender, isIncoming, box, message.TimeStamp, true);
        
        var step = simpleTask.RouteSteps.AddNew();
        step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, sender, null);
        
        simpleTask.Save();
        simpleTask.Start();
      }

      return true;
    }

    /// <summary>
    /// Сгенерировать текст по полученным формализованным документам, для заполнения задачи/задания.
    /// </summary>
    /// <param name="documents">Список формализованных документов.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Входящее сообщение.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="messageDate">Время сообщения.</param>
    /// <param name="allUnsupported">Признак, что все документы не поддерживаемые.</param>
    /// <returns>Сгенерированный текст.</returns>
    protected virtual string GenerateActiveTextFromUnsupportedDocuments(IEnumerable<IDocument> documents, ICounterparty sender, bool isIncoming,
                                                                        IBoxBase box, DateTime messageDate, bool allUnsupported)
    {
      var documentList = new System.Text.StringBuilder();
      var documentNames = string.Empty;
      var otherDocuments = false;
      foreach (var document in documents)
      {
        var isXml = System.IO.Path.GetExtension(document.FileName).TrimStart('.').ToLower() == "xml";
        var generatedName = isXml ? GenerateUnsupportedDocumentName(document) : string.Empty;
        // Если имя не сформировалось, значит, пришел необработанный нами вид документа.
        if (!string.IsNullOrEmpty(generatedName))
          documentList.AppendLine(Resources.FormalizedDocumentNameFormat(generatedName));
        else
          otherDocuments = true;
      }
      
      if (!string.IsNullOrEmpty(documentList.ToString()))
      {
        // Добавить информацию о том, что пришли и другие документы.
        if (otherDocuments)
          documentList.AppendLine(Resources.OtherDocuments);
        
        documentNames = Resources.DocumentsListFormat(documentList);
      }
      
      if (allUnsupported)
        documentNames += this.ProcessBoundedDocuments(documents, null, isIncoming, box);

      documentNames += string.Format("{0}{0}", Environment.NewLine);
      
      var counterPartyLink = Hyperlinks.Get(sender);
      if (isIncoming)
        documentNames += Resources.ReceiptNeededActiveTextFormat(counterPartyLink, messageDate.ToShortDateString(), messageDate.ToShortTimeString());
      else
        documentNames += Resources.ReceiptNeededOutgoingActiveTextFormat(counterPartyLink, messageDate.ToShortDateString(), messageDate.ToShortTimeString());
      documentNames += Environment.NewLine;
      
      var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
      return Resources.NoticeActiveTextFormat(documentNames, Resources.LinkToPersonalDataFormat(exchangeService.Name, exchangeService.LogonUrl));
    }

    /// <summary>
    /// Получить наименование формализованного документа, полученного из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <returns>Наименование, если получилось сформировать, иначе - пустая строка.</returns>
    private static string GenerateUnsupportedDocumentName(IDocument document)
    {
      System.Xml.Linq.XDocument xdoc;
      try
      {
        xdoc = System.Xml.Linq.XDocument.Load(new System.IO.MemoryStream(document.Content));
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Exchange. Failed to load XML: {0}", e.Message);
        return string.Empty;
      }
      
      RemoveNamespaces(xdoc);
      var documentType = string.Empty;
      var documentNumber = string.Empty;
      var documentDate = string.Empty;
      
      var fileElement = xdoc.Element("Файл");
      if (fileElement == null)
        return string.Empty;
      
      var docElement = fileElement.Element("Документ");
      if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.Act)
      {
        var actInfo = docElement.Element("СвАктИ");
        documentType = GetAttributeValueByName(actInfo, "НаимПервДок");
        documentNumber = GetAttributeValueByName(actInfo, "НомАкт");
        documentDate = GetAttributeValueByName(actInfo, "ДатаАкт");
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.Waybill)
      {
        var waybillInfo = docElement.Element("СвТНО");
        documentType = GetAttributeValueByName(waybillInfo, "НаимПервДок");
        
        var waybill = waybillInfo.Element("ТН");
        documentNumber = GetAttributeValueByName(waybill, "НомТН");
        documentDate = GetAttributeValueByName(waybill, "ДатаТН");
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.Invoice ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferDopSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopSeller)
      {
        var invoiceInfo = docElement.Element("СвСчФакт");
        documentNumber = GetAttributeValueByName(invoiceInfo, "НомерСчФ");
        documentDate = GetAttributeValueByName(invoiceInfo, "ДатаСчФ");
        documentType = GetAttributeValueByName(docElement, "НаимДокОпр");
        if (string.IsNullOrEmpty(documentType))
          documentType = "Счет-фактура";
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.InvoiceCorrection ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferDopCorrectionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfCorrectionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopCorrectionSeller)
      {
        var invoiceCorrectionInfo = docElement.Element("СвКСчФ");
        documentNumber = GetAttributeValueByName(invoiceCorrectionInfo, "НомерКСчФ");
        documentDate = GetAttributeValueByName(invoiceCorrectionInfo, "ДатаКСчФ");
        documentType = GetAttributeValueByName(docElement, "НаимДокОпр");
        if (string.IsNullOrEmpty(documentType))
          documentType = "Корректировочный счет-фактура";
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.InvoiceCorrectionRevision ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferDopCorrectionRevisionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller)
      {
        var invoiceCorrectionRevisionInfo = docElement.Element("СвКСчФ").Element("ИспрКСчФ");
        documentNumber = GetAttributeValueByName(invoiceCorrectionRevisionInfo, "НомИспрКСчФ");
        documentDate = GetAttributeValueByName(invoiceCorrectionRevisionInfo, "ДатаИспрКСчФ");
        documentType = GetAttributeValueByName(docElement, "НаимДокОпр");
        if (string.IsNullOrEmpty(documentType))
          documentType = "Исправление корректировочного счета-фактуры";
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.InvoiceRevision ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferDopRevisionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfRevisionSeller ||
               document.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopRevisionSeller)
      {
        var invoiceRevisionInfo = docElement.Element("СвСчФакт").Element("ИспрСчФ");
        documentNumber = GetAttributeValueByName(invoiceRevisionInfo, "НомИспрСчФ");
        documentDate = GetAttributeValueByName(invoiceRevisionInfo, "ДатаИспрСчФ");
        documentType = GetAttributeValueByName(docElement, "НаимДокОпр");
        if (string.IsNullOrEmpty(documentType))
          documentType = "Исправление счета-фактуры";
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.GoodsTransferSeller)
      {
        var goodsTransferSellerInfo = docElement.Element("СвДокПТПрКроме").Element("СвДокПТПр");
        documentNumber = GetAttributeValueByName(goodsTransferSellerInfo.Element("ИдентДок"), "НомДокПТ");
        documentDate = GetAttributeValueByName(goodsTransferSellerInfo.Element("ИдентДок"), "ДатаДокПТ");
        documentType = GetAttributeValueByName(goodsTransferSellerInfo.Element("НаимДок"), "НаимДокОпр");
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.GoodsTransferRevisionSeller)
      {
        var goodsTransferRevisionSellerInfo = docElement.Element("СвДокПТПрКроме").Element("СвДокПТПр");
        documentNumber = GetAttributeValueByName(goodsTransferRevisionSellerInfo.Element("ИспрДокПТ"), "НомДокПТ");
        documentDate = GetAttributeValueByName(goodsTransferRevisionSellerInfo.Element("ИспрДокПТ"), "ДатаДокПТ");
        documentType = GetAttributeValueByName(goodsTransferRevisionSellerInfo.Element("НаимДок"), "НаимДокОпр");
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.WorksTransferSeller)
      {
        var worksTransferSellerInfo = docElement.Element("СвДокПРУ");
        documentNumber = GetAttributeValueByName(worksTransferSellerInfo.Element("ИдентДок"), "НомДокПРУ");
        documentDate = GetAttributeValueByName(worksTransferSellerInfo.Element("ИдентДок"), "ДатаДокПРУ");
        documentType = GetAttributeValueByName(worksTransferSellerInfo.Element("НаимДок"), "НаимДокОпр");
      }
      else if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.WorksTransferRevisionSeller)
      {
        var generalTransferRevisionSellerInfo = docElement.Element("СвДокПРУ");
        documentNumber = GetAttributeValueByName(generalTransferRevisionSellerInfo.Element("ИспрДокПРУ"), "НомИспрДокПРУ");
        documentDate = GetAttributeValueByName(generalTransferRevisionSellerInfo.Element("ИспрДокПРУ"), "ДатаИспрДокПРУ");
        documentType = GetAttributeValueByName(generalTransferRevisionSellerInfo.Element("НаимДок"), "НаимДокОпр");
      }
      else
      {
        return string.Empty;
      }
      
      return Resources.FormalizedDocumentTemplateNameFormat(documentType, documentNumber, documentDate);
    }

    #endregion

    #region Обработка сообщения с новыми документами
    
    #region Процессинг сообщения с новыми документами

    /// <summary>
    /// Обработать новое входящее сообщение.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessNewIncomingMessage(IMessage message, IMessageQueueItem queueItem, ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessNewIncomingMessage.");
      var infos = new List<IExchangeDocumentInfo>();
      var processedDocumentTypes = this.GetSupportedPrimaryDocumentTypes();

      // Обработка документов в сообщении.
      var processingDocuments = message.PrimaryDocuments.Where(x => processedDocumentTypes.Contains(x.DocumentType.Value)).ToList();
      foreach (var processingDocument in processingDocuments)
      {
        if (this.NeedCreateDocumentFromNewIncomingMessage(message, processingDocument, sender, isIncoming, box))
        {
          var serviceCounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
          var exchangeDocument = this.GetOrCreateNewExchangeDocument(processingDocument, sender, serviceCounterpartyId, isIncoming, message.TimeStamp, box);
          this.SignDocumentFromNewIncomingMessage(message, exchangeDocument, processingDocument, box);
          var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, processingDocument.ServiceEntityId);
          if (exchangeDocument.LastVersion.PublicBody.Size == 0)
            Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(exchangeDocument, info.VersionId.Value, exchangeDocument.ExchangeState);
          infos.Add(info);
        }
      }
      
      return this.ProcessDocumentsFromNewIncomingMessage(message, queueItem, infos, processingDocuments, sender, isIncoming, box);
    }

    /// <summary>
    /// Обработать документы, созданные из сообщения.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="infos">Информация по обработанным документам.</param>
    /// <param name="processingDocuments">Обрабатываемые документы.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessDocumentsFromNewIncomingMessage(IMessage message, IMessageQueueItem queueItem, List<IExchangeDocumentInfo> infos,
                                                                  List<IDocument> processingDocuments, ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessDocumentsFromNewIncomingMessage.");
      // Если не создано ни одного документа - завершаем обработку.
      var documents = infos.Where(i => i.Document != null).Select(i => i.Document).ToList();
      if (!documents.Any())
      {
        queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.Processed;
        queueItem.Save();
        return true;
      }
      
      this.FillCounterpartyDataFromNewMessage(message, infos, documents, sender, isIncoming);
      
      foreach (var doc in documents)
        this.GrantAccessRightsForUpperBoxResponsibles(doc, box);

      var exchangeTaskActiveTextBoundedDocuments = this.ProcessBoundedDocuments(processingDocuments, documents, isIncoming, box);

      var needReceive = this.NeedReceiveDocumentProcessingTask(box, message);
      if (needReceive)
        return this.StartExchangeTask(message, infos, sender, isIncoming, box, exchangeTaskActiveTextBoundedDocuments);

      return true;
    }

    /// <summary>
    /// Проверка, что нужно заносить документы в систему.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="document">Обрабатываемый документ.</param>
    /// <param name="sender">Контрагент по документу.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>True - если нужно создавать документ, иначе - false.</returns>
    /// <remarks>Создает информацию по документу, если тот будет загружен позже.</remarks>
    protected virtual bool NeedCreateDocumentFromNewIncomingMessage(IMessage message, IDocument document, ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      if (document.RevocationStatus == NpoComputer.DCX.Common.RevocationStatus.RevocationAccepted)
        return false;
      
      // Если документ может быть подписан, то создаем инфо, чтобы потом найти сообщение и подпись.
      if (!isIncoming && document.DocumentType == DocumentType.Nonformalized &&
          (document.SignStatus == NpoComputer.DCX.Common.SignStatus.Waiting ||
           document.SignStatus == NpoComputer.DCX.Common.SignStatus.None))
      {
        var withoutDoc = GetOrCreateExchangeInfoWithoutDocument(document, sender, message.Receiver.Organization.OrganizationId, isIncoming, message.TimeStamp, box);
        withoutDoc.Save();
        return false;
      }

      // Не грузим отправленные и не подписанные сообщения, кроме формализованных документов.
      if (!isIncoming && document.SignStatus != NpoComputer.DCX.Common.SignStatus.Signed &&
          document.DocumentType == DocumentType.Nonformalized)
        return false;
      
      // Не грузим сообщения с отказом.
      if (document.SignStatus == NpoComputer.DCX.Common.SignStatus.Rejected)
        return false;

      // Если документ нами и отправлен - задачу отправлять уже не надо.
      var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
      if (!isIncoming && info != null)
        return false;
      
      return true;
    }

    /// <summary>
    /// Подписать документ из сервиса обмена.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="signedDocument">Подписываемый документ.</param>
    /// <param name="serviceDocument">Документ в сервисе обмена.</param>
    /// <param name="box">Абонентский ящик.</param>
    protected virtual void SignDocumentFromNewIncomingMessage(IMessage message, IOfficialDocument signedDocument, IDocument serviceDocument, IBoxBase box)
    {
      var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, serviceDocument.ServiceEntityId);
      this.LogDebugFormat(info, "Execute SignDocumentFromNewIncomingMessage.");
      var signatures = message.Signatures.Where(x => x.DocumentId == serviceDocument.ServiceEntityId);
      var addedThumbprints = Signatures.Get(signedDocument.LastVersion)
        .Where(s => s.SignCertificate != null)
        .Select(x => x.SignCertificate.Thumbprint);
      var accountDocument = Docflow.AccountingDocumentBases.As(signedDocument);
      foreach (var signature in signatures)
      {
        var certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature.Content);
        var signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
        
        var signatureIsAlreadyAdded = addedThumbprints.Any(x => x.Equals(certificateInfo.Thumbprint));
        if (!signatureIsAlreadyAdded)
        {
          this.SignDocument(info, signature, signedDocument.LastVersion, signatoryInfo, message.TimeStamp);

          if (accountDocument != null)
          {
            var lastSignature = this.GetLastDocumentSignature(accountDocument);
            accountDocument.SellerSignatureId = lastSignature.Id;
          }
        }
      }
    }
    
    /// <summary>
    /// Обработка связанных документов.
    /// </summary>
    /// <param name="documents">Документы сервиса обмена.</param>
    /// <param name="officialDocuments">Документы в RX.</param>
    /// <param name="fromCounterparty">True, если документы от контрагента.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Текст с информацией о связанных документах.</returns>
    protected virtual string ProcessBoundedDocuments(IEnumerable<IDocument> documents, IList<IOfficialDocument> officialDocuments,
                                                     bool fromCounterparty, IBoxBase box)
    {
      this.LogDebugFormat(box, "Execute ProcessBoundedDocuments.");
      officialDocuments = officialDocuments ?? new List<IOfficialDocument>();

      var bounds = new List<string>();
      foreach (var document in documents.Where(x => x.BoundDocuments != null))
        bounds.AddRange(document.BoundDocuments.Select(x => x.DocumentId));

      bounds = bounds.Distinct().ToList();

      var links = new List<string>();
      var hasBound = false;
      foreach (var bound in bounds)
      {
        var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, bound);
        if (info != null && info.Document != null)
        {
          var relatedDocs = info.Document.Relations.GetRelatedFrom(Constants.Module.SimpleRelationRelationName);
          
          if (officialDocuments.Any())
          {
            foreach (var offdoc in officialDocuments.Where(x => !Equals(x, info.Document)).Where(x => relatedDocs.All(d => !Equals(d, x))))
            {
              if (Docflow.AccountingDocumentBases.Is(offdoc) && Docflow.AccountingDocumentBases.As(offdoc).IsAdjustment == true)
                continue;
              
              this.AddRelations(offdoc, info);
            }
            info.Document.Save();
          }
          hasBound = true;
          var link = Hyperlinks.Get(info.Document);
          if (!officialDocuments.Contains(info.Document))
            links.Add(link);
        }
      }
      
      if (hasBound)
      {
        var text = fromCounterparty ?
          Resources.NoticeCounterpartyBoundDocument :
          Resources.NoticeOurBoundDocument;
        if (links.Any())
        {
          var separator = string.Format(", {0}", Environment.NewLine);
          var allLinks = Environment.NewLine + string.Join(separator, links);
          text = fromCounterparty ?
            Resources.NoticeCounterpartyBoundDocumentWithLinksFormat(allLinks) :
            Resources.NoticeOurBoundDocumentWithLinksFormat(allLinks);
        }
        return string.Format("{0}{0}{1}", Environment.NewLine, text);
      }

      return string.Empty;
    }
    
    /// <summary>
    /// Выдать права на документ ответственным за вышестоящие абонентские ящики.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="box">Абонентский ящик.</param>
    protected virtual void GrantAccessRightsForUpperBoxResponsibles(IOfficialDocument document, IBoxBase box)
    {
      var boxes = new List<IBoxBase>() { box };
      if (!ExchangeCore.BusinessUnitBoxes.Is(box))
      {
        var departmentBox = ExchangeCore.DepartmentBoxes.As(box);
        boxes.Add(departmentBox.RootBox);
        
        ExchangeCore.IBoxBase parentBox = departmentBox.ParentBox;
        while (!ExchangeCore.BusinessUnitBoxes.Is(parentBox))
        {
          departmentBox = ExchangeCore.DepartmentBoxes.As(parentBox);
          boxes.Add(departmentBox);
          parentBox = departmentBox.ParentBox;
        }
      }
      
      var responsibles = new List<Sungero.Company.IEmployee>();
      foreach (var currentBox in boxes)
      {
        if (currentBox.Responsible != null)
          responsibles.Add(currentBox.Responsible);
        
        var info = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(document);
        var computedResponsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(currentBox, info.Counterparty, new List<Exchange.IExchangeDocumentInfo>() { info });
        if (computedResponsible != null && !responsibles.Contains(computedResponsible))
          responsibles.Add(computedResponsible);
        
        var allCompanies = Docflow.PublicFunctions.OfficialDocument.GetCounterparties(document);
        if (allCompanies != null)
        {
          var companies = allCompanies.Where(c => Parties.CompanyBases.Is(c));
          responsibles.AddRange(companies.Where(c => Parties.CompanyBases.As(c).Responsible != null).Select(c => Parties.CompanyBases.As(c).Responsible));
        }
      }
      
      foreach (var responsible in responsibles)
      {
        if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, responsible))
          document.AccessRights.Grant(responsible, DefaultAccessRightsTypes.FullAccess);
      }

      document.AccessRights.Save();
    }
    
    /// <summary>
    /// Заполнить подписывающего и основание со стороны контрагента.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="infos">Информация по обработанным документам.</param>
    /// <param name="documents">Документы сервиса обмена.</param>
    /// <param name="counterparty">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <remarks>Если сообщение исходящее, то подписывающий и основание со стороны контрагента не заполняются.</remarks>
    protected virtual void FillCounterpartyDataFromNewMessage(IMessage message, List<IExchangeDocumentInfo> infos,
                                                              List<IOfficialDocument> documents, ICounterparty counterparty, bool isIncoming)
    {
      this.LogDebugFormat(message, "Execute FillCounterpartyDataFromNewMessage.");
      
      // Если сообщение исходящее, то заполнять подписывающего и основание со стороны контрагента не надо.
      if (!isIncoming)
        return;
      
      foreach (var doc in documents)
      {
        var info = infos.FirstOrDefault(x => x.Document != null && Equals(doc, x.Document));
        this.FillCounterpartySignatoryAndSigningReason(message, info.ServiceDocumentId, doc, counterparty);
      }
    }
    
    /// <summary>
    /// Заполнить подписывающего и основание со стороны контрагента для ответа по документу.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="serviceDocumentId">ИД документа в сервисе обмена.</param>
    /// <param name="document">Документ сервиса обмена.</param>
    /// <param name="counterparty">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <remarks>Если сообщение исходящее и не является ответом, то подписывающий и основание со стороны контрагента не заполняются.</remarks>
    protected virtual void FillCounterpartyDataFromReplyMessage(IMessage message, string serviceDocumentId,
                                                                IOfficialDocument document, ICounterparty counterparty, bool isIncoming)
    {
      this.LogDebugFormat(message, "Execute FillCounterpartyDataFromReplyMessage.");
      
      // Если сообщение исходящее, то заполнять подписывающего и основание со стороны контрагента не надо.
      if (!isIncoming)
        return;
      
      this.FillCounterpartySignatoryAndSigningReason(message, serviceDocumentId, document, counterparty);
    }
    
    /// <summary>
    /// Заполнить подписывающего и основание со стороны контрагента в отдельном документе.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="serviceDocumentId">ИД документа в сервисе обмена.</param>
    /// <param name="doc">Документ сервиса обмена.</param>
    /// <param name="counterparty">Отправитель.</param>
    /// <remarks>Поля должны заполняться только при работе с входящими документами или с ответами на исходящие.</remarks>
    protected virtual void FillCounterpartySignatoryAndSigningReason(IMessage message, string serviceDocumentId,
                                                                     IOfficialDocument doc, ICounterparty counterparty)
    {
      this.LogDebugFormat(message, "Execute FillCounterpartySignatoryAndSigningReason.");
      
      var signature = message.Signatures.Where(x => x.DocumentId == serviceDocumentId).FirstOrDefault();
      if (signature != null)
      {
        var certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature.Content);
        var signatoryName = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
        var signatory = Parties.PublicFunctions.Contact.GetContactByName(signatoryName, counterparty);
        
        if (signatory != null)
          Sungero.Docflow.PublicFunctions.OfficialDocument.FillCounterpartySignatory(doc, signatory);
        
        // Заполнить основание со стороны контрагента по формату: "<Основание подписания> (<Подписант>)".
        var signingReason = string.Empty;
        var unifiedRegNumber = signature.FormalizedPoAUnifiedRegNumber;
        // Если подписали по МЧД, то взять её номер.
        if (!string.IsNullOrWhiteSpace(unifiedRegNumber))
          signingReason = string.Format(Exchange.Resources.CounterpartyPowerOfAttorney, unifiedRegNumber, signatoryName);
        
        // Если МЧД нет и документ формализованный, то получить информацию об основании из xml.
        if (string.IsNullOrWhiteSpace(signingReason))
        {
          if (message.IsReply)
            signingReason = this.GetSigningReasonFromReglamentDocumentXml(message, serviceDocumentId, doc, signatoryName);
          else
            signingReason = this.GetSigningReasonFromPrimaryDocumentXml(message, serviceDocumentId, doc, signatoryName);
        }
        
        // Если не удалось получить основание, то заполняем - "Должностные обязанности".
        if (string.IsNullOrWhiteSpace(signingReason))
          signingReason = SignatureSettings.Resources.DutiesDisplayNameFormat(signatoryName);
        
        Sungero.Docflow.PublicFunctions.OfficialDocument.FillCounterpartySigningReason(doc, signingReason);
        doc.Save();
      }
    }
    
    /// <summary>
    /// Получить основание подписания из XML основного документа.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="serviceDocumentId">ИД документа в сервисе обмена.</param>
    /// <param name="doc">Документ сервиса обмена.</param>
    /// <param name="signatoryName">Имя подписывающего.</param>
    /// <returns>Основание контрагента, если не получилось найти, то пустая строка.</returns>
    protected virtual string GetSigningReasonFromPrimaryDocumentXml(IMessage message, string serviceDocumentId, IOfficialDocument doc, string signatoryName)
    {
      var processingDocument = message.PrimaryDocuments.Where(x => x.ServiceEntityId == serviceDocumentId).FirstOrDefault();
      
      if (processingDocument == null || !AccountingDocumentBases.Is(doc) || AccountingDocumentBases.As(doc).IsFormalized != true)
        return string.Empty;
      
      var xdoc = System.Xml.Linq.XDocument.Load(new System.IO.MemoryStream(processingDocument.Content));
      var documentInfo = xdoc.Element("Файл").Element("Документ");
      var signingReason = this.GetSigningReasonFromXml(documentInfo);
      if (!string.IsNullOrWhiteSpace(signingReason))
        return Sungero.Exchange.Resources.SigningReasonDisplayValueFormat(signingReason, signatoryName);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить основание подписания из XML регламентного документа.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="serviceDocumentId">ИД документа в сервисе обмена.</param>
    /// <param name="doc">Документ сервиса обмена.</param>
    /// <param name="signatoryName">Имя подписывающего.</param>
    /// <returns>Основание контрагента, если не получилось найти, то пустая строка.</returns>
    protected virtual string GetSigningReasonFromReglamentDocumentXml(IMessage message, string serviceDocumentId, IOfficialDocument doc, string signatoryName)
    {
      // Рассматриваем регламентные документы, т.к. работаем с титулами, которые есть только у формализованных документов.
      var processingDocument = message.ReglamentDocuments.Where(x => x.ServiceEntityId == serviceDocumentId).FirstOrDefault();
      
      if (processingDocument == null || !AccountingDocumentBases.Is(doc) || AccountingDocumentBases.As(doc).IsFormalized != true)
        return string.Empty;
      
      var xdoc = System.Xml.Linq.XDocument.Load(new System.IO.MemoryStream(processingDocument.Content));
      var documentInfo = xdoc.Element("Файл").Element("ИнфПок");
      // Для актов и накладных в старом формате (ДПТ, ДПРР) информация о документе находится в другом элементе.
      if (documentInfo == null)
        documentInfo = xdoc.Element("Файл").Element("Документ");
      var signingReason = this.GetSigningReasonFromXml(documentInfo);
      if (!string.IsNullOrWhiteSpace(signingReason))
        return Sungero.Exchange.Resources.SigningReasonDisplayValueFormat(signingReason, signatoryName);
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить основание подписания из XML-документа.
    /// </summary>
    /// <param name="documentInfo">Элемент с информацией о документе.</param>
    /// <returns>Основание. Если не смогли получить, то пустая строка.</returns>
    protected virtual string GetSigningReasonFromXml(XElement documentInfo)
    {
      if (documentInfo == null)
        return string.Empty;
      
      // Попытаться получить основание подписания из атрибута с основанием полномочий.
      var signatoryInfo = documentInfo.Element("Подписант");
      if (signatoryInfo != null)
      {
        var signingReason = GetAttributeValueByName(signatoryInfo, "ОснПолн");
        if (!string.IsNullOrWhiteSpace(signingReason))
          return signingReason;
        
        signingReason = GetAttributeValueByName(signatoryInfo, "ОснПолнПодп");
        if (!string.IsNullOrWhiteSpace(signingReason))
          return signingReason;
      }
      
      return string.Empty;
    }
    
    private static IExchangeDocumentInfo GetOrCreateExchangeInfoWithoutDocument(IDocument document, ICounterparty sender, string serviceCounterpartyId,
                                                                                bool isIncoming, DateTime messageDate, IBoxBase box)
    {
      var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
      if (info != null)
        return info;
      
      var newInfo = ExchangeDocumentInfos.Create();
      newInfo.Box = box;
      newInfo.RootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      newInfo.ServiceDocumentId = document.ServiceEntityId;
      newInfo.MessageType = isIncoming ? Exchange.ExchangeDocumentInfo.MessageType.Incoming : Exchange.ExchangeDocumentInfo.MessageType.Outgoing;
      newInfo.ServiceMessageId = document.ServiceMessageId;
      newInfo.Counterparty = sender;
      newInfo.ServiceCounterpartyId = serviceCounterpartyId;
      newInfo.MessageDate = ToTenantTime(messageDate);
      newInfo.NeedSign = document.NeedSign;
      return newInfo;
    }
    
    /// <summary>
    /// Связать документы типом связи "Прочие".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="relatedExchangeDocumentInfo">Информация о связываемом документе обмена.</param>
    public virtual void AddRelations(IOfficialDocument document, IExchangeDocumentInfo relatedExchangeDocumentInfo)
    {
      document.Relations.AddFromOrUpdate(Constants.Module.SimpleRelationRelationName, null, relatedExchangeDocumentInfo.Document);
      document.Save();
    }
    
    #endregion

    #region Создание и заполнение документа

    /// <summary>
    /// Получить или создать документ из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ из сообщения.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="serviceCounterpartyId">Id контрагента в сервисе обмена.</param>
    /// <param name="isIncoming">Признак документа от контрагента.</param>
    /// <param name="messageDate">Дата сообщения.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Документ RX.</returns>
    protected virtual IOfficialDocument GetOrCreateNewExchangeDocument(IDocument document, ICounterparty sender, string serviceCounterpartyId,
                                                                       bool isIncoming, DateTime messageDate, IBoxBase box)
    {
      var exchangeDoc = Docflow.OfficialDocuments.Null;
      
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
      
      if (exchangeDocumentInfo != null)
        exchangeDoc = exchangeDocumentInfo.Document;
      
      if (exchangeDoc == null)
      {
        var documentFullName = CommonLibrary.FileUtils.NormalizeFileName(document.FileName);
        var documentName = System.IO.Path.GetFileNameWithoutExtension(documentFullName).TrimEnd('.');
        documentName = Functions.Module.GetValidFileName(documentName);
        var newInfo = GetOrCreateExchangeInfoWithoutDocument(document, sender, serviceCounterpartyId, isIncoming, messageDate, box);
        var convertedDocument = document as NpoComputer.DCX.Common.Document;
        var documentComment = string.IsNullOrEmpty(document.Comment) ? string.Empty : Resources.DocumentCommentFormat(document.Comment).ToString();

        if (string.IsNullOrEmpty(documentName))
          documentName = documentFullName;
        var taxDocumentClassifierCode = string.Empty;
        var functionUTD = string.Empty;

        // Неформализованный документ.
        if (document.DocumentType == DocumentType.Nonformalized)
          exchangeDoc = this.CreateExchangeDocument(newInfo, sender, box, documentName, documentComment);
        else
        {
          var taxDocumentClassifier = GetTaxDocumentClassifier(document);
          taxDocumentClassifierCode = taxDocumentClassifier.TaxDocumentClassifierCode;
          functionUTD = taxDocumentClassifier.TaxDocumentClassifierFunction;
        }

        // Товарная накладная.
        if (document.DocumentType == DocumentType.Waybill &&
            taxDocumentClassifierCode != Constants.Module.TaxDocumentClassifier.GoodsTransferSeller &&
            taxDocumentClassifierCode != Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller)
        {
          var waybill = FinancialFunction.Module.CreateWaybillDocument(documentComment, box, sender, newInfo);
          
          var documentInfo = this.GetInfoFromXML(document, sender);
          
          SetDocumentTotalAmount(waybill, documentInfo);
          
          exchangeDoc = waybill;
        }

        // Cчет-фактура.
        if (document.DocumentType == DocumentType.Invoice ||
            document.DocumentType == DocumentType.InvoiceCorrection ||
            document.DocumentType == DocumentType.InvoiceCorrectionRevision ||
            document.DocumentType == DocumentType.InvoiceRevision ||
            document.DocumentType == DocumentType.GeneralTransferSchfSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller)
        {
          exchangeDoc = this.CreateTaxInvoice(convertedDocument, newInfo, sender, isIncoming, box);
        }

        // Акт.
        if (document.DocumentType == DocumentType.Act &&
            taxDocumentClassifierCode != Constants.Module.TaxDocumentClassifier.WorksTransferSeller &&
            taxDocumentClassifierCode != Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller)
        {
          var statement = FinancialFunction.Module.CreateContractStatementDocument(documentComment, box, sender, newInfo);
          
          var documentInfo = this.GetInfoFromXML(document, sender);
          
          SetDocumentTotalAmount(statement, documentInfo);
          
          exchangeDoc = statement;
        }

        // Универсальный передаточный документ.
        var universalDocumentTaxInvoiceAndBasicTypes = new List<NpoComputer.DCX.Common.DocumentType>()
        {
          DocumentType.GeneralTransferSchfDopSeller,
          DocumentType.GeneralTransferSchfDopRevisionSeller,
          DocumentType.GeneralTransferSchfDopCorrectionSeller,
          DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller
        };
        var universalDocumentBasicTypes = new List<NpoComputer.DCX.Common.DocumentType>()
        {
          DocumentType.GeneralTransferDopSeller,
          DocumentType.GeneralTransferDopRevisionSeller,
          DocumentType.GeneralTransferDopCorrectionSeller,
          DocumentType.GeneralTransferDopCorrectionRevisionSeller
        };
        
        var isUTD155ByXmlContent = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller155 &&
          functionUTD == Constants.Module.FunctionUTDDop;
        
        var isUTDByXmlContent = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller &&
          functionUTD == Constants.Module.FunctionUTDDop;
        
        var isUTDCorrectionByXmlContent = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalCorrectionDocumentSeller &&
          functionUTD == Constants.Module.FunctionUTDDopCorrection;
        
        if (universalDocumentTaxInvoiceAndBasicTypes.Contains(document.DocumentType.Value) ||
            universalDocumentBasicTypes.Contains(document.DocumentType.Value) ||
            isUTD155ByXmlContent || isUTDByXmlContent || isUTDCorrectionByXmlContent)
        {
          exchangeDoc = this.CreateUniversalTransferDocument(convertedDocument, newInfo, sender, box, universalDocumentTaxInvoiceAndBasicTypes);
        }
        
        // ДПРР.
        if (document.DocumentType == DocumentType.WorksTransferSeller ||
            document.DocumentType == DocumentType.WorksTransferRevisionSeller ||
            taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.WorksTransferSeller)
        {
          exchangeDoc = this.CreateContractStatementDocument(convertedDocument, newInfo, sender, box);
        }
        
        // ДПТ.
        if (document.DocumentType == DocumentType.GoodsTransferSeller ||
            document.DocumentType == DocumentType.GoodsTransferRevisionSeller ||
            taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.GoodsTransferSeller)
        {
          exchangeDoc = this.CreateWaybillDocument(convertedDocument, newInfo, sender, box);
        }

        if (isIncoming)
          exchangeDoc.ExternalApprovalState = Docflow.OfficialDocument.ExternalApprovalState.Signed;
        else
          exchangeDoc.InternalApprovalState = Docflow.OfficialDocument.InternalApprovalState.Signed;
        
        newInfo.Document = exchangeDoc;
        
        if (ExchangeCore.DepartmentBoxes.Is(box))
          exchangeDoc.Department = ExchangeCore.PublicFunctions.BoxBase.GetDepartment(box);
        
        // Сбрасываем статус эл. обмена, чтобы при создании версии не сбрасывался статус согласования с КА.
        exchangeDoc.ExchangeState = null;

        this.CreateExchangeDocumentVersion(convertedDocument, newInfo, exchangeDoc, sender, isIncoming, box, documentFullName);
        
        newInfo.Save();
      }
      
      return exchangeDoc;
    }

    /// <summary>
    /// Создать накладную.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Созданный документ.</returns>
    protected virtual IOfficialDocument CreateWaybillDocument(Document document, IExchangeDocumentInfo info, ICounterparty sender, IBoxBase box)
    {
      var documentInfo = this.GetInfoFromXML(document, sender);
      var waybill = FinancialFunction.Module.CreateWaybillDocument(documentInfo.Comment, box, sender, info);
      waybill.LeadingDocument = documentInfo.Contract;
      waybill.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer;

      if (documentInfo.IsRevision)
        waybill.IsRevision = true;

      SetDocumentTotalAmount(waybill, documentInfo);

      if (!string.IsNullOrEmpty(documentInfo.DocumentNumber))
        document.FormalizedNumber = documentInfo.DocumentNumber;

      DateTime parsedDate;
      if (!string.IsNullOrEmpty(documentInfo.DocumentDate) &&
          TryParseDate(documentInfo, out parsedDate))
        document.FormalizedDate = parsedDate;
      return waybill;
    }

    /// <summary>
    /// Создать акт.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Созданный документ.</returns>
    protected virtual IOfficialDocument CreateContractStatementDocument(Document document, IExchangeDocumentInfo info, ICounterparty sender,
                                                                        IBoxBase box)
    {
      var documentInfo = this.GetInfoFromXML(document, sender);
      var statement = FinancialFunction.Module.CreateContractStatementDocument(documentInfo.Comment, box, sender, info);
      statement.LeadingDocument = documentInfo.Contract;
      statement.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer;

      if (documentInfo.IsRevision)
        statement.IsRevision = true;

      SetDocumentTotalAmount(statement, documentInfo);

      if (!string.IsNullOrEmpty(documentInfo.DocumentNumber))
        document.FormalizedNumber = documentInfo.DocumentNumber;

      DateTime parsedDate;
      if (!string.IsNullOrEmpty(documentInfo.DocumentDate) &&
          TryParseDate(documentInfo, out parsedDate))
        document.FormalizedDate = parsedDate;

      if (!info.NeedSign.Value)
        statement.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
      return statement;
    }

    /// <summary>
    /// Создать УПД.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="universalDocumentTaxInvoiceAndBasicTypes">Тип документа обмена.</param>
    /// <returns>Созданный документ.</returns>
    protected virtual IOfficialDocument CreateUniversalTransferDocument(Document document, IExchangeDocumentInfo info, ICounterparty sender,
                                                                        IBoxBase box, List<DocumentType> universalDocumentTaxInvoiceAndBasicTypes)
    {
      var documentInfo = this.GetInfoFromXML(document, sender);
      var accounting = universalDocumentTaxInvoiceAndBasicTypes.Contains(document.DocumentType.Value)
        ? FinancialFunction.Module.CreateUniversalTaxInvoiceAndBasic(documentInfo.Comment, box, sender, documentInfo.IsAdjustment, documentInfo.Corrected,
                                                                     info)
        : FinancialFunction.Module.CreateUniversalBasic(documentInfo.Comment, box, sender, documentInfo.IsAdjustment, documentInfo.Corrected, info);

      if (documentInfo.IsRevision)
        accounting.IsRevision = true;

      if (documentInfo.Function != null)
        accounting.FormalizedFunction = documentInfo.Function;

      if (documentInfo.СorrectionRevisionParentDocument != null)
        accounting.Relations.Add(Constants.Module.SimpleRelationRelationName, documentInfo.СorrectionRevisionParentDocument);

      SetDocumentTotalAmount(accounting, documentInfo);

      if (!string.IsNullOrEmpty(documentInfo.DocumentNumber))
        document.FormalizedNumber = documentInfo.DocumentNumber;

      DateTime parsedDate;
      if (!string.IsNullOrEmpty(documentInfo.DocumentDate) &&
          TryParseDate(documentInfo, out parsedDate))
        document.FormalizedDate = parsedDate;

      if (!info.NeedSign.Value)
        accounting.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
      return accounting;
    }

    /// <summary>
    /// Создать счет-фактуру.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="isIncoming">True, если счет-фактура полученный, иначе - выставленный.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Созданный документ.</returns>
    protected virtual IOfficialDocument CreateTaxInvoice(Document document, IExchangeDocumentInfo info, ICounterparty sender, bool isIncoming,
                                                         IBoxBase box)
    {
      var documentInfo = this.GetInfoFromXML(document, sender);
      Docflow.IAccountingDocumentBase accounting = null;
      if (isIncoming)
        accounting = FinancialFunction.Module.CreateIncomingTaxInvoiceDocument(documentInfo.Comment, box, sender, documentInfo.IsAdjustment,
                                                                               documentInfo.Corrected, info);
      else
        accounting = FinancialFunction.Module.CreateOutgoingTaxInvoiceDocument(documentInfo.Comment, box, sender, documentInfo.IsAdjustment,
                                                                               documentInfo.Corrected, info);

      if (documentInfo.IsRevision)
        accounting.IsRevision = true;

      if (documentInfo.Function != null)
        accounting.FormalizedFunction = documentInfo.Function;

      if (documentInfo.СorrectionRevisionParentDocument != null)
        accounting.Relations.Add(Constants.Module.SimpleRelationRelationName, documentInfo.СorrectionRevisionParentDocument);

      if (document.DocumentType == DocumentType.GeneralTransferSchfSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller)
        accounting.FormalizedServiceType = Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;

      SetDocumentTotalAmount(accounting, documentInfo);

      if (!string.IsNullOrEmpty(documentInfo.DocumentNumber))
        document.FormalizedNumber = documentInfo.DocumentNumber;

      DateTime parsedDate;
      if (!string.IsNullOrEmpty(documentInfo.DocumentDate) &&
          TryParseDate(documentInfo, out parsedDate))
        document.FormalizedDate = parsedDate;
      return accounting;
    }

    /// <summary>
    /// Создать документ обмена.
    /// </summary>
    /// <param name="info">Информация о документе.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Ящик обмена.</param>
    /// <param name="fileName">Имя файла.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>Созданный документ.</returns>
    protected virtual IOfficialDocument CreateExchangeDocument(IExchangeDocumentInfo info, ICounterparty counterparty, IBoxBase box,
                                                               string fileName, string comment)
    {
      var exchangeDoc = Docflow.ExchangeDocuments.Create();
      
      if (fileName.Length > exchangeDoc.Info.Properties.Name.Length)
        fileName = fileName.Substring(0, exchangeDoc.Info.Properties.Name.Length);
      
      if (!string.IsNullOrEmpty(comment) && comment.Length > exchangeDoc.Info.Properties.Note.Length)
        comment = comment.Substring(0, exchangeDoc.Info.Properties.Note.Length);
      
      exchangeDoc.Name = fileName;
      exchangeDoc.Subject = fileName;
      exchangeDoc.Note = comment;
      exchangeDoc.BusinessUnit = ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(box);
      exchangeDoc.BusinessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      exchangeDoc.Counterparty = counterparty;
      
      return exchangeDoc;
    }

    /// <summary>
    /// Создать версию документа.
    /// </summary>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="exchangeDoc">Документ в RX.</param>
    /// <param name="sender">Контрагент-отправитель.</param>
    /// <param name="isIncoming">True, если документ входящий, иначе - false.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="documentFullName">Полное имя документа.</param>
    protected virtual void CreateExchangeDocumentVersion(Document document, IExchangeDocumentInfo info, IOfficialDocument exchangeDoc,
                                                         ICounterparty sender, bool isIncoming, IBoxBase box, string documentFullName)
    {
      using (var memory = new System.IO.MemoryStream(document.Content))
      {
        // Создать версию. Сохранить в версию.
        exchangeDoc.CreateVersion();
        var version = exchangeDoc.LastVersion;
        version.Body.Write(memory);
        version.AssociatedApplication = GetOrCreateAssociatedApplicationByDocumentName(documentFullName);
        info.VersionId = version.Id;
        var accountingDoc = Docflow.AccountingDocumentBases.As(exchangeDoc);
        if (accountingDoc != null && accountingDoc.IsFormalized == true)
        {
          accountingDoc.BusinessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
          var isRegistered =
            Docflow.PublicFunctions.OfficialDocument.TryExternalRegister(accountingDoc, document.FormalizedNumber,
                                                                         document.FormalizedDate);

          accountingDoc.SellerTitleId = version.Id;
          accountingDoc.Subject = string.Empty;

          if (FinancialArchive.Waybills.Is(accountingDoc) ||
              FinancialArchive.ContractStatements.Is(accountingDoc) ||
              FinancialArchive.UniversalTransferDocuments.Is(accountingDoc))
            version.Note = FinancialArchive.Resources.SellerTitleVersionNote;

          if (exchangeDoc.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable || !isRegistered)
          {
            if (document.DocumentType == DocumentType.Invoice ||
                document.DocumentType == DocumentType.InvoiceRevision ||
                document.DocumentType == DocumentType.GeneralTransferSchfSeller ||
                document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller)
              exchangeDoc.Note = Sungero.Exchange.Resources.TaxInvoiceFormat(document.FormalizedNumber, document.FormalizedDateString) +
                Environment.NewLine + exchangeDoc.Note;
            else if (document.DocumentType == DocumentType.InvoiceCorrection ||
                     document.DocumentType == DocumentType.InvoiceCorrectionRevision ||
                     document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller ||
                     document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller)
              exchangeDoc.Note =
                Sungero.Exchange.Resources.TaxInvoiceCorrectionFormat(document.FormalizedNumber, document.FormalizedDateString) +
                Environment.NewLine + exchangeDoc.Note;
            else
              exchangeDoc.Note =
                Sungero.Exchange.Resources.IncomingNotNumeratedDocumentNoteFormat(document.FormalizedDateString,
                                                                                  document.FormalizedNumber) +
                Environment.NewLine + exchangeDoc.Note;
          }
        }

        MarkDocumentAsSended(info, exchangeDoc, sender, isIncoming, box, document.SignStatus);

        exchangeDoc.Save();

        this.GrantAccessRightsForUpperBoxResponsibles(exchangeDoc, box);
      }
    }

    private static void CreateVersionFromExchangeDocument(IOfficialDocument document, IDocument exchangeDocument, IAssociatedApplication application)
    {
      using (var memory = new System.IO.MemoryStream(exchangeDocument.Content))
      {
        // Создать версию. Сохранить в версию.
        document.CreateVersion();
        var version = document.LastVersion;
        version.Body.Write(memory);
        version.AssociatedApplication = application;
        document.Save();
      }
    }
    
    private static void SetDocumentTotalAmount(IAccountingDocumentBase document, FormalizedDocumentXML documentInfo)
    {
      if (!string.IsNullOrEmpty(documentInfo.CurrencyCode))
      {
        var currency = Commons.Currencies.GetAll().Where(x => x.NumericCode == documentInfo.CurrencyCode).FirstOrDefault();
        
        if (currency != null)
        {
          document.Currency = currency;
          document.TotalAmount = documentInfo.TotalAmount;
        }
      }
    }
    
    private static bool TryParseDate(FormalizedDocumentXML documentInfo, out DateTime parsedDate)
    {
      var datePattern = "dd.MM.yyyy";
      var dateStyle = System.Globalization.DateTimeStyles.None;
      return DateTime.TryParseExact(documentInfo.DocumentDate, datePattern, null, dateStyle, out parsedDate);
    }

    /// <summary>
    /// Получить информацию из xml тела документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sender">Контрагент.</param>
    /// <returns>Информацию из xml тела документа.</returns>
    protected virtual FormalizedDocumentXML GetInfoFromXML(IDocument document, ICounterparty sender)
    {
      var xdoc = System.Xml.Linq.XDocument.Load(new System.IO.MemoryStream(document.Content));
      RemoveNamespaces(xdoc);
      var documentComment = string.IsNullOrEmpty(document.Comment) ? string.Empty : Resources.DocumentCommentFormat(document.Comment).ToString();
      var corrected = Docflow.AccountingDocumentBases.Null;
      var contract = Docflow.ContractualDocumentBases.Null;
      var correctionRevisionParentDocument = Docflow.AccountingDocumentBases.Null;
      var documentNumber = string.Empty;
      var documentDate = string.Empty;
      var correctedDocumentNumber = string.Empty;
      var correctedDocumentDate = string.Empty;
      var currencyCode = string.Empty;
      var totalAmount = string.Empty;
      double totalAmountNumeric = 0;
      
      var comment = documentComment;
      if (!string.IsNullOrEmpty(comment) && document.DocumentType != DocumentType.Invoice)
        comment += Environment.NewLine;
      
      // Функция документа, для УПД\УКД.
      Enumeration? function = null;
      
      var isAdjustment = false;
      
      // Признак того, что это исправление (но не исправление корректировки, это отдельный тип).
      var isRevision = false;
      
      // А это любое исправление для галочки в документе.
      var isAnyRevision = false;
      
      // Определяем КНД документа для уточнения его типа.
      var taxDocumentClassifierCode = GetTaxDocumentClassifier(document).TaxDocumentClassifierCode;
      
      // Если пришел с типом старого акта, а в теле КНД от УПД, заносим как УПД.
      var isUTDDop = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller &&
        (document.DocumentType == DocumentType.Act || document.DocumentType == DocumentType.Waybill);
      var isRevisionUTDDop = isUTDDop && xdoc.Descendants("ИспрСчФ").Any();
      if (document.DocumentType == DocumentType.Invoice ||
          document.DocumentType == DocumentType.GeneralTransferSchfSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfDopSeller ||
          document.DocumentType == DocumentType.GeneralTransferDopSeller ||
          (isUTDDop && !isRevisionUTDDop))
      {
        var taxInvoiceInfo = xdoc.Element("Файл").Element("Документ").Element("СвСчФакт");
        documentNumber = GetAttributeValueByName(taxInvoiceInfo, "НомерСчФ");
        documentDate = GetAttributeValueByName(taxInvoiceInfo, "ДатаСчФ");
        
        currencyCode = GetAttributeValueByName(taxInvoiceInfo, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("ТаблСчФакт").Element("ВсегоОпл");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтТовУчНалВсего");
        
        if (document.DocumentType == DocumentType.GeneralTransferSchfSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        if (document.DocumentType == DocumentType.GeneralTransferSchfDopSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
        if (document.DocumentType == DocumentType.GeneralTransferDopSeller || isUTDDop)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
      }

      if (document.DocumentType == DocumentType.InvoiceCorrection ||
          document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfDopCorrectionSeller ||
          document.DocumentType == DocumentType.GeneralTransferDopCorrectionSeller)
      {
        var taxInvoiceInfo = xdoc.Element("Файл").Element("Документ").Element("СвКСчФ");
        documentNumber = GetAttributeValueByName(taxInvoiceInfo, "НомерКСчФ");
        documentDate = GetAttributeValueByName(taxInvoiceInfo, "ДатаКСчФ");
        var correctedTaxInvoiceInfo = taxInvoiceInfo.Element("СчФ");
        correctedDocumentNumber = GetAttributeValueByName(correctedTaxInvoiceInfo, "НомерСчФ");
        correctedDocumentDate = GetAttributeValueByName(correctedTaxInvoiceInfo, "ДатаСчФ");
        comment += Resources.TaxInvoiceToFormat(correctedDocumentNumber, correctedDocumentDate);
        
        isAdjustment = true;
        
        if (document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        if (document.DocumentType == DocumentType.GeneralTransferSchfDopCorrectionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
        if (document.DocumentType == DocumentType.GeneralTransferDopCorrectionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
      }
      
      if (document.DocumentType == DocumentType.InvoiceCorrectionRevision ||
          document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller ||
          document.DocumentType == DocumentType.GeneralTransferDopCorrectionRevisionSeller)
      {
        var taxInvoiceInfo = xdoc.Element("Файл").Element("Документ").Element("СвКСчФ");
        documentNumber = GetAttributeValueByName(taxInvoiceInfo, "НомерКСчФ");
        documentDate = GetAttributeValueByName(taxInvoiceInfo, "ДатаКСчФ");
        var correctedTaxInvoiceInfo = taxInvoiceInfo.Element("ИспрКСчФ");
        correctedDocumentNumber = GetAttributeValueByName(correctedTaxInvoiceInfo, "НомИспрКСчФ");
        correctedDocumentDate = GetAttributeValueByName(correctedTaxInvoiceInfo, "ДатаИспрКСчФ");
        comment += Resources.TaxInvoiceRevisionFormat(correctedDocumentNumber, correctedDocumentDate);

        isAdjustment = true;
        isAnyRevision = true;
        
        if (document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        if (document.DocumentType == DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
        if (document.DocumentType == DocumentType.GeneralTransferDopCorrectionRevisionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
      }
      
      if (document.DocumentType == DocumentType.InvoiceRevision ||
          document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller ||
          document.DocumentType == DocumentType.GeneralTransferSchfDopRevisionSeller ||
          document.DocumentType == DocumentType.GeneralTransferDopRevisionSeller ||
          isRevisionUTDDop)
      {
        var taxInvoiceInfo = xdoc.Element("Файл").Element("Документ").Element("СвСчФакт");
        var revisionTaxInvoiceInfo = taxInvoiceInfo.Element("ИспрСчФ");

        // Если это исправление, то номер должен быть как у первичного СФ.
        documentNumber = GetAttributeValueByName(taxInvoiceInfo, "НомерСчФ");
        documentDate = GetAttributeValueByName(taxInvoiceInfo, "ДатаСчФ");
        
        // В примечание запишем номер и дату исправления.
        var initialDocumentNumber = GetAttributeValueByName(revisionTaxInvoiceInfo, "НомИспрСчФ");
        var initialDocumentDate = GetAttributeValueByName(revisionTaxInvoiceInfo, "ДатаИспрСчФ");
        
        comment += Resources.TaxInvoiceRevisionFormat(initialDocumentNumber, initialDocumentDate);

        currencyCode = GetAttributeValueByName(taxInvoiceInfo, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("ТаблСчФакт").Element("ВсегоОпл");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтТовУчНалВсего");
        
        isRevision = true;
        isAnyRevision = true;
        
        if (document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        if (document.DocumentType == DocumentType.GeneralTransferSchfDopRevisionSeller)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop;
        if (document.DocumentType == DocumentType.GeneralTransferDopRevisionSeller || isRevisionUTDDop)
          function = Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
      }
      
      var isGoodsTransferSeller = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.GoodsTransferSeller;
      var isRevisionGoodsTransferSeller = xdoc.Descendants("ИспрДокПТ").Any();
      
      // Проверка типа документа по КНД, т.к. из Диадока приходит ДПТ с типом торг-12.
      if (document.DocumentType == DocumentType.GoodsTransferSeller || (isGoodsTransferSeller && !isRevisionGoodsTransferSeller))
      {
        var goodsTransferSellerInfo =
          xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СвДокПТПр").Element("ИдентДок");
        documentNumber = GetAttributeValueByName(goodsTransferSellerInfo, "НомДокПТ");
        documentDate = GetAttributeValueByName(goodsTransferSellerInfo, "ДатаДокПТ");
        
        var currencyCodeElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СвДокПТПр").Element("ДенИзм");
        currencyCode = GetAttributeValueByName(currencyCodeElement, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СодФХЖ2").Element("Всего");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтУчНДСВс");
      }

      if (document.DocumentType == DocumentType.GoodsTransferRevisionSeller || (isGoodsTransferSeller && isRevisionGoodsTransferSeller))
      {
        var goodsTransferSellerInfo =
          xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СвДокПТПр");
        var goodsTransferRevisionSellerInfo = goodsTransferSellerInfo.Element("ИспрДокПТ");
        
        var initialDocumentNumber = GetAttributeValueByName(goodsTransferRevisionSellerInfo, "НомИспрДокПТ");
        var initialDocumentDate = GetAttributeValueByName(goodsTransferRevisionSellerInfo, "ДатаИспрДокПТ");
        
        documentNumber = GetAttributeValueByName(goodsTransferSellerInfo.Element("ИдентДок"), "НомДокПТ");
        documentDate = GetAttributeValueByName(goodsTransferSellerInfo.Element("ИдентДок"), "ДатаДокПТ");
        
        comment += Resources.TaxInvoiceRevisionFormat(initialDocumentNumber, initialDocumentDate);
        
        var currencyCodeElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СвДокПТПр").Element("ДенИзм");
        currencyCode = GetAttributeValueByName(currencyCodeElement, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПТПрКроме").Element("СодФХЖ2").Element("Всего");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтУчНДСВс");
        
        isRevision = true;
        isAnyRevision = true;
      }
      
      var isWorksTransferSeller = taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.WorksTransferSeller;
      var isRevisionWorksTransferSeller = xdoc.Descendants("ИспрДокПРУ").Any();
      
      // Проверка типа документа по КНД, т.к. из Диадока приходит ДПРР с типом акт старого формата.
      if (document.DocumentType == DocumentType.WorksTransferSeller || (isWorksTransferSeller && !isRevisionWorksTransferSeller))
      {
        var worksTransferSellerInfo =
          xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ").Element("ИдентДок");
        documentNumber = GetAttributeValueByName(worksTransferSellerInfo, "НомДокПРУ");
        documentDate = GetAttributeValueByName(worksTransferSellerInfo, "ДатаДокПРУ");
        
        var currencyCodeElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ").Element("ДенИзм");
        currencyCode = GetAttributeValueByName(currencyCodeElement, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ").Element("СодФХЖ1").Element("ОписРабот");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтУчНДСИт");
      }
      
      if (document.DocumentType == DocumentType.WorksTransferRevisionSeller || (isWorksTransferSeller && isRevisionWorksTransferSeller))
      {
        var worksTransferSellerInfo =
          xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ");
        var worksTransferRevisionSellerInfo = worksTransferSellerInfo.Element("ИспрДокПРУ");
        
        var initialDocumentNumber = GetAttributeValueByName(worksTransferRevisionSellerInfo, "НомИспрДокПРУ");
        var initialDocumentDate = GetAttributeValueByName(worksTransferRevisionSellerInfo, "ДатаИспрДокПРУ");
        
        documentNumber = GetAttributeValueByName(worksTransferSellerInfo.Element("ИдентДок"), "НомДокПРУ");
        documentDate = GetAttributeValueByName(worksTransferSellerInfo.Element("ИдентДок"), "ДатаДокПРУ");
        comment += Resources.TaxInvoiceRevisionFormat(initialDocumentNumber, initialDocumentDate);
        
        var currencyCodeElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ").Element("ДенИзм");
        currencyCode = GetAttributeValueByName(currencyCodeElement, "КодОКВ");
        var totalAmountElement = xdoc.Element("Файл").Element("Документ").Element("СвДокПРУ").Element("СодФХЖ1").Element("ОписРабот");
        totalAmount = GetAttributeValueByName(totalAmountElement, "СтУчНДСИт");
        
        isRevision = true;
        isAnyRevision = true;
      }
      
      // Проверка типа документа по КНД, т.к. из Диадока приходит ДПТ с типом торг-12.
      if (document.DocumentType == DocumentType.Waybill && taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.Waybill)
      {
        var waybillTotalAmountInfo = xdoc.Element("Файл").Element("Документ").Element("СвТНО")
          .Element("ТН").Element("Таблица").Element("ВсегоНакл");
        
        currencyCode = Constants.Module.RoubleCurrencyCode;
        totalAmount = GetAttributeValueByName(waybillTotalAmountInfo, "СумУчНДСВс");
      }
      
      // Проверка типа документа по КНД, т.к. из Диадока приходит ДПРР с типом акт старого формата.
      if (document.DocumentType == DocumentType.Act && taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.Act)
      {
        var actTotalAmountInfo = xdoc.Element("Файл").Element("Документ").Element("СвАктИ").Element("ОписРабот");
        
        currencyCode = Constants.Module.RoubleCurrencyCode;
        totalAmount = GetAttributeValueByName(actTotalAmountInfo, "СумУчНДСИт");
      }
      
      var parentDocument = Docflow.AccountingDocumentBases.Null;
      if (isAdjustment || isRevision)
      {
        var parentDocumentInfo = Sungero.Exchange.ExchangeDocumentInfos.GetAll()
          .Where(x => x.ServiceDocumentId == document.ParentServiceEntityId && Equals(x.Counterparty, sender))
          .FirstOrDefault();

        if (parentDocumentInfo != null && parentDocumentInfo.Document != null)
        {
          parentDocument = Sungero.Docflow.AccountingDocumentBases.As(parentDocumentInfo.Document);
        }
        else if (!string.IsNullOrEmpty(correctedDocumentNumber))
        {
          var datePattern = "dd.MM.yyyy";
          var dateStyle = System.Globalization.DateTimeStyles.None;
          DateTime parsedCorrectedDocumentDate;
          if (!string.IsNullOrEmpty(correctedDocumentDate) &&
              DateTime.TryParseExact(correctedDocumentDate, datePattern, null, dateStyle, out parsedCorrectedDocumentDate))
          {
            parentDocument = Sungero.Docflow.AccountingDocumentBases.GetAll()
              .Where(x => x.RegistrationNumber == correctedDocumentNumber &&
                     x.RegistrationDate == parsedCorrectedDocumentDate && Equals(x.Counterparty, sender))
              .FirstOrDefault();
          }
        }
      }

      if (isAdjustment)
      {
        // Исправление корректировки корректирует первоначальный документ, между корректировкой и исправлением корректировки связь с типом "Прочие".
        if (parentDocument != null &&
            (document.DocumentType == DocumentType.InvoiceCorrectionRevision ||
             document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
             document.DocumentType == DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller ||
             document.DocumentType == DocumentType.GeneralTransferDopCorrectionRevisionSeller))
        {
          correctionRevisionParentDocument = parentDocument;
          corrected = correctionRevisionParentDocument.Corrected;
        }
        else
        {
          corrected = parentDocument;
        }
        
        if (corrected != null)
          contract = corrected.LeadingDocument;
        
        if (corrected != null &&
            document.DocumentType != DocumentType.InvoiceCorrectionRevision &&
            document.DocumentType != DocumentType.GeneralTransferSchfCorrectionRevisionSeller &&
            document.DocumentType != DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller &&
            document.DocumentType != DocumentType.GeneralTransferDopCorrectionRevisionSeller)
          comment = documentComment;
      }
      
      if (isRevision && parentDocument != null)
        contract = parentDocument.LeadingDocument;
      
      if (!string.IsNullOrEmpty(totalAmount) && !string.IsNullOrEmpty(currencyCode))
      {
        // Если распарсить не получилось, валюту не указываем в документе.
        if (!double.TryParse(totalAmount, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out totalAmountNumeric))
          currencyCode = string.Empty;
      }
      
      return Structures.Module.FormalizedDocumentXML.Create(documentNumber, documentDate, isAdjustment, comment, corrected, contract,
                                                            correctionRevisionParentDocument, currencyCode, totalAmountNumeric, isAnyRevision, function);
    }
    
    private static Sungero.Exchange.Structures.Module.ITaxDocumentClassifier GetTaxDocumentClassifier(IDocument document)
    {
      return GetTaxDocumentClassifierByContent(new System.IO.MemoryStream(document.Content));
    }

    /// <summary>
    /// Получить КНД по содержимому документа.
    /// </summary>
    /// <param name="content">Содержимое документа.</param>
    /// <returns>КНД.</returns>
    [Public]
    public static Sungero.Exchange.Structures.Module.ITaxDocumentClassifier GetTaxDocumentClassifierByContent(System.IO.Stream content)
    {
      var xdoc = System.Xml.Linq.XDocument.Load(content);
      RemoveNamespaces(xdoc);
      // Определяем КНД документа для уточнения его типа.
      var documentSection = xdoc.Element("Файл").Element("Документ");
      var taxDocumentClassifierCode = GetAttributeValueByName(documentSection, "КНД");
      var functionUTD = string.Empty;
      
      if (taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller ||
          taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller155 ||
          taxDocumentClassifierCode == Constants.Module.TaxDocumentClassifier.UniversalCorrectionDocumentSeller)
      {
        functionUTD = GetAttributeValueByName(documentSection, "Функция");
      }
      return TaxDocumentClassifier.Create(taxDocumentClassifierCode, functionUTD);
    }

    /// <summary>
    /// Получить печатную форму из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">Версия документа.</param>
    /// <returns>Признак успешности загрузки печатной формы из сервиса обмена.</returns>
    [Public]
    public virtual bool GeneratePublicBodyFromService(IOfficialDocument document, int versionId)
    {
      var documentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, versionId);
      if (documentInfo == null)
        return false;
      
      var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(documentInfo.RootBox) as NpoComputer.DCX.ClientApi.Client;
      var printedForm = client.GetDocumentPrintedForm(documentInfo.ServiceMessageId, documentInfo.ServiceDocumentId);
      
      if (printedForm != null)
      {
        using (var memory = new System.IO.MemoryStream(printedForm))
        {
          var version = document.Versions.FirstOrDefault(ver => Equals(ver.Id, versionId));
          version.PublicBody.Write(memory);
          version.AssociatedApplication = Content.AssociatedApplications.GetByExtension("pdf");
          document.Save();
        }
        return true;
      }
      
      return false;
    }
    
    #endregion

    #region Отправка задач

    /// <summary>
    /// Стартовать задачу на обработку.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="infos">Информация по обработке документов.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="exchangeTaskActiveTextBoundedDocuments">Часть ActiveText для формирования задачи на обработку для связанных документов.</param>
    /// <returns>Признак успешности отправки задачи.</returns>
    protected virtual bool StartExchangeTask(IMessage message, List<IExchangeDocumentInfo> infos, ICounterparty sender,
                                             bool isIncoming, IBoxBase box, string exchangeTaskActiveTextBoundedDocuments)
    {
      var task = this.CreateExchangeTask(message, infos, sender, isIncoming, box, exchangeTaskActiveTextBoundedDocuments);
      if (task != null)
      {
        if (task.Started == null)
          task.Start();
        return true;
      }

      return false;
    }
    
    /// <summary>
    /// Создать задачу на обработку входящих документов эл. обмена.
    /// </summary>
    /// <param name="infos">Информация по документам.</param>
    /// <param name="counterparty">КА из сервиса обмена.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="incomeDate">Дата получения.</param>
    /// <param name="mainProcessingTask">Главная задача.</param>
    /// <returns>Задача на обработку входящих документов эл. обмена.</returns>
    public IExchangeDocumentProcessingTask CreateExchangeTask(List<IExchangeDocumentInfo> infos, ICounterparty counterparty,
                                                              IBoxBase box, DateTime incomeDate, ITask mainProcessingTask)
    {
      
      var task = mainProcessingTask == null ?
        Sungero.Exchange.ExchangeDocumentProcessingTasks.Create() :
        Sungero.Exchange.ExchangeDocumentProcessingTasks.CreateAsSubtask(mainProcessingTask);
      
      task.Box = box;
      task.Counterparty = counterparty;
      task.ExchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
      task.Assignee = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, counterparty, infos);
      task.Deadline = Sungero.ExchangeCore.PublicFunctions.BoxBase.GetProcessingTaskDeadline(box, task.Assignee);
      task.IncomeDate = incomeDate;
      var isIncoming = infos.FirstOrDefault().MessageType == Exchange.ExchangeDocumentInfo.MessageType.Incoming;
      task.IsIncoming = isIncoming;
      
      // Если задача не ответственному за текущий ящик, сообщить об этом исполнителю.
      if (Equals(box.Routing, ExchangeCore.DepartmentBox.Routing.BoxResponsible) && !Equals(task.Assignee, box.Responsible) && ExchangeCore.DepartmentBoxes.Is(box))
      {
        var departmentBox = ExchangeCore.DepartmentBoxes.As(box);
        var department = departmentBox.Department;
        var departmentName = department != null ? department.Name : departmentBox.ServiceName;
        
        if (box.Status == ExchangeCore.BoxBase.Status.Closed)
          task.ActiveText = ExchangeDocumentProcessingTasks.Resources.DocumentSentToClosedBoxFormat(departmentName);
        else
          task.ActiveText = ExchangeDocumentProcessingTasks.Resources.DocumentSentToAnotherResponsibleFormat(departmentName);
        
        task.ActiveText += Environment.NewLine;
      }
      
      using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
        task.Subject = this.GenerateExchangeTaskSubject(task);
      task.Save();
      return task;
    }

    /// <summary>
    /// Создать задачу на обработку.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="infos">Информация по документам, созданным из сообщения, по которому формируется задача.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="isIncoming">True - от контрагента, false - наше.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="exchangeTaskActiveTextBoundedDocuments">Часть ActiveText для формирования задачи на обработку для связанных документов.</param>
    /// <returns>Задача.</returns>
    protected virtual IExchangeDocumentProcessingTask CreateExchangeTask(IMessage message, List<IExchangeDocumentInfo> infos,
                                                                         ICounterparty sender, bool isIncoming, IBoxBase box, string exchangeTaskActiveTextBoundedDocuments)
    {
      var infosForSend = new List<IExchangeDocumentInfo>();
      ITask mainProcessingTask = null;
      IExchangeDocumentProcessingTask parentTask = ExchangeDocumentProcessingTasks.Null;
      foreach (var info in infos)
      {
        var documentGuid = info.Document.GetEntityMetadata().GetOriginal().NameGuid;
        parentTask = ExchangeDocumentProcessingTasks.GetAll()
          .Where(t => t.AttachmentDetails.Any(att => att.AttachmentId == info.Document.Id && att.EntityTypeGuid == documentGuid))
          .FirstOrDefault();
        
        if (parentTask == null)
          infosForSend.Add(info);
        else if (mainProcessingTask == null)
          mainProcessingTask = parentTask.MainTask;
      }
      
      if (!infosForSend.Any())
        return ExchangeDocumentProcessingTasks.As(mainProcessingTask) ?? parentTask;
      
      var task = this.CreateExchangeTask(infos, sender, box, message.TimeStamp, mainProcessingTask);
      task.Save();
      
      var text = string.Empty;
      if (!isIncoming)
        text = ExchangeDocumentProcessingTasks.Resources.OutcomingDocumentProcessingTaskActiveTextFormat(ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box));
      else
        text = ExchangeDocumentProcessingTasks.Resources.TaskActiveText;
      
      if (task.ActiveText != string.Empty)
      {
        text += Environment.NewLine;
        text += Environment.NewLine;
        text += task.ActiveText;
      }
      task.ActiveText = text;
      
      var needSign = new List<Docflow.IOfficialDocument>();
      var signed = new List<Docflow.IOfficialDocument>();
      var dontNeedSign = new List<Docflow.IOfficialDocument>();
      foreach (var info in infos.Where(i => i.Document != null))
      {
        var exchangeDocument = info.Document;
        var processingDocument = message.PrimaryDocuments.Single(d => d.ServiceEntityId == info.ServiceDocumentId);
        if (processingDocument.SignStatus == NpoComputer.DCX.Common.SignStatus.Waiting)
        {
          if (isIncoming)
            needSign.Add(exchangeDocument);
          else
            dontNeedSign.Add(exchangeDocument);
        }
        else if (processingDocument.SignStatus == NpoComputer.DCX.Common.SignStatus.Signed)
          signed.Add(exchangeDocument);
        else
          dontNeedSign.Add(exchangeDocument);
      }
      
      foreach (var doc in needSign)
      {
        task.NeedSigning.All.Add(doc);
      }
      
      foreach (var doc in signed)
      {
        if (isIncoming)
        {
          var hyperlink = Hyperlinks.Get(doc);
          task.ActiveText += Environment.NewLine;
          task.ActiveText += Environment.NewLine;
          task.ActiveText += ExchangeDocumentProcessingTasks.Resources.DocumentIsSignedByUsFormat(hyperlink);
        }
        else
        {
          var hyperlink = Hyperlinks.Get(doc);
          task.ActiveText += Environment.NewLine;
          task.ActiveText += Environment.NewLine;
          task.ActiveText += Resources.DocumentIsSignedByBothSidesFormat(hyperlink);
        }
        
        task.DontNeedSigning.All.Add(doc);
      }

      var processedDocumentTypes = this.GetSupportedPrimaryDocumentTypes();
      var processingDocuments = message.PrimaryDocuments.Where(x => processedDocumentTypes.Contains(x.DocumentType.Value));
      var rejected = processingDocuments.Where(d => d.SignStatus == SignStatus.Rejected).ToList();
      if (rejected.Any())
      {
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Environment.NewLine;
        
        var documents = string.Join(", ", rejected.Select(r => r.FileName));
        if (rejected.Count == 1)
          task.ActiveText += ExchangeDocumentProcessingTasks.Resources.DocumentIsRejectedByUsFormat(documents);
        else
          task.ActiveText += ExchangeDocumentProcessingTasks.Resources.DocumentsIsRejectedByUsFormat(documents);
      }
      
      foreach (var doc in dontNeedSign)
      {
        task.DontNeedSigning.All.Add(doc);
      }
      
      if (mainProcessingTask == null)
        task.ActiveText += exchangeTaskActiveTextBoundedDocuments;
      else
      {
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Sungero.Exchange.Resources.AdditionalDocumentSend;
        foreach (var additionalDocument in infosForSend.Select(d => d.Document))
        {
          task.ActiveText += Environment.NewLine;
          task.ActiveText += Hyperlinks.Get(additionalDocument);
        }
      }
      
      // Обработка формализованных документов в сообщении.
      var formalizedDocuments = message.PrimaryDocuments.Where(x => !processedDocumentTypes.Contains(x.DocumentType.Value));
      if (formalizedDocuments.Any())
      {
        task.ActiveText += System.Environment.NewLine;
        task.ActiveText += System.Environment.NewLine;
        task.ActiveText += this.GenerateActiveTextFromUnsupportedDocuments(formalizedDocuments, sender, isIncoming, box, message.TimeStamp, false);
      }
      
      task.Save();
      return task;
    }
    
    private string GenerateExchangeTaskSubject(IExchangeDocumentProcessingTask task)
    {
      var businessUnit = ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(task.Box);
      var subject = string.Empty;
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(task.IncomeDate.Value);
      subject = task.IsIncoming == true ?
        ExchangeDocumentProcessingTasks.Resources.TaskSubjectFormat(task.Counterparty, businessUnit, dateWithUTC, task.ExchangeService) :
        ExchangeDocumentProcessingTasks.Resources.TaskSubjectFormat(businessUnit, task.Counterparty, dateWithUTC, task.ExchangeService);
      subject = CutText(subject, task.Info.Properties.Subject.Length);
      return subject;
    }

    /// <summary>
    /// Отправлять задания/уведомления ответственному.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="message">Сообщение.</param>
    /// <returns>Признак отправки задания ответственному за ящик/контрагента.</returns>
    protected virtual bool NeedReceiveDocumentProcessingTask(IBoxBase box, IMessage message)
    {
      return ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
    }
    
    #endregion

    #endregion

    #region Обработка ответов от контрагентов
    
    #region Обработка подписания

    /// <summary>
    /// Обработать пришедшие подписи к неформализованным документам.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="client">Клиент к сервису обмена.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="historyOperation">Операция истории - мы подписали или КА подписал.</param>
    /// <param name="historyComment">Комментарий к операции истории.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessNonformalizedSign(IMessage message, IMessageQueueItem queueItem, DcxClient client, IBoxBase box,
                                                    ICounterparty sender, bool isIncoming, Enumeration historyOperation, string historyComment)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessNonformalizedSign.");
      foreach (var document in message.PrimaryDocuments.Where(x => x.SignStatus == NpoComputer.DCX.Common.SignStatus.Signed &&
                                                              x.DocumentType == NpoComputer.DCX.Common.DocumentType.Nonformalized))
      {
        var doc = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
        
        var sign = message.Signatures.FirstOrDefault(x => x.DocumentId == document.ServiceEntityId);
        if (sign == null)
        {
          this.LogDebugFormat(message, queueItem, box, "Message not contain a signature.");
          return false;
        }
        
        var certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(sign.Content);
        var signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
        
        // Пропускаем документы, которые были отправлены нами через личный кабинет сервиса обмена, а также документы без подписи.
        if (doc == null)
        {
          if (this.CanProcessMessageLater(message, queueItem, ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box), document.ServiceEntityId))
          {
            this.LogDebugFormat(message, document, "Document not found for received signature.");
            return false;
          }
          
          continue;
        }
        
        // Когда контрагент прислал нам подпись, а у нас нет документа - нужно немного магии с сообщениями.
        if (doc.Document == null)
        {
          // Загружаем первичное сообщение для создания документа, который не был создан сразу.
          doc = this.LoadDocumentWithSecondSign(message, doc, document, client, sender, isIncoming, box);
        }

        var sentVersion = doc.Document.Versions.FirstOrDefault(x => x.Id == doc.VersionId);
        var newDocumentHash = document.Content.GetMD5Hash();
        var versionIsChanged = false;
        
        if (sentVersion != null && (newDocumentHash == sentVersion.Body.Hash))
        {
          // Прикрепление к неизмененной версии.
          var signatures = message.Signatures.Where(x => x.DocumentId == document.ServiceEntityId);
          var addedThumbprints = Signatures.Get(sentVersion)
            .Where(s => s.SignCertificate != null)
            .Select(x => x.SignCertificate.Thumbprint);
          foreach (var signature in signatures)
          {
            certificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature.Content);
            var signatureIsAlreadyAdded = addedThumbprints.Any(x => x.Equals(certificateInfo.Thumbprint));
            if (!signatureIsAlreadyAdded)
            {
              signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(certificateInfo.SubjectInfo);
              this.SignDocument(doc, signature, sentVersion, signatoryInfo, message.TimeStamp);
            }
          }
        }
        else
        {
          var outMessage = client.GetMessage(doc.ServiceMessageId);
          
          if (outMessage != null)
          {
            var ourSign = outMessage.Signatures.FirstOrDefault(x => x.DocumentId == document.ServiceEntityId);
            var ourCertificateInfo = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(ourSign.Content);
            
            var application = GetOrCreateAssociatedApplicationByDocumentName(document.FileName);
            CreateVersionFromExchangeDocument(doc.Document, document, application);
            
            var originalVersion = doc.Document.LastVersion;
            
            // Прикрепляем нашу подпись как внешнюю.
            var ourSignatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(ourCertificateInfo.SubjectInfo);
            this.SignDocument(doc, ourSign, originalVersion, ourSignatoryInfo, outMessage.TimeStamp);
            
            // Прикрепляем подпись контрагента.
            this.SignDocument(doc, sign, originalVersion, signatoryInfo, message.TimeStamp);
            
            sentVersion = originalVersion;
            doc.VersionId = originalVersion.Id;
            doc.Save();
            
            versionIsChanged = true;
          }
        }
        
        this.ProcessSharedSign(doc.Document, doc, isIncoming, box, sentVersion, signatoryInfo, versionIsChanged, historyOperation, historyComment, true);
        this.FillCounterpartyDataFromReplyMessage(message, doc.ServiceDocumentId, doc.Document, sender, isIncoming);
      }
      
      return true;
    }
    
    /// <summary>
    /// Обработать пришедшие титулы к формализованным документам.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="queueItems">Все элементы очереди.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="historyOperation">Операция истории - мы подписали или КА подписал.</param>
    /// <param name="historyComment">Комментарий к операции истории.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessFormalizedSign(IMessage message, IMessageQueueItem queueItem, List<IMessageQueueItem> queueItems,
                                                 bool isIncoming, IBoxBase box, Enumeration historyOperation, string historyComment)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessFormalizedSign.");
      // Обработка титулов покупателей.
      foreach (var document in message.ReglamentDocuments.Where(x => this.GetSupportedReglamentDocumentTypes().Contains(x.DocumentType)))
      {
        if (!this.ProcessFormalizedTitlesAndSigns(message, queueItem, queueItems, isIncoming, box, historyOperation, historyComment,
                                                  document.RootServiceEntityId, document.ServiceEntityId, document.Content))
          return false;
      }
      
      // Загрузка ответной подписи на СЧФ для СБИС.
      if (queueItem.RootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
      {
        foreach (var document in message.PrimaryDocuments.Where(d => d.SignStatus == NpoComputer.DCX.Common.SignStatus.Signed && d.DocumentType == NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfSeller))
        {
          if (!this.ProcessFormalizedTitlesAndSigns(message, queueItem, queueItems, isIncoming, box, historyOperation, historyComment,
                                                    document.ServiceEntityId, document.ServiceEntityId, null))
            return false;
        }
      }
      
      return true;
    }
    
    /// <summary>
    /// Обработать пришедшие титулы к формализованным документам и ответные подписи на СЧФ из СБИСа.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="queueItems">Все элементы очереди.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="historyOperation">Операция истории - мы подписали или КА подписал.</param>
    /// <param name="historyComment">Комментарий к операции истории.</param>
    /// <param name="rootServiceDocumentId">Ид родительского документа на сервисе.</param>
    /// <param name="serviceDocumentId">Ид документа на сервисе.</param>
    /// <param name="reglamentDocumentContent">Контент титула.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessFormalizedTitlesAndSigns(IMessage message, IMessageQueueItem queueItem, List<IMessageQueueItem> queueItems,
                                                           bool isIncoming, IBoxBase box, Enumeration historyOperation, string historyComment,
                                                           string rootServiceDocumentId, string serviceDocumentId, byte[] reglamentDocumentContent)
    {
      var doc = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, rootServiceDocumentId);
      if (doc == null)
      {
        // Если документ был аннулирован, то титул нам больше не нужен. Иначе документ ещё может загрузиться.
        var documentInService = message.PrimaryDocuments.FirstOrDefault(d => d.ServiceEntityId == rootServiceDocumentId);
        if (documentInService != null && documentInService.RevocationStatus == RevocationStatus.RevocationAccepted)
          return true;
        
        if (documentInService != null && documentInService.BuyerAcceptanceStatus == NpoComputer.DCX.Common.BuyerAcceptanceStatus.Rejected)
          return true;
        
        // Если документ ещё в очереди, обработаем позже.
        if (queueItems.Any(m => !Equals(m, queueItem) && m.Documents.Any(d => d.ExternalId == rootServiceDocumentId &&
                                                                         d.Type == ExchangeCore.MessageQueueItemDocuments.Type.Primary)))
        {
          this.LogDebugFormat(queueItem, "Document not found for received signature: ServiceEntityId: '{0}', RootServiceEntityId '{1}'.",
                              serviceDocumentId, rootServiceDocumentId);
          return false;
        }
        
        return true;
      }
      
      // Документ был подписан в RX, заканчиваем обработку.
      if (doc.OutgoingStatus == Exchange.ExchangeDocumentInfo.OutgoingStatus.Signed ||
          doc.OutgoingStatus == Exchange.ExchangeDocumentInfo.OutgoingStatus.Rejected &&
          doc.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        return true;
      
      var sign = message.Signatures.FirstOrDefault(x => x.DocumentId == serviceDocumentId);
      if (sign == null)
      {
        this.LogDebugFormat(message, queueItem, box, "Message not contain a signature for document with id = '{0}'.", serviceDocumentId);
        return false;
      }
      
      var primaryDocument = message.PrimaryDocuments.FirstOrDefault(d => d.ServiceEntityId == rootServiceDocumentId);
      if (primaryDocument != null && primaryDocument.BuyerAcceptanceStatus != null)
        doc.BuyerAcceptanceStatus = this.GetBuyerAcceptanceStatus(primaryDocument);
      
      var x509certificate = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(sign.Content);
      var signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(x509certificate.SubjectInfo);
      
      if (doc.Document != null)
      {
        var formalizedDocument = Sungero.Docflow.AccountingDocumentBases.As(doc.Document);
        if (formalizedDocument != null && reglamentDocumentContent != null)
        {
          using (var memory = new System.IO.MemoryStream(reglamentDocumentContent))
          {
            formalizedDocument.CreateVersion();
            var version = formalizedDocument.LastVersion;
            version.AssociatedApplication = GetOrCreateAssociatedApplicationByDocumentName("file.xml");
            version.Note = FinancialArchive.Resources.BuyerTitleVersionNote;
            formalizedDocument.BuyerTitleId = version.Id;
            version.Body.Write(memory);
            formalizedDocument.Save();
          }
        }

        this.SignDocument(doc, sign, doc.Document.LastVersion, signatoryInfo, message.TimeStamp);
        if (formalizedDocument != null)
        {
          var lastSignature = this.GetLastDocumentSignature(formalizedDocument);
          formalizedDocument.BuyerSignatureId = lastSignature.Id;
        }
        
        var sentVersion = doc.Document.Versions.FirstOrDefault(x => x.Id == doc.VersionId);
        
        if (doc.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        {
          this.ProcessSharedReject(doc, doc.Document, isIncoming, box, sign.Content, historyOperation, historyComment, string.Empty, string.Empty, true);
        }
        else
        {
          this.ProcessSharedSign(doc.Document, doc, isIncoming, box, doc.Document.LastVersion, signatoryInfo, false, historyOperation, historyComment, true);
          this.FillCounterpartyDataFromReplyMessage(message, serviceDocumentId, formalizedDocument, formalizedDocument.Counterparty, isIncoming);
        }
      }

      return true;
    }

    /// <summary>
    /// Обработать подписание документа - как из RX, так и из веба.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="info">Инфошка документа.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="signedVersion">Реально подписанная версия (на случай как раз изменения отправленной версии).</param>
    /// <param name="signatoryInfo">Информация о подписавшем для задач уведомления. Будет пустой, если вызвано из действия "Подписать и отправить".</param>
    /// <param name="sentVersionIsChanged">Признак того, что версия была изменена в RX после отправки (не должно существовать?).</param>
    /// <param name="historyOperation">Операция - подписали мы или контрагент.</param>
    /// <param name="historyComment">Комментарий к операции в истории.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    protected virtual void ProcessSharedSign(IOfficialDocument document, IExchangeDocumentInfo info, bool isIncoming, IBoxBase box,
                                             IElectronicDocumentVersions signedVersion, string signatoryInfo, bool sentVersionIsChanged,
                                             Enumeration historyOperation, string historyComment, bool isAgent)
    {
      this.LogDebugFormat(info, "Execute ProcessSharedSign.");
      var notSigned = info.OutgoingStatus != Exchange.ExchangeDocumentInfo.OutgoingStatus.Signed;
      var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
      if (!isIncoming && notSigned)
      {
        var responsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(info.Box, info.Counterparty, new List<IExchangeDocumentInfo>() { info });
        if (isAgent && needReceive)
          this.CreateNoticeAfterOurSigning(document, box, true, false, string.Empty);
        
        info.OutgoingStatus = Exchange.ExchangeDocumentInfo.OutgoingStatus.Signed;
        
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
        {
          var tracking = document.Tracking.AddNew();
          tracking.Action = Docflow.OfficialDocumentTracking.Action.Sending;
          tracking.DeliveredTo = Company.Employees.Current ?? responsible;
          tracking.IsOriginal = true;
          tracking.ReturnDeadline = null;
          tracking.Note = Resources.SendSignToCounterpartyFormat(exchangeService.Name);
          tracking.ExternalLinkId = info.Id;
        }
      }
      
      var detailedOperation = new Enumeration(Constants.Module.Exchange.DetailedSign);
      if (isIncoming)
        document.ExternalApprovalState = Docflow.OfficialDocument.ExternalApprovalState.Signed;
      else
        document.InternalApprovalState = Docflow.OfficialDocument.InternalApprovalState.Signed;

      var externalApprovalInTracking = document.Tracking.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement
                                                               && !x.ReturnResult.HasValue && x.ExternalLinkId == info.Id);
      foreach (var trackingString in externalApprovalInTracking)
      {
        trackingString.ReturnResult = Docflow.OfficialDocumentTracking.ReturnResult.Signed;
        
        // Логика по прекращению согласования (контроль возврата и т.д.), уведомление ответственному.
        if (isAgent)
          this.SendDocumentReplyNotice(box, trackingString, signedVersion.Number, sentVersionIsChanged, true, signatoryInfo, false, exchangeService.Name, string.Empty);
      }
      
      document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Signed;
      info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Signed;
      
      if (notSigned)
        document.History.Write(historyOperation, detailedOperation, historyComment, signedVersion.Number);

      // Добавление в очередь генерации PublicBody после диалогового подписания происходит в методах SendBuyerTitle
      // и SendAnswerToNonformalizedDocument, здесь только синхронная агентская генерация.
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      if (isAgent)
      {
        if (accountingDocument != null && accountingDocument.IsFormalized == true)
          Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedDocument(accountingDocument, signedVersion.Id, accountingDocument.ExchangeState);
        else
          Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForNonformalizedDocument(document, signedVersion.Id);
      }
      document.Save();
      info.Save();
    }
    
    /// <summary>
    /// Создать и отправить задачу на обработку подписанного обеими сторонами документа.
    /// </summary>
    /// <param name="message">Сообщение сервиса обмена.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    protected virtual void SendSignedDocumentProcessingTask(IMessage message, IExchangeDocumentInfo info, ICounterparty sender, IBoxBase box)
    {
      var task = this.CreateExchangeTask(new List<IExchangeDocumentInfo>() { info }, sender, info.Box, info.MessageDate.Value, null);

      var text = string.Empty;
      text = ExchangeDocumentProcessingTasks.Resources.TaskActiveText;
      if (task.ActiveText != string.Empty)
      {
        text += Environment.NewLine;
        text += Environment.NewLine;
        text += task.ActiveText;
      }

      task.ActiveText = text;

      var hyperlink = Hyperlinks.Get(info.Document);
      task.ActiveText += Environment.NewLine;
      task.ActiveText += Environment.NewLine;
      task.ActiveText += Resources.DocumentIsSignedByBothSidesFormat(hyperlink);

      task.ActiveText += this.ProcessBoundedDocuments(message.PrimaryDocuments, new List<Docflow.IOfficialDocument>() { info.Document }, false, box);

      this.GrantAccessRightsForUpperBoxResponsibles(info.Document, box);
      task.DontNeedSigning.All.Add(info.Document);

      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Обработка подписи по документу, который еще не был загружен.
    /// </summary>
    /// <param name="message">Сообщение с подписью.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="document">Документ из сервиса обмена.</param>
    /// <param name="client">Клиент DCX.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="isIncoming">True, если сообщение входящее, иначе - false.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Информация о документе с обновлением.</returns>
    protected virtual IExchangeDocumentInfo LoadDocumentWithSecondSign(IMessage message, IExchangeDocumentInfo info, IDocument document, DcxClient client,
                                                                       ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      this.LogDebugFormat(info, "Execute LoadDocumentWithSecondSign.");
      var firstMessage = client.GetMessage(info.ServiceMessageId);
      var firstDocument = firstMessage.PrimaryDocuments.SingleOrDefault(d => d.ServiceEntityId == info.ServiceDocumentId);

      var serviceCounterpartyId = string.Empty;
      if (!isIncoming)
        serviceCounterpartyId = message.Sender.Organization.OrganizationId;
      else
        serviceCounterpartyId = message.Receiver.Organization.OrganizationId;

      info.Document = this.GetOrCreateNewExchangeDocument(firstDocument, sender, serviceCounterpartyId, !isIncoming, firstMessage.TimeStamp, box);

      // Переполучаем инфошку, она меняется.
      info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);

      // Копипаста подписания при получении нового сообщения.
      var signature = firstMessage.Signatures.FirstOrDefault(x => x.DocumentId == firstDocument.ServiceEntityId);
      if (signature != null)
      {
        var x509certificate = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature.Content);
        var signInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(x509certificate.SubjectInfo);
        this.SignDocument(info, signature, info.Document.LastVersion, signInfo, firstMessage.TimeStamp);
      }

      // Отправить задачу на обработку подписанного обеими сторонами документа.
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box) && Exchange.PublicFunctions.ExchangeDocumentInfo.NeedReceiveTask(info);
      if (needReceive)
      {
        this.SendSignedDocumentProcessingTask(message, info, sender, box);
      }

      return info;
    }

    /// <summary>
    /// Подписать документ.
    /// </summary>
    /// <param name="info">Информация о подписываемой версии.</param>
    /// <param name="sign">Подпись.</param>
    /// <param name="version">Версия, которую подписывают.</param>
    /// <param name="signatoryName">Имя подписывающего.</param>
    /// <param name="date">Дата подписи, на случай если её нет в подписи и не получается выполнить импорт легально.</param>
    /// <remarks>В случае если подпись без даты, которая в Sungero обязательна, будет выполнена попытка проставить подпись
    /// хоть как-нибудь. Подпись после этого будет отображаться как невалидная, но она хотя бы будет.
    /// Валидная подпись останется только в сервисе обмена.</remarks>
    protected virtual void SignDocument(IExchangeDocumentInfo info, Signature sign,
                                        IElectronicDocumentVersions version, string signatoryName, DateTime date)
    {
      this.LogDebugFormat(info, "Execute SignDocument.");
      var entity = (Domain.Shared.IExtendedEntity)info.Document;
      entity.Params[ExchangeCore.PublicConstants.BoxBase.JobRunned] = true;

      try
      {
        var unsignedAdditionalInfo = Docflow.PublicFunctions.Module.FormatUnsignedAttribute(Docflow.PublicConstants.Module.UnsignedAdditionalInfoKeyFPoA, sign.FormalizedPoAUnifiedRegNumber);
        Signatures.Import(info.Document, SignatureType.Approval, signatoryName, sign.Content, date, unsignedAdditionalInfo, version);
      }
      catch (Exception ex)
      {
        this.LogDebugFormat(info, "Can't import signature on document, error: {0}", ex);
      }
      
      entity.Params[ExchangeCore.PublicConstants.BoxBase.JobRunned] = false;
      
      var fromCounterparty = GetClient(info.RootBox).OurSubscriber.BoxId != sign.SignerBoxId;
      
      var signature = Signatures.Get(version)
        .Where(s => s.IsExternal == true && s.SignCertificate != null)
        .OrderByDescending(x => x.Id)
        .FirstOrDefault();
      
      if (signature != null)
      {
        if (info.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Incoming ? fromCounterparty : !fromCounterparty)
          info.SenderSignId = signature.Id;
        else
          info.ReceiverSignId = signature.Id;
        info.Save();
      }
      else
      {
        this.LogDebugFormat(info, "Can't find signature on document with version id: '{0}'", version.Id);
      }
    }

    #endregion

    #region Обработка отказа в подписании

    /// <summary>
    /// Обработать документы с отказом в подписании.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="historyOperation">Операция истории - мы отказали или нам отказали.</param>
    /// <param name="historyComment">Комментарий - кто и кому.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessReject(IMessage message, IMessageQueueItem queueItem, bool isIncoming, IBoxBase box,
                                         Enumeration historyOperation, string historyComment)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessReject.");
      foreach (var reglamentDoc in message.ReglamentDocuments.Where(x => x.DocumentType == ReglamentDocumentType.AmendmentRequest ||
                                                                    x.DocumentType == ReglamentDocumentType.InvoiceAmendmentRequest ||
                                                                    x.DocumentType == ReglamentDocumentType.Rejection))
      {
        var primaryDocument = message.PrimaryDocuments.FirstOrDefault(x => x.ServiceEntityId == reglamentDoc.ParentServiceEntityId);
        
        if (primaryDocument == null)
          continue;
        
        var doc = Docflow.OfficialDocuments.Null;

        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, primaryDocument.ServiceEntityId);
        
        if (exchangeDocumentInfo != null)
          doc = exchangeDocumentInfo.Document;
        
        this.LogDebugFormat(message, reglamentDoc, "Processing the invoice amendment request (or rejection).");
        // Уведомление об уточнении.
        if (reglamentDoc.DocumentType == ReglamentDocumentType.InvoiceAmendmentRequest &&
            exchangeDocumentInfo != null && !exchangeDocumentInfo.ServiceDocuments.Any(d => d.DocumentId == reglamentDoc.ServiceEntityId))
        {
          this.LogDebugFormat(exchangeDocumentInfo, "Saving invoice amendment request.");
          this.SaveRejectToDocumentInfo(message, exchangeDocumentInfo, reglamentDoc,
                                        isIncoming, Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReject);
        }
        
        // Отказ в подписании.
        if ((reglamentDoc.DocumentType == ReglamentDocumentType.AmendmentRequest || reglamentDoc.DocumentType == ReglamentDocumentType.Rejection) &&
            exchangeDocumentInfo != null && !exchangeDocumentInfo.ServiceDocuments.Any(d => d.DocumentId == reglamentDoc.ServiceEntityId))
        {
          this.LogDebugFormat(exchangeDocumentInfo, "Saving rejection.");
          this.SaveRejectToDocumentInfo(message, exchangeDocumentInfo, reglamentDoc,
                                        isIncoming, Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Reject);
        }
        
        // Пропускаем документы, которые были отправлены нами через личный кабинет сервиса обмена.
        // Или по которым был отправлен автоматический отказ.
        if (doc == null || exchangeDocumentInfo.OutgoingStatus == Exchange.ExchangeDocumentInfo.OutgoingStatus.Rejected)
        {
          this.LogDebugFormat(exchangeDocumentInfo, "Document not found or received rejection for document.");
          // Инфошка без документа - признак того, что ждали подписи. Больше она не нужна.
          if (exchangeDocumentInfo != null && doc == null)
          {
            ExchangeDocumentInfos.Delete(exchangeDocumentInfo);
            this.LogDebugFormat(exchangeDocumentInfo, "Document info deleted.");
          }
          
          return true;
        }

        if (exchangeDocumentInfo == null &&
            this.CanProcessMessageLater(message, queueItem, ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box), reglamentDoc.RootServiceEntityId))
        {
          this.LogDebugFormat(message, reglamentDoc, "Document info not found for received invoice amendment request (or rejection): RootServiceEntityId: '{0}'.", reglamentDoc.RootServiceEntityId);
          return false;
        }
        
        var signature = message.Signatures.Where(x => x.DocumentId.Equals(reglamentDoc.ServiceEntityId, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        if (reglamentDoc.DocumentType == ReglamentDocumentType.InvoiceAmendmentRequest)
          this.ProcessSharedInvoiceReject(exchangeDocumentInfo, doc, isIncoming, exchangeDocumentInfo.Box, signature.Content, historyOperation, historyComment, primaryDocument.Comment, primaryDocument.Comment, true);
        else
          this.ProcessSharedReject(exchangeDocumentInfo, doc, isIncoming, exchangeDocumentInfo.Box, signature.Content, historyOperation, historyComment, primaryDocument.Comment, primaryDocument.Comment, true);
      }

      return true;
    }

    /// <summary>
    /// Общий для агентов и UI код обработки "уведомления об уточнении" при подписании.
    /// </summary>
    /// <param name="info">Информация о документе в сервисе обмена.</param>
    /// <param name="document">Документ.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="signature">Подпись для уведомлений ответственного.</param>
    /// <param name="historyOperation">Операция истории - отправка отказа или пришедший от КА отказ.</param>
    /// <param name="historyComment">Комментарий истории, обычно перечисляются участники операции.</param>
    /// <param name="serviceComment">Комментарий, пришедший из сервиса. Для уведомлений ответственного.</param>
    /// <param name="rejectNotice">Причина отказа.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. False используется для вызова из UI.</param>
    protected virtual void ProcessSharedInvoiceReject(IExchangeDocumentInfo info, IOfficialDocument document,
                                                      bool isIncoming, IBoxBase box, byte[] signature,
                                                      Enumeration historyOperation, string historyComment, string serviceComment,
                                                      string rejectNotice, bool isAgent)
    {
      this.LogDebugFormat(info, "Execute ProcessSharedInvoiceReject.");
      var isRejected = info.InvoiceState == Exchange.ExchangeDocumentInfo.InvoiceState.Rejected;
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
      if (!isIncoming && !isRejected && isAgent && needReceive)
        this.CreateNoticeAfterOurSigning(document, box, false, true, serviceComment);
      
      if (!isRejected)
        info.InvoiceState = Exchange.ExchangeDocumentInfo.InvoiceState.Rejected;
      
      info.Save();
      
      // TODO Zamerov: Карточка не обновляется 46758.
      // Помечаем свойство изменившимся, чтобы отработало перестроение местонахождения при сохранении.
      document.LocationState = document.LocationState;
      
      var sentVersion = document.Versions.FirstOrDefault(x => x.Id == info.VersionId);
      if (isIncoming && isAgent)
      {
        var externalApprovalInTracking = document.Tracking.Where(x => !x.ReturnResult.HasValue && x.ExternalLinkId == info.Id
                                                                 && (x.Action == Docflow.OfficialDocumentTracking.Action.Sending || x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement));
        var x509certificate = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature);
        var signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(x509certificate.SubjectInfo);
        foreach (var trackingString in externalApprovalInTracking)
          this.SendDocumentReplyNotice(box, trackingString, sentVersion.Number, false, false, signatoryInfo, true, ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name, serviceComment);
      }
      
      if (!string.IsNullOrEmpty(rejectNotice) && !isRejected)
        document.Note += string.IsNullOrEmpty(document.Note) ? Resources.RejectInvoiceNoticeFormat(rejectNotice) : Environment.NewLine + Resources.RejectInvoiceNoticeFormat(rejectNotice);
      
      var maxLength = document.Info.Properties.Note.Length;
      if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > maxLength)
        document.Note = CutText(document.Note, maxLength);
      
      if (!isRejected)
      {
        var detailedOperation = new Enumeration(Constants.Module.Exchange.DetailedInvoiceReject);
        document.History.Write(historyOperation, detailedOperation, historyComment, sentVersion.Number);
      }
      
      document.Save();
    }

    /// <summary>
    /// Общий для агентов и UI код обработки "отказа" при подписании.
    /// </summary>
    /// <param name="info">Информация о документе в сервисе обмена.</param>
    /// <param name="document">Документ.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="signature">Подпись для уведомлений ответственного.</param>
    /// <param name="historyOperation">Операция истории - отправка отказа или пришедший от КА отказ.</param>
    /// <param name="historyComment">Комментарий истории, обычно перечисляются участники операции.</param>
    /// <param name="serviceComment">Комментарий, пришедший из сервиса. Для уведомлений ответственного.</param>
    /// <param name="rejectNotice">Причина отказа.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. False используется для вызова из UI.</param>
    protected virtual void ProcessSharedReject(IExchangeDocumentInfo info, IOfficialDocument document, bool isIncoming,
                                               IBoxBase box, byte[] signature,
                                               Enumeration historyOperation, string historyComment,
                                               string serviceComment, string rejectNotice, bool isAgent)
    {
      this.LogDebugFormat(info, "Execute ProcessSharedReject.");
      // Признак того, а можно ли "отказать" по документу в текущей его версии. Если нет - то обновляем только инфошку, документ не трогаем.
      var canSendAnswer = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document) == info;
      var isRejected = info.OutgoingStatus == Exchange.ExchangeDocumentInfo.OutgoingStatus.Rejected;
      var needReceive = ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box);
      var responsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, info.Counterparty, new List<IExchangeDocumentInfo>() { info });
      if (!isIncoming && !isRejected)
      {
        if (isAgent && needReceive)
          this.CreateNoticeAfterOurSigning(document, box, false, false, serviceComment);
        
        if (info != null)
          info.OutgoingStatus = Exchange.ExchangeDocumentInfo.OutgoingStatus.Rejected;
        
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
        {
          var tracking = document.Tracking.AddNew();
          tracking.Action = Docflow.OfficialDocumentTracking.Action.Sending;
          tracking.DeliveredTo = Company.Employees.Current ?? responsible;
          tracking.IsOriginal = true;
          tracking.ReturnDeadline = null;
          tracking.Note = Resources.SendRejectToCounterpartyFormat(ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name);
          tracking.ExternalLinkId = info.Id;
        }
      }
      
      // Отказ по счёт-фактуре в СО связан с отправкой уточнения и поэтому не должен делать её устаревшей.
      var isTaxInvoice = FinancialArchive.IncomingTaxInvoices.Is(document) || FinancialArchive.OutgoingTaxInvoices.Is(document);
      if (canSendAnswer && !isTaxInvoice)
        Docflow.PublicFunctions.OfficialDocument.SetObsolete(document, false);
      
      var detailedOperation = new Enumeration(Constants.Module.Exchange.DetailedReject);
      
      this.SetRejectStates(document, info, isIncoming, canSendAnswer, isTaxInvoice);
      info.Save();
      
      var externalApprovalInTracking = document.Tracking.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement
                                                               && !x.ReturnResult.HasValue && x.ExternalLinkId == info.Id);
      
      var x509Certificate = Docflow.PublicFunctions.Module.GetSignatureCertificateInfo(signature);
      var signatoryInfo = Docflow.PublicFunctions.Module.GetCertificateSignatoryName(x509Certificate.SubjectInfo);
      
      var rejectedVersion = info.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected
        ? document.LastVersion
        : document.Versions.FirstOrDefault(x => x.Id == info.VersionId);
      
      foreach (var trackingString in externalApprovalInTracking)
      {
        trackingString.ReturnResult = Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
        
        // Логика по прекращению согласования (контроль возврата и т.д.), уведомление ответственному.
        if (isAgent)
          this.SendDocumentReplyNotice(box, trackingString, rejectedVersion.Number, false, false, signatoryInfo, false, ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name, serviceComment);
      }
      
      // Генерация PDF.
      this.GeneratePublicBody(document, rejectedVersion, isAgent);

      if (!string.IsNullOrEmpty(rejectNotice) && !isRejected)
        document.Note += string.IsNullOrEmpty(document.Note) ? Resources.RejectNoticeFormat(rejectNotice) : Environment.NewLine + Resources.RejectNoticeFormat(rejectNotice);
      
      var maxLength = document.Info.Properties.Note.Length;
      if (!string.IsNullOrEmpty(document.Note) && document.Note.Length > maxLength)
        document.Note = CutText(document.Note, maxLength);
      
      if (!isRejected)
        document.History.Write(historyOperation, detailedOperation, historyComment, rejectedVersion.Number);
      
      document.Save();
    }

    /// <summary>
    /// Установить статусы документа при отказе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="isIncoming">True, если сообщение входящее, иначе - false.</param>
    /// <param name="canSendAnswer">Признак смены статуса отказа по документу.</param>
    /// <param name="isTaxInvoice">True, если документ счет-фактура, иначе - false.</param>
    protected virtual void SetRejectStates(IOfficialDocument document, IExchangeDocumentInfo info, bool isIncoming, bool canSendAnswer, bool isTaxInvoice)
    {
      if (isIncoming)
      {
        if (canSendAnswer)
        {
          document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Rejected;
          document.ExternalApprovalState = Docflow.OfficialDocument.ExternalApprovalState.Unsigned;
        }

        info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Rejected;
      }
      else
      {
        if (info.OutgoingStatus == Exchange.ExchangeDocumentInfo.OutgoingStatus.Rejected)
        {
          if (canSendAnswer)
            document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Rejected;

          info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Rejected;
        }
        else
        {
          if (canSendAnswer)
            document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Obsolete;

          info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Obsolete;
        }

        // Отказ по счёт-фактуре в СО не должен прерывать внутреннее согласование.
        if (canSendAnswer && !isTaxInvoice)
          document.InternalApprovalState = Docflow.OfficialDocument.InternalApprovalState.Aborted;
      }
    }
    
    /// <summary>
    /// Добавить служебный документ с отказом в подписании.
    /// </summary>
    /// <param name="message">Сообщение с отказом в подписании.</param>
    /// <param name="info">Информация о документе.</param>
    /// <param name="document">Служебный документ.</param>
    /// <param name="isIncoming">True, если сообщение входящее, иначе - false.</param>
    /// <param name="serviceDocumentType">Тип служебного документа.</param>
    protected virtual void SaveRejectToDocumentInfo(IMessage message, IExchangeDocumentInfo info, IReglamentDocument document,
                                                    bool isIncoming, Enumeration serviceDocumentType)
    {
      this.LogDebugFormat(info, "Execute SaveRejectToDocumentInfo.");
      var serviceDoc = info.ServiceDocuments.AddNew();
      serviceDoc.DocumentId = document.ServiceEntityId;
      serviceDoc.ParentDocumentId = document.ParentServiceEntityId;
      serviceDoc.CounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
      serviceDoc.DocumentType = serviceDocumentType;
      serviceDoc.Date = ToTenantTime(document.DateTime ?? message.TimeStamp);
      serviceDoc.Body = document.Content;
      var sign = message.Signatures.Single(s => s.DocumentId == serviceDoc.DocumentId);
      serviceDoc.Sign = sign.Content;
      serviceDoc.FormalizedPoAUnifiedRegNo = sign.FormalizedPoAUnifiedRegNumber;
      info.Save();
    }
    
    /// <summary>
    /// Сгенерировать PublicBody документа.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    [Public, Remote]
    public virtual void GeneratePublicBody(int documentId)
    {
      var document = Docflow.OfficialDocuments.Get(documentId);
      if (document != null)
        this.GeneratePublicBodyAsync(document);
    }
    
    /// <summary>
    /// Сгенерировать PublicBody документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sentVersion">Версия документа для генерации PublicBody.</param>
    /// <param name="isAgent">True - генерация синхронная из фонового процесса, иначе - постановка в очередь.</param>
    protected virtual void GeneratePublicBody(IOfficialDocument document, IElectronicDocumentVersions sentVersion, bool isAgent)
    {
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      if (accountingDocument != null && accountingDocument.IsFormalized == true)
      {
        foreach (var accVersion in accountingDocument.Versions)
        {
          // Генерация PDF синхронная, если вызвана из агента и наоборот.
          if (isAgent)
            Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedDocument(accountingDocument, accVersion.Id,
                                                                                          accountingDocument.ExchangeState);
          else
          {
            Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(accountingDocument, accVersion.Id);
            Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(accountingDocument, accVersion.Id, accountingDocument.ExchangeState);
          }
        }
      }
      else
      {
        // Генерация PDF синхронная, если вызвана из агента и наоборот.
        if (isAgent)
          Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForNonformalizedDocument(document, sentVersion.Id);
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(document, sentVersion.Id);
          Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(document, sentVersion.Id, document.ExchangeState);
        }
      }
    }
    
    /// <summary>
    /// Асинхронно сгенерировать PublicBody последней версии документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    protected virtual void GeneratePublicBodyAsync(IOfficialDocument document)
    {
      var version = document.LastVersion;
      if (Exchange.ExchangeDocumentInfos.GetAll().Any(x => Equals(x.Document, document) && x.VersionId == version.Id) ||
          (AccountingDocumentBases.Is(document) && AccountingDocumentBases.As(document).IsFormalized == true))
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(document, version.Id);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(document, version.Id, document.ExchangeState);
      }
    }

    #endregion

    #region Уведомления по отказу и подписанию

    /// <summary>
    /// Создать уведомление, если документ был подписан или отправлен отказ нами из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="signed">Признак подписания. False - если в подписании было отказано.</param>
    /// <param name="isInvoiceAmendmentRequest">Отправлено уточнение по СФ или УПД.</param>
    /// <param name="reason">Комментарий.</param>
    private void CreateNoticeAfterOurSigning(IOfficialDocument document, IBoxBase box, bool signed, bool isInvoiceAmendmentRequest, string reason)
    {
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var info = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(document);
      var boxResponsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, info.Counterparty, new List<IExchangeDocumentInfo>() { info });
      
      var documentProcessingTask = ExchangeDocumentProcessingTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid))
        .FirstOrDefault();
      var documentProcessingPerformers =
        Sungero.Workflow.Assignments.GetAll()
        .Where(a => Equals(a.Task, documentProcessingTask))
        .Select(a => a.Performer);
      
      var performers = new List<IUser>();
      performers.AddRange(documentProcessingPerformers);
      performers.Add(boxResponsible);
      if (Contracts.ContractualDocuments.Is(document))
        performers.Add(Contracts.ContractualDocuments.As(document).ResponsibleEmployee);
      
      var task = Workflow.SimpleTasks.Null;
      if (documentProcessingTask != null)
        task = Workflow.SimpleTasks.CreateAsSubtask(documentProcessingTask);
      else
        task = Workflow.SimpleTasks.Create();
      
      task.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      task.NeedsReview = false;
      
      // При создании подзадачи в нее копируются все вложения.
      var docs = task.AllAttachments.Where(d => !Equals(d, document)).ToList();
      foreach (var doc in docs)
        task.Attachments.Remove(doc);
      
      this.GrantAccessRightsForUpperBoxResponsibles(document, box);
      if (!task.AllAttachments.Where(d => Equals(d, document)).Any())
        task.Attachments.Add(document);
      
      performers = performers.Where(x => x != null).Distinct().ToList();
      foreach (var performer in performers)
      {
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = task.AssignmentType;
        step.Performer = performer;
        step.Deadline = null;
      }
      
      var link = Hyperlinks.Get(document);
      if (signed)
      {
        task.Subject = Resources.SignNoticeSubjectObsoleteFormat(document.Name);
        task.Subject = Sungero.Exchange.Resources.DocumentSignedThreadSubject;
        task.ActiveText = Resources.SignNoticeActiveTextObsoleteFormat(link);
      }
      else
      {
        if (isInvoiceAmendmentRequest)
        {
          task.ThreadSubject = Resources.AmendmentNoticeSubjectObsolete;
          task.Subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, task.ThreadSubject, document.Name);
          task.ActiveText = Resources.AmendmentNoticeActiveTextObsoleteFormat(link);
        }
        else
        {
          task.ThreadSubject = Resources.RejectNoticeSubjectObsolete;
          task.Subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, task.ThreadSubject, document.Name);
          task.ActiveText = Resources.RejectNoticeActiveTextObsoleteFormat(link);
        }

        if (!string.IsNullOrEmpty(reason))
          task.ActiveText += Environment.NewLine + Resources.DocumentCommentFormat(reason);
      }

      task.Subject = CutText(task.Subject, task.Info.Properties.Subject.Length);
      
      task.Save();
      task.Start();
    }

    /// <summary>
    /// Создать уведомление о получении ответа от контрагента.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="trackingLine">Строка выдачи.</param>
    /// <param name="signed">Признак подписания. True - если документ подписан контрагентом, иначе - false.</param>
    /// <param name="obsolete">Признак, что документ был отозван нами в сервисе обмена.</param>
    /// <param name="isInvoiceAmendmentRequest">Признак, что отправлено уточнение по СФ или УПД.</param>
    /// <param name="performers">Список пользователей, кому будет отправлено уведомление.</param>
    /// <param name="activeText">Текст уведомления.</param>
    protected virtual void CreateDocumentReplyNotice(IBoxBase box, IOfficialDocumentTracking trackingLine, bool signed, bool obsolete,
                                                     bool isInvoiceAmendmentRequest, List<IUser> performers, string activeText)
    {
      var task = Workflow.SimpleTasks.Null;
      var document = trackingLine.OfficialDocument;
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var parentTask = ExchangeDocumentProcessingTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid))
        .FirstOrDefault();
      
      if (trackingLine.ReturnTask != null)
      {
        task = Workflow.SimpleTasks.CreateAsSubtask(trackingLine.ReturnTask);
        performers.Add(trackingLine.ReturnTask.Author);
      }
      else if (parentTask != null)
      {
        // Подзадача к заданию на обработку, при отзыве формализованного документа в сервисе обмена нашей НОР.
        task = Workflow.SimpleTasks.CreateAsSubtask(parentTask);
      }
      else
      {
        task = Workflow.SimpleTasks.Create();
      }
      
      task.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      task.NeedsReview = false;
      
      // При создании подзадачи в нее копируются все вложения.
      var docs = task.AllAttachments.Where(d => !Equals(d, trackingLine.OfficialDocument)).ToList();
      foreach (var doc in docs)
        task.Attachments.Remove(doc);
      
      this.GrantAccessRightsForUpperBoxResponsibles(trackingLine.OfficialDocument, box);
      if (!task.AllAttachments.Where(d => Equals(d, trackingLine.OfficialDocument)).Any())
        task.Attachments.Add(trackingLine.OfficialDocument);
      
      performers.Add(trackingLine.DeliveredTo);
      performers = performers.Distinct().ToList();
      foreach (var performer in performers)
      {
        var step = task.RouteSteps.AddNew();
        step.Performer = performer;
        step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
        step.Deadline = null;
      }

      if (obsolete)
        task.ThreadSubject = string.Format(Resources.RevocationNoticeOurSubjectTerminate);
      else if (isInvoiceAmendmentRequest)
        task.ThreadSubject = string.Format(Resources.AmendedDocumentSubject);
      else if (signed)
        task.ThreadSubject = string.Format(Resources.AssignDocumentSubject);
      else
        task.ThreadSubject = string.Format(Resources.RejectDocumentSubject);
      
      task.Subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, task.ThreadSubject, trackingLine.OfficialDocument.Name);
      task.Subject = CutText(task.Subject, task.Info.Properties.Subject.Length);
      
      task.ActiveText = activeText;
      
      task.Save();
      task.Start();
    }

    /// <summary>
    /// Отправить уведомление ответственному о поступлении ответа от контрагента.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="trackingString">Строка выдачи.</param>
    /// <param name="versionNumber">Версия документа.</param>
    /// <param name="versionIsChanged">Признак того, что версия была изменена.</param>
    /// <param name="signed">Признак подписания. True - если документ подписан контрагентом, иначе - false.</param>
    /// <param name="signatoryInfo">Информация о контрагенте.</param>
    /// <param name="isInvoiceAmendmentRequest">Отправлено уточнение по СФ или УПД.</param>
    /// <param name="serviceName">Наименование сервиса обмена.</param>
    /// <param name="comment">Комментарии контрагента.</param>
    protected virtual void SendDocumentReplyNotice(IBoxBase box, IOfficialDocumentTracking trackingString, int? versionNumber,
                                                   bool versionIsChanged, bool signed, string signatoryInfo,
                                                   bool isInvoiceAmendmentRequest, string serviceName, string comment)
    {
      var performers = new List<IUser>();
      var activeText = string.Empty;
      var docHyperlink = Hyperlinks.Get(trackingString.OfficialDocument);
      var info = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(trackingString.OfficialDocument);
      var boxResponsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, info.Counterparty, new List<IExchangeDocumentInfo>() { info });
      
      activeText = FillDocumentReplyNoticeTexts(versionNumber, versionIsChanged, signed, signatoryInfo, isInvoiceAmendmentRequest, activeText, comment, serviceName, docHyperlink);

      performers.Add(boxResponsible);
      
      if (trackingString.ReturnTask != null)
      {
        // Агент автоматически выполняет задание на контроль возврата и процесс идет дальше.
        var returnAssignments = Docflow.ApprovalCheckReturnAssignments.GetAll().Where(x => Equals(x.Task, trackingString.ReturnTask) && x.Status == Workflow.Assignment.Status.InProcess).ToList();
        
        if (returnAssignments.Any())
        {
          var isMainDocument = Docflow.ApprovalTasks.As(trackingString.ReturnTask).DocumentGroup.OfficialDocuments.Contains(trackingString.OfficialDocument);
          
          if (isMainDocument)
          {
            // Разделено установка признака AutoReturned и выполнение заданий, т.к. при большом количестве исполнителей схема успевает начать свою рассылку.
            foreach (var assignment in returnAssignments)
            {
              assignment.ActiveText = activeText;
              assignment.AutoReturned = true;
              assignment.Save();
            }
          }
          
          foreach (var assignment in returnAssignments)
          {
            if (isMainDocument)
            {
              var completeResult = signed ? Docflow.ApprovalCheckReturnAssignment.Result.Signed : Docflow.ApprovalCheckReturnAssignment.Result.NotSigned;
              assignment.Complete(completeResult);
            }
            
            performers.Add(assignment.Performer);
          }
          
          // Если не подписано, уведомляем всех кто согласовывал и подписывал документ (логика из задачи).
          if (!signed)
            performers.AddRange(Docflow.PublicFunctions.ApprovalTask.GetAllApproversAndSignatories(Docflow.ApprovalTasks.As(trackingString.ReturnTask)));
          
          performers = performers.Distinct().ToList();
        }
      }
      
      if (ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box))
        this.CreateDocumentReplyNotice(box, trackingString, signed, false, isInvoiceAmendmentRequest, performers, activeText);
    }

    private static string FillDocumentReplyNoticeTexts(int? versionNumber, bool versionIsChanged, bool signed, string signatoryInfo,
                                                       bool isInvoiceAmendmentRequest, string activeText, string comment,
                                                       string serviceName, string documentHyperlink)
    {
      if (signed)
      {
        activeText = string.Format(Resources.AssignDocumentVersion, documentHyperlink, versionNumber, serviceName) +
          Environment.NewLine +
          string.Format(Resources.AssignDocumentBy, signatoryInfo);

        if (versionIsChanged)
          activeText += Environment.NewLine + Resources.DocumentHasChangedAfterSendToCounterpartyFormat(versionNumber);
      }

      if (isInvoiceAmendmentRequest)
      {
        activeText = string.Format(Resources.AmendedDocumentVersion, documentHyperlink, versionNumber.Value);
        activeText += Environment.NewLine;
        activeText += string.Format(Resources.AmendedDocumentBy, signatoryInfo);

        if (!string.IsNullOrEmpty(comment))
        {
          activeText += Environment.NewLine;
          activeText += string.Format(Resources.RejectDocumentComment, comment);
        }
      }

      if (!signed && !isInvoiceAmendmentRequest)
      {
        activeText = string.Format(Resources.RejectDocumentVersion, documentHyperlink, versionNumber.Value);
        activeText += Environment.NewLine;
        activeText += string.Format(Resources.RejectDocumentBy, signatoryInfo);

        if (!string.IsNullOrEmpty(comment))
        {
          activeText += Environment.NewLine;
          activeText += string.Format(Resources.RejectDocumentComment, comment);
        }
      }

      return activeText;
    }
    
    #endregion
    
    #region Обработка аннулирования и отзыва

    /// <summary>
    /// Обработать сообщение об аннулировании или отзыве документа.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItems">Все элементы очереди.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessAnnulmentOrCancellation(IMessage message, List<IMessageQueueItem> queueItems,
                                                          ICounterparty sender, bool isIncoming, IBoxBase box)
    {
      this.LogDebugFormat(message, box, "Execute ProcessAnnulmentOrCancellation.");
      var result = false;
      var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
      var fromCounterparty = isIncoming && !message.IsReply || !isIncoming && message.IsReply;

      foreach (var document in message.PrimaryDocuments.Where(x => x.DocumentType == NpoComputer.DCX.Common.DocumentType.RevocationOffer).ToList())
      {
        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ParentServiceEntityId);
        
        // Если нет инфошки и нет документа в очереди - считаем обработанным.
        // Если для исходящего сообщения есть инфо, но нет документа (случай отправленного от нас неформализованного документа с требованием подписи) - считаем обработанным.
        // Ну или инфошка почему то уже с признаком обработанности - тогда тоже считаем обработанным и пропускаем.
        var isNonformalizedOutgoingDocumentCancellation = !isIncoming && exchangeDocumentInfo != null && exchangeDocumentInfo.Document == null;
        if (exchangeDocumentInfo == null ||
            exchangeDocumentInfo.RevocationStatus == Exchange.ExchangeDocumentInfo.RevocationStatus.Revoked ||
            isNonformalizedOutgoingDocumentCancellation)
        {
          if (queueItems.Any(q => q.Documents.Any(d => d.ExternalId == document.ParentServiceEntityId &&
                                                  d.Type == ExchangeCore.MessageQueueItemDocuments.Type.Primary)))
            continue;

          result = true;
        }
        
        // Не шлём уведомления по уже аннулированному или еще не полученному документу.
        if (exchangeDocumentInfo != null &&
            exchangeDocumentInfo.Document != null && exchangeDocumentInfo.RevocationStatus != Exchange.ExchangeDocumentInfo.RevocationStatus.Revoked)
        {
          var version = exchangeDocumentInfo.Document.Versions.SingleOrDefault(v => v.Id == exchangeDocumentInfo.VersionId);
          var versionNumber = version == null ? null : version.Number;
          var comment = fromCounterparty ? string.Format("{0}|{1}", sender.Name, exchangeService.Name) : exchangeService.Name;
          var isAnnulment = document.SignStatus != NpoComputer.DCX.Common.SignStatus.None;
          
          // Запись служебной информации в инфошку.
          if (!exchangeDocumentInfo.ServiceDocuments.Any(d => d.DocumentId == document.ServiceEntityId))
          {
            var serviceDoc = exchangeDocumentInfo.ServiceDocuments.AddNew();
            serviceDoc.DocumentId = document.ServiceEntityId;
            serviceDoc.ParentDocumentId = document.ParentServiceEntityId;
            serviceDoc.CounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
            serviceDoc.DocumentType = isAnnulment
              ? Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Annulment
              : Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Cancellation;
            serviceDoc.Date = ToTenantTime(document.Date == DateTime.MinValue ? message.TimeStamp : document.Date);
            serviceDoc.Body = document.Content;
            var signature = message.Signatures.Single(s => s.DocumentId == serviceDoc.DocumentId);
            serviceDoc.Sign = signature.Content;
            serviceDoc.FormalizedPoAUnifiedRegNo = signature.FormalizedPoAUnifiedRegNumber;
            exchangeDocumentInfo.Save();
          }

          if (isAnnulment)
          {
            this.ProcessAnnulment(exchangeDocumentInfo, document, isIncoming, fromCounterparty, versionNumber, comment);
          }
          else
          {
            this.ProcessCancellation(exchangeDocumentInfo, document, fromCounterparty, box, versionNumber, comment);
          }
          
          // Штамп об аннулировании или отзыве проставляем на каждую версию.
          var accountingDocument = Docflow.AccountingDocumentBases.As(exchangeDocumentInfo.Document);
          if (accountingDocument != null && accountingDocument.IsFormalized == true)
          {
            this.LogDebugFormat(exchangeDocumentInfo, "Execute GeneratePublicBodyForFormalizedDocument for annulment.");
            foreach (var accountVersion in accountingDocument.Versions)
              Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedDocument(accountingDocument, accountVersion.Id, accountingDocument.ExchangeState);
          }
          else
          {
            this.LogDebugFormat(exchangeDocumentInfo, "Execute GeneratePublicBodyForNonformalizedDocument for annulment.");
            Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForNonformalizedDocument(exchangeDocumentInfo.Document, version.Id);
          }
          result = true;
        }
        
        // Если такой документ уже занесён, то пришла вторая подпись, заносим и её.
        if (exchangeDocumentInfo != null && exchangeDocumentInfo.ServiceDocuments
            .Any(d => d.DocumentId == document.ServiceEntityId && d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Annulment))
        {
          this.LogDebugFormat(exchangeDocumentInfo, "Execute processing answer for annulment.");
          var serviceDoc = exchangeDocumentInfo.ServiceDocuments.Single(d => d.DocumentId == document.ServiceEntityId &&
                                                                        d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Annulment);
          var sign = message.Signatures.SingleOrDefault(s => s.DocumentId == document.ServiceEntityId);
          
          // Если подпись найдена - то это подписание, записываем.
          if (sign != null && !Enumerable.SequenceEqual(serviceDoc.Sign, sign.Content))
          {
            serviceDoc.SecondSign = sign.Content;
            serviceDoc.SecondFormalizedPoAUnifiedRegNo = sign.FormalizedPoAUnifiedRegNumber;
            exchangeDocumentInfo.Save();
          }
          else if (message.ReglamentDocuments.Any(r => (r.DocumentType == ReglamentDocumentType.AmendmentRequest || r.DocumentType == ReglamentDocumentType.Rejection) && r.ParentServiceEntityId == document.ServiceEntityId))
          {
            // Вариант с отказом в аннулировании.
            var annulment = message.ReglamentDocuments.Single(r => (r.DocumentType == ReglamentDocumentType.AmendmentRequest || r.DocumentType == ReglamentDocumentType.Rejection) && r.ParentServiceEntityId == document.ServiceEntityId);
            this.SaveRejectToDocumentInfo(message, exchangeDocumentInfo, annulment,
                                          isIncoming, Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Reject);
          }
        }
      }

      return result;
    }

    /// <summary>
    /// Обработать аннулирование.
    /// </summary>
    /// <param name="info">Информация о документе в сервисе обмена.</param>
    /// <param name="document">Документ.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="fromCounterparty">Признак авторства запроса на аннулирование.</param>
    /// <param name="versionNumber">Номер версии, которую аннулируют.</param>
    /// <param name="comment">Комментарий в истории документа.</param>
    protected virtual void ProcessAnnulment(IExchangeDocumentInfo info, IDocument document, bool isIncoming, bool fromCounterparty,
                                            int? versionNumber, string comment)
    {
      this.LogDebugFormat(info, "Execute ProcessAnnulment.");
      // Признак того, а можно ли "аннулировать" документ в текущей его версии. Если нет - то обновляем только инфошку, документ не трогаем.
      var canSendAnswer = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(info.Document) == info;
      
      if (document.SignStatus != NpoComputer.DCX.Common.SignStatus.None)
      {
        // Аннулирование.
        if (document.SignStatus == NpoComputer.DCX.Common.SignStatus.Signed)
        {
          if (ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(info.Box))
            this.CreateRevocationTask(info, fromCounterparty, document.Comment, true, false);
          if (canSendAnswer)
          {
            Docflow.PublicFunctions.OfficialDocument.SetObsolete(info.Document, true);
            info.Document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Terminated;
            info.Document.ExternalApprovalState = null;
          }
          info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Terminated;
          var operation = new Enumeration(fromCounterparty ? Constants.Module.Exchange.ObsoletedByCounterparty : Constants.Module.Exchange.ObsoleteOur);
          info.Document.History.Write(operation, operation, comment, versionNumber);
          info.Document.Save();
          info.Save();
        }
        
        // Запрос на аннулирование обрабатывается только входящий.
        if (document.SignStatus == NpoComputer.DCX.Common.SignStatus.Waiting && ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(info.Box))
        {
          // Задание на аннулирование, если нет задания в работе.
          if (info.RevocationTask == null || info.RevocationTask.Status != Workflow.Task.Status.InProcess)
          {
            if (isIncoming)
              this.CreateRevocationTask(info, fromCounterparty, document.Comment, true, true);
            else
              this.CreateRequestedAnnulmentNotice(info, document.Comment);
          }
        }
        
        if (document.SignStatus == NpoComputer.DCX.Common.SignStatus.Rejected)
        {
          // Передумали подписывать - откатываем статус подписания.
          info.RevocationStatus = Exchange.ExchangeDocumentInfo.RevocationStatus.None;
          info.Save();
        }
      }
    }

    /// <summary>
    /// Обработать отзыв.
    /// </summary>
    /// <param name="info">Информация о документе в сервисе обмена.</param>
    /// <param name="document">Документ.</param>
    /// <param name="fromCounterparty">Признак авторства отзыва.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="versionNumber">Номер версии, которую отозвали.</param>
    /// <param name="comment">Комментарий в истории документа.</param>
    protected virtual void ProcessCancellation(IExchangeDocumentInfo info, IDocument document, bool fromCounterparty,
                                               IBoxBase box, int? versionNumber, string comment)
    {
      this.LogDebugFormat(info, "Execute ProcessCancellation.");
      // Признак того, а можно ли "отозвать" документ в текущей его версии. Если нет - то обновляем только инфошку, документ не трогаем.
      var canSendAnswer = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(info.Document) == info;
      
      // Изменяем статус документа заранее, чтобы правильно обработать контроль возврата.
      if (canSendAnswer)
      {
        Docflow.PublicFunctions.OfficialDocument.SetObsolete(info.Document, false);
        info.Document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Obsolete;
        info.Document.ExternalApprovalState = null;
        info.Document.Save();
      }
      
      // Отзыв.
      var tracking = info.Document.Tracking.FirstOrDefault(x => x.ExternalLinkId == info.Id);
      if (tracking != null)
      {
        var docHyperlink = Hyperlinks.Get(tracking.OfficialDocument);
        var activeText = Resources.RevocationNoticeOurActiveTextTerminateFormat(docHyperlink).ToString();
        var performers = new List<IUser>();
        
        if (tracking.ReturnTask != null)
        {
          // Агент автоматически выполняет задание на контроль возврата и процесс идет дальше.
          var returnAssignments = Docflow.ApprovalCheckReturnAssignments.GetAll().Where(x => Equals(x.Task, tracking.ReturnTask) && x.Status == Workflow.Assignment.Status.InProcess).ToList();
          
          if (returnAssignments.Any())
          {
            // Разделено установка признака AutoReturned и выполнение заданий, т.к. при большом количестве исполнителей схема успевает начать свою рассылку.
            foreach (var assignment in returnAssignments)
            {
              assignment.ActiveText = Resources.CheckReturnRevocationResult;
              assignment.AutoReturned = true;
              assignment.Save();
            }
            
            foreach (var assignment in returnAssignments)
            {
              var completeResult = Docflow.ApprovalCheckReturnAssignment.Result.NotSigned;
              assignment.Complete(completeResult);
              performers.Add(assignment.Performer);
            }
          }
          
          performers.AddRange(Docflow.PublicFunctions.ApprovalTask.GetAllApproversAndSignatories(Docflow.ApprovalTasks.As(tracking.ReturnTask)));
        }
        
        // Отправить уведомление ответственному за а/я.
        var boxResponsible = ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(box, info.Counterparty,
                                                                                                        new List<IExchangeDocumentInfo>() { info });
        
        performers.Add(boxResponsible);
        performers = performers.Distinct().ToList();
        
        if (!string.IsNullOrEmpty(document.Comment))
        {
          activeText += Environment.NewLine;
          activeText += string.Format(Resources.DocumentComment, document.Comment);
        }
        
        var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box);
        if (ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box))
          this.CreateDocumentReplyNotice(box, tracking, false, true, false, performers, activeText);
        
        tracking.ReturnResult = Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
        tracking.Note = Resources.RevocationTrackingNoteFormat(exchangeService.Name);
      }
      else
      {
        if (ExchangeCore.PublicFunctions.BoxBase.NeedReceiveTask(box))
          this.CreateRevocationTask(info, fromCounterparty, document.Comment, false, false);
      }

      var operation = new Enumeration(fromCounterparty ? Constants.Module.Exchange.TerminatedByCounterparty : Constants.Module.Exchange.TerminateOur);
      info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Obsolete;
      info.Document.History.Write(operation, operation, comment, versionNumber);
      info.Document.Save();
      info.Save();
    }

    /// <summary>
    /// Создать черновик задачи об аннулировании/отзыве документа.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="createAssignments">True, если надо отправить задания, false - уведомления.</param>
    /// <returns>Черновик задачи.</returns>
    protected virtual ISimpleTask CreateRevocationDraftTask(IExchangeDocumentInfo info, bool createAssignments)
    {
      var docGuid = info.Document.GetEntityMetadata().GetOriginal().NameGuid;
      var parentTask = ExchangeDocumentProcessingTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(att => att.AttachmentId == info.Document.Id && att.EntityTypeGuid == docGuid))
        .FirstOrDefault();
      var task = Workflow.SimpleTasks.Null;
      
      if (parentTask != null)
        task = Workflow.SimpleTasks.CreateAsSubtask(parentTask);
      else if (info.RevocationTask != null)
        task = Workflow.SimpleTasks.CreateAsSubtask(info.RevocationTask);
      else
        task = Workflow.SimpleTasks.Create();
      
      var asgType = createAssignments ? Workflow.SimpleTask.AssignmentType.Assignment : Workflow.SimpleTask.AssignmentType.Notice;
      task.AssignmentType = asgType;
      
      // При создании подзадачи в нее копируются все вложения.
      var docs = task.AllAttachments.Where(d => !Equals(d, info.Document)).ToList();
      foreach (var doc in docs)
        task.Attachments.Remove(doc);
      
      this.GrantAccessRightsForUpperBoxResponsibles(info.Document, info.Box);
      if (!task.AllAttachments.Where(d => Equals(d, info.Document)).Any())
        task.Attachments.Add(info.Document);
      
      var performers = this.GetRevocationTaskPerformers(info, parentTask);

      performers = performers.Where(p => p != null).Distinct().ToList();
      
      if (!performers.Any())
        performers.Add(ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(info.Box, info.Counterparty, new List<IExchangeDocumentInfo>() { info }));
      
      foreach (var performer in performers)
      {
        var step = task.RouteSteps.AddNew();
        step.Performer = performer;
        step.AssignmentType = asgType;
        if (!createAssignments)
          step.Deadline = null;
        
        // Задание со сроком в 2 рабочих дня.
        if (createAssignments)
          step.Deadline = Calendar.Now.AddWorkingHours(performer, 16);
      }
      task.NeedsReview = false;
      
      return task;
    }
    
    /// <summary>
    /// Создать и стартовать задачу об аннулировании/отзыве контрагентом.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="fromCounterparty">Аннулирование пришло от контрагента.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    /// <param name="isAnnulment">True - если аннулирован, false - если отозван.</param>
    /// <param name="createAssignments">True, если надо отправить задания, false - уведомления.</param>
    /// <remarks>Еще обновляется статус и ИД задачи на аннулирование в инфошке.</remarks>
    protected virtual void CreateRevocationTask(IExchangeDocumentInfo info, bool fromCounterparty, string reason, bool isAnnulment, bool createAssignments)
    {
      var task = this.CreateRevocationDraftTask(info, createAssignments);

      if (createAssignments)
        this.FillTaskAssignmentText(info, task, reason);
      else if (isAnnulment)
        this.FillTaskAnnulmentNoticeText(info, fromCounterparty, task, reason);
      else
        this.FillTaskCancellationNoticeText(info, fromCounterparty, task, reason);
      
      task.Subject = CutText(task.Subject, task.Info.Properties.Subject.Length);
      task.Save();
      task.Start();

      // Обновить статус документа.
      info.RevocationStatus = createAssignments ?
        Exchange.ExchangeDocumentInfo.RevocationStatus.Waiting :
        Exchange.ExchangeDocumentInfo.RevocationStatus.Revoked;

      // Если было создано задание - запишем задачку, чтобы не пересоздавать их на каждый чих.
      if (createAssignments)
        info.RevocationTask = task;

      info.Save();
    }
    
    /// <summary>
    /// Отправить уведомление об аннулировании документа нашей организацией.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    protected virtual void CreateRequestedAnnulmentNotice(IExchangeDocumentInfo info, string reason)
    {
      var task = this.CreateRevocationDraftTask(info, false);

      this.FillTaskRequestedAnnulmentText(info, task);
      
      this.FillTaskRevocationReason(task, reason);
      task.Subject = CutText(task.Subject, task.Info.Properties.Subject.Length);
      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Заполнить тему и текст задания на обработку аннулирования.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="task">Задача на аннулирование документа.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    protected virtual void FillTaskAssignmentText(IExchangeDocumentInfo info, ISimpleTask task, string reason)
    {
      var link = Hyperlinks.Get(info.Document);
      
      task.Subject = Resources.RevocationNoticeSubjectInProcessFormat(info.Document.Name);
      task.ThreadSubject = Sungero.Exchange.Resources.RevocationTaskThreadSubjectInProcess;
      task.ActiveText = Resources.RevocationNoticeActiveTextInProcessFormat(link);
      this.FillTaskRevocationReason(task, reason);
      
      var service = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box);
      task.ActiveText += Environment.NewLine;
      task.ActiveText += Environment.NewLine;
      task.ActiveText +=
        Resources.RevocationNoticeInProcessCommentFormat(service.Info.Properties.ExchangeProvider.GetLocalizedValue(service.ExchangeProvider),
                                                         service.LogonUrl);
    }
    
    /// <summary>
    /// Заполнить тему и текст уведомления об аннулировании.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="fromCounterparty">Аннулирование пришло от контрагента.</param>
    /// <param name="task">Задача на аннулирование документа.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    protected virtual void FillTaskAnnulmentNoticeText(IExchangeDocumentInfo info, bool fromCounterparty, ISimpleTask task, string reason)
    {
      var link = Hyperlinks.Get(info.Document);
      task.ThreadSubject = fromCounterparty
        ? Resources.RevocationNoticeSubjectObsolete
        : Resources.RevocationNoticeOurSubjectObsolete;
      task.Subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, task.ThreadSubject, info.Document.Name);
      
      task.ActiveText = fromCounterparty
        ? Resources.RevocationNoticeActiveTextObsoleteFormat(link)
        : Resources.RevocationNoticeOurActiveTextObsoleteFormat(link);
      
      this.FillTaskRevocationReason(task, reason);
    }
    
    /// <summary>
    /// Заполнить тему и текст уведомления об отказе.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="fromCounterparty">Аннулирование пришло от контрагента.</param>
    /// <param name="task">Задача на аннулирование документа.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    protected virtual void FillTaskCancellationNoticeText(IExchangeDocumentInfo info, bool fromCounterparty, ISimpleTask task, string reason)
    {
      var link = Hyperlinks.Get(info.Document);
      task.ThreadSubject = fromCounterparty
        ? Resources.RevocationNoticeSubjectTerminate
        : Resources.RevocationNoticeOurSubjectTerminate;
      task.Subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, task.ThreadSubject, info.Document.Name);
      
      task.ActiveText = fromCounterparty
        ? Resources.RevocationNoticeActiveTextTerminateFormat(link)
        : Resources.RevocationNoticeOurActiveTextTerminateFormat(link);
      
      this.FillTaskRevocationReason(task, reason);
    }
    
    /// <summary>
    /// Заполнить тему и текст уведомления о запросе на аннулирование.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <param name="task">Задача на аннулирование документа.</param>
    protected virtual void FillTaskRequestedAnnulmentText(IExchangeDocumentInfo info, ISimpleTask task)
    {
      var link = Hyperlinks.Get(info.Document);
      
      task.Subject = Resources.RequestedAnnulmentNoticeSubjectInProcessFormat(info.Document.Name);
      task.ThreadSubject = Sungero.Exchange.Resources.RevocationTaskThreadSubjectRequestedAnnulment;
      task.ActiveText = Resources.RevocationNoticeActiveTextRequestedAnnulmentFormat(link);
    }
    
    /// <summary>
    /// Заполнить причину аннулирования/отказа.
    /// </summary>
    /// <param name="task">Задача на аннулирование документа.</param>
    /// <param name="reason">Причина аннулирования/отзыва.</param>
    protected virtual void FillTaskRevocationReason(ISimpleTask task, string reason)
    {
      if (!string.IsNullOrWhiteSpace(reason))
      {
        task.ActiveText += Environment.NewLine;
        task.ActiveText += Resources.RevocationNoticeReasonFormat(reason);
      }
    }

    /// <summary>
    /// Получить исполнителей задачи об аннулировании/отзыве контрагентом.
    /// </summary>
    /// <param name="info">Информация о документе.</param>
    /// <param name="parentTask">Основная задача.</param>
    /// <returns>Исполнители.</returns>
    protected virtual List<IRecipient> GetRevocationTaskPerformers(IExchangeDocumentInfo info, IExchangeDocumentProcessingTask parentTask)
    {
      var performers = new List<IRecipient>();
      if (parentTask != null)
      {
        // Ответственный за ящик.
        performers.Add(
          ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(info.Box, info.Counterparty, new List<IExchangeDocumentInfo>() { info }));

        // Исполнители заданий на обработку.
        var assignmentPerformers = ExchangeDocumentProcessingAssignments.GetAll(a => Equals(a.Task, parentTask)).Select(a => a.Performer);
        performers.AddRange(assignmentPerformers);

        // Ответственный за договорной документ.
        var contract = Contracts.ContractualDocuments.As(info.Document);
        if (contract != null && contract.ResponsibleEmployee != null)
          performers.Add(contract.ResponsibleEmployee);
      }
      else
      {
        // Ответственный за возврат из выдачи.
        var tracking = info.Document.Tracking.FirstOrDefault(t => t.ExternalLinkId.ToString() == info.ServiceDocumentId);
        if (tracking != null && tracking.DeliveredTo != null)
          performers.Add(tracking.DeliveredTo);

        // Кто подготовил документ.
        if (info.Document.PreparedBy != null)
          performers.Add(info.Document.PreparedBy);

        // Ответственный за договорной документ (или автор).
        var contract = Contracts.ContractualDocuments.As(info.Document);
        if (contract != null)
        {
          if (contract.ResponsibleEmployee != null)
            performers.Add(contract.ResponsibleEmployee);
          else
            performers.Add(contract.Author);
        }
      }

      return performers;
    }
    
    #endregion

    #region Обработка ИОП

    /// <summary>
    /// Проверить сообщение на наличие подтверждений получения\отправки.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="counterpartyId">Контрагент, от которого получено сообщение. Ожидается сервисный контрагент.</param>
    /// <param name="box">Ящик, через который получено.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessInvoiceConfirmation(IMessage message, IMessageQueueItem queueItem, string counterpartyId, IBusinessUnitBox box)
    {
      this.LogDebugFormat(message, queueItem, box, "Execute ProcessInvoiceConfirmation.");
      foreach (var confirmation in message.ReglamentDocuments.Where(d => d.DocumentType == ReglamentDocumentType.InvoiceConfirmation))
      {
        var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, confirmation.RootServiceEntityId);
        if (info != null && !info.ServiceDocuments.Any(d => d.DocumentId == confirmation.ServiceEntityId))
        {
          var document = info.ServiceDocuments.AddNew();
          if (confirmation.ParentServiceEntityId == info.ServiceDocumentId)
            document.DocumentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IConfirmation;
          else
          {
            var parentServiceDocument = info.ServiceDocuments.Where(s => s.DocumentId == confirmation.ParentServiceEntityId).FirstOrDefault();
            if (parentServiceDocument != null && parentServiceDocument.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReject)
              document.DocumentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IRjConfirmation;
            else
              document.DocumentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IRConfirmation;
          }
          
          document.DocumentId = confirmation.ServiceEntityId;
          document.ParentDocumentId = confirmation.ParentServiceEntityId;
          document.CounterpartyId = counterpartyId;
          document.Date = ToTenantTime(confirmation.DateTime ?? message.TimeStamp);
          document.Body = confirmation.Content;
          document.Sign = message.Signatures.Single(s => s.DocumentId == document.DocumentId).Content;
          info.Save();
        }
        
        if (info == null && this.CanProcessMessageLater(message, queueItem, box, confirmation.RootServiceEntityId))
        {
          this.LogDebugFormat(message, confirmation, "Document info not found for received InvoiceConfirmation: RootServiceEntityId: '{0}'.", confirmation.RootServiceEntityId);
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Обработка ИОП.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="businessUnitBox">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessReceiptNotice(IMessage message, IMessageQueueItem queueItem, ICounterparty sender,
                                                bool isIncoming, IBusinessUnitBox businessUnitBox)
    {
      this.LogDebugFormat(message, queueItem, businessUnitBox, "Execute ProcessReceiptNotice.");
      foreach (var document in message.ReglamentDocuments.Where(d => d.DocumentType == ReglamentDocumentType.Receipt ||
                                                                d.DocumentType == ReglamentDocumentType.InvoiceReceipt))
      {
        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(businessUnitBox, document.RootServiceEntityId);
        if (exchangeDocumentInfo != null)
        {
          if (!exchangeDocumentInfo.ServiceDocuments.Any(d => d.DocumentId == document.ServiceEntityId) ||
              businessUnitBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
          {
            var parent = exchangeDocumentInfo.ServiceDocuments.SingleOrDefault(d => d.DocumentId == document.ParentServiceEntityId);
            if (parent == null && document.ParentServiceEntityId != document.RootServiceEntityId)
            {
              this.LogDebugFormat(message, document, "Service document not found for received Receipt: ParentServiceEntityId: '{0}'.", document.ParentServiceEntityId);
              return false;
            }
            
            Enumeration? documentType = null;
            if (document.DocumentType == ReglamentDocumentType.InvoiceReceipt)
            {
              if (parent == null && document.ParentServiceEntityId == exchangeDocumentInfo.ServiceDocumentId)
                documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt;
              
              if (parent != null)
              {
                if (parent.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReject)
                  documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IRReceipt;
                if (parent.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IConfirmation)
                  documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.ICReceipt;
                if (parent.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IRConfirmation)
                  documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IRCReceipt;
                if (parent.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.NoteReceipt)
                  documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.RNoteReceipt;
              }
            }
            if (document.DocumentType == ReglamentDocumentType.Receipt)
            {
              var parentServiceEntity = exchangeDocumentInfo.ServiceDocuments.FirstOrDefault(d => d.DocumentId == document.ParentServiceEntityId);
              if (parentServiceEntity != null)
              {
                documentType = (parentServiceEntity.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IConfirmation) ?
                  Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.ICReceipt :
                  Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt;
              }
              else
                documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt;
            }
            
            var serviceDoc = exchangeDocumentInfo.ServiceDocuments.FirstOrDefault(d => d.DocumentType == documentType) ?? exchangeDocumentInfo.ServiceDocuments.AddNew();
            serviceDoc.DocumentId = document.ServiceEntityId;
            serviceDoc.ParentDocumentId = document.ParentServiceEntityId;
            serviceDoc.CounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
            serviceDoc.DocumentType = documentType;
            serviceDoc.Date = ToTenantTime(document.DateTime ?? message.TimeStamp);
            serviceDoc.Body = document.Content;
            var signature = message.Signatures.Single(s => s.DocumentId == serviceDoc.DocumentId);
            serviceDoc.Sign = signature.Content;
            serviceDoc.FormalizedPoAUnifiedRegNo = signature.FormalizedPoAUnifiedRegNumber;

            exchangeDocumentInfo.Save();
            
            // Если получили ИОП на сам документ - надо записать в историю.
            if (serviceDoc.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt ||
                serviceDoc.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt)
            {
              exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(businessUnitBox, document.ParentServiceEntityId);
              if (exchangeDocumentInfo != null && exchangeDocumentInfo.Document != null &&
                  exchangeDocumentInfo.DeliveryConfirmationStatus != Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent)
              {
                var senderName = string.Empty;
                if (sender != null)
                  senderName = sender.Name;
                else if (message.Sender != null && message.Sender.Organization != null)
                  senderName = message.Sender.Organization.Name;
                else
                  senderName = Sungero.Exchange.Resources.NoneCounterparty;
                var historyComment = string.Format("{0}|{1}", senderName, businessUnitBox.ExchangeService.Name);
                
                var detailedOperation = new Enumeration(isIncoming ? Constants.Module.Exchange.GetReadMark : Constants.Module.Exchange.SendReadMark);
                var sentVersion = exchangeDocumentInfo.Document.Versions.FirstOrDefault(x => x.Id == exchangeDocumentInfo.VersionId);
                exchangeDocumentInfo.Document.History.Write(detailedOperation, detailedOperation, historyComment, sentVersion.Number);
                if (this.FixReceiptNotificationForSbis(exchangeDocumentInfo) || exchangeDocumentInfo.RootBox.ExchangeService.ExchangeProvider != ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
                  exchangeDocumentInfo.DeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent;
                exchangeDocumentInfo.Save();
              }
            }
          }
        }
        
        if (exchangeDocumentInfo == null && this.CanProcessMessageLater(message, queueItem, businessUnitBox, document.RootServiceEntityId))
        {
          this.LogDebugFormat(message, document, "Document info not found for received Receipt: ParentServiceEntityId: '{0}'.", document.ParentServiceEntityId);
          return false;
        }
      }
      
      return true;
    }
    
    /// <summary>
    /// Обработка УОП.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="businessUnitBox">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessNoteReceipt(IMessage message, IMessageQueueItem queueItem, ICounterparty sender,
                                              bool isIncoming, IBusinessUnitBox businessUnitBox)
    {
      this.LogDebugFormat(message, queueItem, businessUnitBox, "Execute ProcessNoteReceipt.");
      foreach (var document in message.ReglamentDocuments.Where(d => d.DocumentType == ReglamentDocumentType.NotificationReceipt))
      {
        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(businessUnitBox, document.RootServiceEntityId);
        if (exchangeDocumentInfo != null)
        {
          var documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.NoteReceipt;
          var serviceDoc = exchangeDocumentInfo.ServiceDocuments.FirstOrDefault(d => d.DocumentType == documentType) ?? exchangeDocumentInfo.ServiceDocuments.AddNew();
          serviceDoc.DocumentId = document.ServiceEntityId;
          serviceDoc.ParentDocumentId = document.ParentServiceEntityId;
          serviceDoc.CounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
          serviceDoc.DocumentType = documentType;
          serviceDoc.Date = ToTenantTime(document.DateTime ?? message.TimeStamp);
          serviceDoc.Body = document.Content;
          serviceDoc.Sign = message.Signatures.Single(s => s.DocumentId == serviceDoc.DocumentId).Content;

          exchangeDocumentInfo.Save();
          
          // Записать в историю.
          this.ProcessRecordHistory(document, isIncoming, sender, documentType, businessUnitBox);
        }
        
        if (exchangeDocumentInfo == null && this.CanProcessMessageLater(message, queueItem, businessUnitBox, document.RootServiceEntityId))
        {
          this.LogDebugFormat(message, document, "Document info not found for received NotificationReceipt: RootServiceEntityId: '{0}'.", document.RootServiceEntityId);
          return false;
        }
      }
      
      return true;
    }
    
    /// <summary>
    /// Обработка ИОП на УОП.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="sender">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="businessUnitBox">Абонентский ящик.</param>
    /// <returns>Признак успешности обработки сообщения.</returns>
    protected virtual bool ProcessReceiptOfNoteReceipt(IMessage message, IMessageQueueItem queueItem, ICounterparty sender,
                                                       bool isIncoming, IBusinessUnitBox businessUnitBox)
    {
      this.LogDebugFormat(message, queueItem, businessUnitBox, "Execute ProcessReceiptOfNoteReceipt");
      foreach (var document in message.ReglamentDocuments.Where(d => d.DocumentType == ReglamentDocumentType.NotificationOnReceiptOfNotificationReceipt))
      {
        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(businessUnitBox, document.RootServiceEntityId);
        if (exchangeDocumentInfo != null)
        {
          var documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.RNoteReceipt;
          var serviceDoc = exchangeDocumentInfo.ServiceDocuments.FirstOrDefault(d => d.DocumentType == documentType) ?? exchangeDocumentInfo.ServiceDocuments.AddNew();
          serviceDoc.DocumentId = document.ServiceEntityId;
          serviceDoc.ParentDocumentId = document.ParentServiceEntityId;
          serviceDoc.CounterpartyId = isIncoming ? message.Sender.Organization.OrganizationId : message.Receiver.Organization.OrganizationId;
          serviceDoc.DocumentType = documentType;
          serviceDoc.Date = ToTenantTime(document.DateTime ?? message.TimeStamp);
          serviceDoc.Body = document.Content;
          var signature = message.Signatures.Single(s => s.DocumentId == serviceDoc.DocumentId);
          serviceDoc.Sign = signature.Content;
          serviceDoc.FormalizedPoAUnifiedRegNo = signature.FormalizedPoAUnifiedRegNumber;
          serviceDoc.GeneratedName = document.FileName;
          serviceDoc.StageId = document.DocflowStageId;
          exchangeDocumentInfo.Save();
          
          // Записать в историю.
          this.ProcessRecordHistory(document, isIncoming, sender, documentType, businessUnitBox);
        }
        
        if (exchangeDocumentInfo == null)
        {
          if (this.CanProcessMessageLater(message, queueItem, businessUnitBox, document.RootServiceEntityId))
          {
            this.LogDebugFormat(message, document, "Acknowledgment receipt not found for received receipt notification: RootServiceEntityId: '{0}'.", document.RootServiceEntityId);
            return false;
          }
        }
      }
      
      return true;
    }
    
    protected virtual void ProcessRecordHistory(NpoComputer.DCX.Common.IReglamentDocument document, bool isIncoming, ICounterparty sender, Sungero.Core.Enumeration documentType, IBusinessUnitBox businessUnitBox)
    {
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(businessUnitBox, document.ParentServiceEntityId);
      if (exchangeDocumentInfo != null && exchangeDocumentInfo.Document != null &&
          exchangeDocumentInfo.DeliveryConfirmationStatus != Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent)
      {
        var historyComment = string.Format("{0}|{1}", sender.Name, businessUnitBox.ExchangeService.Name);
        var operation = new Enumeration(isIncoming ? Constants.Module.Exchange.GetRNoteReceiptReadMark : Constants.Module.Exchange.SendRNoteReceiptReadMark);
        
        if (documentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.NoteReceipt)
          operation = new Enumeration(isIncoming ? Constants.Module.Exchange.GetNoteReceiptReadMark : Constants.Module.Exchange.SendNoteReceiptReadMark);
        
        var detailedOperation = new Enumeration(isIncoming ? Constants.Module.Exchange.GetReadMark : Constants.Module.Exchange.SendReadMark);
        
        var sentVersion = exchangeDocumentInfo.Document.Versions.FirstOrDefault(x => x.Id == exchangeDocumentInfo.VersionId);
        exchangeDocumentInfo.Document.History.Write(operation, detailedOperation, historyComment, sentVersion.Number);
        
        if (this.FixReceiptNotificationForSbis(exchangeDocumentInfo))
          exchangeDocumentInfo.DeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent;
        exchangeDocumentInfo.Save();
      }
    }

    /// <summary>
    /// Проверить признак получения ИОПа и отправки УОПа для Sbis.
    /// </summary>
    /// <param name="info">Информация о документе обмена.</param>
    /// <returns>Признак получения ИОПа и отправки УОПа для Sbis.</returns>
    [Remote]
    public virtual bool FixReceiptNotificationForSbis(Exchange.IExchangeDocumentInfo info)
    {
      if (info.RootBox.ExchangeService.ExchangeProvider != ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
      {
        return false;
      }
      
      var docs = ExchangeDocumentInfos.GetAll()
        .Where(x => Equals(x.RootBox, info.RootBox) && x.Document != null &&
               x.ServiceDocuments.Any(d => (d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt ||
                                            d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt) && d.Date != null) &&
               x.ServiceDocuments.Any(d => d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.NoteReceipt && d.Date != null));
      return docs != null;
    }
    
    #endregion
    
    #region Обновление статусов по полученным ответам

    /// <summary>
    /// Обработать документ как отправленный - как из RX, так и из веба.
    /// </summary>
    /// <param name="info">Инфошка документа в сервисе.</param>
    /// <param name="document">Документ.</param>
    /// <param name="receiver">Контрагент.</param>
    /// <param name="isIncoming">Признак входящего сообщения.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="signStatus">Текущий статус документа - не подписывается, ожидает подписи, подписан двумя сторонами.</param>
    private static void MarkDocumentAsSended(IExchangeDocumentInfo info, IOfficialDocument document, ICounterparty receiver,
                                             bool isIncoming, IBoxBase box, SignStatus? signStatus)
    {
      if (!isIncoming)
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
      {
        AddTrackingRecordInfo(info, document, signStatus != SignStatus.None);
      }
      
      if (signStatus == SignStatus.Waiting)
      {
        if (isIncoming)
        {
          document.ExchangeState = Docflow.OfficialDocument.ExchangeState.SignRequired;
          if (info != null)
            info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.SignRequired;
        }
        else
        {
          document.ExchangeState = Docflow.OfficialDocument.ExchangeState.SignAwaited;
          if (info != null)
            info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.SignAwaited;
        }
      }
      else if (signStatus == SignStatus.Signed)
      {
        document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Signed;
        if (info != null)
        {
          info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Signed;
        }
      }
      else
      {
        if (isIncoming)
        {
          document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Received;
          if (info != null)
            info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Received;
        }
        else
        {
          document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Sent;
          if (info != null)
            info.ExchangeState = Exchange.ExchangeDocumentInfo.ExchangeState.Sent;
        }
      }
      
      if (!isIncoming)
      {
        var sendDocument = new Enumeration(Constants.Module.Exchange.SendDocument);
        document.History.Write(sendDocument, sendDocument, string.Format("{0}|{1}", receiver.Name, ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name), document.LastVersion.Number);
      }
      
      document.Save();
      info.Save();
    }

    /// <summary>
    /// Добавить запись выдачи в документе и установить статус согласования с КА.
    /// </summary>
    /// <param name="info">Информация о документе в сервисе обмена.</param>
    /// <param name="document">Документ.</param>
    /// <param name="needSign">Признак требования подписания.</param>
    private static void AddTrackingRecordInfo(IExchangeDocumentInfo info, IOfficialDocument document, bool needSign)
    {
      var tracking = document.Tracking.AddNew();
      if (needSign)
      {
        document.ExternalApprovalState = Sungero.Docflow.OfficialDocument.ExternalApprovalState.OnApproval;
        document.ExchangeState = Docflow.OfficialDocument.ExchangeState.SignAwaited;
        tracking.Action = Docflow.OfficialDocumentTracking.Action.Endorsement;
      }
      else
      {
        tracking.Action = Docflow.OfficialDocumentTracking.Action.Sending;
        document.ExchangeState = Docflow.OfficialDocument.ExchangeState.Sent;
        tracking.ReturnDeadline = null;
      }
      
      tracking.IsOriginal = true;
      tracking.DeliveredTo = Company.Employees.Current ??
        ExchangeCore.PublicFunctions.BoxBase.Remote.GetExchangeDocumentResponsible(info.Box, info.Counterparty, new List<IExchangeDocumentInfo>() { info });
      tracking.Note = Exchange.Resources.SendToCounterpartyNoteFormat(ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box));
      tracking.ExternalLinkId = info.Id;
    }
    
    #endregion
    
    #endregion
    
    #region Служебные методы обработки входящих сообщений

    private static NpoComputer.DCX.ClientApi.Client GetClient(IBusinessUnitBox box)
    {
      var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(box) as NpoComputer.DCX.ClientApi.Client;
      if (client == null)
        throw AppliedCodeException.Create("Ошибка при создании клиента.");
      
      return client;
    }

    /// <summary>
    /// Проверка необходимости сохранения сообщения в очереди.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="queueItem">Текущий элемент очереди, чтобы игнорировать его при поиске других элементов.</param>
    /// <param name="box">Головной ящик.</param>
    /// <param name="rootServiceDocumentId">ИД основного документа.</param>
    /// <returns>True, если сообщение еще можно обработать. False, если сообщение уже не нужно.</returns>
    protected virtual bool CanProcessMessageLater(IMessage message, IMessageQueueItem queueItem, IBusinessUnitBox box, string rootServiceDocumentId)
    {
      // Если документ уже аннулировали, то принимать ничего уже не надо.
      var root = message.PrimaryDocuments.FirstOrDefault(d => d.ServiceEntityId == rootServiceDocumentId);
      if (root != null && root.RevocationStatus == RevocationStatus.RevocationAccepted)
        return false;
      
      // Если документ не лежит в очереди - сообщение больше не нужно.
      if (!ExchangeCore.MessageQueueItems.GetAll(q => Equals(q.RootBox, box) &&
                                                 !Equals(q, queueItem) &&
                                                 q.ProcessingStatus != ExchangeCore.MessageQueueItem.ProcessingStatus.Processed &&
                                                 q.Documents.Any(d => d.ExternalId == rootServiceDocumentId)).Any())
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Получить ссылку на документ в вебе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ссылка на документ в вебе.</returns>
    [Public, Remote]
    public static string GetDocumentHyperlink(Docflow.IOfficialDocument document)
    {
      var docInfo = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(document);
      if (docInfo == null)
        return string.Empty;
      
      try
      {
        var client = GetClient(docInfo.RootBox);
        return client.GetDocumentUri(docInfo.ServiceMessageId, docInfo.ServiceDocumentId).ToString();
      }
      catch (AppliedCodeException)
      {
        throw;
      }
      catch (Exception ex)
      {
        Log.Exception(ex);
      }
      return string.Empty;
    }
    
    /// <summary>
    /// Получить ссылку на документ в вебе.
    /// </summary>
    /// <param name="messageQueueItem">Элемент очереди сообщений.</param>
    /// <returns>Ссылка на документ в вебе.</returns>
    [Public, Remote]
    public static string GetDocumentHyperlink(IMessageQueueItem messageQueueItem)
    {
      try
      {
        var client = GetClient(messageQueueItem.RootBox);

        // Для формирования ссылки СБИС достаточно ИД сообщения.
        if (Equals(messageQueueItem.RootBox.ExchangeService.ExchangeProvider, ExchangeCore.ExchangeService.ExchangeProvider.Sbis))
          return client.GetDocumentUri(string.Empty, messageQueueItem.ExternalId).ToString();

        // Для регламентных документов запрашиваем информацию из сервиса.
        var message = client.GetMessage(messageQueueItem.ExternalId);
        var document = message.PrimaryDocuments.FirstOrDefault();
        if (document != null)
        {
          // Формирование ссылки для уведомления об аннулировании.
          if (document.DocumentType == NpoComputer.DCX.Common.DocumentType.RevocationOffer &&
              message.ParentServiceMessageId != null &&
              document.ParentServiceEntityId != null)
            return client.GetDocumentUri(message.ParentServiceMessageId, document.ParentServiceEntityId).ToString();
          
          return client.GetDocumentUri(document.ServiceMessageId, document.ServiceEntityId).ToString();
        }
        else
        {
          // Если в сообщении только сервисные документы, ищем основной документ и его сообщение.
          var reglamentDocument = message.ReglamentDocuments.FirstOrDefault();
          return reglamentDocument != null ? client.GetDocumentUri(message.ParentServiceMessageId, reglamentDocument.RootServiceEntityId).ToString() : string.Empty;
        }
      }
      catch (AppliedCodeException)
      {
        throw;
      }
      catch (Exception ex)
      {
        Log.Exception(ex);
      }
      
      return string.Empty;
    }

    /// <summary>
    /// Привести дату к тенантному времени.
    /// </summary>
    /// <param name="datetime">Дата, пришедшая из МКДО.</param>
    /// <returns>Дата во времени тенанта.</returns>
    private static DateTime ToTenantTime(DateTime datetime)
    {
      return Docflow.PublicFunctions.Module.ToTenantTime(datetime);
    }
    
    /// <summary>
    /// Получить список поддерживаемых основных типов документов.
    /// </summary>
    /// <returns>Список типов.</returns>
    protected virtual List<DocumentType> GetSupportedPrimaryDocumentTypes()
    {
      return new List<NpoComputer.DCX.Common.DocumentType>()
      {
        DocumentType.Nonformalized,
        DocumentType.Waybill,
        DocumentType.Invoice,
        DocumentType.InvoiceCorrection,
        DocumentType.InvoiceCorrectionRevision,
        DocumentType.InvoiceRevision,
        DocumentType.Act,
        DocumentType.GeneralTransferSchfSeller,
        DocumentType.GeneralTransferSchfRevisionSeller,
        DocumentType.GeneralTransferSchfDopSeller,
        DocumentType.GeneralTransferSchfDopRevisionSeller,
        DocumentType.GeneralTransferSchfDopCorrectionSeller,
        DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller,
        DocumentType.GeneralTransferSchfCorrectionSeller,
        DocumentType.GeneralTransferSchfCorrectionRevisionSeller,
        DocumentType.GeneralTransferDopSeller,
        DocumentType.GeneralTransferDopRevisionSeller,
        DocumentType.GeneralTransferDopCorrectionSeller,
        DocumentType.GeneralTransferDopCorrectionRevisionSeller,
        DocumentType.WorksTransferSeller,
        DocumentType.WorksTransferRevisionSeller,
        DocumentType.GoodsTransferSeller,
        DocumentType.GoodsTransferRevisionSeller
      };
    }
    
    /// <summary>
    /// Получить список поддерживаемых регламентных типов документов.
    /// </summary>
    /// <returns>Список типов.</returns>
    protected virtual List<ReglamentDocumentType> GetSupportedReglamentDocumentTypes()
    {
      return new List<NpoComputer.DCX.Common.ReglamentDocumentType>()
      {
        ReglamentDocumentType.ActClientTitle,
        ReglamentDocumentType.WaybillBuyerTitle,
        ReglamentDocumentType.GeneralTransferBuyer,
        ReglamentDocumentType.GeneralTransferCorrectionBuyer,
        ReglamentDocumentType.GoodsTransferBuyer,
        ReglamentDocumentType.WorksTransferBuyer
      };
    }
    
    /// <summary>
    /// Получить список поддерживаемых регламентных типов документов.
    /// </summary>
    /// <returns>Список типов.</returns>
    protected virtual List<ReglamentDocumentType> GetSupportedServiceDocumentTypes()
    {
      return new List<NpoComputer.DCX.Common.ReglamentDocumentType>()
      {
        ReglamentDocumentType.AmendmentRequest,
        ReglamentDocumentType.InvoiceAmendmentRequest,
        ReglamentDocumentType.Rejection,
        ReglamentDocumentType.Receipt,
        ReglamentDocumentType.InvoiceReceipt,
        ReglamentDocumentType.InvoiceConfirmation,
        ReglamentDocumentType.NotificationReceipt,
        ReglamentDocumentType.NotificationOnReceiptOfNotificationReceipt
      };
    }
    
    private static string GetAttributeValueByName(System.Xml.Linq.XElement element, string attributeName)
    {
      var attribute = element.Attribute(attributeName);
      return attribute == null ? string.Empty : attribute.Value;
    }

    /// <summary>
    /// Получить или создать приложение-обработчик для документа.
    /// </summary>
    /// <param name="documentName">Имя документа.</param>
    /// <returns>Приложение-обработчик.</returns>
    [Public, Remote(IsPure = true)]
    public static Sungero.Content.IAssociatedApplication GetOrCreateAssociatedApplicationByDocumentName(string documentName)
    {
      // Определить приложение-обработчик. Если его нет - создать.
      var documentFullName = CommonLibrary.FileUtils.NormalizeFileName(documentName);
      var ext = System.IO.Path.GetExtension(documentFullName).TrimStart('.').ToLower();
      var application = Content.AssociatedApplications.GetByExtension(ext);
      
      // Если разрешения у файла нет, то использовать unknown.
      if (string.IsNullOrWhiteSpace(ext))
        application = Sungero.Content.AssociatedApplications.GetAll()
          .SingleOrDefault(x => x.Sid == Docflow.PublicConstants.Module.UnknownAppSid);
      if (application == null)
      {
        application = Content.AssociatedApplications.Create();
        application.Extension = ext;
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
          application.Name = Resources.AssociatedApplicationFormat(ext);
        application.MonitoringType = Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
        application.FilesType = Content.FilesTypes.GetAll().FirstOrDefault(f => f.Name == Docflow.Resources.Initialize_FileTypes_Other) ??
          Content.FilesTypes.GetAll().FirstOrDefault();
        application.Save();
        
        Functions.Module.LogDebugFormat(string.Format("Associated application \"{0}\" has been created", ext));
      }
      
      return application;
    }

    /// <summary>
    /// Получить последнюю подпись для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Подпись.</returns>
    protected virtual ISignature GetLastDocumentSignature(IOfficialDocument document)
    {
      var version = document.LastVersion;
      if (version == null)
        return null;
      
      return Signatures.Get(version).Where(x => x.SignCertificate != null).OrderByDescending(x => x.Id).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверить, что сообщение содержит документы неподдерживаемого типа.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <returns>True, если содержит, иначе False.</returns>
    public virtual bool IsMessageWithUnsupportedDocuments(NpoComputer.DCX.Common.IMessage message)
    {
      return message.PrimaryDocuments.All(x => (!this.GetSupportedPrimaryDocumentTypes().Contains(x.DocumentType.Value) ||
                                                (x.DocumentType == DocumentType.Nonformalized && x.IsUnknownDocumentType == true)));
    }
    
    #endregion

    #endregion

    #region Отправка документов и ответов по документам
    
    #region Подготовка и отправка сообщений

    /// <summary>
    /// Отправить ответ.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    [Remote, Public]
    public void SendAnswers(List<Docflow.IOfficialDocument> documents, Parties.ICounterparty counterparty, Sungero.ExchangeCore.IBusinessUnitBox box,
                            ICertificate certificate, bool isAgent)
    {
      if (!documents.Any())
        return;
      
      if (HasNotApprovedDocuments(documents.ToArray()))
        throw AppliedCodeException.Create(Resources.SendCounterpartyNotApproved);
      
      foreach (var document in documents)
      {
        var signature = this.GetDocumentSignature(document, certificate);
        if (signature == null)
          throw AppliedCodeException.Create(Resources.SendCounterpartyNotApproved);
      }
      
      foreach (var document in documents)
      {
        try
        {
          Docflow.PublicFunctions.OfficialDocument.SendAnswer(document, box, counterparty, certificate, isAgent);
          
          var info = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(document);
          var sendSignOperation = new Enumeration(Constants.Module.Exchange.SendAnswer);
          var comment = string.Format("{0}|{1}", counterparty.Name, box.ExchangeService.Name);
          
          if (info.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
          {
            var signature = this.GetDocumentSignature(document, certificate);
            this.ProcessSharedReject(info, document, false, box, signature.Body, sendSignOperation, comment, string.Empty, string.Empty, false);
          }
          else
            this.ProcessSharedSign(document, info, false, box, document.LastVersion, string.Empty, false, sendSignOperation, comment, false);
        }
        catch (AppliedCodeException)
        {
          throw;
        }
        catch (Exception ex)
        {
          throw AppliedCodeException.Create(ex.Message, ex);
        }
      }
    }

    /// <summary>
    /// Отправить ответ на пакет документов.
    /// </summary>
    /// <param name="documents">Документы пакета.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    [Obsolete("Теперь функция не актуальна, т.к. реализована поддержка частичного подписания.")]
    public virtual void SendAnswerDocumentsPackage(List<Docflow.IOfficialDocument> documents, Sungero.ExchangeCore.IBusinessUnitBox box,
                                                   ICertificate certificate, bool isAgent)
    {
      var client = GetClient(box);
      var reglamentDocuments = new List<NpoComputer.DCX.Common.IReglamentDocument>();
      var signatures = new List<NpoComputer.DCX.Common.Signature>();
      var serviceCounterpartyId = string.Empty;
      var serviceMessageId = string.Empty;
      var sentDocuments = new Dictionary<int, IOfficialDocument>();
      
      foreach (var document in documents)
      {
        var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(document);
        serviceCounterpartyId = exchangeDocumentInfo.ServiceCounterpartyId;
        serviceMessageId = exchangeDocumentInfo.ServiceMessageId;

        var signature = this.GetDocumentSignature(document, certificate);
        signatures.Add(CreateExchangeDocumentSignature(box.ExchangeService.ExchangeProvider,
                                                       exchangeDocumentInfo.ExternalBuyerTitleId ?? exchangeDocumentInfo.ServiceDocumentId,
                                                       signature.Body, signature.FormalizedPoAUnifiedRegNumber));
        
        exchangeDocumentInfo.ReceiverSignId = signature.Id;
        exchangeDocumentInfo.Save();
        
        var accountingDocument = AccountingDocumentBases.As(document);
        
        // У СФ ИД титула будет пустым всегда.
        if (accountingDocument != null && accountingDocument.BuyerTitleId != null)
        {
          sentDocuments.Add(accountingDocument.BuyerTitleId.Value, document);
          var version = accountingDocument.Versions.Single(v => v.Id == accountingDocument.BuyerTitleId);
          byte[] receipt;
          using (var memory = new System.IO.MemoryStream())
          {
            version.Body.Read().CopyTo(memory);
            receipt = memory.ToArray();
          }
          
          accountingDocument.BuyerSignatureId = signature.Id;
          
          var docWithCertificate = Structures.Module.ReglamentDocumentWithCertificate.Create(FinancialArchive.Resources.BuyerTitleVersionNote, receipt,
                                                                                             certificate, signature.Body, exchangeDocumentInfo.ServiceDocumentId,
                                                                                             box, accountingDocument, exchangeDocumentInfo.ServiceMessageId, null, null,
                                                                                             exchangeDocumentInfo.ServiceCounterpartyId, false,
                                                                                             exchangeDocumentInfo, false, null, null);
          
          var type = NpoComputer.DCX.Common.ReglamentDocumentType.WaybillBuyerTitle;
          
          if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
            type = NpoComputer.DCX.Common.ReglamentDocumentType.WorksTransferBuyer;
          
          if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act)
            type = NpoComputer.DCX.Common.ReglamentDocumentType.ActClientTitle;
          
          if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer)
            type = NpoComputer.DCX.Common.ReglamentDocumentType.GoodsTransferBuyer;
          
          if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
            type = accountingDocument.IsAdjustment == true ?
              NpoComputer.DCX.Common.ReglamentDocumentType.GeneralTransferCorrectionBuyer :
              NpoComputer.DCX.Common.ReglamentDocumentType.GeneralTransferBuyer;
          
          var serviceDocument = this.CreateReglamentExchangeServiceDocument(docWithCertificate, type);
          reglamentDocuments.Add(serviceDocument);
        }
        else
          sentDocuments.Add(exchangeDocumentInfo.VersionId.Value, document);
      }
      
      try
      {
        this.SendMessage(new List<NpoComputer.DCX.Common.IDocument>(),
                         reglamentDocuments, signatures, client, null, serviceCounterpartyId, box, serviceMessageId);
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create(ex.Message, ex);
      }
      
      foreach (var document in sentDocuments)
      {
        if (isAgent)
        {
          Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(document.Value, document.Key, Docflow.OfficialDocument.ExchangeState.Signed);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(document.Value, document.Key);
          Functions.Module.EnqueueXmlToPdfBodyConverter(document.Value, document.Key, Docflow.OfficialDocument.ExchangeState.Signed);
        }
      }
    }
    
    /// <summary>
    /// Отправить ответ на неформализованный документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. Иначе - пользователем в RX.</param>
    [Public]
    public virtual void SendAnswerToNonformalizedDocument(Docflow.IOfficialDocument document, Parties.ICounterparty counterparty,
                                                          ExchangeCore.IBusinessUnitBox box, ICertificate certificate, bool isAgent)
    {
      var client = GetClient(box);
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(document);
      
      // Нет информации об обмене по последней версии.
      if (exchangeDocumentInfo == null)
        return;
      
      var parentDocumentId = exchangeDocumentInfo.ServiceDocumentId;
      if (string.IsNullOrEmpty(parentDocumentId))
        return;
      
      var signature = this.GetDocumentSignature(document, certificate);
      if (signature == null)
        throw AppliedCodeException.Create(Resources.SendCounterpartyAddendaNotSigned);
      
      if (exchangeDocumentInfo.NeedSign == true)
      {
        // Если документ требовал подписания, проверяем - не подписан/отказан он в вебе.
        var allowedAnswers = client.GetAllowedAnswers(exchangeDocumentInfo.ServiceDocumentId, exchangeDocumentInfo.ServiceMessageId, null);
        if (!allowedAnswers.CanSendSign)
          return;
      }
      else
      {
        // Не отправляем подписи по документам, которые не требовали подписания.
        return;
      }
      
      var dcxSign = CreateExchangeDocumentSignature(box.ExchangeService.ExchangeProvider, parentDocumentId, signature.Body, signature.FormalizedPoAUnifiedRegNumber);

      try
      {
        var sentMessage = this.SendMessage(new List<NpoComputer.DCX.Common.IDocument>(),
                                           new List<NpoComputer.DCX.Common.IReglamentDocument>(),
                                           new List<NpoComputer.DCX.Common.Signature>() { dcxSign }, client, counterparty, exchangeDocumentInfo.ServiceCounterpartyId, box, exchangeDocumentInfo.ServiceMessageId);
        exchangeDocumentInfo.ReceiverSignId = signature.Id;
        exchangeDocumentInfo.Save();
        
        var needUpdateSign = box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && !string.IsNullOrEmpty(signature.FormalizedPoAUnifiedRegNumber);
        
        if (needUpdateSign)
        {
          var patсhedSignature = sentMessage.Signatures.Single(s => string.Equals(s.DocumentId, parentDocumentId));
          Docflow.PublicFunctions.Module.SetDataSignature(document, signature.Id, patсhedSignature.Content);
        }
        
        if (isAgent)
        {
          Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(document, exchangeDocumentInfo.VersionId.Value, Docflow.OfficialDocument.ExchangeState.Signed);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(document, exchangeDocumentInfo.VersionId.Value);
          Functions.Module.EnqueueXmlToPdfBodyConverter(document, exchangeDocumentInfo.VersionId.Value, Docflow.OfficialDocument.ExchangeState.Signed);
        }
        
        this.LogDebugFormat(exchangeDocumentInfo, "Send answer to nonformalized document.");
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create(ex.Message, ex);
      }
    }

    /// <summary>
    /// Отправить титул покупателя для накладной или акта.
    /// </summary>
    /// <param name="waybill">Накладная или акт.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса. False используется для вызова из UI.</param>
    [Public]
    public virtual void SendBuyerTitle(Docflow.IAccountingDocumentBase waybill, ExchangeCore.IBusinessUnitBox box, ICertificate certificate, bool isAgent)
    {
      if (box == null)
        throw AppliedCodeException.Create(Resources.BoxIsNotValid);
      
      if (certificate == null)
        throw AppliedCodeException.Create(Resources.CertificateNotFound);
      
      var docsWithCertificate = Structures.Module.ReglamentDocumentWithCertificate.Create();
      var externalDocumentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(waybill);
      try
      {
        if (externalDocumentInfo == null)
          return;
        
        var version = waybill.Versions.Single(v => v.Id == waybill.BuyerTitleId);
        byte[] receipt;
        var documentName = string.Empty;
        using (var memory = new System.IO.MemoryStream())
        {
          version.Body.Read().CopyTo(memory);
          receipt = memory.ToArray();
          try
          {
            var encoding = Encoding.GetEncoding(1251);
            var title = XDocument.Parse(encoding.GetString(receipt));
            documentName = title.Element("Файл").Attribute("ИдФайл").Value + ".xml";
          }
          catch (Exception ex)
          {
            Logger.Error("Can't parse document name from xml", ex);
          }
        }
        
        var sign = Signatures.Get(version)
          .FirstOrDefault(s => s.SignCertificate != null && s.SignCertificate.Thumbprint == certificate.Thumbprint);
        var signBody = sign.GetDataSignature();
        var unifiedRegistrationNumber = Docflow.PublicFunctions.Module.GetUnsignedAttribute(sign, Docflow.PublicConstants.Module.UnsignedAdditionalInfoKeyFPoA);
        
        waybill.BuyerSignatureId = sign.Id;
        externalDocumentInfo.ReceiverSignId = sign.Id;
        externalDocumentInfo.Save();
        
        docsWithCertificate = Structures.Module.ReglamentDocumentWithCertificate.Create(string.IsNullOrEmpty(documentName) ? FinancialArchive.Resources.BuyerTitleVersionNote : documentName,
                                                                                        receipt, certificate, signBody, externalDocumentInfo.ServiceDocumentId,
                                                                                        box, waybill, externalDocumentInfo.ServiceMessageId, null, null,
                                                                                        externalDocumentInfo.ServiceCounterpartyId, false,
                                                                                        externalDocumentInfo, false, null, unifiedRegistrationNumber);
      }
      catch (Exception ex)
      {
        if (ex is CommonLibrary.Exceptions.PlatformException)
          throw;
        
        throw AppliedCodeException.Create(Resources.ErrorWhileSendingDocToCounterparty, ex);
      }
      
      // По идее, разницы у титулов на уровне тел нет, пока сделаем просто перебором.
      var type = NpoComputer.DCX.Common.ReglamentDocumentType.WaybillBuyerTitle;
      var exchangeService = waybill.BusinessUnitBox.ExchangeService.ExchangeProvider;
      
      if (waybill.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer &&
          exchangeService != ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        type = NpoComputer.DCX.Common.ReglamentDocumentType.WorksTransferBuyer;
      
      if (waybill.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act ||
          exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc &&
          waybill.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
        type = NpoComputer.DCX.Common.ReglamentDocumentType.ActClientTitle;
      
      if (waybill.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer &&
          exchangeService != ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        type = NpoComputer.DCX.Common.ReglamentDocumentType.GoodsTransferBuyer;
      
      if (waybill.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
        type = waybill.IsAdjustment == true ?
          NpoComputer.DCX.Common.ReglamentDocumentType.GeneralTransferCorrectionBuyer :
          NpoComputer.DCX.Common.ReglamentDocumentType.GeneralTransferBuyer;
      
      this.SendServiceDocument(new List<Structures.Module.ReglamentDocumentWithCertificate> { docsWithCertificate }, box, type);
      
      if (isAgent)
      {
        Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(waybill, waybill.BuyerTitleId.Value, Docflow.OfficialDocument.ExchangeState.Signed);
      }
      else
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(waybill, waybill.BuyerTitleId.Value);
        Functions.Module.EnqueueXmlToPdfBodyConverter(waybill, waybill.BuyerTitleId.Value, Docflow.OfficialDocument.ExchangeState.Signed);
      }
    }

    /// <summary>
    /// Отправить уведомления об уточнении документов.
    /// </summary>
    /// <param name="signedDocuments">Подписанные уведомления об уточнении.</param>
    /// <param name="receiver">Получатель уведомления.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="note">Комментарий.</param>
    [Remote]
    public void SendAmendmentRequest(List<Structures.Module.ReglamentDocumentWithCertificate> signedDocuments, Parties.ICounterparty receiver,
                                     ExchangeCore.IBoxBase box, string note)
    {
      var operation = new Enumeration(Constants.Module.Exchange.SendAnswer);
      var comment = string.Format("{0}|{1}", receiver.Name, ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(box).Name);
      
      var businessUnitBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);

      // Для счетов-фактур и УПД тип служебного документа в сервисе другой.
      var invoiceDocuments = signedDocuments.Where(d => d.IsInvoiceFlow).ToList();
      
      // Отправляем одним сообщением УОУ на комплект документов из СБИС.
      var packageProcessingSbis = signedDocuments.Count > 1 && businessUnitBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis;
      if (packageProcessingSbis)
        this.SendServiceDocument(signedDocuments,
                                 ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box), ReglamentDocumentType.InvoiceAmendmentRequest);

      if (!packageProcessingSbis && invoiceDocuments.Any())
        this.SendServiceDocument(invoiceDocuments,
                                 ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box), ReglamentDocumentType.InvoiceAmendmentRequest);
      
      foreach (var document in invoiceDocuments)
      {
        var doc = document.LinkedDocument;
        var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ParentDocumentId);
        this.ProcessSharedInvoiceReject(info, doc, false, box, document.Signature, operation, comment, string.Empty, note, false);
      }
      
      var notInvoiceDocuments = signedDocuments.Where(d => !d.IsInvoiceFlow).ToList();
      var reglamentDocumentType = businessUnitBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc ?
        ReglamentDocumentType.Rejection : ReglamentDocumentType.AmendmentRequest;
      if (!packageProcessingSbis && notInvoiceDocuments.Any())
        this.SendServiceDocument(notInvoiceDocuments,
                                 ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box), reglamentDocumentType);
      
      foreach (var document in notInvoiceDocuments)
      {
        var doc = document.LinkedDocument;
        var info = Functions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ParentDocumentId);
        this.ProcessSharedReject(info, doc, false, box, document.Signature, operation, comment, string.Empty, note, false);
      }
    }

    /// <summary>
    /// Отправить извещения о получении документов.
    /// </summary>
    /// <param name="signedDocuments">Подписанные извещения о получении.</param>
    /// <param name="box">Абонентский ящик.</param>
    [Remote]
    public virtual void SendDeliveryConfirmation(List<Structures.Module.ReglamentDocumentWithCertificate> signedDocuments, ExchangeCore.IBusinessUnitBox box)
    {
      this.LogDebugFormat(box, "Execute SendDeliveryConfirmation.");
      // Нельзя разделять по типам служебок для СБИС, потому что не будет работать отправка комплектов формализованный + неформализованный.
      if (box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        this.SendServiceDocument(signedDocuments, box, NpoComputer.DCX.Common.ReglamentDocumentType.InvoiceReceipt);
      else
      {
        // Для счетов-фактур и УПД тип служебного документа в сервисе другой.
        var invoiceDocuments = signedDocuments.Where(d => d.IsInvoiceFlow).ToList();
        if (invoiceDocuments.Any())
          this.SendServiceDocument(invoiceDocuments, box, NpoComputer.DCX.Common.ReglamentDocumentType.InvoiceReceipt);
        
        var notInvoiceDocuments = signedDocuments.Where(d => !d.IsInvoiceFlow).ToList();
        if (notInvoiceDocuments.Any())
          this.SendServiceDocument(notInvoiceDocuments, box, NpoComputer.DCX.Common.ReglamentDocumentType.Receipt);
      }

      var rootReceipts = signedDocuments.Where(d => d.IsRootDocumentReceipt == true);
      foreach (var receipt in rootReceipts)
      {
        var comment = string.Format("{0}|{1}", receipt.Info.Counterparty.Name, box.ExchangeService.Name);
        this.FixReceiptNotification(receipt.Info, comment, true);
      }
      
      Jobs.GetMessages.Enqueue();
    }

    /// <summary>
    /// Отправить служебные документы сервиса обмена.
    /// </summary>
    /// <param name="signedDocuments">Коллекция подписанных документов.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="documentType">Тип документа.</param>
    protected virtual void SendServiceDocument(List<ReglamentDocumentWithCertificate> signedDocuments, IBusinessUnitBox box, ReglamentDocumentType documentType)
    {
      var client = GetClient(box);
      var processedMessagesId = new List<string>();
      
      // Для СБИС хранится составной ParentDocumentId, первая часть которого - ИД сообщения, вторая - ИД документа.
      // ИОПы необходимо отправлять одним сообщением на весь комплект документов.
      foreach (var serviceDocuments in signedDocuments.GroupBy(d => d.ParentDocumentId.Split('#').First()))
      {
        var document = serviceDocuments.First();
        var serviceDocumentsToSend = new List<NpoComputer.DCX.Common.IReglamentDocument>();
        var serviceDocumentsSigns = new List<NpoComputer.DCX.Common.Signature>();
        
        foreach (var reglamentDocument in serviceDocuments)
        {
          var currentDocumentType = documentType;
          var serviceDocument = this.CreateReglamentExchangeServiceDocument(reglamentDocument, currentDocumentType);
          serviceDocumentsToSend.Add(serviceDocument);
          
          var sign = CreateExchangeDocumentSignature(box.ExchangeService.ExchangeProvider, serviceDocument.ServiceEntityId,
                                                     reglamentDocument.Signature, reglamentDocument.FormalizedPoAUnifiedRegNumber);

          serviceDocumentsSigns.Add(sign);
          this.LogDebugFormat(reglamentDocument.Info, "Execute SendServiceDocument. Prepare service document with DocumentType = {0}, LinkedDocumentId = {1}.",
                              reglamentDocument.ReglamentDocumentType, reglamentDocument.LinkedDocument.Id);
        }
        var isBuyerTitle = this.GetSupportedReglamentDocumentTypes().Contains(documentType);
        
        try
        {
          var sentMessage = this.SendMessage(new List<NpoComputer.DCX.Common.IDocument>(),
                                             serviceDocumentsToSend, serviceDocumentsSigns, client, null, document.ServiceCounterpartyId, box, document.ServiceMessageId);
          
          this.LogDebugFormat(document.Info, serviceDocumentsToSend, "Execute SendServiceDocument. Send service document: ServiceCounterpartyId = {0}.", document.ServiceCounterpartyId);
          
          var needUpdateSign = box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && isBuyerTitle &&
            signedDocuments.Any(d => !string.IsNullOrEmpty(d.FormalizedPoAUnifiedRegNumber));
          if (needUpdateSign)
          {
            var signedDocument = signedDocuments.First();
            var patсhedSignature = sentMessage.Signatures.Single(s => string.Equals(s.DocumentId, signedDocument.Info.ExternalBuyerTitleId));
            Docflow.PublicFunctions.Module.SetDataSignature(signedDocument.LinkedDocument, (int)signedDocument.Info.ReceiverSignId, patсhedSignature.Content);
          }
        }
        catch (NpoComputer.DCX.Common.Exceptions.WorkflowViolationException ex)
        {
          if (documentType == ReglamentDocumentType.InvoiceReceipt || documentType == ReglamentDocumentType.Receipt)
          {
            var innerExceptionText = ex.InnerException != null
              ? string.Format("{0}. ", ex.InnerException.Message)
              : string.Empty;
            var reglamentDocumentTypeValue = document.ReglamentDocumentType.HasValue ? document.ReglamentDocumentType.Value.Value : string.Empty;
            var debugText = string.Format("{0}Receipt notice with Name = '{1}', ReglamentDocumentType = '{2}', ParentDocumentId = '{3}' already sent. " +
                                          "Start the job of receiving messages from the exchange service.",
                                          innerExceptionText, document.Name, reglamentDocumentTypeValue, document.ParentDocumentId);
            this.LogDebugFormat(debugText);
          }
          else if (isBuyerTitle)
          {
            this.LogDebugFormat(ex.Message);
            throw AppliedCodeException.Create(Resources.OneOrMoreDocumentAlreadyProcessing);
          }
          else
            throw;
        }
        catch (Exception e)
        {
          if (documentType == ReglamentDocumentType.InvoiceReceipt || documentType == ReglamentDocumentType.Receipt)
          {
            this.LogDebugFormat(e.ToString());
          }
          else
            throw;
        }
      }
    }
    
    /// <summary>
    /// Отправить пакет служебных документов сервиса обмена.
    /// </summary>
    /// <param name="signedDocuments">Коллекция подписанных документов.</param>
    /// <param name="box">Ящик.</param>
    /// <param name="documentType">Тип документа.</param>
    [Obsolete("Теперь функция не актуальна, т.к. реализована поддержка частичного подписания.")]
    protected virtual void SendPackageServiceDocuments(List<ReglamentDocumentWithCertificate> signedDocuments, IBusinessUnitBox box, ReglamentDocumentType documentType)
    {
      var client = GetClient(box);
      var serviceDocuments = new List<NpoComputer.DCX.Common.IReglamentDocument>();
      var signatures = new List<NpoComputer.DCX.Common.Signature>();
      var serviceCounterpartyId = signedDocuments.First().ServiceCounterpartyId;
      var serviceMessageId = signedDocuments.First().ServiceMessageId;

      foreach (var document in signedDocuments)
      {
        var serviceDoc = this.CreateReglamentExchangeServiceDocument(document, documentType);
        serviceDocuments.Add(serviceDoc);
        signatures.Add(CreateExchangeDocumentSignature(box.ExchangeService.ExchangeProvider, serviceDoc.ServiceEntityId, document.Signature, null));
        Logger.Debug(string.Format("Prepare receipt notification with ExchangeDocumentInfoId = {0}, DocumentType = {1}, LinkedDocumentId = {2}, ServiceMessageId = {3}",
                                   document.Info.Id, document.ReglamentDocumentType, document.LinkedDocument.Id, document.ServiceMessageId));
      }
      
      try
      {
        this.SendMessage(new List<NpoComputer.DCX.Common.IDocument>(), serviceDocuments, signatures,
                         client, null, serviceCounterpartyId, box, serviceMessageId);
        
        foreach (var document in signedDocuments)
          Logger.Debug(string.Format("Send receipt notification: ServiceCounterpartyId = {0}, ParentDocumentId = {1}, ServiceMessageId = {2}",
                                     document.ServiceCounterpartyId, document.ParentDocumentId, document.ServiceMessageId));
      }
      catch (Exception e)
      {
        throw e;
      }
    }

    /// <summary>
    /// Создать сообщение в сервис обмена.
    /// </summary>
    /// <param name="primaryDocuments">Список основных документов.</param>
    /// <param name="reglamentDocuments">Список регламентных документов.</param>
    /// <param name="signs">Список подписей.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="serviceCounterpartyId">Внешний ИД контрагента.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="parentServiceMessageId">ИД сообщения, для которого отправляется ответ.</param>
    /// <returns>Результат отправки.</returns>
    protected NpoComputer.DCX.Common.Message CreateMessage(List<IDocument> primaryDocuments, List<IReglamentDocument> reglamentDocuments,
                                                           List<Signature> signs, DcxClient client, ICounterparty receiver, string serviceCounterpartyId, IBusinessUnitBox box,
                                                           string parentServiceMessageId)
    {
      if (serviceCounterpartyId == string.Empty)
      {
        var receiverLine = receiver.ExchangeBoxes.Where(x => Equals(x.Box, box) && x.IsDefault == true).SingleOrDefault();
        if (receiverLine == null)
          throw AppliedCodeException.Create(string.Format("Для контрагента c ИД {0} не установлена связь через указанный абонентский ящик с ИД {1}.", receiver.Id, box.Id));
        serviceCounterpartyId = receiverLine.OrganizationId;
      }
      
      var counterpartyBoxId = client.GetContact(serviceCounterpartyId).Organization.BoxId;
      
      var message = new NpoComputer.DCX.Common.Message()
      {
        IsReply = !string.IsNullOrEmpty(parentServiceMessageId),
        ParentServiceMessageId = parentServiceMessageId,
        PrimaryDocuments = primaryDocuments,
        ReglamentDocuments = reglamentDocuments,
        Signatures = signs.ToList(),
        Receiver = new NpoComputer.DCX.Common.Subscriber()
        {
          BoxId = counterpartyBoxId
        }
      };
      
      return message;
    }
    
    /// <summary>
    /// Отправить сообщение в сервис обмена.
    /// </summary>
    /// <param name="primaryDocuments">Список основных документов.</param>
    /// <param name="reglamentDocuments">Список регламентных документов.</param>
    /// <param name="signs">Список подписей.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="serviceCounterpartyId">Внешний ИД контрагента.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="parentServiceMessageId">ИД сообщения, для которого отправляется ответ.</param>
    /// <returns>Результат отправки.</returns>
    protected virtual SentMessage SendMessage(List<IDocument> primaryDocuments, List<IReglamentDocument> reglamentDocuments,
                                              List<Signature> signs, DcxClient client, ICounterparty receiver, string serviceCounterpartyId, IBusinessUnitBox box,
                                              string parentServiceMessageId)
    {
      var message = this.CreateMessage(primaryDocuments, reglamentDocuments, signs, client, receiver, serviceCounterpartyId, box, parentServiceMessageId);
      
      return client.SendMessage(message);
    }
    
    /// <summary>
    /// Отправить сообщение в сервис обмена c УОП.
    /// </summary>
    /// <param name="primaryDocuments">Список основных документов.</param>
    /// <param name="reglamentDocuments">Список регламентных документов.</param>
    /// <param name="signs">Список подписей.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="serviceCounterpartyId">Внешний ИД контрагента.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="parentServiceMessageId">ИД сообщения, для которого отправляется ответ.</param>
    /// <param name="document">Документ, для которого отправляется ответ.</param>
    /// <returns>Результат отправки.</returns>
    protected virtual SentMessage SendMessageWithServiceDocument(List<IDocument> primaryDocuments, List<IReglamentDocument> reglamentDocuments,
                                                                 List<Signature> signs, DcxClient client, ICounterparty receiver, string serviceCounterpartyId, IBusinessUnitBox box,
                                                                 string parentServiceMessageId, Docflow.IOfficialDocument document)
    {
      var message = this.CreateMessage(primaryDocuments, reglamentDocuments, signs, client, receiver, serviceCounterpartyId, box, parentServiceMessageId);
      
      if (string.IsNullOrEmpty(serviceCounterpartyId))
      {
        var receiverLine = receiver.ExchangeBoxes.Where(x => Equals(x.Box, box) && x.IsDefault == true).SingleOrDefault();
        if (receiverLine == null)
          throw AppliedCodeException.Create(string.Format("Для контрагента c ИД {0} не установлена связь через указанный абонентский ящик с ИД {1}.", receiver.Id, box.Id));
        
        serviceCounterpartyId = receiverLine.OrganizationId;
      }
      
      var counterpartyBoxId = client.GetContact(serviceCounterpartyId).Organization.BoxId;
      
      if (receiver != null && message.IsReply)
      {
        var cert = box.CertificateReceiptNotifications;
        var counterpartyBox = receiver.ExchangeBoxes.First(b => b.OrganizationId == counterpartyBoxId);
        if (counterpartyBox != null && counterpartyBox.Box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
          this.PrepareReplyMessage(message, cert, client, primaryDocuments, box, document);
      }
      
      return client.SendMessage(message);
    }
    
    /// <summary>
    /// Подготовить ответное сообщение к отправке на сервис.
    /// В методе генерируются служебные документы для отправки на сервис результатов подписания документов сообщения.
    /// Служебки генерируются только при наличии установленного сертификата для подписания.
    /// </summary>
    /// <param name="outcomeReplyMessage">Сообщение, которое будет отправлено на сервис.</param>
    /// <param name="certificate">Исходящее ответное сообщение из справочника.</param>
    /// <param name="client">Клиент.</param>
    /// <param name="documents">Список документов.</param>
    /// <param name="box">Яшик нашего абонента.</param>
    /// <param name="document">Основной документ.</param>
    private void PrepareReplyMessage(IMessage outcomeReplyMessage, ICertificate certificate, DcxClient client, List<NpoComputer.DCX.Common.IDocument> documents, IBusinessUnitBox box, IOfficialDocument document)
    {
      var exchangeDocumentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(document);
      if (certificate == null)
        return;
      else
        Logger.DebugFormat("No signing certificate found. ServiceMessageId = {0}", outcomeReplyMessage.ServiceMessageId);
      
      var signerInfo = NpoComputer.DCX.Common.SignerInfo.CreateFromSignature(certificate.X509Certificate);
      foreach (var sign in outcomeReplyMessage.Signatures)
        sign.SignerInfo = signerInfo;
      
      var documentsForSign = client.PrepareReplyMessage(outcomeReplyMessage, signerInfo);
      
      var documentsToSave = new List<Structures.Module.ReglamentDocumentWithCertificate>();
      foreach (var documentForSign in documentsForSign)
      {
        if (documentForSign is NpoComputer.DCX.Common.IReglamentDocument)
        {
          var doc = (NpoComputer.DCX.Common.IReglamentDocument)documentForSign;
          Logger.DebugFormat("PrepareReplyMessage. ServiceEntityId = {0}, ServiceMessageId = {1}", doc.ServiceEntityId, outcomeReplyMessage.ServiceMessageId);
          var documentPriority = new Dictionary<string, byte[]>();
          documentPriority.Add(doc.ServiceEntityId, doc.Content);
          var signs = ExternalSignatures.Sign(certificate, documentPriority);
          
          // TODO Использовать конструктор в прикладной CreateExchangeDocumentSignature.
          var sign = new NpoComputer.DCX.Common.Signature
          {
            DocumentId = doc.ServiceEntityId,
            Content = signs[doc.ServiceEntityId],
            SignerInfo = signerInfo
          };
          
          outcomeReplyMessage.Signatures.Add(sign);
          
          if (doc.DocumentType == ReglamentDocumentType.NotificationReceipt)
          {
            var type = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.NoteReceipt;
            
            var docWithCertificate = Structures.Module.ReglamentDocumentWithCertificate.Create(documentForSign.FileName, documentForSign.Content,
                                                                                               certificate, sign.Content, documentForSign.ParentServiceEntityId,
                                                                                               box, document, null, documentForSign.ServiceEntityId, null,
                                                                                               exchangeDocumentInfo.ServiceCounterpartyId, false,
                                                                                               exchangeDocumentInfo, false, type, null);

            var isSendJobEnabled = PublicFunctions.Module.Remote.IsJobEnabled(PublicConstants.Module.SendSignedReceiptNotificationsId);
            this.SaveDeliveryConfirmationSigns(new List<Structures.Module.ReglamentDocumentWithCertificate> { docWithCertificate });
            
            var info = docWithCertificate.Info;
            
            var serviceDocument = info.ServiceDocuments.FirstOrDefault(d => d.DocumentType == docWithCertificate.ReglamentDocumentType);
            serviceDocument.CounterpartyId = docWithCertificate.ServiceCounterpartyId;
            serviceDocument.DocumentId = docWithCertificate.ServiceDocumentId;
            serviceDocument.ParentDocumentId = docWithCertificate.ParentDocumentId;
            serviceDocument.StageId = doc.DocflowStageId;
            serviceDocument.Date = doc.DateTime;
            info.Save();
          }
        }
      }
    }

    /// <summary>
    /// Отправить документ в сервис обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="receiver">Получатель.</param>
    /// <param name="box">Абонентский ящик отправителя.</param>
    /// <param name="certificate">Сертификат, которым подписаны документы.</param>
    /// <param name="needSign">Требовать подписание от контрагента.</param>
    /// <param name="comment">Комментарий к сообщению в сервисе.</param>
    [Remote, Public]
    public virtual void SendDocuments(Sungero.Docflow.IOfficialDocument document, List<Sungero.Docflow.IOfficialDocument> addenda,
                                      Parties.ICounterparty receiver, ExchangeCore.IBusinessUnitBox box, ICertificate certificate, bool needSign, string comment)
    {
      if (HasNotApprovedDocuments(document, addenda))
        throw AppliedCodeException.Create(Resources.SendCounterpartyNotApproved);
      
      var signatures = new List<NpoComputer.DCX.Common.Signature>();
      
      Func<IOfficialDocument, bool> isNeedSign = (d) =>
      {
        var accountingDocument = AccountingDocumentBases.As(d);
        if (accountingDocument == null)
          return needSign;
        
        var isTaxInvoice = accountingDocument.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        var isOutgoingInvoice = Contracts.OutgoingInvoices.Is(d);
        return needSign && !isTaxInvoice && !isOutgoingInvoice;
      };
      
      var primaryDocuments = new List<NpoComputer.DCX.Common.IDocument>();
      primaryDocuments.Add(Functions.Module.CreatePrimaryExchangeServiceDocument(document, needSign, comment));
      foreach (var doc in addenda)
      {
        // Для СБИС нельзя на часть документов комплекта запросить подпись, а на остальные не запрашивать.
        primaryDocuments.Add(Functions.Module.CreatePrimaryExchangeServiceDocument(doc,
                                                                                   box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis ? needSign : isNeedSign(doc),
                                                                                   string.Empty));
      }
      
      var documents = new List<Sungero.Docflow.IOfficialDocument>() { document };
      documents.AddRange(addenda);
      foreach (var doc in documents)
      {
        if (!doc.AccessRights.CanUpdate() || !doc.AccessRights.CanSendByExchange())
          throw AppliedCodeException.Create(Resources.SendCounterpartyAddendaNotRightFormat(doc.Name));
        
        var signature = this.GetDocumentSignature(doc, certificate);
        if (signature == null)
          throw AppliedCodeException.Create(Resources.SendCounterpartyAddendaNotSigned);
      }
      foreach (var doc in documents)
      {
        var signature = this.GetDocumentSignature(doc, certificate);
        var dcxSign = CreateExchangeDocumentSignature(box.ExchangeService.ExchangeProvider, doc.Id.ToString(), signature.Body, signature.FormalizedPoAUnifiedRegNumber);
        signatures.Add(dcxSign);
      }
      var client = GetClient(box);
      var sentMessage = this.SendMessage(primaryDocuments, new List<NpoComputer.DCX.Common.IReglamentDocument>(), signatures, client, receiver, string.Empty, box, string.Empty);
      
      foreach (var ids in sentMessage.DocumentIds)
      {
        var doc = documents.Where(x => x.Id.ToString() == ids.LocalId).Single();
        /* Для основного документа признак Требуется подписание заполяется переданным в метод параметром.
         * Для приложений дополнительно проверить требование подписи.
         * Не требуется подпись для счет-фактур и исх. счетов.
         * Исключение СБИС - подпись на счет-фактуру требуется.
         */
        var isPrimaryDocument = doc.Id == document.Id;
        var needSignSentDocument = isPrimaryDocument || box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis ? needSign : isNeedSign(doc);
        doc.DeliveryMethod = Docflow.PublicFunctions.MailDeliveryMethod.Remote.GetExchangeDeliveryMethod();
        
        var info = SaveExternalDocumentInfo(doc, ids.ServiceId, sentMessage.ServiceMessageId, needSignSentDocument, receiver, box);
        var signature = this.GetDocumentSignature(doc, certificate);
        info.SenderSignId = signature.Id;
        
        var needUpdateSign = box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && !string.IsNullOrEmpty(signature.FormalizedPoAUnifiedRegNumber);
        if (needUpdateSign)
        {
          var patсhedSignature = sentMessage.Signatures.Single(s => string.Equals(s.DocumentId, doc.Id.ToString()));
          Docflow.PublicFunctions.Module.SetDataSignature(doc, signature.Id, patсhedSignature.Content);
        }
        
        var accountingDoc = Docflow.AccountingDocumentBases.As(doc);
        
        if (accountingDoc != null)
          accountingDoc.BusinessUnitBox = box;
        
        if (accountingDoc != null && accountingDoc.IsFormalized == true)
        {
          accountingDoc.ExchangeState = needSignSentDocument ? Docflow.OfficialDocument.ExchangeState.SignAwaited : Docflow.OfficialDocument.ExchangeState.Sent;
          accountingDoc.SellerSignatureId = this.GetDocumentSignature(accountingDoc, certificate).Id;
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(accountingDoc, accountingDoc.SellerTitleId.Value);
          Functions.Module.EnqueueXmlToPdfBodyConverter(accountingDoc, accountingDoc.SellerTitleId.Value, accountingDoc.ExchangeState);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(doc, info.VersionId.Value);
          Functions.Module.EnqueueXmlToPdfBodyConverter(doc, info.VersionId.Value, doc.ExchangeState);
        }
        MarkDocumentAsSended(info, doc, receiver, false, box, needSignSentDocument ? SignStatus.Waiting : SignStatus.None);
      }
    }

    /// <summary>
    /// Сохранить ИД документа в сервисе обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="serviceId">ИД в сервисе обмена.</param>
    /// <param name="messageId">ИД сообщения.</param>
    /// <param name="needSign">Признак требования подписания.</param>
    /// <param name="counterparty">Контрагент - получатель.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <returns>ExternalLink.</returns>
    protected static IExchangeDocumentInfo SaveExternalDocumentInfo(IOfficialDocument document, string serviceId, string messageId, bool needSign,
                                                                    ICounterparty counterparty, IBusinessUnitBox box)
    {
      var newInfo = ExchangeDocumentInfos.Create();
      
      newInfo.Document = document;
      newInfo.Box = box;
      newInfo.RootBox = box;
      newInfo.ServiceDocumentId = serviceId;
      newInfo.MessageType = Exchange.ExchangeDocumentInfo.MessageType.Outgoing;
      newInfo.ServiceMessageId = messageId;
      newInfo.Counterparty = counterparty;
      newInfo.MessageDate = Calendar.Now;
      newInfo.NeedSign = needSign;
      newInfo.VersionId = document.LastVersion.Id;
      
      newInfo.Save();
      return newInfo;
    }

    /// <summary>
    /// Получить подпись документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <returns>Подпись.</returns>
    protected virtual Structures.Module.Signature GetDocumentSignature(IOfficialDocument document, ICertificate certificate)
    {
      var version = document.LastVersion;
      if (version == null)
        return null;
      
      var keyFPoA = Docflow.PublicConstants.Module.UnsignedAdditionalInfoKeyFPoA + Docflow.PublicConstants.Module.UnsignedAdditionalInfoSeparator.KeyValue;
      var signature = Signatures.Get(version).Where(x => x.IsValid && x.SignCertificate != null)
        .Where(x => x.SignCertificate.Thumbprint.Equals(certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
        .OrderByDescending(x => !string.IsNullOrEmpty(x.UnsignedAdditionalInfo) && x.UnsignedAdditionalInfo.Contains(keyFPoA))
        .ThenByDescending(x => x.Id)
        .FirstOrDefault();
      
      if (signature == null)
        return null;
      
      var unifiedRegistrationNumber = Docflow.PublicFunctions.Module.GetUnsignedAttribute(signature, Docflow.PublicConstants.Module.UnsignedAdditionalInfoKeyFPoA);
      
      return Structures.Module.Signature.Create(signature.GetDataSignature(), signature.Id, unifiedRegistrationNumber);
    }

    /// <summary>
    /// Создать новый основной документ из документа RX.
    /// </summary>
    /// <param name="document">Документ RX.</param>
    /// <param name="needSign">Требуется подписание.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>Основной документ сервиса обмена.</returns>
    public virtual NpoComputer.DCX.Common.IDocument CreatePrimaryExchangeServiceDocument(IOfficialDocument document, bool needSign, string comment)
    {
      byte[] content;
      using (var memory = new System.IO.MemoryStream())
      {
        using (var sourceStream = document.LastVersion.Body.Read())
          sourceStream.CopyTo(memory);
        content = memory.ToArray();
      }
      
      var documentName = Functions.Module.GetExchangeDocumentName(document);
      documentName = Functions.Module.GetValidFileName(documentName);
      var fileName = string.Format("{0}.{1}", documentName, document.LastVersion.BodyAssociatedApplication.Extension);
      fileName = CommonLibrary.FileUtils.NormalizeFileName(fileName);
      
      return new NpoComputer.DCX.Common.Document()
      {
        ServiceEntityId = document.Id.ToString(),
        DocumentType = Functions.Module.GetDCXDocumentType(document),
        FileName = fileName,
        Content = content,
        NeedSign = needSign,
        Comment = comment,
        Date = document.DocumentDate.Value
      };
    }
    
    /// <summary>
    /// Получить имя документа для отправки в сервис обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Имя документа для отправки в сервис обмена.</returns>
    public virtual string GetExchangeDocumentName(IOfficialDocument document)
    {
      var extensionLength = document.LastVersion.BodyAssociatedApplication.Extension.Length;
      var documentNameMaxLength = Constants.Module.ExchangeDocumentMaxLength - extensionLength - 2;
      var documentName = document.Name;
      if (documentName.Length > documentNameMaxLength)
        documentName = string.Format("{0}~", document.Name.Substring(0, documentNameMaxLength));
      return documentName;
    }

    /// <summary>
    /// Создать подпись для документа обмена.
    /// </summary>
    /// <param name="exchangeProvider">Сервис обмена.</param>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="formalizedPoAUnifiedRegNumber">Единый регистрационный номер эл. доверенности.</param>
    /// <returns>Подпись сервиса обмена.</returns>
    protected static Signature CreateExchangeDocumentSignature(Enumeration? exchangeProvider, string documentId, byte[] signature, string formalizedPoAUnifiedRegNumber)
    {
      var needLink = exchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && !string.IsNullOrEmpty(formalizedPoAUnifiedRegNumber);
      
      return new NpoComputer.DCX.Common.Signature()
      {
        Content = signature,
        DocumentId = documentId,
        FormalizedPoAUnifiedRegNumber = formalizedPoAUnifiedRegNumber,
        FormalizedPoALink = needLink ? Functions.Module.GetFormalizedPoALink(formalizedPoAUnifiedRegNumber) : null,
        FormalizedPoALinkTitle = needLink ? Functions.Module.GetFormalizedPoALinkTitle(formalizedPoAUnifiedRegNumber) : null
      };
    }

    /// <summary>
    /// Создать новый регламентный документ из временного документа.
    /// </summary>
    /// <param name="document">Временный документ.</param>
    /// <param name="documentType">Тип регламентного документа.</param>
    /// <returns>Регламентный документ сервиса обмена.</returns>
    public virtual NpoComputer.DCX.Common.ReglamentDocument CreateReglamentExchangeServiceDocument(
      Sungero.Exchange.Structures.Module.ReglamentDocumentWithCertificate document,
      NpoComputer.DCX.Common.ReglamentDocumentType documentType)
    {
      var documentId = this.GetReglamentDocumentId(document, documentType);
      var stageId = this.GetReglamentDocumentStageId(document, documentType);

      if (string.IsNullOrEmpty(documentId))
        documentId = Guid.NewGuid().ToString();
      
      return new NpoComputer.DCX.Common.ReglamentDocument()
      {
        ServiceEntityId = documentId,
        DocumentType = documentType,
        FileName = document.Name,
        ParentServiceEntityId = document.ParentDocumentId,
        Content = document.Content,
        DocflowStageId = stageId
      };
    }
    
    /// <summary>
    /// Получить ИД регламентного документа на сервисе.
    /// </summary>
    /// <param name="document">Регламентный документ.</param>
    /// <param name="documentType">Тип регламентного документа.</param>
    /// <returns>ИД регламентного документа на сервисе.</returns>
    protected virtual string GetReglamentDocumentId(ReglamentDocumentWithCertificate document, ReglamentDocumentType documentType)
    {
      return this.GetSupportedReglamentDocumentTypes().Contains(documentType) ? document.Info.ExternalBuyerTitleId : document.ServiceDocumentId;
    }
    
    /// <summary>
    /// Получить ИД этапа регламентного документа на сервисе.
    /// </summary>
    /// <param name="document">Регламентный документ.</param>
    /// <param name="documentType">Тип регламентного документа.</param>
    /// <returns>ИД этапа регламентного документа на сервисе.</returns>
    protected virtual string GetReglamentDocumentStageId(ReglamentDocumentWithCertificate document, ReglamentDocumentType documentType)
    {
      return this.GetSupportedReglamentDocumentTypes().Contains(documentType) ? document.Info.StageId : document.ServiceDocumentStageId;
    }
    
    /// <summary>
    /// Получить тип документа в DCX.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Тип документа в DCX.</returns>
    public virtual NpoComputer.DCX.Common.DocumentType GetDCXDocumentType(IOfficialDocument document)
    {
      var documentType = NpoComputer.DCX.Common.DocumentType.Nonformalized;
      var accounting = Docflow.AccountingDocumentBases.As(document);
      
      if (accounting != null && accounting.FormalizedServiceType != null)
      {
        var exchangeService = accounting.BusinessUnitBox.ExchangeService.ExchangeProvider;
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act)
          documentType = DocumentType.Act;
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
        {
          if (accounting.IsAdjustment == true)
          {
            if (accounting.IsRevision == true)
            {
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf)
                if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
                  documentType = DocumentType.InvoiceCorrectionRevision;
                else
                  documentType = DocumentType.GeneralTransferSchfCorrectionRevisionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop)
                documentType = DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Dop)
                documentType = DocumentType.GeneralTransferDopCorrectionRevisionSeller;
            }
            else
            {
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf)
                if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
                  documentType = DocumentType.InvoiceCorrection;
                else
                  documentType = DocumentType.GeneralTransferSchfCorrectionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop)
                documentType = DocumentType.GeneralTransferSchfDopCorrectionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Dop)
                documentType = DocumentType.GeneralTransferDopCorrectionSeller;
            }
          }
          else
          {
            if (accounting.IsRevision == true)
            {
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf)
                if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
                  documentType = DocumentType.InvoiceRevision;
                else
                  documentType = DocumentType.GeneralTransferSchfRevisionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop)
                documentType = DocumentType.GeneralTransferSchfDopRevisionSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Dop)
                documentType = DocumentType.GeneralTransferDopRevisionSeller;
            }
            else
            {
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf)
                if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
                  documentType = DocumentType.Invoice;
                else
                  documentType = DocumentType.GeneralTransferSchfSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop)
                documentType = DocumentType.GeneralTransferSchfDopSeller;
              if (accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Dop)
                documentType = DocumentType.GeneralTransferDopSeller;
            }
          }
        }
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer)
          if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
            documentType = DocumentType.Waybill;
          else
            documentType = accounting.IsRevision == true ? DocumentType.GoodsTransferRevisionSeller : DocumentType.GoodsTransferSeller;
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice)
        {
          if (accounting.IsAdjustment == true)
          {
            if (accounting.IsRevision == true)
              documentType = DocumentType.InvoiceCorrectionRevision;
            else
              documentType = DocumentType.InvoiceCorrection;
          }
          else
          {
            if (accounting.IsRevision == true)
              documentType = DocumentType.InvoiceRevision;
            else
              documentType = DocumentType.Invoice;
          }
        }
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill)
          documentType = DocumentType.Waybill;
        if (accounting.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
          if (exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
            documentType = DocumentType.Act;
          else
            documentType = accounting.IsRevision == true ? DocumentType.WorksTransferRevisionSeller : DocumentType.WorksTransferSeller;
      }
      return documentType;
    }
    
    #endregion

    #region Генерация титула
    
    /// <summary>
    /// Сгенерировать титул покупателя.
    /// </summary>
    /// <param name="statement">Документ.</param>
    /// <param name="buyerTitle">Структура с данными для генерации титула.</param>
    [Public]
    public virtual void GenerateBuyerTitle(Docflow.IAccountingDocumentBase statement, Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      if (statement.IsFormalized != true)
        return;
      
      byte[] sellerTitle;
      using (var memory = new System.IO.MemoryStream())
      {
        statement.Versions.Single(v => v.Id == statement.SellerTitleId).Body.Read().CopyTo(memory);
        sellerTitle = memory.ToArray();
      }
      
      var exchangeInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(statement);
      if (exchangeInfo == null)
        return;
      
      var client = GetClient(ExchangeCore.PublicFunctions.BoxBase.GetRootBox(exchangeInfo.Box));
      
      var title = new NpoComputer.DCX.Common.BuyerTitle();
      title.AcceptanceDate = buyerTitle.AcceptanceDate.Value;
      title.OrganizationName = statement.BusinessUnit.LegalName;
      title.ActOfDisagreement = this.GetActOfDisagreementText(buyerTitle);
      title.SignResult = this.GetBuyerTitleSignResult(buyerTitle);
      title.Signer.FirstName = buyerTitle.Signatory.Person.FirstName;
      title.Signer.LastName = buyerTitle.Signatory.Person.LastName;
      title.Signer.MiddleName = buyerTitle.Signatory.Person.MiddleName;
      title.Signer.JobTitle = this.GetBuyerSignatoryJobTitle(buyerTitle);
      title.Signer.TIN = statement.BusinessUnit.TIN;
      title.Signer.SignerPowers = Functions.Module.GetSignerPowers(buyerTitle.SignatoryPowers);
      title.Signer.PowersBase = buyerTitle.SignatoryPowersBase;
      
      title.SellerTitle = sellerTitle;
      if (statement.IsAdjustment == true)
      {
        title.DocumentTypeNamedId = statement.IsRevision == true ? Constants.Module.DocumentTypeNamedId.UniversalCorrectionDocumentRevision
          : Constants.Module.DocumentTypeNamedId.UniversalCorrectionDocument;
        title.DocumentVersion = Constants.Module.UCDVersion;
      }
      
      if (buyerTitle.Consignee != null)
      {
        title.Consignee = new Consignee();
        title.Consignee.FirstName = buyerTitle.Consignee.Person.FirstName;
        title.Consignee.LastName = buyerTitle.Consignee.Person.LastName;
        title.Consignee.MiddleName = buyerTitle.Consignee.Person.MiddleName;
        title.Consignee.JobTitle = this.GetBuyerConsigneeJobTitle(buyerTitle);
        title.Consignee.PowersBase = buyerTitle.ConsigneePowersBase;
        
        this.FillAttorney(title.Consignee, buyerTitle.ConsigneePowerOfAttorney, buyerTitle.ConsigneeOtherReason);
      }
      
      FileFromService xml = null;
      
      var signResultAccepted = buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
      
      if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
      {
        title.OperationContent = signResultAccepted ? "Результаты работ (оказанных услуг) приняты без претензий" : title.ActOfDisagreement;
        this.LogDebugFormat(exchangeInfo, "Start GenerateWorksTransferXmlForBuyer.");
        xml = client.GenerateWorksTransferXmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId);
      }
      
      if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act)
      {
        title.OperationContent = signResultAccepted ? "Услуги оказаны в полном объеме" : title.ActOfDisagreement;
        this.LogDebugFormat(exchangeInfo, "Start GenerateActXmlForBuyer.");
        xml = client.GenerateActXmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId);
      }
      
      if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer &&
          statement.FormalizedFunction != Docflow.AccountingDocumentBase.FormalizedFunction.Schf)
      {
        if (statement.IsAdjustment != true)
          title.OperationContent = string.IsNullOrEmpty(buyerTitle.ActOfDisagreement) ? this.GetBuyerAcceptanceStatusText(buyerTitle) : title.ActOfDisagreement;
        else
          title.OperationContent = "С изменением стоимости согласен";
        
        this.LogDebugFormat(exchangeInfo, "Start {0}.", statement.IsAdjustment == true ? "GenerateUniversalTransferCorrectionDocumentXmlForBuyer" : "GenerateUniversalTransferDocumentXmlForBuyer");
        xml = statement.IsAdjustment == true ?
          client.GenerateUniversalTransferCorrectionDocumentXmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId) :
          client.GenerateUniversalTransferDocumentXmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId);
      }
      
      if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer)
      {
        title.OperationContent = signResultAccepted ? "Перечисленные в документе ценности приняты без претензий" : title.ActOfDisagreement;
        this.LogDebugFormat(exchangeInfo, "Start GenerateGoodsTransferXmlForBuyer.");
        xml = client.GenerateGoodsTransferXmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId);
      }
      
      if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill)
      {
        title.OperationContent = signResultAccepted ? "Товары переданы" : title.ActOfDisagreement;
        this.LogDebugFormat(exchangeInfo, "Start GenerateTorg12XmlForBuyer.");
        xml = client.GenerateTorg12XmlForBuyer(title, exchangeInfo.ServiceMessageId, exchangeInfo.ServiceDocumentId);
      }
      
      if (xml != null)
      {
        using (var memory = new System.IO.MemoryStream(xml.Content))
        {
          if (!HasUnsignedBuyerTitle(statement))
          {
            // При создании версии чистится статус эл. обмена, восстанавливаем его.
            var exchangeState = statement.ExchangeState;
            statement.CreateVersion();
            statement.ExchangeState = exchangeState;
          }
          
          var version = statement.LastVersion;
          version.AssociatedApplication = GetOrCreateAssociatedApplicationByDocumentName("file.xml");
          version.Note = FinancialArchive.Resources.BuyerTitleVersionNote;
          statement.BuyerTitleId = version.Id;
          statement.OurSignatory = buyerTitle.Signatory;
          statement.OurSigningReason = buyerTitle.SignatureSetting;
          version.Body.Write(memory);
          statement.Save();
        }
        
        // Сохранить ИД титула покупателя и ИД этапа. ID передается для СБИС, для SD и Диадок - пустое значение.
        exchangeInfo.ExternalBuyerTitleId = xml.ServiceDocumentId;
        exchangeInfo.StageId = xml.StageId;
        
        if (statement.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer && statement.IsAdjustment != true)
          exchangeInfo.BuyerAcceptanceStatus = buyerTitle.BuyerAcceptanceStatus;
      }
    }
    
    /// <summary>
    /// Получить текст разногласий.
    /// </summary>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <returns>Текст разногласий.</returns>
    protected virtual string GetActOfDisagreementText(Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      var actOfDisagreementText = !string.IsNullOrEmpty(buyerTitle.ActOfDisagreement) ? ": " + buyerTitle.ActOfDisagreement : string.Empty;
      if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted)
        return "Принято с разногласиями" + actOfDisagreementText;
      else if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        return "Не принято" + actOfDisagreementText;
      else
        return string.Empty;
    }
    
    /// <summary>
    /// Получить текст расшифровки кода итога.
    /// </summary>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <returns>Текст расшифровки кода итога.</returns>
    protected virtual string GetBuyerAcceptanceStatusText(Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted)
        return "Товары (работы, услуги, права) приняты без расхождений (претензий)";
      else if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted)
        return "Товары (работы, услуги, права) приняты с расхождениями (претензией)";
      else if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        return "Товары (работы, услуги, права) не приняты";

      return string.Empty;
    }
    
    /// <summary>
    /// Получить результат приемки для титула покупателя.
    /// </summary>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <returns>Результат приемки в DCX.</returns>
    protected virtual SignResult GetBuyerTitleSignResult(Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted)
        return SignResult.Signed;
      else if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted)
        return SignResult.SignedWithAct;
      else if (buyerTitle.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        return SignResult.NotAccepted;
      
      throw AppliedCodeException.Create(string.Format("Unsupported BuyerAcceptanceStatus: '{0}'.", buyerTitle.BuyerAcceptanceStatus));
    }
    
    /// <summary>
    /// Получить наименование должности грузополучателя для титула покупателя.
    /// </summary>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <returns>Наименование должности.</returns>
    public virtual string GetBuyerConsigneeJobTitle(Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      if (buyerTitle == null)
        return null;
      
      var signatoryJobTitle = this.GetBuyerSignatoryJobTitle(buyerTitle);
      var consigneeJobTitle = buyerTitle.Consignee.JobTitle != null ? buyerTitle.Consignee.JobTitle.Name : null;
      return Docflow.PublicFunctions.Module.CutText(buyerTitle.Signatory == buyerTitle.Consignee ? signatoryJobTitle : consigneeJobTitle,
                                                    Docflow.PublicConstants.AccountingDocumentBase.JobTitleMaxLength);
    }
    
    /// <summary>
    /// Получить наименование должности подписанта для титула покупателя.
    /// </summary>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <returns>Наименование должности.</returns>
    public virtual string GetBuyerSignatoryJobTitle(Docflow.Structures.AccountingDocumentBase.IBuyerTitle buyerTitle)
    {
      if (buyerTitle == null)
        return null;
      
      var settingJobTitle = buyerTitle.SignatureSetting != null && buyerTitle.SignatureSetting.JobTitle != null ? buyerTitle.SignatureSetting.JobTitle.Name : null;
      var signatoryJobTitle = buyerTitle.Signatory.JobTitle != null ? buyerTitle.Signatory.JobTitle.Name : null;
      return Docflow.PublicFunctions.Module.CutText(settingJobTitle != null ? settingJobTitle : signatoryJobTitle,
                                                    Docflow.PublicConstants.AccountingDocumentBase.JobTitleMaxLength);
    }
    
    /// <summary>
    /// Заполнить информацию о подписанте.
    /// </summary>
    /// <param name="title">Титул покупателя.</param>
    /// <param name="consignee">Подписывающий.</param>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <param name="otherReason">Основание подписания.</param>
    [Obsolete("Используйте метод FillAttorney без параметра Титул покупателя.")]
    protected virtual void FillAttorney(BuyerTitle title, Consignee consignee, IPowerOfAttorneyBase powerOfAttorney, string otherReason)
    {
      this.FillAttorney(consignee, powerOfAttorney, otherReason);
    }
    
    /// <summary>
    /// Заполнить информацию о подписанте.
    /// </summary>
    /// <param name="consignee">Подписывающий.</param>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <param name="otherReason">Основание подписания.</param>
    protected virtual void FillAttorney(Consignee consignee, IPowerOfAttorneyBase powerOfAttorney, string otherReason)
    {
      if (consignee == null)
        return;
      
      if (powerOfAttorney != null)
      {
        consignee.PowersBase = Docflow.SignatureSettings.Info.Properties.Reason.GetLocalizedValue(Docflow.SignatureSetting.Reason.PowerOfAttorney);
        
        var number = string.Empty;
        if (Docflow.FormalizedPowerOfAttorneys.Is(powerOfAttorney))
          number = Docflow.FormalizedPowerOfAttorneys.As(powerOfAttorney).UnifiedRegistrationNumber;
        else
          number = powerOfAttorney.RegistrationNumber;
        
        if (!string.IsNullOrWhiteSpace(number))
          consignee.PowersBase += Docflow.OfficialDocuments.Resources.Number + number;
        
        if (powerOfAttorney.RegistrationDate != null)
          consignee.PowersBase += Docflow.OfficialDocuments.Resources.DateFrom + powerOfAttorney.RegistrationDate.Value.ToString("d");
      }
      else if (!string.IsNullOrWhiteSpace(otherReason))
        consignee.PowersBase = otherReason;
    }

    /// <summary>
    /// Заполнить информацию о подписанте.
    /// </summary>
    /// <param name="consignee">Подписывающий.</param>
    /// <param name="signatureSetting">Право подписи.</param>
    protected virtual void FillSignerPowersBase(Consignee consignee, Docflow.ISignatureSetting signatureSetting)
    {
      if (consignee == null || signatureSetting == null)
        return;
      
      consignee.PowersBase = Docflow.PublicFunctions.Module.GetPowersBase(signatureSetting);
    }
    
    /// <summary>
    /// Проверка наличия неподписанного титула покупателя.
    /// </summary>
    /// <param name="statement">Документ.</param>
    /// <returns>Признак наличия неподписанного титула покупателя.</returns>
    [Public, Remote]
    public static bool HasUnsignedBuyerTitle(Docflow.IAccountingDocumentBase statement)
    {
      if (statement.BuyerTitleId != null)
      {
        var existingBuyerTitle = statement.Versions.Where(x => x.Id == statement.BuyerTitleId).FirstOrDefault();
        if (existingBuyerTitle != null && !Signatures.Get(existingBuyerTitle).Any())
          return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Проверка, запрошено ли УОУ контрагентом.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если запрошено УОУ.</returns>
    [Public, Remote, Obsolete("Теперь функция не актуальна, т.к. реализована поддержка частичного подписания.")]
    public virtual bool IsInvoiceAmendmentRequest(IOfficialDocument document)
    {
      var documentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      if (documentInfo == null)
        return false;
      
      var serviceDocuments = documentInfo.ServiceDocuments
        .Where(x => x.Date != null && (x.DocumentType == ExchDocumentType.Reject || x.DocumentType == ExchDocumentType.IReject))
        .OrderByDescending(x => x.Date);
      return serviceDocuments.Any();
    }
    
    /// <summary>
    /// Проверка возможности отправки ответной подписи по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True - если можно отправить подпись, иначе  - false.</returns>
    [Public, Remote]
    public virtual bool CanSendSign(IOfficialDocument document)
    {
      var documentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      if (documentInfo == null)
        return false;
      
      var client = GetClient(documentInfo.RootBox);
      var allowedAnswers = client.GetAllowedAnswers(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId, documentInfo.ExternalBuyerTitleId);
      return allowedAnswers.CanSendSign;
    }
    
    /// <summary>
    /// Получить область полномочий.
    /// </summary>
    /// <param name="authority">Полномочие.</param>
    /// <returns>Область полномочий.</returns>
    public virtual NpoComputer.DCX.Common.SignerPowers GetSignerPowers(string authority)
    {
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchf)
        return SignerPowers.InvoiceSigner;
      
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register)
        return SignerPowers.PersonDocumentedOperation;
      
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Deal)
        return SignerPowers.PersonMadeOperation;
      
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister)
        return SignerPowers.MadeAndSignOperation;
      
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchfAndRegister)
        return SignerPowers.ResponsibleForOperationAndSignerForInvoice;
      
      if (authority == Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Other)
        return SignerPowers.Other;
      
      throw AppliedCodeException.Create(Sungero.Exchange.Resources.NotFoundAuthority);
    }
    
    #endregion
    
    #region Генерация извещений о получении
    
    /// <summary>
    /// Получить сгенерированные извещения о получении.
    /// </summary>
    /// <param name="documentInfos">Информация о документах МКДО.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="certificate">Сертификат для подписания ИОП.</param>
    /// <param name="generateServiceDocuments">Сгенерировать ИОП.</param>
    /// <returns>Извещения о получении и сертификат, которым они должны быть подписаны.</returns>
    [Remote]
    public virtual System.Collections.Generic.List<Structures.Module.ReglamentDocumentWithCertificate> GetGeneratedDeliveryConfirmationDocuments(List<Exchange.IExchangeDocumentInfo> documentInfos,
                                                                                                                                                 ExchangeCore.IBusinessUnitBox box, ICertificate certificate, bool generateServiceDocuments)
    {
      if (certificate == null)
        throw AppliedCodeException.Create(Resources.CertificateNotFound);
      
      this.LogDebugFormat(box, "Execute GetGeneratedDeliveryConfirmationDocuments.");
      var processedMessagesId = new List<string>();
      var client = GetClient(box);
      var documents = new List<Structures.Module.ReglamentDocumentWithCertificate>();
      foreach (var documentInfo in documentInfos)
      {
        this.LogDebugFormat(documentInfo, "Execute GetGeneratedDeliveryConfirmationDocuments. Processing document info.");
        // Удаляем ранее сгенерированные ИОП, если они сгенерированы под другой сертификат.
        if (generateServiceDocuments)
        {
          var serviceDocsList = documentInfo.ServiceDocuments.Where(d => d.Date == null && !Equals(certificate, d.Certificate) && d.Sign == null).ToList();
          foreach (var serviceDoc in serviceDocsList)
          {
            documentInfo.ServiceDocuments.Remove(serviceDoc);
            this.LogDebugFormat(documentInfo, "Execute GetGeneratedDeliveryConfirmationDocuments. Delete old receipts for info.");
          }
          documentInfo.Save();
        }
        
        var isInvoiceFlow = Functions.Module.IsInvoiceFlowDocument(documentInfo.Document);
        // Извещение о получении документа.
        if (documentInfo.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Incoming &&
            !documentInfo.ServiceDocuments.Any(d => (d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt ||
                                                     d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt) && d.Date != null))
        {
          var documentType = isInvoiceFlow ?
            Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt :
            Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt;
          var generated = documentInfo.ServiceDocuments
            .FirstOrDefault(d => d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt ||
                            d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt);
          
          byte[] content = null;
          string name = string.Empty;
          byte[] signature = null;
          var serviceDocumentId = string.Empty;
          var serviceDocumentStageId = string.Empty;
          var receipts = new List<IReglamentDocument>();
          if (generated == null && generateServiceDocuments)
          {
            var canSendDeliveryConfirmation = false;
            try
            {
              canSendDeliveryConfirmation = client.CanSendDeliveryConfirmation(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
              this.LogDebugFormat(documentInfo, "Execute GetGeneratedDeliveryConfirmationDocuments. CanSendDeliveryConfirmation = '{0}' for info.", canSendDeliveryConfirmation);
            }
            catch (Exception ex)
            {
              this.LogDebugFormat(documentInfo, "Execute GetGeneratedDeliveryConfirmationDocuments. Error while getting document from the service to generate delivery confirmation: {0}.",
                                  ex.Message);
            }
            
            // Для СБИС хранится составной ServiceDocumentId, первая часть которого - ИД сообщения, вторая - ИД документа.
            // ИОПы необходимо отправлять одним сообщением на весь комплект документов.
            if (canSendDeliveryConfirmation && !processedMessagesId.Contains(documentInfo.ServiceDocumentId.Split('#').First()))
              receipts = this.GetReglamentDocuments(documentInfo, certificate, client);
          }
          else if (generated != null && Equals(generated.Certificate, certificate))
          {
            this.LogDebugFormat(documentInfo, "Execute GetGeneratedDeliveryConfirmationDocuments. Get reglament documents from system for info.");
            content = generated.Body;
            name = generated.GeneratedName;
            signature = generated.Sign;
            serviceDocumentId = generated.DocumentId;
            serviceDocumentStageId = generated.StageId;
          }
          
          if (content != null)
          {
            var serviceDocument = Structures.Module.ReglamentDocumentWithCertificate.Create(name, content, certificate, signature, documentInfo.ServiceDocumentId,
                                                                                            box, documentInfo.Document,
                                                                                            documentInfo.ServiceMessageId, serviceDocumentId, serviceDocumentStageId,
                                                                                            documentInfo.ServiceCounterpartyId, true, documentInfo, isInvoiceFlow,
                                                                                            documentType, null);
            documents.Add(serviceDocument);
          }
          
          if (receipts.Any())
          {
            foreach (var receipt in receipts)
            {
              // Получить тип служебного документа заново, т.к. по комплектам СБИСа ИОПы приходят пачкой на одну инфошку из комплекта.
              var rootDocumentInfo = documentInfos.Where(info => Equals(info.ServiceDocumentId, receipt.RootServiceEntityId)).FirstOrDefault();
              if (rootDocumentInfo != null)
                isInvoiceFlow = Functions.Module.IsInvoiceFlowDocument(rootDocumentInfo.Document);
              if (receipt.DocumentType == ReglamentDocumentType.Receipt)
                documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt;
              if (receipt.DocumentType == ReglamentDocumentType.InvoiceReceipt)
                documentType = Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt;
              
              content = receipt.Content;
              name = receipt.FileName;
              serviceDocumentId = receipt.ServiceEntityId;
              serviceDocumentStageId = receipt.DocflowStageId;
              
              this.LogDebugFormat(documentInfo,
                                  "Execute GetGeneratedDeliveryConfirmationDocuments. Generate receipt notification with DocumentType = {0}, ServiceCounterpartyId = {1}, ServiceEntityId = {2}, serviceDocumentStageId = {3}.",
                                  documentType, documentInfo.ServiceCounterpartyId, serviceDocumentId, serviceDocumentStageId);
              
              var parentDocumentInfo = documentInfos.Where(i => i.ServiceDocumentId == receipt.ParentServiceEntityId).FirstOrDefault();
              // Для письма СБИС есть 2 сущности: текстовое сообщение в XML и тело документа.
              // Текстовое сообщение не загружается в RX, но на него сервис генерирует ИОП.
              if (parentDocumentInfo != null)
              {
                var serviceDocument = Structures.Module.ReglamentDocumentWithCertificate.Create(name, content, certificate, signature, parentDocumentInfo.ServiceDocumentId, box, parentDocumentInfo.Document,
                                                                                                parentDocumentInfo.ServiceMessageId, serviceDocumentId, serviceDocumentStageId, parentDocumentInfo.ServiceCounterpartyId,
                                                                                                true, parentDocumentInfo, isInvoiceFlow, documentType, null);
                documents.Add(serviceDocument);
              }
            }
          }
        }
        processedMessagesId.Add(documentInfo.ServiceDocumentId.Split('#').First());
      }
      return documents;
    }
    
    /// <summary>
    /// Получить служебные документы с сервиса обмена.
    /// </summary>
    /// <param name="documentInfo">Информация о документе обмена.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="client">Клиент.</param>
    /// <returns>Список служебных документов.</returns>
    private List<IReglamentDocument> GetReglamentDocuments(IExchangeDocumentInfo documentInfo, ICertificate certificate, DcxClient client)
    {
      this.LogDebugFormat(documentInfo, "Execute GetReglamentDocuments. Try get reglament documents from service for info.");
      bool isDocflowFinished = false;
      var dcxDocument = new NpoComputer.DCX.Common.Document();
      dcxDocument.ServiceMessageId = documentInfo.ServiceMessageId;
      dcxDocument.ServiceEntityId = documentInfo.ServiceDocumentId;
      dcxDocument.DocumentType = NpoComputer.DCX.Common.DocumentType.Nonformalized;
      var accountingDocument = Docflow.AccountingDocumentBases.As(documentInfo.Document);
      
      if (accountingDocument != null)
      {
        var exchangeService = accountingDocument.BusinessUnitBox.ExchangeService.ExchangeProvider;
        
        if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer &&
            exchangeService != ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
          dcxDocument.DocumentType = NpoComputer.DCX.Common.DocumentType.WorksTransferSeller;
        
        if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act ||
            exchangeService == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc &&
            accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer)
          dcxDocument.DocumentType = NpoComputer.DCX.Common.DocumentType.Act;
        
        if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer &&
            exchangeService != ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
          dcxDocument.DocumentType = NpoComputer.DCX.Common.DocumentType.GoodsTransferSeller;
        
        if (accountingDocument.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer)
          dcxDocument.DocumentType = accountingDocument.IsAdjustment == true ?
            NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopCorrectionSeller :
            NpoComputer.DCX.Common.DocumentType.GeneralTransferSchfDopSeller;
      }
      
      var signerInfo = NpoComputer.DCX.Common.SignerInfo.CreateFromSignature(certificate.X509Certificate);
      return client.GetNextReglamentDocuments(dcxDocument, signerInfo, out isDocflowFinished);
    }
    
    /// <summary>
    /// Сгенерировать уведомление об уточнении.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="note">Комментарий.</param>
    /// <param name="throwError">Не гасить ошибку.</param>
    /// <param name="certificate">Сертификат для подписания УОУ.</param>
    /// <param name="sendInvoiceAmendmentRequest">True для УОУ, False для отказа.</param>
    /// <returns>Уведомления об уточнении и сертификат, которым они должны быть подписаны.</returns>
    [Remote]
    public virtual List<Structures.Module.ReglamentDocumentWithCertificate> GenerateAmendmentRequestDocuments(List<Docflow.IOfficialDocument> documents,
                                                                                                              ExchangeCore.IBoxBase box, string note,
                                                                                                              bool throwError, ICertificate certificate,
                                                                                                              bool sendInvoiceAmendmentRequest)
    {
      if (box == null)
        throw AppliedCodeException.Create(Resources.BoxIsNotValid);
      
      if (certificate == null)
        throw AppliedCodeException.Create(Resources.CertificateNotFound);
      
      var docsWithCertificates = new List<Structures.Module.ReglamentDocumentWithCertificate>();
      foreach (var document in documents)
      {
        bool packageProcessing = false;
        try
        {
          var externalDocumentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(document);
          if (externalDocumentInfo == null)
            continue;
          
          var client = GetClient(ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box));
          var allowedAnswers = client.GetAllowedAnswers(externalDocumentInfo.ServiceDocumentId, externalDocumentInfo.ServiceMessageId, externalDocumentInfo.ExternalBuyerTitleId);
          
          // Убеждаемся, что можно отправить хоть что-то.
          if (!allowedAnswers.CanSendInvoiceAmendmentRequest && !allowedAnswers.CanSendAmendmentRequest)
            throw AppliedCodeException.Create(Resources.AnswerIsAlreadySent);
          
          // Если нельзя отправить УОУ - отправляем отказ и наоборот.
          var isInvoiceAmendmentRequest = sendInvoiceAmendmentRequest;
          if (isInvoiceAmendmentRequest && !allowedAnswers.CanSendInvoiceAmendmentRequest)
            isInvoiceAmendmentRequest = false;
          if (!isInvoiceAmendmentRequest && !allowedAnswers.CanSendAmendmentRequest)
            isInvoiceAmendmentRequest = true;
          
          var receipts = new List<NpoComputer.DCX.Common.IReglamentDocument>();
          var rootBox = ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
          var packageDocumentsExchangeInfos = GetPackageDocumentsExchangeInfos(externalDocumentInfo.ServiceMessageId);
          var documentIds = documents.Select(d => d.Id);
          packageDocumentsExchangeInfos = packageDocumentsExchangeInfos.Where(info => documentIds.Contains(info.Document.Id)).ToList();
          var invoiceExchangeInfoIds = new List<int>();
          if (rootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis &&
              packageDocumentsExchangeInfos.Count > 1)
          {
            var tempDocs = new List<NpoComputer.DCX.Common.IDocument>();
            foreach (IExchangeDocumentInfo info in packageDocumentsExchangeInfos)
            {
              var allowedAnswersSbis = client.GetAllowedAnswers(info.ServiceDocumentId, info.ServiceMessageId, info.ExternalBuyerTitleId);
              if (allowedAnswersSbis.CanSendAmendmentRequest || allowedAnswersSbis.CanSendInvoiceAmendmentRequest)
              {
                tempDocs.Add(new NpoComputer.DCX.Common.Document()
                             {
                               ServiceMessageId = info.ServiceMessageId,
                               ServiceEntityId = info.ServiceDocumentId
                             });
                if (allowedAnswersSbis.CanSendInvoiceAmendmentRequest == true)
                  invoiceExchangeInfoIds.Add(info.Id);
              }
            }
            // Для Сбиса передаем сразу сертификат, чтобы не искать в хранилище по отпечатку.
            receipts = client.GenerateInvoiceAmendmentRequestsForPackage(tempDocs, certificate.X509Certificate, note);
            packageProcessing = true;
          }
          else
          {
            var tempDoc = new NpoComputer.DCX.Common.Document()
            {
              ServiceMessageId = externalDocumentInfo.ServiceMessageId,
              ServiceEntityId = externalDocumentInfo.ServiceDocumentId
            };

            // Для Сбиса передаем сразу сертификат, чтобы не искать в хранилище по отпечатку.
            // Для Сбиса генерируем всегда только уведомление об уточнении, т.к. нет разделения между отказом и уведомлением об уточнении.
            if (externalDocumentInfo.RootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
              receipts.Add(client.GenerateInvoiceAmendmentRequest(tempDoc, certificate.X509Certificate, note));
            else
            {
              if (isInvoiceAmendmentRequest)
                receipts.Add(client.GenerateInvoiceAmendmentRequest(tempDoc, certificate.Thumbprint, note));
              else
                receipts.Add(client.GenerateAmendmentRequest(tempDoc, note, certificate.Thumbprint));
            }
          }
          
          foreach (NpoComputer.DCX.Common.IReglamentDocument receipt in receipts)
          {
            var exchangeDocumentInfo = ExchangeDocumentInfos.GetAll().Where(i => i.ServiceDocumentId == receipt.ParentServiceEntityId && Equals(i.RootBox, box)).First();
            var exchangeDocument = OfficialDocuments.Get(exchangeDocumentInfo.Document.Id);
            var formalizedPoA = Docflow.PublicFunctions.OfficialDocument.GetFormalizedPoA(exchangeDocument, Employees.Current, certificate);
            
            if (packageProcessing)
              isInvoiceAmendmentRequest = invoiceExchangeInfoIds.Contains(exchangeDocumentInfo.Id);
            var documentType = isInvoiceAmendmentRequest ? Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReject : Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Reject;
            var doc = Structures.Module.ReglamentDocumentWithCertificate.Create(receipt.FileName, receipt.Content, certificate,
                                                                                null, exchangeDocumentInfo.ServiceDocumentId,
                                                                                ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box),
                                                                                exchangeDocument, exchangeDocumentInfo.ServiceMessageId, receipt.ServiceEntityId, receipt.DocflowStageId,
                                                                                exchangeDocumentInfo.ServiceCounterpartyId, false,
                                                                                exchangeDocumentInfo, isInvoiceAmendmentRequest, documentType, formalizedPoA?.UnifiedRegistrationNumber);
            docsWithCertificates.Add(doc);
          }
        }
        catch (AppliedCodeException ex)
        {
          // Гасить исключение, если операция недоступна в сервисе.
          if (ex.Message != Resources.AnswerIsAlreadySent || throwError)
            throw;
        }
        catch (Exception ex)
        {
          if (ex is CommonLibrary.Exceptions.PlatformException)
            throw;
          
          throw AppliedCodeException.Create(Resources.AmendmentRequestError);
        }
        
        if (packageProcessing)
          break;
      }
      return docsWithCertificates;
    }
    
    /// <summary>
    /// Сгенерировать извещение о получении на служебный документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация.</param>
    /// <param name="client">Dcx клиент.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <param name="documentType">Тип служебного документа, на который генерируется ИОП.</param>
    /// <param name="certificate">Сертификат.</param>
    /// <param name="reglamentDocumentType">Тип служебного документа, который будет сгенерирован.</param>
    /// <param name="generateServiceDocuments">Перегенерировать ИОП.</param>
    /// <returns>Извещение о получении на служебный документ.</returns>
    protected virtual ReglamentDocumentWithCertificate GenerateInvoiceServiceDeliveryConfirmation(IOfficialDocument document,
                                                                                                  IExchangeDocumentInfo documentInfo, DcxClient client, IBusinessUnitBox box, Enumeration documentType,
                                                                                                  ICertificate certificate, Enumeration reglamentDocumentType, bool generateServiceDocuments)
    {
      var parentServiceDocument = documentInfo.ServiceDocuments.First(d => d.DocumentType == documentType);
      var generatedDocument = documentInfo.ServiceDocuments.FirstOrDefault(d => d.DocumentType == reglamentDocumentType);
      byte[] content = null;
      var name = string.Empty;
      byte[] signature = null;
      var serviceDocumentId = string.Empty;
      var serviceDocumentStageId = string.Empty;
      if (generatedDocument == null && generateServiceDocuments)
      {
        // Для Сбиса передаем сразу сертификат, чтобы не искать в хранилище по отпечатку.
        var receipt = box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis ?
          client.GenerateInvoiceDeliveryConfirmation(parentServiceDocument.DocumentId, certificate.X509Certificate, documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId) :
          client.GenerateInvoiceDeliveryConfirmation(parentServiceDocument.DocumentId, certificate.Thumbprint, documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        content = receipt.Content;
        name = receipt.FileName;
        serviceDocumentId = receipt.ServiceEntityId;
        serviceDocumentStageId = receipt.DocflowStageId;
        Logger.DebugFormat("Generate receipt notification with ExchangeDocumentInfoId = {0}, DocumentType = {1}, CounterpartyId = {2}," +
                           " ParentDocumentId = {3} LinkedDocumentId = {4}, ServiceMessageId = {5}, serviceDocumentId = {6}, serviceDocumentStageId = {7}",
                           documentInfo.Id, reglamentDocumentType, parentServiceDocument.CounterpartyId, parentServiceDocument.DocumentId,
                           document.Id, documentInfo.ServiceMessageId, serviceDocumentId, serviceDocumentStageId);
      }
      else if (generatedDocument != null && Equals(generatedDocument.Certificate, certificate))
      {
        content = generatedDocument.Body;
        name = generatedDocument.GeneratedName;
        signature = generatedDocument.Sign;
      }

      return content != null ?
        Structures.Module.ReglamentDocumentWithCertificate.Create(name, content, certificate, signature, parentServiceDocument.DocumentId, box, document,
                                                                  documentInfo.ServiceMessageId, serviceDocumentId, serviceDocumentStageId, parentServiceDocument.CounterpartyId, false, documentInfo,
                                                                  true, reglamentDocumentType, null)
        : null;
    }
    
    #endregion

    #region Отправка документов через диалог

    /// <summary>
    /// Формирование вспомогательной информации о документе для отправки контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Структура с дополнительной информацией.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.Module.SendToCounterpartyInfo GetInfoForSendToCounterparty(Docflow.IOfficialDocument document)
    {
      var result = Structures.Module.SendToCounterpartyInfo.Create();
      if (document == null)
        return result;
      
      result.CanApprove = !Docflow.PublicFunctions.OfficialDocument.Remote.GetApprovalValidationErrors(document, true).Any();
      result.HasApprovalSignature = !HasNotApprovedDocuments(document);
      
      var businessUnit = document.BusinessUnit;
      result.HasError = false;
      result.Counterparties = new List<Parties.ICounterparty>();
      
      var exchangeDocumentInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(document);
      result.IsSignedByCounterparty = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
      result.BuyerAcceptanceStatus = exchangeDocumentInfo?.BuyerAcceptanceStatus;

      // Нельзя отправлять уже отправленные формализованные документы.
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      if (accountingDocument != null && accountingDocument.IsFormalized == true)
      {
        var lastDocumentInfo = Functions.ExchangeDocumentInfo.GetLastDocumentInfo(document);
        if (lastDocumentInfo != null && lastDocumentInfo.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Outgoing)
        {
          result.Error = Exchange.Resources.DocumentIsAlreadySentToCounterparty;
          result.HasError = true;
          return result;
        }
      }
      
      DocumentAllowedAnswer documentAllowedAnswer = null;
      if (document.Versions.Count > 1 && exchangeDocumentInfo != null)
      {
        var hasSign = Signatures.Get(document.Versions.OrderBy(v => v.Number).First()).Any(x => x.SignatureType == SignatureType.Approval && x.IsExternal == true);
        if (hasSign)
        {
          var firstBox = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes()
            .Where(a => Equals(a, exchangeDocumentInfo.RootBox)).SingleOrDefault();
          if (firstBox != null)
          {
            var client = GetClient(firstBox);
            documentAllowedAnswer = client.GetAllowedAnswers(exchangeDocumentInfo.ServiceDocumentId, exchangeDocumentInfo.ServiceMessageId, exchangeDocumentInfo.ExternalBuyerTitleId);
            result.NeedRejectFirstVersion = documentAllowedAnswer.CanSendAmendmentRequest;
          }
        }
      }
      
      result = this.FillCounterpartyInfo(document, businessUnit, result);
      if (result.HasError)
        return result;

      var allBoxes = GetAllExchangeBoxesToCounterparty(document, result.Counterparties);
      result.Boxes = allBoxes
        .Where(b => b.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        .ToList();
      if (!result.Boxes.Any())
      {
        if (!allBoxes.Any())
          result.Error = Resources.BoxesNotFound;
        else
          result.Error = Resources.BoxesNotConnected;
        
        result.HasError = true;
        return result;
      }
      
      result = this.FillSignByCounterparty(document, exchangeDocumentInfo, result, businessUnit, documentAllowedAnswer);
      
      if (result.AnswerIsSent)
        return result;
      
      result = this.FillAddendaInfo(document, result);

      if (result.DefaultBox != null && (result.HasApprovalSignature || result.IsSignedByCounterparty))
      {
        try
        {
          result.Certificates = this.GetDocumentCertificatesToBox(document, result.DefaultBox);
        }
        catch (AppliedCodeException ex)
        {
          result.Error = ex.Message;
          result.HasError = true;
          return result;
        }
        
        result.IsSignedByUs = result.Certificates.Certificates.Any();
      }
      else
      {
        result.Certificates = Structures.Module.DocumentCertificatesInfo
          .Create(new List<ICertificate>(), result.CanApprove, new List<ICertificate>());
        result.IsSignedByUs = false;
      }
      
      return result;
    }

    /// <summary>
    /// Заполнить варианты отправки ответа контрагенту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="exchangeDocumentInfo">Информация о документе.</param>
    /// <param name="result">Вспомогательная информация о документе для отправки контрагенту.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="documentAllowedAnswers">Допустимые варианты подписания/отказа/УОУ на документ.</param>
    /// <returns>Информация о документе с вариантами отправки ответа контрагенту.</returns>
    protected virtual SendToCounterpartyInfo FillSignByCounterparty(IOfficialDocument document,
                                                                    IExchangeDocumentInfo exchangeDocumentInfo, SendToCounterpartyInfo result,
                                                                    IBusinessUnit businessUnit, NpoComputer.DCX.Common.DocumentAllowedAnswer documentAllowedAnswers)
    {
      if (result.IsSignedByCounterparty)
      {
        try
        {
          result.DefaultBox = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes()
            .Where(a => Equals(a, exchangeDocumentInfo.RootBox)).SingleOrDefault();
          if (result.DefaultBox == null)
            throw AppliedCodeException.Create(Resources.BoxIsNotValid);

          result.ParentDocumentId = exchangeDocumentInfo.ServiceDocumentId;
          if (documentAllowedAnswers == null)
          {
            var client = GetClient(result.DefaultBox);
            documentAllowedAnswers = client.GetAllowedAnswers(exchangeDocumentInfo.ServiceDocumentId, exchangeDocumentInfo.ServiceMessageId, exchangeDocumentInfo.ExternalBuyerTitleId);
          }
          result.CanSendSignAsAnswer = documentAllowedAnswers.CanSendSign;
          result.CanSendAmendmentRequestAsAnswer = documentAllowedAnswers.CanSendAmendmentRequest;
          result.CanSendInvoiceAmendmentRequestAsAnswer = documentAllowedAnswers.CanSendInvoiceAmendmentRequest;
          result.AnswerIsSent = !result.CanSendAmendmentRequestAsAnswer && !result.CanSendSignAsAnswer && !result.CanSendInvoiceAmendmentRequestAsAnswer;
        }
        catch (AppliedCodeException ex)
        {
          result.Error = ex.Message;
          result.HasError = true;
          return result;
        }
      }
      else
      {
        var businessUnitOrder = businessUnit ?? Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Company.Employees.Current);
        if (Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true)
          result.DefaultBox = Docflow.AccountingDocumentBases.As(document).BusinessUnitBox;
        if (result.DefaultBox == null)
          result.DefaultBox = result.Boxes
            .OrderByDescending(x => Equals(x.BusinessUnit, businessUnitOrder))
            .First();
      }

      return result;
    }

    /// <summary>
    /// Заполнить вспомогательную информацию о приложениях, которые будут отправлены с основным.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="result">Вспомогательная информация о документе для отправки контрагенту.</param>
    /// <returns>Информация о документе и его приложениях для отправки контрагенту.</returns>
    protected virtual SendToCounterpartyInfo FillAddendaInfo(IOfficialDocument document, SendToCounterpartyInfo result)
    {
      var addenda = document.Relations
        .GetRelated().Union(document.Relations.GetRelatedFrom()).Distinct()
        .Select(e => Docflow.OfficialDocuments.As(e))
        .Where(d => d != null && d.HasVersions && d.AccessRights.CanUpdate() && d.AccessRights.CanSendByExchange()).ToList();

      result.Addenda = new List<Exchange.Structures.Module.AddendumInfo>();
      foreach (var addendum in addenda)
      {
        var addendumInfo = new Exchange.Structures.Module.AddendumInfo();
        addendumInfo.Addendum = addendum;

        var exchangeDocInfo = Functions.ExchangeDocumentInfo.GetIncomingExDocumentInfo(addendum);
        
        addendumInfo.BuyerAcceptanceStatus = exchangeDocInfo?.BuyerAcceptanceStatus;
        
        if (result.IsSignedByCounterparty)
        {
          if (!addendum.HasVersions)
            continue;

          if (exchangeDocInfo == null || exchangeDocInfo.NeedSign != true)
            continue;

          // Нельзя отвечать на документы, которые не требуют от нас подписания.
          if (addendum.ExchangeState != Docflow.OfficialDocument.ExchangeState.SignRequired)
            continue;
        }
        else
        {
          if (exchangeDocInfo != null && addendum.Versions.Count > 1)
            addendumInfo.NeedRejectFirstVersion = true;

          // Нельзя отправлять документы, у которых есть какой-то статус МКДО, они или пришли нам или уже ушли от нас.
          if (addendum.ExchangeState != null)
            continue;
        }

        result.Addenda.Add(addendumInfo);
      }

      result.HasAddendaToSend = result.Addenda.Any();
      return result;
    }

    /// <summary>
    /// Заполнить информацию о контрагентах.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="result">Информация о документе для отправки контрагенту.</param>
    /// <returns>Информация о документе с данными о контрагентах.</returns>
    protected virtual SendToCounterpartyInfo FillCounterpartyInfo(IOfficialDocument document, IBusinessUnit businessUnit, SendToCounterpartyInfo result)
    {
      var allBusinessUnitCounterparties = Parties.PublicFunctions.Counterparty.Remote.GetExchangeCounterparty(businessUnit);
      var documentCounterparties = Functions.ExchangeDocumentInfo.GetDocumentCounterparties(document);
      var hasDocumentCounterparties = documentCounterparties != null && documentCounterparties.Any();

      if (hasDocumentCounterparties)
      {
        // В основном в документах 1 контрагент, работаем как раньше.
        if (documentCounterparties.Count < 2)
          result.DefaultCounterparty = documentCounterparties.FirstOrDefault();
        else
        {
          documentCounterparties = allBusinessUnitCounterparties.Intersect(documentCounterparties).ToList();

          // Если после фильтрации кто-то остался - он по умолчанию.
          if (documentCounterparties.Count < 2)
            result.DefaultCounterparty = documentCounterparties.FirstOrDefault();
        }
      }

      if (result.DefaultCounterparty != null)
      {
        result.Counterparties.Add(result.DefaultCounterparty);

        var parties = result.DefaultCounterparty.ExchangeBoxes
          .Where(x => Equals(x.Status, Parties.CounterpartyExchangeBoxes.Status.Active) && x.IsDefault == true);

        if (businessUnit != null)
          parties = parties.Where(x => Equals(x.Box.BusinessUnit, businessUnit));

        if (!parties.Any())
        {
          if (!result.IsSignedByCounterparty)
            result.Error = Resources.NeedSetExchangeForCounterparty;
          else
            result.Error = Resources.NoExchangeThroughThisService;

          result.HasError = true;
        }
      }
      else if (hasDocumentCounterparties)
      {
        result.Counterparties.AddRange(documentCounterparties);
      }
      else
      {
        result.Counterparties.AddRange(allBusinessUnitCounterparties);
      }

      return result;
    }

    /// <summary>
    /// Получить сертификаты для подписания документов, которые будут отправлены через сервис обмена.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Информация о сертификатах.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.Module.DocumentCertificatesInfo GetDocumentCertificatesToBox(Docflow.IOfficialDocument document, ExchangeCore.IBusinessUnitBox box)
    {
      var version = document.LastVersion;
      var signatures = Signatures.Get(version).Where(s => s.IsExternal != true && s.IsValid && s.SignCertificate != null).ToList();
      var allowedCertificates = new List<ICertificate>();
      var allowedCertificatesThumbprints = new List<Structures.Module.Certificate>();
      
      var allCertificates = Certificates.GetAll().ToList();
      var currentUserCertificates = allCertificates.Where(x => Equals(x.Owner, Users.Current) && x.Enabled == true).ToList();
      var canSign = currentUserCertificates.Any();

      if (box.HasExchangeServiceCertificates == true)
      {
        var exchangeCertificates = box.ExchangeServiceCertificates.Select(x => x.Certificate).ToList();
        
        signatures = signatures.Where(s => exchangeCertificates.Any(x => x.Thumbprint.Equals(s.SignCertificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))).ToList();
      }
      
      allowedCertificatesThumbprints = signatures.GroupBy(s => s.Signatory)
        .Select(x => x.OrderByDescending(s => Equals(s.SignatureType, SignatureType.Approval))
                .ThenByDescending(s => s.SigningDate)
                .First())
        .OrderByDescending(s => Equals(s.SignatureType, SignatureType.Approval))
        .ThenByDescending(s => s.SigningDate)
        .Select(s => Structures.Module.Certificate.Create(s.SignCertificate.Thumbprint, s.Signatory))
        .ToList();
      
      foreach (var cert in allowedCertificatesThumbprints)
      {
        var existCert = allCertificates.FirstOrDefault(x => x.Thumbprint.Equals(cert.Thumbprint, StringComparison.InvariantCultureIgnoreCase) && Equals(x.Owner, cert.Owner));
        
        if (existCert != null)
          allowedCertificates.Add(existCert);
      }
      
      allowedCertificates = allowedCertificates.Distinct().ToList();
      
      return Structures.Module.DocumentCertificatesInfo.Create(allowedCertificates, canSign, currentUserCertificates);
    }

    /// <summary>
    /// Получить подключенные ящики сервисов обмена для отправки документа контрагентам.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="counterparties">Контрагенты.</param>
    /// <returns>Список подключенных ящиков сервисов обмена.</returns>
    [Remote(IsPure = true)]
    public static List<ExchangeCore.IBusinessUnitBox> GetConnectedExchangeBoxesToCounterparty(Docflow.IOfficialDocument document, List<Parties.ICounterparty> counterparties)
    {
      return GetAllExchangeBoxesToCounterparty(document, counterparties)
        .Where(b => b.ConnectionStatus == ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        .ToList();
    }

    /// <summary>
    /// Получить все ящики сервисов обмена для отправки документа контрагентам.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="counterparties">Контрагенты.</param>
    /// <returns>Список всех ящиков сервисов обмена.</returns>
    [Remote(IsPure = true)]
    public static List<ExchangeCore.IBusinessUnitBox> GetAllExchangeBoxesToCounterparty(Docflow.IOfficialDocument document, List<Parties.ICounterparty> counterparties)
    {
      var boxes = counterparties.SelectMany(c => c.ExchangeBoxes
                                            .Where(b => b.Status == Parties.CounterpartyExchangeBoxes.Status.Active && b.IsDefault == true)
                                            .Select(b => b.Box)).ToList();
      
      boxes = boxes.Distinct().Where(x => x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).ToList();
      if (document.BusinessUnit != null)
        boxes = boxes.Where(x => Equals(x.BusinessUnit, document.BusinessUnit)).ToList();
      
      return boxes;
    }

    private static bool HasNotApprovedDocuments(IOfficialDocument document, List<IOfficialDocument> documents)
    {
      var notSigned = HasNotApprovedDocuments(document);
      if (notSigned)
        return true;
      
      return HasNotApprovedDocuments(documents.ToArray());
    }

    private static bool HasNotApprovedDocuments(IOfficialDocument document)
    {
      return HasNotApprovedDocuments(new[] { document });
    }

    private static bool HasNotApprovedDocuments(params IOfficialDocument[] documents)
    {
      foreach (var document in documents)
      {
        var signed = Signatures.Get(document.LastVersion)
          .Any(s => s.SignatureType == SignatureType.Approval && s.IsExternal != true && s.IsValid);
        if (!signed)
          return true;
      }
      return false;
    }

    #endregion
    
    #endregion
    
    #region Отправка извещений о получении
    
    /// <summary>
    /// Получить список информации о документах, для которых требуется отправить ИОП.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <param name="skip">Количество пропускаемых записей.</param>
    /// <param name="take">Количество получаемых записей.</param>
    /// <param name="withoutGenerated">True, если хотим получить только инфошки, по которым ещё надо выполнить генерацию ИОП.</param>
    /// <returns>Информация о документах, для которых требуется отправить ИОП.</returns>
    [Public, Remote(IsPure = true)]
    public List<IExchangeDocumentInfo> GetDocumentInfosWithoutReceiptNotificationPart(Sungero.ExchangeCore.IBusinessUnitBox box,
                                                                                      int skip, int take, bool withoutGenerated)
    {
      var documentInfos = this.GetDocumentInfosWithoutReceiptNotification(box, withoutGenerated).ToList();
      // Получить инфошки по сообщениям для обработки служебных документов сообщений целиком(одновременная отправка ИОПов на комплект. СБИС).
      var messagesIds = documentInfos.Select(d => d.ServiceMessageId).Distinct().Skip(skip).Take(take).ToList();
      documentInfos = documentInfos.Where(info => messagesIds.Contains(info.ServiceMessageId)).ToList();
      
      // Рассчитываем, что объем данных, запрошенных в take, будет небольшой.
      var documentIds = documentInfos.Select(d => d.Document.Id).Distinct().ToList();
      var availableIds = Docflow.OfficialDocuments.GetAll(d => documentIds.Contains(d.Id)).Select(d => d.Id).ToList();
      return documentInfos.Where(i => availableIds.Contains(i.Document.Id)).ToList();
    }

    /// <summary>
    /// Получить список информации о документах, для которых требуется отправить ИОП.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <param name="withoutGenerated">True, если хотим получить только инфошки, по которым ещё надо выполнить генерацию ИОП.</param>
    /// <returns>Информация о документах, для которых требуется отправить ИОП.</returns>
    [Public]
    public IQueryable<IExchangeDocumentInfo> GetDocumentInfosWithoutReceiptNotification(Sungero.ExchangeCore.IBusinessUnitBox box, bool withoutGenerated)
    {
      var certificate = box.CertificateReceiptNotifications;
      var documentInfos = ExchangeDocumentInfos.GetAll()
        .Where(x => Equals(x.RootBox, box) && x.Document != null &&
               (!x.ServiceDocuments.Any(d => (d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.IReceipt ||
                                              d.DocumentType == Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType.Receipt) &&
                                        (withoutGenerated ? d.Certificate == certificate : d.Date != null)) &&
                x.DeliveryConfirmationStatus == null));
      
      return documentInfos;
    }

    /// <summary>
    /// Определить, отправлены ли служебные документы.
    /// </summary>
    /// <param name="documentInfos">Список информации о документах обмена.</param>
    /// <returns>True - если документы не отправлены, иначе - false.</returns>
    [Remote(IsPure = true)]
    public bool IsReglamentDocumentsNotSent(List<IExchangeDocumentInfo> documentInfos)
    {
      var documentInfoIds = documentInfos.Select(i => i.ServiceDocumentId).ToList();
      return ExchangeDocumentInfos.GetAll(info => documentInfoIds.Contains(info.ServiceDocumentId))
        .Any(info => !Equals(info.DeliveryConfirmationStatus, Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent));
    }
    
    /// <summary>
    /// Получить список документов, для которых требуется отправить ИОП.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <returns>Список документов, для которых требуется отправить ИОП.</returns>
    [Remote]
    public IQueryable<Content.IElectronicDocument> GetDocumentsWithoutReceiptNotification(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      return this.GetDocumentInfosWithoutReceiptNotification(box, false).Select(d => d.Document);
    }
    
    /// <summary>
    /// Проставить признак получения ИОПа.
    /// </summary>
    /// <param name="info">Информация о документе МКДО.</param>
    /// <param name="comment">Комментарий.</param>
    /// <param name="sent">Была ли реальная отправка или отправка не требуется.</param>
    [Remote]
    public virtual void FixReceiptNotification(Exchange.IExchangeDocumentInfo info, string comment, bool sent)
    {
      var operation = new Enumeration(Constants.Module.Exchange.SendReadMark);
      this.LogDebugFormat(info, "Execute FixReceiptNotification.");
      if (info != null && info.DeliveryConfirmationStatus != Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent)
      {
        if (sent)
        {
          var version = info.Document.Versions.Where(v => v.Id == info.VersionId).Single();
          info.Document.History.Write(operation, operation, comment, version.Number);
          var client = GetClient(info.RootBox);
          if (!client.CanSendDeliveryConfirmation(info.ServiceDocumentId, info.ServiceMessageId))
          {
            info.DeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.Sent;
            this.LogDebugFormat(info, "Execute FixReceiptNotification. Receipts successfully sent.");
          }
        }
        else
        {
          info.DeliveryConfirmationStatus = Exchange.ExchangeDocumentInfo.DeliveryConfirmationStatus.NotRequired;
          this.LogDebugFormat(info, "Execute FixReceiptNotification. Receipts not sent.");
        }
        info.Save();
      }
    }
    
    /// <summary>
    /// Проставить признак получения ИОПа.
    /// </summary>
    /// <param name="documentInfos">Информация о документах МКДО.</param>
    /// <param name="comment">Комментарий.</param>
    [Remote]
    public virtual void FixReceiptNotification(List<Exchange.IExchangeDocumentInfo> documentInfos, string comment)
    {
      foreach (var info in documentInfos)
        this.FixReceiptNotification(info, comment, false);
    }
    
    /// <summary>
    /// Создать задачу на отправку извещений о получении документов.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <returns>Задача на отправку извещений о получении документов.</returns>
    [Remote, Public]
    public IReceiptNotificationSendingTask CreateReceiptNotificationSendingTask(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      var task = Sungero.Exchange.ReceiptNotificationSendingTasks.Create();
      task.Box = box;
      task.Subject = CutText(Sungero.Exchange.ReceiptNotificationSendingTasks.Resources.TaskSubjectFormat(box.Name), task.Info.Properties.Subject.Length);
      task.ActiveText = Sungero.Exchange.ReceiptNotificationSendingTasks.Resources.TaskActiveTextFormat(box.ExchangeService.Name);
      task.MaxDeadline = Calendar.Now.AddWorkingHours(box.Responsible, 4);
      task.Save();
      return task;
    }
    
    /// <summary>
    /// Сохранить служебные документы, которые будут подписаны.
    /// </summary>
    /// <param name="documentsToSign">Сервисный документ, сертификат, которым он должен быть подписан и подпись.</param>
    [Remote]
    public virtual void SaveDeliveryConfirmationSigns(List<Structures.Module.ReglamentDocumentWithCertificate> documentsToSign)
    {
      foreach (var doc in documentsToSign)
      {
        var info = doc.Info;
        if (Locks.GetLockInfo(info).IsLockedByOther)
          continue;
        
        var serviceDocument = info.ServiceDocuments.FirstOrDefault(d => d.DocumentType == doc.ReglamentDocumentType);
        if (serviceDocument == null)
        {
          serviceDocument = info.ServiceDocuments.AddNew();
          serviceDocument.DocumentType = doc.ReglamentDocumentType;
        }
        serviceDocument.DocumentId = doc.ServiceDocumentId;
        serviceDocument.ParentDocumentId = doc.ParentDocumentId;
        serviceDocument.Sign = doc.Signature;
        serviceDocument.Certificate = doc.Certificate;
        serviceDocument.Body = doc.Content;
        serviceDocument.GeneratedName = doc.Name;
        serviceDocument.StageId = doc.ServiceDocumentStageId;
        serviceDocument.FormalizedPoAUnifiedRegNo = doc.FormalizedPoAUnifiedRegNumber;
        info.Save();
      }
    }
    
    #endregion

    #region Общие сервисные методы

    /// <summary>
    /// Создать элемент очереди и запустить агент конвертации версий документов.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">ИД версии документа.</param>
    /// <param name="exchangeStatus">Статус обмена.</param>
    [Public]
    public void EnqueueXmlToPdfBodyConverter(Sungero.Docflow.IOfficialDocument document, int versionId, Enumeration? exchangeStatus)
    {
      var queueItem = ExchangeCore.BodyConverterQueueItems.Create();
      queueItem.Document = document;
      queueItem.VersionId = versionId;
      queueItem.ExchangeState = exchangeStatus;
      queueItem.ProcessingStatus = ExchangeCore.MessageQueueItem.ProcessingStatus.NotProcessed;
      queueItem.Save();
      
      Sungero.Exchange.Jobs.BodyConverterJob.Enqueue();
    }

    /// <summary>
    /// Убрать пространства имен.
    /// </summary>
    /// <param name="document">Документ.</param>
    [Public]
    public static void RemoveNamespaces(System.Xml.Linq.XDocument document)
    {
      foreach (var rootElements in document.Root.Descendants())
      {
        var attributesWithNamespace = rootElements.Attributes().Where(x => x.IsNamespaceDeclaration).ToList();
        foreach (var attributeWithNamespace in attributesWithNamespace)
          attributeWithNamespace.Remove();
      }
      foreach (var element in document.Descendants().Where(x => x.Name.NamespaceName.Any()).ToList())
        element.Name = element.Name.LocalName;
    }

    /// <summary>
    /// Обрезать длинную строку.
    /// </summary>
    /// <param name="text">Строка.</param>
    /// <param name="maxLength">Максимальная длина строки.</param>
    /// <returns>Строка указанной длины.</returns>
    [Public]
    public static string CutText(string text, int maxLength)
    {
      if (text.Length > maxLength)
        return Sungero.Exchange.Resources.Ellipsis_CutTextFormat(text.Substring(0, maxLength - 1));
      
      return text;
    }

    /// <summary>
    /// Проверка фонового процесса.
    /// </summary>
    /// <param name="id">Id фонового процесса.</param>
    /// <returns>Включен ли фоновый процесс.</returns>
    [Public, Remote]
    public static bool IsJobEnabled(string id)
    {
      var isJobEnabled = false;
      AccessRights.AllowRead(
        () =>
        {
          var guid = Guid.Parse(id);
          var job = CoreEntities.Jobs.GetAll(x => Equals(x.JobId, guid)).FirstOrDefault();
          isJobEnabled = job != null && job.Status == Sungero.CoreEntities.DatabookEntry.Status.Active;
        });
      return isJobEnabled;
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Получение сообщений".
    /// </summary>
    [Public, Remote]
    public static void RequeueMessagesGet()
    {
      Jobs.GetMessages.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Преобразование документов в PDF".
    /// </summary>
    [Public, Remote]
    public static void RequeueBodyConverterJob()
    {
      Jobs.BodyConverterJob.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Создание извещений о получении документов".
    /// </summary>
    [Public, Remote]
    public static void RequeueGenerateServiceDocuments()
    {
      Jobs.CreateReceiptNotifications.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Электронный обмен. Отправка извещений о получении документов".
    /// </summary>
    [Public, Remote]
    public static void RequeueSendSignedReceiptNotifications()
    {
      Jobs.SendSignedReceiptNotifications.Enqueue();
    }
    
    #endregion

    /// <summary>
    /// Получить сертификаты.
    /// </summary>
    /// <param name="owner">Владелец сертификата.</param>
    /// <returns>Список сертификатов.</returns>
    [Remote]
    public virtual IQueryable<ICertificate> GetCertificates(IUser owner)
    {
      return Certificates.GetAll().Where(x => Equals(x.Owner, owner) && x.Enabled == true);
    }
    
    /// <summary>
    /// Заменить спец. символы и зарезервированные слова.
    /// </summary>
    /// <param name="name">Имя файла без расширения.</param>
    /// <returns>Преобразованное имя файла.</returns>
    [Public, Remote]
    public virtual string GetValidFileName(string name)
    {
      var replacement = "_";
      var normalizedName = name.Trim();
      
      if (string.IsNullOrEmpty(normalizedName))
        normalizedName = replacement;

      var specialWordsPattern = @"^(CON|PRN|AUX|CLOCK\$|NUL|COM0|COM1|COM2|COM3|COM4|COM5|COM6|COM7|COM8|COM9|LPT0|LPT1|LPT2|LPT3|LPT4|LPT5|LPT6|LPT7|LPT8|LPT9)(\.+|\.*$)";
      normalizedName = System.Text.RegularExpressions.Regex.Replace(normalizedName, specialWordsPattern, replacement, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

      var specialSimbolPattern = @"\\|\/|\:|\*|\?|\<|\>|\||\'|""";
      normalizedName = System.Text.RegularExpressions.Regex.Replace(normalizedName, specialSimbolPattern, replacement);

      normalizedName = System.Text.RegularExpressions.Regex.Replace(normalizedName, @"\.$", replacement);

      this.LogDebugFormat(string.Format("Normalize File Name {0} To {1}", name, normalizedName));
      return normalizedName;
    }
    
    /// <summary>
    /// Получить список с информацией по документам обмена.
    /// </summary>
    /// <param name="serviceMessageId">Ид сообщения.</param>
    /// <returns>Список с информацией по документам обмена.</returns>
    [Public, Remote]
    public static List<IExchangeDocumentInfo> GetPackageDocumentsExchangeInfos(string serviceMessageId)
    {
      return ExchangeDocumentInfos.GetAll().Where(d => d.ServiceMessageId == serviceMessageId &&
                                                  d.MessageType.Value == Sungero.Exchange.ExchangeDocumentInfo.MessageType.Incoming).ToList();
    }
    
    /// <summary>
    /// Определить наличие прав у пользователя на документы комплекта.
    /// </summary>
    /// <param name="exchangeDocumentsInfos">Список информации по документам обмена.</param>
    /// <returns>True - если есть права на все документы комплекта, иначе - false.</returns>
    [Public, Remote]
    public static bool HasRightsToPackageExchangeDocuments(List<IExchangeDocumentInfo> exchangeDocumentsInfos)
    {
      var documentsIds = exchangeDocumentsInfos.Select(d => d.Document.Id).ToArray();
      var packageDocuments = Docflow.OfficialDocuments.GetAll(d => documentsIds.Contains(d.Id));
      if (packageDocuments.Count() != exchangeDocumentsInfos.Count())
        return false;
      foreach (var document in packageDocuments)
      {
        if (!document.AccessRights.CanUpdate())
          return false;
      }
      return true;
    }
    
    #region Логирование
    
    /// <summary>
    /// Записать в лог полную информацию о содержимом сообщения из сервиса обмена.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    public virtual void LogFullMessage(NpoComputer.DCX.Common.IMessage message)
    {
      this.LogMessage(message);
      this.LogMessagePrimaryDocuments(message);
      this.LogMessageReglamentDocuments(message);
      this.LogMessageSignatures(message);
    }
    
    /// <summary>
    /// Записать в лог общую информацию о сообщении из сервиса обмена.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    public virtual void LogMessage(NpoComputer.DCX.Common.IMessage message)
    {
      this.LogDebugFormat(message, "Service message: IsReply: '{0}', IsIncoming: '{1}', Sender: '{2}', Receiver: '{3}', ParentServiceMessageId: '{4}'.",
                          message.IsReply, message.IsIncome, message.Sender == null ? string.Empty : message.Sender.BoxId, message.Receiver == null ? string.Empty : message.Receiver.BoxId, message.ParentServiceMessageId);
      
      var fromDepartment = message.FromDepartment;
      if (fromDepartment == null)
        this.LogDebugFormat(message, "Service message: property FromDepartment is null.");
      else
        this.LogDebugFormat(message, "Service message: FromDepartment: Name:  '{0}', Id:  '{1}', Kpp:  '{2}', ParentDepartmentId:  '{3}'.",
                            fromDepartment.Name, fromDepartment.Id, fromDepartment.Kpp, fromDepartment.ParentDepartmentId);
      
      var intoDepartment = message.ToDepartment;
      if (intoDepartment == null)
        this.LogDebugFormat(message, "Service message: property ToDepartment is null.");
      else
        this.LogDebugFormat(message, "Service message: ToDepartment: Name:  '{0}', Id:  '{1}', Kpp:  '{2}', ParentDepartmentId:  '{3}'.",
                            intoDepartment.Name, intoDepartment.Id, intoDepartment.Kpp, intoDepartment.ParentDepartmentId);
      
    }
    
    /// <summary>
    /// Записать в лог информацию о документах сообщения из сервиса обмена.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    public virtual void LogMessagePrimaryDocuments(NpoComputer.DCX.Common.IMessage message)
    {
      foreach (var primaryDocument in message.PrimaryDocuments)
      {
        this.LogDebugFormat(message, primaryDocument, "Primary document: NeedSign: '{0}', SignStatus: '{1}', NeedReceipt: '{2}', RevocationStatus: '{3}', ParentServiceEntityId: '{4}'.",
                            primaryDocument.NeedSign, primaryDocument.SignStatus, primaryDocument.NeedReceipt, primaryDocument.RevocationStatus, primaryDocument.ParentServiceEntityId);
        
        this.LogDebugFormat(message, primaryDocument, "Primary document: NonformalizedKind: '{0}', IsUnknownDocumentType: '{1}', Comment: '{2}', Date: '{3}', IsLegitimate: '{4}', Card: '{5}',  GlobalDocumentId: '{6}'.",
                            primaryDocument.NonformalizedKind, primaryDocument.IsUnknownDocumentType, primaryDocument.Comment, primaryDocument.Date, primaryDocument.IsLegitimate, primaryDocument.Card,
                            primaryDocument.GlobalDocumentId);
        
        if (primaryDocument.DocumentType != DocumentType.Nonformalized)
          this.LogDebugFormat(message, primaryDocument, "Primary document: FileName: '{0}'.", primaryDocument.FileName);
        
        this.LogDebugFormat(message, primaryDocument, "Primary document: BoundDocuments: '{0}'.", string.Join(", ",  primaryDocument.BoundDocuments.Select(x => x.DocumentId).ToList()));
        
        // Метаданные.
        foreach (var item in primaryDocument.Metadata)
        {
          this.LogDebugFormat(message, primaryDocument, "Metadata: Key = '{0}', Value = '{1}'.", item.Key, item.Value);
        }
      }
    }
    
    /// <summary>
    /// Записать в лог информацию о служебных документах сообщения из сервиса.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    public virtual void LogMessageReglamentDocuments(NpoComputer.DCX.Common.IMessage message)
    {
      foreach (var reglamentDocument in message.ReglamentDocuments)
      {
        this.LogDebugFormat(message, reglamentDocument, "Reglament document:  Type: '{0}', FileName: '{1}', DateTime: '{2}'.", reglamentDocument.DocumentType, reglamentDocument.FileName, reglamentDocument.DateTime);
        
        this.LogDebugFormat(message, reglamentDocument, "Reglament document:  Type: '{0}', RootServiceEntityId: '{1}', ParentServiceEntityId: '{2}'.",
                            reglamentDocument.DocumentType, reglamentDocument.RootServiceEntityId, reglamentDocument.ParentServiceEntityId);
      }
    }
    
    /// <summary>
    /// Записать в лог информацию о подписи из сообщения.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    public virtual void LogMessageSignatures(NpoComputer.DCX.Common.IMessage message)
    {
      foreach (var signature in message.Signatures)
      {
        this.LogDebugFormat(message, "Signature:  DocumentId: '{0}', SignerBoxId: '{1}'.", signature.DocumentId, signature.SignerBoxId);
      }
    }
    
    /// <summary>
    /// Получить строку с префиксом Exchange.
    /// </summary>
    /// <param name="text">Сообщение.</param>
    /// <param name="paramsInformation">Строка с параметрами.</param>
    /// <returns>Строка с префиксом Exchange.</returns>
    public virtual string ExchangeLogPattern(string text, string paramsInformation)
    {
      if (string.IsNullOrEmpty(paramsInformation))
        return string.Format("Exchange. {0}", text);
      
      return string.Format("Exchange. {0} {1}", text, paramsInformation);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(string text)
    {
      var format = this.ExchangeLogPattern(text, null);
      Logger.Debug(format);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="documentId">Ид документа.</param>
    /// <param name="versionId">Ид версии.</param>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(int documentId, int versionId, string text)
    {
      var documentInformation = string.Format("DocumentId: '{0}', VersionId: '{1}'.", documentId, versionId);
      var format = this.ExchangeLogPattern(text, documentInformation);
      Logger.Debug(format);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(ExchangeCore.IBoxBase box, string text)
    {
      this.LogDebugFormat(box, text, string.Empty);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="documentInfo">Информация о документе обмена.</param>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(IExchangeDocumentInfo documentInfo, string text)
    {
      this.LogDebugFormat(documentInfo, text, string.Empty);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="queueItem">Элемент очереди конвертации тел документов.</param>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(ExchangeCore.IBodyConverterQueueItem queueItem, string text)
    {
      this.LogDebugFormat(queueItem, text, string.Empty);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="queueItem">Элемент очереди синхронизации контрагентов.</param>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogDebugFormat(ExchangeCore.ICounterpartyQueueItem queueItem, string text)
    {
      this.LogDebugFormat(queueItem, text, string.Empty);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(ExchangeCore.IBoxBase box, string logFormat, params object[] args)
    {
      var boxInformation = string.Format("Box: DisplayValue: '{0}', Id: '{1}'.", box?.DisplayValue, box?.Id);
      var format = this.ExchangeLogPattern(logFormat, boxInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IMessage message, string logFormat, params object[] args)
    {
      var messageInformation = string.Format("ServiceMessageId: '{0}'.", message?.ServiceMessageId);
      var format = this.ExchangeLogPattern(logFormat, messageInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <param name="primaryDocument">Документ сообщения.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IMessage message, NpoComputer.DCX.Common.IDocument primaryDocument, string logFormat, params object[] args)
    {
      var primaryDocumentInformation = string.Format("ServiceMessageId: '{0}'. Primary document: Type: '{1}', ServiceEntityId: '{2}'.",
                                                     message?.ServiceMessageId, primaryDocument?.DocumentType, primaryDocument?.ServiceEntityId);
      var format = this.ExchangeLogPattern(logFormat, primaryDocumentInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <param name="reglamentDocument">Служебный документ сообщения.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IMessage message, NpoComputer.DCX.Common.IReglamentDocument reglamentDocument, string logFormat, params object[] args)
    {
      var reglamentDocumentInformation = string.Format("ServiceMessageId: '{0}'. Reglament document: Type: '{1}, ServiceEntityId: '{2}'.",
                                                       message?.ServiceMessageId, reglamentDocument?.DocumentType, reglamentDocument?.ServiceEntityId);
      var format = this.ExchangeLogPattern(logFormat, reglamentDocumentInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="queueItem">Элемент очереди синхронизации сообщений.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(ExchangeCore.IMessageQueueItem queueItem, string logFormat, params object[] args)
    {
      var queueItemInformation = string.Format("MessageQueueItem: Id: '{0}', ExternalId: '{1}'.", queueItem?.Id, queueItem?.ExternalId);
      var format = this.ExchangeLogPattern(logFormat, queueItemInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <param name="queueItem">Элемент очереди синхронизации сообщений.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IMessage message, IMessageQueueItem queueItem, ExchangeCore.IBoxBase box, string logFormat, params object[] args)
    {
      var paramsInformation = string.Format("ServiceMessageId: '{0}'. MessageQueueItem: Id: '{1}'. BoxId: '{2}'.",
                                            message?.ServiceMessageId, queueItem?.Id, box?.Id);
      
      var format = this.ExchangeLogPattern(logFormat, paramsInformation);
      Logger.DebugFormat(format, args);
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="message">Сообщение из сервиса обмена.</param>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(NpoComputer.DCX.Common.IMessage message, ExchangeCore.IBoxBase box, string logFormat, params object[] args)
    {
      var paramsInformation = string.Format("ServiceMessageId: '{0}'. BoxId: '{1}'.",
                                            message?.ServiceMessageId, box?.Id);
      
      var format = this.ExchangeLogPattern(logFormat, paramsInformation);
      Logger.DebugFormat(format, args);
    }

    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="documentInfo">Информация о документе обмена.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(IExchangeDocumentInfo documentInfo, string logFormat, params object[] args)
    {
      var serviceMessageId = documentInfo?.ServiceMessageId;
      var serviceDocumentId = documentInfo?.ServiceDocumentId;
      var exchangeDocumentInfoId = documentInfo?.Id;
      var documentId = documentInfo?.Document?.Id;
      
      var paramsInformation = string.Format("ExchangeDocumentInfo: Id: '{0}', ServiceMessageId: '{1}', ServiceDocumentId: '{2}', DocumentId: '{3}'.",
                                            exchangeDocumentInfoId, serviceMessageId, serviceDocumentId, documentId);

      var format = this.ExchangeLogPattern(logFormat, paramsInformation);
      
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="queueItem">Элемент очереди конвертации тел документов.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(ExchangeCore.IBodyConverterQueueItem queueItem, string logFormat, params object[] args)
    {
      var queueItemInformation = string.Format("BodyConverterQueueItem: Id: '{0}', DocumentId: '{1}'.", queueItem?.Id, queueItem?.Document?.Id);
      var format = this.ExchangeLogPattern(logFormat, queueItemInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="queueItem">Элемент очереди синхронизации контрагентов.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(ExchangeCore.ICounterpartyQueueItem queueItem, string logFormat, params object[] args)
    {
      var queueItemInformation = string.Format("CounterpartyQueueItem: Id: '{0}', ExternalId: '{1}', RootBoxId: '{2}'.", queueItem?.Id, queueItem?.ExternalId, queueItem?.RootBox?.Id);
      var format = this.ExchangeLogPattern(logFormat, queueItemInformation);
      Logger.DebugFormat(format, args);
    }
    
    /// <summary>
    /// Записать сообщение в лог.
    /// </summary>
    /// <param name="documentInfo">Информация о документе обмена.</param>
    /// <param name="reglamentDocuments">Служебные документы сообщения.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogDebugFormat(IExchangeDocumentInfo documentInfo, List<NpoComputer.DCX.Common.IReglamentDocument> reglamentDocuments, string logFormat, params object[] args)
    {
      foreach (var reglamentDocument in reglamentDocuments)
      {
        var serviceMessageId = documentInfo?.ServiceMessageId;
        var serviceDocumentId = documentInfo?.ServiceDocumentId;
        var exchangeDocumentInfoId = documentInfo?.Id;
        var documentId = documentInfo?.Document?.Id;
        
        var paramsInformation = string.Format("ExchangeDocumentInfo: Id: '{0}', ServiceMessageId: '{1}', ServiceDocumentId: '{2}', DocumentId: '{3}'. " +
                                              "Reglament document:  DocumentType: '{4}', RootServiceEntityId: '{5}', ParentServiceEntityId: '{6}'.",
                                              exchangeDocumentInfoId, serviceMessageId, serviceDocumentId, documentId,
                                              reglamentDocument.DocumentType, reglamentDocument.RootServiceEntityId, reglamentDocument.ParentServiceEntityId);

        var format = this.ExchangeLogPattern(logFormat, paramsInformation);
        Logger.DebugFormat(format, args);
      }
    }
    
    /// <summary>
    /// Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="text">Сообщение.</param>
    [Public]
    public virtual void LogErrorFormat(string text)
    {
      var format = this.ExchangeLogPattern(text, null);
      Logger.Error(format);
    }
    
    /// <summary>
    /// Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="text">Сообщение.</param>
    /// <param name="ex">Исключение.</param>
    [Public]
    public virtual void LogErrorFormat(string text, System.Exception ex)
    {
      var format = this.ExchangeLogPattern(text, null);
      Logger.Error(format, ex);
    }
    
    /// <summary>
    /// Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="documentId">Ид документа.</param>
    /// <param name="versionId">Ид версии.</param>
    /// <param name="text">Сообщение.</param>
    /// <param name="ex">Исключение.</param>
    [Public]
    public virtual void LogErrorFormat(int documentId, int versionId, string text, System.Exception ex)
    {
      var documentInformation = string.Format("DocumentId: '{0}', VersionId: '{1}'.", documentId, versionId);
      var format = this.ExchangeLogPattern(text, documentInformation);
      Logger.Error(format, ex);
    }
    
    /// <summary>
    ///  Записать сообщение об ошибке в лог.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="logFormat">Формат строки.</param>
    /// <param name="args">Аргументы.</param>
    public virtual void LogErrorFormat(ExchangeCore.IBoxBase box, string logFormat, params object[] args)
    {
      var paramsInformation = string.Format("BoxId: '{0}'.", box?.Id);
      
      var format = this.ExchangeLogPattern(logFormat, paramsInformation);
      Logger.ErrorFormat(format, args);
    }
    
    #endregion
    
    /// <summary>
    /// Проверка накопленных ошибок обмена.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    public virtual void RunExchangeCheckup(ExchangeCore.IBusinessUnitBox box)
    {
      var poisonedMessagePeriodBegin = Calendar.Now.AddDays(-Constants.Module.PoisonedMessagePeriod);
      var poisonedQueueItemsCount = MessageQueueItems.GetAll().Where(q => Equals(q.Box, box) && q.Created != null && q.Created < poisonedMessagePeriodBegin).Count();
      if (poisonedQueueItemsCount > 0)
        this.LogErrorFormat(box, "Business unit box contains {0} poisoned messages.", poisonedQueueItemsCount);
      
      var counterpartyExternalIds = CounterpartyQueueItems.GetAll(c => Equals(c.Box, box) && c.MatchingTask != null).Select(q => q.ExternalId).ToList();
      var counterpartyConflict = MessageQueueItems.GetAll().Where(q => Equals(q.Box, box) && q.CounterpartyExternalId != null && q.CounterpartyExternalId != string.Empty &&
                                                                  counterpartyExternalIds.Contains(q.CounterpartyExternalId));
      var counterpartyConflictCount = counterpartyConflict.Count();
      if (counterpartyConflictCount > 0)
        this.LogErrorFormat(box, "{0} messages with unresolved counterparty conflicts for business unit box.", counterpartyConflictCount);
    }
    
    /// <summary>
    /// Получить статус приемки.
    /// </summary>
    /// <param name="primaryDocument">Документ сообщения.</param>
    /// <returns>Статус приемки.</returns>
    public Enumeration? GetBuyerAcceptanceStatus(NpoComputer.DCX.Common.IDocument primaryDocument)
    {
      switch (primaryDocument.BuyerAcceptanceStatus)
      {
        case NpoComputer.DCX.Common.BuyerAcceptanceStatus.Accepted:
          return Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
          
        case NpoComputer.DCX.Common.BuyerAcceptanceStatus.PartiallyAccepted:
          return Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted;
          
        case NpoComputer.DCX.Common.BuyerAcceptanceStatus.Rejected:
          return Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected;
          
        default:
          return null;
      }
    }
    
    /// <summary>
    /// Получить ссылку на эл. доверенность в сервисе.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый рег. № эл. доверенности.</param>
    /// <returns>Ссылка на эл. доверенность в сервисе.</returns>
    public virtual string GetFormalizedPoALink(string unifiedRegistrationNumber)
    {
      return PublicConstants.Module.DefaultFormalizedPoALink;
    }
    
    /// <summary>
    /// Получить текстовое описание ссылки на эл. доверенность.
    /// </summary>
    /// <param name="unifiedRegistrationNumber">Единый рег. № эл. доверенности.</param>
    /// <returns>Текстовое описание ссылки на эл. доверенность.</returns>
    public virtual string GetFormalizedPoALinkTitle(string unifiedRegistrationNumber)
    {
      return Resources.SbisFormalizedPoALinkTitleFormat(unifiedRegistrationNumber);
    }
  }
}
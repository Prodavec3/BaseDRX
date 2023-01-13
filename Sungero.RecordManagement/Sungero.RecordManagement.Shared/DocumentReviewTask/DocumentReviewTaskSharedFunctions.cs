using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.RecordManagement.Structures.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Shared
{
  partial class DocumentReviewTaskFunctions
  {
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    public virtual List<StartValidationMessage> GetStartValidationMessages()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var errors = new List<StartValidationMessage>();
      bool workingWithGUI = Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj, Constants.DocumentReviewTask.WorkingWithGuiParamName);
      
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Проверить, что у инициатора есть права на документ.
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj)))
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.NoRightsToDocument, false, false));
      
      // Документ на исполнении нельзя отправлять на рассмотрение.
      if (workingWithGUI && document != null && document.ExecutionState == Docflow.OfficialDocument.ExecutionState.OnExecution)
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.DocumentOnExecution, false, false));
      
      // Проверить корректность срока.
      if (_obj.Addressees.Any(x => !Docflow.PublicFunctions.Module.CheckDeadline(x.Addressee, _obj.Deadline, Calendar.Now)))
        errors.Add(StartValidationMessage.Create(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, true, false));
      
      // Проверить, что входящий документ зарегистрирован.
      if (workingWithGUI && !Functions.DocumentReviewTask.IncomingDocumentRegistered(document))
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.IncomingDocumentMustBeRegistered, false, false));
      
      return errors;
    }
    
    /// <summary>
    /// Валидация старта задачи на рассмотрение.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateDocumentReviewTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = this.GetStartValidationMessages();
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else if (error.IsImpossibleSpecifyDeadlineLessThanTodayMessage)
            e.AddError(_obj.Info.Properties.Deadline, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Проверка, зарегистрирован ли входящий документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ зарегистрирован, либо документ не входящий.</returns>
    public static bool IncomingDocumentRegistered(IOfficialDocument document)
    {
      if (document == null || document.DocumentKind == null)
        return true;
      
      var documentKind = document.DocumentKind;
      return documentKind.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Incoming ||
        documentKind.NumberingType != Docflow.DocumentKind.NumberingType.Registrable ||
        document.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered;
    }
    
    /// <summary>
    /// Проверить, завершена ли задача на рассмотрение.
    /// </summary>
    /// <returns>True, если задача на рассмотрение выполнена, иначе - False.</returns>
    public virtual bool IsDocumentReviewTaskCompleted()
    {
      return Docflow.PublicFunctions.Module.IsTaskCompleted(_obj);
    }
    
    /// <summary>
    /// Получить список просроченных задач на исполнение поручения в состоянии Черновик.
    /// </summary>
    /// <returns>Список просроченных задач на исполнение поручения в состоянии Черновик.</returns>
    public virtual List<IActionItemExecutionTask> GetDraftOverdueActionItemExecutionTasks()
    {
      var tasks = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft);
      var overdueTasks = new List<IActionItemExecutionTask>();
      foreach (var task in tasks)
        if (Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(task))
          overdueTasks.Add(task);
      
      return overdueTasks;
    }
    
    /// <summary>
    /// Доступность результата выполнения "Вернуть инициатору".
    /// </summary>
    /// <param name="task">Задача на рассмотрение.</param>
    /// <returns>True - если доступно, иначе - False.</returns>
    public static bool SchemeVersionSupportsRework(ITask task)
    {
      return task.GetStartedSchemeVersion() >= LayerSchemeVersions.V5;
    }
    
    /// <summary>
    /// Синхронизировать адресатов из документа в задачу на рассмотрение руководителем.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    [Public]
    public virtual void SynchronizeAddressees(Docflow.IOfficialDocument document)
    {
      var documentAddressees = Docflow.PublicFunctions.OfficialDocument.GetAddressees(document);
      var newAddressees = documentAddressees
        .Except(_obj.Addressees.Select(x => x.Addressee))
        .Where(x => x != null)
        .ToList();
      foreach (var newAddressee in newAddressees)
        _obj.Addressees.AddNew().Addressee = newAddressee;
    }
    
    /// <summary>
    /// Задать адресатов в задаче.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
    
    /// <summary>
    /// Установить срок задачи на рассмотрение документа.
    /// </summary>
    /// <param name="days">Срок в днях.</param>
    /// <param name="hours">Срок в часах.</param>
    [Public]
    public virtual void SetDeadline(int? days, int? hours)
    {
      _obj.Deadline = Calendar.Now.AddWorkingDays(_obj.Author, days ?? 0).AddWorkingHours(_obj.Author, hours ?? 0);
    }
    
    /// <summary>
    /// Проверить наличие документа на рассмотрение в задаче и наличие хоть каких-то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    [Public]
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentForReviewGroup.OfficialDocuments.Any();
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    public virtual void SynchronizeAddendaAndAttachmentsGroup()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
      {
        _obj.AddendaGroup.All.Clear();
        _obj.AddedAddenda.Clear();
        _obj.RemovedAddenda.Clear();
        return;
      }

      // Документы, связанные связью Приложение с основным документом.
      var documentAddenda = Docflow.PublicFunctions.Module.GetAddenda(document);
      // Документы в группе Приложения.
      var taskAddenda = Functions.DocumentReviewTask.GetAddendaGroupAttachments(_obj);
      // Документы в коллекции добавленных вручную документов.
      var taskAddedAddenda = Functions.DocumentReviewTask.GetAddedAddenda(_obj);
      
      // Удалить из гр. Приложения документы, которые не связаны связью приложения и не добавленные вручную.
      var addendaToRemove = taskAddenda.Except(documentAddenda).Where(x => !taskAddedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToRemove)
      {
        _obj.AddendaGroup.All.Remove(addendum);
        this.RemovedAddendaRemove(addendum);
      }
      
      // Добавить документы, связанные связью типа Приложение с основным документом.
      var taskRemovedAddenda = this.GetRemovedAddenda();
      var addendaToAdd = documentAddenda.Except(taskAddenda).Where(x => !taskRemovedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToAdd)
      {
        _obj.AddendaGroup.All.Add(addendum);
        this.AddedAddendaRemove(addendum);
      }
    }
    
    /// <summary>
    /// Синхронизировать вложения задачи на рассмотрение в поручения из проекта резолюции.
    /// </summary>
    /// <remarks>Используется для синхронизации вложений в схеме задачи, когда рассмотрение с созданным проектом резолюции отправили на доработку
    /// и инициатор изменил состав приложений. Полная синхронизация (SynchronizeAttachmentsToActionItem) при этом не требуется.</remarks>
    public virtual void SynchronizeAddendaToDraftResolution()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      // Если основной документ отсутствует, то не синхронизировать приложения.
      if (document == null)
        return;
      
      var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks;
      foreach (var actionItem in actionItems)
        this.SynchronizeDocumentReviewAddendaToActionItem(actionItem);
    }
    
    /// <summary>
    /// Синхронизировать вложения задачи на рассмотрение в поручение из проекта резолюции.
    /// </summary>
    /// <param name="actionItem">Поручение.</param>
    public virtual void SynchronizeDocumentReviewAddendaToActionItem(IActionItemExecutionTask actionItem)
    {
      // Добавить документы в группу Приложения, которые были добавлены в основную задачу. Документ может быть уже добавлен, поэтому повторно не добавляем.
      var addendaToAdd = _obj.AddendaGroup.OfficialDocuments.Except(actionItem.AddendaGroup.All);
      foreach (var addendum in addendaToAdd)
        actionItem.AddendaGroup.All.Add(addendum);
      
      // Удалить документы из группы Приложения, которые были удалены из основной задачи.
      var removedAddendumIds = _obj.RemovedAddenda.Select(x => x.AddendumId);
      foreach (var removedAddendumId in removedAddendumIds)
      {
        var addendum = actionItem.AddendaGroup.All.Where(x => x.Id == removedAddendumId).FirstOrDefault();
        if (addendum != null)
          actionItem.AddendaGroup.All.Remove(addendum);
      }
    }
    
    /// <summary>
    /// Получить вложения группы "Приложения".
    /// </summary>
    /// <returns>Вложения группы "Приложения".</returns>
    public virtual List<IElectronicDocument> GetAddendaGroupAttachments()
    {
      return _obj.AddendaGroup.All
        .Where(x => ElectronicDocuments.Is(x))
        .Select(x => ElectronicDocuments.As(x))
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, добавленных в группу "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    public virtual List<int> GetAddedAddenda()
    {
      return _obj.AddedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, удаленных из группы "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    public virtual List<int> GetRemovedAddenda()
    {
      return _obj.RemovedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void AddedAddendaAppend()
    {
      Logger.DebugFormat("DocumentReviewTask (ID={0}). AddedAddenda append from assignments.", _obj.Id);
      var addedAttachments = Docflow.PublicFunctions.Module.GetAddedAddendaFromAssignments(_obj, Constants.DocumentReviewTask.AddendaGroupGuid);
      foreach (var attachment in addedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.AddedAddendaAppend(attachment);
        this.RemovedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void RemovedAddendaAppend()
    {
      Logger.DebugFormat("DocumentReviewTask (ID={0}). RemovedAddenda append from assignments.", _obj.Id);
      var removedAttachments = Docflow.PublicFunctions.Module.GetRemovedAddendaFromAssignments(_obj, Constants.DocumentReviewTask.AddendaGroupGuid);
      foreach (var attachment in removedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.RemovedAddendaAppend(attachment);
        this.AddedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, добавленный в группу "Приложения".</param>
    public virtual void AddedAddendaAppend(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var addedAddendaItem = _obj.AddedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (addedAddendaItem == null)
      {
        _obj.AddedAddenda.AddNew().AddendumId = addendum.Id;
        Logger.DebugFormat("DocumentReviewTask (ID={0}). Append AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Из коллекции добавленных вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void AddedAddendaRemove(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var addedAddendaItem = _obj.AddedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (addedAddendaItem != null)
      {
        _obj.AddedAddenda.Remove(addedAddendaItem);
        Logger.DebugFormat("DocumentReviewTask (ID={0}). Remove from AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Из коллекции удалённых вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void RemovedAddendaRemove(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var removedAddendaItem = _obj.RemovedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (removedAddendaItem != null)
      {
        _obj.RemovedAddenda.Remove(removedAddendaItem);
        Logger.DebugFormat("DocumentReviewTask (ID={0}). Remove from RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, удаленный вручную из группы "Приложения".</param>
    public virtual void RemovedAddendaAppend(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      if (_obj.RemovedAddenda.Any(x => x.AddendumId == addendum.Id))
        return;
      
      _obj.RemovedAddenda.AddNew().AddendumId = addendum.Id;
      Logger.DebugFormat("DocumentReviewTask (ID={0}). Append RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
    }
    
    #endregion
  }
}
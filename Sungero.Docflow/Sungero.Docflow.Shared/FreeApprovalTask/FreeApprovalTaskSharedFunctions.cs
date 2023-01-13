using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;
using Sungero.Docflow.Structures.FreeApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Shared
{
  partial class FreeApprovalTaskFunctions
  {
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    public virtual List<StartValidationMessage> GetStartValidationMessages()
    {
      var errors = new List<StartValidationMessage>();
      
      // Задачу может отправить только сотрудник.
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Проверить корректность срока.
      if (!Functions.Module.CheckDeadline(_obj.MaxDeadline, Calendar.Now))
        errors.Add(StartValidationMessage.Create(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday, true, false));
      
      var hasDocument = Functions.FreeApprovalTask.HasDocumentAndCanRead(_obj);
      // Проверить наличие документа в задаче и наличие прав на него.
      if (!hasDocument)
        errors.Add(StartValidationMessage.Create(Docflow.Resources.NoRightsToDocument, false, false));
      
      // Проверить права на изменение документа.
      if (hasDocument && !_obj.ForApprovalGroup.ElectronicDocuments.First().AccessRights.CanUpdate())
        errors.Add(StartValidationMessage.Create(FreeApprovalTasks.Resources.CantSendDocumentsWithoutUpdateRights, false, false));
      
      return errors;
    }
    
    /// <summary>
    /// Валидация старта задачи на свободное согласование.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateFreeApprovalTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = this.GetStartValidationMessages();
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else if (error.IsImpossibleSpecifyDeadlineLessThanTodayMessage)
            e.AddError(_obj.Info.Properties.MaxDeadline, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }

    #region Синхронизация группы приложений
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    public virtual void SynchronizeAddendaAndAttachmentsGroup()
    {
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      if (document == null)
      {
        _obj.AddendaGroup.All.Clear();
        _obj.AddedAddenda.Clear();
        _obj.RemovedAddenda.Clear();
        return;
      }

      var documentAddenda = Functions.Module.GetAddenda(document);
      var taskAddenda = Functions.FreeApprovalTask.GetAddendaGroupAttachments(_obj);
      var taskAddedAddenda = Functions.FreeApprovalTask.GetAddedAddenda(_obj);
      var addendaToRemove = taskAddenda.Except(documentAddenda).Where(x => !taskAddedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToRemove)
      {
        _obj.AddendaGroup.All.Remove(addendum);
        this.RemovedAddendaRemove(addendum);
      }
      
      var taskRemovedAddenda = Functions.FreeApprovalTask.GetRemovedAddenda(_obj);
      var addendaToAdd = documentAddenda.Except(taskAddenda).Where(x => !taskRemovedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToAdd)
      {
        _obj.AddendaGroup.All.Add(addendum);
        this.AddedAddendaRemove(addendum);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void AddedAddendaAppend()
    {
      Logger.DebugFormat("FreeApprovalTask (ID={0}). AddedAddenda append from assignments.", _obj.Id);
      var addedAttachments = this.GetAddedAddendaFromAssignments();
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
      Logger.DebugFormat("FreeApprovalTask (ID={0}). RemovedAddenda append from assignments.", _obj.Id);
      var removedAttachments = this.GetRemovedAddendaFromAssignments();
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
        Logger.DebugFormat("FreeApprovalTask (ID={0}). Append AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
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
        Logger.DebugFormat("FreeApprovalTask (ID={0}). Remove from AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документы, удаленные вручную из группы "Приложения".</param>
    public virtual void RemovedAddendaAppend(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var removedAddendaItem = _obj.RemovedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (removedAddendaItem == null)
      {
        _obj.RemovedAddenda.AddNew().AddendumId = addendum.Id;
        Logger.DebugFormat("FreeApprovalTask (ID={0}). Append RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Из коллекции удаленных вручную документов удалить запись о приложении.
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
        Logger.DebugFormat("FreeApprovalTask (ID={0}). Remove from RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
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
    /// Получить список документов, добавленных в группу "Приложения" в заданиях.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IElectronicDocument> GetAddedAddendaFromAssignments()
    {
      return Docflow.Functions.Module.GetAddedAddendaFromAssignments(_obj, Constants.FreeApprovalTask.AddendaGroupGuid);
    }
    
    /// <summary>
    /// Получить список документов, удаленных из группы "Приложения" в заданиях.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IElectronicDocument> GetRemovedAddendaFromAssignments()
    {
      return Docflow.Functions.Module.GetRemovedAddendaFromAssignments(_obj, Constants.FreeApprovalTask.AddendaGroupGuid);
    }
    
    #endregion
    
    /// <summary>
    /// Проверить наличие документа в задаче и наличие прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.ForApprovalGroup.ElectronicDocuments.Any();
    }
  }
}
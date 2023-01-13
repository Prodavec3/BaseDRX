using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement.Shared
{
  partial class AcquaintanceTaskFunctions
  {

    /// <summary>
    /// Валидация старта задачи на ознакомление.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateAcquaintanceTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = Sungero.RecordManagement.Functions.AcquaintanceTask.Remote.GetStartValidationMessage(_obj);
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsShowNotAutomatedEmployeesMessage)
            e.AddError(error.Message, _obj.Info.Actions.ShowNotAutomatedEmployees);
          else if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Сохранить номер версии и хеш документа в задаче.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isMainDocument">Признак главного документа.</param>
    public void StoreAcquaintanceVersion(IElectronicDocument document, bool isMainDocument)
    {
      var lastVersion = document.LastVersion;
      var mainDocumentVersion = _obj.AcquaintanceVersions.AddNew();
      mainDocumentVersion.IsMainDocument = isMainDocument;
      mainDocumentVersion.DocumentId = document.Id;
      if (lastVersion != null)
      {
        mainDocumentVersion.Number = lastVersion.Number;
        mainDocumentVersion.Hash = lastVersion.Body.Hash;
      }
      else
      {
        mainDocumentVersion.Number = 0;
        mainDocumentVersion.Hash = null;
      }
    }
    
    /// <summary>
    /// Доступность действия "Исключить из ознакомления".
    /// </summary>
    /// <returns>True - если доступно, иначе - False.</returns>
    public bool SchemeVersionSupportsExcludeFromAcquaintance()
    {
      return _obj.GetStartedSchemeVersion() >= LayerSchemeVersions.V1;
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    public virtual void SynchronizeAddendaAndAttachmentsGroup()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
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
      var taskAddenda = Functions.AcquaintanceTask.GetAddendaGroupAttachments(_obj);
      // Документы в коллекции добавленных вручную документов.
      var taskAddedAddenda = Functions.AcquaintanceTask.GetAddedAddenda(_obj);
      
      // Удалить из гр. Приложения документы, которые не связаны связью "Приложение" и не добавлены вручную.
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
      Logger.DebugFormat("AcquaintanceTask (ID={0}). Append to AddedAddenda from assignments.", _obj.Id);
      var addedAttachments = Docflow.PublicFunctions.Module.GetAddedAddendaFromAssignments(_obj, Constants.AcquaintanceTask.AddendaGroupGuid);
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
      Logger.DebugFormat("AcquaintanceTask (ID={0}). Append to RemovedAddenda from assignments.", _obj.Id);
      var removedAttachments = Docflow.PublicFunctions.Module.GetRemovedAddendaFromAssignments(_obj, Constants.AcquaintanceTask.AddendaGroupGuid);
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
        Logger.DebugFormat("AcquaintanceTask (ID={0}). Append to AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
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
        Logger.DebugFormat("AcquaintanceTask (ID={0}). Remove from AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
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
        Logger.DebugFormat("AcquaintanceTask (ID={0}). Remove from RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
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
      Logger.DebugFormat("AcquaintanceTask (ID={0}). Append to RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
    }
    
    #endregion

    /// <summary>
    /// Проверить наличие документа в задаче и наличие прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }
    
    /// <summary>
    /// Заполнить участников из списка ознакомления.
    /// </summary>
    /// <param name="acquaintanceList">Список ознакомления.</param>
    [Public]
    public void FillFromAcquaintanceList(IAcquaintanceList acquaintanceList)
    {
      if (acquaintanceList == null)
        return;
      
      var participants = acquaintanceList.Participants.Where(p => p.Participant.Status == Company.Employee.Status.Active);
      foreach (var participant in participants)
      {
        var newParticipantRow = _obj.Performers.AddNew();
        newParticipantRow.Performer = participant.Participant;
      }
      foreach (var excludedParticipant in acquaintanceList.ExcludedParticipants)
      {
        var newExcludedPerformer = _obj.ExcludedPerformers.AddNew();
        newExcludedPerformer.ExcludedPerformer = excludedParticipant.ExcludedParticipant;
      }
    }
  }
}
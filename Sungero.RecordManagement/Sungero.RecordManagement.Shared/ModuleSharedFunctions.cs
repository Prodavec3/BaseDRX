using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.RecordManagement.Shared
{
  public class ModuleFunctions
  {
    
    #region Параметры модуля
    
    /// <summary>
    /// Получить параметры модуля.
    /// </summary>
    /// <returns>Параметры модуля.</returns>
    [Public]
    public virtual IRecordManagementSetting GetSettings()
    {
      return RecordManagementSettings.GetAllCached().SingleOrDefault();
    }
    
    /// <summary>
    /// Разрешены ли бессрочные поручения.
    /// </summary>
    /// <returns>true, если разрешены ли бессрочные поручения.</returns>
    [Public]
    public virtual bool AllowActionItemsWithIndefiniteDeadline()
    {
      return this.GetSettings().AllowActionItemsWithIndefiniteDeadline == true;
    }
    
    /// <summary>
    /// Разрешено ли ознакомление по замещению.
    /// </summary>
    /// <returns>True, если разрешено ознакомление по замещению.</returns>
    [Public]
    public virtual bool AllowAcquaintanceBySubstitute()
    {
      return this.GetSettings().AllowAcquaintanceBySubstitute == true;
    }
    
    #endregion
    
    #region статус "Исполнение"
    
    /// <summary>
    /// Получить приоритеты для статусов исполнения.
    /// </summary>
    /// <returns>Словарь с приоритетами статусов исполнения.</returns>
    [Public]
    public virtual System.Collections.Generic.IDictionary<Enumeration?, int> GetExecutionStatePriorities()
    {
      var priorities = new Dictionary<Enumeration?, int>();
      priorities.Add(ExecutionState.OnExecution, 110);
      priorities.Add(ExecutionState.Sending, 100);
      priorities.Add(ExecutionState.OnReview, 90);
      priorities.Add(ExecutionState.Executed, 80);
      priorities.Add(ExecutionState.WithoutExecut, 70);
      priorities.Add(ExecutionState.Aborted, 0);
      return priorities;
    }
    
    #endregion
    
    #region Синхронизация вложений из задач в поручения
    
    /// <summary>
    /// Синхронизировать вложения из родительской задачи во вновь созданное поручение.
    /// </summary>
    /// <param name="parentTask">Родительская задача.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <remarks>Вложения копируются из задач на согласование по регламенту, рассмотрение документа и на исполнение поручения.</remarks>
    [Public]
    public virtual void SynchronizeAttachmentsToActionItem(Sungero.Workflow.ITask parentTask, IActionItemExecutionTask actionItem)
    {
      if (DocumentReviewTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromDocumentReviewToActionItem(DocumentReviewTasks.As(parentTask), actionItem);
      else if (ActionItemExecutionTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromActionItemToActionItem(ActionItemExecutionTasks.As(parentTask), actionItem);
      else if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromApprovalToActionItem(Sungero.Docflow.ApprovalTasks.As(parentTask), actionItem);
    }
    
    /// <summary>
    /// Синхронизировать вложения из задачи на рассмотрение во вновь созданное поручение.
    /// </summary>
    /// <param name="documentReview">Задача на рассмотрение документа.</param>
    /// <param name="actionItem">Поручение.</param>
    public virtual void SynchronizeAttachmentsFromDocumentReviewToActionItem(IDocumentReviewTask documentReview, IActionItemExecutionTask actionItem)
    {
      var primaryDocument = documentReview.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Functions.DocumentReviewTask.GetAddendaGroupAttachments(documentReview);
      var addedAddendaIds = Functions.DocumentReviewTask.GetAddedAddenda(documentReview);
      var removedAddendaIds = Functions.DocumentReviewTask.GetRemovedAddenda(documentReview);
      var otherAttachments = documentReview.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToActionItem(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, actionItem);
    }
    
    /// <summary>
    /// Синхронизировать вложения из родительского поручения во вновь созданное подчинённое.
    /// </summary>
    /// <param name="parentActionItem">Родительское поручение.</param>
    /// <param name="actionItem">Подчинённое поручение.</param>
    /// <remarks>Также работает для синхронизации из составного поручения в пункты или в поручения соисполнителям.</remarks>
    public virtual void SynchronizeAttachmentsFromActionItemToActionItem(IActionItemExecutionTask parentActionItem, IActionItemExecutionTask actionItem)
    {
      var primaryDocument = parentActionItem.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Functions.ActionItemExecutionTask.GetAddendaGroupAttachments(parentActionItem);
      var addedAddendaIds = Functions.ActionItemExecutionTask.GetAddedAddenda(parentActionItem);
      var removedAddendaIds = Functions.ActionItemExecutionTask.GetRemovedAddenda(parentActionItem);
      var otherAttachments = parentActionItem.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToActionItem(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, actionItem);
    }
    
    /// <summary>
    /// Синхронизировать вложения из задачи на согласование по регламенту во вновь созданное поручение.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="actionItem">Поручение.</param>
    public virtual void SynchronizeAttachmentsFromApprovalToActionItem(Sungero.Docflow.IApprovalTask approvalTask, IActionItemExecutionTask actionItem)
    {
      var primaryDocument = approvalTask.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddendaGroupAttachments(approvalTask)
        .Select(x => Sungero.Content.ElectronicDocuments.As(x))
        .ToList();
      var addedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddedAddenda(approvalTask);
      var removedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetRemovedAddenda(approvalTask);
      var otherAttachments = approvalTask.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToActionItem(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, actionItem);
    }
    
    /// <summary>
    /// Синхронизировать вложения во вновь созданное поручение.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="addenda">Вложения из группы "Приложения".</param>
    /// <param name="addedAddendaIds">Список ИД добавленных приложений.</param>
    /// <param name="removedAddendaIds">Список ИД удалённых приложений.</param>
    /// <param name="otherAttachments">Вложения из группы "Дополнительно".</param>
    /// <param name="actionItem">Поручение, в которое будут добавлены вложения.</param>
    [Public]
    public virtual void SynchronizeAttachmentsToActionItem(Sungero.Docflow.IOfficialDocument primaryDocument,
                                                           List<Sungero.Content.IElectronicDocument> addenda,
                                                           List<int> addedAddendaIds,
                                                           List<int> removedAddendaIds,
                                                           List<Sungero.Domain.Shared.IEntity> otherAttachments,
                                                           IActionItemExecutionTask actionItem)
    {
      // Синхронизация коллекции приложений, добавленных вручную.
      actionItem.AddedAddenda.Clear();
      foreach (var addendumId in addedAddendaIds)
        actionItem.AddedAddenda.AddNew().AddendumId = addendumId;
      
      // Синхронизация коллекции приложений, удаленных вручную.
      actionItem.RemovedAddenda.Clear();
      foreach (var addendumId in removedAddendaIds)
        actionItem.RemovedAddenda.AddNew().AddendumId = addendumId;
      
      // При заполнении основного документа будет выполнена синхронизация приложений.
      if (primaryDocument != null && !actionItem.DocumentsGroup.OfficialDocuments.Any())
        actionItem.DocumentsGroup.OfficialDocuments.Add(primaryDocument);
      
      // Добавить в группу "Приложения" документы, которые есть в основной задаче. Документ может быть уже добавлен, поэтому повторно не добавляем.
      // Устаревшие документы добавляются, только если они были добавлены вручную.
      var addendaToAdd = addenda.Except(actionItem.AddendaGroup.All)
        .Where(x => Docflow.OfficialDocuments.Is(x))
        .Select(x => Docflow.OfficialDocuments.As(x))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x) || addedAddendaIds.Contains(x.Id));
      foreach (var addendum in addendaToAdd)
        actionItem.AddendaGroup.OfficialDocuments.Add(addendum);
      
      // Удалить из группы "Приложения" документы, которые были удалены из задачи.
      foreach (var addendum in actionItem.AddendaGroup.OfficialDocuments)
      {
        if (removedAddendaIds.Contains(addendum.Id))
          actionItem.AddendaGroup.OfficialDocuments.Remove(addendum);
      }
      
      // Синхронизировать группу "Дополнительно".
      actionItem.OtherGroup.All.Clear();
      foreach (var entity in otherAttachments)
        actionItem.OtherGroup.All.Add(entity);
    }
    
    #endregion
    
    #region Синхронизация вложений из задач в рассмотрение
    
    /// <summary>
    /// Синхронизировать вложения из родительской задачи во вновь созданную задачу на рассмотрение.
    /// </summary>
    /// <param name="parentTask">Родительская задача.</param>
    /// <param name="documentReview">Задача на рассмотрение документа.</param>
    /// <remarks>Вложения копируются из задачи на согласование по регламенту и задачи на рассмотрение документа.</remarks>
    [Public]
    public virtual void SynchronizeAttachmentsToDocumentReview(Sungero.Workflow.ITask parentTask, IDocumentReviewTask documentReview)
    {
      if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromApprovalToDocumentReview(Sungero.Docflow.ApprovalTasks.As(parentTask), documentReview);
      else if (DocumentReviewTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromDocumentReviewToDocumentReview(DocumentReviewTasks.As(parentTask), documentReview);
    }
    
    /// <summary>
    /// Синхронизировать вложения из задачи на согласование по регламенту во вновь созданную задачу на рассмотрение.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="documentReview">Задача на рассмотрение документа.</param>
    public virtual void SynchronizeAttachmentsFromApprovalToDocumentReview(Sungero.Docflow.IApprovalTask approvalTask, IDocumentReviewTask documentReview)
    {
      var primaryDocument = approvalTask.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddendaGroupAttachments(approvalTask)
        .Select(x => Sungero.Content.ElectronicDocuments.As(x))
        .ToList();
      var addedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddedAddenda(approvalTask);
      var removedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetRemovedAddenda(approvalTask);
      var otherAttachments = approvalTask.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToDocumentReview(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, documentReview);
    }
    
    /// <summary>
    /// Синхронизировать вложения из главной задачи на рассмотрение в подчинённую.
    /// </summary>
    /// <param name="parentDocumentReview">Главная задача на рассмотрение.</param>
    /// <param name="documentReview">Подчинённая задача на рассмотрение.</param>
    /// <remarks>Используется для синхронизации вложений в многоадресном рассмотрении.</remarks>
    public virtual void SynchronizeAttachmentsFromDocumentReviewToDocumentReview(IDocumentReviewTask parentDocumentReview, IDocumentReviewTask documentReview)
    {
      var primaryDocument = parentDocumentReview.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Functions.DocumentReviewTask.GetAddendaGroupAttachments(parentDocumentReview);
      var addedAddendaIds = Functions.DocumentReviewTask.GetAddedAddenda(parentDocumentReview);
      var removedAddendaIds = Functions.DocumentReviewTask.GetRemovedAddenda(parentDocumentReview);
      var otherAttachments = parentDocumentReview.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToDocumentReview(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, documentReview);
    }
    
    /// <summary>
    /// Синхронизировать вложения во вновь созданную задачу на рассмотрение.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="addenda">Вложения из группы "Приложения".</param>
    /// <param name="addedAddendaIds">Список ИД добавленных приложений.</param>
    /// <param name="removedAddendaIds">Список ИД удалённых приложений.</param>
    /// <param name="otherAttachments">Вложения из группы "Дополнительно".</param>
    /// <param name="documentReview">Задача на рассмотрение документа.</param>
    [Public]
    public virtual void SynchronizeAttachmentsToDocumentReview(Sungero.Docflow.IOfficialDocument primaryDocument,
                                                               List<Sungero.Content.IElectronicDocument> addenda,
                                                               List<int> addedAddendaIds,
                                                               List<int> removedAddendaIds,
                                                               List<Sungero.Domain.Shared.IEntity> otherAttachments,
                                                               IDocumentReviewTask documentReview)
    {
      // Синхронизация коллекции приложений, добавленных вручную.
      documentReview.AddedAddenda.Clear();
      foreach (var addendumId in addedAddendaIds)
        documentReview.AddedAddenda.AddNew().AddendumId = addendumId;
      
      // Синхронизация коллекции приложений, удаленных вручную.
      documentReview.RemovedAddenda.Clear();
      foreach (var addendumId in removedAddendaIds)
        documentReview.RemovedAddenda.AddNew().AddendumId = addendumId;
      
      // При заполнении основного документа будет выполнена синхронизация приложений.
      if (primaryDocument != null && !documentReview.DocumentForReviewGroup.OfficialDocuments.Any())
        documentReview.DocumentForReviewGroup.OfficialDocuments.Add(primaryDocument);
      
      // Добавить в группу "Приложения" документы, которые есть в основной задаче. Документ может быть уже добавлен, поэтому повторно не добавляем.
      // Устаревшие документы добавляются, только если они были добавлены вручную.
      var addendaToAdd = addenda.Except(documentReview.AddendaGroup.All)
        .Where(x => Docflow.OfficialDocuments.Is(x))
        .Select(x => Docflow.OfficialDocuments.As(x))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x) || addedAddendaIds.Contains(x.Id));
      foreach (var addendum in addendaToAdd)
        documentReview.AddendaGroup.OfficialDocuments.Add(addendum);
      
      // Удалить из группы "Приложения" документы, которые были удалены из задачи.
      foreach (var addendum in documentReview.AddendaGroup.OfficialDocuments)
      {
        if (removedAddendaIds.Contains(addendum.Id))
          documentReview.AddendaGroup.OfficialDocuments.Remove(addendum);
      }
      
      // Синхронизировать группу "Дополнительно".
      documentReview.OtherGroup.All.Clear();
      foreach (var entity in otherAttachments)
        documentReview.OtherGroup.All.Add(entity);
    }
    
    #endregion

    #region Синхронизация вложений из задач в ознакомление
    
    /// <summary>
    /// Синхронизировать вложения из родительской задачи во вновь созданную задачу на ознакомление.
    /// </summary>
    /// <param name="parentTask">Родительская задача.</param>
    /// <param name="acquaintanceTask">Задача на ознакомление с документом.</param>
    /// <remarks>Вложения копируются из задачи на согласование по регламенту.</remarks>
    [Public]
    public virtual void SynchronizeAttachmentsToAcquaintance(Sungero.Workflow.ITask parentTask, IAcquaintanceTask acquaintanceTask)
    {
      if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        this.SynchronizeAttachmentsFromApprovalToAcquaintance(Sungero.Docflow.ApprovalTasks.As(parentTask), acquaintanceTask);
    }
    
    /// <summary>
    /// Синхронизировать вложения из задачи на согласование по регламенту во вновь созданную задачу на ознакомление.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="acquaintanceTask">Задача на ознакомление с документом.</param>
    public virtual void SynchronizeAttachmentsFromApprovalToAcquaintance(Sungero.Docflow.IApprovalTask approvalTask, IAcquaintanceTask acquaintanceTask)
    {
      var primaryDocument = approvalTask.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var taskAddenda = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddendaGroupAttachments(approvalTask)
        .Select(x => Sungero.Content.ElectronicDocuments.As(x))
        .ToList();
      var addedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetAddedAddenda(approvalTask);
      var removedAddendaIds = Sungero.Docflow.PublicFunctions.ApprovalTask.GetRemovedAddenda(approvalTask);
      var otherAttachments = approvalTask.OtherGroup.All.ToList();
      this.SynchronizeAttachmentsToAcquaintance(primaryDocument, taskAddenda, addedAddendaIds, removedAddendaIds, otherAttachments, acquaintanceTask);
    }
    
    /// <summary>
    /// Синхронизировать вложения во вновь созданную задачу на ознакомление.
    /// </summary>
    /// <param name="primaryDocument">Основной документ.</param>
    /// <param name="addenda">Вложения из группы "Приложения".</param>
    /// <param name="addedAddendaIds">Список ИД добавленных приложений.</param>
    /// <param name="removedAddendaIds">Список ИД удалённых приложений.</param>
    /// <param name="otherAttachments">Вложения из группы "Дополнительно".</param>
    /// <param name="acquaintanceTask">Задача на ознакомление с документом.</param>
    [Public]
    public virtual void SynchronizeAttachmentsToAcquaintance(Sungero.Docflow.IOfficialDocument primaryDocument,
                                                             List<Sungero.Content.IElectronicDocument> addenda,
                                                             List<int> addedAddendaIds,
                                                             List<int> removedAddendaIds,
                                                             List<Sungero.Domain.Shared.IEntity> otherAttachments,
                                                             IAcquaintanceTask acquaintanceTask)
    {
      // Синхронизация коллекции приложений, добавленных вручную.
      acquaintanceTask.AddedAddenda.Clear();
      foreach (var addendumId in addedAddendaIds)
        acquaintanceTask.AddedAddenda.AddNew().AddendumId = addendumId;
      
      // Синхронизация коллекции приложений, удаленных вручную.
      acquaintanceTask.RemovedAddenda.Clear();
      foreach (var addendumId in removedAddendaIds)
        acquaintanceTask.RemovedAddenda.AddNew().AddendumId = addendumId;
      
      // При заполнении основного документа будет выполнена синхронизация приложений.
      if (primaryDocument != null && !acquaintanceTask.DocumentGroup.OfficialDocuments.Any())
        acquaintanceTask.DocumentGroup.OfficialDocuments.Add(primaryDocument);
      
      // Добавить в группу "Приложения" документы, которые есть в основной задаче. Документ может быть уже добавлен, поэтому повторно не добавляем.
      // Устаревшие документы добавляются, только если они были добавлены вручную.
      var addendaToAdd = addenda.Except(acquaintanceTask.AddendaGroup.OfficialDocuments)
        .Where(x => Docflow.OfficialDocuments.Is(x))
        .Select(x => Docflow.OfficialDocuments.As(x))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x) || addedAddendaIds.Contains(x.Id));
      foreach (var addendum in addendaToAdd)
        acquaintanceTask.AddendaGroup.OfficialDocuments.Add(addendum);
      
      // Удалить из группы "Приложения" документы, которые были удалены из задачи.
      foreach (var addendum in acquaintanceTask.AddendaGroup.OfficialDocuments)
      {
        if (removedAddendaIds.Contains(addendum.Id))
          acquaintanceTask.AddendaGroup.OfficialDocuments.Remove(addendum);
      }
      
      // Синхронизировать группу "Дополнительно".
      acquaintanceTask.OtherGroup.All.Clear();
      foreach (var entity in otherAttachments)
        acquaintanceTask.OtherGroup.All.Add(entity);
    }
    
    #endregion

  }
}
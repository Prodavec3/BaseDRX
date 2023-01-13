using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionTaskRouteHandlers
  {

    #region - 113 - Мониторинг ожидания выполнения родительского задания
    
    public virtual bool Monitoring113Result()
    {
      return _obj.WaitForParentAssignment != true || _obj.ParentAssignment == null || _obj.ParentAssignment.Status == Workflow.Assignment.Status.Completed;
    }
    
    #endregion
    
    #region - 112 - Корректировка поручения
    [Obsolete("Логика перенесена в асинхронный обработчик ApplyActionItemLockIndependentChanges")]
    public virtual void Script112Execute()
    {
    }
    
    #endregion

    #region - 110 - Мониторинг начала корректировки
    [Obsolete("Логика перенесена в асинхронный обработчик ApplyActionItemLockIndependentChanges")]
    public virtual bool Monitoring110Result()
    {
      return false;
    }

    public virtual void StartBlock110(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      e.Block.Period = TimeSpan.FromDays(10000);
    }
    
    #endregion
    
    #region - 109 - Завершение корректировки
    [Obsolete("Логика перенесена в асинхронный обработчик ApplyActionItemLockDependentChanges")]
    public virtual void Script109Execute()
    {
    }
    
    #endregion

    #region - 100 - Синхронизация результатов исполнения
    
    public virtual void Script100Execute()
    {
      // Добавить документы из группы "Результаты исполнения" в ведущее задание на исполнение.
      Functions.ActionItemExecutionTask.SynchronizeResultGroup(_obj);
      
      // Автоматически выполнить ведущее поручение.
      if (Functions.ActionItemExecutionTask.CanAutoExecParentAssignment(_obj))
      {
        Functions.ActionItemExecutionTask.SynchronizeResultActiveText(_obj);
        Functions.ActionItemExecutionTask.CompleteParentAssignment(_obj);
        Functions.ActionItemExecutionTask.SetCompletedByInParentAssignment(_obj);
      }
    }
    
    #endregion
    
    #region - 2 - Уведомление контролеру

    public virtual void StartBlock2(Sungero.RecordManagement.Server.ActionItemSupervisorNotificationArguments e)
    {
      Logger.DebugFormat("ActionItemExecutionTask({0}) StartBlock2", _obj.Id);
      // Задать тему.
      e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ControlNoticeSubject);
      
      // Задать исполнителя.
      if (Functions.ActionItemExecutionTask.NeedSendSupervisorNotice(_obj))
        e.Block.Performers.Add(_obj.Supervisor);

      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.DocumentsGroup.All.ToList(), true);
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.AddendaGroup.All.ToList(), true);
      
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
        var nonStartedActiveTasks = ActionItemExecutionTasks
          .GetAll(t => t.AttachmentDetails.Any(a => a.GroupId == documentsGroupGuid && a.AttachmentId == document.Id) && t.Status == Workflow.Task.Status.InProcess && t.Id != _obj.Id)
          .ToList();

        foreach (var task in nonStartedActiveTasks)
        {
          var hasAssignments = Workflow.Assignments.GetAll(s => Equals(task, s.Task)).Any();
          var hasSubTasks = Workflow.Tasks.GetAll(s => Equals(task, s.ParentTask)).Any();
          if (!hasAssignments && !hasSubTasks)
          {
            Logger.DebugFormat("Granting rights for task {0}. Current Task: {1}", task.Id, _obj.Id);
            Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(task, task.DocumentsGroup.All.ToList(), true);
            Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(task, task.AddendaGroup.All.ToList(), true);
          }
        }
      }
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      // Выдача прав соисполнителям и составным, чтобы Script10Execute в цикле не зависал при блокировках документа.
      if (document != null)
      {
        var assignees = _obj.CoAssignees.Select(a => a.Assignee);
        var partAssignees = _obj.ActionItemParts.Select(p => p.Assignee);
        foreach (var assignee in assignees.Concat(partAssignees))
        {
          if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, assignee))
            document.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Read);
        }
      }
      
      // Задать состояние поручения.
      if (_obj.ExecutionState != ExecutionState.OnRework && _obj.Assignee != null)
        _obj.ExecutionState = ExecutionState.OnExecution;
      
      // Заполнить исполнителя, если это первое поручение по документу.
      if (document != null && document.Assignee == null && document.State.Properties.Assignee.IsVisible)
      {
        document.Assignee = _obj.Assignee;
      }
      
      // Обновить статус исполнения документа.
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
    }
    
    public virtual void StartNotice2(Sungero.RecordManagement.IActionItemSupervisorNotification notice, Sungero.RecordManagement.Server.ActionItemSupervisorNotificationArguments e)
    {
      notice.Importance = _obj.Importance;
    }
    
    public virtual void EndBlock2(Sungero.RecordManagement.Server.ActionItemSupervisorNotificationEndBlockEventArguments e)
    {
    }

    #endregion
    
    #region - 3 - Есть исполнитель?

    public virtual bool Decision3Result()
    {
      return _obj.Assignee != null;
    }
    
    #endregion
    
    #region - 4 - Исполнение поручения

    public virtual void StartBlock4(Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      // Задать тему, исполнителей и срок.
      e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ActionItemExecutionSubject);
      
      e.Block.Performers.Add(_obj.Assignee);
      if (_obj.Deadline.HasValue)
      {
        e.Block.AbsoluteDeadline = _obj.Deadline.Value;
        e.Block.ScheduledDate = _obj.Deadline.Value;
      }
      
      // Для подзадач соисполнителям заполнять "Выдал" из основной задачи.
      IActionItemExecutionTask actionItemTask = null;
      if (_obj.ActionItemType != ActionItemType.Main)
      {
        var mainActionItemExecution = ActionItemExecutionTasks.As(_obj.MainTask);
        if (mainActionItemExecution != null && !(mainActionItemExecution.IsCompoundActionItem ?? false))
          actionItemTask = mainActionItemExecution;
      }
      if (actionItemTask == null)
        actionItemTask = _obj;
      e.Block.AssignedBy = actionItemTask.AssignedBy;
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }
    
    public virtual void StartAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      assignment.ActionItem = _obj.ActionItem;
      assignment.Importance = _obj.Importance;
      if (_obj.ActionItemType == ActionItemType.Additional)
        assignment.Author = Sungero.RecordManagement.ActionItemExecutionTasks.As(_obj.ParentAssignment.Task).AssignedBy;
      
      // Выдать права на документ.
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        Docflow.PublicFunctions.OfficialDocument.GrantAccessRightsToActionItemAttachment(document, _obj.Assignee);
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
      
      Functions.ActionItemExecutionTask.GrantAccessRightsToAssignee(_obj, _obj.ResultGroup.All.ToList());
      Functions.ActionItemExecutionTask.GrantAccessRightsToAssignee(_obj, _obj.DocumentsGroup.All.ToList());
      Functions.ActionItemExecutionTask.GrantAccessRightsToAssignee(_obj, _obj.AddendaGroup.All.ToList());
    }

    public virtual void CompleteAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      // Переписка.
      _obj.Report = assignment.ActiveText;
      
      // Прекратить задание на продление срока, если оно есть.
      // Устаревший тип задания, оставлен для совместимости.
      var extendDeadlineTasks = DeadlineExtensionTasks.GetAll(j => Equals(j.ParentAssignment, assignment) &&
                                                              j.Status == Workflow.Task.Status.InProcess);
      foreach (var extendDeadlineTask in extendDeadlineTasks)
        extendDeadlineTask.Abort();
      
      // Прекратить задание на продление срока, если оно есть.
      var newExtendDeadlineTasks = Docflow.DeadlineExtensionTasks.GetAll(j => Equals(j.ParentAssignment, assignment) &&
                                                                         j.Status == Workflow.Task.Status.InProcess);
      foreach (var newExtendDeadlineTask in newExtendDeadlineTasks)
        newExtendDeadlineTask.Abort();
      
      // Прекратить задачи на запрос отчета, созданные из текущей задачи.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTask(_obj);
      
      // Прекратить задачи на запрос отчета, созданные из родительского задания.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromAssignmentToAssignee(_obj,
                                                                                               ActionItemExecutionAssignments.As(_obj.ParentAssignment),
                                                                                               _obj.Assignee);
      
      // Прекратить задачи на запрос отчета, созданные из составного поручения исполнителю пункта.
      if (ActionItemExecutionTasks.As(_obj.ParentTask)?.IsCompoundActionItem == true)
      {
        Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTaskToAssignee(_obj,
                                                                                           ActionItemExecutionTasks.As(_obj.ParentTask),
                                                                                           _obj.Assignee);
      }

      // Рекурсивно прекратить подзадачи.
      if (assignment.NeedAbortChildActionItems ?? false)
      {
        var notCompletedExecutionSubTasks = Functions.ActionItemExecutionAssignment.GetNotCompletedSubActionItems(assignment);
        foreach (var subTask in notCompletedExecutionSubTasks)
        {
          Functions.Module.AbortSubtasksAndSendNotices(subTask, assignment.Performer, ActionItemExecutionTasks.Resources.AutoAbortingReason);
          subTask.Abort();
        }
        
        var otherTasksToAbort = new List<ITask>();
        otherTasksToAbort.AddRange(Functions.ActionItemExecutionAssignment.GetNotCompletedSubDeadlineExtensionTasks(assignment));
        otherTasksToAbort.AddRange(Functions.ActionItemExecutionAssignment.GetNotCompletedSubReportRequestTasks(assignment));
        foreach (var task in otherTasksToAbort)
          task.Abort();
      }
      
      // Выдать права на вложенные документы.
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.ResultGroup.All.ToList(), false);
      
      // Связать документы из группы "Результаты исполнения" с основным документом.
      var mainDocument = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (mainDocument != null)
      {
        foreach (var document in _obj.ResultGroup.OfficialDocuments.Where(d => !Equals(d, mainDocument)))
        {
          document.Relations.AddFrom(Constants.Module.SimpleRelationRelationName, mainDocument);
          document.Save();
        }
      }
    }
    
    public virtual void EndBlock4(Sungero.RecordManagement.Server.ActionItemExecutionAssignmentEndBlockEventArguments e)
    {
      // Заполнить фактическую дату завершения исполнения поручения.
      var assignment = e.CreatedAssignments
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      var completed = assignment.Completed;
      if (completed != null)
      {
        _obj.ActualDate = e.Block.AbsoluteDeadline.HasTime()
          ? completed
          : completed.ToUserTime(assignment.Performer).Value.Date;
      }
    }

    #endregion
    
    #region - 5 - Нужен контроль?

    public virtual bool Decision5Result()
    {
      return _obj.Supervisor != null && _obj.Supervisor != _obj.Assignee;
    }
    
    #endregion
    
    #region - 6 - Контроль
    
    public virtual void StartBlock6(Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      // Задать тему, исполнителей и срок.
      e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ControlWorkFromJob);
      var controller = _obj.Supervisor;
      if (controller != null)
        e.Block.Performers.Add(controller);
      
      Functions.ActionItemExecutionTask.SetControlRelativeDeadline(_obj, e);
      
      // Задать состояние поручения.
      _obj.ExecutionState = ExecutionState.OnControl;
      
      // Заполнить даты поручения.
      e.Block.ScheduledDate = _obj.Deadline;
      
      // Для подзадач соисполнителям заполнять данными из основной задачи.
      if (_obj.ActionItemType != ActionItemType.Main)
      {
        var mainActionItemExecution = ActionItemExecutionTasks.As(_obj.MainTask);
        if (mainActionItemExecution != null && !(mainActionItemExecution.IsCompoundActionItem ?? false))
        {
          // Задать автора.
          e.Block.AssignedBy = mainActionItemExecution.AssignedBy;
        }
      }
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }
    
    public virtual void StartAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      assignment.Author = _obj.Assignee;
      assignment.ActionItem = _obj.ActionItem;
      assignment.Importance = _obj.Importance;
      if (_obj.HasIndefiniteDeadline != true)
        assignment.NewDeadline = _obj.Deadline;
      assignment.AssignedBy = _obj.AssignedBy;
      
      // Выдать права на вложенные документы.
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.ResultGroup.All.ToList(), false);
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.DocumentsGroup.All.ToList(), false);
      Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(_obj, _obj.AddendaGroup.All.ToList(), false);
      
      // Выдать права на изменение для возможности прекращения задачи.
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }

    public virtual void CompleteAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      // Переписка.
      _obj.ReportNote = assignment.ActiveText;
    }
    
    public virtual void EndBlock6(Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentEndBlockEventArguments e)
    {
      var assignment = e.CreatedAssignments.OrderByDescending(a => a.Created).FirstOrDefault();
      if (assignment != null && assignment.Result == Result.ForRework)
      {
        _obj.ExecutionState = ExecutionState.OnRework;
        var newDeadline = ActionItemSupervisorAssignments.As(assignment).NewDeadline;
        if (_obj.Deadline != newDeadline)
          Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(_obj, true, true);
        _obj.Deadline = newDeadline;
        
        if (_obj.ActionItemType == ActionItemType.Component && ActionItemExecutionTasks.Is(_obj.ParentTask))
        {
          var rootTask = ActionItemExecutionTasks.As(_obj.ParentTask);
          var actionItem = rootTask.ActionItemParts.Where(i => Equals(i.ActionItemPartExecutionTask, _obj)).FirstOrDefault();
          if (actionItem != null && (actionItem.Deadline != null || rootTask.FinalDeadline != newDeadline))
          {
            if (actionItem.Deadline != newDeadline)
              Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(rootTask, true, true);
            actionItem.Deadline = newDeadline;
          }
        }
      }
    }
    
    #endregion
    
    #region - 8 - Уведомления о приемке поручения
    
    public virtual void StartBlock8(Sungero.RecordManagement.Server.ActionItemExecutionNotificationArguments e)
    {
      Logger.DebugFormat("ActionItemExecutionTask({0}) StartBlock8", _obj.Id);
      if (_obj.Supervisor != null)
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsAccepted);
      else
      {
        var employee = Company.PublicFunctions.Employee.GetShortName(_obj.Assignee, false);
        var begin = ActionItemExecutionTasks.Resources.WorkFromActionItemIsCompletedFormat(employee);
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, begin);
      }
      
      if (_obj.Supervisor != null && _obj.Assignee != _obj.Supervisor)
        e.Block.Performers.Add(_obj.Assignee);
      
      Sungero.CoreEntities.IUser initiator;
      if (_obj.ActionItemType == ActionItemType.Component && _obj.ParentTask != null)
        initiator = _obj.ParentTask.StartedBy ?? _obj.ParentTask.Author;
      else if (_obj.ActionItemType == ActionItemType.Additional)
        initiator = _obj.Author;
      else
        initiator = _obj.StartedBy ?? _obj.Author;

      if (initiator != _obj.Supervisor && !e.Block.Performers.Contains(initiator))
        e.Block.Performers.Add(initiator);
      
      // Задать состояние поручения.
      _obj.ExecutionState = ExecutionState.Executed;
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }
    
    public virtual void StartNotice8(Sungero.RecordManagement.IActionItemExecutionNotification notice, Sungero.RecordManagement.Server.ActionItemExecutionNotificationArguments e)
    {
      Logger.DebugFormat("ActionItemExecutionTask({0}) StartNotice8", _obj.Id);
      if (_obj.Supervisor != null)
        notice.Author = _obj.Supervisor;
      else
      {
        notice.Author = _obj.Assignee;
        notice.ThreadSubject = ActionItemExecutionTasks.Resources.ActionItemExecutionNotificationThreadSubject;
      }
      notice.Importance = _obj.Importance;
      
      // Обновить статус исполнения документа - исполнен, статус контроля исполнения - снято с контроля.
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
    }
    
    public virtual void EndBlock8(Sungero.RecordManagement.Server.ActionItemExecutionNotificationEndBlockEventArguments e)
    {
      /* Для старых задач по схеме V1 добавить документы из группы "Результаты исполнения" в ведущее задание на исполнение.
       * Для новых задач документы из группы "Результаты исполнения" синхронизируются в блоке 100.
       */
      if (_schemeVersion < SchemeVersions.V2)
        Functions.ActionItemExecutionTask.SynchronizeResultGroup(_obj);
    }
    
    #endregion

    #region - 9 - Мониторинг создания задания исполнителю или контролёру

    public virtual bool Monitoring9Result()
    {
      return Functions.ActionItemExecutionTask.AssignmentsCreated(_obj);
    }

    public virtual void StartBlock9(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      e.Block.Period = TimeSpan.FromSeconds(10);
    }

    #endregion
    
    #region - 10 - Подзадачи на исполнение
    
    public virtual void Script10Execute()
    {
      Functions.ActionItemExecutionTask.CreateActionItemExecutionTask(_obj);
    }

    #endregion
    
    #region - 11 - Мониторинг завершения поручений
    
    public virtual bool Monitoring11Result()
    {
      if (_obj.IsCompoundActionItem != true)
        return true;
      
      return Functions.ActionItemExecutionTask.AllActionItemPartsAreCompleted(_obj);
    }

    public virtual void StartBlock11(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      e.Block.Period = TimeSpan.FromHours(Constants.ActionItemExecutionTask.CheckCompletionMonitoringPeriodInHours);
    }
    
    #endregion
    
    #region - 12 - Уведомление о старте поручения
    
    public virtual void StartBlock12(Sungero.RecordManagement.Server.ActionItemObserversNotificationArguments e)
    {
      if (_obj.IsDraftResolution == true)
      {
        // Скопировать собственные права поручения в MainTask, чтобы корректно определялись права на задания.
        foreach (var accessRight in _obj.AccessRights.Current)
          if (!_obj.MainTask.AccessRights.IsGranted(accessRight.AccessRightsType, accessRight.Recipient))
            _obj.MainTask.AccessRights.Grant(accessRight.Recipient, accessRight.AccessRightsType);
        
        _obj.IsDraftResolution = null;
      }
      
      if (_obj.IsCompoundActionItem == true)
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreatedCompound);
      else
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreated);
      
      foreach (var observer in _obj.ActionItemObservers)
        e.Block.Performers.Add(observer.Observer);
      
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var assignees = _obj.ActionItemParts.Select(x => x.Assignee).ToList();
        assignees.AddRange(_obj.CoAssignees.Select(x => x.Assignee).ToList());
        foreach (var assignee in assignees)
          if (!document.AccessRights.CanRead(assignee))
            document.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Read);
      }
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.ActionItemExecutionTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }

    public virtual void StartNotice12(Sungero.RecordManagement.IActionItemObserversNotification notice, Sungero.RecordManagement.Server.ActionItemObserversNotificationArguments e)
    {
      notice.Importance = _obj.Importance;
    }

    public virtual void EndBlock12(Sungero.RecordManagement.Server.ActionItemObserversNotificationEndBlockEventArguments e)
    {
      
    }

    #endregion
    
    #region - 13 - Задания соисполнителям созданы?
    
    public virtual bool Decision13Result()
    {
      return Functions.ActionItemExecutionTask.AreAssignmentsCreated(_obj);
    }
    
    #endregion
    
    #region - 99 - Finish
    
    public virtual void StartReviewAssignment99(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion
  }
}
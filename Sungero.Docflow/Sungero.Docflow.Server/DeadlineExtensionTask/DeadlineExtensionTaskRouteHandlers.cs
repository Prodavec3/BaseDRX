using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class DeadlineExtensionTaskRouteHandlers
  {

    #region -  3 - Решение на продление срока
    
    public virtual void StartBlock3(Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.RequestExtensionDeadline);
      e.Block.Performers.Add(_obj.Assignee);
      e.Block.ScheduledDate = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      e.Block.RelativeDeadlineDays = 1;
    }

    public virtual void StartAssignment3(Sungero.Docflow.IDeadlineExtensionAssignment assignment, Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      _obj.MaxDeadline = assignment.Deadline;
      
      // "От".
      assignment.Author = _obj.Author;

      // Выдать права на изменение для возможности прекращения подзадач.
      if (RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment) != null)
        Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.GrantAccessRightToAssignment(assignment, _obj);

    }

    public virtual void CompleteAssignment3(Sungero.Docflow.IDeadlineExtensionAssignment assignment, Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      _obj.NewDeadline = assignment.NewDeadline;
    }

    public virtual void EndBlock3(Sungero.Docflow.Server.DeadlineExtensionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region -  4 - Принятие результата запроса продления срока
    
    public virtual void StartBlock4(Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.ExtensionDeadlineDenied);
      e.Block.Performers.Add(_obj.Author);
      e.Block.CurrentDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      e.Block.RelativeDeadlineDays = 1;
    }

    public virtual void StartAssignment4(Sungero.Docflow.IDeadlineRejectionAssignment assignment, Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      _obj.MaxDeadline = assignment.Deadline;
      
      // "От".
      assignment.Author = _obj.Assignee;
    }

    public virtual void CompleteAssignment4(Sungero.Docflow.IDeadlineRejectionAssignment assignment, Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      // Сохранить срок.
      _obj.NewDeadline = assignment.NewDeadline;
    }

    public virtual void EndBlock4(Sungero.Docflow.Server.DeadlineRejectionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region -  5 - Продление срока (сценарий)
    
    public virtual void Script5Execute()
    {
      // Если родительское задание прекращено, то срок не продлять.
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      var desiredDeadline = _obj.NewDeadline;
      
      if (RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment) != null)
      {
        // Обновить срок у задания.
        var actionItemAssignment = RecordManagement.ActionItemExecutionAssignments.Get(_obj.ParentAssignment.Id);
        actionItemAssignment.Deadline = desiredDeadline;
        actionItemAssignment.ScheduledDate = desiredDeadline;
        
        // Обновить срок у задачи.
        var actionItemExecution = RecordManagement.ActionItemExecutionTasks.Get(actionItemAssignment.Task.Id);
        RecordManagement.PublicFunctions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(actionItemExecution, true, true);
        
        var newCoAssigneesDeadline = Functions.Module.GetNewCoAssigneeDeadline(actionItemExecution.Deadline, actionItemExecution.CoAssigneesDeadline,
                                                                               desiredDeadline, actionItemExecution.Assignee);
        
        if (actionItemExecution.CoAssignees.Any())
        {
          actionItemExecution.CoAssigneesDeadline = newCoAssigneesDeadline;
        }
        actionItemExecution.Deadline = desiredDeadline;
        actionItemExecution.MaxDeadline = desiredDeadline;
        
        // Обновить срок у составной задачи.
        if (actionItemExecution.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
        {
          var component = RecordManagement.ActionItemExecutionTasks.Get(actionItemExecution.ParentTask.Id);
          var actionItem = component.ActionItemParts.FirstOrDefault(j => Equals(j.ActionItemPartExecutionTask, actionItemExecution));
          if (actionItem != null)
          {
            RecordManagement.PublicFunctions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(component, true, true);
            actionItem.CoAssigneesDeadline = newCoAssigneesDeadline;
            actionItem.Deadline = desiredDeadline;
          }
        }
        
        // Продлить сроки соисполнителей.
        foreach (var performer in actionItemExecution.CoAssignees)
        {
          var subTasks = RecordManagement.ActionItemExecutionTasks.GetAll()
            .Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess &&
                   Equals(t.Assignee, performer.Assignee));
          
          if (actionItemExecution.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
            subTasks = subTasks.Where(t => t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional &&
                                      Equals(t.ParentAssignment.Task, actionItemExecution));
          else
            subTasks = subTasks.Where(t => Equals(t.MainTask, actionItemExecution));
          
          foreach (var subTask in subTasks)
          {
            RecordManagement.PublicFunctions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(subTask, true, true);
            
            subTask.Deadline = newCoAssigneesDeadline;
            subTask.MaxDeadline = newCoAssigneesDeadline;
            
            // Продлить срок у активного задания соисполнителя.
            var assignment = RecordManagement.ActionItemExecutionAssignments.GetAll()
              .FirstOrDefault(a => Equals(a.Task, subTask) && a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
            
            if (assignment != null)
            {
              assignment.Deadline = newCoAssigneesDeadline;
              assignment.ScheduledDate = newCoAssigneesDeadline;
            }
          }
        }
      }
      else if (RecordManagement.ReportRequestAssignments.Is(_obj.ParentAssignment))
      {
        // Обновить срок у задания.
        _obj.ParentAssignment.Deadline = desiredDeadline;
        // Обновить срок у задачи.
        var reportRequestTask = RecordManagement.StatusReportRequestTasks.Get(_obj.ParentAssignment.Task.Id);
        reportRequestTask.MaxDeadline = desiredDeadline;
      }
      else if (FreeApprovalAssignments.Is(_obj.ParentAssignment))
      {
        // Обновить срок у задания.
        var freeApprovalAssignment = FreeApprovalAssignments.Get(_obj.ParentAssignment.Id);
        freeApprovalAssignment.Deadline = desiredDeadline;
        
        // Обновить срок у задачи.
        var freeApprovalTask = FreeApprovalTasks.Get(_obj.ParentAssignment.Task.Id);
        if (Functions.Module.CheckDeadline(desiredDeadline, freeApprovalTask.MaxDeadline))
          freeApprovalTask.MaxDeadline = desiredDeadline;
      }
      else if (ApprovalCheckReturnAssignments.Is(_obj.ParentAssignment))
      {
        // Обновить срок у задания.
        var checkReturnAssignment = ApprovalCheckReturnAssignments.As(_obj.ParentAssignment);
        checkReturnAssignment.Deadline = desiredDeadline;
        
        // Обновить срок на вкладке "Выдача" документа.
        if (checkReturnAssignment.DocumentGroup.OfficialDocuments.Any())
        {
          var document = checkReturnAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault();
          ((Domain.Shared.IExtendedEntity)document).Params[Docflow.Constants.Module.DeadlineExtentsionTaskCallContext] = true;
          var tracks = document.Tracking.Where(t => Equals(t.ReturnTask, checkReturnAssignment.Task) &&
                                               t.ReturnResult == null && t.ReturnDeadline != null);
          foreach (var track in tracks)
            track.ReturnDeadline = desiredDeadline;
        }
      }
      else if (CheckReturnAssignments.Is(_obj.ParentAssignment))
      {
        // Обновить срок у задания.
        var checkReturnAssignment = CheckReturnAssignments.As(_obj.ParentAssignment);
        checkReturnAssignment.Deadline = desiredDeadline;
        if (CheckReturnTasks.Is(checkReturnAssignment.Task))
        {
          var checkReturnTask = CheckReturnTasks.As(checkReturnAssignment.Task);
          checkReturnTask.MaxDeadline = desiredDeadline;
        }
        // Обновить срок на вкладке "Выдача" документа.
        if (checkReturnAssignment.DocumentGroup.OfficialDocuments.Any())
        {
          var document = checkReturnAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault();
          ((Domain.Shared.IExtendedEntity)document).Params[Docflow.Constants.Module.DeadlineExtentsionTaskCallContext] = true;
          var tracks = document.Tracking.Where(t => Equals(t.ReturnTask, checkReturnAssignment.Task) &&
                                               t.ReturnResult == null && t.ReturnDeadline != null);
          foreach (var track in tracks)
            track.ReturnDeadline = desiredDeadline;
        }
      }
      else
      {
        // Обновить срок у задания.
        _obj.ParentAssignment.Deadline = desiredDeadline;
      }
      
      // Обновить срок у задач.
      var parentAssignment = _obj.ParentAssignment;
      if (ApprovalTasks.Is(parentAssignment.MainTask))
      {
        var approvalTask = ApprovalTasks.As(parentAssignment.MainTask);
        approvalTask.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(approvalTask);
      }
      else if (RecordManagement.AcquaintanceTasks.Is(parentAssignment.MainTask))
      {
        var acquaintanceTask = RecordManagement.AcquaintanceTasks.As(parentAssignment.MainTask);
        if (Functions.Module.CheckDeadline(desiredDeadline, acquaintanceTask.Deadline))
          acquaintanceTask.Deadline = desiredDeadline;
      }
    }
    
    #endregion

    #region -  6 - Уведомление о продлении срока
    
    public virtual void StartBlock6(Sungero.Docflow.Server.DeadlineExtensionNotificationArguments e)
    {
      if (_obj.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
        return;
      
      var desiredDeadline = _obj.NewDeadline.Value;
      var desiredDeadlineLabel = Functions.DeadlineExtensionTask.GetDesiredDeadlineLabel(desiredDeadline);
      var subjectFormat = DeadlineExtensionTasks.Resources.ExtensionDeadlineFormat(desiredDeadlineLabel);
      var subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, subjectFormat);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      e.Block.Performers.Add(_obj.Author);
      
      e.Block.PreviousDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = desiredDeadline;
      
      if (RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment) != null)
      {
        // Отправить уведомления соисполнителям.
        var actionItemAssignment = RecordManagement.ActionItemExecutionAssignments.Get(_obj.ParentAssignment.Id);
        var actionItemExecution = RecordManagement.ActionItemExecutionTasks.Get(actionItemAssignment.Task.Id);
        if (actionItemExecution.CoAssignees.Count > 0)
        {
          foreach (var performer in actionItemExecution.CoAssignees)
          {
            e.Block.Performers.Add(performer.Assignee);
          }
        }
      }
    }

    public virtual void StartNotice6(Sungero.Docflow.IDeadlineExtensionNotification notice, Sungero.Docflow.Server.DeadlineExtensionNotificationArguments e)
    {
      // "От".
      notice.Author = _obj.Assignee;
      
      if (RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment) == null || Equals(notice.Performer, _obj.Author))
        return;
      
      var actionItemExecutionTask = RecordManagement.ActionItemExecutionTasks.As(_obj.ParentAssignment.Task);
      var desiredDeadline = actionItemExecutionTask.CoAssigneesDeadline == null ? _obj.NewDeadline.Value : actionItemExecutionTask.CoAssigneesDeadline.Value;
      var desiredDeadlineLabel = Functions.DeadlineExtensionTask.GetDesiredDeadlineLabel(desiredDeadline);
      var subjectFormat = DeadlineExtensionTasks.Resources.ExtensionCoAssigneeDeadlineFormat(desiredDeadlineLabel);
      var subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, subjectFormat);
      notice.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      notice.NewDeadline = desiredDeadline;
      notice.PreviousDeadline = null;
    }

    public virtual void EndBlock6(Sungero.Docflow.Server.DeadlineExtensionNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DeadlineExtensionTaskRouteHandlers
  {

    #region -  2 - Решение на продление срока

    public virtual void StartBlock2(Sungero.RecordManagement.Server.DeadlineExtensionAssignmentArguments e)
    {
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.RequestExtensionDeadline);
      if (_obj.MaxDeadline.HasValue)
        e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
      
      e.Block.Performers.Add(_obj.Assignee);
      e.Block.ScheduledDate = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public virtual void StartAssignment2(Sungero.RecordManagement.IDeadlineExtensionAssignment assignment, Sungero.RecordManagement.Server.DeadlineExtensionAssignmentArguments e)
    {
      if (string.IsNullOrEmpty(_obj.Reason))
        assignment.Reason = _obj.ActiveText;
      else
        assignment.Reason = _obj.Reason;
      
      // "От".
      assignment.Author = _obj.Author;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }

    public virtual void CompleteAssignment2(Sungero.RecordManagement.IDeadlineExtensionAssignment assignment, Sungero.RecordManagement.Server.DeadlineExtensionAssignmentArguments e)
    {
      _obj.NewDeadline = assignment.NewDeadline;
      _obj.RejectionReason = assignment.ActiveText;
    }

    public virtual void EndBlock2(Sungero.RecordManagement.Server.DeadlineExtensionAssignmentEndBlockEventArguments e)
    {
      
    }

    #endregion
    
    #region -  3 - Принятие результата запроса продления срока
    
    public virtual void StartBlock3(Sungero.RecordManagement.Server.DeadlineRejectionAssignmentArguments e)
    {
      e.Block.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, DeadlineExtensionTasks.Resources.ExtensionDeadlineDenied);
      e.Block.Performers.Add(_obj.Author);
      
      e.Block.CurrentDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = _obj.NewDeadline;
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public virtual void StartAssignment3(Sungero.RecordManagement.IDeadlineRejectionAssignment assignment, Sungero.RecordManagement.Server.DeadlineRejectionAssignmentArguments e)
    {
      var assignmentsDeadLine = 1;
      _obj.MaxDeadline = Calendar.Now.AddWorkingDays(assignmentsDeadLine);
      assignment.Deadline = _obj.MaxDeadline;
      
      // "От".
      assignment.Author = _obj.Assignee;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }

    public virtual void CompleteAssignment3(Sungero.RecordManagement.IDeadlineRejectionAssignment assignment, Sungero.RecordManagement.Server.DeadlineRejectionAssignmentArguments e)
    {
      // Сохранить переписку и срок.
      _obj.Reason = assignment.ActiveText;
      _obj.NewDeadline = assignment.NewDeadline;
    }

    public virtual void EndBlock3(Sungero.RecordManagement.Server.DeadlineRejectionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region -  4 - Продление срока (сценарий)
    
    public virtual void Script4Execute()
    {
      var desiredDeadline = _obj.NewDeadline;
      
      // Обновить срок у задания.
      var actionItemAssignment = ActionItemExecutionAssignments.Get(_obj.ParentAssignment.Id);
      actionItemAssignment.Deadline = desiredDeadline;
      actionItemAssignment.ScheduledDate = desiredDeadline;
      actionItemAssignment.Save();
      
      // Обновить срок у задачи.
      var actionItemExecution = ActionItemExecutionTasks.Get(actionItemAssignment.Task.Id);
      actionItemExecution.Deadline = desiredDeadline;
      actionItemExecution.MaxDeadline = desiredDeadline;
      
      // Обновить срок у составной задачи.
      if (actionItemExecution.ActionItemType == ActionItemType.Component)
      {
        var component = ActionItemExecutionTasks.Get(actionItemExecution.ParentTask.Id);
        var actionItem = component.ActionItemParts.FirstOrDefault(j => Equals(j.Assignee, actionItemExecution.Assignee) &&
                                                                  Equals(j.ActionItemPart, actionItemExecution.ActionItem) &&
                                                                  j.Deadline == _obj.CurrentDeadline);
        if (actionItem != null)
          actionItem.Deadline = desiredDeadline;
      }
      
      // Продлить сроки соисполнителей.
      foreach (var performer in actionItemExecution.CoAssignees)
      {
        var subTasks = ActionItemExecutionTasks.GetAll(t => Equals(t.MainTask, actionItemExecution) &&
                                                    t.Status == Sungero.Workflow.Task.Status.InProcess &&
                                                    Equals(t.Assignee, performer.Assignee));
        foreach (var subTask in subTasks)
        {
          subTask.Deadline = desiredDeadline;
          subTask.MaxDeadline = desiredDeadline;
          
          // Продлить срок у активного задания соисполнителя.
          var assignment = ActionItemExecutionAssignments.GetAll()
            .FirstOrDefault(a => Equals(a.Task, subTask) && a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
          
          if (assignment != null)
          {
            assignment.Deadline = desiredDeadline;
            assignment.ScheduledDate = desiredDeadline;
          }
        }
      }
    }
    
    #endregion
    
    #region -  5 - Уведомление о продлении срока

    public virtual void StartBlock5(Sungero.RecordManagement.Server.DeadlineExtensionNotificationArguments e)
    {
      var desiredDeadline = _obj.NewDeadline.Value;
      var desiredDeadlineLabel = Functions.DeadlineExtensionTask.GetDesiredDeadlineLabel(desiredDeadline);
      var subjectFormat = DeadlineExtensionTasks.Resources.ExtensionDeadlineFormat(desiredDeadlineLabel);
      var subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(_obj, subjectFormat);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      e.Block.Performers.Add(_obj.Author);
      
      e.Block.PreviousDeadline = _obj.CurrentDeadline;
      e.Block.NewDeadline = desiredDeadline;
      
      // Отправить уведомления соисполнителям.
      var actionItemAssignment = ActionItemExecutionAssignments.Get(_obj.ParentAssignment.Id);
      var actionItemExecution = ActionItemExecutionTasks.Get(actionItemAssignment.Task.Id);
      if (actionItemExecution.CoAssignees.Count > 0)
      {
        foreach (var performer in actionItemExecution.CoAssignees)
        {
          e.Block.Performers.Add(performer.Assignee);
        }
      }
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public virtual void StartNotice5(Sungero.RecordManagement.IDeadlineExtensionNotification notice, Sungero.RecordManagement.Server.DeadlineExtensionNotificationArguments e)
    {
      // "От".
      notice.Author = _obj.Assignee;
    }

    public virtual void EndBlock5(Sungero.RecordManagement.Server.DeadlineExtensionNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    public virtual void StartReviewAssignment99(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
  }
}
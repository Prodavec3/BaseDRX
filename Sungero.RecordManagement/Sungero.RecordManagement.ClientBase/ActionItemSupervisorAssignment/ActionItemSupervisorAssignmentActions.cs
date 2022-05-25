using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemSupervisorAssignmentActions
  {

    public virtual void Forwarded(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      _obj.Performer = _obj.ForwardedTo.SingleOrDefault();
      _obj.Save();
    }

    public virtual bool CanForwarded(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual bool CanAgree(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      this.CheckAssignmentAborted(e);
      
      if (!RecordManagement.Functions.ActionItemSupervisorAssignment.ValidateActionItemSupervisorAssignment(_obj, e))
        return;
      
      // Если срок вышел, добавить в диалог дополнительное описание.
      var description = Docflow.PublicFunctions.Module.CheckDeadline(ActionItemExecutionTasks.As(_obj.Task).Assignee, _obj.NewDeadline, Calendar.Now)
        ? null
        : ActionItemSupervisorAssignments.Resources.NewDeadlineLessThenTodayDescription;
      var dialogID = Constants.ActionItemExecutionTask.ActionItemSupervisorAssignmentConfirmDialogID.ForRework;
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, description, null, dialogID))
        e.Cancel();
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Agree(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      this.CheckAssignmentAborted(e);
      
      var parentAssignment = Functions.ActionItemExecutionTask.GetParentAssignment(ActionItemExecutionTasks.As(_obj.Task));
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (parentAssignment != null && parentAssignment.Result == Sungero.RecordManagement.ActionItemExecutionAssignment.Result.Done)
      {
        if (!RecordManagement.Functions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, RecordManagement.ActionItemExecutionTasks.Resources.ParentAssignmentExists,
                                                                      null, parentAssignment))
          e.Cancel();
      }
      else
      {
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                   Constants.ActionItemExecutionTask.ActionItemSupervisorAssignmentConfirmDialogID.Agree))
          e.Cancel();
      }
      
      if (parentAssignment != null &&
          parentAssignment.AccessRights.CanUpdate() &&
          parentAssignment.Result != Sungero.RecordManagement.ActionItemExecutionAssignment.Result.Done)
      {
        if (!Functions.Module.ShowCompleteParentActionItemConfirmationDialog(_obj, parentAssignment))
          e.Cancel();
      }
    }

    public virtual bool CanAgree(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CheckAssignmentAborted(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var actionItemTask = ActionItemExecutionTasks.As(_obj.Task);
      if (actionItemTask.Status == Status.Aborted)
      {
        Dialogs.ShowMessage(ActionItemSupervisorAssignments.Resources.AbortAssignmentWhenTaskWasAborted, MessageType.Error);
        e.Cancel();
      }
      if (!Equals(actionItemTask.Supervisor, _obj.Performer) && _obj.ForwardedTo != null)
      {
        Dialogs.ShowMessage(ActionItemSupervisorAssignments.Resources.AbortAssignmentWhenSupervisorChanged, MessageType.Error);
        e.Cancel();
      }
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalReworkAssignment;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalReworkAssignmentActions
  {
    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var newDeadline = Functions.ApprovalTask.GetNewDeadline(_obj.Deadline);
      
      if (newDeadline != null)
      {
        _obj.Deadline = newDeadline.Value;
        _obj.Save();
        Dialogs.NotifyMessage(Docflow.Resources.CurrentAssignmentNewDeadline);
      }
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate() &&
        Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

    public virtual void AbortAction(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var assignees = _obj.Approvers.Where(a => a.Approver != null).Select(a => Recipients.As(a.Approver)).ToList();
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              assignees,
                                                                              e.Action,
                                                                              Constants.FreeApprovalTask.ReworkAssignmentConfirmDialogID.AbortAction))
        return;
      
      _obj.Task.Abort();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanAbortAction(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess;
    }

    public virtual void Reworked(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var assignees = _obj.Approvers.Where(a => a.Approver != null).Select(a => Recipients.As(a.Approver)).ToList();
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              assignees,
                                                                              e.Action,
                                                                              Constants.FreeApprovalTask.ReworkAssignmentConfirmDialogID.Reworked))
        e.Cancel();
    }

    public virtual bool CanReworked(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

  }

}
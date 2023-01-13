using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalFinishAssignment;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalFinishAssignmentActions
  {

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                              Constants.FreeApprovalTask.FinishConfirmDialogID))
        e.Cancel();
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
    }

  }

}
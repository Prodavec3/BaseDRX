using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineRejectionAssignment;

namespace Sungero.Docflow.Client
{
  partial class DeadlineRejectionAssignmentActions
  {
    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DeadlineRejectionAssignment.ValidateDeadlineRejectionAssignment(_obj, e))
        return;
      
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                Constants.DeadlineExtensionTask.DeadlineRejectionAssignmentConfirmDialogID))
        e.Cancel();
      
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Accept(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanAccept(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}
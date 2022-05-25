using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionAssignment;

namespace Sungero.Docflow.Client
{
  partial class DeadlineExtensionAssignmentActions
  {

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность причины отказа.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(DeadlineExtensionAssignments.Resources.RefusalReasonNotFilled);
        return;
      }
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.DeadlineExtensionTask.DeadlineExtensionAssignmentConfirmDialogID.ForRework))
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
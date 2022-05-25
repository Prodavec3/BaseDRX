using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow.Client
{
  partial class DeadlineExtensionTaskActions
  {
    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Sungero.Docflow.Functions.DeadlineExtensionTask.ValidateDeadlineExtensionTaskStart(_obj, e))
        return;
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.DeadlineExtensionTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }

}
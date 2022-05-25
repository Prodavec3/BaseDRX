using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalTaskActions
  {
    public override void Restart(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Restart(e);
    }

    public override bool CanRestart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRestart(e);
    }

    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SaveAndClose(e);
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Sungero.Docflow.Functions.FreeApprovalTask.ValidateFreeApprovalTaskStart(_obj, e))
        return;
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              e.Action,
                                                                              Constants.FreeApprovalTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }

}
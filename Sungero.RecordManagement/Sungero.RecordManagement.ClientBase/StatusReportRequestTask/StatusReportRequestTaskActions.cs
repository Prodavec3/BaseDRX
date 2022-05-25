using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement.Client
{
  partial class StatusReportRequestTaskActions
  {

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e))
        return;
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.StatusReportRequestTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }
}
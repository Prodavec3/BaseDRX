using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestCheckAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReportRequestCheckAssignmentActions
  {
    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(StatusReportRequestTasks.Resources.ReportCommentNotFilled);
        return;
      }
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.StatusReportRequestTask.ReportRequestCheckAssignmentConfirmDialogID.ForRework))
        e.Cancel();
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Accept(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.StatusReportRequestTask.ReportRequestCheckAssignmentConfirmDialogID.Accept))
        e.Cancel();
    }

    public virtual bool CanAccept(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }
}
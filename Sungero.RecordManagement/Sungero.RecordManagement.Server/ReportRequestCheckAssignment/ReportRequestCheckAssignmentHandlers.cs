using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestCheckAssignment;

namespace Sungero.RecordManagement
{
  partial class ReportRequestCheckAssignmentServerHandlers
  {
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result.Value == Result.ForRework)
        e.Result = ReportRequestCheckAssignments.Resources.ForRework;
      else
        e.Result = ReportRequestCheckAssignments.Resources.ReportAccepted;
    }
  }
}
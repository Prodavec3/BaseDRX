using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestAssignment;

namespace Sungero.RecordManagement
{
  partial class ReportRequestAssignmentServerHandlers
  {
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      e.Result = ReportRequestAssignments.Resources.Completed;
    }
  }
}
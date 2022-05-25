using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalReworkAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalReworkAssignmentServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      if (_obj.State.Properties.Deadline.IsChanged)
      {
        e.Operation = new Enumeration(Constants.FreeApprovalReworkAssignment.Operation.DeadlineExtend);
      }
    }

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (!Functions.Module.CheckDeadline(_obj.NewDeadline, Calendar.Now))
        e.AddError(_obj.Info.Properties.NewDeadline, FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday, new[] { _obj.Info.Properties.NewDeadline });
      else if (_obj.Result == Result.Reworked)
        e.Result = Docflow.FreeApprovalTasks.Resources.ForReapproving;
      else
        e.Result = Docflow.FreeApprovalTasks.Resources.AbortApproving;
    }
  }

}
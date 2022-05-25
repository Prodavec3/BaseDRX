using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionAssignment;

namespace Sungero.RecordManagement
{
  partial class ActionItemExecutionAssignmentServerHandlers
  {
    
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Добавить автотекст.
      if (_obj.Result == Result.Done)
        e.Result = ActionItemExecutionAssignments.Resources.JobExecuted;
      else if (_obj.Result == Result.Forwarded)
        e.Result = ActionItemExecutionAssignments.Resources.Forwarded;
    }
  }
}
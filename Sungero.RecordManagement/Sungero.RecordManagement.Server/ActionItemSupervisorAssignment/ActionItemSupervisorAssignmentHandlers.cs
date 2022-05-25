using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement
{
  partial class ActionItemSupervisorAssignmentServerHandlers
  {
    
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (!RecordManagement.Functions.ActionItemSupervisorAssignment.ValidateActionItemSupervisorAssignment(_obj, e))
        return;
      
      // Добавить автотекст.
      if (_obj.Result == Result.Agree)
        e.Result = ActionItemSupervisorAssignments.Resources.JobAccepted;
      else if (_obj.Result == Result.Forwarded)
        e.Result = ActionItemSupervisorAssignments.Resources.Forwarded;
      else
        e.Result = ActionItemSupervisorAssignments.Resources.SendForRework;
    }
  }
}
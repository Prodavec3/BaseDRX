using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineRejectionAssignment;

namespace Sungero.Docflow
{
  partial class DeadlineRejectionAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Task.ParentAssignment.Status != Workflow.AssignmentBase.Status.InProcess)
      {
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.Complete;
        return;
      }
      
      if (_obj.Result.Value == Result.ForRework)
      {
        if (!Functions.DeadlineRejectionAssignment.ValidateDeadlineRejectionAssignment(_obj, e))
          return;
        
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.RequestedRepeatedly;
      }
      else
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.RequestedAccepted;
    }
  }

}
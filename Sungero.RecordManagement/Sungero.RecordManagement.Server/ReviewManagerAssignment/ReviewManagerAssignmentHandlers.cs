using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewManagerAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewManagerAssignmentServerHandlers
  {
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Добавить автотекст.
      if (_obj.Result.Value == Result.AddResolution)
        e.Result = ReviewManagerAssignments.Resources.ResolutionAdded;
      if (_obj.Result.Value == Result.Explored)
        e.Result = ReviewManagerAssignments.Resources.Explored;
      if (_obj.Result.Value == Result.AddAssignment)
        e.Result = ReviewManagerAssignments.Resources.AssignmentCreated;
      if (_obj.Result.Value == Result.Forward)
        e.Result = DocumentReviewTasks.Resources.ForwardFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      if (_obj.Result.Value == Result.ForRework)
        e.Result = ReviewDraftResolutionAssignments.Resources.ReworkResolution;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewDraftResolutionAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Добавить автотекст.
      if (_obj.Result.Value == Result.AddResolution)
        e.Result = ReviewDraftResolutionAssignments.Resources.ReworkResolution;
      if (_obj.Result.Value == Result.Informed)
        e.Result = ReviewManagerAssignments.Resources.Explored;
      if (_obj.Result.Value == Result.ForExecution)
        e.Result = ReviewManagerAssignments.Resources.AssignmentCreated;
      if (_obj.Result.Value == Result.Forward)
        e.Result = DocumentReviewTasks.Resources.ForwardFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
    }
  }

}
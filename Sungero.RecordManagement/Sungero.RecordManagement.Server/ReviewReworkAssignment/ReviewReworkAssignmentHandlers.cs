using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewReworkAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewReworkAssignmentServerHandlers
  {
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Добавить автотекст.
      if (_obj.Result.Value == Result.Forward)
        e.Result = DocumentReviewTasks.Resources.ForwardFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
    }
  }

}
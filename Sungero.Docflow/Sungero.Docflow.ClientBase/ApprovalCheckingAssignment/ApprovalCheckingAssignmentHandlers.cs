using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalCheckingAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalCheckingAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
            
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);     
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer;
    }

  }
}
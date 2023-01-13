using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSendingAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalSendingAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      if ((!schemeVersionSupportsRework) || (schemeVersionSupportsRework && !reworkParameters.AllowSendToRework))
        e.HideAction(_obj.Info.Actions.ForRevision);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);

      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);           
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer && schemeVersionSupportsRework;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer && schemeVersionSupportsRework;
    }

  }
}
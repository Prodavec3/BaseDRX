using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      _obj.State.Properties.Addressee.IsVisible = _obj.Task.GetStartedSchemeVersion() != LayerSchemeVersions.V1;

      // Скрывать контрол состояния со сводкой, если сводка пустая.
      var needViewDocumentSummary = Functions.ApprovalAssignment.NeedViewDocumentSummary(_obj);
      _obj.State.Controls.DocumentSummary.IsVisible = needViewDocumentSummary;
            
      var reworkParameters = Functions.ApprovalTask.Remote.GetReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);     
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer;
    }
    
    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.Task.GetStartedSchemeVersion() == LayerSchemeVersions.V1)
      {
        e.HideAction(_obj.Info.Actions.Forward);
        e.HideAction(_obj.Info.Actions.AddApprover);
      }
    }
  }

}
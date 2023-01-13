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
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      // Показывать поле "Переадресовать сотруднику", если:
      //  - позволяет схема;
      //  - исполнитель задания видит документ.
      _obj.State.Properties.Addressee.IsVisible = 
        _obj.Task.GetStartedSchemeVersion() != LayerSchemeVersions.V1 &&
        Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));

      // Скрывать контрол состояния со сводкой, если сводка пустая.
      var needViewDocumentSummary = Functions.ApprovalAssignment.NeedViewDocumentSummary(_obj);
      _obj.State.Controls.DocumentSummary.IsVisible = needViewDocumentSummary;
            
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);     
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
      
      // Скрывать результат выполнения "Согласовать с замечаниями" для стартованных на ранних версиях схемы задач и в случаях когда он отключен в настройках этапа.
      var schemeSupportsApproveWithSuggestions = Functions.ApprovalTask.SchemeVersionSupportsApproveWithSuggestions(ApprovalTasks.As(_obj.Task));
      var stageAllowsApproveWithSuggestions = Functions.ApprovalTask.Remote.GetApprovalWithSuggestionsParameter(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      if (!schemeSupportsApproveWithSuggestions || !stageAllowsApproveWithSuggestions)
        e.HideAction(_obj.Info.Actions.WithSuggestions);
    }
  }

}
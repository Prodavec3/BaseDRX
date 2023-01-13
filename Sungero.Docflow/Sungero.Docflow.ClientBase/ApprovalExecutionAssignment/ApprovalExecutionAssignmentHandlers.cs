using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalExecutionAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalExecutionAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);

      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);     
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer && schemeVersionSupportsRework;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer && schemeVersionSupportsRework;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var collapsedStageTypes = _obj.CollapsedStagesTypesExe.Select(s => s.StageType).ToList();
      
      // Скрыть действия по отправке документа контрагенту через сервис обмена и отправке вложением в письмо.
      if (collapsedStageTypes.All(x => x != Docflow.ApprovalStage.StageType.Sending))
      {
        e.HideAction(_obj.Info.Actions.SendViaExchangeService);
        e.HideAction(_obj.Info.Actions.SendByMail);
      }
      
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      if ((!schemeVersionSupportsRework) || (schemeVersionSupportsRework && !reworkParameters.AllowSendToRework))
        e.HideAction(_obj.Info.Actions.ForRevision);
    }

  }
}
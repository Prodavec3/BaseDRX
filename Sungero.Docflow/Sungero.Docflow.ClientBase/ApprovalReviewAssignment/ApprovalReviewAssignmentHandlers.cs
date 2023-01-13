using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalReviewAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      // Если схлопнуто с печатью, то отобразить адресата.
      _obj.State.Properties.Addressee.IsVisible = _obj.Addressee != null;
      
      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer;      
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Скрыть вынесение резолюции, если нет этапа создания поручений в правиле согласования.
      if (_obj.NeedHideAddResolutionAction == true)
        e.HideAction(_obj.Info.Actions.AddResolution);
      
      var collapsedStageTypes = _obj.CollapsedStagesTypesRe.Select(s => s.StageType).ToList();
      
      // Скрыть действия по отправке документа контрагенту через сервис обмена и отправке вложением в письмо.
      if (collapsedStageTypes.All(x => x != Docflow.ApprovalStage.StageType.Sending))
      {
        e.HideAction(_obj.Info.Actions.SendViaExchangeService);
        e.HideAction(_obj.Info.Actions.SendByMail);
      }
    }
  }
}
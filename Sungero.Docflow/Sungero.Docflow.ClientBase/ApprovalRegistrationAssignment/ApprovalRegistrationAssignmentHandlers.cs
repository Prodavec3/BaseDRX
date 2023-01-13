using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRegistrationAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalRegistrationAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);

      // Если схлопнуто с печатью, то отобразить кому нести на подпись, рассмотрение.
      _obj.State.Properties.Signatory.IsVisible = _obj.Signatory != null;
      _obj.State.Properties.Addressee.IsVisible = _obj.Addressee != null;
      
      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);           
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer && schemeVersionSupportsRework;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer && schemeVersionSupportsRework;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Скрыть действие по созданию сопроводительного письма.
      var collapsedStageTypes = _obj.CollapsedStagesTypesReg.Select(s => s.StageType).ToList();
      if (Functions.ApprovalTask.NeedHideCoverLetterAction(ApprovalTasks.As(_obj.Task), collapsedStageTypes))
        e.HideAction(_obj.Info.Actions.CreateCoverLetter);
      
      // Скрыть действия по отправке документа контрагенту через сервис обмена и отправке вложением в письмо.
      if (collapsedStageTypes.All(x => x != Docflow.ApprovalStage.StageType.Sending))
      {
        e.HideAction(_obj.Info.Actions.SendViaExchangeService);
        e.HideAction(_obj.Info.Actions.SendByMail);
      }
      
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      if ((!schemeVersionSupportsRework) || (schemeVersionSupportsRework && !reworkParameters.AllowSendToRework))
        e.HideAction(_obj.Info.Actions.ForRevision);
    }
  }
}
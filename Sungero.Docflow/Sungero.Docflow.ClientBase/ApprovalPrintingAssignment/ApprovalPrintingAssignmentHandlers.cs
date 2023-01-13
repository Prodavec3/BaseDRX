using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalPrintingAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalPrintingAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Скрыть действие по созданию сопроводительного письма.
      var collapsedStageTypes = _obj.CollapsedStagesTypesPr.Select(s => s.StageType).ToList();
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
      
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      Functions.ApprovalTask.GetOrUpdateAssignmentRefreshParams(ApprovalTasks.As(_obj.Task), _obj, true);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var refreshParameters = Functions.ApprovalTask.GetOrUpdateAssignmentRefreshParams(ApprovalTasks.As(_obj.Task), _obj, false);
      
      _obj.State.Properties.Addressee.IsVisible = refreshParameters.AddresseeIsVisible;
      _obj.State.Properties.Addressee.IsRequired = refreshParameters.AddresseeIsRequired;
      _obj.State.Properties.Addressee.IsEnabled = false;
      
      _obj.State.Properties.Signatory.IsVisible = refreshParameters.SignatoryIsVisible;
      _obj.State.Properties.Signatory.IsRequired = refreshParameters.SignatoryIsRequired;
      _obj.State.Properties.Signatory.IsEnabled = false;
      
      _obj.State.Attachments.ForPrinting.IsVisible = _obj.CollapsedStagesTypesPr.Count <= 1;
      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);           
      var schemeVersionSupportsRework = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer && schemeVersionSupportsRework;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer && schemeVersionSupportsRework;
    }

  }
}
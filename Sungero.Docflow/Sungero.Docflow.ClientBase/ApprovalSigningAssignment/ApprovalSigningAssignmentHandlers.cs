using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSigningAssignment;
using Sungero.Domain.Shared;

namespace Sungero.Docflow
{
  partial class ApprovalSigningAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      _obj.State.Properties.DeliveryMethodDescription.IsVisible = !string.IsNullOrEmpty(_obj.DeliveryMethodDescription);
      
      // Скрывать контрол состояния со сводкой, если сводка пустая.
      var needViewDocumentSummary = Functions.ApprovalSigningAssignment.NeedViewDocumentSummary(_obj);
      _obj.State.Controls.DocumentSummary.IsVisible = needViewDocumentSummary;
            
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);           
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer;
    }
    
    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Скрыть действие по созданию сопроводительного письма.
      var collapsedStageTypes = _obj.CollapsedStagesTypesSig.Select(s => s.StageType).ToList();
      if (Functions.ApprovalTask.NeedHideCoverLetterAction(ApprovalTasks.As(_obj.Task), collapsedStageTypes))
        e.HideAction(_obj.Info.Actions.CreateCoverLetter);
      
      // Скрыть действие по созданию поручения и ознакомления.
      if (!collapsedStageTypes.Any(s => s == Docflow.ApprovalStage.StageType.Execution))
      {
        e.HideAction(_obj.Info.Actions.CreateActionItem);
        e.HideAction(_obj.Info.Actions.CreateAcquaintance);
      }
      
      // Скрыть действия по отправке документа контрагенту через сервис обмена и отправке вложением в письмо.
      if (collapsedStageTypes.All(x => x != Docflow.ApprovalStage.StageType.Sending))
      {
        e.HideAction(_obj.Info.Actions.SendViaExchangeService);
        e.HideAction(_obj.Info.Actions.SendByMail);
      }
      
      // Заменить "Подписать" на "Подтвердить подписание", если помощнику приходит задание подтверждения.
      var task = ApprovalTasks.As(_obj.Task);
      var isConfirm = _obj.Stage.IsConfirmSigning == true && !Equals(_obj.Performer, task.Signatory);
      if (isConfirm != true)
      {
        e.HideAction(_obj.Info.Actions.ExtendDeadline);
        e.HideAction(_obj.Info.Actions.ConfirmSign);
      }
      else
        e.HideAction(_obj.Info.Actions.Sign);
    }
  }
}
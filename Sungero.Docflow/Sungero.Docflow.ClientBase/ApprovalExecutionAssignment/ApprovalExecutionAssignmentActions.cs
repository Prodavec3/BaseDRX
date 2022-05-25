using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalExecutionAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalExecutionAssignmentActions
  {
    public virtual void ForRevision(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Валидация заполнения ответственного за доработку.
      if (_obj.ReworkPerformer == null)
      {
        e.AddError(ApprovalTasks.Resources.CantSendForReworkWithoutPerformer);
        e.Cancel();
      }
      
      // Проверить заполненность активного текста.
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalTasks.Resources.NeedTextForRework, e))
        e.Cancel();
      
      // Вызов диалога запроса выдачи прав на вложения (при отсутствии прав).
      Functions.ApprovalTask.ShowReworkConfirmationDialog(ApprovalTasks.As(_obj.Task), _obj, _obj.OtherGroup.All.ToList(), new List<IRecipient>(), _obj.ReworkPerformer, e,
                                                          Constants.ApprovalTask.ApprovalExecutionAssignmentConfirmDialogID.ForRevision);
    }

    public virtual bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalSendingAssignment.SendByMail(ApprovalTasks.As(_obj.Task));
    }

    public virtual bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any(d => d.HasVersions);
    }

    public virtual void CreateAcquaintance(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      var subTask = RecordManagement.PublicFunctions.Module.Remote.CreateAcquaintanceTaskAsSubTask(document, _obj);
      if (subTask != null)
        subTask.ShowModal();
    }

    public virtual bool CanCreateAcquaintance(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void SendViaExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      Functions.ApprovalSendingAssignment.SendToCounterparty(document, _obj.Task);
    }

    public virtual bool CanSendViaExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return _obj.DocumentGroup.OfficialDocuments.Any() && Functions.ApprovalSendingAssignment.CanSendToCounterparty(document);
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      var parentAssignmentId = _obj.Id;
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var documentApprovalTask = ApprovalTasks.As(_obj.Task);
      var stages = Functions.ApprovalRuleBase.Remote.GetStages(documentApprovalTask.ApprovalRule, document, documentApprovalTask).Stages;
      var assignedBy = Sungero.Company.Employees.Null;
      
      // Автором резолюции вычислить адресата, либо подписывающего.
      if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Review))
        assignedBy = PublicFunctions.Module.Remote.GetResolutionAuthor(documentApprovalTask);
      else if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Sign))
        assignedBy = documentApprovalTask.Signatory;
      
      var hackTask = Functions.Module.CreateActionItemExecutionWithResolution(document, parentAssignmentId, _obj.ResolutionText, assignedBy);
      if (hackTask != null)
      {
        RecordManagement.ActionItemExecutionTasks.As(hackTask).AssignedBy = assignedBy;
        hackTask.ShowModal();
      }
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesExe.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          return;
        }
      }
      
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), e);
      var sendDialog = Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesExe.Select(x => x.StageType), e);
      
      if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), (confirmationAccepted || sendDialog) ? null : e.Action,
                                                                                           Constants.ApprovalTask.ApprovalExecutionAssignmentConfirmDialogID.Complete) == false)
        e.Cancel();
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

  }

}
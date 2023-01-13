using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;
using Sungero.Workflow;

namespace Sungero.Docflow.Client
{
  partial class ApprovalReworkAssignmentActions
  {
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      if (_obj.ForwardPerformer == null)
      {
        e.AddError(ApprovalReworkAssignments.Resources.CantRedirectWithoutForwardPerformer);
        e.Cancel();
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var stages = Functions.ApprovalTask.Remote.GetStages(ApprovalTasks.As(_obj.Task));
      var hasSignStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Docflow.ApprovalStage.StageType.Sign, document, stages.Stages);
      
      // Проверить, имеет ли право подписывающий на подпись.
      if (hasSignStage &&
          !Functions.ApprovalTask.Remote.CheckSignatory(ApprovalTasks.As(_obj.Task), _obj.Signatory, stages.Stages))
      {
        e.AddError(ApprovalTasks.Resources.IncorrectSignatory);
        return;
      }
      
      var giveRights = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), new List<IRecipient>() { _obj.ForwardPerformer });
      // Замена стандартного диалога подтверждения выполнения действия.
      if (giveRights == null)
      {
        if (!Functions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, e.Action.ConfirmationDescription, null,
                                                     Constants.ApprovalTask.ApprovalReworkAssignmentConfirmDialogID.Forward))
          e.Cancel();
      }
      else if (giveRights == false)
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Status == Status.InProcess && Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var newDeadline = Functions.ApprovalTask.GetNewDeadline(_obj.Deadline);
      
      if (newDeadline != null)
      {
        _obj.Deadline = newDeadline.Value;
        _obj.Save();
        Dialogs.NotifyMessage(Docflow.Resources.CurrentAssignmentNewDeadline);
      }
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.ForwardPerformer == null && _obj.AccessRights.CanUpdate() && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void AbortApprovingAction(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;

      var assignees = new List<IRecipient>();
      if (_obj.Signatory != null)
        assignees.Add(_obj.Signatory);
      assignees.AddRange(_obj.AddApprovers.Where(a => a.Approver != null).Select(a => a.Approver));
      if (Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees) == false)
        return;
      
      if (Functions.ApprovalTask.GetReasonBeforeAbort(ApprovalTasks.As(_obj.Task), _obj.ActiveText, e, false))
      {
        _obj.Task.Abort();
        e.CloseFormAfterAction = true;
        Functions.ApprovalTask.AbortAsyncProcessingNotify(ApprovalTasks.As(_obj.Task));
      }
    }

    public virtual bool CanAbortApprovingAction(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Sungero.Workflow.AssignmentBase.Status.InProcess && _obj.ForwardPerformer == null;
    }

    public virtual void ForReapproving(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Если регламент указан, но есть ошибки в определении условий - значит, не все поля документа заполнены.
      var stages = Functions.ApprovalTask.Remote.GetStages(ApprovalTasks.As(_obj.Task));
      if (!stages.IsConditionsDefined)
      {
        e.AddError(stages.ErrorMessage);
        e.Cancel();
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var hasSignStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Docflow.ApprovalStage.StageType.Sign, document, stages.Stages);
      
      // Проверить, имеет ли право подписывающий на подпись.
      if (hasSignStage &&
          !Functions.ApprovalTask.Remote.CheckSignatory(ApprovalTasks.As(_obj.Task), _obj.Signatory, stages.Stages))
      {
        e.AddError(ApprovalTasks.Resources.IncorrectSignatory);
        return;
      }
      
      if (!Functions.ApprovalReworkAssignment.ValidateApprovalReworkAssignment(_obj, stages, e))
        e.Cancel();
      
      var assignees = new List<IRecipient>();
      if (_obj.Signatory != null)
        assignees.Add(_obj.Signatory);
      if (_obj.Addressee != null)
        assignees.Add(_obj.Addressee);
      assignees.AddRange(_obj.AddApprovers.Where(a => a.Approver != null).Select(a => a.Approver));
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), assignees, e.Action,
                                                                              Constants.ApprovalTask.ApprovalReworkAssignmentConfirmDialogID.ForReapproving))
        e.Cancel();
      
      // Если инициатор указан в этапе согласования с руководителем, то установить его подпись сразу.
      var approvalStage = Functions.ApprovalTask.Remote.AuthorMustApproveDocument(ApprovalTasks.As(_obj.Task), _obj.Performer,
                                                                                  _obj.Approvers.Select(app => Recipients.As(app.Approver)).Where(app => app != null).ToList());
      if (approvalStage.HasApprovalStage && document.Versions.Any())
      {
        if (document.HasVersions && approvalStage.NeedStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
        {
          e.AddError(ApprovalTasks.Resources.CertificateNeeded);
          return;
        }
        Functions.Module.EndorseDocument(_obj, true, approvalStage.NeedStrongSign, e);
      }
    }

    public virtual bool CanForReapproving(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.ForwardPerformer == null && _obj.DocumentGroup.OfficialDocuments.Any();
    }

  }
}
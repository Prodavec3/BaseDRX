using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalManagerAssignmentActions
  {
    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate() && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void ForRevision(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Валидация заполнения ответственного за доработку.
      if (_obj.ReworkPerformer == null)
      {
        e.AddError(ApprovalTasks.Resources.CantSendForReworkWithoutPerformer);
        e.Cancel();
      }
      
      // Валидация заполненности активного текста.
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalTasks.Resources.NeedTextForRework, e))
        e.Cancel();
      
      var assignees = new List<IRecipient>();
      if (_obj.Signatory != null)
        assignees.Add(_obj.Signatory);
      assignees.AddRange(_obj.AddApprovers.Where(a => a.Approver != null).Select(a => a.Approver));

      // Вызов диалога запроса выдачи прав на вложения (при отсутствии прав).
      Functions.ApprovalTask.ShowReworkConfirmationDialog(ApprovalTasks.As(_obj.Task), _obj, _obj.OtherGroup.All.ToList(), assignees, _obj.ReworkPerformer, e,
                                                          Constants.ApprovalTask.ApprovalManagerAssignmentConfirmDialogID.ForRevision);
      
      // Подписание согласующей подписью с результатом "не согласовано".
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      Functions.Module.EndorseDocument(_obj, false, needStrongSign, e);
    }

    public virtual bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
      {
        e.AddError(ApprovalTasks.Resources.CertificateNeeded);
        e.Cancel();
      }
      
      var assignees = new List<IRecipient>();
      if (_obj.Signatory != null)
        assignees.Add(_obj.Signatory);
      assignees.AddRange(_obj.AddApprovers.Where(a => a.Approver != null).Select(a => a.Approver));
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees);
      if (accessRightsGranted == false)
        e.Cancel();

      var confirmationMessage = e.Action.ConfirmationMessage;
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Docflow.ApprovalAssignments.Resources.ApprovalConfirmationMessage;
      if (accessRightsGranted == null && !Functions.ApprovalTask.ConfirmCompleteAssignment(document, confirmationMessage, Constants.ApprovalTask.ApprovalManagerAssignmentConfirmDialogID.Approved, false))
        e.Cancel();

      Functions.Module.EndorseDocument(_obj, true, needStrongSign, e);
    }

    public virtual bool CanApproved(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.Single();
      Functions.Module.RunApprovalSheetReport(document);
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any(d => d.HasVersions);
    }

  }
}
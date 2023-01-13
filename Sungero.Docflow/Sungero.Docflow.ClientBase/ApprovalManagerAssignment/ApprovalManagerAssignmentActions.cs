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
    public virtual void WithSuggestions(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ApprovalTasks.Resources.CommentNeeded);
        e.Cancel();
      }
      
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      var errorText = Functions.ApprovalTask.GetPrimaryDocumentApproveValidationError(ApprovalTasks.As(_obj.Task), needStrongSign);
      if (errorText != null)
      {
        e.AddError(errorText);
        e.Cancel();
      }

      var additionalAssignees = Functions.ApprovalManagerAssignment.GetAdditionalAssignees(_obj);
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), additionalAssignees);
      if (accessRightsGranted == false)
        e.Cancel();
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var confirmationMessage = e.Action.ConfirmationMessage;
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Sungero.Docflow.ApprovalManagerAssignments.Resources.ApprovalWithSuggestionsConfirmationMessage;
      // Не показывать лишний раз диалог подтверждения, если уже был показан диалог выдачи прав.
      if (accessRightsGranted == null && !Functions.ApprovalTask.ConfirmCompleteAssignment(document, confirmationMessage, Constants.ApprovalTask.ApprovalManagerAssignmentConfirmDialogID.WithSuggestions, false))
        e.Cancel();
      
      var comment = Docflow.Functions.Module.HasApproveWithSuggestionsMark(_obj.ActiveText) 
        ? _obj.ActiveText 
        : Functions.Module.AddApproveWithSuggestionsMark(_obj.ActiveText);
      var performer = Company.Employees.As(_obj.Performer);
      
      // Получить документы из группы вложений "Приложения", исключая дубли и основной документ.
      var addenda = Functions.Module.GetApprovalTaskAddendaForEndorse(_obj);
      addenda = addenda.Where(x => x.Id != document.Id).Distinct().ToList();
      Functions.Module.EndorseDocument(Sungero.Content.ElectronicDocuments.As(document), addenda, performer, true, needStrongSign, comment, e);
    }

    public virtual bool CanWithSuggestions(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
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
      return Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      var errorText = Functions.ApprovalTask.GetPrimaryDocumentApproveValidationError(ApprovalTasks.As(_obj.Task), needStrongSign);
      if (errorText != null)
      {
        e.AddError(errorText);
        e.Cancel();
      }
      
      var additionalAssignees = Functions.ApprovalManagerAssignment.GetAdditionalAssignees(_obj);
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), additionalAssignees);
      if (accessRightsGranted == false)
        e.Cancel();

      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var confirmationMessage = e.Action.ConfirmationMessage;
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Docflow.ApprovalAssignments.Resources.ApprovalConfirmationMessage;
      // Не показывать лишний раз диалог подтверждения, если уже был показан диалог выдачи прав.
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
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
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
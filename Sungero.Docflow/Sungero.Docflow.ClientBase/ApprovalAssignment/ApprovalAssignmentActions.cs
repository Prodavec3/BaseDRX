using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalAssignmentActions
  {
    public virtual void WithSuggestions(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
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
      
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (accessRightsGranted == false)
        e.Cancel();
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var confirmationMessage = e.Action.ConfirmationMessage;
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Docflow.ApprovalAssignments.Resources.ApprovalWithSuggestionsConfirmationMessage;
      if (accessRightsGranted == null && !Functions.ApprovalTask.ConfirmCompleteAssignment(document, confirmationMessage, Constants.ApprovalTask.ApprovalAssignmentConfirmDialogID.WithSuggestions, false))
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
      return _obj.Addressee == null && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void AddApprover(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(FreeApprovalTasks.Resources.AddApprover);
      var employee = dialog.AddSelect<Sungero.Company.IEmployee>(FreeApprovalTasks.Resources.Approver, true, null)
        .Where(x => x.Status != Company.Employee.Status.Closed);
      var addButton = dialog.Buttons.AddCustom(FreeApprovalTasks.Resources.Add);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick(a =>
                              {
                                if (a.IsValid && a.Button == addButton)
                                {
                                  if (Functions.ApprovalAssignment.Remote.CanForwardTo(_obj, employee.Value))
                                  {
                                    // Довыдаем права новому согласующему на вложения.
                                    if (Functions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                     _obj.OtherGroup.All.ToList(),
                                                                                     new List<IRecipient>() { employee.Value }) == false)
                                    {
                                      a.CloseAfterExecute = false;
                                      return;
                                    }
                                    
                                    Docflow.Functions.Module.Remote.AddApprover(_obj, employee.Value);
                                    _obj.State.Controls.Control.Refresh();
                                    var employeeNameInDative = Company.PublicFunctions.Employee.GetShortName(employee.Value, DeclensionCase.Dative, false);
                                    if (_obj.Stage.Sequence == ApprovalStage.Sequence.Parallel)
                                      Dialogs.NotifyMessage(FreeApprovalTasks.Resources.SendedToFormat(employeeNameInDative));
                                    else
                                      Dialogs.NotifyMessage(ApprovalTasks.Resources.TaskWillBeSendedFormat(employeeNameInDative));
                                  }
                                  else
                                    a.AddError(FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(employee.Value.Person.ShortName));
                                }
                              });
      var result = dialog.Show();
    }

    public virtual bool CanAddApprover(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess && _obj.AccessRights.CanUpdate() && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      if (_obj.Addressee == null)
      {
        e.AddError(FreeApprovalTasks.Resources.CantRedirectWithoutAddressee);
        e.Cancel();
      }
      
      if (_obj.Addressee == _obj.Performer)
      {
        e.AddError(FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
        e.Cancel();
      }
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                              _obj.OtherGroup.All.ToList(),
                                                                              new List<IRecipient>() { _obj.Addressee },
                                                                              e.Action, Constants.ApprovalTask.ApprovalAssignmentConfirmDialogID.Forward))
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
      
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate() && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void NoPerformers(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanNoPerformers(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
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
      
      // Вызов диалога запроса выдачи прав на вложения (при отсутствии прав).
      Functions.ApprovalTask.ShowReworkConfirmationDialog(ApprovalTasks.As(_obj.Task), _obj, _obj.OtherGroup.All.ToList(), new List<IRecipient>(), _obj.ReworkPerformer, e,
                                                          Constants.ApprovalTask.ApprovalAssignmentConfirmDialogID.ForRevision);
      
      // Подписание согласующей подписью с результатом "не согласовано".
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      Functions.Module.EndorseDocument(_obj, false, needStrongSign, e);
    }

    public virtual bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      var errorText = Functions.ApprovalTask.GetPrimaryDocumentApproveValidationError(ApprovalTasks.As(_obj.Task), needStrongSign);
      if (errorText != null)
      {
        e.AddError(errorText);
        e.Cancel();
      }
      
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (accessRightsGranted == false)
        e.Cancel();
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var confirmationMessage = e.Action.ConfirmationMessage;
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Docflow.ApprovalAssignments.Resources.ApprovalConfirmationMessage;
      if (accessRightsGranted == null && !Functions.ApprovalTask.ConfirmCompleteAssignment(document, confirmationMessage, Constants.ApprovalTask.ApprovalAssignmentConfirmDialogID.Approved, false))
        e.Cancel();
      
      Functions.Module.EndorseDocument(_obj, true, needStrongSign, e);
    }

    public virtual bool CanApproved(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && _obj.DocumentGroup.OfficialDocuments.Any();
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
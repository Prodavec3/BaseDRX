using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalReviewAssignmentActions
  {
    public virtual void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalSendingAssignment.SendByMail(ApprovalTasks.As(_obj.Task));
    }

    public virtual bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any(d => d.HasVersions);
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

    public virtual void CreateAcquaintance(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var approvalTask = ApprovalTasks.As(_obj.Task);
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(approvalTask))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      var subTask = RecordManagement.PublicFunctions.Module.Remote.CreateAcquaintanceTaskAsSubTask(document, _obj);
      if (subTask != null)
      {
        RecordManagement.PublicFunctions.Module.SynchronizeAttachmentsToAcquaintance(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault(),
                                                                                     _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                                                     Functions.ApprovalTask.GetAddedAddenda(approvalTask),
                                                                                     Functions.ApprovalTask.GetRemovedAddenda(approvalTask),
                                                                                     _obj.OtherGroup.All.ToList(),
                                                                                     subTask);
        subTask.ShowModal();
      }
    }

    public virtual bool CanCreateAcquaintance(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void SendViaExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
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

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
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
                                                          Constants.ApprovalTask.ApprovalReviewAssignmentConfirmDialogID.ForRework);
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void Abort(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Проверить заполненность активного текста.
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalReviewAssignments.Resources.NeedTextForAbortReview, e))
        e.Cancel();
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                              Constants.ApprovalTask.ApprovalReviewAssignmentConfirmDialogID.Abort))
        e.Cancel();
    }

    public virtual bool CanAbort(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }
    
    public virtual void Informed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var haveError = false;
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesRe.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          haveError = true;
        }
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
      {
        e.AddError(ApprovalReviewAssignments.Resources.CertificateNeeded);
        haveError = true;
      }
      
      if (haveError)
        return;
      
      var sendDialog = Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesRe.Select(x => x.StageType), e);
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), sendDialog ? null : e.Action,
                                                                              Constants.ApprovalTask.ApprovalReviewAssignmentConfirmDialogID.Informed))
        e.Cancel();
      
      // Подписать ЭП.
      var comment = string.IsNullOrWhiteSpace(_obj.ActiveText) ? ApprovalReviewAssignments.Resources.Informed : _obj.ActiveText;
      Functions.ApprovalReviewAssignment.SetSignature(_obj, e, comment);
    }

    public virtual bool CanInformed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void AddResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var haveError = false;
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ApprovalReviewAssignments.Resources.ResolutionTextNeeded);
        haveError = true;
      }
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesRe.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          haveError = true;
        }
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
      {
        e.AddError(ApprovalReviewAssignments.Resources.CertificateNeeded);
        haveError = true;
      }
      
      if (haveError)
        return;

      var sendDialog = Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesRe.Select(x => x.StageType), e);
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), sendDialog ? null : e.Action,
                                                                              Constants.ApprovalTask.ApprovalReviewAssignmentConfirmDialogID.AddResolution))
        e.Cancel();
      
      // Подписать ЭП.
      var comment = string.IsNullOrWhiteSpace(_obj.ActiveText) ? ApprovalReviewAssignments.Resources.ResolutionAdded : _obj.ActiveText;
      Functions.ApprovalReviewAssignment.SetSignature(_obj, e, comment);
    }

    public virtual bool CanAddResolution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void AddActionItem(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      var haveError = false;
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesRe.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = document.RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          haveError = true;
        }
      }
      
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
      {
        e.AddError(ApprovalReviewAssignments.Resources.CertificateNeeded);
        haveError = true;
      }
      
      // Для утверждения необходимо, чтобы документ не был заблокирован.
      var lockInfo = Functions.OfficialDocument.GetDocumentLockInfo(document);
      var canSignByEmployee = Functions.OfficialDocument.Remote.CanSignByEmployee(document, Company.Employees.Current);
      var currentEmployee = Company.Employees.Current;
      if (lockInfo != null && lockInfo.IsLocked &&
          document.AccessRights.CanApprove() && canSignByEmployee)
      {
        e.AddError(Sungero.Docflow.ApprovalReviewAssignments.Resources.CanNotSetSignatureFormat(lockInfo.OwnerName, lockInfo.LockTime));
        haveError = true;
      }

      if (haveError)
        return;
      
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), e);
      var sendDialog = Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesRe.Select(x => x.StageType), e);
      
      if (Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), (confirmationAccepted || sendDialog) ? null : e.Action,
                                                                             Constants.ApprovalTask.ApprovalReviewAssignmentConfirmDialogID.AddActionItem) == false)
        e.Cancel();
      
      // Подписать ЭП.
      var comment = string.IsNullOrWhiteSpace(_obj.ActiveText) ? ApprovalReviewAssignments.Resources.SentForExecution : _obj.ActiveText;
      Functions.ApprovalReviewAssignment.SetSignature(_obj, e, comment);
    }

    public virtual bool CanAddActionItem(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      var parentAssignmentId = _obj.Id;
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var assignedBy = ApprovalTasks.As(_obj.Task).Addressee;
      var hackTask = Functions.Module.CreateActionItemExecutionWithResolution(document, parentAssignmentId, _obj.ActiveText, assignedBy);
      if (hackTask != null)
        hackTask.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any();
    }

  }

}
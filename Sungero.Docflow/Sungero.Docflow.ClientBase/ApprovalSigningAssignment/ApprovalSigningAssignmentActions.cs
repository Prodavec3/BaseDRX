using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSigningAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalSigningAssignmentActions
  {
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
      return _obj.DocumentGroup.OfficialDocuments.Any() &&
        Functions.ApprovalSendingAssignment.CanSendToCounterparty(document);
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
      var assignedBy = ApprovalTasks.As(_obj.Task).Signatory;
      var hackTask = Functions.Module.CreateActionItemExecutionWithResolution(document, parentAssignmentId, _obj.ActiveText, assignedBy);
      if (hackTask != null)
        hackTask.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void ConfirmSign(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesSig.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          return;
        }
      }
      
      // Запросить подтверждение "не создания" поручений, если схлопнуто с созданием поручений.
      var confirmationAccepted = false;
      if (_obj.CollapsedStagesTypesSig.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Execution))
        confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), e);
      
      confirmationAccepted = confirmationAccepted || Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesSig.Select(x => x.StageType), e);
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (accessRightsGranted == false)
        e.Cancel();
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (!confirmationAccepted && accessRightsGranted == null &&
          !Functions.ApprovalTask.ConfirmCompleteAssignment(document, e.Action.ConfirmationMessage, Constants.ApprovalTask.ApprovalSigningAssignmentConfirmDialogID.ConfirmSign, true))
        e.Cancel();
    }

    public virtual bool CanConfirmSign(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }

    public virtual void CreateCoverLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      Functions.ApprovalSendingAssignment.CreateCoverLetter(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), _obj.OtherGroup);
    }

    public virtual bool CanCreateCoverLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (_obj.IsCollapsed == false)
        return false;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return _obj.Status == Workflow.Task.Status.InProcess &&
        _obj.DocumentGroup.OfficialDocuments.Any() &&
        Functions.ApprovalTask.EnableCreateCoverLetter(document);
    }

    public virtual void Abort(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Валидация заполненности активного текста.
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalTasks.Resources.NeedTextForAbort, e))
        e.Cancel();
      
      if (!Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action, Constants.ApprovalTask.ApprovalSigningAssignmentConfirmDialogID.Abort))
        e.Cancel();
      
      // Подписание согласующей подписью с результатом "не согласовано".
      var task = ApprovalTasks.As(_obj.Task);
      var isConfirm = _obj.Stage.IsConfirmSigning;
      if (isConfirm == false)
      {
        var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
        Functions.Module.EndorseDocument(_obj, false, needStrongSign, e);
      }
    }

    public virtual bool CanAbort(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
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
                                                          Constants.ApprovalTask.ApprovalSigningAssignmentConfirmDialogID.ForRevision);
      
      // Подписание согласующей подписью с результатом "не согласовано".
      var task = ApprovalTasks.As(_obj.Task);
      var isConfirm = _obj.Stage.IsConfirmSigning;
      if (isConfirm == false)
      {
        var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
        Functions.Module.EndorseDocument(_obj, false, needStrongSign, e);
      }
    }

    public virtual bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task));
    }

    public virtual void Sign(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      var task = ApprovalTasks.As(_obj.Task);
      var currentEmployee = Company.Employees.Current;
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var validate = Functions.ApprovalSigningAssignment.Remote.ValidateBeforeSign(_obj);
      
      foreach (var error in validate.Errors)
        e.AddError(error);
      var haveError = validate.Errors.Any();
      
      var needStrongSign = _obj.Stage.NeedStrongSign ?? false;
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
      {
        e.AddError(ApprovalTasks.Resources.CertificateNeededToSign);
        haveError = true;
      }
      
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.CollapsedStagesTypesSig.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          haveError = true;
        }
      }
      
      if (!validate.CanApprove)
      {
        if (!document.AccessRights.CanApprove())
          e.AddError(ApprovalSigningAssignments.Resources.NoRigthToApproveDocumentForSubstituteFormat(currentEmployee.Name, _obj.Performer.Name));
        else
          e.AddError(Docflow.Resources.NoRightsToApproveDocument);
        haveError = true;
      }
      if (haveError)
        return;
      
      // Запросить подтверждение "не создания" поручений, если схлопнуто с созданием поручений.
      var confirmationAccepted = false;
      if (_obj.CollapsedStagesTypesSig.Any(s => s.StageType == Docflow.ApprovalStage.StageType.Execution))
        confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), e);
      
      ((Domain.Shared.IExtendedEntity)document).Params[Exchange.PublicConstants.Module.DefaultSignResult] = true;
      confirmationAccepted = confirmationAccepted || Functions.Module.ShowConfirmationDialogSendToCounterparty(_obj, _obj.CollapsedStagesTypesSig.Select(x => x.StageType), e);
      ((Domain.Shared.IExtendedEntity)document).Params.Remove(Exchange.PublicConstants.Module.DefaultSignResult);
      
      var accessRightsGranted = Functions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList());
      if (accessRightsGranted == false)
        e.Cancel();
      
      var confirmationMessage = e.Action.ConfirmationMessage;
      var documentViewed = Functions.ApprovalTask.Remote.DocumenHasBeenViewed(document);
      if (_obj.AddendaGroup.OfficialDocuments.Any())
        confirmationMessage = Docflow.ApprovalSigningAssignments.Resources.SignConfirmationMessage;
      if (!confirmationAccepted && accessRightsGranted == null && !Functions.ApprovalTask.ConfirmCompleteAssignment(validate.DocumentBodyChanged, documentViewed, confirmationMessage,
                                                                                                                    Constants.ApprovalTask.ApprovalSigningAssignmentConfirmDialogID.Sign, true))
        e.Cancel();
      
      // Подписание утверждающей подписью.
      Functions.ApprovalSigningAssignment.ApproveDocument(_obj, needStrongSign, e);
    }

    public virtual bool CanSign(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
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
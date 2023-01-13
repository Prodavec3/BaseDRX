using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Client
{
  partial class VerificationAssignmentActions
  {
    public virtual void ShowInvalidDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct()
        .Cast<Docflow.IOfficialDocument>().ToList();
      
      var invalidDocuments = attachments
        .Where(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x))
        .ToList();
      if (invalidDocuments.Count() > 0)
        invalidDocuments.Show();
    }

    public virtual bool CanShowInvalidDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void DeleteDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct()
        .Cast<Docflow.IOfficialDocument>().ToList();
      
      var successfullyDeletedDocumentIds = SmartProcessing.Client.ModuleFunctions.DeleteDocumentsDialogInWeb(attachments);
      var attachmentsToRemove = new List<Sungero.Domain.Shared.IEntity>();
      foreach (var entity in _obj.Task.Attachments.Where(x => successfullyDeletedDocumentIds.Any(y => y == x.Id)))
        attachmentsToRemove.Add(entity);
      foreach (var attachment in attachmentsToRemove)
        _obj.Task.Attachments.Remove(attachment);

    }

    public virtual bool CanDeleteDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (ClientApplication.ApplicationType != ApplicationType.Web)
        return false;
      
      if (_obj.Status != Sungero.Workflow.AssignmentBase.Status.InProcess)
        return false;
      
      if (e.Params.Contains(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName))
      {
        bool canDelete;
        if (!(e.Params.TryGetValue(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName, out canDelete) && canDelete))
          return false;
      }
      
      return _obj.AllAttachments.Any(a => Content.ElectronicDocuments.Is(a));
    }

    public virtual void SendForExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей во всех документах комплекта.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      if (attachments.Cast<Docflow.IOfficialDocument>().Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenSendInWork,
                   _obj.Info.Actions.ShowInvalidDocuments);
        return;
      }
      
      // Определить главный документ.
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      var probablyMainDocument = Content.ElectronicDocuments.As(Functions.Module.GetLeadingDocument(suitableDocuments.Cast<Docflow.IOfficialDocument>().ToList()));
      var mainDocument = Docflow.PublicFunctions.OfficialDocument.ChooseMainDocument(suitableDocuments,
                                                                                     new List<Content.IElectronicDocument> { probablyMainDocument });
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var actionItemTask = Sungero.RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(mainOfficialDocument);
      
      // Добавить вложения.
      foreach (var attachment in attachments.Where(att => !Equals(att, mainDocument)))
      {
        if (!Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          continue;
        else
          actionItemTask.OtherGroup.All.Add(attachment);
      }
      
      actionItemTask.Show();
    }

    public virtual bool CanSendForExecution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      return suitableDocuments.Any();
    }

    public virtual void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей во всех документах комплекта.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      if (attachments.Cast<Docflow.IOfficialDocument>().Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenSendInWork,
                   _obj.Info.Actions.ShowInvalidDocuments);
        return;
      }
      
      // Определить главный документ.
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForReview);
      var probablyMainDocument = Content.ElectronicDocuments.As(Functions.Module.GetLeadingDocument(suitableDocuments.Cast<Docflow.IOfficialDocument>().ToList()));
      var mainDocument = Docflow.PublicFunctions.OfficialDocument.ChooseMainDocument(suitableDocuments,
                                                                                     new List<Content.IElectronicDocument> { probablyMainDocument });
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateReviewTask(mainOfficialDocument))
        return;

      var task = RecordManagement.PublicFunctions.Module.Remote.CreateDocumentReview(mainOfficialDocument);
      var reviewTask = RecordManagement.DocumentReviewTasks.As(task);
      
      // Добавить вложения.
      foreach (var attachment in attachments.Where(att => !Equals(att, mainDocument)))
      {
        if (!Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          continue;
        else
          reviewTask.OtherGroup.All.Add(attachment);
      }
      
      reviewTask.Show();
    }

    public virtual bool CanSendForReview(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForReview);
      return suitableDocuments.Any();
    }

    public virtual void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей во всех документах комплекта.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      if (attachments.Cast<Docflow.IOfficialDocument>().Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenSendInWork,
                   _obj.Info.Actions.ShowInvalidDocuments);
        return;
      }
      
      // Определить главный документ.
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      var probablyMainDocument = Content.ElectronicDocuments.As(Functions.Module.GetLeadingDocument(suitableDocuments.Cast<Docflow.IOfficialDocument>().ToList()));
      var mainDocument = Docflow.PublicFunctions.OfficialDocument.ChooseMainDocument(suitableDocuments,
                                                                                     new List<Content.IElectronicDocument> { probablyMainDocument });
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
      var freeApprovalTask = Sungero.Docflow.PublicFunctions.Module.Remote.CreateFreeApprovalTask(mainOfficialDocument);
      
      // Добавить вложения.
      foreach (var attachment in attachments.Where(att => !Equals(att, mainDocument)))
      {
        if (!Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
          continue;
        else
          freeApprovalTask.OtherGroup.All.Add(attachment);
      }
      
      freeApprovalTask.Show();
    }

    public virtual bool CanSendForFreeApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      return suitableDocuments.Any();
    }

    public virtual void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей во всех документах комплекта.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();

      if (attachments.Cast<Docflow.IOfficialDocument>().Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenSendInWork,
                   _obj.Info.Actions.ShowInvalidDocuments);
        return;
      }
      
      // Определить главный документ.
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      var probablyMainDocument = Content.ElectronicDocuments.As(Functions.Module.GetLeadingDocument(suitableDocuments.Cast<IOfficialDocument>().ToList()));
      var mainDocument = Docflow.PublicFunctions.OfficialDocument.ChooseMainDocument(suitableDocuments,
                                                                                     new List<Content.IElectronicDocument> { probablyMainDocument });
      if (mainDocument == null)
        return;
      var mainOfficialDocument = OfficialDocuments.As(mainDocument);
      
      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateApprovalTask(mainOfficialDocument))
        return;
      
      // Проверить наличие регламента.
      var availableApprovalRules = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(mainOfficialDocument);
      if (availableApprovalRules.Any())
      {
        var approvalTask = Docflow.PublicFunctions.Module.Remote.CreateApprovalTask(mainOfficialDocument);

        // Добавить вложения.
        foreach (var attachment in attachments.Where(att => !Equals(att, mainDocument)))
        {
          if (!Docflow.PublicFunctions.OfficialDocument.NeedToAttachDocument(attachment, mainOfficialDocument))
            continue;
          else
            approvalTask.OtherGroup.All.Add(attachment);
        }
        
        approvalTask.Show();
      }
      else
      {
        // Если по документу нет регламента, вывести сообщение.
        Dialogs.ShowMessage(Docflow.OfficialDocuments.Resources.NoApprovalRuleWarning, MessageType.Warning);
        return;
      }
    }

    public virtual bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var suitableDocuments = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments,
                                                                                            Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      return suitableDocuments.Any();
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (_obj.Addressee == null)
      {
        e.AddError(VerificationTasks.Resources.CantRedirectWithoutAddressee);
        e.Cancel();
      }
      
      if (_obj.Addressee == _obj.Performer)
      {
        e.AddError(VerificationTasks.Resources.ApproverAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
        e.Cancel();
      }

      if (_obj.NewDeadline == null)
      {
        e.AddError(VerificationTasks.Resources.CantRedirectWithoutNewDeadline);
        e.Cancel();
      }

      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                         Constants.VerificationTask.VerificationAssignmentConfirmDialogID.ReAddress))
      {
        e.Cancel();
        return;
      }
      
      // Прокинуть исполнителя в задачу.
      var task = VerificationTasks.As(_obj.Task);
      task.Addressee = _obj.Addressee;
      task.Save();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Status == Status.InProcess && Functions.VerificationTask.HasDocumentAndCanRead(VerificationTasks.As(_obj.Task));
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct()
        .Cast<Docflow.IOfficialDocument>().ToList();

      if (attachments.Any(x => Sungero.Docflow.PublicFunctions.OfficialDocument.HasEmptyRequiredProperties(x)))
      {
        e.AddError(VerificationAssignments.Resources.InvalidDocumentWhenCompleted,
                   _obj.Info.Actions.ShowInvalidDocuments);
        e.Cancel();
      }
      
      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                         Constants.VerificationTask.VerificationAssignmentConfirmDialogID.Complete))
      {
        e.Cancel();
        return;
      }
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ExchangeDocumentProcessingAssignmentActions
  {
    public virtual void SendForExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendActionItem);
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
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendActionItem);
      return documentsList.Any();
    }

    public virtual void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForReview);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Создать задачу.
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
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForReview);
      return documentsList.Any();
    }

    public virtual void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment.ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
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
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval);
      return documentsList.Any();
    }

    public virtual void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Определить главный документ.
      var attachments = _obj.AllAttachments.Select(a => Content.ElectronicDocuments.As(a)).Distinct().ToList();
      var needSigningAttachments = _obj.NeedSigning.All.Select(a => Content.ElectronicDocuments.As(a)).ToList();
      var mainDocument = Functions.ExchangeDocumentProcessingAssignment
        .ShowMainDocumentChoosingDialog(attachments, needSigningAttachments, Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      if (mainDocument == null)
        return;
      var mainOfficialDocument = Docflow.OfficialDocuments.As(mainDocument);
      
      // Проверить наличие регламента.
      var availableApprovalRules = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(mainOfficialDocument);
      if (availableApprovalRules.Any())
      {
        // Создать задачу.
        var approvalTask = Sungero.Docflow.PublicFunctions.Module.Remote.CreateApprovalTask(mainOfficialDocument);

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
      var documentsList = Docflow.PublicFunctions.OfficialDocument.GetSuitableDocuments(attachments, Docflow.OfficialDocuments.Info.Actions.SendForApproval);
      return documentsList.Any();
    }

    public virtual void Abort(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.BusinessUnitBox))
      {
        e.AddError(Resources.RejectCertificateNotFoundReadressToResponsible);
        return;
      }
      
      if (string.IsNullOrEmpty(_obj.ActiveText))
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.NeedCommentToAbort);
        return;
      }
      else if (_obj.ActiveText.Length > 1000)
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.TextOverlong);
        return;
      }
      
      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                         Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.Abort))
      {
        e.Cancel();
        return;
      }
      
      var certificate = Functions.Module.GetCurrentUserExchangeCertificate(_obj.Box, Company.Employees.Current);
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, certificate))
      {
        e.Cancel();
        return;
      }
      
      var documents = _obj.AllAttachments.Select(d => Docflow.OfficialDocuments.As(d)).Where(d => d != null).ToList();
      var error = Exchange.PublicFunctions.Module.SendAmendmentRequest(documents, _obj.Counterparty, _obj.ActiveText, false, _obj.Box, certificate, false);
      if (!string.IsNullOrWhiteSpace(error))
      {
        if (error == Resources.CertificateNotFound)
          e.AddError(Resources.RejectCertificateNotFoundReadressToResponsible);
        else if (error == Resources.AllAnswersIsAlreadySent)
          e.AddError(error);
        else
          e.AddError(Resources.CannotSendAmendmentNotice);
      }
    }

    public virtual bool CanAbort(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      var isIncoming = ExchangeDocumentProcessingTasks.As(_obj.Task).IsIncoming == true;
      return _obj.Addressee == null && isIncoming && Docflow.OfficialDocuments.AccessRights.CanSendByExchange();
    }

    public virtual void ReAddress(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Не давать переадресовать, если адресат или срок не заполнены.
      if (_obj.Addressee == null || _obj.NewDeadline == null)
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.CantReAddressWithoutAddresseeAndDeadline);
        return;
      }
      
      // Не давать переадресовывать на срок меньше, чем сейчас.
      if (_obj.NewDeadline.HasValue)
      {
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee, _obj.NewDeadline, Calendar.Now))
        {
          e.AddError(ExchangeDocumentProcessingAssignments.Resources.ImpossibleSpecifyDeadlineLessThenToday);
          return;
        }
      }

      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                         Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.ReAddress))
      {
        e.Cancel();
        return;
      }
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, null))
      {
        e.Cancel();
        return;
      }
      
      // Прокинуть новый срок и исполнителя в задачу.
      var task = ExchangeDocumentProcessingTasks.As(_obj.Task);
      task.Addressee = _obj.Addressee;
      task.Deadline = _obj.NewDeadline;
      task.Save();
    }

    public virtual bool CanReAddress(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (_obj.AllAttachments.Any(d => Sungero.Docflow.ExchangeDocuments.Is(d)))
      {
        e.AddError(ExchangeDocumentProcessingTasks.Resources.NotAllDocumentTypesAreChanged);
        return;
      }
      
      var areAllDocumentsInWork = Functions.ExchangeDocumentProcessingTask.Remote.AreAllDocumentsSendToWork(ExchangeDocumentProcessingTasks.As(_obj.Task));
      var dialogText = e.Action.ConfirmationMessage;
      var dialogID = Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.Complete;
      if (!areAllDocumentsInWork)
      {
        dialogText = ExchangeDocumentProcessingTasks.Resources.NotAllDocumentsSendedForProcessing;
        dialogID = Constants.ExchangeDocumentProcessingTask.ExchangeDocumentProcessingAssignmentConfirmDialogID.CompleteWithoutAllDocumentsSendedForProcessing;
      }

      if (!Sungero.Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, null, null, dialogID))
      {
        e.Cancel();
        return;
      }
      
      if (!Functions.ExchangeDocumentProcessingAssignment.SendDeliveryConfirmation(_obj, null))
      {
        e.Cancel();
        return;
      }

    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && _obj.NewDeadline == null;
    }

  }

}
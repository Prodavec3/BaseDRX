using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewManagerAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewManagerAssignmentActions
  {
    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность текста комментария.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewDraftResolutionAssignments.Resources.NeedTextToRework);
        return;
      }
      
      // Вывести предупреждение.
      var dialogID = Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.ForRework;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            null, e.Action, dialogID))
      {
        e.Cancel();
      }
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (_obj.Addressee == null)
      {
        e.AddError(DocumentReviewTasks.Resources.CantRedirectWithoutAddressee);
        e.Cancel();
      }
      
      if (Equals(_obj.Addressee, _obj.Performer))
      {
        e.AddError(DocumentReviewTasks.Resources.AddresseeAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
        e.Cancel();
      }
      
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            assignees,
                                                                                            e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.Forward))
        e.Cancel();
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      var document = documentReviewTask.DocumentForReviewGroup.OfficialDocuments.First();
      var assignedBy = documentReviewTask.Addressee.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed &&
        Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, documentReviewTask.Addressee) ?
        documentReviewTask.Addressee :
        null;
      var task = Functions.Module.Remote.CreateActionItemExecutionWithResolution(document, _obj.Id, _obj.ActiveText, assignedBy);
      
      Functions.Module.SynchronizeAttachmentsToActionItem(_obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                          _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                          Functions.DocumentReviewTask.GetAddedAddenda(documentReviewTask),
                                                          Functions.DocumentReviewTask.GetRemovedAddenda(documentReviewTask),
                                                          _obj.OtherGroup.All.ToList(),
                                                          task);
      
      task.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && 
        _obj.Addressee == null && 
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void AddResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность текста резолюции при выполнении задания с результатом "Вынести резолюцию".
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewManagerAssignments.Resources.ResolutionTextNeeded);
        return;
      }
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.AddResolution))
        e.Cancel();
    }

    public virtual bool CanAddResolution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void Explored(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Вывести предупреждение, если заполнена резолюция.
      var resolutionExists = !string.IsNullOrWhiteSpace(_obj.ActiveText);
      if (resolutionExists)
      {
        var dialogText = e.Action.ConfirmationMessage;
        var dialogDescription = ReviewManagerAssignments.Resources.ConfirmResultIsExploredDescription;
        var dialogID = Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.ExploredWithResolution;
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(dialogText, dialogDescription, null, dialogID))
          e.Cancel();
      }
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            null, resolutionExists ? null : e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.Explored))
      {
        e.Cancel();
      }
    }

    public virtual bool CanExplored(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void AddAssignment(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            confirmationAccepted ? null : e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.AddAssignment))
        e.Cancel();
    }

    public virtual bool CanAddAssignment(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }
}
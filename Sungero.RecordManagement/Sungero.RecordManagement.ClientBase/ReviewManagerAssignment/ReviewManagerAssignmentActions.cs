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
    public virtual void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
      
      if (_obj.Addressee == null)
      {
        e.AddError(DocumentReviewTasks.Resources.CantRedirectWithoutAddressee);
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
      return true;
    }

    public virtual void CreateActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      
      var documentReview = DocumentReviewTasks.As(_obj.Task);
      var task = Functions.DocumentReviewTask.CreateActionItemExecution(_obj, documentReview, _obj.ActiveText);
      
      if (documentReview.Addressee.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        task.AssignedBy = null;
      else
        task.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(task.DocumentsGroup.OfficialDocuments.SingleOrDefault(), documentReview.Addressee) ?
          documentReview.Addressee :
          null;
      
      task.ShowModal();
    }

    public virtual bool CanCreateActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.InProcess && _obj.Addressee == null;
    }

    public virtual void AddResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
      
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
      return _obj.Addressee == null;
    }

    public virtual void Explored(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
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
      return _obj.Addressee == null;
    }

    public virtual void AddAssignment(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
      
      var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(),
                                                                                            confirmationAccepted ? null : e.Action,
                                                                                            Constants.DocumentReviewTask.ReviewManagerAssignmentConfirmDialogID.AddAssignment))
        e.Cancel();
    }

    public virtual bool CanAddAssignment(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

  }
}
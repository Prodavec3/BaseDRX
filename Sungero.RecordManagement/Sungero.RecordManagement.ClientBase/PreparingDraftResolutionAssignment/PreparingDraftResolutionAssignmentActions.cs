using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class PreparingDraftResolutionAssignmentActions
  {
    public virtual void PrintResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      var report = RecordManagement.Reports.GetDraftResolutionReport();
      var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks;
      report.Resolution.AddRange(actionItems);
      report.TextResolution = _obj.ActiveText;
      report.Document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      report.Author = DocumentReviewTasks.As(_obj.Task).Addressee;
      report.Open();
    }

    public virtual bool CanPrintResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Assignment.Status.InProcess && _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }

    public virtual void AddAssignment(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
      
      if (!_obj.ResolutionGroup.ActionItemExecutionTasks.Any(t => t.Status == ActionItemExecutionTask.Status.Draft))
      {
        var confirmationAccepted = Functions.Module.ShowConfirmationDialogCreationActionItem(_obj, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(), e);
        var dialogID = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.AddAssignment;
        if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                             _obj.OtherGroup.All.ToList(),
                                                                                             confirmationAccepted ? null : e.Action,
                                                                                             dialogID) == false)
          e.Cancel();
      }
      else
      {
        Functions.DocumentReviewTask.CheckOverdueActionItemExecutionTasks(DocumentReviewTasks.As(_obj.Task), e);
        
        var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                    _obj.OtherGroup.All.ToList(),
                                                                                    null);
        if (giveRights == false)
          e.Cancel();
        
        if (giveRights == null && Functions.PreparingDraftResolutionAssignment.ShowConfirmationDialogStartDraftResolution(_obj, e) == false)
          e.Cancel();
        
        RecordManagement.Functions.DocumentReviewTask.Remote.StartActionItemsForDraftResolution(DocumentReviewTasks.As(_obj.Task), _obj);
      }
    }

    public virtual bool CanAddAssignment(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
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
      
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
        e.Cancel();
      }
      
      var hasActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      if (hasActionItems)
      {
        var dropDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ExploredWithDeletingDraftResolutions;
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      var confirmDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.Explored;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            hasActionItems ? null : e.Action,
                                                                                            confirmDialogId))
      {
        e.Cancel();
      }
      
      _obj.NeedDeleteActionItems = hasActionItems;
    }

    public virtual bool CanExplored(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

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
      
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
        e.Cancel();
      }
      
      var hasActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      if (hasActionItems)
      {
        var dropDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.ForwardWithDeletingDraftResolutions;
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);

        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      var confirmDialogId = Constants.DocumentReviewTask.PreparingDraftResolutionAssignmentConfirmDialogID.Forward;
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            assignees,
                                                                                            hasActionItems ? null : e.Action,
                                                                                            confirmDialogId))
      {
        e.Cancel();
      }
      
      _obj.NeedDeleteActionItems = hasActionItems;
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void SendForReview(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        e.Cancel();
      }
      
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
      
      var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj,
                                                                                  _obj.OtherGroup.All.ToList(),
                                                                                  null);
      if (giveRights == false)
        e.Cancel();
      
      if (giveRights == null && !Functions.PreparingDraftResolutionAssignment.ShowConfirmationDialogSendForReview(_obj, e))
        e.Cancel();
    }

    public virtual bool CanSendForReview(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

    public virtual void AddResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)))
      {
        e.AddError(DocumentReviewTasks.Resources.NoRightsToDocument);
        return;
      }
      
      _obj.Save();
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var task = Functions.Module.Remote.CreateActionItemExecution(document);
      var assignee = task.Assignee ?? Users.Current;
      task.MaxDeadline = _obj.Deadline.HasValue ? _obj.Deadline.Value : Calendar.Today.AddWorkingDays(assignee, 2);
      task.IsDraftResolution = true;
      var assignedBy = DocumentReviewTasks.As(_obj.Task).Addressee;
      task.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, assignedBy) ? assignedBy : null;
      foreach (var otherGroupAttachment in _obj.OtherGroup.All)
        task.OtherGroup.All.Add(otherGroupAttachment);
      task.ShowModal();
      if (!task.State.IsInserted)
      {
        _obj.ResolutionGroup.ActionItemExecutionTasks.Add(task);
        _obj.Save();
      }
    }

    public virtual bool CanAddResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == PreparingDraftResolutionAssignment.Status.InProcess && _obj.Addressee == null;
    }

  }

}
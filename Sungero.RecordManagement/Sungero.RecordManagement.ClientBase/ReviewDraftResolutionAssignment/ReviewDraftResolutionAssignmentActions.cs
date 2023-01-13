using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReviewDraftResolutionAssignmentActions
  {
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
      
      // Подтверждение удаления проекта резолюции.
      var hasActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      var dropDialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.ForwardWithDrop;
      if (hasActionItems)
      {
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      // Запрос прав на группу "Дополнительно".
      var assignees = new List<IRecipient>() { _obj.Addressee };
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(_obj.Addressee);
      if (assistant != null)
        assignees.Add(assistant);
      var grandRightDialogResult = Docflow.PublicFunctions.Module
        .ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), assignees);
      if (grandRightDialogResult == false)
        e.Cancel();
      
      // Подтверждение выполнения действия.
      var dialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.Forward;
      if (!hasActionItems && grandRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, dialogId))
        e.Cancel();
      
      _obj.NeedDeleteActionItems = hasActionItems;
    }

    public virtual bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task)) &&
        Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }
    
    public virtual void AddResolution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
        e.Cancel();
      }
      
      // Проверить заполненность текста резолюции.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ReviewDraftResolutionAssignments.Resources.NeedTextToRework);
        return;
      }
      
      var dialogID = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.AddResolution;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogID))
        e.Cancel();
    }

    public virtual bool CanAddResolution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

    public virtual void Informed(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
        e.Cancel();
      }
      
      // Подтверждение удаления проекта резолюции.
      var hasActionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      var dropDialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.InformedWithDrop;
      if (hasActionItems)
      {
        var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage,
                                                                                    Resources.ConfirmDeleteDraftResolutionAssignment,
                                                                                    null, dropDialogId);
        if (!dropIsConfirmed)
          e.Cancel();
      }
      
      // Запрос прав на группу "Дополнительно".
      var grandRightDialogResult = Docflow.PublicFunctions.Module
        .ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), null);
      if (grandRightDialogResult == false)
        e.Cancel();
      
      // Подтверждение выполнения действия.
      var dialogId = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.Informed;
      if (!hasActionItems && grandRightDialogResult == null &&
          !Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, dialogId))
        e.Cancel();
      
      _obj.NeedDeleteActionItems = hasActionItems;
    }

    public virtual bool CanInformed(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

    public virtual void ForExecution(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        e.AddError(DocumentReviewTasks.Resources.FindNotDraftResolution);
        e.Cancel();
      }
      
      Functions.DocumentReviewTask.CheckOverdueActionItemExecutionTasks(DocumentReviewTasks.As(_obj.Task), e);
      
      // Замена стандартного диалога подтверждения выполнения действия.
      var dialogID = Constants.DocumentReviewTask.ReviewDraftResolutionAssignmentConfirmDialogID.ForExecution;
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            dialogID))
        e.Cancel();
      
      RecordManagement.Functions.DocumentReviewTask.Remote.StartActionItemsForDraftResolution(DocumentReviewTasks.As(_obj.Task), _obj);
    }

    public virtual bool CanForExecution(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null && Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
    }

  }

}
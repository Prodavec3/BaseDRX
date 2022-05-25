using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewTaskActions
  {
    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var error = Docflow.PublicFunctions.Module.Remote.GetTaskAbortingError(_obj, Docflow.PublicConstants.Module.TaskMainGroup.DocumentReviewTask.ToString());
      if (!string.IsNullOrWhiteSpace(error))
      {
        e.AddError(error);
      }
      else
      {
        if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null, Constants.DocumentReviewTask.AbortConfirmDialogID))
          return;
        
        base.Abort(e);
      }
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }

    public virtual void AddResolution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var task = (document == null) ? Functions.Module.Remote.CreateActionItemExecution() : Functions.Module.Remote.CreateActionItemExecution(document);
      var assignee = task.Assignee ?? Users.Current;
      task.MaxDeadline = _obj.Deadline ?? Calendar.Today.AddWorkingDays(assignee, 2);
      task.IsDraftResolution = true;
      foreach (var otherGroupAttachment in _obj.OtherGroup.All)
        task.OtherGroup.All.Add(otherGroupAttachment);
      task.ShowModal();
      if (!task.State.IsInserted)
      {
        var draftActionItem = Functions.Module.Remote.GetActionitemById(task.Id);
        _obj.ResolutionGroup.ActionItemExecutionTasks.Add(draftActionItem);
        _obj.Save();
      }
    }

    public virtual bool CanAddResolution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status.Value == Workflow.Task.Status.Draft;
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!RecordManagement.Functions.DocumentReviewTask.ValidateDocumentReviewTaskStart(_obj, e))
        return;
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.DocumentReviewTask.StartConfirmDialogID))
        return;
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

  }
}
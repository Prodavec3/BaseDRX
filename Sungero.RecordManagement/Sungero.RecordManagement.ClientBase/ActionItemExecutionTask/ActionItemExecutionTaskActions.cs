using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement.Client
{
  internal static class ActionItemExecutionTaskActionItemPartsStaticActions
  {

    public static bool CanAddCompoundActionItemPart(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var task = ActionItemExecutionTasks.As(e.Entity);
      return (task.State.IsInserted || Locks.GetLockInfo(task).IsLockedByMe) && task != null && task.Status == ActionItemExecutionTask.Status.Draft;
    }

    public static void AddCompoundActionItemPart(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var task = ActionItemExecutionTasks.As(e.Entity);
      Functions.ActionItemExecutionTask.FillCompoundActionItemPart(task, null);
    }
  }

  partial class ActionItemExecutionTaskActionItemPartsActions
  {

    public virtual bool CanChangeCompoundActionItemPart(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return (_obj.ActionItemExecutionTask.State.IsInserted || Locks.GetLockInfo(_obj.ActionItemExecutionTask).IsLockedByMe) &&
        _obj.ActionItemExecutionTask.Status == ActionItemExecutionTask.Status.Draft || Functions.ActionItemExecutionTask.CanChangeActionItem(_obj.ActionItemExecutionTask);
    }

    public virtual void ChangeCompoundActionItemPart(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      if (_obj.ActionItemExecutionTask.Status == ActionItemExecutionTask.Status.Draft)
      {
        Functions.ActionItemExecutionTask.FillCompoundActionItemPart(_obj.ActionItemExecutionTask, _obj);
      }
      else
      {
        var errorMessage = Functions.ActionItemExecutionTask.Remote.CheckActionItemPartEditBeforeDialog(_obj.ActionItemExecutionTask,
                                                                                                        _obj.ActionItemPartExecutionTask);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
          Dialogs.ShowMessage(errorMessage, MessageType.Warning);
          return;
        }
        
        Functions.ActionItemExecutionTask.ChangeCompoundActionItemPart(_obj.ActionItemExecutionTask, _obj);
      }
    }
  }

  internal static class ActionItemExecutionTaskStaticActions
  {
    public static void FollowUpExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Reports.GetActionItemsExecutionReport().Open();
    }

    public static bool CanFollowUpExecution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Reports.GetActionItemsExecutionReport().CanExecute();
    }
  }

  partial class ActionItemExecutionTaskActions
  {

    public virtual void ChangeActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var errorMessage = Functions.ActionItemExecutionTask.Remote.CheckActionItemEditBeforeDialog(_obj);
      if (!string.IsNullOrWhiteSpace(errorMessage))
      {
        Dialogs.ShowMessage(errorMessage, MessageType.Warning);
        return;
      }
      
      var actionItemChangeSuccessfullyStarted = _obj.IsCompoundActionItem == true ?
        Functions.ActionItemExecutionTask.ChangeCompoundActionItem(_obj) :
        Functions.ActionItemExecutionTask.ChangeSimpleActionItem(_obj);
      if (actionItemChangeSuccessfullyStarted)
        e.CloseFormAfterAction = true;
    }

    public virtual bool CanChangeActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.ActionItemExecutionTask.CanChangeActionItem(_obj);
    }

    public virtual void ChangeCompoundMode(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsCompoundActionItem == true)
      {
        if (_obj.ActionItemParts.Count(a => a.Assignee != null) > 1 || _obj.ActionItemParts.Any(a => a.Deadline != null || !string.IsNullOrEmpty(a.ActionItemPart)))
        {
          var dialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.ChangeCompoundModeQuestion,
                                                ActionItemExecutionTasks.Resources.ChangeCompoundModeDescription,
                                                MessageType.Question);
          dialog.Buttons.AddYesNo();
          dialog.Buttons.Default = DialogButtons.No;
          var yesResult = dialog.Show() == DialogButtons.Yes;
          if (yesResult)
            _obj.IsCompoundActionItem = false;
        }
        else
          _obj.IsCompoundActionItem = false;
      }
      else
        _obj.IsCompoundActionItem = true;
    }

    public virtual bool CanChangeCompoundMode(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (_obj.State.IsInserted || Locks.GetLockInfo(_obj).IsLockedByMe) && _obj.Status == Workflow.Task.Status.Draft;
    }

    public override void Restart(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ActionItemExecutionTask.DisablePropertiesRequirement(_obj);
      base.Restart(e);
    }

    public override bool CanRestart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRestart(e);
    }

    public virtual void AddPerformer(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var recipients = Company.PublicFunctions.Module.GetAllActiveNoSystemGroups();
      var performer = recipients.ShowSelect(ActionItemExecutionTasks.Resources.SelectDepartmentOrRole);
      if (performer != null)
      {
        var error = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.SetRecipientsToAssignees(_obj, performer);
        if (error == ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Constants.ActionItemExecutionTask.MaxCompoundGroup))
          Dialogs.NotifyMessage(error);
      }
    }

    public virtual bool CanAddPerformer(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (_obj.State.IsInserted || Locks.GetLockInfo(_obj).IsLockedByMe) && _obj.IsCompoundActionItem == true && _obj.Status == Workflow.Task.Status.Draft;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCopyEntity(e) && _obj.IsDraftResolution != true;
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). ValidateActionItemExecutionTaskSave.", _obj.Id);
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskSave(_obj, e))
        return;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). ValidateActionItemExecutionTaskStart.", _obj.Id);
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskStart(_obj, e, true))
        return;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). ShowDialogGrantAccessRightsWithConfirmationDialog.", _obj.Id);
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.ActionItemExecutionTask.ActionItemExecutionTaskConfirmDialogID))
        return;
      e.Params.AddOrUpdate(PublicConstants.ActionItemExecutionTask.CheckDeadline, true);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Call base.Start().", _obj.Id);
      base.Start(e);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Ready to start.", _obj.Id);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsDraftResolution == true ? false : base.CanStart(e);
    }

    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(ActionItemExecutionTasks.Resources.Confirmation);
      var abortingReason = dialog.AddMultilineString(_obj.Info.Properties.AbortingReason.LocalizedName, true);
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (string.IsNullOrWhiteSpace(abortingReason.Value))
                                  args.AddError(ActionItemExecutionTasks.Resources.EmptyAbortingReason, abortingReason);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        _obj.AbortingReason = abortingReason.Value;
        Functions.ActionItemExecutionTask.DisablePropertiesRequirement(_obj);
        base.Abort(e);
      }
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && base.CanAbort(e) && _obj.IsDraftResolution != true;
    }

    public virtual void RequireReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Functions.StatusReportRequestTask.Remote.CreateStatusReportRequest(_obj);
      if (task == null)
        e.AddWarning(ActionItemExecutionTasks.Resources.NoActiveChildActionItems);
      else
        task.Show();
    }

    public virtual bool CanRequireReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Task.Status.InProcess &&
        _obj.ExecutionState != RecordManagement.ActionItemExecutionTask.ExecutionState.Executed &&
        _obj.AccessRights.CanUpdate();
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Client
{
  partial class ApprovalRuleBaseConditionsActions
  {
    public virtual bool CanChartConfigCondition(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ChartConfigCondition(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      
    }

    public virtual bool CanChartDeleteCondition(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void ChartDeleteCondition(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var dialog = Dialogs.CreateTaskDialog(ApprovalRuleBases.Resources.ChartDeleteQuestion, ApprovalRuleBases.Resources.ChartDeleteConditionRemark);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      
      if (dialog.Show() == DialogButtons.Yes)
        _all.Remove(_obj);
    }
  }

  partial class ApprovalRuleBaseStagesActions
  {
    
    public virtual bool CanChartSelectStage(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartSelectStage(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      IApprovalStageBase stage;
      if (_obj.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function)
        stage = Functions.ApprovalRuleBase.Remote.ChartSelectFunctionStageBase().ShowSelect();
      else 
        stage = Functions.ApprovalRuleBase.Remote.ChartSelectStage(_obj.StageType).ShowSelect();
      
      if (stage != null)
        _obj.StageBase = stage;
    }

    public virtual bool CanChartConfigStage(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.Stage != null || _obj.StageBase != null;
    }

    public virtual void ChartConfigStage(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      if (_obj.Stage != null)
      {
        _obj.Stage.State.Properties.StageType.IsEnabled = false;
        _obj.Stage.Show();
      }
      else
        _obj.StageBase.Show();
      
    }
    
    public virtual bool CanChartDeleteStage(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartDeleteStage(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      if (_obj.StageBase == null)
      {
        _all.Remove(_obj);
        return;
      }
      
      var dialog = Dialogs.CreateTaskDialog(ApprovalRuleBases.Resources.ChartDeleteQuestion);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      
      if (dialog.Show() == DialogButtons.Yes)
        _all.Remove(_obj);
    }
  }

  internal static class ApprovalRuleBaseStaticActions
  {

    public static bool CanShowApprovalRulesConsolidatedReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public static void ShowApprovalRulesConsolidatedReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Reports.GetApprovalRulesConsolidatedReport().Open();
    }
  }
  
  partial class ApprovalRuleBaseActions
  {
    public virtual void ChartAddFunctionStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalRuleBaseStages.StageType.Function;
    }

    public virtual bool CanChartAddFunctionStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void PrintApprovalRule(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetApprovalRuleCardReport();
      report.ApprovalRule = _obj;
      report.Open();
    }

    public virtual bool CanPrintApprovalRule(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged;
    }

    public virtual void ShowAllVersions(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var rules = Functions.ApprovalRuleBase.Remote.GetAllRuleVersions(_obj);
      
      rules.Show();
    }

    public virtual bool CanShowAllVersions(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void DoClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var message = e.Action.ConfirmationMessage;
      var description = ((Domain.Shared.IInternalActionInfo)e.Action).ActionMetadata.GetConfirmationDescription();
      
      if (_obj.Status == Status.Draft)
        description = ApprovalRuleBases.Resources.Action_DoCloseConfirmationDescription;
      
      var dialog = Dialogs.CreateTaskDialog(message, string.IsNullOrEmpty(description) ? null : description, MessageType.Question);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      if (dialog.Show() == DialogButtons.Yes)
      {
        if (e.Validate())
        {
          _obj.Status = Status.Closed;
          _obj.Save();
        }
      }
    }

    public virtual bool CanDoClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && _obj.Status != Status.Closed && _obj.AccessRights.CanUpdate() && Functions.Module.IsLockedByMe(_obj);
    }

    public virtual void DoActive(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Functions.ApprovalRuleBase.Remote.GetDoubleRules(_obj).Any())
      {
        e.AddWarning(ApprovalRuleBases.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicate);
      }
      
      var message = e.Action.ConfirmationMessage;
      string description = ((Domain.Shared.IInternalActionInfo)e.Action).ActionMetadata.GetConfirmationDescription();
      
      var prevRule = Functions.ApprovalRuleBase.Remote.GetPreviousActiveRule(_obj);
      
      if (prevRule != null)
        description = string.Format("{0}{1}{2}", description, Environment.NewLine, ApprovalRuleBases.Resources.ClosePreviousRule);
      
      var dialog = Dialogs.CreateTaskDialog(message, string.IsNullOrEmpty(description) ? null : description, MessageType.Question);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      
      if (dialog.Show() == DialogButtons.Yes)
      {
        var prevStatus = _obj.Status;
        _obj.Status = Status.Active;
        try
        {
          _obj.Save();
        }
        catch
        {
          _obj.Status = prevStatus;
          throw;
        }
        Dialogs.NotifyMessage(ApprovalRuleBases.Resources.ApprovalRuleSetActive);
      }
    }

    public virtual bool CanDoActive(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.Draft && _obj.AccessRights.CanUpdate() && Functions.Module.IsLockedByMe(_obj);
    }

    public virtual void CreateVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Functions.ApprovalRuleBase.Remote.GetNextVersion(_obj) != null)
      {
        e.AddWarning(ApprovalRuleBases.Resources.NewVersionNotAllowed, _obj.Info.Actions.ShowAllVersions);
        return;
      }

      var message = e.Action.ConfirmationMessage;
      var description = (_obj.Status == ApprovalRuleBase.Status.Closed)
        ? ApprovalRuleBases.Resources.Action_CreateVersionConfirmationDescriptionStatusClosed
        : ApprovalRuleBases.Resources.Action_CreateVersionConfirmationDescriptionStatusNotClosed;
      var dialog = Dialogs.CreateTaskDialog(message, string.IsNullOrEmpty(description) ? null : description, MessageType.Question);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      if (dialog.Show() == DialogButtons.Yes)
      {
        var version = Functions.ApprovalRuleBase.Remote.GetOrCreateNextVersion(_obj);
        e.CloseFormAfterAction = true;
        version.Show();
      }
    }

    public virtual bool CanCreateVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && !_obj.State.IsInserted && _obj.Status != Status.Draft;
    }

    public virtual void TakeScreenshot(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.State.Controls.FlowChart.TakeScreenshot();
    }

    public virtual bool CanTakeScreenshot(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.State.Controls.FlowChart.IsLoaded;
    }

    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SaveAndClose(e);
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }
    
    public virtual void ChartAddCondition(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var condition = _obj.Conditions.AddNew();
    }

    public virtual bool CanChartAddCondition(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ChartAddExecutionStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Execution;
    }

    public virtual bool CanChartAddExecutionStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddReviewStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Review;
    }

    public virtual bool CanChartAddReviewStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddNoticeStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Notice;
    }

    public virtual bool CanChartAddNoticeStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddControlReturnStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.CheckReturn;
    }

    public virtual bool CanChartAddControlReturnStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddPrintStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Print;
    }

    public virtual bool CanChartAddPrintStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddSimpleAgrStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.SimpleAgr;
    }

    public virtual bool CanChartAddSimpleAgrStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddSendingStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Sending;
    }

    public virtual bool CanChartAddSendingStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddRegisterStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Register;
    }

    public virtual bool CanChartAddRegisterStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddApproversStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Approvers;
    }

    public virtual bool CanChartAddApproversStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddManagerStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Manager;
    }

    public virtual bool CanChartAddManagerStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ChartAddSignStage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stage = _obj.Stages.AddNew();
      stage.StageType = Docflow.ApprovalStage.StageType.Sign;
    }

    public virtual bool CanChartAddSignStage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      bool canEditSchema;
      return e.Params.TryGetValue(Constants.ApprovalRuleBase.CanEditSchema, out canEditSchema) ? canEditSchema : true;
    }

    public virtual void ShowDuplicate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.ApprovalRuleBase.Remote.GetDoubleRules(_obj);
      
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(ApprovalRuleBases.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowActiveTasks(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalRuleBase.Remote.GetTasksInProcess(_obj).Show();
    }

    public virtual bool CanShowActiveTasks(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}
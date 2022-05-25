using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;

namespace Sungero.Docflow
{

  partial class ApprovalRuleBaseClientHandlers
  {
    public virtual void ReworkDeadlineValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue <= 0)
        e.AddError(Sungero.Docflow.ApprovalRuleBases.Resources.IncorrectReworkDeadline);
      
      if (e.NewValue.HasValue && e.NewValue > Docflow.Constants.ApprovalRuleBase.MaxReworkDeadline)
        e.AddError(Sungero.Docflow.ApprovalRuleBases.Resources.IncorrectMaxReworkDeadlineFormat(Docflow.Constants.ApprovalRuleBase.MaxReworkDeadline));
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var hintsInfo = 0;

      if (!e.Params.TryGetValue(Constants.ApprovalRuleBase.HintsInfoParam, out hintsInfo))
      {
        hintsInfo = Functions.ApprovalRuleBase.Remote.CanRegisterAndHasTaskInProcess(_obj);
        e.Params.Add(Constants.ApprovalRuleBase.HintsInfoParam, hintsInfo);
      }
      
      var сanRegister = Constants.ApprovalRuleBase.HintMask.CanRegister == (hintsInfo & Constants.ApprovalRuleBase.HintMask.CanRegister);
      var hasTasksInProcess = Constants.ApprovalRuleBase.HintMask.HasTaskInProcess == (hintsInfo & Constants.ApprovalRuleBase.HintMask.HasTaskInProcess);
      
      _obj.State.Controls.FlowChart.IsEnabled = !hasTasksInProcess;
      
      if (!_obj.State.Controls.FlowChart.IsInitialized)
      {
        _obj.State.Controls.FlowChart.Blocks.Bind(_obj.Stages,
                                                  _obj.Info.Properties.Stages.Properties.Number,
                                                  FlowchartBlockType.Process,
                                                  Docflow.Client.ApprovalRuleBaseFunctions.GetStageIcon,
                                                  Docflow.Client.ApprovalRuleBaseFunctions.GetPlaceHolder);
        _obj.State.Controls.FlowChart.Blocks.Bind(_obj.Conditions, _obj.Info.Properties.Conditions.Properties.Number, FlowchartBlockType.Condition);
        
        _obj.State.Controls.FlowChart.Edges.Bind(_obj.Transitions,
                                                 _obj.Info.Properties.Transitions.Properties.SourceStage,
                                                 _obj.Info.Properties.Transitions.Properties.TargetStage,
                                                 _obj.Info.Properties.Transitions.Properties.ConditionValue);
        
        _obj.State.Controls.FlowChart.Blocks.Actions.Add(_obj.Info.Actions.ChartConfigStage);
        _obj.State.Controls.FlowChart.Blocks.Actions.Add(_obj.Info.Actions.ChartConfigCondition);
        
        // Если нет прав на изменение или правило заблокировано другим пользователем
        // или по правилу есть задачи в работе, то добавлять действия не надо.
        if (_obj.AccessRights.CanUpdate() &&
            (_obj.State.IsInserted || (Locks.GetLockInfo(_obj) != null && Locks.GetLockInfo(_obj).IsLockedByMe)) &&
            !hasTasksInProcess)
        {
          _obj.State.Controls.FlowChart.Blocks.Actions.Add(_obj.Info.Actions.ChartSelectStage);
          _obj.State.Controls.FlowChart.Blocks.Actions.Add(_obj.Info.Actions.ChartDeleteStage);
          _obj.State.Controls.FlowChart.Blocks.Actions.Add(_obj.Info.Actions.ChartDeleteCondition);
          
          _obj.State.Controls.FlowChart.Actions.Add(ApprovalRuleBases.Resources.ChartGroupConditions, FlowchartBlockType.Condition, _obj.Info.Actions.ChartAddCondition);
          
          _obj.State.Controls.FlowChart.Actions.Add(ApprovalRuleBases.Resources.ChartGroupApproval,
                                                    _obj.Info.Actions.ChartAddManagerStage,
                                                    _obj.Info.Actions.ChartAddApproversStage,
                                                    _obj.Info.Actions.ChartAddSignStage,
                                                    _obj.Info.Actions.ChartAddReviewStage);
          
          _obj.State.Controls.FlowChart.Actions.Add(ApprovalRuleBases.Resources.ChartGroupDocumentHandling,
                                                    _obj.Info.Actions.ChartAddPrintStage,
                                                    _obj.Info.Actions.ChartAddRegisterStage,
                                                    _obj.Info.Actions.ChartAddExecutionStage,
                                                    _obj.Info.Actions.ChartAddSimpleAgrStage,
                                                    _obj.Info.Actions.ChartAddNoticeStage,
                                                   _obj.Info.Actions.ChartAddFunctionStage);
          
          _obj.State.Controls.FlowChart.Actions.Add(ApprovalRuleBases.Resources.ChartGroupCounterpartyEndorsing,
                                                    _obj.Info.Actions.ChartAddSendingStage,
                                                    _obj.Info.Actions.ChartAddControlReturnStage);        
          
        }
      }
      
      if (_obj.ParentRule != null)
        _obj.State.Properties.DocumentFlow.IsEnabled = false;
      
      if (_obj.Status == ApprovalRuleBase.Status.Closed)
        _obj.State.Properties.Name.IsEnabled = false;

      Functions.ApprovalRuleBase.SetStateProperties(_obj);
      
      // Если нет прав на изменение, то хинты выводить не надо.
      if (!_obj.AccessRights.CanUpdate())
        return;
      
      if (!сanRegister)
        e.AddWarning(ApprovalRuleBases.Resources.CantRegisterAllDocumentKinds);
      
      if (!(_obj.State.IsInserted || _obj.State.IsCopied) && hasTasksInProcess)
      {
        e.AddInformation(ApprovalRuleBases.Resources.RuleHasTasksInProcess, _obj.Info.Actions.CreateVersion);
        _obj.State.Properties.Stages.IsEnabled = false;
        _obj.State.Properties.Conditions.IsEnabled = false;
        _obj.State.Properties.Transitions.IsEnabled = false;
        _obj.State.Properties.DocumentFlow.IsEnabled = false;
        e.Params.AddOrUpdate(Constants.ApprovalRuleBase.CanEditSchema, false);
      }
    }
  }
}
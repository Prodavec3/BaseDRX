using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;

namespace Sungero.Docflow.Client
{
  partial class ApprovalRuleBaseFunctions
  {
    
    /// <summary>
    /// Получить отображаемый текст по умолчанию для этапа согласования.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <returns>Отображаемый текст по умолчанию.</returns>
    public static string GetPlaceHolder(IApprovalRuleBaseStages stage)
    {
      if (stage == null || stage.StageType == null)
        return null;
      
      return ApprovalRuleBases.Info.Properties.Stages.Properties.StageType.GetLocalizedValue(stage.StageType.Value);
    }
    
    /// <summary>
    /// Получить иконку для этапа согласования.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <returns>Иконка.</returns>
    public static Sungero.Core.IIconInfo GetStageIcon(IApprovalRuleBaseStages stage)
    {
      if (stage == null || stage.StageType == null)
        return null;
      
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Approvers)
        return ApprovalRuleBases.Info.Actions.ChartAddApproversStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.CheckReturn)
        return ApprovalRuleBases.Info.Actions.ChartAddControlReturnStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Execution)
        return ApprovalRuleBases.Info.Actions.ChartAddExecutionStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Manager)
        return ApprovalRuleBases.Info.Actions.ChartAddManagerStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Notice)
        return ApprovalRuleBases.Info.Actions.ChartAddNoticeStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Print)
        return ApprovalRuleBases.Info.Actions.ChartAddPrintStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Register)
        return ApprovalRuleBases.Info.Actions.ChartAddRegisterStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Review)
        return ApprovalRuleBases.Info.Actions.ChartAddReviewStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Sending)
        return ApprovalRuleBases.Info.Actions.ChartAddSendingStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Sign)
        return ApprovalRuleBases.Info.Actions.ChartAddSignStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.SimpleAgr)
        return ApprovalRuleBases.Info.Actions.ChartAddSimpleAgrStage.LargeIcon;
      if (stage.StageType == Sungero.Docflow.ApprovalRuleBaseStages.StageType.Function)
        return ApprovalRuleBases.Info.Actions.ChartAddFunctionStage.LargeIcon;

      return null;
    }
    
  }
}
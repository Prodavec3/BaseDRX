using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStageBase;

namespace Sungero.Docflow.Client
{
  partial class ApprovalStageBaseFunctions
  {

    /// <summary>
    /// Показать предупреждение о редактировании карточки этапа.
    /// </summary>
    /// <param name="e">Аргументы события "Обновление формы".</param>
    public void ShowEditWarning(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!(_obj.State.IsInserted || _obj.State.IsCopied) && _obj.AccessRights.CanUpdate())
      {
        bool hasRules;
        // HACK, BUG 208989 - параметры HasRules, ChangeRequisites исчезают при повторном вызове функции, если добавлены через e.Params.
        var approvalStageParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
        if (!approvalStageParams.ContainsKey(Sungero.Docflow.Constants.ApprovalStage.HasRules))
          approvalStageParams.Add(Sungero.Docflow.Constants.ApprovalStage.HasRules, Functions.ApprovalStageBase.Remote.HasRules(_obj));
        object paramValue;
        if (approvalStageParams.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.HasRules, out paramValue) && 
            bool.TryParse(paramValue.ToString(), out hasRules) && hasRules)
        {
          foreach (var property in _obj.State.Properties)
          {
            property.IsEnabled = false;
          }
          e.AddInformation(ApprovalStages.Resources.DisableStageProperties, _obj.Info.Actions.ChangeRequisites);
        }
        
        bool changeRequisites;
        object changeRequisitesParamValue;
        if (approvalStageParams.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites, out changeRequisitesParamValue) && 
            bool.TryParse(changeRequisitesParamValue.ToString(), out changeRequisites) && changeRequisites)
          e.AddInformation(ApprovalStages.Resources.StageHasRules, _obj.Info.Actions.GetApprovalRules);
      }
    }

  }
}
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
        if (!e.Params.Contains(Sungero.Docflow.Constants.ApprovalStage.HasRules))
          e.Params.Add(Sungero.Docflow.Constants.ApprovalStage.HasRules, Functions.ApprovalStageBase.Remote.HasRules(_obj));

        if (e.Params.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.HasRules, out hasRules) && hasRules)
        {
          foreach (var property in _obj.State.Properties)
          {
            property.IsEnabled = false;
          }
          e.AddInformation(ApprovalStages.Resources.DisableStageProperties, _obj.Info.Actions.ChangeRequisites);
        }
        
        bool changeRequisites;
        if (e.Params.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites, out changeRequisites) && changeRequisites)
          e.AddInformation(ApprovalStages.Resources.StageHasRules, _obj.Info.Actions.GetApprovalRules);
      }
    }

  }
}
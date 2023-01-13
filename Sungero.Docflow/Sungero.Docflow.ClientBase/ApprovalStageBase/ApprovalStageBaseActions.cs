using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStageBase;

namespace Sungero.Docflow.Client
{
  partial class ApprovalStageBaseActions
  {
    public virtual void ChangeRequisites(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Открыть поля для изменения этапа согласования, если он используется в правиле.
      if (Functions.ApprovalStageBase.Remote.HasRules(_obj))
      {
        foreach (var property in _obj.State.Properties)
        {
          property.IsEnabled = true;
        }
        
        // Очистить признак используемости в правилах.
        // HACK, BUG 208989
        var approvalStageParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
        approvalStageParams[Sungero.Docflow.Constants.ApprovalStage.HasRules] = false;
        approvalStageParams[Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites] = true;
        // HACK, BUG 28505
        ((Domain.Shared.Validation.IValidationObject)_obj).ValidationResult.Clear();
      }
    }

    public virtual bool CanChangeRequisites(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void GetApprovalRules(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var rules = Functions.ApprovalStageBase.Remote.GetApprovalRules(_obj);
      rules.Show();
    }

    public virtual bool CanGetApprovalRules(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}
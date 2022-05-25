using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow.Client
{
  partial class ApprovalStageActions
  {
    public override void ChangeRequisites(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeRequisites(e);

      if (Functions.ApprovalStageBase.Remote.HasRules(_obj))
      {
        // Тип этапа и признак доп. согласующих оставить недоступными для редактирования.
        _obj.State.Properties.StageType.IsEnabled = false;
        _obj.State.Properties.AllowAdditionalApprovers.IsEnabled = false;
      }
    }

    public override bool CanChangeRequisites(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanChangeRequisites(e);
    }

    public virtual void GetApprovalRulesWithImpossibleRoles(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalStage.Remote.GetRulesWithImpossibleRoles(_obj).Show();
    }

    public virtual bool CanGetApprovalRulesWithImpossibleRoles(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
}
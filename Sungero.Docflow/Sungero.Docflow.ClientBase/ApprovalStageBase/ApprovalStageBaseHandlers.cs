using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStageBase;

namespace Sungero.Docflow
{
  partial class ApprovalStageBaseClientHandlers
  {

    public virtual void DeadlineInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStageBases.Resources.IncorrectDayDeadline);
    }

    public virtual void DeadlineInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStageBases.Resources.IncorrectDayDeadline);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ApprovalStageBase.ShowEditWarning(_obj, e);
    }

  }
}
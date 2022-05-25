using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalFunctionStageBase;

namespace Sungero.Docflow
{
  partial class ApprovalFunctionStageBaseClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      _obj.State.Properties.TimeoutInDays.IsRequired = !_obj.TimeoutInHours.HasValue;
      _obj.State.Properties.TimeoutInHours.IsRequired = !_obj.TimeoutInDays.HasValue;
    }

    public virtual void TimeoutInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.Docflow.ApprovalFunctionStageBases.Resources.IncorrectTimeout);
      
      if ((e.NewValue ?? 0) > Docflow.Constants.ApprovalFunctionStageBase.MaxTimeoutValue)
        e.AddError(Sungero.Docflow.ApprovalFunctionStageBases.Resources.MaxTimeoutInHoursErrorFormat(Docflow.Constants.ApprovalFunctionStageBase.MaxTimeoutValue));
    }

    public virtual void TimeoutInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.Docflow.ApprovalFunctionStageBases.Resources.IncorrectTimeout);
      
      if ((e.NewValue ?? 0) > Docflow.Constants.ApprovalFunctionStageBase.MaxTimeoutValue)
        e.AddError(Sungero.Docflow.ApprovalFunctionStageBases.Resources.MaxTimeoutInDaysErrorFormat(Docflow.Constants.ApprovalFunctionStageBase.MaxTimeoutValue));
    }

  }
}
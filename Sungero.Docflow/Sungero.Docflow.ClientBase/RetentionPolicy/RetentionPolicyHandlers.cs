using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RetentionPolicy;

namespace Sungero.Docflow
{
  partial class RetentionPolicyClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.RetentionPolicy.SetRequiredProperties(_obj);
    }

    public virtual void IntervalValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(Sungero.Docflow.RetentionPolicies.Resources.IncorrectIntervalValue);
      else if (e.NewValue >= Constants.RetentionPolicy.IntervalMaxValue)
        e.AddError(Sungero.Docflow.RetentionPolicies.Resources.IntervalTooMatchFormat(Constants.RetentionPolicy.IntervalMaxValue.ToString("N0")));
    }

    public virtual void DaysToMoveValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(Sungero.Docflow.RetentionPolicies.Resources.IncorrectDays);
      else if (e.NewValue >= Constants.RetentionPolicy.DaysToMoveMaxValue)
        e.AddError(Sungero.Docflow.RetentionPolicies.Resources.DaysTooMatchFormat(Constants.RetentionPolicy.DaysToMoveMaxValue.ToString("N0")));
    }
  }
}
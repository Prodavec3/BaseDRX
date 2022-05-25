using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalFunctionStageBase;

namespace Sungero.Docflow
{
  partial class ApprovalFunctionStageBaseSharedHandlers
  {

    public virtual void TimeoutInHoursChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.TimeoutInDays.IsRequired = !e.NewValue.HasValue;
    }

    public virtual void TimeoutInDaysChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.TimeoutInHours.IsRequired = !e.NewValue.HasValue;
    }

  }
}
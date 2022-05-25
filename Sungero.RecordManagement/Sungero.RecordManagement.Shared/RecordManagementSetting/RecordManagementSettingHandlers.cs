using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.RecordManagementSetting;

namespace Sungero.RecordManagement
{
  partial class RecordManagementSettingSharedHandlers
  {

    public virtual void ControlRelativeDeadlineInDaysChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.ControlRelativeDeadlineInHours.IsRequired = !e.NewValue.HasValue;
    }

    public virtual void ControlRelativeDeadlineInHoursChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.ControlRelativeDeadlineInDays.IsRequired = !e.NewValue.HasValue;
    }

  }
}
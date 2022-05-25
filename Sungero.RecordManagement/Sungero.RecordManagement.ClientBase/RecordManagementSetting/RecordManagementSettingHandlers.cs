using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.RecordManagementSetting;

namespace Sungero.RecordManagement
{
  partial class RecordManagementSettingClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.ControlRelativeDeadlineInDays.IsRequired = !_obj.ControlRelativeDeadlineInHours.HasValue;
      _obj.State.Properties.ControlRelativeDeadlineInHours.IsRequired = !_obj.ControlRelativeDeadlineInDays.HasValue;
    }

    public virtual void ControlRelativeDeadlineInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectAssignmentDeadline);
      
      if (e.NewValue.HasValue && e.NewValue > Sungero.RecordManagement.Constants.RecordManagementSetting.MaxDeadline)
        e.AddError(Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectAssignmentMaxDeadlineInHoursFormat(Sungero.RecordManagement.Constants.RecordManagementSetting.MaxDeadline));
    }

    public virtual void ControlRelativeDeadlineInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectAssignmentDeadline);
      
      if (e.NewValue.HasValue && e.NewValue > Sungero.RecordManagement.Constants.RecordManagementSetting.MaxDeadline)
        e.AddError(Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectAssignmentMaxDeadlineInDaysFormat(Sungero.RecordManagement.Constants.RecordManagementSetting.MaxDeadline));
    }

  }
}
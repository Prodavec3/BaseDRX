using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.RecordManagementSetting;

namespace Sungero.RecordManagement
{
  partial class RecordManagementSettingServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if ((_obj.ControlRelativeDeadlineInDays ?? 0) + (_obj.ControlRelativeDeadlineInHours ?? 0) == 0 && e.IsValid)
      {
        e.AddError(_obj.Info.Properties.ControlRelativeDeadlineInDays, Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectHoursAssignmentDeadline, 
                   new[] { _obj.Info.Properties.ControlRelativeDeadlineInDays, _obj.Info.Properties.ControlRelativeDeadlineInDays });
        e.AddError(_obj.Info.Properties.ControlRelativeDeadlineInHours, Sungero.RecordManagement.RecordManagementSettings.Resources.IncorrectHoursAssignmentDeadline, 
                   new[] { _obj.Info.Properties.ControlRelativeDeadlineInHours, _obj.Info.Properties.ControlRelativeDeadlineInHours });
      }
    }
  }
}
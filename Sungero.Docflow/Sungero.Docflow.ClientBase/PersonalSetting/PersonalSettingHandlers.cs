using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow
{
  partial class PersonalSettingClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var properties = _obj.State.Properties;
      properties.Supervisor.IsEnabled = !(_obj.IsAutoCalcSupervisor ?? false);
      properties.ResolutionAuthor.IsEnabled = !(_obj.IsAutoCalcResolutionAuthor ?? false);
      
      properties.IsAutoExecLeadingActionItem.IsEnabled = Functions.PersonalSetting.CanAutoExecLeadingActionItem(_obj,
                                                                                                                _obj.FollowUpActionItem,
                                                                                                                _obj.Supervisor);
    }
  }
}
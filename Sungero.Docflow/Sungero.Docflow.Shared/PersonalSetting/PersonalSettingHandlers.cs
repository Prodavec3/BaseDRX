using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow
{
  partial class PersonalSettingSharedHandlers
  {

    public virtual void FollowUpActionItemChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Functions.PersonalSetting.ChangeIsAutoExecLeadingActionItemAccess(_obj, e.NewValue, _obj.Supervisor);
    }

    public virtual void SupervisorChanged(Sungero.Docflow.Shared.PersonalSettingSupervisorChangedEventArgs e)
    {
      Functions.PersonalSetting.ChangeIsAutoExecLeadingActionItemAccess(_obj, _obj.FollowUpActionItem, e.NewValue);
    }

    public virtual void IsAutoCalcResolutionAuthorChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.ResolutionAuthor.IsEnabled = !(e.NewValue ?? false);
      if (!_obj.State.Properties.ResolutionAuthor.IsEnabled)
        _obj.ResolutionAuthor = null;
    }

    public virtual void IsAutoCalcSupervisorChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = !(e.NewValue ?? false);
      if (!_obj.State.Properties.Supervisor.IsEnabled)
        _obj.Supervisor = null;
      
      Functions.PersonalSetting.ChangeIsAutoExecLeadingActionItemAccess(_obj, _obj.FollowUpActionItem, _obj.Supervisor);
    }

    public virtual void EmployeeChanged(Sungero.Docflow.Shared.PersonalSettingEmployeeChangedEventArgs e)
    {
      _obj.Name = e.NewValue != null ? e.NewValue.Name : null;
    }
  }
}
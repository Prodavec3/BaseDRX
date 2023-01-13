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

    public virtual void BottomIndentValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue.Value < 0)
        e.AddError(Docflow.Resources.RegistrationStampCoordsMustBePositive);
    }

    public virtual void RightIndentValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue.Value < 0)
        e.AddError(Docflow.Resources.RegistrationStampCoordsMustBePositive);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.PersonalSetting.ChangeRegistrationStampCoordsVisibility(_obj, _obj.RegistrationStampPosition);
    }

    public virtual void RegistrationStampPositionValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      Functions.PersonalSetting.ChangeRegistrationStampCoordsVisibility(_obj, e.NewValue);
    }
    
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
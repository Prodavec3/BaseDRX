using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.VisibilitySetting;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class VisibilitySettingClientHandlers
  {

    public virtual void NeedRestrictVisibilityValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (e.NewValue == true)
        e.AddWarning(Sungero.Company.VisibilitySettings.Resources.OnlyWebWarning);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.VisibilitySetting.SetRequiredProperties(_obj);
      if (_obj.NeedRestrictVisibility == true)
        e.AddWarning(Sungero.Company.VisibilitySettings.Resources.OnlyWebWarning);
    }

  }
}
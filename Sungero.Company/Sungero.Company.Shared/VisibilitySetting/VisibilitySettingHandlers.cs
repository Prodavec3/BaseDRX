using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.VisibilitySetting;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class VisibilitySettingSharedHandlers
  {

    public virtual void NeedRestrictVisibilityChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Functions.VisibilitySetting.SetRequiredProperties(_obj);
    }

  }
}
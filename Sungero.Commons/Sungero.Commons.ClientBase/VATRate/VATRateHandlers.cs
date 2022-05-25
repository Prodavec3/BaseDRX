using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.VATRate;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class VATRateClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_obj.Sid))
      {
        _obj.State.Properties.Name.IsEnabled = false;
        _obj.State.Properties.Rate.IsEnabled = false;
      }
    }

    public virtual void RateValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(VATRates.Resources.RateMustBePositive);      
    }

  }
}
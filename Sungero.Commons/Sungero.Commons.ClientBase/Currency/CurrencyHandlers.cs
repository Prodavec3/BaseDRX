using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Currency;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class CurrencyClientHandlers
  {

    public override void StatusValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      _obj.State.Properties.IsDefault.IsEnabled = e.NewValue != Status.Closed;
    }

    public virtual void NumericCodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Functions.Currency.ValidateNumericCode(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(_obj.Info.Properties.NumericCode, result);
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Country;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class CountryClientHandlers
  {

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Functions.Country.ValidateCountryCode(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(_obj.Info.Properties.Code, result);
    }

  }
}
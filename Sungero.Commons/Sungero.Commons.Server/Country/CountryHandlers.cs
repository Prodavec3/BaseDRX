using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Country;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class CountryServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {     
      // Проверить валидность кода.
      var result = Functions.Country.ValidateCountryCode(_obj.Code);
      if (!string.IsNullOrEmpty(result))
        e.AddError(Countries.Resources.InvalidCountryCode);
      
      if (Functions.Country.HaveDuplicates(_obj))
        e.AddWarning(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
    }

  }
}
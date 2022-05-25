using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.City;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class CitySharedHandlers
  {
    public virtual void CountryChanged(Sungero.Commons.Shared.CityCountryChangedEventArgs e)
    {
      // Очистить регион при изменении страны.
      if (!Equals(e.NewValue, e.OldValue) && _obj.Region != null && !Equals(_obj.Region.Country, e.NewValue))
        _obj.Region = null;
    }

    public virtual void RegionChanged(Sungero.Commons.Shared.CityRegionChangedEventArgs e)
    {
      // Изменить страну в соответствии с регионом.
      if (e.NewValue != null)
        _obj.Country = e.NewValue.Country;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.City;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{

  partial class CityRegionPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> RegionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Отфильтровать регионы по странам.
      if (_obj.Country != null)
        query = query.Where(region => Equals(region.Country, _obj.Country));
      
      return query;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Country;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  partial class CountryFunctions
  {
    /// <summary>
    /// Получить дубли страны.
    /// </summary>
    /// <returns>Страны, дублирующие текущую.</returns>
    [Remote(IsPure = true)]
    public IQueryable<ICountry> GetDuplicates()
    {
      return Countries.GetAll()
        .Where(c => c.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed)
        .Where(c => Equals(c.Code, _obj.Code))
        .Where(c => !Equals(c, _obj));
    }
  }
}
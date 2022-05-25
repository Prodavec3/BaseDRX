using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.City;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  partial class CityFunctions
  {
    /// <summary>
    /// Получить населенный пункт из адреса.
    /// </summary>
    /// <param name="address">Адрес.</param>
    /// <returns>Населенный пункт, указанный в адресе.</returns>
    [Public]
    public static ICity GetCityFromAddress(string address)
    {
      if (string.IsNullOrWhiteSpace(address))
        return null;
      
      var pattern = string.Format(@"(?i:{0}\s(?<cityName>[а-я][а-я-\s\.]+))", Constants.City.AddressTypesMask);
      var match = System.Text.RegularExpressions.Regex.Match(address, pattern);
      
      var cityName = match.Groups["cityName"].Value;
      var city = string.Format("{0} {1}", Constants.City.CityMask, cityName);
      var locality = string.Format("{0} {1}", Constants.City.LocalityMask, cityName);
      
      return !match.Success
        ? null
        : Cities.GetAll().FirstOrDefault(c =>
                                         c.Name.ToLower() == city.ToLower() || c.Name.ToLower() == cityName || 
                                         c.Name.ToLower() == locality.ToLower());
    }
    
  }
}
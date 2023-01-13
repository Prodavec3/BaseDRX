using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Region;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  partial class RegionFunctions
  {
    /// <summary>
    /// Получить регион из адреса.
    /// </summary>
    /// <param name="address">Адрес.</param>
    /// <returns>Регион, указанный в адресе.</returns>
    [Public]
    public static IRegion GetRegionFromAddress(string address)
    {
      if (string.IsNullOrWhiteSpace(address))
        return null;
      
      var pattern = string.Format(@"(?i:(?<pref>{0}\s)?(?<region>[а-я-]+)(?<suf>\s{0})?)", Constants.Region.AddressTypesMask);
      var match = System.Text.RegularExpressions.Regex.Match(address, pattern);
      
      while (match.Success)
      {
        // Нашли республику/округ/край/область.
        if (match.Groups["pref"].Success || match.Groups["suf"].Success)
          return Regions.GetAll().FirstOrDefault(r => r.Name.Contains(match.Groups["region"].Value));
        
        match = match.NextMatch();
      }
      
      return null;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.DueDiligenceWebsite;

namespace Sungero.Parties.Shared
{
  partial class DueDiligenceWebsiteFunctions
  {
    /// <summary>
    /// Проверка URL сайта проверки контрагента на валидность.
    /// </summary>
    /// <param name="url">URL.</param>
    /// <returns>String.Empty, если URL валидный.
    /// Иначе сообщение с ошибкой.</returns>
    [Public]
    public static string CheckDueDiligenceWebsiteUrl(string url)
    {
      if (string.IsNullOrWhiteSpace(url))
        return DueDiligenceWebsites.Resources.ErrorUrlNotSet;

      var urlPattern = @"^((https?|ftp):\/\/)?([\w\.-]+)\.([\w]{2,6})(\/[\w\.@:_\+~#?&\/={}%\-\[\w\]]*)*\/?$";
      if (!System.Text.RegularExpressions.Regex.IsMatch(url, urlPattern))
        return DueDiligenceWebsites.Resources.ErrorInvalidUrl;

      var mask = System.Text.RegularExpressions.Regex.Match(url, @"{.*?}");
      while (mask.Success)
      {
        if (mask.Value.ToUpper() != Constants.DueDiligenceWebsite.Websites.OgrnMask &&
            mask.Value.ToUpper() != Constants.DueDiligenceWebsite.Websites.InnMask)
          return DueDiligenceWebsites.Resources.ErrorInvalidMaskFormat(
            Constants.DueDiligenceWebsite.Websites.OgrnMask,
            Constants.DueDiligenceWebsite.Websites.InnMask);
        
        mask = mask.NextMatch();
      }
      
      return string.Empty;
    }
  }
}
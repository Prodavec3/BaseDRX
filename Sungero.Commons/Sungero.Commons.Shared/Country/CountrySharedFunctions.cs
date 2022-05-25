using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Country;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Shared
{
  partial class CountryFunctions
  {
    /// <summary>
    /// Проверить дубли стран.
    /// </summary>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public bool HaveDuplicates()
    {
      if (string.IsNullOrWhiteSpace(_obj.Code) ||
          Equals(_obj.Code, "000") ||
          _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        return false;
      
      return Functions.Country.Remote.GetDuplicates(_obj).Any();
    }
    
    /// <summary>
    /// Проверить код страны на валидность.
    /// </summary>
    /// <param name="code">Код страны.</param>
    /// <returns>Пустая строка, если код страны валидный, иначе сообщение с ошибкой о невалидности.</returns>
    public static string ValidateCountryCode(string code)
    {
      if (string.IsNullOrEmpty(code) || !System.Text.RegularExpressions.Regex.IsMatch(code, @"(^\d{3}$)"))
        return Countries.Resources.InvalidCountryCode;
      
      return string.Empty;
    }
  }
}
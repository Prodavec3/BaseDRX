using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Currency;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Shared
{
  partial class CurrencyFunctions
  {
    /// <summary>
    /// Проверить дубли валюты.
    /// </summary>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public bool HaveDuplicates()
    {
      if (string.IsNullOrWhiteSpace(_obj.NumericCode) || 
          Equals(_obj.NumericCode, "000") || 
          _obj.Status == Sungero.Commons.Currency.Status.Closed)
        return false;
      
      return Functions.Currency.Remote.GetDuplicates(_obj).Any();
    }
    
    /// <summary>
    /// Проверить код валюты на валидность.
    /// </summary>
    /// <param name="numericCode">Код валюты.</param>
    /// <returns>Пустая строка, если код валидный, иначе сообщение с ошибкой о невалидности.</returns>
    public static string ValidateNumericCode(string numericCode)
    {
      if (string.IsNullOrEmpty(numericCode) || !System.Text.RegularExpressions.Regex.IsMatch(numericCode,  @"(^\d{3}$)"))
        return Currencies.Resources.InvalidNumericCode;
      
      return string.Empty;
    }

  }
}
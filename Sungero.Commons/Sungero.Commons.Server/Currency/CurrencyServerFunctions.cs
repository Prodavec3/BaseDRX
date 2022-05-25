using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Currency;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Server
{
  partial class CurrencyFunctions
  {
    /// <summary>
    /// Получить валюту по умолчанию.
    /// </summary>
    /// <returns>Валюта, используемая по умолчанию.</returns>
    [Remote(IsPure = true), Public]
    public static ICurrency GetDefaultCurrency()
    {
      return Currencies.GetAll().FirstOrDefault(r => r.IsDefault.Value == true);
    }
    
    /// <summary>
    /// Получить дубли валюты.
    /// </summary>
    /// <returns>Валюты, дублирующие текущую.</returns>
    [Remote(IsPure = true)]
    public IQueryable<ICurrency> GetDuplicates()
    {
      return Currencies.GetAll()
        .Where(c => c.Status != Sungero.Commons.Currency.Status.Closed)
        .Where(c => Equals(c.NumericCode, _obj.NumericCode))
        .Where(c => !Equals(c, _obj));
    }
  }
}
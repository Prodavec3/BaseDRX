using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Currency;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class CurrencyServerHandlers
  {
    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!Functions.Module.IsAllExternalEntityLinksDeleted(_obj))
        throw AppliedCodeException.Create(Commons.Resources.HasLinkedExternalEntities);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
      {
        _obj.IsDefault = false;
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var result = Functions.Currency.ValidateNumericCode(_obj.NumericCode);
      if (!string.IsNullOrEmpty(result))
        e.AddError(result);
      
      if (Functions.Currency.HaveDuplicates(_obj))
        e.AddError(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
      
      // Нельзя закрывать валюту по умолчанию.
      if (_obj.Status == Status.Closed && _obj.IsDefault == true)
        e.AddError(Currencies.Resources.ClosedCurrencyCannotBeDefault);
      
      // Если установить для текущей валюты флаг валюты по умолчанию, то с другой валюты этот флаг снимается.
      if (_obj.IsDefault == true)
      {
        var defaultCurrency = Functions.Currency.GetDefaultCurrency();
        if (defaultCurrency != null && defaultCurrency != _obj)
        {
          var lockInfo = Locks.GetLockInfo(defaultCurrency);
          if (lockInfo != null && lockInfo.IsLocked)
          {
            var error = Commons.Resources.LinkedEntityLockedFormat(
              defaultCurrency.Name,
              defaultCurrency.Id,
              lockInfo.OwnerName);
            e.AddError(error);
          }
          
          defaultCurrency.IsDefault = false;
        }
      }
    }
  }
}
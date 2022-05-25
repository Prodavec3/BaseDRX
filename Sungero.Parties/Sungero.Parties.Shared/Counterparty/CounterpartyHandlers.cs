using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties
{
  partial class CounterpartyExchangeBoxesSharedHandlers
  {

    public virtual void ExchangeBoxesIsDefaultChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        foreach (var boxLine in _obj.Counterparty.ExchangeBoxes.Where(x => Equals(_obj.Box, x.Box) && !Equals(x, _obj)))
          boxLine.IsDefault = false;
      }
    }
  }

  partial class CounterpartyExchangeBoxesSharedCollectionHandlers
  {

    public virtual void ExchangeBoxesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (!_added.IsDefault.HasValue)
        _added.IsDefault = false;
    }
  }

  partial class CounterpartySharedHandlers
  {

    public virtual void ExchangeBoxesChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      // Установить свойство Эл. обмен.
      _obj.CanExchange = _obj.ExchangeBoxes.Any(box => box.Status.Equals(Sungero.Parties.CounterpartyExchangeBoxes.Status.Active));
    }

    public virtual void LegalAddressChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Заполнить почтовый адрес в соответствии с юрид. адресом.
      if (!string.IsNullOrWhiteSpace(e.NewValue) &&
          (string.IsNullOrWhiteSpace(_obj.PostalAddress) || _obj.PostalAddress == e.OldValue))
        _obj.PostalAddress = e.NewValue;
    }

    public virtual void RegionChanged(Sungero.Parties.Shared.CounterpartyRegionChangedEventArgs e)
    {
      // Очистить город при смене региона.
      if (!Equals(e.NewValue, e.OldValue) && _obj.City != null && !_obj.City.Region.Equals(e.NewValue))
        _obj.City = null;
    }

    public virtual void CityChanged(Sungero.Parties.Shared.CounterpartyCityChangedEventArgs e)
    {
      // Установить регион в соответствии с городом.
      if (e.NewValue != null)
        _obj.Region = e.NewValue.Region;
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.BusinessUnit;
using Sungero.Company.Shared;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Company
{
  partial class BusinessUnitSharedHandlers
  {

    public virtual void LegalAddressChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Заполнить почтовый адрес в соответствии с юрид. адресом.
      if (e.NewValue != null && _obj.PostalAddress == null)
        _obj.PostalAddress = e.NewValue;
    }

    public override void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Заполнить юрид. наименование по умолчанию наименованием.
      if (e.NewValue != null && _obj.LegalName == null)
        _obj.LegalName = e.NewValue;
    }

    public virtual void RegionChanged(Sungero.Company.Shared.BusinessUnitRegionChangedEventArgs e)
    {
      // Очистить город при смене региона.
      if (!Equals(e.NewValue, e.OldValue) && _obj.City != null && !Equals(_obj.City.Region, e.NewValue))
        _obj.City = null;
    }

    public virtual void CityChanged(Sungero.Company.Shared.BusinessUnitCityChangedEventArgs e)
    {
      // Установить регион в соответствии с городом.
      if (e.NewValue != null)
        _obj.Region = e.NewValue.Region;
    }
  }
}
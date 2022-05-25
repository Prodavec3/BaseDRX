using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class DepartmentSharedHandlers
  {

    public virtual void BusinessUnitChanged(Sungero.Company.Shared.DepartmentBusinessUnitChangedEventArgs e)
    {
      // Очистить головное подразделение при очистке нашей организации.
      if (e.NewValue == null && !Equals(e.NewValue, e.OldValue) && _obj.HeadOffice != null)
        _obj.HeadOffice = null;
      
      if (_obj.HeadOffice != null)
        _obj.Parent = _obj.HeadOffice;
      else
        _obj.Parent = e.NewValue;
    }

    public virtual void HeadOfficeChanged(Sungero.Company.Shared.DepartmentHeadOfficeChangedEventArgs e)
    {
      // Заполнить нашу организацию в соответствии с нашей организацией головного подразделения.
      if (e.NewValue != null && e.NewValue.BusinessUnit != null)
        _obj.BusinessUnit = e.NewValue.BusinessUnit;
      
      if (e.NewValue != null)
        _obj.Parent = e.NewValue;
      else
        _obj.Parent = _obj.BusinessUnit;
    }
  }
}
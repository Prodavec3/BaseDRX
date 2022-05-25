using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorney;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneySharedHandlers
  {

    public virtual void IssuedToChanged(Sungero.Docflow.Shared.PowerOfAttorneyIssuedToChangedEventArgs e)
    {
      this.FillName();
      
      if (e.NewValue != null && _obj.Department == null)
        _obj.Department = e.NewValue.Department;
      if (e.NewValue != null && _obj.BusinessUnit == null)
        _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;      
    }

  }
}
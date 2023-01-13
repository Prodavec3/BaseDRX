using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyBaseSharedHandlers
  {

    public virtual void IssuedToChanged(Sungero.Docflow.Shared.PowerOfAttorneyBaseIssuedToChangedEventArgs e)
    {
      this.FillName();
      
      if (e.NewValue != null && _obj.Department == null)
        _obj.Department = e.NewValue.Department;
      if (e.NewValue != null && _obj.BusinessUnit == null)
        _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;
    }

  }
}
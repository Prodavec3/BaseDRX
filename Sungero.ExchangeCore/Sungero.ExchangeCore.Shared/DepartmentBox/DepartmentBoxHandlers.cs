using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.DepartmentBox;

namespace Sungero.ExchangeCore
{
  partial class DepartmentBoxSharedHandlers
  {

    public virtual void ServiceNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.DepartmentBox.SetDepartmentBoxName(_obj);
    }

    public virtual void ParentBoxChanged(Sungero.ExchangeCore.Shared.DepartmentBoxParentBoxChangedEventArgs e)
    {
      Functions.DepartmentBox.SetDepartmentBoxName(_obj);
    }

    public override void ResponsibleChanged(Sungero.ExchangeCore.Shared.BoxBaseResponsibleChangedEventArgs e)
    {
      base.ResponsibleChanged(e);
      
      if (e.NewValue != null && _obj.Department == null)
        _obj.Department = e.NewValue.Department;
    }

  }
}
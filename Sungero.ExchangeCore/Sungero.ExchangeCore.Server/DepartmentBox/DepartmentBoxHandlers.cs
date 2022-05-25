using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.DepartmentBox;

namespace Sungero.ExchangeCore
{
  partial class DepartmentBoxServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      Functions.DepartmentBox.SetDepartmentBoxName(_obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);

      if (_obj.IsDeleted == null)
        _obj.IsDeleted = false;
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.DepartmentBox;

namespace Sungero.ExchangeCore
{
  partial class DepartmentBoxClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      _obj.State.Properties.Status.IsEnabled = _obj.IsDeleted != true;
    }

  }
}
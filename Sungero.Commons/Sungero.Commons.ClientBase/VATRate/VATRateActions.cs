using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.VATRate;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Client
{
  partial class VATRateActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return string.IsNullOrWhiteSpace(_obj.Sid);
    }

  }

}
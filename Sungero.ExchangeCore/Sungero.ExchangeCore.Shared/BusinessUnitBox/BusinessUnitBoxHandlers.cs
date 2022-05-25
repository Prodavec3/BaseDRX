using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore
{
  partial class BusinessUnitBoxSharedHandlers
  {

    public override void ConnectionStatusChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.ConnectionStatusChanged(e);
      if (!e.Params.Contains(Constants.BoxBase.JobRunned) && !Equals(e.NewValue, e.OldValue) && Equals(e.NewValue, BusinessUnitBox.ConnectionStatus.Connected))
        Functions.Module.Remote.RequeueBoxSync();
    }

    public override void StatusChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.StatusChanged(e);
      Functions.BusinessUnitBox.ResetConnectionStatus(_obj);
    }

    public virtual void LoginChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.BusinessUnitBox.ResetConnectionStatus(_obj);
      _obj.Password = string.Empty;
    }

    public virtual void ExchangeServiceChanged(Sungero.ExchangeCore.Shared.BusinessUnitBoxExchangeServiceChangedEventArgs e)
    {
      Functions.BusinessUnitBox.SetBusinessUnitBoxName(_obj);
      Functions.BusinessUnitBox.ResetConnectionStatus(_obj);
    }

    public virtual void BusinessUnitChanged(Sungero.ExchangeCore.Shared.BusinessUnitBoxBusinessUnitChangedEventArgs e)
    {
      Functions.BusinessUnitBox.SetBusinessUnitBoxName(_obj);
      Functions.BusinessUnitBox.ResetConnectionStatus(_obj);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
        _obj.ExchangeService = Functions.BusinessUnitBox.Remote.GetDefaultExchangeService(_obj);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore
{
  partial class BoxBaseSharedHandlers
  {

    public virtual void DeadlineInHoursChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.DeadlineInDays.IsRequired = !e.NewValue.HasValue;
    }

    public virtual void DeadlineInDaysChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      _obj.State.Properties.DeadlineInHours.IsRequired = !e.NewValue.HasValue;
    }

    public virtual void RoutingChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == Sungero.ExchangeCore.BoxBase.Routing.NoAssignments)
      {
        _obj.State.Properties.DeadlineInDays.IsEnabled = false;
        _obj.State.Properties.DeadlineInHours.IsEnabled = false;       
        _obj.DeadlineInDays = null;
        _obj.DeadlineInHours = null;
      }
      else
      {
        _obj.State.Properties.DeadlineInDays.IsEnabled = true;
        _obj.State.Properties.DeadlineInHours.IsEnabled = true;
        if (_obj.DeadlineInDays == null && _obj.DeadlineInHours == null)
          _obj.DeadlineInHours = Sungero.ExchangeCore.PublicConstants.BoxBase.DefaultDeadlineInHours;
      }
      
      _obj.State.Properties.DeadlineInDays.IsRequired = !_obj.DeadlineInHours.HasValue && e.NewValue != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
      _obj.State.Properties.DeadlineInHours.IsRequired = !_obj.DeadlineInDays.HasValue && e.NewValue != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
    }
  }

}
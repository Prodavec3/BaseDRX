using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore
{
  partial class BoxBaseClientHandlers
  {

    public virtual void DeadlineInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.ExchangeCore.BoxBases.Resources.IncorrectAssignmentDeadline);
      
      if (e.NewValue.HasValue && e.NewValue > Sungero.ExchangeCore.PublicConstants.BoxBase.MaxDeadline)
        e.AddError(Sungero.ExchangeCore.BoxBases.Resources.IncorrectMaxDeadlineInHoursFormat(Sungero.ExchangeCore.PublicConstants.BoxBase.MaxDeadline));
    }

    public virtual void DeadlineInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(Sungero.ExchangeCore.BoxBases.Resources.IncorrectAssignmentDeadline);
      
      if (e.NewValue.HasValue && e.NewValue > Sungero.ExchangeCore.PublicConstants.BoxBase.MaxDeadline)
        e.AddError(Sungero.ExchangeCore.BoxBases.Resources.IncorrectMaxDeadlineInDaysFormat(Sungero.ExchangeCore.PublicConstants.BoxBase.MaxDeadline));
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.DeadlineInDays.IsEnabled = _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
      _obj.State.Properties.DeadlineInHours.IsEnabled = _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
      
      _obj.State.Properties.DeadlineInDays.IsRequired = !_obj.DeadlineInHours.HasValue && _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
      _obj.State.Properties.DeadlineInHours.IsRequired = !_obj.DeadlineInDays.HasValue && _obj.Routing != Sungero.ExchangeCore.BoxBase.Routing.NoAssignments;
    }

  }
}
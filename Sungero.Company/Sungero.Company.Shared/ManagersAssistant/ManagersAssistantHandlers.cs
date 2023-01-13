using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantSharedHandlers
  {

    public virtual void IsAssistantChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true && _obj.SendActionItems != true)
        _obj.SendActionItems = true;
    }

    public virtual void PreparesResolutionChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
        _obj.IsAssistant = true;
    }

  }
}
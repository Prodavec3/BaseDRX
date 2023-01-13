using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantClientHandlers
  {

    public virtual void PreparesResolutionValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      _obj.State.Properties.IsAssistant.IsEnabled = e.NewValue != true;
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
       _obj.State.Properties.IsAssistant.IsEnabled = _obj.PreparesResolution != true;
       _obj.State.Properties.SendActionItems.IsEnabled = _obj.IsAssistant != true;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationGroup;

namespace Sungero.Docflow
{

  partial class RegistrationGroupClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!e.IsValid)
        return;
      
      var isResponsible = _obj.AccessRights.CanUpdate() && Functions.Module.CalculateParams(e, _obj, true, false, false, false, null);
      if (!isResponsible && !_obj.AccessRights.CanManage())
      {
        e.AddInformation(RegistrationGroups.Resources.FormDisable);
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
      }
      
      if (_obj.AccessRights.CanUpdate() && Functions.Module.CalculateParams(e, _obj, false, true, false, false, null))
      {
        _obj.State.Properties.CanRegisterIncoming.IsEnabled = true;
        _obj.State.Properties.CanRegisterOutgoing.IsEnabled = true;
        _obj.State.Properties.CanRegisterInternal.IsEnabled = true;
        _obj.State.Properties.CanRegisterContractual.IsEnabled = true;
      }
      else
        _obj.State.Properties.Status.IsEnabled = false;
    }
  }
}
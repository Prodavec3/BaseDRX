using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow.Client
{
  partial class PowerOfAttorneyBaseActions
  {
    public virtual void FindActiveSignatureSetting(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var signSettings = Functions.PowerOfAttorneyBase.Remote.GetActiveSignatureSettingsByPOA(_obj);
      signSettings.Show();
    }

    public virtual bool CanFindActiveSignatureSetting(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void FindSignatureSetting(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var signSettings = Functions.PowerOfAttorneyBase.Remote.GetSignatureSettingsByPOA(_obj);
      signSettings.Show();
    }

    public virtual bool CanFindSignatureSetting(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }

}
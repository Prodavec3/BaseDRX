using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneyServerHandlers
  {
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.LifeCycleState == LifeCycleState.Active)
      {
        var duplicatesError = Functions.FormalizedPowerOfAttorney.GetDuplicatesErrorText(_obj);
        if (!string.IsNullOrEmpty(duplicatesError))
          e.AddError(duplicatesError, _obj.Info.Actions.ShowDuplicates);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.LifeCycleState = FormalizedPowerOfAttorney.LifeCycleState.Draft;
    }
  }

  partial class FormalizedPowerOfAttorneyCreatingFromServerHandler
  {
    
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.UnifiedRegistrationNumber);
    }
  }

}
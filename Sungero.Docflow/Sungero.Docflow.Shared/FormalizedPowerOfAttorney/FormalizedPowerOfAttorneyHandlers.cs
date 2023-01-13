using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow
{
  partial class FormalizedPowerOfAttorneySharedHandlers
  {

    public override void DepartmentChanged(Sungero.Docflow.Shared.OfficialDocumentDepartmentChangedEventArgs e)
    {
      base.DepartmentChanged(e);
      
      Functions.FormalizedPowerOfAttorney.SetActiveLifeCycleState(_obj);
    }

    public override void BusinessUnitChanged(Sungero.Docflow.Shared.OfficialDocumentBusinessUnitChangedEventArgs e)
    {
      base.BusinessUnitChanged(e);
      
      Functions.FormalizedPowerOfAttorney.SetActiveLifeCycleState(_obj);
    }

    public override void IssuedToChanged(Sungero.Docflow.Shared.PowerOfAttorneyBaseIssuedToChangedEventArgs e)
    {
      base.IssuedToChanged(e);
      
      Functions.FormalizedPowerOfAttorney.SetActiveLifeCycleState(_obj);
    }

    public virtual void UnifiedRegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      this.FillName();
    }

  }
}
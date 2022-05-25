using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorney;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyClientHandlers
  {

    public virtual void ValidTillValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var errorText = Sungero.Docflow.Client.PowerOfAttorneyFunctions.CheckCorrectnessDaysToFinishWorks(e.NewValue, _obj.DaysToFinishWorks);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }
    
    public virtual void DaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(PowerOfAttorneys.Resources.IncorrectReminder);
      
      var errorText = Sungero.Docflow.Client.PowerOfAttorneyFunctions.CheckCorrectnessDaysToFinishWorks(_obj.ValidTill, e.NewValue);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }
  }
}
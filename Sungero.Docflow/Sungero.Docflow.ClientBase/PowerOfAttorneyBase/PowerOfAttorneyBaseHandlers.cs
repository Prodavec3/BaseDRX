using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow
{
  partial class PowerOfAttorneyBaseClientHandlers
  {

    public override void NameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.NameValueInput(e);
      
      // Убрать пробелы в имени, если оно вводится вручную.
      if (_obj.DocumentKind.GenerateDocumentName == false && !string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void DaysToFinishWorksValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(PowerOfAttorneyBases.Resources.IncorrectReminder);
      
      var errorText = Sungero.Docflow.Client.PowerOfAttorneyFunctions.CheckCorrectnessDaysToFinishWorks(_obj.ValidTill, e.NewValue);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }

    public virtual void ValidTillValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var errorText = Sungero.Docflow.Client.PowerOfAttorneyFunctions.CheckCorrectnessDaysToFinishWorks(e.NewValue, _obj.DaysToFinishWorks);
      if (!string.IsNullOrEmpty(errorText))
        e.AddError(errorText);
    }

  }
}
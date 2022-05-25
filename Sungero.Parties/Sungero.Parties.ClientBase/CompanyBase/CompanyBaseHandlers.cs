using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties
{
  partial class CompanyBaseClientHandlers
  {

    public override void NonresidentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      base.NonresidentValueInput(e);
      
      if (e.NewValue != true)
      {
        var errorMessage = Functions.CompanyBase.CheckTRRC(_obj.TRRC);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TRRC, errorMessage);
      }
    }

    public virtual void TRRCValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var errorMessage = Functions.CompanyBase.CheckTRRC(e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(errorMessage);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      if (_obj.IsCardReadOnly == true)
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
    }
  }
}
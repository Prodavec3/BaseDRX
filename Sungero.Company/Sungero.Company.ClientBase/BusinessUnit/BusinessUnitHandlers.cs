using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company.BusinessUnit;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties;

namespace Sungero.Company
{
  partial class BusinessUnitClientHandlers
  {

    public virtual void TRRCValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var errorMessage = Parties.PublicFunctions.CompanyBase.CheckTRRC(e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(errorMessage);
      }
    }

    public virtual void NonresidentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (e.NewValue != true)
      {
        var errorMessage = Parties.PublicFunctions.Counterparty.CheckTin(_obj.TIN, true);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TIN, errorMessage);
        
        errorMessage = Parties.PublicFunctions.CompanyBase.CheckTRRC(_obj.TRRC);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TRRC, errorMessage);

        errorMessage = Functions.BusinessUnit.CheckPsrnLength(_obj, _obj.PSRN);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.PSRN, errorMessage);
        
        errorMessage = Functions.BusinessUnit.CheckNceoLength(_obj, _obj.NCEO);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.NCEO, errorMessage);
      }
    }

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      
      // Использование пробелов в середине кода запрещено.
      var newCode = e.NewValue.Trim();
      if (Regex.IsMatch(newCode, @"\s"))
        e.AddError(Company.Resources.NoSpacesInCode);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.State.IsInserted && _obj.CEO != null)
        e.AddInformation(_obj.Info.Properties.CEO, BusinessUnits.Resources.SingatureSettingWillBeCreated);
    }
    
    public virtual void CEOValueInput(Sungero.Company.Client.BusinessUnitCEOValueInputEventArgs e)
    {
      if (!Equals(e.NewValue, _obj.State.Properties.CEO.OriginalValue))
      {
        var info = e.NewValue != null
          ? BusinessUnits.Resources.SingatureSettingWillBeCreated
          : BusinessUnits.Resources.SingatureSettingWillBeClosed;
        e.AddInformation(info);
      }
    }

    public virtual void AccountValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Parties.PublicFunctions.Counterparty.CheckAccountLength(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(result);
    }

    public virtual void NCEOValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var result = Functions.BusinessUnit.CheckNceoLength(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
    }

    public virtual void PSRNValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var result = Functions.BusinessUnit.CheckPsrnLength(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
    }

    public virtual void TINValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var result = Parties.PublicFunctions.Counterparty.CheckTin(e.NewValue, true);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
    }

    public virtual void EmailValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(e.NewValue) && !Parties.PublicFunctions.Module.EmailIsValid(e.NewValue))
        e.AddWarning(Parties.Resources.WrongEmailFormat);
    }
  }
}
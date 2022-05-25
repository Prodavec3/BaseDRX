using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties
{
  partial class CounterpartyClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      var key = Counterparties.Resources.ParameterSaveFromUIFormat(_obj.Id);
      if (e.Params.Contains(key))
        e.Params.Remove(key);
    }

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      // Использование пробелов в середине кода запрещено.
      var newCode = e.NewValue.Trim();
      if (Regex.IsMatch(newCode, @"\s"))
        e.AddError(Sungero.Company.Resources.NoSpacesInCode);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!_obj.AccessRights.CanChangeCard())
      {
        foreach (var prop in _obj.State.Properties)
          prop.IsEnabled = false;
      }
      e.Params.AddOrUpdate(Counterparties.Resources.ParameterSaveFromUIFormat(_obj.Id), true);
    }

    public virtual void AccountValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var errorMessage = Functions.Counterparty.CheckAccountLength(e.NewValue);
      if (!string.IsNullOrEmpty(errorMessage))
        e.AddError(errorMessage);
    }

    public virtual void NCEOValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var errorMessage = Functions.Counterparty.CheckNceoLength(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(errorMessage);
      }
    }

    public virtual void PSRNValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var errorMessage = Functions.Counterparty.CheckPsrnLength(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(errorMessage);
      }
    }

    public virtual void NonresidentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (e.NewValue != true)
      {
        var errorMessage = Functions.Counterparty.CheckTin(_obj, _obj.TIN);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TIN, errorMessage);

        errorMessage = Functions.Counterparty.CheckPsrnLength(_obj, _obj.PSRN);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.PSRN, errorMessage);
        
        errorMessage = Functions.Counterparty.CheckNceoLength(_obj, _obj.NCEO);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.NCEO, errorMessage);
      }
    }

    public virtual void EmailValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(e.NewValue) && !Functions.Module.EmailIsValid(e.NewValue))
        e.AddWarning(Resources.WrongEmailFormat);
    }

    public virtual void TINValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var errorMessage = Functions.Counterparty.CheckTin(_obj, e.NewValue);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TIN, errorMessage, _obj.Info.Properties.Nonresident);
      }
    }

  }
}
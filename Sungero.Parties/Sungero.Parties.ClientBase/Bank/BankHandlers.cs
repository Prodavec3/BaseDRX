using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties
{
  partial class BankClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      Sungero.Parties.PublicFunctions.Bank.SetRequiredProperties(_obj);
    }

    public override void NonresidentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      base.NonresidentValueInput(e);
      
      if (e.NewValue != true)
      {
        var errorMessage = Functions.Bank.CheckBicLength(_obj.BIC);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.BIC, errorMessage);
        
        errorMessage = Functions.Bank.CheckCorrLength(_obj.CorrespondentAccount);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.CorrespondentAccount, errorMessage);
      }
      else
      {
        var errorMessage = Functions.Bank.CheckCorrAccountForNonresident(_obj.CorrespondentAccount);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.CorrespondentAccount, errorMessage);
      }
    }

    public virtual void SWIFTValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Functions.Bank.CheckSwift(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(result);
    }

    public virtual void CorrespondentAccountValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var result = Functions.Bank.CheckCorrLength(e.NewValue);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
      else
      {
        var result = Functions.Bank.CheckCorrAccountForNonresident(e.NewValue);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
    }

    public virtual void BICValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (_obj.Nonresident != true)
      {
        var result = Functions.Bank.CheckBicLength(e.NewValue);
        if (!string.IsNullOrEmpty(result))
          e.AddError(result);
      }
    }

  }
}
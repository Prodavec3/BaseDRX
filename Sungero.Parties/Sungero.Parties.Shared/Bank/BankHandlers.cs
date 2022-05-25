using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties
{
  partial class BankSharedHandlers
  {

    public override void NonresidentChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      base.NonresidentChanged(e);
      
      Sungero.Parties.PublicFunctions.Bank.SetRequiredProperties(_obj);
    }

  }
}
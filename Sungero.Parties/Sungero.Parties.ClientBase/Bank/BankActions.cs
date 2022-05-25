using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties.Client
{
  partial class BankActions
  {
    public override void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = new List<IBank>();
      if (!string.IsNullOrWhiteSpace(_obj.BIC))
        duplicates.AddRange(Functions.Bank.Remote.GetBanksWithSameBic(_obj, true));
      if (!string.IsNullOrWhiteSpace(_obj.SWIFT))
        duplicates.AddRange(Functions.Bank.Remote.GetBanksWithSameSwift(_obj, true));
      if (duplicates.Any())
      {
        duplicates.Show();
        return;
      }
      
      base.ShowDuplicates(e);
    }

    public override bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowDuplicates(e);
    }
  }
}
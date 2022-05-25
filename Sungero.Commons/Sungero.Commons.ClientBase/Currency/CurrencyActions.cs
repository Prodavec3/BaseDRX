using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Currency;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons.Client
{
  partial class CurrencyActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.Currency.Remote.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Sungero.Commons.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}
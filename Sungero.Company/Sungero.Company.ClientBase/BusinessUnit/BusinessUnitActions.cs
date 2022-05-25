using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.BusinessUnit;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Client
{
  partial class BusinessUnitActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicateBusinessUnit = Functions.BusinessUnit.Remote.GetDuplicateBusinessUnit(_obj);
      
      if (duplicateBusinessUnit.Any())
        duplicateBusinessUnit.Show();
      else
      {
        int? companyId = null;
        if (_obj.Company != null)
          companyId = _obj.Company.Id;
        var duplicateCounterparties = Parties.PublicFunctions.Counterparty.GetDuplicateCounterparties(_obj.TIN, _obj.TRRC, string.Empty, companyId, true);
        duplicateCounterparties.Show();
      }
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !string.IsNullOrWhiteSpace(_obj.TIN);
    }

  }

}
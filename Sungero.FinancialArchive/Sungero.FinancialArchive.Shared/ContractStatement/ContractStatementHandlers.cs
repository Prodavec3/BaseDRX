using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive
{
  partial class ContractStatementSharedHandlers
  {

    public override void CounterpartyChanged(Sungero.Docflow.Shared.AccountingDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      if (_obj.CounterpartySignatory != null && !Equals(_obj.CounterpartySignatory.Company, e.NewValue))
        _obj.CounterpartySignatory = null;
    }
  }

}
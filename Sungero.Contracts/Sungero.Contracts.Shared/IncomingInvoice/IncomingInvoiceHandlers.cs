using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class IncomingInvoiceSharedHandlers
  {

    public override void NumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.NumberChanged(e);
      _obj.RegistrationNumber = e.NewValue;
    }

    public override void DateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.DateChanged(e);
      _obj.RegistrationDate = e.NewValue;
    }
    
    public virtual void ContractChanged(Sungero.Contracts.Shared.IncomingInvoiceContractChangedEventArgs e) 
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue != null)
      {
        var contract = e.NewValue;
        _obj.Counterparty = contract.Counterparty;
        _obj.BusinessUnit = contract.BusinessUnit;
      }    

      _obj.Relations.AddFromOrUpdate(Constants.Module.AccountingDocumentsRelationName, e.OldValue, e.NewValue);
    }

    public override void CounterpartyChanged(Sungero.Docflow.Shared.AccountingDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      FillName();
      
      // Очистить договор при изменении контрагента.
      if (_obj.Contract == null || Equals(e.NewValue, _obj.Contract.Counterparty))
        return;
      if (!Equals(e.NewValue, e.OldValue))
        _obj.Contract = null;
    }
  }
}
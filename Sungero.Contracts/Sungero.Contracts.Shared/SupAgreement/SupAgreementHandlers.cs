using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class SupAgreementSharedHandlers
  {

    public override void LeadingDocumentChanged(Sungero.Docflow.Shared.OfficialDocumentLeadingDocumentChangedEventArgs e)
    {
      base.LeadingDocumentChanged(e);
      
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue != null)
      {
        var contract = ContractBases.As(e.NewValue);
        _obj.Counterparty = contract.Counterparty;
        _obj.BusinessUnit = contract.BusinessUnit;
        
        Docflow.PublicFunctions.OfficialDocument.CopyProjects(e.NewValue, _obj);
      }
      
      FillName();
      _obj.Relations.AddFromOrUpdate(Constants.Module.SupAgreementRelationName, e.OldValue, e.NewValue);
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      // Проставить корректно жизненный цикл для договора после смены типа.
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete)
      {
        if (_obj.ExchangeState == Docflow.OfficialDocument.ExchangeState.Obsolete)
          _obj.LifeCycleState = ContractBase.LifeCycleState.Obsolete;
        if (_obj.ExchangeState == Docflow.OfficialDocument.ExchangeState.Terminated)
          _obj.LifeCycleState = ContractBase.LifeCycleState.Terminated;
      }
    }

    public override void CounterpartyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      // Очистить договор при изменении контрагента.
      if (_obj.LeadingDocument == null || Equals(e.NewValue, _obj.LeadingDocument.Counterparty))
        return;
      if (!Equals(e.NewValue, e.OldValue))
        _obj.LeadingDocument = null;
    }

  }
}
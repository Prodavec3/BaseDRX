using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractualDocument;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{

  partial class ContractualDocumentMilestonesSharedCollectionHandlers
  {

    public virtual void MilestonesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_deleted.IsCompleted.Value)
        throw AppliedCodeException.Create(ContractualDocuments.Resources.CannotDeleteCompleteContractMilestone);
    }

    public virtual void MilestonesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (!_added.State.IsCopied)
      {
        _added.Performer = _obj.ResponsibleEmployee != null ? _obj.ResponsibleEmployee : Company.Employees.Current;
        _added.DaysToFinishWorks = 3;
      }
      _added.IsCompleted = false;
      _added.Task = null;
    }
  }

  partial class ContractualDocumentSharedHandlers
  {

    public override void TotalAmountChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        return;
      
      // Подставить по умолчанию валюту рубль.
      if (_obj.Currency == null)
      {
        var defaultCurrency = Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
        if (defaultCurrency != null)
          _obj.Currency = defaultCurrency;
      }
      
      base.TotalAmountChanged(e);
    }

    public override void OurSignatoryChanged(Sungero.Docflow.Shared.OfficialDocumentOurSignatoryChangedEventArgs e)
    {
      base.OurSignatoryChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        if (_obj.BusinessUnit == null)
        {
          var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(e.NewValue);
          if (businessUnit != null)
            _obj.BusinessUnit = businessUnit;
        }
      }
    }
    
    public override void CounterpartyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      // При изменении организации почистить подписывающего и контакта.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
      {
        if (_obj.CounterpartySignatory != null && !Equals(_obj.CounterpartySignatory.Company, e.NewValue))
          _obj.CounterpartySignatory = null;
        if (_obj.Contact != null && !Equals(_obj.Contact.Company, e.NewValue))
          _obj.Contact = null;
      }
      
      var isCompany = Sungero.Parties.CompanyBases.Is(e.NewValue) || e.NewValue == null;

      _obj.State.Properties.Contact.IsEnabled = isCompany;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = isCompany;
    }

    public override void CounterpartySignatoryChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartySignatoryChangedEventArgs e)
    {
      base.CounterpartySignatoryChanged(e);
      
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = e.NewValue.Company;

      if (_obj.ExternalApprovalState != ExternalApprovalState.Signed)
      {
        _obj.CounterpartySigningReason = e.NewValue != null
          ? e.NewValue.SigningReason
          : string.Empty;
      }
    }

    public virtual void ContactChanged(Sungero.Contracts.Shared.ContractualDocumentContactChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = e.NewValue.Company;
    }

    public virtual void ResponsibleEmployeeChanged(Sungero.Contracts.Shared.ContractualDocumentResponsibleEmployeeChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.BusinessUnit == null)
      {
        var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(e.NewValue);
        if (businessUnit != null)
          _obj.BusinessUnit = businessUnit;
      }
    }

  }
}
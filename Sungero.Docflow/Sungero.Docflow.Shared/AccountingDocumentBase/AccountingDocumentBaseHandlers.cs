using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow
{
  partial class AccountingDocumentBaseVersionsSharedCollectionHandlers
  {

    public override void VersionsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      base.VersionsAdded(e);
      
      // Если это формализованный документ и один титул уже есть, то при формировании второго титула не сбрасываем статусы эл. обмена и согласования с КА.
      if (_obj.IsFormalized == true && _obj.SellerTitleId.HasValue)
      {
        _obj.ExternalApprovalState = _obj.State.Properties.ExternalApprovalState.OriginalValue;
        _obj.ExchangeState = _obj.State.Properties.ExchangeState.OriginalValue;
      }
    }
  }

  partial class AccountingDocumentBaseSharedHandlers
  {

    public virtual void CounterpartySigningReasonChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var trimmedReason = e.NewValue.Trim();
      if (e.NewValue == trimmedReason)
        return;
      
      _obj.CounterpartySigningReason = trimmedReason;
    }

    public virtual void CorrectedChanged(Sungero.Docflow.Shared.AccountingDocumentBaseCorrectedChangedEventArgs e)
    {
      _obj.Relations.AddFromOrUpdate(Constants.Module.CorrectionRelationName, e.OldValue, e.NewValue);
      
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = e.NewValue.Counterparty;
    }

    public virtual void IsAdjustmentChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      FillName();
      if (e.NewValue == false)
        _obj.Corrected = null;
    }

    public override void LeadingDocumentChanged(Sungero.Docflow.Shared.OfficialDocumentLeadingDocumentChangedEventArgs e)
    {
      base.LeadingDocumentChanged(e);
      
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = AccountingDocumentBases.Is(e.NewValue)
          ? Docflow.AccountingDocumentBases.As(e.NewValue).Counterparty
          : Contracts.ContractualDocuments.As(e.NewValue).Counterparty;
      
      if (e.NewValue != null && _obj.BusinessUnit == null)
        _obj.BusinessUnit = e.NewValue.BusinessUnit;
      
      if (e.NewValue != null)
        Docflow.PublicFunctions.OfficialDocument.CopyProjects(e.NewValue, _obj);
      
      FillName();
      _obj.Relations.AddFromOrUpdate(Contracts.PublicConstants.Module.AccountingDocumentsRelationName, e.OldValue, e.NewValue);
    }

    public override void LifeCycleStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {

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

    public virtual void CounterpartySignatoryChanged(Sungero.Docflow.Shared.AccountingDocumentBaseCounterpartySignatoryChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = e.NewValue.Company;

      if (_obj.ExternalApprovalState != ExternalApprovalState.Signed)
      {
        _obj.CounterpartySigningReason = e.NewValue != null
          ? e.NewValue.SigningReason
          : string.Empty;
      }
    }

    public virtual void CounterpartyChanged(Sungero.Docflow.Shared.AccountingDocumentBaseCounterpartyChangedEventArgs e)
    {
      // При изменении организации почистить подписывающего и контакта.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
      {
        if (_obj.CounterpartySignatory != null && !Equals(_obj.CounterpartySignatory.Company, e.NewValue))
          _obj.CounterpartySignatory = null;
        if (_obj.Contact != null && !Equals(_obj.Contact.Company, e.NewValue))
          _obj.Contact = null;
        if (_obj.LeadingDocument != null &&
            ((Docflow.AccountingDocumentBases.Is(_obj.LeadingDocument) && !Equals(Docflow.AccountingDocumentBases.As(_obj.LeadingDocument).Counterparty, e.NewValue)) ||
             (Contracts.ContractualDocuments.Is(_obj.LeadingDocument) && !Equals(Contracts.ContractualDocuments.As(_obj.LeadingDocument).Counterparty, e.NewValue))))
          _obj.LeadingDocument = null;
      }
      
      var isCompany = Sungero.Parties.CompanyBases.Is(e.NewValue) || e.NewValue == null;

      _obj.State.Properties.Contact.IsEnabled = isCompany;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = isCompany;
      
      FillName();
    }

    public virtual void ContactChanged(Sungero.Docflow.Shared.AccountingDocumentBaseContactChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = e.NewValue.Company;
    }

    public virtual void ResponsibleEmployeeChanged(Sungero.Docflow.Shared.AccountingDocumentBaseResponsibleEmployeeChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.BusinessUnit == null)
      {
        var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(e.NewValue);
        if (businessUnit != null)
          _obj.BusinessUnit = businessUnit;
      }
    }

    public virtual void TotalAmountChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
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
    }

    public virtual void DateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      FillName();
    }

    public virtual void NumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      FillName();
    }

  }
}
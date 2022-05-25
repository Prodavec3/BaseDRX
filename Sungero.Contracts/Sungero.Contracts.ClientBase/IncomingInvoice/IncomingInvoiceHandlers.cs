using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class IncomingInvoiceClientHandlers
  {

    public virtual void ContractValueInput(Sungero.Contracts.Client.IncomingInvoiceContractValueInputEventArgs e)
    {
      this._obj.State.Properties.Contract.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      // Контрагент не дб задизейблен, если незаполнен.
      if (_obj.Counterparty == null)
        _obj.State.Properties.Counterparty.IsEnabled = true;
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.Number.IsRequired = false;
      _obj.State.Properties.Date.IsRequired = false;
      _obj.State.Properties.TotalAmount.IsRequired = false;
      _obj.State.Properties.Currency.IsRequired = false;
    }

    public override void CounterpartyValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCounterpartyValueInputEventArgs e)
    {
      base.CounterpartyValueInput(e);
      if (Functions.IncomingInvoice.HaveDuplicates(_obj, _obj.DocumentKind, _obj.Number, _obj.Date, _obj.TotalAmount, _obj.Currency, e.NewValue))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.Number,
                     _obj.Info.Properties.Date,
                     _obj.Info.Properties.TotalAmount,
                     _obj.Info.Properties.Currency,
                     _obj.Info.Properties.Counterparty);
    }

    public override void CurrencyValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCurrencyValueInputEventArgs e)
    {
      base.CurrencyValueInput(e);
      if (Functions.IncomingInvoice.HaveDuplicates(_obj, _obj.DocumentKind, _obj.Number, _obj.Date, _obj.TotalAmount, e.NewValue, _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.Number,
                     _obj.Info.Properties.Date,
                     _obj.Info.Properties.TotalAmount,
                     _obj.Info.Properties.Currency,
                     _obj.Info.Properties.Counterparty);
    }

    public override void DateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.DateValueInput(e);
      
      if (e.NewValue != null && e.NewValue >= Calendar.SqlMinValue)
      {
        if (Functions.IncomingInvoice.HaveDuplicates(_obj, _obj.DocumentKind, _obj.Number, e.NewValue, _obj.TotalAmount, _obj.Currency, _obj.Counterparty))
          e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                       _obj.Info.Properties.DocumentKind,
                       _obj.Info.Properties.Number,
                       _obj.Info.Properties.Date,
                       _obj.Info.Properties.TotalAmount,
                       _obj.Info.Properties.Currency,
                       _obj.Info.Properties.Counterparty);
      }
      
      this._obj.State.Properties.Date.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void NumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.NumberValueInput(e);
      if (Functions.IncomingInvoice.HaveDuplicates(_obj, _obj.DocumentKind, e.NewValue, _obj.Date, _obj.TotalAmount, _obj.Currency, _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.Number,
                     _obj.Info.Properties.Date,
                     _obj.Info.Properties.TotalAmount,
                     _obj.Info.Properties.Currency,
                     _obj.Info.Properties.Counterparty);
      
      this._obj.State.Properties.Number.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      base.DocumentKindValueInput(e);
      if (Functions.IncomingInvoice.HaveDuplicates(_obj, e.NewValue, _obj.Number, _obj.Date, _obj.TotalAmount, _obj.Currency, _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.Number,
                     _obj.Info.Properties.Date,
                     _obj.Info.Properties.TotalAmount,
                     _obj.Info.Properties.Currency,
                     _obj.Info.Properties.Counterparty);
    }

    public override void TotalAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(IncomingInvoices.Resources.TotalAmountMustBePositive);
      
      base.TotalAmountValueInput(e);
      if (Functions.IncomingInvoice.HaveDuplicates(_obj, _obj.DocumentKind, _obj.Number, _obj.Date, e.NewValue, _obj.Currency, _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.Number,
                     _obj.Info.Properties.Date,
                     _obj.Info.Properties.TotalAmount,
                     _obj.Info.Properties.Currency,
                     _obj.Info.Properties.Counterparty);
    }
  }

}
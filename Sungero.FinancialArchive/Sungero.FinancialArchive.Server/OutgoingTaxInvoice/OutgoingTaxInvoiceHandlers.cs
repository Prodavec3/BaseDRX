using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.OutgoingTaxInvoice;

namespace Sungero.FinancialArchive
{
  partial class OutgoingTaxInvoiceConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Contracts.IncomingInvoices.Is(_source))
        e.Map(_info.Properties.LeadingDocument, Sungero.Contracts.IncomingInvoices.Info.Properties.Contract);
        
      // Отключить проброс полей, которых нет в выставленных СФ.
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySignatory);
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySigningReason);
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.ResponsibleEmployee);
    }
  }

  partial class OutgoingTaxInvoiceCorrectedPropertyFilteringServerHandler<T>
  {
    public override IQueryable<T> CorrectedFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.CorrectedFiltering(query, e);
      return query.Where(x => (OutgoingTaxInvoices.Is(x) || UniversalTransferDocuments.Is(x)) && x.IsAdjustment != true);
    }
  }

  partial class OutgoingTaxInvoiceServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.LifeCycleState == null || _obj.LifeCycleState == LifeCycleState.Draft)
        _obj.LifeCycleState = LifeCycleState.Active;
      
      _obj.ResponsibleEmployee = null;
    }
  }

}
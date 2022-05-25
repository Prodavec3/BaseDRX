using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.UniversalTransferDocument;

namespace Sungero.FinancialArchive
{
  partial class UniversalTransferDocumentConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Contracts.IncomingInvoices.Is(_source))
        e.Map(_info.Properties.LeadingDocument, Sungero.Contracts.IncomingInvoices.Info.Properties.Contract);
    }
  }

  partial class UniversalTransferDocumentCorrectedPropertyFilteringServerHandler<T>
  {
    public override IQueryable<T> CorrectedFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.CorrectedFiltering(query, e);
      return query.Where(x => UniversalTransferDocuments.Is(x) && x.IsAdjustment != true);
    }
  }

}
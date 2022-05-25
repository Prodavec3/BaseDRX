using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.Waybill;

namespace Sungero.FinancialArchive
{
  partial class WaybillConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Contracts.IncomingInvoices.Is(_source))
        e.Map(_info.Properties.LeadingDocument, Sungero.Contracts.IncomingInvoices.Info.Properties.Contract);
      
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.IsAdjustment);
    }
  }

}
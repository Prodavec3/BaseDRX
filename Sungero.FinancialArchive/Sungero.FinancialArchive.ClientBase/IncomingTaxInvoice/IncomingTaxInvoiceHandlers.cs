using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.IncomingTaxInvoice;

namespace Sungero.FinancialArchive
{
  partial class IncomingTaxInvoiceClientHandlers
  {

    public override void CorrectedValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCorrectedValueInputEventArgs e)
    {
      base.CorrectedValueInput(e);
      this._obj.State.Properties.Corrected.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void IsAdjustmentValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      base.IsAdjustmentValueInput(e);
      this._obj.State.Properties.IsAdjustment.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void LeadingDocumentValueInput(Sungero.Docflow.Client.OfficialDocumentLeadingDocumentValueInputEventArgs e)
    {
      base.LeadingDocumentValueInput(e);
      this._obj.State.Properties.LeadingDocument.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void RegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.RegistrationNumberValueInput(e);
      this._obj.State.Properties.RegistrationNumber.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void RegistrationDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.RegistrationDateValueInput(e);
      this._obj.State.Properties.RegistrationDate.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override IEnumerable<Enumeration> LifeCycleStateFiltering(IEnumerable<Enumeration> query)
    {
      query = base.LifeCycleStateFiltering(query);
      return query.Where(x => x != Docflow.AccountingDocumentBase.LifeCycleState.Draft);
    }

  }
}
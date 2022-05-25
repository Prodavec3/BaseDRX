using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.OutgoingTaxInvoice;

namespace Sungero.FinancialArchive
{
  partial class OutgoingTaxInvoiceClientHandlers
  {

    public override void ContactValueInput(Sungero.Docflow.Client.AccountingDocumentBaseContactValueInputEventArgs e)
    {
      base.ContactValueInput(e);
      this._obj.State.Properties.Contact.HighlightColor = Sungero.Core.Colors.Empty;
    }

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
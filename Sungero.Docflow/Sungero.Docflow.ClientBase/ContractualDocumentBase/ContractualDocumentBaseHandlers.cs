using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow
{
  partial class ContractualDocumentBaseClientHandlers
  {

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

    public override void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      base.BusinessUnitValueInput(e);
      
      this._obj.State.Properties.BusinessUnit.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void OurSignatoryValueInput(Sungero.Docflow.Client.OfficialDocumentOurSignatoryValueInputEventArgs e)
    {
      base.OurSignatoryValueInput(e);
      
      this._obj.State.Properties.OurSignatory.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void CounterpartyValueInput(Sungero.Docflow.Client.ContractualDocumentBaseCounterpartyValueInputEventArgs e)
    {
      this._obj.State.Properties.Counterparty.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void CounterpartySignatoryValueInput(Sungero.Docflow.Client.ContractualDocumentBaseCounterpartySignatoryValueInputEventArgs e)
    {
      this._obj.State.Properties.CounterpartySignatory.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void TotalAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      this._obj.State.Properties.TotalAmount.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void CurrencyValueInput(Sungero.Docflow.Client.ContractualDocumentBaseCurrencyValueInputEventArgs e)
    {
      this._obj.State.Properties.Currency.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.Subject.IsRequired = false;
      _obj.State.Properties.Counterparty.IsRequired = false;
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.UniversalTransferDocument;

namespace Sungero.FinancialArchive
{
  partial class UniversalTransferDocumentClientHandlers
  {

    public override void CorrectedValueInput(Sungero.Docflow.Client.AccountingDocumentBaseCorrectedValueInputEventArgs e)
    {
      base.CorrectedValueInput(e);
      this._obj.State.Properties.Corrected.HighlightColor = Sungero.Core.Colors.Empty;
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
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow
{
  partial class ContractualDocumentBaseSharedHandlers
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

    public override void OurSignatoryChanged(Sungero.Docflow.Shared.OfficialDocumentOurSignatoryChangedEventArgs e)
    {
      base.OurSignatoryChanged(e);
      
      this._obj.State.Properties.OurSignatory.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void CounterpartySignatoryChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartySignatoryChangedEventArgs e)
    {
      this._obj.State.Properties.CounterpartySignatory.HighlightColor = Sungero.Core.Colors.Empty;
    }

  }
}
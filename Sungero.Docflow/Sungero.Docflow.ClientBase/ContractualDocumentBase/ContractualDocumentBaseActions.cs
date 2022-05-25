using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow.Client
{
  partial class ContractualDocumentBaseActions
  {
    public virtual void PrintEnvelopeCard(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(null, new List<IContractualDocumentBase>() { _obj });
    }

    public virtual bool CanPrintEnvelopeCard(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

  }

  partial class ContractualDocumentBaseCollectionActions
  {

    public virtual bool CanPrintEnvelope(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_objs.Any(t => t.State.IsInserted || t.State.IsChanged);
    }

    public virtual void PrintEnvelope(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(null, _objs.ToList());
    }
  }

}
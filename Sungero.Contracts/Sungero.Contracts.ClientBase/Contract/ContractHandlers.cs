using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.Contract;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractClientHandlers
  {

    public override void CurrencyValueInput(Sungero.Docflow.Client.ContractualDocumentBaseCurrencyValueInputEventArgs e)
    {
      base.CurrencyValueInput(e);
    }
  }

}
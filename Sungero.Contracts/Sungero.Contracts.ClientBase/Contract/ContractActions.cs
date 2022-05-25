using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.Contract;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Client
{
  partial class ContractActions
  {
    public override void ChangeDocumentType(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeDocumentType(e);
    }

    public override bool CanChangeDocumentType(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.VerificationState == VerificationState.InProcess && base.CanChangeDocumentType(e);
    }

  }

}
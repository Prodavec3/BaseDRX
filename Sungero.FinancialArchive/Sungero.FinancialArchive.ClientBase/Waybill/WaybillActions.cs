using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.FinancialArchive.Waybill;

namespace Sungero.FinancialArchive.Client
{
  partial class WaybillActions
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
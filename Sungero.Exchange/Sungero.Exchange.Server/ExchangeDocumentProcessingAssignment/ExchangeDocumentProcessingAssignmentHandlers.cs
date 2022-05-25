using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentProcessingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result.Value == Result.Abort)
      {
        e.Result = ExchangeDocumentProcessingAssignments.Resources.RejectResult;
      }
      else if (_obj.Result.Value == Result.ReAddress && !string.IsNullOrEmpty(_obj.ActiveText))
      {
        e.Result = ExchangeDocumentProcessingAssignments.Resources.ReAddressedResultFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      }
    }
  }

}
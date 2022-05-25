using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingAssignment;

namespace Sungero.Exchange
{
  partial class ReceiptNotificationSendingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.Forwarded && !string.IsNullOrEmpty(_obj.ActiveText))
      {
        e.Result = ExchangeDocumentProcessingAssignments.Resources.ReAddressedResultFormat(
          Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      }
    }
  }

}
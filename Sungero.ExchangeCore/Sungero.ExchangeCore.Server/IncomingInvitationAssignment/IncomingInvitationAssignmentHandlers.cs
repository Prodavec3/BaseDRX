using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.IncomingInvitationAssignment;

namespace Sungero.ExchangeCore
{
  partial class IncomingInvitationAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.Accept)
        e.Result = IncomingInvitationAssignments.Resources.InvitationAccepted;
      else
        e.Result = IncomingInvitationAssignments.Resources.InvitationRejected;
    }
  }

}
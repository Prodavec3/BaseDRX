using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.IncomingInvitationTask;
using Sungero.Workflow;

namespace Sungero.ExchangeCore.Server
{
  partial class IncomingInvitationTaskRouteHandlers
  {

    public virtual void StartBlock3(Sungero.ExchangeCore.Server.IncomingInvitationAssignmentArguments e)
    {
      e.Block.Performers.Add(_obj.Box.Responsible);
      e.Block.Subject = IncomingInvitationTasks.Resources.AssignmentSubjectFormat(_obj.Box.BusinessUnit.Name, _obj.Counterparty.Name, _obj.Box.ExchangeService.Name);
      e.Block.Box = _obj.Box;
      e.Block.Counterparty = _obj.Counterparty;
    }

    public virtual void StartAssignment3(Sungero.ExchangeCore.IIncomingInvitationAssignment assignment, Sungero.ExchangeCore.Server.IncomingInvitationAssignmentArguments e)
    {
      assignment.Deadline = _obj.MaxDeadline;
    }

    public virtual void CompleteAssignment3(Sungero.ExchangeCore.IIncomingInvitationAssignment assignment, Sungero.ExchangeCore.Server.IncomingInvitationAssignmentArguments e)
    {
      
    }

    public virtual void EndBlock3(Sungero.ExchangeCore.Server.IncomingInvitationAssignmentEndBlockEventArguments e)
    {
      
    }

    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

  }
}
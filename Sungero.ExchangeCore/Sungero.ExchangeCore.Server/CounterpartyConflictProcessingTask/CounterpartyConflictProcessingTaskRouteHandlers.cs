using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingTask;
using Sungero.Workflow;

namespace Sungero.ExchangeCore.Server
{
  partial class CounterpartyConflictProcessingTaskRouteHandlers
  {

    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }

    public virtual void StartBlock3(Sungero.ExchangeCore.Server.CounterpartyConflictProcessingAssignmentArguments e)
    {
      e.Block.Performers.Add(_obj.Assignee);
      e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
    }

    public virtual void StartAssignment3(Sungero.ExchangeCore.ICounterpartyConflictProcessingAssignment assignment, Sungero.ExchangeCore.Server.CounterpartyConflictProcessingAssignmentArguments e)
    {
      
    }

    public virtual void CompleteAssignment3(Sungero.ExchangeCore.ICounterpartyConflictProcessingAssignment assignment, Sungero.ExchangeCore.Server.CounterpartyConflictProcessingAssignmentArguments e)
    {
      
    }

    public virtual void EndBlock3(Sungero.ExchangeCore.Server.CounterpartyConflictProcessingAssignmentEndBlockEventArguments e)
    {
      
    }

  }
}
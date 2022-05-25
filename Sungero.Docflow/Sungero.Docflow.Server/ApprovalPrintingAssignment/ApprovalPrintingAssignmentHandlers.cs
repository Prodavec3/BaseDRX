using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalPrintingAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalPrintingAssignmentReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var reworkPerformersIds = Functions.ApprovalTask.GetReworkPerformers(ApprovalTasks.As(_obj.Task))
        .Select(p => p.Id).ToList();
      return query.Where(x => reworkPerformersIds.Contains(x.Id));
    }
  }

  partial class ApprovalPrintingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.ForRevision)
        e.Result = ApprovalTasks.Resources.ForRework;
      else
        e.Result = Functions.ApprovalTask.GetCollapsedResult(ApprovalTasks.As(_obj.Task), _obj.Result);
    }
  }
}
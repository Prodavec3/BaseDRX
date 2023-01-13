using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalAssignmentReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var reworkPerformersIds = Functions.ApprovalTask.GetReworkPerformers(ApprovalTasks.As(_obj.Task))
        .Select(p => p.Id).ToList();
      return query.Where(x => reworkPerformersIds.Contains(x.Id));
    }
  }

  partial class ApprovalAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Result == Result.Forward && _obj.Task.GetStartedSchemeVersion() > LayerSchemeVersions.V1 && !Functions.ApprovalAssignment.CanForwardTo(_obj, _obj.Addressee))
        e.AddError(_obj.Info.Properties.Addressee, FreeApprovalAssignments.Resources.ApproverAlreadyExistsFormat(_obj.Addressee.Person.ShortName));
      
      if (_obj.Result == Result.Approved)
        e.Result = ApprovalTasks.Resources.Endorsed;
      else if (_obj.Result == Result.WithSuggestions)
        e.Result = ApprovalTasks.Resources.EndorsedWithSuggestions;
      else if (_obj.Result == Result.Forward)
        e.Result = FreeApprovalTasks.Resources.ForwardedFormat(Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, DeclensionCase.Dative, true));
      else
        e.Result = ApprovalTasks.Resources.ForRework;
    }
  }
}
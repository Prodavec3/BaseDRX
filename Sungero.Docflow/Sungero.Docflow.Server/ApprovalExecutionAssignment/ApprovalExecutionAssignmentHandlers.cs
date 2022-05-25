using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalExecutionAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalExecutionAssignmentReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var reworkPerformersIds = Functions.ApprovalTask.GetReworkPerformers(ApprovalTasks.As(_obj.Task))
        .Select(p => p.Id).ToList();
      return query.Where(x => reworkPerformersIds.Contains(x.Id));
    }
  }

  partial class ApprovalExecutionAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Проверить зарегистрированность документа, если схлопнуто с этапом регистрации.
      if (_obj.Result == Result.Complete && Functions.ApprovalTask.CurrentStageCollapsedWithSpecificStage(ApprovalTasks.As(_obj.Task), _obj.StageNumber, Docflow.ApprovalStage.StageType.Register))
      {
        var registrationState = _obj.DocumentGroup.OfficialDocuments.First().RegistrationState;
        if (registrationState == null || registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        {
          e.AddError(ApprovalTasks.Resources.ToPerformNeedRegisterDocument);
          return;
        }
      }
      if (_obj.Result == Result.ForRevision)
        e.Result = ApprovalTasks.Resources.ForRework;
      else
        e.Result = Functions.ApprovalTask.GetCollapsedResult(ApprovalTasks.As(_obj.Task), _obj.Result);
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement
{
  partial class StatusReportRequestTaskAssigneePropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> AssigneeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      
      if (parentTask != null)
        query = parentTask.IsCompoundActionItem ?? false ?
          query.Where(u => Functions.ActionItemExecutionTask.GetActionItemsPerformers(parentTask).Contains(u)) :
          query.Where(u => u.Equals(parentTask.Assignee));
      
      if (parentAssignment != null)
        query = query.Where(u => Functions.ActionItemExecutionAssignment.GetActionItemsAssignees(parentAssignment).Contains(u));
      
      return query;
    }
  }

  partial class StatusReportRequestTaskServerHandlers
  {
    
    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      _obj.Report = string.Empty;
      _obj.ReportNote = string.Empty;
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Assignee, _obj.MaxDeadline, Calendar.Now))
        e.AddError(_obj.Info.Properties.MaxDeadline, RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      
      // Выдать права на изменение для возможности прекращения задачи.
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var taskDeadlineInDays = 1;
      var assignee = _obj.Assignee ?? Users.Current;
      _obj.MaxDeadline = Calendar.Now.AddWorkingDays(assignee, taskDeadlineInDays);
      _obj.NeedsReview = false;
    }
  }
}
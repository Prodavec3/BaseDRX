using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionTaskAssigneePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AssigneeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var allUsers = new List<IUser>();
      var actionItemExecutionAssignment = RecordManagement.ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      if (actionItemExecutionAssignment != null)
      {
        allUsers.AddRange(Functions.DeadlineExtensionTask.GetAssigneesForActionItemExecutionTask(actionItemExecutionAssignment).Assignees);
      }
      else if (Sungero.Docflow.ApprovalManagerAssignments.Is(_obj.ParentAssignment))
      {
        allUsers.Add(_obj.ParentAssignment.Author);
      }
      else
      {
        allUsers.Add(_obj.ParentAssignment.Author);
        var employee = Company.Employees.As(_obj.ParentAssignment.Author);
        if (employee != null && employee.Department.Manager != null)
          allUsers.Add(employee.Department.Manager);
      }
      
      return query.Where(x => allUsers.Contains(x));
    }
  }

  partial class DeadlineExtensionTaskServerHandlers
  {

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Sungero.Docflow.Functions.DeadlineExtensionTask.ValidateDeadlineExtensionTaskStart(_obj, e);
      
      var assignmentsDeadLine = 1;
      _obj.MaxDeadline = Calendar.Now.AddWorkingDays(_obj.Assignee, assignmentsDeadLine);
    }
  }

}
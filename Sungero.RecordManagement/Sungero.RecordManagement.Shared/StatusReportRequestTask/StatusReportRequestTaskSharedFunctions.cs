using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement.Shared
{
  partial class StatusReportRequestTaskFunctions
  {
    /// <summary>
    /// Получить тему запроса отчета.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="beginningSubject">Начальная тема.</param>
    /// <returns>Сформированная тема.</returns>
    public static string GetStatusReportRequestSubject(Sungero.RecordManagement.IStatusReportRequestTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      var actionItemExecution = ActionItemExecutionTasks.As(task.ParentTask) ?? ActionItemExecutionTasks.As(task.ParentAssignment.Task);
      if (actionItemExecution.IsCompoundActionItem.Value && task.Assignee != null)
      {
        var assignment = Functions.ActionItemExecutionTask.Remote.GetActionItemPartAssignments(actionItemExecution)
          .Where(a => Equals(a.Performer, task.Assignee))
          .Where(a => ActionItemExecutionTasks.Is(a.Task))
          .FirstOrDefault();
        actionItemExecution = ActionItemExecutionTasks.As(assignment.Task);
      }
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, beginningSubject);
      
      return Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
    }
  }
}
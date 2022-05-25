using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.DeadlineExtensionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DeadlineExtensionTaskFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentAssignment.Task);
      return Functions.ActionItemExecutionTask.GetStateView(parentTask);
    }

    /// <summary>
    /// Получить срок продления в строковом формате.
    /// </summary>
    /// <param name="desiredDeadline">Срок.</param>
    /// <returns>Строковое представление.</returns>
    public static string GetDesiredDeadlineLabel(DateTime desiredDeadline)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (desiredDeadline == desiredDeadline.Date)
          return desiredDeadline.ToString("d");
        
        var utcOffset = Calendar.UtcOffset.TotalHours;
        var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
        return string.Format("{0:g} (UTC{1})", desiredDeadline, utcOffsetLabel);
      }
    }
    
    /// <summary>
    /// Получить задачу на продление срока по поручению.
    /// </summary>
    /// <param name="executionAssignment">Поручение.</param>
    /// <returns>Задача, на основе которой создано поручение.</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IDeadlineExtensionTask GetDeadlineExtension(Sungero.RecordManagement.IActionItemExecutionAssignment executionAssignment)
    {
      // Проверить наличие старой задачи на продление срока.
      var oldTask = DeadlineExtensionTasks.GetAll()
        .Where(j => Equals(j.ParentAssignment, executionAssignment))
        .Where(j => j.Status == Workflow.Task.Status.InProcess || j.Status == Workflow.Task.Status.Draft)
        .FirstOrDefault();
      
      if (oldTask != null)
        return oldTask;
      
      var task = DeadlineExtensionTasks.CreateAsSubtask(executionAssignment);
      
      task.ActionItemExecutionAssignment = executionAssignment;
      
      task.MaxDeadline = (executionAssignment.Deadline < Calendar.Now) ? Calendar.Today : executionAssignment.Deadline;
      task.NeedsReview = false;
      task.ActionItem = executionAssignment.ActionItem;
      var itemExecution = ActionItemExecutionTasks.As(executionAssignment.Task);
      var document = ActionItemExecutionTasks.Get(itemExecution.Id).DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        task.DocumentsGroup.OfficialDocuments.Add(document);
      task.Subject = Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(task, DeadlineExtensionTasks.Resources.ExtendDeadlineTaskSubject);

      // Определить исполнителя. Для составного поручения взять ведущую задачу.
      var leadItemExecution = itemExecution;
      var parentItemExecution = ActionItemExecutionTasks.As(itemExecution.ParentTask);
      if (parentItemExecution != null && parentItemExecution.IsCompoundActionItem == true)
        leadItemExecution = parentItemExecution;
      
      // Исполнителем указать контролёра, если его нет, то стартовавшего задачу, если и его нет, то автора.
      // Если контроля не было, то стартовавшего задачу.
      if (itemExecution.IsUnderControl == true)
        task.Assignee = leadItemExecution.Supervisor ?? leadItemExecution.StartedBy;
      else
        task.Assignee = leadItemExecution.StartedBy;

      if (task.Assignee == null || task.Assignee.IsSystem == true)
        task.Assignee = leadItemExecution.Author;
      
      task.CurrentDeadline = executionAssignment.Deadline;
      task.Author = executionAssignment.Performer;
      
      return task;
    }
    
    /// <summary>
    /// Проверить документ на вхождение в обязательную группу вложений.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ обязателен.</returns>
    public virtual bool DocumentInRequredGroup(Docflow.IOfficialDocument document)
    {
      return _obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(d, document));
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var deadlineExtension = DeadlineExtensionTasks.As(_obj);
      if (deadlineExtension == null)
        return assignees;
      
      if (deadlineExtension.Assignee != null)
        assignees.Add(deadlineExtension.Assignee);
      
      if (ActionItemExecutionTasks.Is(deadlineExtension.ParentAssignment.Task))
        assignees.AddRange(ActionItemExecutionTasks.As(deadlineExtension.ParentAssignment.Task).CoAssignees.Select(ca => ca.Assignee));
      
      return assignees.Distinct().ToList();
    }
  }
}
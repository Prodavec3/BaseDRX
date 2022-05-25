using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow.Server
{
  partial class DeadlineExtensionTaskFunctions
  {
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
    /// Получить исполнителей продления поручения.
    /// </summary>
    /// <param name="parent">Родительское задание, от которого создается задача на продление.</param>
    /// <returns>Исполнители и признак, можно ли пользователю выбирать самому.</returns>
    [Remote(IsPure = true, PackResultEntityEagerly = true)]
    public static Structures.DeadlineExtensionTask.ActionItemAssignees GetAssigneesForActionItemExecutionTask(RecordManagement.IActionItemExecutionAssignment parent)
    {
      var users = new List<IUser>();
      var canSelect = true;
      var leadItemExecution = RecordManagement.ActionItemExecutionTasks.As(parent.Task);
      
      // Исполнителем указать контролёра, если его нет, то стартовавшего задачу, если и его нет, то автора.
      // Если контроля не было, то стартовавшего задачу.
      if (leadItemExecution.IsUnderControl == true)
      {
        canSelect = false;
        
        if (leadItemExecution.IsCompoundActionItem == true)
        {
          var part = leadItemExecution.ActionItemParts.Where(x => Equals(x.ActionItemPartExecutionTask, parent.Task)).FirstOrDefault();
          if (part.Supervisor != null)
            users.Add(part.Supervisor);
        }
        
        users.Add(leadItemExecution.Supervisor);
      }

      if (leadItemExecution.ActionItemType.Value == RecordManagement.ActionItemExecutionTask.ActionItemType.Component && leadItemExecution.ParentTask != null &&
          RecordManagement.ActionItemExecutionTasks.Is(leadItemExecution.ParentTask))
        users.Add(leadItemExecution.ParentTask.StartedBy);
      else
        users.Add(leadItemExecution.StartedBy);
      users.Add(leadItemExecution.Author);
      users = users.Where(u => u.IsSystem != true).Distinct().ToList();
      if (canSelect && users.Count == 1)
        canSelect = false;
      
      return Structures.DeadlineExtensionTask.ActionItemAssignees.Create(users, canSelect);
    }
    
    /// <summary>
    /// Получить задачу на продление срока по заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Задача, на основе которой создано задание.</returns>
    [Remote(PackResultEntityEagerly = true)]
    [Public]
    public static IDeadlineExtensionTask GetDeadlineExtension(Sungero.Workflow.IAssignment assignment)
    {
      // Проверить наличие старой задачи на продление срока.
      var oldTask = Docflow.DeadlineExtensionTasks.GetAll()
        .Where(j => Equals(j.ParentAssignment, assignment))
        .Where(j => j.Status == Workflow.Task.Status.InProcess || j.Status == Workflow.Task.Status.Draft)
        .FirstOrDefault();
      
      if (oldTask != null)
        return oldTask;
      
      var task = Docflow.DeadlineExtensionTasks.CreateAsSubtask(assignment);
      
      task.MaxDeadline = (assignment.Deadline < Calendar.Today) ? Calendar.Today : assignment.Deadline;
      task.NeedsReview = false;
      task.Subject = Sungero.Docflow.Functions.DeadlineExtensionTask.GetDeadlineExtensionSubject(task, DeadlineExtensionTasks.Resources.ExtendDeadlineTaskSubject);

      // Определить исполнителя.
      if (Sungero.RecordManagement.ActionItemExecutionAssignments.Is(assignment))
      {
        var actionItem = RecordManagement.ActionItemExecutionAssignments.As(assignment);
        task.Assignee = Functions.DeadlineExtensionTask.GetAssigneesForActionItemExecutionTask(actionItem).Assignees.FirstOrDefault();
      }
      else if (Sungero.Docflow.ApprovalManagerAssignments.Is(assignment) || Sungero.RecordManagement.ReportRequestAssignments.Is(assignment))
      {
        task.Assignee = assignment.Author;
      }
      else
      {
        task.Assignee = assignment.Author;
        if (Equals(assignment.Author, assignment.Performer))
        {
          var author = Company.Employees.As(assignment.Author);
          if (author != null && author.Department.Manager != null)
            task.Assignee = author.Department.Manager;
        }
      }
      
      task.CurrentDeadline = assignment.Deadline;
      task.Author = assignment.Performer;

      return task;
    }
    
    /// <summary>
    /// Получить тему задачи на продление срока.
    /// </summary>
    /// <param name="task">Задача "Продление срока".</param>
    /// <param name="beginningSubject">Начальная тема задачи.</param>
    /// <returns>Сформированная тема задачи.</returns>
    public static string GetDeadlineExtensionSubject(Sungero.Docflow.IDeadlineExtensionTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = string.Format(">> {0} ", beginningSubject);
        
        if (Sungero.RecordManagement.ActionItemExecutionAssignments.Is(task.ParentAssignment))
        {
          var executionAssignment = Sungero.RecordManagement.ActionItemExecutionAssignments.As(task.ParentAssignment);
          if (!string.IsNullOrWhiteSpace(executionAssignment.ActionItem))
          {
            var hasDocument = executionAssignment.DocumentsGroup.OfficialDocuments.Any();
            var resolution = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask
              .FormatActionItemForSubject(executionAssignment.ActionItem, hasDocument);
            
            // Кавычки даже для поручений без документа.
            if (!hasDocument)
              resolution = string.Format("\"{0}\"", resolution);
            
            subject += DeadlineExtensionTasks.Resources.SubjectFromActionItemFormat(resolution);
          }
          
          // Добавить имя документа, если поручение с документом.
          var document = executionAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
          if (document != null)
            subject += Sungero.RecordManagement.ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        }
        else
        {
          subject += task.ParentAssignment.Subject;
        }
        
        subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject != null && subject.Length > DeadlineExtensionTasks.Info.Properties.Subject.Length)
          subject = subject.Substring(0, DeadlineExtensionTasks.Info.Properties.Subject.Length);
        
        return subject;
      }
    }
  }
}
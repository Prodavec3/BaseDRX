using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class StatusReportRequestTaskFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var parent = _obj.ParentAssignment != null ? _obj.ParentAssignment.Task : _obj.ParentTask;
      var parentTask = ActionItemExecutionTasks.As(parent);
      return Functions.ActionItemExecutionTask.GetStateView(parentTask);
    }
    
    /// <summary>
    /// Создать Запрос отчета.
    /// </summary>
    /// <param name="task">Поручение, для которого нужен отчет.</param>
    /// <returns>Задача "Запрос отчета по поручению".</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IStatusReportRequestTask CreateStatusReportRequest(IActionItemExecutionTask task)
    {
      var performers = Functions.ActionItemExecutionTask.GetActionItemsPerformers(task).ToList();
      if (!performers.Any())
        return null;
      
      var statusReportRequest = StatusReportRequestTasks.CreateAsSubtask(task);
      statusReportRequest.ActionItem = task.ActionItem;
      var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        statusReportRequest.DocumentsGroup.OfficialDocuments.Add(document);
      statusReportRequest.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(statusReportRequest, StatusReportRequestTasks.Resources.ReportRequestTaskSubject);
      statusReportRequest.ActiveText = StatusReportRequestTasks.Resources.ReportFromJob;
      
      if (task.IsCompoundActionItem ?? false)
      {
        if (performers.Count == 1)
          statusReportRequest.Assignee = Company.Employees.As(performers.First());
      }
      else
        statusReportRequest.Assignee = task.Assignee;
      
      return statusReportRequest;
    }

    /// <summary>
    /// Создать Запрос отчета.
    /// </summary>
    /// <param name="job">Задание по поручению, для которого нужен отчет.</param>
    /// <returns>Задача "Запрос отчета по поручению".</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IStatusReportRequestTask CreateStatusReportRequest(IActionItemExecutionAssignment job)
    {
      var performers = Functions.ActionItemExecutionAssignment.GetActionItemsAssignees(job).ToList();
      if (!performers.Any())
        return null;
      
      var statusReportRequest = StatusReportRequestTasks.CreateAsSubtask(job);
      statusReportRequest.ActionItem = job.ActionItem;
      var document = job.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        statusReportRequest.DocumentsGroup.OfficialDocuments.Add(document);
      statusReportRequest.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubject(statusReportRequest, StatusReportRequestTasks.Resources.ReportRequestTaskSubject);
      statusReportRequest.ActiveText = StatusReportRequestTasks.Resources.ReportFromJob;
      
      if (performers.Count == 1)
        statusReportRequest.Assignee = Company.Employees.As(performers.First());

      statusReportRequest.Author = job.Performer;
      return statusReportRequest;
    }
    
    /// <summary>
    /// Выдать права на задание.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="assignment">Задание.</param>
    public static void GrantRightToAssignment(ITask task, IAssignment assignment)
    {
      // Выдать права на задание контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
      var leadPerformers = Functions.ActionItemExecutionTask.GetLeadActionItemExecutionPerformers(ActionItemExecutionTasks.As(task));
      foreach (var performer in leadPerformers)
        assignment.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
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

      var statusReport = StatusReportRequestTasks.As(_obj);
      if (statusReport == null)
        return assignees;
      
      if (statusReport.Assignee != null)
        assignees.Add(statusReport.Assignee);
      
      return assignees.Distinct().ToList();
    }       
  }
}
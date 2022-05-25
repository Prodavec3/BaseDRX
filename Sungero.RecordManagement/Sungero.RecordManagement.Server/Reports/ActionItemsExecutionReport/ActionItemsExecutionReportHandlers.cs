using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement
{
  partial class ActionItemsExecutionReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ActionItemsExecutionReport.SourceTableName, ActionItemsExecutionReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      #region Параметры и дата выполнения отчета
      
      ActionItemsExecutionReport.ReportSessionId = Guid.NewGuid().ToString();
      ActionItemsExecutionReport.ReportDate = Calendar.Now;
      
      if (string.IsNullOrEmpty(ActionItemsExecutionReport.Header))
      {
        if (ActionItemsExecutionReport.Meeting == null && ActionItemsExecutionReport.IsMeetingsCoverContext == true)
          ActionItemsExecutionReport.Header = Sungero.RecordManagement.Resources.ActionItemsExecutionReportForMeetings;
        else
          ActionItemsExecutionReport.Header = Sungero.RecordManagement.Resources.ActionItemsExecutionReport;
      }
      
      if (ActionItemsExecutionReport.Meeting != null)
      {
        ActionItemsExecutionReport.Subheader = Reports.Resources.ActionItemsExecutionReport.HeaderMeetingFormat(ActionItemsExecutionReport.Meeting.Name);
        
        if (ActionItemsExecutionReport.IsMeetingsCoverContext == true)
        {
          var deadlineHeaderString = Reports.Resources.ActionItemsExecutionReport.HeaderDeadlineFormat(ActionItemsExecutionReport.BeginDate.Value.ToShortDateString(),
                                                                                                       ActionItemsExecutionReport.ClientEndDate.Value.ToShortDateString());
          ActionItemsExecutionReport.Subheader += string.Format("\n{0}", deadlineHeaderString);
        }
      }
      else if (ActionItemsExecutionReport.Document == null)
        ActionItemsExecutionReport.Subheader = Reports.Resources.ActionItemsExecutionReport.HeaderDeadlineFormat(ActionItemsExecutionReport.BeginDate.Value.ToShortDateString(),
                                                                                                                 ActionItemsExecutionReport.ClientEndDate.Value.ToShortDateString());
      else
        ActionItemsExecutionReport.Subheader = Reports.Resources.ActionItemsExecutionReport.HeaderDocumentFormat(ActionItemsExecutionReport.Document.Name);
      
      if (ActionItemsExecutionReport.Author != null)
        ActionItemsExecutionReport.ParamsDescriprion += Reports.Resources.ActionItemsExecutionReport.FilterAuthorFormat(ActionItemsExecutionReport.Author.Person.ShortName,
                                                                                                                        System.Environment.NewLine);
      
      if (ActionItemsExecutionReport.BusinessUnit != null)
        ActionItemsExecutionReport.ParamsDescriprion += Reports.Resources.ActionItemsExecutionReport.FilterBusinessUnitFormat(ActionItemsExecutionReport.BusinessUnit.Name,
                                                                                                                              System.Environment.NewLine);
      
      if (ActionItemsExecutionReport.Department != null)
        ActionItemsExecutionReport.ParamsDescriprion += Reports.Resources.ActionItemsExecutionReport.FilterDepartmentFormat(ActionItemsExecutionReport.Department.Name,
                                                                                                                            System.Environment.NewLine);
      
      if (ActionItemsExecutionReport.Performer != null)
      {
        var performerName = Employees.Is(ActionItemsExecutionReport.Performer) ?
          Employees.As(ActionItemsExecutionReport.Performer).Person.ShortName :
          ActionItemsExecutionReport.Performer.Name;
        ActionItemsExecutionReport.ParamsDescriprion += Reports.Resources.ActionItemsExecutionReport.FilterResponsibleFormat(performerName,
                                                                                                                             System.Environment.NewLine);
      }
      
      #endregion
      
      #region Расчет итогов
      var actionItems = Functions.Module.GetActionItemCompletionData(ActionItemsExecutionReport.Meeting,
                                                                     ActionItemsExecutionReport.Document,
                                                                     ActionItemsExecutionReport.BeginDate,
                                                                     ActionItemsExecutionReport.EndDate,
                                                                     ActionItemsExecutionReport.Author,
                                                                     ActionItemsExecutionReport.BusinessUnit,
                                                                     ActionItemsExecutionReport.Department,
                                                                     ActionItemsExecutionReport.Performer,
                                                                     ActionItemsExecutionReport.DocumentType,
                                                                     ActionItemsExecutionReport.IsMeetingsCoverContext,
                                                                     true);
      ActionItemsExecutionReport.TotalCount = actionItems.Count();
      ActionItemsExecutionReport.Completed = actionItems.Where(j => j.Status == Workflow.Task.Status.Completed).Count();
      ActionItemsExecutionReport.CompletedInTime = actionItems
        .Where(j => j.Status == Workflow.Task.Status.Completed)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, j.ActualDate.Value, j.Assignee) == 0).Count();
      ActionItemsExecutionReport.CompletedOverdue = actionItems
        .Where(j => j.Status == Workflow.Task.Status.Completed)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, j.ActualDate.Value, j.Assignee) > 0).Count();
      ActionItemsExecutionReport.InProcess = actionItems.Where(j => j.Status == Workflow.Task.Status.InProcess).Count();
      
      ActionItemsExecutionReport.InProcessOverdue = actionItems
        .Where(j => j.Status == Workflow.Task.Status.InProcess)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, Calendar.Now, j.Assignee) > 0).Count();
      
      if (ActionItemsExecutionReport.TotalCount != 0)
      {
        var inTimeActionItems = ActionItemsExecutionReport.CompletedInTime + ActionItemsExecutionReport.InProcess - ActionItemsExecutionReport.InProcessOverdue;
        ActionItemsExecutionReport.ExecutiveDisciplineLevel =
          string.Format("{0:P2}", inTimeActionItems / (double)ActionItemsExecutionReport.TotalCount);
      }
      else
        ActionItemsExecutionReport.ExecutiveDisciplineLevel = Reports.Resources.ActionItemsExecutionReport.NoAnyActionItems;
      
      #endregion
      
      var dataTable = new List<Structures.ActionItemsExecutionReport.TableLine>();
      foreach (var actionItem in actionItems.OrderBy(a => a.Deadline))
      {
        var tableLine = Structures.ActionItemsExecutionReport.TableLine.Create();
        
        // ИД и ссылка.
        tableLine.Id = actionItem.Id;
        tableLine.Hyperlink = Core.Hyperlinks.Get(ActionItemExecutionTasks.Info, actionItem.Id);
        
        // Поручение.
        tableLine.ActionItemText = actionItem.ActionItem;
        
        // Автор.
        var author = Employees.As(actionItem.Author);
        if (author != null && author.Person != null)
          tableLine.Author = author.Person.ShortName;
        else
          tableLine.Author = actionItem.Author.Name;
        
        // TODO Lomagin: Убрать замену на пробел после исправления 33360. Сделано в рамках бага 33343.
        // Сделано для корректного переноса инициалов, если фамилия длинная.
        tableLine.Author = tableLine.Author.Replace("\u00A0", " ");
        
        // Статус.
        tableLine.State = string.Empty;
        if (actionItem.ExecutionState != null)
          tableLine.State = ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(actionItem.ExecutionState.Value);
        
        // Даты и просрочка.
        tableLine.PlanDate = string.Empty;
        if (actionItem.Deadline.HasValue)
        {
          var deadline = Calendar.ToUserTime(actionItem.Deadline.Value);
          tableLine.PlanDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline);
          tableLine.PlanDateSort = actionItem.Deadline.Value;
        }
        tableLine.ActualDate = string.Empty;
        var isCompleted = actionItem.Status == Sungero.Workflow.Task.Status.Completed;
        if (isCompleted)
        {
          var endDate = actionItem.ActualDate.HasValue ? actionItem.ActualDate.Value : Calendar.Now;
          tableLine.ActualDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(Calendar.ToUserTime(endDate));
          tableLine.Overdue = Docflow.PublicFunctions.Module.CalculateDelay(actionItem.Deadline, endDate, actionItem.Assignee);
        }
        else
          tableLine.Overdue = Docflow.PublicFunctions.Module.CalculateDelay(actionItem.Deadline, Calendar.Now, actionItem.Assignee);
        
        // Исполнители.
        tableLine.Assignee = actionItem.Assignee.Person.ShortName;
        
        tableLine.CoAssignees = string.Join(", ", actionItem.CoAssigneesShortNames);
        
        tableLine.ReportSessionId = ActionItemsExecutionReport.ReportSessionId;
        
        dataTable.Add(tableLine);
      }
      
      var dataTableSort = dataTable.Where(d => !string.IsNullOrEmpty(d.PlanDate)).OrderBy(d => d.PlanDateSort).ToList();
      dataTableSort.AddRange(dataTable.Where(d => string.IsNullOrEmpty(d.PlanDate)).OrderBy(d => d.Id).ToList());
      var rowIndex = 1;
      foreach (var item in dataTableSort)
      {
        item.RowIndex = rowIndex++;
      }
      
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ActionItemsExecutionReport.SourceTableName, dataTableSort);
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        // Заполнить таблицу именами документов.
        command.CommandText = string.Format(Queries.ActionItemsExecutionReport.PasteDocumentNames, Constants.ActionItemsExecutionReport.SourceTableName, ActionItemsExecutionReport.ReportSessionId);
        command.ExecuteNonQuery();
      }
    }
  }
}
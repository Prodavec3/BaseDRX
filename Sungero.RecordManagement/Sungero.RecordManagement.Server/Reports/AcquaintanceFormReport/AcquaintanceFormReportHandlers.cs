using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow.Task;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceFormReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.AcquaintanceFormReport.SourceTableName, AcquaintanceFormReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var task = AcquaintanceFormReport.Task;
      var taskId = task.Id;
      var isElectronicAcquaintance = task.IsElectronicAcquaintance == true;
      var sourceDocument = task.DocumentGroup.OfficialDocuments.First();
      var versionNumber = Functions.AcquaintanceTask.GetDocumentVersion(task);
      var nonBreakingSpace = Convert.ToChar(160);
      
      // Шапка.
      AcquaintanceFormReport.DocumentName = Docflow.PublicFunctions.Module.FormatDocumentNameForReport(sourceDocument, versionNumber, false);
      
      // Приложения.
      var documentAddenda = Functions.Module.GetAcquintanceTaskAddendas(task);
      if (documentAddenda.Any())
      {
        AcquaintanceFormReport.AddendaDescription = Reports.Resources.AcquaintanceReport.Addendas;
        foreach (var addendum in documentAddenda)
        {
          var addendumInfo = string.Format("\n - {0} ({1}:{2}{3}).", addendum.DisplayValue.Trim(),
                                          Docflow.Resources.Id, nonBreakingSpace, addendum.Id);
          AcquaintanceFormReport.AddendaDescription += addendumInfo;
        }
      }
      
      // Данные.
      var reportSessionId = System.Guid.NewGuid().ToString();
      AcquaintanceFormReport.ReportSessionId = reportSessionId;
      var dataTable = new List<Structures.AcquaintanceFormReport.TableLine>();
      var employees = GetEmployeesFromParticipants(task);
      foreach (var employee in employees)
      {
        var newLine = Structures.AcquaintanceFormReport.TableLine.Create();
        newLine.ShortName = employee.Person.ShortName;
        newLine.LastName = employee.Person.LastName;
        if (employee.JobTitle != null)
          newLine.JobTitle = employee.JobTitle.DisplayValue;
        newLine.Department = employee.Department.DisplayValue;
        newLine.RowNumber = 0;
        newLine.ReportSessionId = reportSessionId;
        dataTable.Add(newLine);
      }
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.AcquaintanceFormReport.SourceTableName, dataTable);
      
      // Подвал.
      var currentUser = Users.Current;
      var printedByName = Employees.Is(currentUser)
        ? Employees.As(currentUser).Person.ShortName
        : currentUser.Name;
      AcquaintanceFormReport.Printed = Reports.Resources.AcquaintanceReport.PrintedByFormat(printedByName, Calendar.UserNow);
    }
    
    /// <summary>
    /// Получить список конечных исполнителей ознакомления на момент отправки.
    /// </summary>
    /// <param name="task">Ознакомление.</param>
    /// <returns>Список сотрудников на момент отправки задачи.</returns>
    public static IEnumerable<IEmployee> GetEmployeesFromParticipants(IAcquaintanceTask task)
    {
      // Заполнение AcquaintanceTaskParticipants происходит в схеме.
      // От старта задачи до начала обработки схемы там ничего не будет - взять из исполнителей задачи.
      var storedParticipants = AcquaintanceTaskParticipants.GetAll().FirstOrDefault(x => x.TaskId == task.Id);
      if (storedParticipants != null)
        return storedParticipants.Employees.Select(p => p.Employee).ToList();
      
      return Functions.AcquaintanceTask.GetParticipants(task);
    }
  }
}
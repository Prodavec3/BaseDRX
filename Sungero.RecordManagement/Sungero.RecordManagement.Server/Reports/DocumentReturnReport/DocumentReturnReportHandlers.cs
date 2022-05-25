using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.WorkingTimeCalendar;

namespace Sungero.RecordManagement
{
  partial class DocumentReturnReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {     
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.DocumentReturnReport.SourceTableName, DocumentReturnReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      // Задать минимальную и максимальную даты. Формат yyyy-MM-dd нужен для правильного парсинга на ru и en локализациях.
      DocumentReturnReport.MinDeliveryDate = DateTime.Parse("1753-01-01");
      DocumentReturnReport.MaxDeliveryDate = DateTime.Parse("9999-12-31");
      
      // Отфильтровать доступные документы.
      var documents = Docflow.OfficialDocuments.GetAll()
        .Where(d => d.IsReturnRequired.HasValue)
        .Where(d => d.Tracking.Any(l => (!l.ReturnDate.HasValue || l.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.AtControl) && l.ExternalLinkId == null));
      
      // Фильтр по дате выдачи.
      if (DocumentReturnReport.DeliveryDateFrom.HasValue)
        documents = documents.Where(d => d.Tracking.Any(l => l.DeliveryDate >= DocumentReturnReport.DeliveryDateFrom && !l.ReturnDate.HasValue));
      if (DocumentReturnReport.DeliveryDateTo.HasValue)
        documents = documents.Where(d => d.Tracking.Any(l => l.DeliveryDate <= DocumentReturnReport.DeliveryDateTo && !l.ReturnDate.HasValue));
      
      // Фильтр по НОР.
      if (DocumentReturnReport.BusinessUnit != null)
        documents = documents.Where(d => Equals(d.BusinessUnit, DocumentReturnReport.BusinessUnit));
      
      // Фильтр по подразделению сотрудника.
      if (DocumentReturnReport.Department != null)
        documents = documents.Where(d => d.Tracking.Any(l => Equals(l.DeliveredTo.Department, DocumentReturnReport.Department) && !l.ReturnDate.HasValue));
      
      // Фильтр по сотруднику.
      if (DocumentReturnReport.Employee != null)
        documents = documents.Where(d => d.Tracking.Any(l => Equals(l.DeliveredTo, DocumentReturnReport.Employee) && !l.ReturnDate.HasValue));
      
      var reportSessionId = System.Guid.NewGuid().ToString();
      DocumentReturnReport.ReportSessionId = reportSessionId;
      
      // Заполнить данные.
      var dataTable = new List<Structures.DocumentReturnReport.DocumentReturnTableLine>();
      foreach (var document in documents)
      {
        // Заполнить информацию о выдачах документа.
        var tracking = document.Tracking.Where(l => (!l.ReturnDate.HasValue || l.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.AtControl) && l.ExternalLinkId == null);
        var docHyperlink = Hyperlinks.Get(document);
        
        foreach (var location in tracking)
        {
          var tableLine = Structures.DocumentReturnReport.DocumentReturnTableLine.Create();
          tableLine.ReportSessionId = reportSessionId;
          tableLine.DocId = document.Id;
          tableLine.DocName = document.Name;
          tableLine.OriginalOrCopy = location.IsOriginal.Value ? Reports.Resources.DocumentReturnReport.Original : Reports.Resources.DocumentReturnReport.Copy;
          tableLine.Hyperlink = docHyperlink;
          
          // Заполнить информацию по дате выдаче и просрочке.
          var today = Calendar.UserToday;
          tableLine.DeliveryDate = location.DeliveryDate.Value.ToString("d");
          var returnDeadline = location.ReturnDeadline;
          tableLine.ScheduledReturnDate = returnDeadline.HasValue ? returnDeadline.Value.ToString("d") : "-";
          var scheduledReturnDate = returnDeadline ?? today.AddDays(1);
          tableLine.OverdueDelay = Docflow.PublicFunctions.Module.CalculateDelay(scheduledReturnDate, today, location.DeliveredTo);
          
          // Заполнить информацию о сотруднике.
          var employee = location.DeliveredTo;
          tableLine.FullName = employee.Person.ShortName ?? employee.Name;
          tableLine.DepId = employee.Department.Id;
          tableLine.DepName = employee.Department.Name;
          dataTable.Add(tableLine);
        }
      }
      
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.DocumentReturnReport.SourceTableName, dataTable);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class ActionItemPrintReportServerHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      // ИД отчета.
      var reportId = Guid.NewGuid().ToString();
      ActionItemPrintReport.ReportSessionId = reportId;
      
      var task = ActionItemPrintReport.Task;
      var assignment = ActionItemPrintReport.Assignment;
      var author = task.Assignee;
      ActionItemPrintReport.Author = Company.PublicFunctions.Employee.GetShortName(author, DeclensionCase.Nominative, false);
      var currentUser = Users.Current;
      ActionItemPrintReport.PrintedBy = Sungero.Company.Employees.Is(currentUser)
        ? Company.PublicFunctions.Employee.GetShortName(Sungero.Company.Employees.Current,
                                                        DeclensionCase.Nominative,
                                                        false)
        : currentUser.Name;
      
      // Номер и дата документа.
      var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      ActionItemPrintReport.DocumentShortName = PublicFunctions.Module.GetActionItemPrintReportDocumentShortName(document, assignment);
      
      // Получить данные для отчета.
      var reportData = PublicFunctions.Module.GetActionItemPrintReportData(task, reportId);
      
      // Записать данные в таблицу.
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ActionItemPrintReport.SourceTableName, reportData);
    }
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ActionItemPrintReport.SourceTableName, ActionItemPrintReport.ReportSessionId);
    }
  }
}

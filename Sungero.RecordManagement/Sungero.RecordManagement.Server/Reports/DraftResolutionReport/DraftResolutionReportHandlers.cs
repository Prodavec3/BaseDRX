using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class DraftResolutionReportServerHandlers
  {
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      // ИД отчета.
      var reportSessionId = Guid.NewGuid().ToString();
      DraftResolutionReport.ReportSessionId = reportSessionId;
      
      // Автор.
      var author = DraftResolutionReport.Author;
      if (author != null)
      {
        if (author.JobTitle != null)
          DraftResolutionReport.AuthorJobTitle = author.JobTitle.Name;
        
        DraftResolutionReport.AuthorShortName = Company.PublicFunctions.Employee.GetReverseShortName(author);
      }
      
      // Номер и дата документа.
      var document = DraftResolutionReport.Document;
      DraftResolutionReport.DocumentShortName = PublicFunctions.Module.GetDraftResolutionReportDocumentShortName(document);
      
      // НОР.
      DraftResolutionReport.BusinessUnit = string.Empty;
      if (document != null && document.BusinessUnit != null)
      {
        DraftResolutionReport.BusinessUnit = document.BusinessUnit.Name;
      }
      else if (author.Department != null && author.Department.BusinessUnit != null)
      {
        DraftResolutionReport.BusinessUnit = author.Department.BusinessUnit.Name;
      }
      
      // Получить данные для отчета.
      var actionItems = DraftResolutionReport.Resolution.ToList();
      var reportData = PublicFunctions.Module.GetDraftResolutionReportData(actionItems, reportSessionId, DraftResolutionReport.TextResolution);
      
      // Записать данные в таблицу.
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.DraftResolutionReport.SourceTableName, reportData);
    }
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.DraftResolutionReport.SourceTableName, DraftResolutionReport.ReportSessionId);
    }
  }
}
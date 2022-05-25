using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class IncomingDocumentsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsReport.AvailableDocumentsIdsTableName, IncomingDocumentsReport.DocumentsDataTableName, IncomingDocumentsReport.JobsDataTableName });
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName, IncomingDocumentsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      IncomingDocumentsReport.DocumentsDataTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id);
      IncomingDocumentsReport.JobsDataTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id, "JobsList");
      IncomingDocumentsReport.AvailableDocumentsIdsTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id, "Access");
            
      var reportSessionId = System.Guid.NewGuid().ToString();
      IncomingDocumentsReport.ReportSessionId = reportSessionId;      
      
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsReport.DocumentsDataTableName, IncomingDocumentsReport.JobsDataTableName, IncomingDocumentsReport.AvailableDocumentsIdsTableName });
      
      // Создать таблицу писем для учета прав.
      var availableDocumentsIds = Sungero.Docflow.IncomingDocumentBases.GetAll()
        .Where(d => Equals(d.DocumentRegister, IncomingDocumentsReport.DocumentRegister))
        .Where(d => d.RegistrationDate >= IncomingDocumentsReport.BeginDate.Value)
        .Where(d => d.RegistrationDate <= IncomingDocumentsReport.EndDate.Value)
        .Select(d => d.Id);
      Docflow.PublicFunctions.Module.CreateIncomingDocumentsReportTempTable(IncomingDocumentsReport.AvailableDocumentsIdsTableName, availableDocumentsIds);
      
      // Заполнить таблицы данных.
      var commandText = Queries.IncomingDocumentsReport.FillTempTables;
      Functions.Module.ExecuteSQLCommandFormat(commandText, new string[] { IncomingDocumentsReport.DocumentsDataTableName, IncomingDocumentsReport.JobsDataTableName, IncomingDocumentsReport.AvailableDocumentsIdsTableName });
     
      // Заполнить сурс таблицу.
      commandText = Queries.IncomingDocumentsReport.FillSourceTable;
      Functions.Module.ExecuteSQLCommandFormat(commandText, new string[] { IncomingDocumentsReport.DocumentsDataTableName, IncomingDocumentsReport.JobsDataTableName, Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName, reportSessionId });  
    
      // Удалить таблицу с правами.
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsReport.AvailableDocumentsIdsTableName });
    }
  }
}
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
      Docflow.PublicFunctions.Module.DropReportTempTables(
        new[] {
          IncomingDocumentsReport.AvailableDocumentsIdsTableName,
          IncomingDocumentsReport.AllIncomingDocumentsIdsTableName,
          IncomingDocumentsReport.DocumentsDataTableName,
          IncomingDocumentsReport.JobsDataTableName
        });
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName, IncomingDocumentsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      IncomingDocumentsReport.DocumentsDataTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id);
      IncomingDocumentsReport.JobsDataTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id, "JobsList");
      IncomingDocumentsReport.AvailableDocumentsIdsTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id, "Access");
      IncomingDocumentsReport.AllIncomingDocumentsIdsTableName = Docflow.PublicFunctions.Module.GetReportTableName(IncomingDocumentsReport, Users.Current.Id, "All");
      
      var reportSessionId = System.Guid.NewGuid().ToString();
      IncomingDocumentsReport.ReportSessionId = reportSessionId;
      
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DropReportTempTables(
        new[] 
        {
          IncomingDocumentsReport.DocumentsDataTableName,
          IncomingDocumentsReport.JobsDataTableName,
          IncomingDocumentsReport.AvailableDocumentsIdsTableName,
          IncomingDocumentsReport.AllIncomingDocumentsIdsTableName
        });
      
      // Создать таблицу входящих документов для учета прав.
      var availableDocumentsIds = this.GetIncomingDocuments();
      Docflow.PublicFunctions.Module.CreateIncomingDocumentsReportTempTable(IncomingDocumentsReport.AvailableDocumentsIdsTableName, availableDocumentsIds);
      
      // Создать таблицу входящих документов без учета прав.
      var allIncomingDocumentsIds = Enumerable.Empty<int>().AsQueryable();
      AccessRights.AllowRead(() => { allIncomingDocumentsIds = this.GetIncomingDocuments(); });
      Docflow.PublicFunctions.Module.CreateIncomingDocumentsReportTempTable(IncomingDocumentsReport.AllIncomingDocumentsIdsTableName, allIncomingDocumentsIds);
      
      // Заполнить таблицы данных.
      var commandText = Queries.IncomingDocumentsReport.FillTempTables;
      var tempTableParams = new string[] {
        IncomingDocumentsReport.DocumentsDataTableName,
        IncomingDocumentsReport.JobsDataTableName,
        IncomingDocumentsReport.AvailableDocumentsIdsTableName,
        IncomingDocumentsReport.AllIncomingDocumentsIdsTableName
      };
      Functions.Module.ExecuteSQLCommandFormat(commandText, tempTableParams);
      
      // Заполнить исходную таблицу.
      commandText = Queries.IncomingDocumentsReport.FillSourceTable;
      var sourceTableParams = new string[] {
        IncomingDocumentsReport.DocumentsDataTableName,
        IncomingDocumentsReport.JobsDataTableName,
        Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName,
        reportSessionId };
      Functions.Module.ExecuteSQLCommandFormat(commandText, sourceTableParams);
      
      // Удалить таблицу с правами.
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { IncomingDocumentsReport.AvailableDocumentsIdsTableName, IncomingDocumentsReport.AllIncomingDocumentsIdsTableName });
    }
    
    /// <summary>
    /// Получить ИД входящих документов для журнала.
    /// </summary>
    /// <returns>ИД входящих документов.</returns>
    public virtual IQueryable<int> GetIncomingDocuments()
    {
      return Sungero.Docflow.IncomingDocumentBases.GetAll()
        .Where(d => Equals(d.DocumentRegister, IncomingDocumentsReport.DocumentRegister))
        .Where(d => d.RegistrationDate >= IncomingDocumentsReport.BeginDate.Value)
        .Where(d => d.RegistrationDate <= IncomingDocumentsReport.EndDate.Value)
        .Select(d => d.Id);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class MailRegisterReportServerHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var documents = MailRegisterReport.OutgoingDocuments;
      MailRegisterReport.TotalMailCount = documents.Sum(d => d.Addressees.Count).ToString();
      MailRegisterReport.FromName = documents.First().BusinessUnit.Name;
      MailRegisterReport.ReportSessionId = System.Guid.NewGuid().ToString();
        
      var dataTable = new List<Structures.MailRegisterReport.TableLine>();
      foreach (var document in documents)
      {
        foreach (var addressee in document.Addressees.OrderBy(a => a.Number))
        {
          var tableLine = Structures.MailRegisterReport.TableLine.Create();
          tableLine.ReportSessionId = MailRegisterReport.ReportSessionId;
          tableLine.PostAddress = addressee.Correspondent.PostalAddress ?? addressee.Correspondent.LegalAddress ?? string.Empty;
          tableLine.ToName = addressee.Correspondent.Name;
          dataTable.Add(tableLine);
        }
      }

      Functions.Module.WriteStructuresToTable(Constants.MailRegisterReport.SourceTableName, dataTable);
    }

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить данные из временной таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.MailRegisterReport.SourceTableName, MailRegisterReport.ReportSessionId);
    }
    
  }
}
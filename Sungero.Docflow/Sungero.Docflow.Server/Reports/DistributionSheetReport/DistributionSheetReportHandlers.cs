using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class DistributionSheetReportServerHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var document = DistributionSheetReport.OutgoingDocument;
      DistributionSheetReport.ReportSessionId = System.Guid.NewGuid().ToString();

      // Шапка.
      DistributionSheetReport.LetterSubject = Docflow.PublicFunctions.Module.FormatDocumentNameForReport(document, false);
      
      var dataTable = new List<Structures.DistributionSheetReport.TableLine>();
      foreach (var addressee in document.Addressees.OrderBy(a => a.Number))
      {
        var tableLine = Structures.DistributionSheetReport.TableLine.Create();
        tableLine.ReportSessionId = DistributionSheetReport.ReportSessionId;
        tableLine.CompanyName = addressee.Correspondent.Name;
        tableLine.NameWithJobTitle = addressee.Addressee != null && !string.IsNullOrEmpty(addressee.Addressee.JobTitle)
          ? string.Concat(addressee.Addressee.JobTitle, System.Environment.NewLine)
          : string.Empty;
        tableLine.NameWithJobTitle += addressee.Addressee != null ? addressee.Addressee.Name : string.Empty;
        tableLine.DeliveryMethod = addressee.DeliveryMethod != null 
          ? addressee.DeliveryMethod.Name
          : string.Empty;
        tableLine.ContactsInformation = Docflow.PublicFunctions.OutgoingDocumentBase.GetContactsInformation(addressee, document);
        
        dataTable.Add(tableLine);
      }
      
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.DistributionSheetReport.SourceTableName, dataTable);
    }
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить данные из временной таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.DistributionSheetReport.SourceTableName, DistributionSheetReport.ReportSessionId);
    }
  }
}
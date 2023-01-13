using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using ExchDocumentType = Sungero.Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType;
using MessageType = Sungero.Exchange.ExchangeDocumentInfo.MessageType;
using ReportResources = Sungero.Docflow.Reports.Resources;

namespace Sungero.Docflow
{
  partial class ExchangeOrderReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ExchangeOrderReport.SourceTableName, ExchangeOrderReport.SessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      ExchangeOrderReport.SessionId = reportSessionId;
      
      var document = ExchangeOrderReport.Entity;
      ExchangeOrderReport.DocumentName = document.Name;
      
      var exchangeInfo = Functions.Module.GetExchangeOrderInfo(reportSessionId, document);
      
      var dataTable = exchangeInfo.ExchangeOrderInfo;
      ExchangeOrderReport.CompletationString = Reports.Resources.ExchangeOrderReport.Docflow + " ";
      var completionString = exchangeInfo.IsComplete ? Reports.Resources.ExchangeOrderReport.Completed : Reports.Resources.ExchangeOrderReport.NotCompleted;
      ExchangeOrderReport.CompletationString += completionString;

      Functions.Module.WriteStructuresToTable(Constants.ExchangeOrderReport.SourceTableName, dataTable);
    }
    
    private static string DateFormat(DateTime? datetime)
    {
      if (datetime == null)
        return null;
      
      return Functions.Module.ToTenantTime(datetime.Value).ToUserTime().ToString("g");
    }
  }
}
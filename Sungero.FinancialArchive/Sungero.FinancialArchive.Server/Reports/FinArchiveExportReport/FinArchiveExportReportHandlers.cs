using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchive
{
  partial class FinArchiveExportReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.FinArchiveExportReport.SourceTableName, FinArchiveExportReport.ReportSessionId);
    }
  }
}
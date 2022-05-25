using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class IncomingDocumentsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      DateTime? beginDate = IncomingDocumentsReport.BeginDate;
      DateTime? endDate = IncomingDocumentsReport.EndDate;
      var documentRegister = IncomingDocumentsReport.DocumentRegister;
      bool needRun = true;
      
      if (!IncomingDocumentsReport.BeginDate.HasValue && !IncomingDocumentsReport.EndDate.HasValue)
      {
        var dialogResult = Functions.Module.ShowDocumentRegisterReportDialog(Resources.IncomingDocumentsReport,
                                                                             Docflow.DocumentRegister.DocumentFlow.Incoming,
                                                                             documentRegister, Constants.IncomingDocumentsReport.HelpCode);
        needRun = dialogResult.RunReport;
        beginDate = dialogResult.PeriodBegin;
        endDate = dialogResult.PeriodEnd;
        documentRegister = dialogResult.DocumentRegister;
      }

      if (needRun)
      {
        IncomingDocumentsReport.BeginDate = beginDate.Value;
        IncomingDocumentsReport.EndDate = endDate.Value;
        IncomingDocumentsReport.DocumentRegister = documentRegister;
      }
      else
        e.Cancel = true;
    }
  }
}
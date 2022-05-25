using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class OutgoingDocumentsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      DateTime? beginDate = OutgoingDocumentsReport.BeginDate;
      DateTime? endDate = OutgoingDocumentsReport.EndDate;
      var documentRegister = OutgoingDocumentsReport.DocumentRegister;
      bool needRun = true;
      
      if (!beginDate.HasValue && !endDate.HasValue)
      {
        var dialogResult = Functions.Module.ShowDocumentRegisterReportDialog(Resources.OutgoingDocumentsReport,
                                                                             Docflow.DocumentRegister.DocumentFlow.Outgoing,
                                                                             documentRegister, Constants.OutgoingDocumentsReport.HelpCode);
        needRun = dialogResult.RunReport;
        beginDate = dialogResult.PeriodBegin;
        endDate = dialogResult.PeriodEnd;
        documentRegister = dialogResult.DocumentRegister;
      }

      if (needRun)
      {
        OutgoingDocumentsReport.BeginDate = beginDate.Value;
        OutgoingDocumentsReport.EndDate = endDate.Value;
        OutgoingDocumentsReport.DocumentRegister = documentRegister;
      }
      else
        e.Cancel = true;
    }
  }
}
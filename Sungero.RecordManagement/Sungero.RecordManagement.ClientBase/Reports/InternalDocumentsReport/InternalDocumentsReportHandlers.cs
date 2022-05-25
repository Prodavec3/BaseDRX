using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class InternalDocumentsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      DateTime? beginDate = InternalDocumentsReport.BeginDate;
      DateTime? endDate = InternalDocumentsReport.EndDate;
      var documentRegister = InternalDocumentsReport.DocumentRegister;
      bool needRun = true;
      
      if (!beginDate.HasValue && !endDate.HasValue)
      {
        var dialogResult = Functions.Module.ShowDocumentRegisterReportDialog(Resources.InternalDocumentsReport,
                                                                             Docflow.DocumentRegister.DocumentFlow.Inner,
                                                                             documentRegister, Constants.InternalDocumentsReport.HelpCode);
        needRun = dialogResult.RunReport;
        beginDate = dialogResult.PeriodBegin;
        endDate = dialogResult.PeriodEnd;
        documentRegister = dialogResult.DocumentRegister;
      }
      
      if (needRun)
      {
        InternalDocumentsReport.BeginDate = beginDate.Value;
        InternalDocumentsReport.EndDate = endDate.Value;
        InternalDocumentsReport.DocumentRegister = documentRegister;
      }
      else
        e.Cancel = true;
    }
  }
}
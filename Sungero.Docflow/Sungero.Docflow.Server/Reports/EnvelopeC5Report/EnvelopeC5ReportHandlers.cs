using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{

  partial class EnvelopeC5ReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC5Report.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      EnvelopeC5Report.ReportSessionId = Guid.NewGuid().ToString();
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC5Report.ReportSessionId);
      Functions.Module.FillEnvelopeTable(EnvelopeC5Report.ReportSessionId,
                                         EnvelopeC5Report.OutgoingDocuments.ToList(),
                                         EnvelopeC5Report.ContractualDocuments.ToList(),
                                         EnvelopeC5Report.AccountingDocuments.ToList());
    }

  }
}
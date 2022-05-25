using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EnvelopeC4ReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC4Report.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      EnvelopeC4Report.ReportSessionId = Guid.NewGuid().ToString();
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC4Report.ReportSessionId);
      Functions.Module.FillEnvelopeTable(EnvelopeC4Report.ReportSessionId,
                                         EnvelopeC4Report.OutgoingDocuments.ToList(),
                                         EnvelopeC4Report.ContractualDocuments.ToList(),
                                         EnvelopeC4Report.AccountingDocuments.ToList());
    }

  }
}
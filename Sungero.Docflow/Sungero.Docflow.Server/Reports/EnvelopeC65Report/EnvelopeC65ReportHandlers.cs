using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EnvelopeC65ReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC65Report.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      EnvelopeC65Report.ReportSessionId = Guid.NewGuid().ToString();
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeC65Report.ReportSessionId);
      Functions.Module.FillEnvelopeTable(EnvelopeC65Report.ReportSessionId,
                                         EnvelopeC65Report.OutgoingDocuments.ToList(),
                                         EnvelopeC65Report.ContractualDocuments.ToList(),
                                         EnvelopeC65Report.AccountingDocuments.ToList());
    }

  }
}
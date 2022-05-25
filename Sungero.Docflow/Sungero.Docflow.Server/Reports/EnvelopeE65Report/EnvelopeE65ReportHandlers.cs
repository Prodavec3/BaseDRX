using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EnvelopeE65ReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeE65Report.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      EnvelopeE65Report.ReportSessionId = Guid.NewGuid().ToString();
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.EnvelopeC4Report.EnvelopesTableName, EnvelopeE65Report.ReportSessionId);
      Functions.Module.FillEnvelopeTable(EnvelopeE65Report.ReportSessionId,
                                         EnvelopeE65Report.OutgoingDocuments.ToList(),
                                         EnvelopeE65Report.ContractualDocuments.ToList(),
                                         EnvelopeE65Report.AccountingDocuments.ToList());
    }

  }
}
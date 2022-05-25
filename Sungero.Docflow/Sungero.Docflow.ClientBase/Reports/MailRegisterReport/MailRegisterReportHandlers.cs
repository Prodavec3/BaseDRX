using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class MailRegisterReportClientHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var differentBusinessUnits = MailRegisterReport.OutgoingDocuments.Select(d => d.BusinessUnit).Distinct().ToList();
      if (differentBusinessUnits.Count > 1)
      {
        Dialogs.ShowMessage(Resources.SelectedMailsHasDifferenBusinessUnits,
                            Resources.SelectOutgoingDocumentsWithSameBusinessUnit,
                            MessageType.Warning);
        e.Cancel = true;
        return;
      }
    }
  }
}
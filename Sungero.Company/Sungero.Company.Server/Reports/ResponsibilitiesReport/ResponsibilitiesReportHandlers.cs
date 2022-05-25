using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ResponsibilitiesReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ResponsibilitiesReport.ResponsibilitiesReportTableName, ResponsibilitiesReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      ResponsibilitiesReport.ReportSessionId = reportSessionId;
      ResponsibilitiesReport.CurrentDate = Calendar.Now;
      var reportData = PublicFunctions.Module.GetAllResponsibilitiesReportData(this.ResponsibilitiesReport.Employee);
      
      foreach (var element in reportData)
        element.ReportSessionId = reportSessionId;
      
      Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ResponsibilitiesReport.ResponsibilitiesReportTableName, reportData);
    }

  }
}
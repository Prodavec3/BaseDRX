using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class DocumentUsageReportServerHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e) 
    {
      DocumentUsageReport.ReportDate = Calendar.Now;
      DocumentUsageReport.DepartmentId = DocumentUsageReport.Department != null ? DocumentUsageReport.Department.Id : 0;
    }

  }
}
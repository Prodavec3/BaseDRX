using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class DepartmentsAssignmentCompletionReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(Constants.DepartmentsAssignmentCompletionReport.SourceTableName, DepartmentsAssignmentCompletionReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      DepartmentsAssignmentCompletionReport.ReportSessionId = reportSessionId;
      DepartmentsAssignmentCompletionReport.ReportDate = Calendar.Now;
      DepartmentsAssignmentCompletionReport.DetailedReportLink = Hyperlinks.Functions.OpenEmployeesAssignmentCompletionReport("HyperlinkBusinessUnitId",
                                                                                                                              int.MaxValue.ToString(),
                                                                                                                              DepartmentsAssignmentCompletionReport.PeriodBegin.ToString(),
                                                                                                                              DepartmentsAssignmentCompletionReport.PeriodEnd.ToString(),
                                                                                                                              "Unwrap");
      
      var departmentIds = DepartmentsAssignmentCompletionReport.DepartmentIds.Where(d => d != null).Select(d => d.Value).ToList();
      var businessUnitIds = DepartmentsAssignmentCompletionReport.BusinessUnitIds.Where(d => d != null).Select(d => d.Value).ToList();
      
      if (DepartmentsAssignmentCompletionReport.WidgetParameter == null)
      {
        if (DepartmentsAssignmentCompletionReport.BusinessUnit != null)
          DepartmentsAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportBusinessUnitFormat(DepartmentsAssignmentCompletionReport.BusinessUnit.Name,
                                                                                                                System.Environment.NewLine);
        
        if (DepartmentsAssignmentCompletionReport.Department != null)
          DepartmentsAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportDepartmentFormat(DepartmentsAssignmentCompletionReport.Department.Name, System.Environment.NewLine);
      }
      else if (!string.IsNullOrEmpty(DepartmentsAssignmentCompletionReport.WidgetParameter))
      {
        DepartmentsAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportEmployeesFormat(DepartmentsAssignmentCompletionReport.WidgetParameter);
      }
      
      var needFilter = DepartmentsAssignmentCompletionReport.BusinessUnit != null || DepartmentsAssignmentCompletionReport.Department != null;
      var tableData = Functions.Module.GetBusinessUnitAssignmentCompletionReportData(businessUnitIds,
                                                                                     departmentIds,
                                                                                     DepartmentsAssignmentCompletionReport.PeriodBegin.Value, DepartmentsAssignmentCompletionReport.PeriodEnd.Value.EndOfDay(),
                                                                                     DepartmentsAssignmentCompletionReport.Unwrap == true,
                                                                                     DepartmentsAssignmentCompletionReport.WithSubstitution == true, needFilter);
      
      var rowIndex = 1;
      foreach (var item in tableData)
      {
        item.RowIndex = rowIndex++;
        item.ReportSessionId = reportSessionId;
      }
      
      Functions.Module.WriteStructuresToTable(Constants.DepartmentsAssignmentCompletionReport.SourceTableName, tableData);
    }

  }
}
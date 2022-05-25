using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EmployeesAssignmentCompletionReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(Constants.EmployeesAssignmentCompletionReport.SourceTableName, EmployeesAssignmentCompletionReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      EmployeesAssignmentCompletionReport.ReportSessionId = reportSessionId;
      EmployeesAssignmentCompletionReport.ReportDate = Calendar.Now;
      EmployeesAssignmentCompletionReport.DetailedReportLink = Hyperlinks.Functions.OpenEmployeeAssignmentsReport(int.MaxValue.ToString(),
                                                                                                                  EmployeesAssignmentCompletionReport.PeriodBegin.ToString(),
                                                                                                                  EmployeesAssignmentCompletionReport.PeriodEnd.ToString());
      
      var departmentIds = EmployeesAssignmentCompletionReport.DepartmentIds.Where(d => d != null).Select(d => d.Value).ToList();
      var businessUnitds = EmployeesAssignmentCompletionReport.BusinessUnitIds.Where(d => d != null).Select(d => d.Value).ToList();
      
      if (EmployeesAssignmentCompletionReport.WidgetParameter == null)
      {
        if (EmployeesAssignmentCompletionReport.BusinessUnit != null)
          EmployeesAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportBusinessUnitFormat(EmployeesAssignmentCompletionReport.BusinessUnit.Name, System.Environment.NewLine);
        
        if (EmployeesAssignmentCompletionReport.Department != null)
          EmployeesAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportDepartmentFormat(EmployeesAssignmentCompletionReport.Department.Name, System.Environment.NewLine);
      }
      else if (!string.IsNullOrEmpty(EmployeesAssignmentCompletionReport.WidgetParameter))
      {
        EmployeesAssignmentCompletionReport.ParamsDescription += Docflow.Resources.ReportEmployeesFormat(EmployeesAssignmentCompletionReport.WidgetParameter);
      }
      
      var needFilter = EmployeesAssignmentCompletionReport.BusinessUnitIds.Any() || departmentIds.Any();
      var tableData = Functions.Module.GetDepartmentAssignmentCompletionReportData(businessUnitds,
                                                                                   departmentIds,
                                                                                   EmployeesAssignmentCompletionReport.PeriodBegin.Value, EmployeesAssignmentCompletionReport.PeriodEnd.Value.EndOfDay(),
                                                                                   EmployeesAssignmentCompletionReport.Unwrap == true,
                                                                                   EmployeesAssignmentCompletionReport.WithSubstitution == true, needFilter);
      
      EmployeesAssignmentCompletionReport.AssignmentCount = tableData.Select(d => d.AssignmentsCount).Sum();
      EmployeesAssignmentCompletionReport.AffectDisciplineCount = tableData.Select(d => d.AffectDisciplineAssignmentsCount).Sum();
      EmployeesAssignmentCompletionReport.OverdueAssignmentsCount = tableData.Select(d => d.OverdueAssignmentsCount).Sum();
      EmployeesAssignmentCompletionReport.CompletedInTimeCount = tableData.Select(d => d.CompletedInTimeAssignmentsCount).Sum();
      
      var tableDataSort = EmployeesAssignmentCompletionReport.SortByAssignmentCompletion == false ? tableData.OrderByDescending(d => d.AssignmentsCount).ThenBy(d => d.EmployeeName)
        : tableData.OrderBy(d => d.AssignmentsCount == 0).ThenBy(d => d.AssignmentCompletion).ThenBy(d => d.EmployeeName);
      
      var rowIndex = 1;
      foreach (var item in tableDataSort)
      {
        item.RowIndex = rowIndex++;
        item.ReportSessionId = reportSessionId;
      }
      
      Functions.Module.WriteStructuresToTable(Constants.EmployeesAssignmentCompletionReport.SourceTableName, tableDataSort);
    }

  }
}
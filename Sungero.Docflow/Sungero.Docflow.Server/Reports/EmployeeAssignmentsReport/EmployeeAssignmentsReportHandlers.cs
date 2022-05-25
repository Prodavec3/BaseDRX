using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EmployeeAssignmentsReportServerHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      EmployeeAssignmentsReport.ReportSessionId = reportSessionId;
      EmployeeAssignmentsReport.ReportDate = Calendar.Now;
      EmployeeAssignmentsReport.AssignmentHyperlink = Hyperlinks.Get(Sungero.Workflow.AssignmentBases.Info, int.MaxValue);
      
      if (EmployeeAssignmentsReport.Employee != null)
      {
        var employeeBusinessUnit = EmployeeAssignmentsReport.Employee.Department.BusinessUnit != null ? string.Format("({0})", EmployeeAssignmentsReport.Employee.Department.BusinessUnit) : string.Empty;
        EmployeeAssignmentsReport.ParamsDescription += string.Format(Sungero.Docflow.Reports.Resources.EmployeeAssignmentsReport.ReportDepartment,
                                                                     EmployeeAssignmentsReport.Employee.Department.Name,
                                                                     employeeBusinessUnit,
                                                                     System.Environment.NewLine);
        var employeeJobTitle = EmployeeAssignmentsReport.Employee.JobTitle != null ? string.Format("({0})", EmployeeAssignmentsReport.Employee.JobTitle.Name) : string.Empty;
        EmployeeAssignmentsReport.ParamsDescription += string.Format(Sungero.Docflow.Reports.Resources.EmployeeAssignmentsReport.ReportEmployee,
                                                                     EmployeeAssignmentsReport.Employee.Name,
                                                                     employeeJobTitle,
                                                                     System.Environment.NewLine);
      }
      
      var lightAssignments = Functions.Module.GetPerformerLightAssignments(EmployeeAssignmentsReport.Employee,
                                                                           EmployeeAssignmentsReport.PeriodBegin.Value, EmployeeAssignmentsReport.PeriodEnd.Value.EndOfDay());
      var tableData = Functions.Module.GetPerformerAssignmentCompletionReportData(lightAssignments);
      
      foreach (var item in tableData)
        item.ReportSessionId = reportSessionId;
      
      Functions.Module.WriteStructuresToTable(Constants.EmployeeAssignmentsReport.SourceTableName, tableData);

      var statistic = Functions.Module.GetAssignmentStatistic(lightAssignments);
      EmployeeAssignmentsReport.AssignmentCompletion = Functions.Module.GetAssignmentCompletion(statistic.TotalAssignmentCount, statistic.CompletedInTimeCount, statistic.OverdueCount)?.ToString();
      EmployeeAssignmentsReport.AssignmentCount = statistic.TotalAssignmentCount;
      EmployeeAssignmentsReport.AffectDisciplineCount = statistic.AffectAssignmentCount;
      EmployeeAssignmentsReport.OverdueAssignmentsCount = statistic.OverdueCount;
      EmployeeAssignmentsReport.CompletedInTimeCount = statistic.CompletedInTimeCount;
      EmployeeAssignmentsReport.RealPerformerCount = tableData.Count(x => x.RealPerformerName != null);
    }

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(Constants.EmployeeAssignmentsReport.SourceTableName, EmployeeAssignmentsReport.ReportSessionId);
    }

  }
}
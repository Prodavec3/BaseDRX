using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EmployeeAssignmentsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (EmployeeAssignmentsReport.PeriodBegin.HasValue && EmployeeAssignmentsReport.PeriodEnd.HasValue && EmployeeAssignmentsReport.Employee != null)
        return;
      
      // Запросить параметры.
      var dialog = Dialogs.CreateInputDialog(Docflow.Reports.Resources.EmployeeAssignmentsReport.ReportName);
      dialog.HelpCode = Constants.EmployeeAssignmentsReport.HelpCode;

      dialog.Buttons.AddOkCancel();
      
      CommonLibrary.IDateDialogValue periodBegin = null;
      CommonLibrary.IDateDialogValue periodEnd = null;
      INavigationDialogValue<Company.IEmployee> employee = null;
      
      // Период.
      var today = Calendar.UserToday;
      if (!EmployeeAssignmentsReport.PeriodBegin.HasValue)
        periodBegin = dialog.AddDate(Resources.DocumentUsagePeriodBegin, true, today.AddDays(-30));
      if (!EmployeeAssignmentsReport.PeriodEnd.HasValue)
        periodEnd = dialog.AddDate(Resources.DocumentUsagePeriodEnd, true, today);
      
      // Сотрудник.
      if (EmployeeAssignmentsReport.Employee == null)
      {
        var isAdministratorOrAdvisor = Functions.Module.Remote.IsAdministratorOrAdvisor();
        employee = dialog.AddSelect(Resources.Employee, true, Company.Employees.Null);
        if (!isAdministratorOrAdvisor)
        {
          var visibleEmployees = Functions.Module.Remote.GetVisibleEmployees();
          employee = employee.From(visibleEmployees);
        }
      }
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Functions.Module.CheckReportDialogPeriod(args, periodBegin, periodEnd);
                              });
      
      if (dialog.Show() != DialogButtons.Ok)
      {
        e.Cancel = true;
        return;
      }
      
      if (!EmployeeAssignmentsReport.PeriodBegin.HasValue)
        EmployeeAssignmentsReport.PeriodBegin = periodBegin.Value;
      
      if (!EmployeeAssignmentsReport.PeriodEnd.HasValue)
        EmployeeAssignmentsReport.PeriodEnd = periodEnd.Value;

      if (EmployeeAssignmentsReport.Employee == null)
        EmployeeAssignmentsReport.Employee = employee.Value;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class EmployeesAssignmentCompletionReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (EmployeesAssignmentCompletionReport.PeriodBegin.HasValue && EmployeesAssignmentCompletionReport.PeriodEnd.HasValue)
        return;
      
      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.Reports.Resources.EmployeesAssignmentCompletionReport.DialogTitle);
      dialog.HelpCode = Constants.EmployeesAssignmentCompletionReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      CommonLibrary.IDateDialogValue periodBegin = null;
      CommonLibrary.IDateDialogValue periodEnd = null;
      INavigationDialogValue<Company.IBusinessUnit> businessUnit = null;
      INavigationDialogValue<Company.IDepartment> department = null;
      List<Company.IDepartment> visibleDepartments = new List<Sungero.Company.IDepartment>();
      
      // Период.
      var today = Calendar.UserToday;
      if (!EmployeesAssignmentCompletionReport.PeriodBegin.HasValue)
        periodBegin = dialog.AddDate(Resources.DocumentUsagePeriodBegin, true, today.AddDays(-30));
      if (!EmployeesAssignmentCompletionReport.PeriodEnd.HasValue)
        periodEnd = dialog.AddDate(Resources.DocumentUsagePeriodEnd, true, today);
      
      // НОР.
      var isAdministratorOrAdvisor = Functions.Module.Remote.IsAdministratorOrAdvisor();
      if (EmployeesAssignmentCompletionReport.BusinessUnit == null)
      {
        businessUnit = dialog.AddSelect(Resources.BusinessUnit, false, Company.BusinessUnits.Null);
        if (!isAdministratorOrAdvisor)
        {
          var visibleBusinessUnits = Functions.Module.Remote.GetVisibleBusinessUnits();
          businessUnit = businessUnit.From(visibleBusinessUnits);
        }
      }
      
      // Подразделение.
      if (!EmployeesAssignmentCompletionReport.DepartmentIds.Any())
      {
        department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
        if (!isAdministratorOrAdvisor)
        {
          visibleDepartments = Functions.Module.Remote.GetVisibleDepartments();
          department = department.From(visibleDepartments);
        }
        else
          visibleDepartments = Sungero.Company.PublicFunctions.Department.Remote.GetDepartments().ToList();
      }
      
      businessUnit.SetOnValueChanged((arg) =>
                                     {
                                       if (Equals(arg.NewValue, arg.OldValue))
                                         return;
                                       
                                       if (department.Value != null && !Equals(arg.NewValue, department.Value.BusinessUnit))
                                         department.Value = Sungero.Company.Departments.Null;
                                       if (arg.NewValue != null)
                                         department.From(visibleDepartments.Where(d => Equals(d.BusinessUnit, arg.NewValue)));
                                       else
                                         department.From(visibleDepartments);
                                     });
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Functions.Module.CheckReportDialogPeriod(args, periodBegin, periodEnd);
                              });
      if (dialog.Show() != DialogButtons.Ok)
      {
        e.Cancel = true;
        return;
      }

      if (EmployeesAssignmentCompletionReport.WithSubstitution == null)
        EmployeesAssignmentCompletionReport.WithSubstitution = true;
      
      if (EmployeesAssignmentCompletionReport.Unwrap == null)
        EmployeesAssignmentCompletionReport.Unwrap = true;
      
      if (!EmployeesAssignmentCompletionReport.PeriodBegin.HasValue)
        EmployeesAssignmentCompletionReport.PeriodBegin = periodBegin.Value;
      
      if (!EmployeesAssignmentCompletionReport.PeriodEnd.HasValue)
      {
        EmployeesAssignmentCompletionReport.PeriodEnd = periodEnd.Value;
      }
      
      if (EmployeesAssignmentCompletionReport.BusinessUnit == null && businessUnit != null)
        EmployeesAssignmentCompletionReport.BusinessUnit = businessUnit.Value;
      
     if (EmployeesAssignmentCompletionReport.BusinessUnit != null)
        EmployeesAssignmentCompletionReport.BusinessUnitIds.Add(EmployeesAssignmentCompletionReport.BusinessUnit.Id);
      
      if (EmployeesAssignmentCompletionReport.Department == null && department != null)
        EmployeesAssignmentCompletionReport.Department = department.Value;
      
      if (EmployeesAssignmentCompletionReport.Department != null)
        EmployeesAssignmentCompletionReport.DepartmentIds.Add(EmployeesAssignmentCompletionReport.Department.Id);
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class DepartmentsAssignmentCompletionReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (DepartmentsAssignmentCompletionReport.PeriodBegin.HasValue && DepartmentsAssignmentCompletionReport.PeriodEnd.HasValue)
        return;

      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.Reports.Resources.DepartmentsAssignmentCompletionReport.DialogTitle);
      dialog.HelpCode = Constants.DepartmentsAssignmentCompletionReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      CommonLibrary.IDateDialogValue periodBegin = null;
      CommonLibrary.IDateDialogValue periodEnd = null;
      INavigationDialogValue<Company.IBusinessUnit> businessUnit = null;
      INavigationDialogValue<Company.IDepartment> department = null;
      List<Company.IDepartment> visibleDepartments = new List<Sungero.Company.IDepartment>();
      
      // Период.
      var today = Calendar.UserToday;
      if (!DepartmentsAssignmentCompletionReport.PeriodBegin.HasValue)
        periodBegin = dialog.AddDate(Resources.DocumentUsagePeriodBegin, true, today.AddDays(-30));
      if (!DepartmentsAssignmentCompletionReport.PeriodEnd.HasValue)
        periodEnd = dialog.AddDate(Resources.DocumentUsagePeriodEnd, true, today);
      
      // НОР.
      var isAdministratorOrAdvisor = Functions.Module.Remote.IsAdministratorOrAdvisor();
      if (DepartmentsAssignmentCompletionReport.BusinessUnit == null)
      {
        businessUnit = dialog.AddSelect(Resources.BusinessUnit, false, Company.BusinessUnits.Null);
        if (!isAdministratorOrAdvisor)
        {
          var visibleBusinessUnits = Functions.Module.Remote.GetVisibleBusinessUnits();
          businessUnit = businessUnit.From(visibleBusinessUnits);
        }
      }
      
      // Подразделение.
      if (!DepartmentsAssignmentCompletionReport.DepartmentIds.Any())
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
      
      // События.
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
      
      if (DepartmentsAssignmentCompletionReport.WithSubstitution == null)
        DepartmentsAssignmentCompletionReport.WithSubstitution = true;
      
       if (DepartmentsAssignmentCompletionReport.Unwrap == null)
        DepartmentsAssignmentCompletionReport.Unwrap = true;
      
      if (!DepartmentsAssignmentCompletionReport.PeriodBegin.HasValue)
        DepartmentsAssignmentCompletionReport.PeriodBegin = periodBegin.Value;
      
      if (!DepartmentsAssignmentCompletionReport.PeriodEnd.HasValue)
      {
        DepartmentsAssignmentCompletionReport.PeriodEnd = periodEnd.Value;
      }
      
      if (DepartmentsAssignmentCompletionReport.BusinessUnit == null && businessUnit != null)
        DepartmentsAssignmentCompletionReport.BusinessUnit = businessUnit.Value;
      
      if (DepartmentsAssignmentCompletionReport.BusinessUnit != null)
        DepartmentsAssignmentCompletionReport.BusinessUnitIds.Add(DepartmentsAssignmentCompletionReport.BusinessUnit.Id);
      
      if (DepartmentsAssignmentCompletionReport.Department == null && department != null)
        DepartmentsAssignmentCompletionReport.Department = department.Value;
      
      if (DepartmentsAssignmentCompletionReport.Department != null)
        DepartmentsAssignmentCompletionReport.DepartmentIds.Add(DepartmentsAssignmentCompletionReport.Department.Id);
      
    }

  }
}
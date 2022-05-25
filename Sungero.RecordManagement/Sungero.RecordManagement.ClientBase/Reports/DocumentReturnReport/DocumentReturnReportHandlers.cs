using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class DocumentReturnReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (!DocumentReturnReport.NeedShowDialog ?? false)
        return;
      
      // Задать минимальную и максимальную даты. Формат yyyy-MM-dd нужен для правильного парсинга на ru и en локализациях.
      DocumentReturnReport.MinDeliveryDate = DateTime.Parse("1753-01-01");
      DocumentReturnReport.MaxDeliveryDate = DateTime.Parse("9999-12-31");
      
      var userSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Sungero.Company.Employees.Current);
      var dialog = Dialogs.CreateInputDialog(Resources.DocumentReturnReport);
      dialog.HelpCode = Constants.DocumentReturnReport.HelpCode;
      
      var defaultBusinessUnit = (userSettings != null) ? userSettings.BusinessUnit : Sungero.Company.BusinessUnits.Null;
      
      var businessUnit = dialog.AddSelect(Docflow.Resources.BusinessUnit, false, defaultBusinessUnit);
      var department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
      var employee = dialog.AddSelect(Resources.Employee, false, Company.Employees.Null);
      var deliveryDateFrom = dialog.AddDate(Resources.DeliveredFrom, false);
      var deliveryDateTo = dialog.AddDate(Resources.DeliveredTo, false);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, deliveryDateFrom, deliveryDateTo);
                              });
      
      dialog.Buttons.AddOkCancel();
      if (dialog.Show() == DialogButtons.Ok)
      {
        var dateFrom = deliveryDateFrom.Value;
        DocumentReturnReport.DeliveryDateFrom = dateFrom.HasValue ? dateFrom : DocumentReturnReport.MinDeliveryDate;
        
        var dateTo = deliveryDateTo.Value;
        DocumentReturnReport.DeliveryDateTo = dateTo.HasValue ? dateTo.Value.EndOfDay() : DocumentReturnReport.MaxDeliveryDate;
        
        DocumentReturnReport.BusinessUnit = businessUnit.Value;
        DocumentReturnReport.Department = department.Value;
        DocumentReturnReport.Employee = employee.Value;
      }
      else
        e.Cancel = true;
    }
  }
}
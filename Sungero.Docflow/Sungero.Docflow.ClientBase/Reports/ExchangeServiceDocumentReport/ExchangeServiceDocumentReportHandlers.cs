using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class ExchangeServiceDocumentReportClientHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var connectedBoxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetActiveBoxes();
      if (!connectedBoxes.Any())
      {
        Dialogs.ShowMessage(Parties.Counterparties.Resources.InvitationBoxesNotFound,
                            Parties.Resources.ContactAdministrator,
                            MessageType.Error);
        e.Cancel = true;
        return;
      }
      var userSettings = Docflow.Functions.PersonalSetting.GetPersonalSettings(Sungero.Company.Employees.Current);
      var defaultBusinessUnit = userSettings != null ? userSettings.BusinessUnit : Sungero.Company.BusinessUnits.Null;
      
      var resources = Sungero.Docflow.Reports.Resources.ExchangeServiceDocumentReport;
      var dialog = Dialogs.CreateInputDialog(resources.ExchangeServiceDocumentReportName);
      dialog.HelpCode = Constants.ExchangeServiceDocumentReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      var businessUnit = dialog.AddSelect(Resources.BusinessUnit, false, defaultBusinessUnit);
      var department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
      var employee = dialog.AddSelect(Resources.Employee, false, Company.Employees.Null);
      var counterparty = dialog.AddSelect(resources.Counterparty, false, Parties.Counterparties.Null);
      var sendDateFrom = dialog.AddDate(resources.SendDateFrom, false);
      var sendDateTo = dialog.AddDate(resources.SendDateTo, false);
      var defaultExchangeServices = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetUsedServicesOfBox().ToArray();
      var exchangeServices = dialog.AddSelectMany(Parties.Resources.WizardExchangeServices, true, defaultExchangeServices)
        .From(defaultExchangeServices);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, sendDateFrom, sendDateTo);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        ExchangeServiceDocumentReport.SendDateFrom = sendDateFrom.Value;
        ExchangeServiceDocumentReport.SendDateTo = sendDateTo.Value;
        ExchangeServiceDocumentReport.BusinessUnit = businessUnit.Value;
        ExchangeServiceDocumentReport.Department = department.Value;
        ExchangeServiceDocumentReport.Employee = employee.Value;
        ExchangeServiceDocumentReport.Counterparty = counterparty.Value;
        ExchangeServiceDocumentReport.ExchangeService.AddRange(exchangeServices.Value.ToList());
      }
      else
        e.Cancel = true;
    }

  }
}
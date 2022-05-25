using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Client.Hyperlinks;

namespace Sungero.Docflow
{
  partial class RegistrationSettingReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (RegistrationSettingReport.BusinessUnit != null && !string.IsNullOrWhiteSpace(RegistrationSettingReport.Direction))
        return;
      
      RegistrationSettingReport.ParamsDescriprion = string.Empty;
      
      INavigationDialogValue<IBusinessUnit> businessUnit = null;
      CommonLibrary.IDropDownDialogValue documentFlow = null;
      
      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.RegistrationSettingReport);
      dialog.HelpCode = Constants.RegistrationSettingReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      // НОР.
      if (RegistrationSettingReport.BusinessUnit == null)
        businessUnit = dialog.AddSelect(Docflow.Resources.BusinessUnit, false, BusinessUnits.Null);

      // Документопоток.
      var availableDirections = new List<Enumeration>() { RegistrationSetting.DocumentFlow.Incoming, RegistrationSetting.DocumentFlow.Outgoing,
        RegistrationSetting.DocumentFlow.Inner, RegistrationSetting.DocumentFlow.Contracts };
      var documentFlows = availableDirections.Select(a => RegistrationSettings.Info.Properties.DocumentFlow.GetLocalizedValue(a)).ToList();
      if (string.IsNullOrWhiteSpace(RegistrationSettingReport.Direction))
        documentFlow = dialog.AddSelect(Docflow.Resources.Direction, false).From(documentFlows.ToArray());
      var filterDepartmentsForBusinessUnits = dialog.AddBoolean(Reports.Resources.ApprovalRulesConsolidatedReport.FilterDepartmentsForBusinessUnitsDialogCheckBox, true);
      
      // Показ диалога.
      if (dialog.Show() == DialogButtons.Ok)
      {
        if (RegistrationSettingReport.BusinessUnit == null && businessUnit.Value != null)
          RegistrationSettingReport.BusinessUnit = businessUnit.Value;
        
        if (string.IsNullOrWhiteSpace(RegistrationSettingReport.Direction))
        {
          RegistrationSettingReport.DirectionLabel = documentFlow.Value;
          RegistrationSettingReport.Direction = documentFlow.Value == null ? string.Empty : availableDirections[documentFlows.IndexOf(documentFlow.Value)].Value;
        }
        
        if (filterDepartmentsForBusinessUnits != null)
          RegistrationSettingReport.FilterDepartmentsForBusinessUnits = filterDepartmentsForBusinessUnits.Value;
      }
      else
        e.Cancel = true;
      
      // Получить адрес сервиса гиперссылок.
      var userHyperlink = Sungero.Domain.Client.Hyperlinks.HyperlinkExtensions.CreateHyperlink(Users.Current);
      RegistrationSettingReport.HyperlinkServer = string.Format("{0}://{1}{2}", userHyperlink.Scheme, userHyperlink.Host, userHyperlink.LocalPath);
    }
  }
}
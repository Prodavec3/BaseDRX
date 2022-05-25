using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class DocumentUsageReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (DocumentUsageReport.PeriodBegin.HasValue && DocumentUsageReport.PeriodEnd.HasValue)
        return;
      
      var dialog = Dialogs.CreateInputDialog(Resources.DocumentUsageReportDialog);
      dialog.HelpCode = Constants.DocumentUsageReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      CommonLibrary.IDateDialogValue periodBegin = null;
      CommonLibrary.IDateDialogValue periodEnd = null;
      INavigationDialogValue<Company.IDepartment> department = null;
      
      // Период.
      var today = Calendar.UserToday;
      if (!DocumentUsageReport.PeriodBegin.HasValue)
        periodBegin = dialog.AddDate(Resources.DocumentUsagePeriodBegin, true, today.AddDays(-30));
      if (!DocumentUsageReport.PeriodEnd.HasValue)
        periodEnd = dialog.AddDate(Resources.DocumentUsagePeriodEnd, true, today);
      if (DocumentUsageReport.Department == null)
        department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null)
          .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);

      dialog.SetOnButtonClick((args) =>
                              {
                                Functions.Module.CheckReportDialogPeriod(args, periodBegin, periodEnd);
                              });
      
      if (dialog.Show() != DialogButtons.Ok)
      {
        e.Cancel = true;
        return;
      }
      if (!DocumentUsageReport.PeriodBegin.HasValue)
      {
        DocumentUsageReport.PeriodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin.Value.Value);
        DocumentUsageReport.ClientPeriodBegin = periodBegin.Value.Value;
      }
      if (!DocumentUsageReport.PeriodEnd.HasValue)
      {
        DocumentUsageReport.PeriodEnd = periodEnd.Value.Value.EndOfDay().FromUserTime();
        DocumentUsageReport.ClientPeriodEnd = periodEnd.Value.Value;
      }
      if (DocumentUsageReport.Department == null)
        DocumentUsageReport.Department = department.Value;
    }
  }
}
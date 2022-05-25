using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class IncomingDocumentsProcessingReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      DateTime? reportBeginDate = IncomingDocumentsProcessingReport.BeginDate;
      DateTime? reportEndDate = IncomingDocumentsProcessingReport.EndDate;
      
      if (reportBeginDate.HasValue && reportEndDate.HasValue)
        return;
      
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Company.Employees.Current);
      var dialog = Dialogs.CreateInputDialog(Resources.IncomingDocumentsProcessingReportName);
      dialog.HelpCode = Constants.IncomingDocumentsProcessingReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      var settingsBeginDate = Docflow.PublicFunctions.PersonalSetting.GetStartDate(personalSettings);
      var beginDate = dialog.AddDate(Resources.StartDate, true, settingsBeginDate ?? reportBeginDate ?? Calendar.UserToday);
      var settingsEndDate = Docflow.PublicFunctions.PersonalSetting.GetEndDate(personalSettings);
      var endDate = dialog.AddDate(Resources.EndDate, true, settingsEndDate ?? reportEndDate ?? Calendar.UserToday);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        IncomingDocumentsProcessingReport.BeginDate = beginDate.Value;
        IncomingDocumentsProcessingReport.EndDate = endDate.Value;
      }
      else
      {
        e.Cancel = true;
      }
      
      IncomingDocumentsProcessingReport.ReportDate = Calendar.Now;
    }
  }
}
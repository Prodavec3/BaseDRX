using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class ActionItemsExecutionReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      // Если отчёт вызывается из документа или совещания (свойство Документ или Совещание заполнено), то не показывать диалог с выбором параметров отчёта.
      if (ActionItemsExecutionReport.Document != null || ActionItemsExecutionReport.Meeting != null)
        return;
      
      if (ActionItemsExecutionReport.BeginDate.HasValue && ActionItemsExecutionReport.EndDate.HasValue &&
          ActionItemsExecutionReport.ClientEndDate.HasValue)
        return;
      
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var dialog = Dialogs.CreateInputDialog(Resources.ActionItemsExecutionReport);
      dialog.HelpCode = Constants.ActionItemsExecutionReport.HelpCode;

      var settingsStartDate = Docflow.PublicFunctions.PersonalSetting.GetStartDate(personalSettings);
      var beginDate = dialog.AddDate(Resources.PeriodFrom, true, settingsStartDate ?? Calendar.UserToday);
      var settingsEndDate = Docflow.PublicFunctions.PersonalSetting.GetEndDate(personalSettings);
      var endDate = dialog.AddDate(Resources.PeriodTo, true, settingsEndDate ?? Calendar.UserToday);
      
      var author = dialog.AddSelect(Resources.AssignedBy, false, Company.Employees.Null);
      var businessUnit = dialog.AddSelect(Docflow.Resources.BusinessUnit, false, Company.BusinessUnits.Null);
      var department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
      var performer = dialog.AddSelect(Resources.ResponsiblePerformer, false, Company.Employees.Null);
      var meeting = dialog.AddSelect(Meetings.Meetings.Info.LocalizedName, false, Meetings.Meetings.Null);
      meeting.IsVisible = ActionItemsExecutionReport.IsMeetingsCoverContext == true;
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        ActionItemsExecutionReport.BeginDate = beginDate.Value.Value;
        ActionItemsExecutionReport.ClientEndDate = endDate.Value.Value;
        ActionItemsExecutionReport.EndDate = endDate.Value.Value.EndOfDay();
        ActionItemsExecutionReport.Author = author.Value;
        ActionItemsExecutionReport.BusinessUnit = businessUnit.Value;
        ActionItemsExecutionReport.Department = department.Value;
        ActionItemsExecutionReport.Performer = performer.Value;
        ActionItemsExecutionReport.Meeting = meeting.Value;
      }
      else
      {
        e.Cancel = true;
      }
    }
  }
}
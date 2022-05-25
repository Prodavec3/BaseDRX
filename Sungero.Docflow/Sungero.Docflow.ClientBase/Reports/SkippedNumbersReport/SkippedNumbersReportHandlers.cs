using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class SkippedNumbersReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      // При запуске из диалога регистрации передается дата регистрации. Запрашивать доп.параметры не требуется.
      if (SkippedNumbersReport.RegistrationDate.HasValue || !string.IsNullOrEmpty(SkippedNumbersReport.Period))
        return;
      
      var description = string.Empty;
      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.SkippedNumbersReport);
      dialog.HelpCode = Constants.SkippedNumbersReport.HelpCode;
      
      // Журнал регистрации.
      var reportDocumentRegister = SkippedNumbersReport.DocumentRegister ?? DocumentRegisters.Null;
      var documentRegister = dialog.AddSelect(Docflow.Resources.DocumentRegister, true, reportDocumentRegister);
      
      // Ведущий документ.
      IQueryable<IOfficialDocument> availableLeadingDocuments = null;
      if (SkippedNumbersReport.DocumentRegister != null &&
          SkippedNumbersReport.DocumentRegister.NumberingSection == DocumentRegister.NumberingSection.LeadingDocument)
        availableLeadingDocuments = Docflow.Functions.Module.Remote.GetAvaliableLeadingDocuments();
      var leadingDocumentSelect = dialog.AddSelect(Docflow.Resources.LeadingDocument, false, OfficialDocuments.Null)
        .From(availableLeadingDocuments);
      
      // Подразделение.
      var departmentSelect = dialog.AddSelect(Docflow.Resources.Department, false, Company.Departments.Null);
      
      // НОР.
      var businessUnitSelect = dialog.AddSelect(Docflow.Resources.BusinessUnit, false, Company.BusinessUnits.Null);
      
      var period = dialog.AddSelect(Reports.Resources.SkippedNumbersReport.Period, true).From(this.GetPeriods(SkippedNumbersReport.DocumentRegister));
      period.Value = Reports.Resources.SkippedNumbersReport.CurrentMonth;
      
      documentRegister.SetOnValueChanged((arg) =>
                                         {
                                           var periodValues = this.GetPeriods(arg.NewValue);
                                           var actualPeriodValue = string.Empty;
                                           if (periodValues.Contains(period.Value))
                                             actualPeriodValue = period.Value;
                                           period.From(periodValues);
                                           period.Value = actualPeriodValue;

                                           if (!Equals(arg.NewValue, arg.OldValue))
                                           {
                                             leadingDocumentSelect.Value = null;
                                             departmentSelect.Value = null;
                                             businessUnitSelect.Value = null;
                                           }
                                         });
      
      dialog.SetOnButtonClick((arg) =>
                              {
                                if (SkippedNumbersReport.DocumentRegister == null && documentRegister.Value != null)
                                  SkippedNumbersReport.DocumentRegister = documentRegister.Value;
                                
                                if (leadingDocumentSelect != null)
                                  SkippedNumbersReport.LeadingDocument = leadingDocumentSelect.Value;
                                
                                if (departmentSelect != null)
                                  SkippedNumbersReport.Department = departmentSelect.Value;
                                
                                if (businessUnitSelect != null)
                                  SkippedNumbersReport.BusinessUnit = businessUnitSelect.Value;
                                
                                SkippedNumbersReport.PeriodOffset = 0;
                                if (period.Value != null)
                                {
                                  // Определить период.
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.CurrentYear) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousYear))
                                    SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Year;
                                  
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.CurrentQuarter) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousQuarter))
                                    SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Quarter;
                                  
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.CurrentMonth) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousMonth))
                                    SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Month;
                                  
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.CurrentWeek) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousWeek))
                                    SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Week;
                                  
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.CurrentDay))
                                    SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Day;
                                  
                                  // Определить смещение.
                                  if (period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousYear) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousQuarter) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousMonth) ||
                                      period.Value.Equals(Reports.Resources.SkippedNumbersReport.PreviousWeek))
                                    SkippedNumbersReport.PeriodOffset = -1;
                                }
                                
                              });
      
      dialog.SetOnRefresh((arg) =>
                          {
                            var docRegister = SkippedNumbersReport.DocumentRegister;
                            var calledFromRegister = docRegister != null;
                            documentRegister.IsVisible = !calledFromRegister;
                            if (!calledFromRegister)
                              docRegister = documentRegister.Value;
                            
                            var documentRegisterSpecified = docRegister != null;
                            var hasLeadDocSection = false;
                            var hasDepartmentSection = false;
                            var hasBusinessUnitSection = false;
                            
                            if (documentRegisterSpecified)
                            {
                              hasLeadDocSection = docRegister.NumberingSection == DocumentRegister.NumberingSection.LeadingDocument;
                              hasDepartmentSection = docRegister.NumberingSection == DocumentRegister.NumberingSection.Department;
                              hasBusinessUnitSection = docRegister.NumberingSection == DocumentRegister.NumberingSection.BusinessUnit;
                            }
                            
                            if (hasLeadDocSection && availableLeadingDocuments == null)
                            {
                              availableLeadingDocuments = Docflow.Functions.Module.Remote.GetAvaliableLeadingDocuments();
                              leadingDocumentSelect.From(availableLeadingDocuments);
                            }
                            leadingDocumentSelect.IsVisible = documentRegisterSpecified && hasLeadDocSection;
                            leadingDocumentSelect.IsRequired = documentRegisterSpecified && hasLeadDocSection;
                            
                            departmentSelect.IsVisible = documentRegisterSpecified && hasDepartmentSection;
                            departmentSelect.IsRequired = documentRegisterSpecified && hasDepartmentSection;
                            
                            businessUnitSelect.IsVisible = documentRegisterSpecified && hasBusinessUnitSection;
                            businessUnitSelect.IsRequired = documentRegisterSpecified && hasBusinessUnitSection;
                          });
      
      if (dialog.Show() != DialogButtons.Ok)
        e.Cancel = true;
    }
    
    public string[] GetPeriods(IDocumentRegister documentRegister)
    {
      var currentPeriods  = new List<string>
      {
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.CurrentYear,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.CurrentQuarter,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.CurrentMonth,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.CurrentWeek,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.CurrentDay
      };
      var previousPeriods  = new List<string>
      {
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.PreviousYear,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.PreviousQuarter,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.PreviousMonth,
        Sungero.Docflow.Reports.Resources.SkippedNumbersReport.PreviousWeek
      };
      
      var skipPeriods = 0;
      var takePeriods = 3;
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod.Value == Sungero.Docflow.DocumentRegister.NumberingPeriod.Day)
      {
        skipPeriods = 4;
        takePeriods = 1;
      }
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod.Value == Sungero.Docflow.DocumentRegister.NumberingPeriod.Month)
      {
        skipPeriods = 2;
        takePeriods = 2;
      }
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod.Value == Sungero.Docflow.DocumentRegister.NumberingPeriod.Quarter)
        skipPeriods = 1;
      
      var result  = new List<string>();
      result.AddRange(currentPeriods.Skip(skipPeriods).Take(takePeriods));
      result.AddRange(previousPeriods.Skip(skipPeriods).Take(takePeriods));

      return result.ToArray();
    }
  }
}
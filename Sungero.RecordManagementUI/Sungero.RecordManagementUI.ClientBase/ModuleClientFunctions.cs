using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.RecordManagementUI.Client
{
  public class ModuleFunctions
  {
    
    #region Документы
    
    /// <summary>
    /// Создать документ.
    /// </summary>
    public virtual void CreateDocument()
    {
      Docflow.OfficialDocuments.CreateDocumentWithCreationDialog(InternalDocumentBases.Info,
                                                                 IncomingDocumentBases.Info,
                                                                 OutgoingDocumentBases.Info);
    }
    
    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Открыть отчет пропусков нумерации.
    /// </summary>
    public virtual void ShowSkippedNumbersReport()
    {
      Docflow.Reports.GetSkippedNumbersReport().Open();
    }
    
    /// <summary>
    /// Показать список всех отчетов.
    /// </summary>
    public virtual void ShowAllReports()
    {
      var reports = RecordManagement.Reports.GetAll()
        .Where(r => !(r is RecordManagement.IAcquaintanceReport))
        .ToList();
      reports.Add(Docflow.Reports.GetSkippedNumbersReport());
      reports.AsEnumerable().Show(RecordManagement.Resources.AllReportsTitle);
    }
    
    #endregion
    
    #region Поиск документов
    
    /// <summary>
    /// Поиск по регистрационным данным.
    /// </summary>
    /// <returns>Список документов, удовлетворяющих регистрационным данным.</returns>
    public virtual IQueryable<IOfficialDocument> SearchByRegistrationData()
    {
      // Создать диалог ввода организации и даты регистрации.
      var dialog = Dialogs.CreateInputDialog(Resources.SearchFromRegistrationData);
      var registrationNumber = dialog.AddString(Docflow.Resources.RegistrationNumber, false);
      var registrationDateFrom = dialog.AddDate(Resources.RegistrationDateFrom, false);
      var registrationDateTo = dialog.AddDate(Resources.RegistrationDateTo, false);
      var documentRegister = dialog.AddSelect(Docflow.Resources.DocumentRegister, false, DocumentRegisters.Null);
      var fileList = dialog.AddSelect(Resources.FileList, false, CaseFiles.Null);
      var registeredBy = dialog.AddSelect(Resources.RegisteredBy, false, Company.Employees.Null);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckDialogPeriod(args, registrationDateFrom, registrationDateTo);
                              });
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
        return Functions.Module.Remote.GetFilteredRegisteredDocuments(registrationNumber.Value,
                                                                      registrationDateFrom.Value,
                                                                      registrationDateTo.Value,
                                                                      documentRegister.Value,
                                                                      fileList.Value,
                                                                      registeredBy.Value);
      return null;
    }
    
    /// <summary>
    /// Получить официальную переписку с корреспондентом.
    /// </summary>
    /// <returns>Список корреспонденции.</returns>
    public virtual IQueryable<IOfficialDocument> GetOfficialCorrespondenceWithCounterparty()
    {
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Company.Employees.Current);
      
      var dialog = Dialogs.CreateInputDialog(Resources.OfficialCorrespondenceWithCounterparty);
      var counterparty = dialog.AddSelect(Resources.Correspondent, true, Parties.Counterparties.Null);
      var settingsStartDate = Docflow.PublicFunctions.PersonalSetting.GetStartDate(personalSettings);
      var beginDate = dialog.AddDate(Resources.RegistrationDateFrom, false, settingsStartDate ?? Calendar.UserToday);
      var settingsEndDate = Docflow.PublicFunctions.PersonalSetting.GetEndDate(personalSettings);
      var endDate = dialog.AddDate(Resources.RegistrationDateTo, false, settingsEndDate ?? Calendar.UserToday);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
        return Functions.Module.Remote.GetOfficialCorrespondenceWithCounterparty(counterparty.Value, beginDate.Value, endDate.Value);
      
      return null;
    }
    
    #endregion
    
    /// <summary>
    /// Показать параметры модуля.
    /// </summary>
    public virtual void ShowRecordManagementSettings()
    {
      RecordManagement.PublicFunctions.Module.GetSettings().Show();
    }
  }
}
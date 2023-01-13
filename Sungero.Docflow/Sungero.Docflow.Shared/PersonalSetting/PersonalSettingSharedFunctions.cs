using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow.Shared
{
  partial class PersonalSettingFunctions
  {
    /// <summary>
    /// Получить url сервиса информации по компаниям. Не используется. Помечено устаревшим.
    /// </summary>
    /// <param name="isPerson">Признак, что ИП.</param>
    /// <returns>Url сервиса.</returns>
    [Public, Obsolete]
    public static string GetCompanyServiceUrl(bool isPerson)
    {
      var settings = GetPersonalSettings();
      if (settings == null || settings.CompanyService == null ||
          settings.CompanyService == Docflow.PersonalSetting.CompanyService.OwnerOnline)
        return "https://compliance.vladelets.online/#/report/{0}/company";

      if (settings.CompanyService == Docflow.PersonalSetting.CompanyService.HonestBusiness)
        return "https://honestbusiness.ru/id{0}";

      if (settings.CompanyService == Docflow.PersonalSetting.CompanyService.ForFairBusiness)
        return "https://zachestnyibiznes.ru/company/ul/{0}";
      
      if (settings.CompanyService == Docflow.PersonalSetting.CompanyService.Sbis)
        return "https://sbis.ru/contragents/{1}";
      
      if (settings.CompanyService == Docflow.PersonalSetting.CompanyService.KonturFocus)
        return "https://focus.kontur.ru/entity?query={0}";
      
      return string.Empty;
    }
    
    /// <summary>
    /// Признак необходимости указания ИНН. Не используется. Помечено устаревшим.
    /// </summary>
    /// <param name="isPerson">Признак, что ИП.</param>
    /// <returns>True, если нужен ИНН, иначе - false.</returns>
    [Public, Obsolete]
    public static bool NeedTinToCompanyService(bool isPerson)
    {
      var settings = GetPersonalSettings();
      if (settings == null || settings.CompanyService == null ||
          settings.CompanyService == Docflow.PersonalSetting.CompanyService.OwnerOnline ||
          settings.CompanyService == Docflow.PersonalSetting.CompanyService.HonestBusiness ||
          settings.CompanyService == Docflow.PersonalSetting.CompanyService.ForFairBusiness ||
          settings.CompanyService == Docflow.PersonalSetting.CompanyService.KonturFocus)
        return false;

      if (settings.CompanyService == Docflow.PersonalSetting.CompanyService.Sbis)
        return true;
      
      return false;
    }

    /// <summary>
    /// Получить автора резолюции из настроек или орг. структуры.
    /// </summary>
    /// <param name="entity">Настройки.</param>
    /// <returns>Автор резолюции.</returns>
    [Public]
    public static IEmployee GetResolutionAuthor(Sungero.Docflow.IPersonalSetting entity)
    {
      if (entity == null)
        return null;
      
      if (entity.IsAutoCalcResolutionAuthor == true)
        return Functions.PersonalSetting.Remote.GetEmployeeResolutionAuthor(entity.Employee) ?? entity.Employee;
      else
        return entity.ResolutionAuthor;
    }

    /// <summary>
    /// Получить контролера поручения из настроек или орг. структуры.
    /// </summary>
    /// <param name="entity">Настройки.</param>
    /// <returns>Контролер.</returns>
    [Public]
    public static IEmployee GetSupervisor(Sungero.Docflow.IPersonalSetting entity)
    {
      if (entity == null)
        return null;
      
      if (entity.IsAutoCalcSupervisor == true)
        return Functions.Module.GetSecretary(entity.Employee) ?? entity.Employee;
      else
        return entity.Supervisor;
    }
    
    /// <summary>
    /// Получить настройки пользователя.
    /// </summary>
    /// <param name="employee">Пользователь.</param>
    /// <returns>Настройки пользователя.</returns>
    [Public]
    public static IPersonalSetting GetPersonalSettings(IEmployee employee = null)
    {
      if (employee == null)
        employee = Employees.Current;
      
      // Для пользователя без сотрудника настроек быть не может.
      if (employee == null)
        return null;
      
      return Functions.PersonalSetting.Remote.CreatePersonalSettings(employee);
    }
    
    /// <summary>
    /// Получить дату начала периода из настроек.
    /// </summary>
    /// <param name="settings">Пользовательские настройки.</param>
    /// <returns>Дата.</returns>
    [Public]
    public static DateTime? GetStartDate(IPersonalSetting settings)
    {
      if (settings == null)
        return null;
      
      DateTime today = Calendar.UserToday;
      if (settings.Period == null)
        return null;
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.Today)
        return today;
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.CurrentMonth)
        return today.BeginningOfMonth();
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.CurrentYear)
        return today.BeginningOfYear();
      return null;
    }
    
    /// <summary>
    /// Получить дату окончания периода из настроек.
    /// </summary>
    /// <param name="settings">Пользовательские настройки.</param>
    /// <returns>Дата.</returns>
    [Public]
    public static DateTime? GetEndDate(IPersonalSetting settings)
    {
      if (settings == null)
        return null;
      
      DateTime today = Calendar.UserToday;
      if (settings.Period == null)
        return null;
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.Today)
        return today;
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.CurrentMonth)
        return today.EndOfMonth().Date;
      if (settings.Period == Sungero.Docflow.PersonalSetting.Period.CurrentYear)
        return today.EndOfYear().Date;
      return null;
    }
    
    /// <summary>
    /// Проверить, можно ли установить признак включения автоматического выполнения ведущего поручения.
    /// </summary>
    /// <param name="followUpActionItem">Признак установки на контроль.</param>
    /// <param name="supervisor">Контролер по умолчанию.</param>
    /// <returns>True, если можно установить, иначе - False.</returns>
    public virtual bool CanAutoExecLeadingActionItem(bool? followUpActionItem, IEmployee supervisor)
    {
      if (followUpActionItem != true)
        return true;
      
      if (supervisor == null || !Equals(supervisor, _obj.Employee))
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Изменить доступность признака включения автоматического выполнения ведущего поручения.
    /// </summary>
    /// <param name="followUpActionItem">Признак установки на контроль.</param>
    /// <param name="supervisor">Контролер по умолчанию.</param>
    public virtual void ChangeIsAutoExecLeadingActionItemAccess(bool? followUpActionItem, IEmployee supervisor)
    {
      var canAutoExec = this.CanAutoExecLeadingActionItem(followUpActionItem, supervisor);
      _obj.State.Properties.IsAutoExecLeadingActionItem.IsEnabled = canAutoExec;
      if (!canAutoExec)
        _obj.IsAutoExecLeadingActionItem = false;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow.Shared
{
  partial class RegistrationSettingFunctions
  {
    /// <summary>
    /// Вернуть активные настройки по журналу.
    /// </summary>
    /// <param name="documentRegister">Журнал.</param>
    /// <returns>Настройки по журналу.</returns>
    [Public]
    public static List<IRegistrationSetting> GetByDocumentRegister(IDocumentRegister documentRegister)
    {
      return Functions.Module.Remote.GetRegistrationSettingByDocumentRegister(documentRegister).ToList();
    }
    
    /// <summary>
    /// Получить настройку по документу.
    /// </summary>
    /// <param name="document">Документ для подбора настройки.</param>
    /// <param name="settingType">Тип настройки.</param>
    /// <returns>Настройка по документу, которое имеет наивысший приоритет.</returns>
    [Public]
    public static IRegistrationSetting GetSettingByDocument(IOfficialDocument document, Enumeration? settingType)
    {
      var settings = GetAvailableSettingsByParams(settingType, document.BusinessUnit, document.DocumentKind, document.Department);
      return settings.OrderByDescending(r => r.Priority).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить доступные настройки по документу.
    /// </summary>
    /// <param name="document">Документ для подбора настройки.</param>
    /// <param name="settingType">Тип настройки.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Все настройки, которые подходят к документу.</returns>
    [Public]
    public static IRegistrationSetting GetSettingForKind(IOfficialDocument document,
                                                         Enumeration? settingType,
                                                         IDocumentKind documentKind)
    {
      return GetSettingByParams(settingType, document.BusinessUnit, documentKind, document.Department);
    }
    
    /// <summary>
    /// Получить настройку по параметрам.
    /// </summary>
    /// <param name="settingType">Тип настройки.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="department">Подразделение.</param>
    /// <returns>Настройка, которое имеет наивысший приоритет.</returns>
    [Public]
    public static IRegistrationSetting GetSettingByParams(Enumeration? settingType,
                                                          IBusinessUnit businessUnit,
                                                          IDocumentKind documentKind,
                                                          IDepartment department)
    {
      var settings = GetAvailableSettingsByParams(settingType, businessUnit, documentKind, department);
      return settings.OrderByDescending(r => r.Priority).FirstOrDefault();
    }

    /// <summary>
    /// Получить доступные настройки по параметрам.
    /// </summary>
    /// <param name="settingType">Тип настройки.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="department">Подразделение.</param>
    /// <returns>Все настройки, которые подходят по параметрам.</returns>
    [Public]
    public static IQueryable<IRegistrationSetting> GetAvailableSettingsByParams(Enumeration? settingType,
                                                                                IBusinessUnit businessUnit,
                                                                                IDocumentKind documentKind,
                                                                                IDepartment department)
    {
      return Functions.Module.Remote.GetAvailableRegistrationSettings(settingType, businessUnit, documentKind, department);
    }
    
    /// <summary>
    /// Получить настройку по параметрам.
    /// </summary>
    /// <param name="activeSettings">Активные настройки.</param>
    /// <param name="settingType">Тип настройки.</param>
    /// <param name="businessUnitId">НОР.</param>
    /// <param name="documentKindId">Вид документа.</param>
    /// <param name="departmentId">Подразделение.</param>
    /// <returns>Настройка, которая имеет наивысший приоритет.</returns>
    [Public]
    public static IRegistrationSetting GetSettingByParamsIds(List<IRegistrationSetting> activeSettings,
                                                             Enumeration? settingType,
                                                             int businessUnitId, int documentKindId, int departmentId)
    {
      var activeStatus = CoreEntities.DatabookEntry.Status.Active;
      var settings = activeSettings.Where(r => r.SettingType == settingType)
        .Where(r => !r.DocumentKinds.Any() ||
               r.DocumentKinds.Any(o => o.DocumentKind != null && o.DocumentKind.Id == documentKindId))
        .Where(r => !r.BusinessUnits.Any() ||
               r.BusinessUnits.Any(o => o.BusinessUnit != null && o.BusinessUnit.Id == businessUnitId))
        .Where(r => !r.Departments.Any() ||
               r.Departments.Any(o => o.Department != null && o.Department.Id == departmentId));
      
      return settings.OrderByDescending(r => r.Priority).FirstOrDefault();
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow.Server
{
  partial class RegistrationSettingFunctions
  {
    /// <summary>
    /// Получить дублирующие настройки.
    /// </summary>
    /// <param name="setting">Текущая настройка.</param>
    /// <returns>Настройки, конфликтующие с текущей.</returns>
    [Remote(IsPure = true), Public]
    public static List<IRegistrationSetting> GetDoubleSettings(IRegistrationSetting setting)
    {
      var conflictedSettings = new List<IRegistrationSetting>();
      
      #region Конфликт по условиям
      
      var allSettings = RegistrationSettings.GetAll(s => !Equals(s, setting) && setting.SettingType == s.SettingType &&
                                                    s.Status.Value == CoreEntities.DatabookEntry.Status.Active).ToList();
      
      foreach (var documentKind in setting.DocumentKinds)
      {
        conflictedSettings.AddRange(allSettings.Where(s => s.DocumentKinds.Any(o => o.DocumentKind == documentKind.DocumentKind)).ToList());
      }
      
      allSettings = conflictedSettings.ToList();
      conflictedSettings.Clear();
      
      if (setting.BusinessUnits.Any())
      {
        foreach (var unit in setting.BusinessUnits)
        {
          conflictedSettings.AddRange(allSettings.Where(s => s.BusinessUnits.Any(o => o.BusinessUnit == unit.BusinessUnit)).ToList());
        }
      }
      else
      {
        conflictedSettings.AddRange(allSettings.Where(s => !s.BusinessUnits.Any()).ToList());
      }
      
      allSettings = conflictedSettings.ToList();
      conflictedSettings.Clear();
      
      if (setting.Departments.Any())
      {
        foreach (var department in setting.Departments)
        {
          conflictedSettings.AddRange(allSettings.Where(s => s.Departments.Any(o => o.Department == department.Department)).ToList());
        }
      }
      else
      {
        conflictedSettings.AddRange(allSettings.Where(s => !s.Departments.Any()).ToList());
      }
      
      #endregion
      
      return conflictedSettings.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить действующие настройки регистрации по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Настройки регистрации.</returns>
    [Public]
    public static IQueryable<IRegistrationSetting> GetRegistrationSettingsByDocumentKind(IDocumentKind documentKind)
    {
      return RegistrationSettings.GetAll(s => s.Status == Docflow.RegistrationSetting.Status.Active)
        .Where(s => s.DocumentKinds.Any(k => k.DocumentKind.Id == documentKind.Id));
    } 
  }
}
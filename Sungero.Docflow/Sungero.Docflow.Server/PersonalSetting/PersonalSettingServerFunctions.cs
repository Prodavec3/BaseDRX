using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow.Server
{
  partial class PersonalSettingFunctions
  {

    /// <summary>
    /// Получить автора резолюции из орг. структуры.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Помощник сотрудника.</returns>
    [Remote(IsPure = true), Public]
    public static IEmployee GetEmployeeResolutionAuthor(IEmployee employee)
    {
      return Functions.Module.GetSecretaryManager(employee);
    }

    /// <summary>
    /// Создать персональные настройки для пользователя.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Настройки сотрудника.</returns>
    [Remote, Public]
    public static IPersonalSetting CreatePersonalSettings(IEmployee employee)
    {
      var personalSettings = PersonalSettings.GetAll(s => Equals(s.Employee, employee)).SingleOrDefault();
      if (personalSettings != null)
        return personalSettings;
      
      personalSettings = PersonalSettings.Create();
      personalSettings.Employee = employee;
      personalSettings.Save();
      return personalSettings;
    }
    
    /// <summary>
    /// Получить признак отображения области регистрации из персональных настроек пользователя.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True - если отображение области регистрации включено в настройках пользователя, иначе - false.</returns>
    [Remote(IsPure = true), Public]
    public static bool GetShowRegistrationPaneParam(IEmployee employee)
    {
      if (employee == null)
        employee = Employees.Current;
      
      // Для пользователя без сотрудника настроек быть не может.
      if (employee == null)
        return false;
      
      var showRegPane = PersonalSettings.GetAll(s => s.Employee.Id == employee.Id).Select(p => p.ShowRegPane).SingleOrDefault();
      if (showRegPane != null)
        return (bool)showRegPane;
      
      var personalSettings = CreatePersonalSettings(employee);
      return (bool)personalSettings.ShowRegPane;
    }

    /// <summary>
    /// Получить или создать, если не существуют, настройки пользователя.
    /// </summary>
    /// <param name="employee">Пользователь.</param>
    /// <returns>Настройки пользователя.</returns>
    [Remote, Public]
    public static IPersonalSetting GetOrCreatePersonalSettings(IEmployee employee)
    {
      var personalSettings = PersonalSettings.GetAll(s => employee.Equals(s.Employee)).SingleOrDefault();
      if (personalSettings == null)
        personalSettings = CreatePersonalSettings(employee);
      
      return personalSettings;
    }
    
  }
}
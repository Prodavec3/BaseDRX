using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Company.Server
{
  partial class EmployeeFunctions
  {
    /// <summary>
    /// Создать асинхронное событие обновления имени сотрудника из персоны.
    /// </summary>
    /// <param name="personId">ИД персоны.</param>
    [Public]
    public static void CreateUpdateEmployeeNameAsyncHandler(int personId)
    {
      var asyncUpdateEmployeeName = Sungero.Company.AsyncHandlers.UpdateEmployeeName.Create();
      asyncUpdateEmployeeName.PersonId = personId;
      asyncUpdateEmployeeName.ExecuteAsync();
    }
    
    /// <summary>
    /// Получить количество активных сотрудников.
    /// </summary>
    /// <returns>Количество.</returns>
    public static int GetEmployeesCount()
    {
      return Employees.GetAll().Where(e => e.Status.Equals(Sungero.CoreEntities.DatabookEntry.Status.Active)).Count();
    }
    
    /// <summary>
    /// Получить сотрудника по имени.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <returns>Сотрудник.</returns>
    [Public, Remote(IsPure = true)]
    public static Company.IEmployee GetEmployeeByName(string name)
    {
      var employees = GetEmployeesByName(name);
      return employees.Count == 1 ? employees.First() : null;
    }
    
    /// <summary>
    /// Получить сотрудников по имени.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <returns>Список сотрудников.</returns>
    /// <remarks>Используется на слое.</remarks>
    [Public, Remote(IsPure = true)]
    public static List<Company.IEmployee> GetEmployeesByName(string name)
    {
      #region Список форматов ФИО
      
      // Реализован парсинг следующих форматов ФИО:
      // Иванов
      // Иванов  Иван
      // Иванов  Иван  Иванович
      // Иван  Иванов
      // Иван  Иванович  Иванов
      // Иванов  И.  И.
      // Иванов  И.
      // Иванов  И.И.
      // Иванов  И  И
      // Иванов  И
      // Иванов  ИИ
      // И. И. Иванов
      // И. Иванов
      // И.И. Иванов
      // И И  Иванов
      // И Иванов
      // ИИ Иванов
      #endregion
      
      if (name == null)
        return null;
      
      var oldChar = "ё";
      var newChar = "е";
      name = name.ToLower().Replace(oldChar, newChar);
      var activeEmployees = Company.Employees.GetAll(e => e.Status == Company.Employee.Status.Active);
      
      var employees = activeEmployees.Where(e => e.Name.ToLower().Replace(oldChar, newChar) == name).ToList();
      
      if (!employees.Any())
        employees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == name).ToList();
      
      if (!employees.Any())
      {
        // Полные ФИО, могут быть без отчества.
        var fullNameRegex = System.Text.RegularExpressions.Regex.Match(name, @"^(\S+)(?<!\.)\s*(\S+)(?<!\.)\s*(\S*)(?<!\.)$");
        if (fullNameRegex.Success)
        {
          var lastName = fullNameRegex.Groups[1].Value;
          var firstName = fullNameRegex.Groups[2].Value;
          employees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == lastName &&
                                            e.Person.FirstName.ToLower().Replace(oldChar, newChar) == firstName).ToList();
          var middleName = fullNameRegex.Groups[3].Value;
          if (middleName != string.Empty)
            employees = employees.Where(e => e.Person.MiddleName == null ||
                                        e.Person.MiddleName != null && e.Person.MiddleName.ToLower().Replace(oldChar, newChar) == middleName).ToList();
          
          firstName = fullNameRegex.Groups[1].Value;
          lastName = fullNameRegex.Groups[3].Value;
          middleName = string.Empty;
          if (lastName == string.Empty)
            lastName = fullNameRegex.Groups[2].Value;
          else
            middleName = fullNameRegex.Groups[2].Value;
          
          var revertedEmployees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == lastName &&
                                                        e.Person.FirstName.ToLower().Replace(oldChar, newChar) == firstName).ToList();
          
          if (middleName != string.Empty)
            revertedEmployees = revertedEmployees.Where(e => e.Person.MiddleName == null ||
                                                        e.Person.MiddleName != null && e.Person.MiddleName.ToLower().Replace(oldChar, newChar) == middleName).ToList();

          employees.AddRange(revertedEmployees);
        }
      }
      
      if (!employees.Any())
      {
        // Сокращённое ФИО (Иванов И. И.), могут быть без отчества.
        var fullNameRegex = System.Text.RegularExpressions.Regex.Match(name, @"^(\S+)\s*(\S)\.?\s*(\S?)(?<!\.)\.?$");
        if (fullNameRegex.Success)
        {
          var lastName = fullNameRegex.Groups[1].Value;
          var firstName = fullNameRegex.Groups[2].Value;
          employees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == lastName).ToList();
          employees = employees.Where(e => e.Person.FirstName.ToLower().Replace(oldChar, newChar)[0] == firstName[0]).ToList();
          
          var middleName = fullNameRegex.Groups[3].Value;
          if (middleName != string.Empty)
            employees = employees.Where(e => e.Person.MiddleName == null ||
                                        e.Person.MiddleName != null && e.Person.MiddleName.ToLower().Replace(oldChar, newChar)[0] == middleName[0]).ToList();
        }
      }
      
      if (!employees.Any())
      {
        // Сокращённое ФИО (И. И. Иванов), могут быть без отчества.
        var fullNameRegex = System.Text.RegularExpressions.Regex.Match(name, @"^(\S)\.?\s*(\S?)(?<!\.)\.?\s+(\S+)$");
        if (fullNameRegex.Success)
        {
          var firstName = fullNameRegex.Groups[1].Value;
          var lastName = fullNameRegex.Groups[3].Value;
          var middleName = string.Empty;
          if (lastName == string.Empty)
            lastName = fullNameRegex.Groups[2].Value;
          else
            middleName = fullNameRegex.Groups[2].Value;
          
          employees = activeEmployees.Where(e => e.Person.LastName.ToLower().Replace(oldChar, newChar) == lastName).ToList();
          employees = employees.Where(e => e.Person.FirstName.ToLower().Replace(oldChar, newChar)[0] == firstName[0]).ToList();
          
          if (middleName != string.Empty)
            employees = employees.Where(e => e.Person.MiddleName == null ||
                                        e.Person.MiddleName != null && e.Person.MiddleName.ToLower().Replace(oldChar, newChar)[0] == middleName[0]).ToList();
        }
      }
      
      return employees;
    }
    
    /// <summary>
    /// Получить нумерованный список сотрудников.
    /// </summary>
    /// <param name="employees">Список сотрудников.</param>
    /// <param name="withJobTitle">Признак отображения должности сотрудников.</param>
    /// <returns>Строка с нумерованным списком сотрудников.</returns>
    [Public, Remote(IsPure = true)]
    public static string GetEmployeesNumberedList(List<IEmployee> employees, bool withJobTitle)
    {
      if (!employees.Any())
        return null;
      
      employees = employees
        .GroupBy(g => g)
        .Select(s => s.Key)
        .ToList<Company.IEmployee>();
      
      return Functions.Employee.GetEmployeesNumberedList(employees, withJobTitle, true);
    }
    
    /// <summary>
    /// Получить нумерованный список сотрудников.
    /// </summary>
    /// <param name="employees">Список сотрудников.</param>
    /// <param name="withJobTitle">Признак отображения должности сотрудников.</param>
    /// <param name="platformLogic">Дублировать платформенную логику при формировании ФИО сотрудников.</param>
    /// <returns>Строка с нумерованным списком сотрудников.</returns>
    [Public, Remote(IsPure = true)]
    public static string GetEmployeesNumberedList(List<IEmployee> employees, bool withJobTitle, bool platformLogic)
    {
      if (!employees.Any())
        return null;
      
      var employeesNumberedList = new List<string>();
      foreach (var employee in employees)
      {
        var shortName = Functions.Employee.GetShortName(employee, platformLogic);
        var employeeNumberedName = string.Format("{0}. {1}", employees.IndexOf(employee) + 1, shortName);
        if (withJobTitle && employee.JobTitle != null && !string.IsNullOrWhiteSpace(employee.JobTitle.Name))
          employeeNumberedName = string.Format("{0} – {1}", employeeNumberedName, employee.JobTitle.Name);
        employeesNumberedList.Add(employeeNumberedName);
      }
      
      return string.Join("\r\n", employeesNumberedList);
    }
    
    /// <summary>
    /// Сформировать всплывающую подсказку о сотруднике в виде модели всплывающего окна.
    /// </summary>
    /// <returns>Всплывающая подсказка о сотруднике в виде модели всплывающего окна.</returns>
    /// <remarks>Используется в подсказке о сотруднике.</remarks>
    public virtual Sungero.Core.IDigestModel GetEmployeePopup()
    {
      if (_obj.IsSystem == true)
        return null;
      
      var digest = Sungero.Core.UserDigest.Create(_obj);
      if (_obj.Department != null)
        digest.AddEntity(_obj.Department);
      
      if (_obj.JobTitle != null)
        digest.AddLabel(_obj.JobTitle.Name);
      
      if (!string.IsNullOrWhiteSpace(_obj.Phone))
        digest.AddLabel(string.Format("{0} {1}", Company.Employees.Resources.PopupPhoneDescription, _obj.Phone));
      
      if (_obj.Department != null)
      {
        var manager = _obj.Department.Manager;
        if (manager == null && _obj.Department.HeadOffice != null)
          manager = _obj.Department.HeadOffice.Manager;
        
        if (manager != null && !Equals(manager, _obj))
          digest.AddEntity(manager, Company.Employees.Resources.PopupManagerDescription);
      }
      
      return digest;
    }
    
    /// <summary>
    /// Сформировать всплывающую подсказку о сотруднике в виде текста.
    /// </summary>
    /// <returns>Всплывающая подсказка о сотруднике в виде текста.</returns>
    [Public]
    public virtual string GetEmployeePopupText()
    {
      // Используется в тестах подсказки о сотруднике.
      var employeePopup = this.GetEmployeePopup() as Sungero.Domain.Shared.UserDigestModel;
      if (employeePopup == null)
        return string.Empty;
      
      var popupText = new System.Text.StringBuilder();
      popupText.AppendLine(employeePopup.Header);
      
      foreach (var control in employeePopup.Controls)
      {
        var digestLabel = control as DigestLabel;
        if (digestLabel != null)
        {
          popupText.AppendLine(digestLabel.Text);
          continue;
        }
        
        var digestProperty = control as DigestNavigationProperty;
        if (digestProperty != null)
        {
          popupText.AppendLine(string.Concat(digestProperty.Text, digestProperty.Entity.ToString()));
          continue;
        }
        
        var digestLink = control as DigestLink;
        if (digestLink != null)
        {
          popupText.AppendLine(string.Concat(digestLink.Text, digestLink.Href));
        }
      }
      
      return popupText.ToString();
    }
    
    /// <summary>
    /// Проверить, что сотрудник может готовить проект резолюции.
    /// </summary>
    /// <returns>True, если сотрудник может готовить проект резолюции, иначе - False.</returns>
    /// <remarks>Сотрудник может готовить проект резолюции,
    /// как непосредственно являясь помощником или замещая помощника.
    /// Используется CoreEntities.Recipients.DirectSubstitutionRecipientIdsFor,
    /// которая внутри себя делает remote-запрос.</remarks>
    [Public, Remote(IsPure = true)]
    public virtual bool CanPrepareDraftResolution()
    {
      var assistants = Functions.Module.GetResolutionPreparers();
      var substitutes = CoreEntities.Recipients.DirectSubstitutionRecipientIdsFor(_obj);
      return assistants.Any(x => substitutes.Contains(x.Assistant.Id) || Equals(x.Assistant, _obj));
    }
    
    /// <summary>
    /// Вернуть всех сотрудников.
    /// </summary>
    /// <returns>Все сотрудники.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<IEmployee> GetEmployees()
    {
      return Employees.GetAll();
    }
  }
}
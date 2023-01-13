using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Shared
{
  partial class EmployeeFunctions
  {
    
    #region Сокращенное ФИО
    
    /// <summary>
    /// Возвращает форматированное значение вида "Должность (Фамилия И.О.)" для сотрудника.
    /// </summary>
    /// <returns>Строка вида "Должность (Фамилия И.О.)".</returns>
    [Public]
    public string GetJobTitleWithShortName()
    {
      var employeeShortName = Functions.Employee.GetShortName(_obj, false);
      
      if (_obj.JobTitle != null)
        return string.Format("<b>{0}</b> ({1})", _obj.JobTitle.Name, employeeShortName);
      else
        return string.Format("{0}", employeeShortName);
    }
    
    /// <summary>
    /// Получить фамилию и инициалы в указанном падеже.
    /// </summary>
    /// <param name="declensionCase">Падеж.</param>
    /// <param name="platformLogic">Дублировать платформенную логику.</param>
    /// <returns>Фамилия и инициалы.</returns>
    [Public]
    public virtual string GetShortName(Sungero.Core.DeclensionCase declensionCase, bool platformLogic)
    {
      var fullNameInDeclension = Functions.Employee.GetFullNameInDeclension(_obj, declensionCase);
      var lastName = fullNameInDeclension.LastName;
      var firstName = fullNameInDeclension.FirstName;
      var middleName = fullNameInDeclension.MiddleName;
      
      // ФИО в формате "Фамилия И.О." или "Фамилия Имя". Аналогично платформенной логике построения краткого имени в переписке.
      if (platformLogic && string.IsNullOrEmpty(_obj.Person.MiddleName))
      {
        using (TenantInfo.Culture.SwitchTo())
          return Sungero.Parties.People.Resources.FullNameWithoutMiddleFormat(firstName, lastName);
      }
      
      return Parties.PublicFunctions.Module.GetSurnameAndInitialsInTenantCulture(firstName, middleName, lastName);
    }
    
    /// <summary>
    /// Получить фамилию и инициалы.
    /// </summary>
    /// <param name="platformLogic">Дублировать платформенную логику.</param>
    /// <returns>Фамилия и инициалы.</returns>
    [Public]
    public virtual string GetShortName(bool platformLogic)
    {
      return this.GetShortName(DeclensionCase.Nominative, platformLogic);
    }
    
    /// <summary>
    ///  Получить фамилию и инициалы в формате И.О. Фамилия.
    /// </summary>
    /// <returns>Фамилия и инициалы.</returns>
    [Public]
    public virtual string GetReverseShortName()
    {
      var lastName = _obj.Person.LastName;
      var firstName = _obj.Person.FirstName;
      var middleName = _obj.Person.MiddleName;
      
      if (string.IsNullOrWhiteSpace(firstName))
        return lastName;
      
      if (string.IsNullOrEmpty(middleName))
        return Sungero.Parties.People.Resources.ShortReverseNameWithoutMiddleFormat(firstName.ToUpper()[0], lastName);
      
      return Sungero.Parties.People.Resources.ShortReverseNameFormat(firstName.ToUpper()[0], middleName.ToUpper()[0], lastName);
    }
    
    #endregion
    
    /// <summary>
    /// Обновить ФИО сотрудника.
    /// </summary>
    /// <param name="person">Персона.</param>
    [Public]
    public void UpdateName(Parties.IPerson person)
    {
      if (person != null && !Equals(person.Name, _obj.Name))
        _obj.Name = person.Name;
    }

    /// <summary>
    /// Установить обязательность свойства Email.
    /// </summary>
    [Public]
    public void SetRequiredProperties()
    {
      var isEmailRequired = _obj.NeedNotifyExpiredAssignments == true || _obj.NeedNotifyNewAssignments == true || _obj.NeedNotifyAssignmentsSummary == true;
      
      _obj.State.Properties.Email.IsRequired = isEmailRequired;
    }

    /// <summary>
    /// Проверить Email-адрес на валидность.
    /// </summary>
    /// <param name="emailAddress">Email-адрес.</param>
    /// <returns>True - если непустой email является валидным.</returns>
    public static bool IsValidEmail(string emailAddress)
    {
      return string.IsNullOrEmpty(emailAddress) || Parties.PublicFunctions.Module.EmailIsValid(emailAddress);
    }

    /// <summary>
    /// Проверить, что корректно выполнена настройка уведомлений по почте.
    /// </summary>
    /// <param name="emailAddress">Email-адрес.</param>
    /// <param name="needNotifyNewAssignments">Нужно ли уведомлять о новых заданиях.</param>
    /// <param name="needNotifyExpiredAssignments">Нужно ли уведомлять о просроченных заданиях.</param>
    /// <returns>True - если при установленных галочках рассылки указан Email, либо галочки не установлены.</returns>
    public static bool IsValidNotificationSetting(string emailAddress, bool? needNotifyNewAssignments, bool? needNotifyExpiredAssignments)
    {
      return !string.IsNullOrEmpty(emailAddress) || (needNotifyNewAssignments != true && needNotifyExpiredAssignments != true);
    }

    /// <summary>
    /// Получить ФИО в указанном падеже.
    /// </summary>
    /// <param name="declensionCase">Падеж.</param>
    /// <returns>ФИО в указанном падеже.</returns>
    public virtual Structures.Employee.PersonFullName GetFullNameInDeclension(Sungero.Core.DeclensionCase declensionCase)
    {
      var fullName = CommonLibrary.PersonFullName.Create(_obj.Person.LastName,
                                                         _obj.Person.FirstName,
                                                         _obj.Person.MiddleName);
      
      var gender = CommonLibrary.Gender.NotDefined;
      if (_obj.Person.Sex != null)
        gender = _obj.Person.Sex == Sungero.Parties.Person.Sex.Female ?
          CommonLibrary.Gender.Feminine :
          CommonLibrary.Gender.Masculine;
      
      // Для фамилий типа Ардо (Иванова) неправильно склоняется через API. Баг 32895.
      var fullNameInDeclension = CommonLibrary.Padeg.ConvertPersonFullNameToTargetDeclension(fullName,
                                                                                             (CommonLibrary.DeclensionCase)(int)declensionCase,
                                                                                             gender);
      
      var lastName = fullNameInDeclension.LastName;
      var firstName = fullNameInDeclension.FirstName;
      var middleName = string.IsNullOrWhiteSpace(_obj.Person.MiddleName) ? string.Empty : fullNameInDeclension.MiddleName;
      
      return Structures.Employee.PersonFullName.Create(lastName, firstName, middleName);
    }

    /// <summary>
    /// Получить должность сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="declensionCase">Падеж.</param>
    /// <returns>Должность в нужном падеже.</returns>
    [Public]
    public static string GetJobTitle(IEmployee employee, Sungero.Core.DeclensionCase declensionCase)
    {
      if (employee == null)
        return string.Empty;
      
      if (employee.JobTitle == null)
        return string.Empty;
      
      return CaseConverter.ConvertJobTitleToTargetDeclension(employee.JobTitle.Name,
                                                             declensionCase);
    }
    
    /// <summary>
    /// Получить помощников руководителя, готовящих проекты резолюций.
    /// </summary>
    /// <returns>Список помощников, готовящих проекты резолюций.</returns>
    [Public]
    public virtual List<IManagersAssistant> GetManagerAssistantsWhoPrepareDraftResolution()
    {
      return this.GetManagerAssistants()
        .Where(x => x.PreparesResolution == true)
        .ToList();
    }
    
    /// <summary>
    /// Получить помощников руководителя.
    /// </summary>
    /// <returns>Список записей ассистентов руководителя.</returns>
    [Public]
    public virtual List<IManagersAssistant> GetManagerAssistants()
    {
      return Functions.Module.GetActiveManagersAssistants()
        .Where(m => Equals(_obj, m.Manager) && m.IsAssistant == true)
        .ToList();
    }
    
    /// <summary>
    /// Получить ассистентов руководителя по помощнику.
    /// </summary>
    /// <returns>Список записей ассистентов руководителя.</returns>
    [Public]
    public virtual List<IManagersAssistant> GetManagersByAssistant()
    {
      return Functions.Module.GetActiveManagersAssistants()
        .Where(m => Equals(_obj, m.Assistant))
        .ToList();
    }
    
    /// <summary>
    /// Получить JSON-строку для индексирования в поисковой системе.
    /// </summary>
    /// <returns>JSON-строка.</returns>
    public virtual string GetIndexingJson()
    {
      var lastName = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Person.LastName);
      var firstName = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Person.FirstName);
      var middleName = Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Person.MiddleName);
      return string.Format(Constants.Employee.ElasticsearchIndexTemplate,
                           _obj.Id,
                           Sungero.Commons.PublicFunctions.Module.TrimSpecialSymbols(_obj.Name),
                           lastName,
                           firstName,
                           middleName,
                           string.IsNullOrEmpty(firstName) ? string.Empty : firstName.Substring(0, 1),
                           string.IsNullOrEmpty(middleName) ? string.Empty : middleName.Substring(0, 1),
                           _obj.Department != null && _obj.Department.BusinessUnit != null ? _obj.Department.BusinessUnit.Id : 0,
                           Sungero.Core.Calendar.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                           _obj.Status.Value.Value);
    }
  }
}
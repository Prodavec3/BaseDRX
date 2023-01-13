using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Department;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  partial class DepartmentFunctions
  {

    /// <summary>
    /// Создать системные замещения.
    /// </summary>
    /// <param name="users">Список пользователей, для которых надо создать замещение.</param>
    /// <param name="manager">Руководитель.</param>
    public static void CreateSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> users, IUser manager)
    {
      foreach (var user in users)
        CreateSystemSubstitution(user, manager);
    }

    /// <summary>
    /// Удалить системные замещения.
    /// </summary>
    /// <param name="users">Список пользователей, для которых надо создать замещение.</param>
    /// <param name="manager">Руководитель.</param>
    [Obsolete("Используйте функцию DeleteSystemSubstitutions модуля Company.")]
    public static void DeleteSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> users, IUser manager)
    {
      Functions.Module.DeleteSystemSubstitutions(users, manager);
    }

    /// <summary>
    /// Получить удаляемых из подразделения сотрудников.
    /// </summary>
    /// <returns>Удаляемые сотрудники.</returns>
    public IQueryable<IEmployee> GetDeletedEmployees()
    {
      var employees = _obj.State.Properties.RecipientLinks.Deleted
        .Select(r => r.Member)
        .Where(m => m != null)
        .ToList();
      return Employees.GetAll().Where(r => employees.Contains(r));
    }

    /// <summary>
    /// Создать системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый пользователь.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    private static void CreateSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      if (substitutedUser.Equals(substitute))
        return;
      
      var substitution = Substitutions.Create();
      substitution.User = substitutedUser;
      substitution.Substitute = substitute;
      substitution.IsSystem = true;
      substitution.Save();
    }

    /// <summary>
    /// Синхронизировать руководителя в роль "Руководители подразделений".
    /// </summary>
    public virtual void SynchronizeManagerInRole()
    {
      var originalManager = _obj.State.Properties.Manager.OriginalValue;
      var manager = _obj.Manager;
      
      // Добавить руководителя в роль "Руководители подразделений".
      var managerRole = Roles.GetAll(r => r.Sid == Constants.Module.DepartmentManagersRole).SingleOrDefault();
      
      if (managerRole == null || (manager != null && manager.IncludedIn(managerRole) && originalManager != null &&
                                  Equals(originalManager, manager) && _obj.State.Properties.Status.OriginalValue == _obj.Status))
        return;
      
      var ceoRole = Functions.Module.GetCEORole();
      var managerRoleRecipients = managerRole.RecipientLinks;
      
      if (_obj.Status != CoreEntities.DatabookEntry.Status.Closed && manager != null && !manager.IncludedIn(managerRole) &&
          !manager.IncludedIn(ceoRole))
        managerRoleRecipients.AddNew().Member = manager;

      // Удалить руководителя из роли "Руководители подразделений"
      // при смене или закрытии, если он не руководитель других действующих организаций.
      if (originalManager != null &&
          (_obj.Status == CoreEntities.DatabookEntry.Status.Closed ||
           originalManager.IncludedIn(ceoRole) ||
           (!Equals(originalManager, manager) &&
            !Departments.GetAll(c => c.Status == CoreEntities.DatabookEntry.Status.Active &&
                                Equals(originalManager, c.Manager) &&
                                c.Id != _obj.Id).Any())))
      {
        while (managerRoleRecipients.Any(r => Equals(r.Member, originalManager)))
          managerRoleRecipients.Remove(managerRoleRecipients.First(r => Equals(r.Member, originalManager)));
      }
    }
    
    /// <summary>
    /// Получить подразделения.
    /// </summary>
    /// <returns>Подразделения.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDepartment> GetDepartments()
    {
      return Departments.GetAll();
    }
    
    /// <summary>
    /// Получить подразделение по ид.
    /// </summary>
    /// <param name="id">Ид подразделения.</param>
    /// <returns>Подразделение.</returns>
    [Remote(IsPure = true), Public]
    public static IDepartment GetDepartment(int id)
    {
      return Departments.GetAll().FirstOrDefault(d => d.Id == id);
    }
    
    /// <summary>
    /// Получить подразделения с учётом видимости орг. структуры.
    /// </summary>
    /// <returns>Подразделения.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IDepartment> GetVisibleDepartments()
    {
      var allDepartments = Departments.GetAll();
      if (Functions.Module.IsRecipientRestrict())
        return RestrictDepartments(allDepartments);
      
      return allDepartments;
    }
    
    /// <summary>
    /// Ограничить список подразделений, оставив только доступные в режиме ограниченной видимости орг. структуры.
    /// </summary>
    /// <param name="departments">Список подразделений.</param>
    /// <returns>Только те подразделения из списка, которые доступны в режиме ограниченной видимости орг. структуры.</returns>
    public static IQueryable<IDepartment> RestrictDepartments(IQueryable<IDepartment> departments)
    {
      var visibleRecipientIds = Functions.Module.GetVisibleRecipientIds(Constants.Module.DepartmentTypeGuid);
      return departments.Where(c => visibleRecipientIds.Contains(c.Id));
    }
    
    /// <summary>
    /// Получить ИД подчиненных подразделений.
    /// </summary>
    /// <returns>ИД подчиненных подразделений.</returns>
    [Remote(IsPure = true), Public]
    public virtual List<int> GetSubordinateDepartmentIds()
    {
      var result = new List<int>();
      var subordinateDepartments = Departments.GetAll(x => !Equals(x, _obj) && Equals(x.HeadOffice, _obj)).ToList();
      result.AddRange(subordinateDepartments.Select(x => x.Id));
      
      foreach (var department in subordinateDepartments)
        // Вызов через Functions позволяет передать аргументом подразделение, для которого должна быть выполнена функция.
        result.AddRange(Functions.Department.GetSubordinateDepartmentIds(department));
      
      return result;
    }
    
    /// <summary>
    /// Получить подразделение из настроек сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Подразделение.</returns>
    /// <remarks>Используется на слое.</remarks>
    [Public]
    public static Company.IDepartment GetDepartment(Company.IEmployee employee)
    {
      if (employee == null)
        return null;
      
      var department = Company.Departments.Null;
      var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(employee);
      if (settings != null)
        department = settings.Department;
      if (department == null)
        department = employee.Department;
      return department;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.BusinessUnit;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  partial class BusinessUnitFunctions
  {
    /// <summary>
    /// Получить нашу организацию сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Наша организация сотрудника.</returns>
    [Remote(IsPure = true), Public]
    public static IBusinessUnit GetBusinessUnit(IEmployee employee)
    {
      if (employee == null)
        return null;
      
      var organizations = Departments.GetAll(d => d.Status == Status.Active && d.RecipientLinks.Any(l => Equals(l.Member, employee)))
        .Where(department => department.BusinessUnit != null)
        .Select(department => department.BusinessUnit)
        .Distinct()
        .ToList();
      if (organizations.Count == 1)
        return organizations.First();
      return null;
    }
    
    /// <summary>
    /// Получить нашу организацию подразделения.
    /// </summary>
    /// <param name="department">Подразделение.</param>
    /// <returns>Наша организация подразделения.</returns>
    /// <remarks>Учитывая головные подразделения.</remarks>
    [Remote(IsPure = true), Public]
    public static IBusinessUnit GetBusinessUnit(IDepartment department)
    {
      if (department == null)
        return null;
      
      if (department.BusinessUnit != null)
        return department.BusinessUnit;
      
      return GetBusinessUnit(department.HeadOffice);
    }
    
    /// <summary>
    /// Получить наши организации.
    /// </summary>
    /// <returns>Наши организации.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IBusinessUnit> GetBusinessUnits()
    {
      return BusinessUnits.GetAll().Where(b => b.Status == Status.Active);
    }

    /// <summary>
    /// Получить нашу организацию по ид.
    /// </summary>
    /// <param name="id">Ид нашей организации.</param>
    /// <returns>Наша организация.</returns>
    [Remote(IsPure = true), Public]
    public static IBusinessUnit GetBusinessUnit(int id)
    {
      return BusinessUnits.GetAll().FirstOrDefault(b => b.Id == id);
    }
    
    /// <summary>
    /// Получить список НОР по ИНН/КПП.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <returns>Список НОР.</returns>
    /// <remarks>Используется на слое.</remarks>
    [Public]
    public static List<IBusinessUnit> GetBusinessUnits(string tin, string trrc)
    {
      var searchByTin = !string.IsNullOrWhiteSpace(tin);
      var searchByTrrc = !string.IsNullOrWhiteSpace(trrc);
      
      if (!searchByTin && !searchByTrrc)
        return new List<IBusinessUnit>();

      // Отфильтровать закрытые НОР.
      var businessUnits = BusinessUnits.GetAll().Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      // Поиск по ИНН, если ИНН передан.
      if (searchByTin)
      {
        var strongTinBusinessUnits = businessUnits.Where(x => x.TIN == tin).ToList();
        
        // Поиск по КПП, если КПП передан.
        if (searchByTrrc)
        {
          var strongTrrcBusinessUnits = strongTinBusinessUnits
            .Where(c => !string.IsNullOrWhiteSpace(c.TRRC) && c.TRRC == trrc)
            .ToList();
          
          if (strongTrrcBusinessUnits.Count > 0)
            return strongTrrcBusinessUnits;
          
          return strongTinBusinessUnits.Where(c => string.IsNullOrWhiteSpace(c.TRRC)).ToList();
        }
        return strongTinBusinessUnits;
      }
      return new List<IBusinessUnit>();
    }
    
    /// <summary>
    /// Создать права подписи для руководителя НОР.
    /// </summary>
    public virtual void UpdateSignatureSettings()
    {
      try
      {
        Docflow.PublicFunctions.SignatureSetting.Remote.UpdateBusinessUnitSetting(_obj);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("UpdateSignatureSettings: Cannot create SignatureSettings for BusinessUnit {0}.", ex, _obj.Id);
      }
    }

    /// <summary>
    /// Синхронизировать руководителя в роль "Руководители наших организаций".
    /// </summary>
    public virtual void SynchronizeCEOInRole()
    {
      var originalCEO = _obj.State.Properties.CEO.OriginalValue;
      var ceo = _obj.CEO;

      // Добавить руководителя в роль "Руководители наших организаций".
      var ceoRole = Functions.Module.GetCEORole();
      if (ceoRole == null)
        return;
      
      if (ceo != null && ceo.IncludedIn(ceoRole) && Equals(originalCEO, ceo) &&
          _obj.State.Properties.Status.OriginalValue == _obj.Status)
        return;

      var ceoRoleRecipients = ceoRole.RecipientLinks;
      
      if (_obj.Status != CoreEntities.DatabookEntry.Status.Closed && ceo != null && !ceo.IncludedIn(ceoRole))
        ceoRoleRecipients.AddNew().Member = ceo;

      // Удалить руководителя из роли "Руководители наших организаций"
      // при смене или закрытии, если он не руководитель других действующих организаций.
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Closed ||
          (originalCEO != null && !Equals(originalCEO, ceo) &&
           !BusinessUnits.GetAll(c => c.Status == CoreEntities.DatabookEntry.Status.Active &&
                                 Equals(originalCEO, c.CEO) &&
                                 c.Id != _obj.Id).Any()))
      {
        while (ceoRoleRecipients.Any(r => Equals(r.Member, originalCEO)))
          ceoRoleRecipients.Remove(ceoRoleRecipients.First(r => Equals(r.Member, originalCEO)));
      }
      
      // Исключить из роли "Руководители подразделений" нового руководителя НОР, либо
      // включить в роль "Руководители подразделений" при смене или закрытии, если он остался руководителем подразделения.
      var managerRole = Roles.GetAll(r => r.Sid == Constants.Module.DepartmentManagersRole).SingleOrDefault();
      if (managerRole != null)
      {
        var managerRoleRecipients = managerRole.RecipientLinks;
        // Исключить из роли "Руководители подразделений".
        if (_obj.Status != CoreEntities.DatabookEntry.Status.Closed && ceo != null && ceo.IncludedIn(managerRole))
        {
          while (managerRoleRecipients.Any(r => Equals(r.Member, ceo)))
            managerRoleRecipients.Remove(managerRoleRecipients.First(r => Equals(r.Member, ceo)));
        }
        
        if (originalCEO == null)
          return;
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Closed ||
            (Departments.GetAll().Any(d => Equals(d.Manager, originalCEO)) &&
             !BusinessUnits.GetAll(c => c.Status == CoreEntities.DatabookEntry.Status.Active &&
                                   Equals(originalCEO, c.CEO) &&
                                   c.Id != _obj.Id).Any()))
          // Включить в роль руководителя подразделений.
          managerRoleRecipients.AddNew().Member = originalCEO;
      }
    }
    
    /// <summary>
    /// Получить дубликаты нашей организации.
    /// </summary>
    /// <returns>Список Наши организации.</returns>
    [Remote(IsPure = true), Public]
    public List<IBusinessUnit> GetDuplicateBusinessUnit()
    {
      var duplicateBusinessUnits = new List<IBusinessUnit>();
      if (!string.IsNullOrWhiteSpace(_obj.TIN))
        duplicateBusinessUnits = BusinessUnits.GetAll(b => !Equals(b, _obj) && b.Status == CoreEntities.DatabookEntry.Status.Active &&
                                                      _obj.TIN == b.TIN && _obj.TRRC == b.TRRC).ToList();
      return duplicateBusinessUnits;
    }
    
    /// <summary>
    /// Получить Id всех подразделений, относящихся к НОР.
    /// </summary>
    /// <returns>Id всех подразделений, относящихся к НОР.</returns>
    [Remote(IsPure = true), Public]
    public List<int> GetAllDepartmentIds()
    {
      return this.GetAllDepartments().Select(x => x.Id).ToList();
    }
    
    /// <summary>
    /// Получить все подразделения, относящиеся к нашей организации.
    /// </summary>
    /// <returns>Все подразделения, относящиеся к нашей организации.</returns>
    [Remote(IsPure = true), Public]
    public List<IDepartment> GetAllDepartments()
    {
      var departmentsIn = Departments.GetAll()
        .Where(x => Equals(x.BusinessUnit, _obj))
        .ToList();
      
      var departmentsWoBusinessUnitButWithHeadOffice = Departments.GetAll()
        .Where(x => x.BusinessUnit == null && x.HeadOffice != null)
        .ToList();
      
      var departmentsHeadOfficeInBusinessUnit = new List<IDepartment>();
      do
      {
        departmentsHeadOfficeInBusinessUnit = departmentsWoBusinessUnitButWithHeadOffice.Where(x => departmentsIn.Any(y => Equals(y, x.HeadOffice))).ToList();
        departmentsIn.AddRange(departmentsHeadOfficeInBusinessUnit);
        foreach (var d in departmentsHeadOfficeInBusinessUnit)
          departmentsWoBusinessUnitButWithHeadOffice.Remove(d);
      } while (departmentsHeadOfficeInBusinessUnit.Count > 0);
      
      return departmentsIn;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить список guid для всех системных реципиентов за исключением "Все пользователи".
    /// </summary>
    /// <param name="fullSystemRecipientList">Включать в список системные роли: Администраторы, Аудиторы, Пользователи Solo, Менеджеры системы.</param>
    /// <returns>Список guid для системных реципиентов за исключением "Все пользователи".</returns>
    [Public]
    public virtual List<Guid> GetSystemRecipientsSidWithoutAllUsers(bool fullSystemRecipientList)
    {
      var systemRecipientsSid = new List<Guid>();
      if (fullSystemRecipientList)
      {
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.Administrators);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.Auditors);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.SoloUsers);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid);
      }
      
      systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers);
      systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.ServiceUsers);
      systemRecipientsSid.Add(Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.CollaborationService);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.DefaultGroup);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.DefaultUser);
      
      return systemRecipientsSid;
    }
    
    /// <summary>
    /// Получить помощников руководителей.
    /// </summary>
    /// <returns>Список помощников.</returns>
    public virtual IQueryable<IManagersAssistant> GetActiveManagersAssistants()
    {
      return Functions.Module.Remote.GetActiveManagerAssistants();
    }
    
    /// <summary>
    /// Получить несистемные активные записи сотрудников.
    /// </summary>
    /// <param name="recipients">Список исполнителей.</param>
    /// <returns>Несистемные активные записи исполнителей.</returns>
    [Public]
    public static List<IEmployee> GetNotSystemEmployees(List<IRecipient> recipients)
    {
      recipients = recipients.Where(x => x != null && x.Status == CoreEntities.DatabookEntry.Status.Active).ToList();
      var employees = Functions.Module.Remote.GetEmployeesFromRecipientsRemote(recipients)
        .Where(x => x.IsSystem != true && x.Status == CoreEntities.DatabookEntry.Status.Active)
        .Distinct()
        .ToList();
      
      return employees;
    }
  }
}
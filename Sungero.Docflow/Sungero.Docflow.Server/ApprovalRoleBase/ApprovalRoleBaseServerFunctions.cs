using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRoleBase;

namespace Sungero.Docflow.Server
{
  partial class ApprovalRoleBaseFunctions
  {

    /// <summary>
    /// Получить сотрудника из роли.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public static IEmployee GetPerformer(Enumeration? roleType, IApprovalTask task)
    {
      var role = Functions.ApprovalRoleBase.GetRole(roleType);
      if (role == null)
        return null;
      
      return Functions.ApprovalRoleBase.GetRolePerformer(role, task);
    }
    
    /// <summary>
    /// Получить сотрудника из роли.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public virtual IEmployee GetRolePerformer(IApprovalTask task)
    {
      return null;
    }
    
    /// <summary>
    /// Получить сотрудников роли "Согласующие".
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="additionalApprovers">Доп.согласующие.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IEmployee> GetApproversRolePerformers(IApprovalTask task, List<Sungero.CoreEntities.IRecipient> additionalApprovers)
    {
      return new List<IEmployee>();
    }
    
    /// <summary>
    /// Получить сотрудников роли "Адресаты".
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IEmployee> GetAddresseesRolePerformers(IApprovalTask task)
    {
      return new List<IEmployee>();
    }
    
    /// <summary>
    /// Получить сотрудников роли согласования с несколькими участниками.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IEmployee> GetRolePerformers(IApprovalTask task)
    {
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.Approvers)
        return this.GetApproversRolePerformers(task, null);
        
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.Addressees)
        return this.GetAddresseesRolePerformers(task);
      
      return new List<IEmployee>();
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRoleBase;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalRoleBaseFunctions
  {

    /// <summary>
    /// Получить роль по типу.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <returns>Роль.</returns>
    [Public]
    public static IApprovalRoleBase GetRole(Enumeration? roleType)
    {
      return ApprovalRoleBases.GetAllCached().Where(r => r.Type == roleType).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверка, все ли указанные виды подходят для роли.
    /// </summary>
    /// <param name="kinds">Виды.</param>
    /// <returns>True, если виды поддерживаются.</returns>
    public virtual bool SupportDocumentKinds(List<IDocumentKind> kinds)
    {
      var distinctKinds = kinds.Distinct().ToList();
      return distinctKinds.Count == Functions.ApprovalRoleBase.Filter(_obj, distinctKinds).Count;
    }
    
    /// <summary>
    /// Отфильтровать виды по роли.
    /// </summary>
    /// <param name="kinds">Виды.</param>
    /// <returns>Отфильтрованные виды.</returns>
    /// <remarks>Для видов в правилах согласования.</remarks>
    public virtual List<IDocumentKind> Filter(List<IDocumentKind> kinds)
    {
      return kinds;
    }
    
  }
}
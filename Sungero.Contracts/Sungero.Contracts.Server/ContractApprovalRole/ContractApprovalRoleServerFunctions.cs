using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts.ContractApprovalRole;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Server
{
  partial class ContractApprovalRoleFunctions
  {
    /// <summary>
    /// Получить сотрудника из роли по документу.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public override IEmployee GetRolePerformer(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (document == null)
        return base.GetRolePerformer(task); 
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ContractResp)
        return Functions.Module.GetPerformerContractResponsible(document);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ContRespManager)
        return this.GetPerformerContractResponsibleManager(document);
      
      return base.GetRolePerformer(task);
    }
    
    #region Вычисление ролей
    
    /// <summary>
    /// Получить руководителя сотрудника, ответственного за договор.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetPerformerContractResponsibleManager(IOfficialDocument document)
    {
      return Docflow.PublicFunctions.Module.Remote.GetManager(Functions.Module.GetPerformerContractResponsible(document));
    }
    
    #endregion
    
  }
}
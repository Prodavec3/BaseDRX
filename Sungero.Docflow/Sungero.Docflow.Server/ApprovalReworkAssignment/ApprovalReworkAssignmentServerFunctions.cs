using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalReworkAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      var task = ApprovalTasks.As(_obj.Task);
      var approvers = _obj.Approvers.Select(a => Recipients.As(a.Approver)).ToList();
      var reqApprovers = _obj.RegApprovers.Select(a => Recipients.As(a.Approver)).ToList();
      var addApprovers = approvers.Where(a => !reqApprovers.Contains(a)).ToList();
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(task, addApprovers, _obj.Signatory, _obj.Addressee, _obj.DeliveryMethod, _obj.ExchangeService);
    }
    
    #endregion
  }
}
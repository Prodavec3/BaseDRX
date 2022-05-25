using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalManagerAssignmentFunctions
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
      var approvers = _obj.AddApprovers.Select(a => a.Approver).ToList();
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(task, approvers, _obj.Signatory, _obj.Addressee, _obj.DeliveryMethod, _obj.ExchangeService);
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public StateView GetDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Docflow.PublicFunctions.Module.GetDocumentSummary(document);
    }
    
    #endregion
  }
}
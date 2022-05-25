using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSendingAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalSendingAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    #endregion
  }
}
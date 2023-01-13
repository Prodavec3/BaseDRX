using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalExecutionAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalExecutionAssignmentFunctions
  {
    /// <summary>
    /// Получить значение для поля Выдал.
    /// </summary>
    /// <returns>Значение поля Выдал.</returns>
    public virtual Company.IEmployee GetAssignedBy()
    {
      var assignedBy = Sungero.Company.Employees.Null;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return assignedBy;
      var task = ApprovalTasks.As(_obj.Task);
      var stages = Functions.ApprovalRuleBase.Remote.GetStages(task.ApprovalRule, document, task).Stages;
      
      // Автором резолюции вычислить адресата, либо подписывающего.
      if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Review))
        assignedBy = PublicFunctions.Module.Remote.GetResolutionAuthor(task);
      else if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Sign))
        assignedBy = task.Signatory;
      return assignedBy;
    }
  }
}
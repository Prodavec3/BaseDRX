using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemSupervisorAssignmentFunctions
  {
    /// <summary>
    /// Показать диалог подтверждения выполнения ведущего поручения.
    /// </summary>
    /// <returns>True, если запрос был подтвержден.
    /// False, если была нажата отмена.</returns>
    [Obsolete("Используйте метод ShowAbortParentActionItemConfirmationDialog")]
    public virtual bool ShowAcceptanceDialog()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var parentAssignment = Functions.ActionItemExecutionTask.GetParentAssignment(task);
      return Functions.Module.ShowCompleteParentActionItemConfirmationDialog(_obj, parentAssignment);
    }
  }
}
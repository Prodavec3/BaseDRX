using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement.Shared
{
  partial class ActionItemSupervisorAssignmentFunctions
  {
    /// <summary>
    /// Валидация задания на приемку работ контролером при отправке на доработку.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateActionItemSupervisorAssignment(Sungero.Core.IValidationArgs e)
    {
      bool isValid = true;
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ActionItemSupervisorAssignments.Resources.ReportCommentIsEmpty);
        isValid = false;
      }
      if (_obj.NewDeadline == null && ActionItemExecutionTasks.As(_obj.Task).HasIndefiniteDeadline != true)
      {
        e.AddError(_obj.Info.Properties.NewDeadline, ActionItemSupervisorAssignments.Resources.EmptyNewDeadline);
        isValid = false;
      }
      return isValid;
    }

  }
}
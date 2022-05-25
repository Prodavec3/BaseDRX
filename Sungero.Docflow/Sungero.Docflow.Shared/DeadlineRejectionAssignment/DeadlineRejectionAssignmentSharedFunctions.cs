using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineRejectionAssignment;

namespace Sungero.Docflow.Shared
{
  partial class DeadlineRejectionAssignmentFunctions
  {
    /// <summary>
    /// Валидация задания на отказ в продлении срока при повторном запросе.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateDeadlineRejectionAssignment(Sungero.Core.IValidationArgs e)
    {
      bool isValid = true;
      
      // Проверить заполненность комментария к повторному запросу.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(DeadlineRejectionAssignments.Resources.RequestCommentNotFilled);
        isValid = false;
      }
      
      // Новый срок должен быть позже старого.
      if (!Functions.Module.CheckDeadline(_obj.NewDeadline, _obj.CurrentDeadline))
      {
        e.AddError(_obj.Info.Properties.NewDeadline, DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
        isValid = false;
      }
      
      return isValid;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionTask;

namespace Sungero.Docflow.Shared
{
  partial class DeadlineExtensionTaskFunctions
  {
    
    /// <summary>
    /// Валидация старта задачи на продление срока.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateDeadlineExtensionTaskStart(Sungero.Core.IValidationArgs e)
    {
      var isValid = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
      
      // Проверить заполненность причины продления срока.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(DeadlineExtensionTasks.Resources.SpecifyReason);
        isValid = false;
      }
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, Calendar.Now))
      {
        e.AddError(_obj.Info.Properties.NewDeadline, RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
        isValid = false;
      }
      
      // Новый срок должен быть больше старого.
      if (e.IsValid && !Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, _obj.CurrentDeadline))
      {
        e.AddError(_obj.Info.Properties.NewDeadline, DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
        isValid = false;
      }
      
      return isValid;
    }
  }
}
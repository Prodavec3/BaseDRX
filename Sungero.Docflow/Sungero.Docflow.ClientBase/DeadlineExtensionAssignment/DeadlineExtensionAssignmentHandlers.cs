using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionAssignment;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionAssignmentClientHandlers
  {

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(_obj.Author, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (_obj.ScheduledDate < Calendar.Now)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Author, e.NewValue, Calendar.Now))
          e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      }
      else
      {
        // Новый срок должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.ScheduledDate))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

  }
}
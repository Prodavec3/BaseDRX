using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineRejectionAssignment;

namespace Sungero.Docflow
{
  partial class DeadlineRejectionAssignmentClientHandlers
  {

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (_obj.CurrentDeadline < Calendar.Now)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
          e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      }
      else
      {
        // Новый срок должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.CurrentDeadline))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

  }
}
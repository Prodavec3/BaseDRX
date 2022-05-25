using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionAssignment;

namespace Sungero.RecordManagement
{
  partial class DeadlineExtensionAssignmentClientHandlers
  {
    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e) 
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (_obj.ScheduledDate < Calendar.Now)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
          e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      }
      else
      {     
        // Новый срок поручения должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.ScheduledDate))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e) 
    {
      // Изменить доступность контролов на форме в соответствии со статусом.
      var isEnabled = _obj.Status.Value == Workflow.AssignmentBase.Status.InProcess;
      _obj.State.Properties.NewDeadline.IsEnabled = isEnabled;
      
      _obj.State.Properties.Reason.IsVisible = !string.IsNullOrWhiteSpace(_obj.Reason);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineRejectionAssignment;

namespace Sungero.RecordManagement
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
        // Новый срок поручения должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.CurrentDeadline))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

    public override void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e) 
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e) 
    {
      // Изменить доступность контролов на форме в соответствии со статусом.
      var isEnabled = _obj.Status.Value == Workflow.AssignmentBase.Status.InProcess;
      _obj.State.Properties.NewDeadline.IsEnabled = isEnabled;
      _obj.State.Properties.Deadline.IsEnabled = isEnabled;
    }
  }
}
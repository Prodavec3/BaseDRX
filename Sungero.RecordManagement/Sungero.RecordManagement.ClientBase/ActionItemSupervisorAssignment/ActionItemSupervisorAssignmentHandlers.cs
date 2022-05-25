using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement
{
  partial class ActionItemSupervisorAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.NewDeadline.IsEnabled = ActionItemExecutionTasks.As(_obj.Task).HasIndefiniteDeadline != true;
    }

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var assignee = ActionItemExecutionTasks.As(_obj.Task).Assignee;
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(assignee, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee, e.NewValue, Calendar.Now))
        e.AddWarning(ActionItemSupervisorAssignments.Resources.NewDeadlineLessThenToday);
      else if (e.NewValue != null &&
               e.NewValue != _obj.ScheduledDate &&
               !Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.ScheduledDate))
        e.AddError(ActionItemSupervisorAssignments.Resources.ImpossibleSpecifyDeadlineLessActionItemDeadline);
    }
  }

}
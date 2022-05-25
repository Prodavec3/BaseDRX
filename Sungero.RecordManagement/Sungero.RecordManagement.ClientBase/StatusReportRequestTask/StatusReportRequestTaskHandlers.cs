using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement
{
  partial class StatusReportRequestTaskClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.Assignee != null)
        _obj.State.Properties.Assignee.IsEnabled = false;
    }

    public virtual void AssigneeValueInput(Sungero.RecordManagement.Client.StatusReportRequestTaskAssigneeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue, _obj.MaxDeadline);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.MaxDeadline, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }
    
    public override void MaxDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(_obj.Assignee, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Assignee, e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineForAssignee);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow
{
  partial class FreeApprovalTaskClientHandlers
  {

    public override void MaxDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.Functions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Functions.Module.CheckDeadline(e.NewValue, Calendar.Now))
        e.AddError(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Subject.IsEnabled = false;
      if (_obj.Status != Workflow.Task.Status.Draft &&
          _obj.Status != Workflow.Task.Status.Aborted &&
          !Functions.FreeApprovalTask.HasDocumentAndCanRead(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

  }
}
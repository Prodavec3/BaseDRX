using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalReworkAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalReworkAssignmentApproversClientHandlers
  {

    public virtual void ApproversApproverValueInput(Sungero.Docflow.Client.FreeApprovalReworkAssignmentApproversApproverValueInputEventArgs e)
    {
      // Запрещено добавлять повторяющихся согласующих.
      if (FreeApprovalReworkAssignments.As(_obj.RootEntity).Approvers.Any(app => Equals(app.Approver, e.NewValue) && app.Id != _obj.Id))
        e.AddError(_obj.Info.Properties.Approver, ApprovalReworkAssignments.Resources.CantAddApproverTwice);
    }
  }

  partial class FreeApprovalReworkAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var canReadDocument = Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task));
      _obj.State.Properties.Approvers.IsVisible = canReadDocument;
      _obj.State.Properties.NewDeadline.IsVisible = canReadDocument;
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.Functions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (!Functions.Module.CheckDeadline(e.NewValue, Calendar.Now))
        e.AddError(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

  }
}
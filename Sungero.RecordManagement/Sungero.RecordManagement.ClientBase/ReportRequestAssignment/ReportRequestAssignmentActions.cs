using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ReportRequestAssignmentActions
  {
    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.State.Properties.Assignee.IsEnabled = false;
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate();
    }

    public virtual void Done(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Проверить заполненность отчета перед выполнением.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(StatusReportRequestTasks.Resources.ReportNotFilled);
        return;
      }
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.StatusReportRequestTask.ReportRequestAssignmentConfirmDialogID))
        e.Cancel();
    }

    public virtual bool CanDone(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }
}
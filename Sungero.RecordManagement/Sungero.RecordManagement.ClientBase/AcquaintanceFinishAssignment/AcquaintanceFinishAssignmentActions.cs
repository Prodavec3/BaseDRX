using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceFinishAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class AcquaintanceFinishAssignmentActions
  {
    public virtual void ShowAcquaintanceReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = AcquaintanceTasks.As(_obj.Task);
      RecordManagement.Functions.Module.GetAcquaintanceReport(task).Open();
    }

    public virtual bool CanShowAcquaintanceReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task));
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var newDeadline = Sungero.Docflow.Client.ApprovalTaskFunctions.GetNewDeadline(_obj.Deadline);
      if (newDeadline != null)
      {
        _obj.Deadline = newDeadline.Value;
        _obj.Save();
        Dialogs.NotifyMessage(Docflow.Resources.CurrentAssignmentNewDeadline);
      }
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate() &&
        Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task));
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }
    
    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task));
    }

  }

}
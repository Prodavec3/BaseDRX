using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class DeadlineExtensionAssignmentActions
  {
    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, 
                                                                                            _obj.OtherGroup.All.ToList(), 
                                                                                            e.Action))
        e.Cancel();
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Accept(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, 
                                                                                            _obj.OtherGroup.All.ToList(), 
                                                                                            e.Action))
        e.Cancel();
    }

    public virtual bool CanAccept(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }
}
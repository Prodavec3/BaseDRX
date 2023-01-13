using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalFinishAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalFinishAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.FreeApprovalTask.HasDocumentAndCanRead(FreeApprovalTasks.As(_obj.Task)))
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }
  }

}
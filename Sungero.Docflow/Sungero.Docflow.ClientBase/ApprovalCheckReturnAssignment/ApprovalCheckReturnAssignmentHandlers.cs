using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalCheckReturnAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalCheckReturnAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
    }

  }
}
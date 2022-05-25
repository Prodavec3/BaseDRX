using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Description.IsVisible = !string.IsNullOrWhiteSpace(_obj.Description);
      
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(Docflow.ApprovalTasks.Resources.NoRightsToDocument);
    }
  }

}
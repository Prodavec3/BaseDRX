using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceFinishAssignment;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceFinishAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Functions.AcquaintanceTask.HasDocumentAndCanRead(AcquaintanceTasks.As(_obj.Task)))
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

  }
}
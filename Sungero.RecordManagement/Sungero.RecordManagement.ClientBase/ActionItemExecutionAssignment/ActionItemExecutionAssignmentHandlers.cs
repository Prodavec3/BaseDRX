using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionAssignment;

namespace Sungero.RecordManagement
{
  partial class ActionItemExecutionAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (!Docflow.IncomingDocumentBases.Is(_obj.DocumentsGroup.OfficialDocuments.FirstOrDefault()))
        e.HideAction(_obj.Info.Actions.CreateReplyLetter);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }
  }
}
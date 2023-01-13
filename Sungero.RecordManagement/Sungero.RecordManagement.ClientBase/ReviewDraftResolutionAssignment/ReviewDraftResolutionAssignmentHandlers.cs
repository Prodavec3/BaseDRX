using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewDraftResolutionAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var canReadDocument = Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
      _obj.State.Properties.Addressee.IsVisible = canReadDocument;
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

  }
}
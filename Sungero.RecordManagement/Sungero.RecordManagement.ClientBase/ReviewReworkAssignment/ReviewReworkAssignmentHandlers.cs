using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewReworkAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewReworkAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Attachments.ResolutionGroup.IsVisible = Functions.ReviewReworkAssignment.CanPrepareDraftResolution(_obj);
      var canReadDocument = Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
      _obj.State.Properties.Addressee.IsVisible = canReadDocument;
      
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (!Functions.ReviewReworkAssignment.CanPrepareDraftResolution(_obj))
      {
        e.HideAction(_obj.Info.Actions.AddResolution);
        e.HideAction(_obj.Info.Actions.PrintResolution);
      }
      if (!Functions.DocumentReviewTask.Remote.CurrentUserIsPerformerOrSubstitute(_obj.Performer))
      {
        e.HideAction(_obj.Info.Actions.Abort);
        e.HideAction(_obj.Info.Actions.AddResolution);
      }
    }
  }

}
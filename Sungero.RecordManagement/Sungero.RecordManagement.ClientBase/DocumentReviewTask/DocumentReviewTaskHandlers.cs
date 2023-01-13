using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      e.Params.AddOrUpdate(RecordManagement.Constants.DocumentReviewTask.WorkingWithGuiParamName, true);
      
      _obj.State.Attachments.ResolutionGroup.IsVisible = Functions.DocumentReviewTask.CanPrepareDraftResolution(_obj);
      
      if (_obj.Status != Workflow.Task.Status.Draft &&
          _obj.Status != Workflow.Task.Status.Aborted &&
          !Functions.DocumentReviewTask.HasDocumentAndCanRead(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      e.Params.AddOrUpdate(RecordManagement.Constants.DocumentReviewTask.WorkingWithGuiParamName, true);
      
      if (!Functions.DocumentReviewTask.CanPrepareDraftResolution(_obj))
        e.HideAction(_obj.Info.Actions.AddResolution);
    }
  }
}
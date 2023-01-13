using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewManagerAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewManagerAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var oldVersion = _obj.Task.GetStartedSchemeVersion() == LayerSchemeVersions.V1;
      var canReadDocument = Functions.DocumentReviewTask.HasDocumentAndCanRead(DocumentReviewTasks.As(_obj.Task));
      _obj.State.Properties.Addressee.IsVisible = !oldVersion && canReadDocument;
      
      if (!canReadDocument)
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.Task.GetStartedSchemeVersion() == LayerSchemeVersions.V1)
        e.HideAction(_obj.Info.Actions.Forward);
      
      // Скрывать результат выполнения "Вернуть инициатору" для задач, стартованных на ранних версиях схемы или в рамках согласования по регламенту.
      var schemeSupportsRework = Functions.DocumentReviewTask.SchemeVersionSupportsRework(_obj.Task);
      var reviewStartedFromApproval = Functions.DocumentReviewTask.ReviewStartedFromApproval(_obj.Task);
      if (!schemeSupportsRework || reviewStartedFromApproval)
        e.HideAction(_obj.Info.Actions.ForRework);
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class PreparingDraftResolutionAssignmentSharedHandlers
  {

    public virtual void ResolutionGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var task = ActionItemExecutionTasks.As(e.Attachment);
      if (task != null)
      {
        task.IsDraftResolution = true;
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          task.DocumentsGroup.OfficialDocuments.Add(document);
        foreach (var otherGroupAttachment in _obj.OtherGroup.All)
          task.OtherGroup.All.Add(otherGroupAttachment);
      }
    }

  }
}
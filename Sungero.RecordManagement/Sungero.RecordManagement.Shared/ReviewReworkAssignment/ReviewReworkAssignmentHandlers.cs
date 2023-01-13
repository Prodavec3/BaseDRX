using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewReworkAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewReworkAssignmentSharedHandlers
  {

    public virtual void ResolutionGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        foreach (var actionItem in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true))
          _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(actionItem);
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.FindNotDraftResolution);
      }
    }

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
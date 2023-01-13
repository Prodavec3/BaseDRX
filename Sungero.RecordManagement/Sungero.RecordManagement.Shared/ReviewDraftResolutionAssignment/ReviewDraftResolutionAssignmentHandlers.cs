using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewDraftResolutionAssignmentSharedHandlers
  {

    public virtual void ResolutionGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var task = ActionItemExecutionTasks.As(e.Attachment);
      var documentReviewTask = DocumentReviewTasks.As(_obj.Task);
      if (task != null)
      {
        task.IsDraftResolution = true;
        Functions.Module.SynchronizeAttachmentsToActionItem(_obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault(),
                                                            _obj.AddendaGroup.OfficialDocuments.Select(x => Sungero.Content.ElectronicDocuments.As(x)).ToList(),
                                                            Functions.DocumentReviewTask.GetAddedAddenda(documentReviewTask),
                                                            Functions.DocumentReviewTask.GetRemovedAddenda(documentReviewTask),
                                                            _obj.OtherGroup.All.ToList(),
                                                            task);
      }
    }

  }
}
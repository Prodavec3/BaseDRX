using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionTask;

namespace Sungero.RecordManagement
{
  partial class DeadlineExtensionTaskSharedHandlers
  {
    public virtual void DocumentsGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, null);
    }

    public virtual void DocumentsGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      if (!_obj.State.IsCopied)
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > DeadlineExtensionTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, DeadlineExtensionTasks.Info.Properties.Subject.Length);
    }
  }
}
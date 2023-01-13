using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceTaskSharedHandlers
  {

    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.AcquaintanceTask.RemovedAddendaAppend(_obj, addendum);
      Functions.AcquaintanceTask.AddedAddendaRemove(_obj, addendum);
    }

    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.AcquaintanceTask.AddedAddendaAppend(_obj, addendum);
      Functions.AcquaintanceTask.RemovedAddendaRemove(_obj, addendum);
    }

    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.AcquaintanceTask.AddedAddendaAppend(_obj, addendum);
      Functions.AcquaintanceTask.RemovedAddendaRemove(_obj, addendum);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > AcquaintanceTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, AcquaintanceTasks.Info.Properties.Subject.Length);
    }

    public virtual void DocumentGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      _obj.Subject = _obj.DocumentGroup.All.Any()
        ? AcquaintanceTasks.Resources.AcquaintanceTaskSubjectFormat(_obj.DocumentGroup.All.First().DisplayValue)
        : Docflow.Resources.AutoformatTaskSubject;
      
      // Очистка вложений.
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, null);
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      if (Docflow.OfficialDocuments.Is(document))
        Docflow.PublicFunctions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(Docflow.OfficialDocuments.As(document), _obj.OtherGroup);
      
      Functions.AcquaintanceTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }

    public virtual void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      _obj.Subject = AcquaintanceTasks.Resources.AcquaintanceTaskSubjectFormat(e.Attachment.DisplayValue);

      // Добавление вложений.
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      if (!_obj.State.IsCopied)
        Functions.AcquaintanceTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
    }
    
    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = e.NewValue;
    }
  }
}
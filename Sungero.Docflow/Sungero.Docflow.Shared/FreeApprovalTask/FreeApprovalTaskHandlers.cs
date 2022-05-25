using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow
{

  partial class FreeApprovalTaskSharedHandlers
  {
    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj, addendum);
      Functions.FreeApprovalTask.RemovedAddendaRemove(_obj, addendum);
    }
    
    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj, addendum);
      Functions.FreeApprovalTask.RemovedAddendaRemove(_obj, addendum);
    }
    
    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.FreeApprovalTask.RemovedAddendaAppend(_obj, addendum);
      Functions.FreeApprovalTask.AddedAddendaRemove(_obj, addendum);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > FreeApprovalTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, FreeApprovalTasks.Info.Properties.Subject.Length);
    }

    public virtual void ForApprovalGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      // Очистить группу "Дополнительно".
      var document = OfficialDocuments.As(e.Attachment);
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      
      _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
    }

    public virtual void ForApprovalGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = _obj.ForApprovalGroup.ElectronicDocuments.First();
      
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.TaskSubject, document.Name);

      if (!_obj.State.IsCopied)
      {
        Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
        if (OfficialDocuments.Is(document))
          Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      }
      
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.DocumentAttachedInMainGroup(OfficialDocuments.As(document), _obj);
    }

  }
}
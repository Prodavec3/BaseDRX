using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnTask;

namespace Sungero.Docflow
{
  partial class CheckReturnTaskSharedHandlers
  {

    public override void MaxDeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.AssignmentStartDate = e.NewValue.Value.AddWorkingDays(_obj.Assignee, -1);
      _obj.Deadline = e.NewValue;
    }
    
    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > CheckReturnTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, CheckReturnTasks.Info.Properties.Subject.Length);
    }
    
    public virtual void DocumentGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      _obj.DocumentToReturn = null;
    }

    public virtual void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      _obj.DocumentToReturn = ElectronicDocuments.As(e.Attachment);
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = CheckReturnTasks.Resources.ReturnTaskSubjectFormat(_obj.DocumentToReturn.Name);
    }
  }
}
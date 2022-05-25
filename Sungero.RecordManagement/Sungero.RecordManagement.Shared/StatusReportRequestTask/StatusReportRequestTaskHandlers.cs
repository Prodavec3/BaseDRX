using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.StatusReportRequestTask;

namespace Sungero.RecordManagement
{
  partial class StatusReportRequestTaskSharedHandlers
  {

    public virtual void DocumentsGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup,
                                                                           _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void DocumentsGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup,
                                                                           _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void AssigneeChanged(Sungero.RecordManagement.Shared.StatusReportRequestTaskAssigneeChangedEventArgs e)
    {
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      
      IActionItemExecutionTask newParentTask = null;
      IActionItemExecutionAssignment newParentAssignment = null;
      if (e.NewValue != null)
      {
        // Определить исполняемое выбранным сотрудником поручение, если отправляем запрос из задачи.
        if (parentTask != null)
        {
          if (parentTask.IsCompoundActionItem.Value)
          {
            var assignment = Functions.ActionItemExecutionTask.Remote.GetActionItems(parentTask)
              .Where(j => Equals(j.Performer, e.NewValue))
              .Where(a => ActionItemExecutionTasks.Is(a.Task))
              .First();
            newParentTask = ActionItemExecutionTasks.As(assignment.Task);
          }
          else
            newParentTask = parentTask;
        }
        
        // Определить исполняемое выбранным сотрудником поручение, если отправляем запрос из задания.
        if (parentAssignment != null)
        {
          newParentAssignment = Functions.ActionItemExecutionAssignment.Remote.GetActionItems(parentAssignment)
            .First(j => Equals(j.Performer, e.NewValue));
        }
        // Обновить тему.
        _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(newParentTask ?? ActionItemExecutionTasks.As(newParentAssignment.Task), StatusReportRequestTasks.Resources.ReportRequestTaskSubject);
      }
      else
        _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(parentTask ?? ActionItemExecutionTasks.As(parentAssignment.Task), StatusReportRequestTasks.Resources.ReportRequestTaskSubject);      
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue.Length > StatusReportRequestTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, StatusReportRequestTasks.Info.Properties.Subject.Length);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskCoAssigneesSharedCollectionHandlers
  {

    public virtual void CoAssigneesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      var coAssigneesExist = _obj.CoAssignees.Any();
      _obj.State.Properties.CoAssigneesDeadline.IsEnabled = coAssigneesExist;
      _obj.State.Properties.CoAssigneesDeadline.IsRequired = coAssigneesExist && _obj.HasIndefiniteDeadline != true;
      
      Functions.ActionItemExecutionTask.SetDefaultCoAssigneesDeadline(_obj);
    }
    
    public virtual void CoAssigneesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      var coAssigneesExist = _obj.CoAssignees.Any();
      _obj.State.Properties.CoAssigneesDeadline.IsEnabled = coAssigneesExist;
      _obj.State.Properties.CoAssigneesDeadline.IsRequired = coAssigneesExist && _obj.HasIndefiniteDeadline != true;
      
      if (_obj.CoAssigneesDeadline != null && !coAssigneesExist)
        _obj.CoAssigneesDeadline = null;
    }
  }

  partial class ActionItemExecutionTaskActionItemPartsSharedCollectionHandlers
  {

    public virtual void ActionItemPartsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      Functions.ActionItemExecutionTask.DeletePartsCoAssignees(_obj, _deleted);
    }
    
    public virtual void ActionItemPartsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.PartGuid = Guid.NewGuid().ToString();
      // Задать порядковый номер для пункта поручения.
      var lastNumber = _obj.ActionItemParts.OrderBy(j => j.Number).LastOrDefault();
      if (lastNumber.Number.HasValue)
        _added.Number = lastNumber.Number + 1;
      else
        _added.Number = 1;
      
      if (_added.State.IsCopied)
      {
        var coAssigneesCopy = Functions.ActionItemExecutionTask.GetPartCoAssignees(_obj, _source.PartGuid);
        Functions.ActionItemExecutionTask.AddPartsCoAssignees(_obj, _added, coAssigneesCopy);
      }

    }
  }

  partial class ActionItemExecutionTaskSharedHandlers
  {

    public virtual void AddendaGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ActionItemExecutionTask.RemovedAddendaAppend(_obj, addendum);
      Functions.ActionItemExecutionTask.AddedAddendaRemove(_obj, addendum);
    }

    public virtual void AddendaGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ActionItemExecutionTask.AddedAddendaAppend(_obj, addendum);
      Functions.ActionItemExecutionTask.RemovedAddendaRemove(_obj, addendum);
    }

    public virtual void AddendaGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var addendum = ElectronicDocuments.As(e.Attachment);
      if (addendum == null)
        return;
      
      Functions.ActionItemExecutionTask.AddedAddendaAppend(_obj, addendum);
      Functions.ActionItemExecutionTask.RemovedAddendaRemove(_obj, addendum);
    }

    public virtual void HasIndefiniteDeadlineChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        if (_obj.IsCompoundActionItem != true)
        {
          _obj.Deadline = null;
          _obj.CoAssigneesDeadline = null;
        }
        else
        {
          _obj.FinalDeadline = null;
          
          foreach (var part in _obj.ActionItemParts)
          {
            part.Deadline = null;
            part.CoAssigneesDeadline = null;
          }
        }
      }
      
      var isDeadlineEnabled = _obj.Status == Workflow.Task.Status.Draft && e.NewValue != true;
      _obj.State.Properties.Deadline.IsEnabled = isDeadlineEnabled;
      _obj.State.Properties.FinalDeadline.IsEnabled = isDeadlineEnabled;
      _obj.State.Properties.ActionItemParts.Properties.Deadline.IsEnabled = isDeadlineEnabled;
      
      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SupervisorChanged(Sungero.RecordManagement.Shared.ActionItemExecutionTaskSupervisorChangedEventArgs e)
    {
      /* _obj.AssignedBy пробрасывается при изменении в _obj.Author.
       * В _obj.StartedBy до старта задачи записывается тот, кто задачу создал.
       */
      var canAutoExec = _obj.IsUnderControl != true || !Equals(e.NewValue, _obj.StartedBy);
      if (!canAutoExec)
        _obj.IsAutoExec = false;
      // В десктоп-клиенте не отрабатывает событие Refresh при изменении значения Supervisor.
      _obj.State.Properties.IsAutoExec.IsEnabled = canAutoExec;
    }

    public virtual void ActionItemPartsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void CoAssigneesChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = e.NewValue ?? false;
      _obj.State.Properties.ActionItemParts.Properties.Supervisor.IsEnabled = e.NewValue ?? false;
      
      if (e.NewValue != null && e.NewValue == true)
      {
        _obj.Supervisor = Docflow.PublicFunctions.PersonalSetting.GetSupervisor(Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Employees.Current));
      }
      else
      {
        _obj.Supervisor = null;
        
        foreach (var actionItemPart in _obj.ActionItemParts)
          actionItemPart.Supervisor = null;
      }
      
      /* _obj.AssignedBy пробрасывается при изменении в _obj.Author.
       * В _obj.StartedBy до старта задачи записывается тот, кто задачу создал.
       */
      var canAutoExec = e.NewValue != true || !Equals(_obj.Supervisor, _obj.StartedBy);
      if (!canAutoExec)
        _obj.IsAutoExec = false;
      // В десктоп-клиенте не отрабатывает событие Refresh при изменении значения IsUnderControl.
      _obj.State.Properties.IsAutoExec.IsEnabled = canAutoExec;
      
      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
    }

    public virtual void AssignedByChanged(Sungero.RecordManagement.Shared.ActionItemExecutionTaskAssignedByChangedEventArgs e)
    {
      if (e.NewValue != null)
        _obj.Author = e.NewValue;
    }
    
    public virtual void DocumentsGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);
      
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
    }

    public virtual void DocumentsGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      
      // Заполнить исполнителя из документа для первого поручения по документу.
      var isActionItemAssigneeEmpty = _obj.IsCompoundActionItem == true ? !_obj.ActionItemParts.Any() : _obj.Assignee == null;
      if (document != null && document.Assignee != null && isActionItemAssigneeEmpty &&
          !Docflow.PublicFunctions.OfficialDocument.Remote.HasActionItemExecutionTasks(document))
      {
        if (_obj.IsCompoundActionItem == true)
          _obj.ActionItemParts.AddNew().Assignee = document.Assignee;
        else
          _obj.Assignee = document.Assignee;
      }
      
      if (!_obj.State.IsCopied)
        Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);
      
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      // Не синхронизировать приложения при копировании в случае программного вызова.
      // Такой вызов происходит внутри платформы при создании поручения копированием.
      var workingWithGui = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI);
      if (!_obj.State.IsCopied || workingWithGui)
        Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
    }

    public virtual void ActionItemChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      // Установить тему.
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      // Заменить первый символ на прописной.
      _obj.ActionItem = _obj.ActionItem != null ? _obj.ActionItem.Trim() : string.Empty;
      _obj.ActionItem = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(_obj.ActionItem);
      
      if (_obj.ActionItemType != ActionItemType.Additional && !string.IsNullOrWhiteSpace(_obj.ActionItem))
        _obj.ActiveText = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(_obj.ActiveText.Trim());
      
      _obj.State.Controls.Control.Refresh();
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > ActionItemExecutionTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, ActionItemExecutionTasks.Info.Properties.Subject.Length);
    }

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = e.NewValue;
      
      Functions.ActionItemExecutionTask.SetDefaultCoAssigneesDeadline(_obj);
    }

    public virtual void IsCompoundActionItemChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.OldValue != e.NewValue)
      {
        // Заполнить данные из составного поручения в обычное и наоборот.
        if (e.NewValue.Value)
        {
          // Составное поручение.
          _obj.ActionItemParts.Clear();
          _obj.FinalDeadline = _obj.Deadline;
          _obj.CoAssigneesDeadline = null;
          
          if (_obj.Assignee != null)
          {
            var newJob = _obj.ActionItemParts.AddNew();
            newJob.Assignee = _obj.Assignee;
          }
          
          foreach (var job in _obj.CoAssignees)
          {
            var newJob = _obj.ActionItemParts.AddNew();
            newJob.Assignee = job.Assignee;
          }
          _obj.CoAssignees.Clear();
        }
        else
        {
          // Не составное поручение.
          var actionItemPart = _obj.ActionItemParts.OrderBy(x => x.Number).FirstOrDefault();
          if (_obj.FinalDeadline != null)
            _obj.Deadline = _obj.FinalDeadline;
          else if (actionItemPart != null)
            _obj.Deadline = actionItemPart.Deadline;
          else
            _obj.Deadline = null;
          
          if (actionItemPart != null)
            _obj.Assignee = actionItemPart.Assignee;
          else
            _obj.Assignee = null;
          
          _obj.CoAssignees.Clear();
          
          foreach (var job in _obj.ActionItemParts.OrderBy(x => x.Number).Skip(1))
          {
            if (job.Assignee != null && !_obj.CoAssignees.Select(z => z.Assignee).Contains(job.Assignee))
              _obj.CoAssignees.AddNew().Assignee = job.Assignee;
          }
          
          if (string.IsNullOrEmpty(_obj.ActiveText) && actionItemPart != null)
          {
            _obj.ActiveText = actionItemPart.ActionItemPart;
            Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);
          }

          if (actionItemPart != null && _obj.Supervisor == null)
            _obj.Supervisor = actionItemPart.Supervisor;
          
          // Чистим грид в составном, чтобы не мешать валидации.
          _obj.ActionItemParts.Clear();
        }
        
        // Установить тему.
        var subjectTemplate = _obj.IsCompoundActionItem == true ?
          ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
          ActionItemExecutionTasks.Resources.TaskSubject;
        _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      }
      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
      _obj.State.Controls.Control.Refresh();
    }
  }
}
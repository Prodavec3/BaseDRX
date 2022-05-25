using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskActionItemPartsClientHandlers
  {

    public virtual void ActionItemPartsAssigneeValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskActionItemPartsAssigneeValueInputEventArgs e)
    {
      if (e.NewValue != null && !Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.Deadline, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, _obj.Info.Properties.Deadline);
    }
    
    public virtual void ActionItemPartsActionItemPartValueInput(Sungero.Presentation.TextValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public virtual void ActionItemPartsNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(ActionItemExecutionTasks.Resources.NumberIsNotPositive);
    }

    public virtual void ActionItemPartsDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var assignee = _obj.Assignee ?? Users.Current;
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(assignee, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Проверить корректность срока исполнителя.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee, e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday);
      
      // Проверить сроки соисполнителей.
      if (!Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(e.NewValue, _obj.CoAssigneesDeadline))
        e.AddError(RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError);
    }
  }

  partial class ActionItemExecutionTaskClientHandlers
  {

    public virtual void CoAssigneesDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue == e.OldValue || !e.NewValue.HasValue)
        return;
      
      // Срок соисполнителей должен быть больше или равен текущей дате.
      if (!Functions.ActionItemExecutionTask.CheckCoAssigneesDeadline(_obj, e.NewValue))
        e.AddError(ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday);

      var coAssignees = _obj.CoAssignees.Select(a => a.Assignee).ToList();
      // Срок выполнения соисполнителей выпадает на выходной день.
      foreach (IEmployee coAssignee in coAssignees)
      {
        var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(coAssignee, e.NewValue);
        if (!string.IsNullOrEmpty(warnMessage))
          e.AddWarning(warnMessage);
      }
    }

    public virtual void FinalDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      this.CheckDeadline(e, Users.Current);

      if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public virtual void SupervisorValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskSupervisorValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void AssigneeValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskAssigneeValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }
    
    public virtual void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var assignee = _obj.Assignee ?? Users.Current;
      this.CheckDeadline(e, assignee);

      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee, e.NewValue, Calendar.Now))
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (_obj.IsDraftResolution == true)
      {
        if (_obj.State.IsInserted)
          e.AddWarning(ActionItemExecutionTasks.Resources.WillBeAddToDocumentReviewTask);
        else
          e.AddWarning(ActionItemExecutionTasks.Resources.WillBeSentAfterApprove);
      }
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;
      var isTaskStateDraft = _obj.Status == Workflow.Task.Status.Draft;

      var properties = _obj.State.Properties;
      
      properties.ActionItemParts.IsVisible = isComponentResolution;
      
      properties.Assignee.IsVisible = !isComponentResolution;
      properties.Deadline.IsVisible = !isComponentResolution;
      properties.FinalDeadline.IsVisible = isComponentResolution;
      properties.CoAssignees.IsVisible = !isComponentResolution;
      properties.CoAssigneesDeadline.IsVisible = !isComponentResolution;
      
      var hasIndefiniteDeadline = false;
      var indefiniteDeadlineParamExists = e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadline);
      e.Params.TryGetValue(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadline, out hasIndefiniteDeadline);
      properties.HasIndefiniteDeadline.IsVisible = indefiniteDeadlineParamExists
        ? hasIndefiniteDeadline
        : Functions.Module.AllowActionItemsWithIndefiniteDeadline() || _obj.HasIndefiniteDeadline == true;
      
      if (e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadlineIsEnabled))
      {
        var hasIndefiniteDeadlineIsEnabled = _obj.State.Properties.HasIndefiniteDeadline.IsEnabled;
        e.Params.TryGetValue(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadlineIsEnabled, out hasIndefiniteDeadlineIsEnabled);
        _obj.State.Properties.HasIndefiniteDeadline.IsEnabled = hasIndefiniteDeadlineIsEnabled;
      }

      properties.AbortingReason.IsVisible = _obj.Status == Workflow.Task.Status.Aborted;
      
      _obj.State.Attachments.ResultGroup.IsVisible = _obj.ResultGroup.All.Any();
      _obj.State.Attachments.OtherGroup.IsVisible = isTaskStateDraft || _obj.OtherGroup.All.Any();

      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
      
      properties.IsUnderControl.IsEnabled = isTaskStateDraft;
      var isSupervisorEnabled = (_obj.IsUnderControl ?? false) && isTaskStateDraft;
      properties.Supervisor.IsEnabled = isSupervisorEnabled;
      properties.ActionItemParts.Properties.Supervisor.IsEnabled = isSupervisorEnabled;
      
      var isDeadlineEnabled = isTaskStateDraft && _obj.HasIndefiniteDeadline != true;
      properties.Deadline.IsEnabled = isDeadlineEnabled;
      properties.FinalDeadline.IsEnabled = isDeadlineEnabled;
      properties.ActionItemParts.Properties.Deadline.IsEnabled = isDeadlineEnabled;
      
      properties.IsAutoExec.IsVisible = _obj.ParentAssignment != null && ActionItemExecutionAssignments.Is(_obj.ParentAssignment);
      /* Смотрим на _obj.StartedBy.
       * _obj.AssignedBy пробрасывается при изменении в _obj.Author.
       * До старта задачи в _obj.StartedBy записывается тот, кто задачу создал.
       */
      properties.IsAutoExec.IsEnabled = !(_obj.IsUnderControl == true && Equals(_obj.Supervisor, _obj.StartedBy)) && isTaskStateDraft;
      properties.CoAssigneesDeadline.IsEnabled = _obj.CoAssignees.Any();
      
      e.Title = (_obj.Subject == Docflow.Resources.AutoformatTaskSubject) ? null : _obj.Subject;
      _obj.State.Controls.Control.Refresh();
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      e.Params.AddOrUpdate(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI, true);
      if (!e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadline))
        e.Params.AddOrUpdate(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadline,
                             Functions.Module.AllowActionItemsWithIndefiniteDeadline() || _obj.HasIndefiniteDeadline == true);
      
      if (_obj.ActionItemType.Value == ActionItemExecutionTask.ActionItemType.Additional)
        e.Params.AddOrUpdate(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadlineIsEnabled, ActionItemExecutionTasks.As(_obj.ParentAssignment.Task).HasIndefiniteDeadline == true);
      
      if (_obj.ActionItemType.Value == ActionItemExecutionTask.ActionItemType.Component)
        e.Params.AddOrUpdate(RecordManagement.Constants.ActionItemExecutionTask.HasIndefiniteDeadlineIsEnabled, ActionItemExecutionTasks.As(_obj.ParentTask).HasIndefiniteDeadline == true);
    }
    
    private void CheckDeadline(Sungero.Presentation.DateTimeValueInputEventArgs e, IUser user)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(user, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Предупреждение на установку даты больше даты основного поручения.
      var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      if (parentAssignment != null && parentAssignment.Deadline != null && Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, parentAssignment.Deadline))
        e.AddWarning(ActionItemExecutionTasks.Resources.DeadlineSubActionItemExecutionFormat(parentAssignment.Deadline.Value.ToUserTime().ToShortDateString()));
      _obj.State.Controls.Control.Refresh();
    }
  }
}
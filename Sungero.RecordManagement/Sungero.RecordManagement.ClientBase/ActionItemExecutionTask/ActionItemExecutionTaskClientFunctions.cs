using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.Structures.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemExecutionTaskFunctions
  {

    /// <summary>
    /// Получить заголовок и текст для диалога корректировки поручения/пункта поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <returns>Заголовок и текст для диалога корректировки поручения/пункта поручения.</returns>
    public IChangeDialogInfo GetDialogTitleAndText(IActionItemExecutionTask task)
    {
      var changeDialogInfo = Structures.ActionItemExecutionTask.ChangeDialogInfo.Create();
      var parentActionItem = ActionItemExecutionTasks.As(task.ParentTask);
      var actionItemPart = parentActionItem.ActionItemParts.Where(t => Equals(t.ActionItemPartExecutionTask, task)).FirstOrDefault();
      var supervisorText = string.Empty;
      if (task.IsUnderControl == true)
      {
        var actualSupervisor = task.Supervisor;
        supervisorText = ActionItemExecutionTasks.Resources.SupervisorFormat(actualSupervisor.Person.ShortName);
      }
      else
        supervisorText = ActionItemExecutionTasks.Resources.ActionItemNotUnderControl;
      
      if (!string.IsNullOrWhiteSpace(supervisorText) && supervisorText.Last() != '.')
        supervisorText += '.';
      
      var deadlineText = string.Empty;
      if (task.HasIndefiniteDeadline == true)
        deadlineText = ActionItemExecutionTasks.Resources.ActionItemWithoutDeadline;
      else
      {
        var actualDeadline = task.Deadline;
        deadlineText = actualDeadline.Value.HasTime() ? actualDeadline.Value.ToString("dd.MM.yyyy H:mm") : actualDeadline.Value.ToShortDateString();
      }
      
      var shortActionItem = task.ActiveText;
      if (!string.IsNullOrWhiteSpace(shortActionItem) && shortActionItem.Last() != '.')
        shortActionItem += '.';
      if (shortActionItem.Length > 50)
        shortActionItem = shortActionItem.Substring(0, 50) + Sungero.RecordManagement.ActionItemExecutionTasks.Resources.Ellipsis_ShortActionItemPart;
      changeDialogInfo.DialogText = ActionItemExecutionTasks.Resources.ChangeCompoundActionItemPartDialogInfoFormat(actionItemPart.Assignee.Person.ShortName,
                                                                                                                    deadlineText,
                                                                                                                    supervisorText,
                                                                                                                    shortActionItem);
      changeDialogInfo.DialogTitle = ActionItemExecutionTasks.Resources.ChangeActionItemPartFormat(actionItemPart.Number);
      return changeDialogInfo;
    }
    
    /// <summary>
    /// Отключение обязательности свойств для прекращения и рестарта поручения.
    /// </summary>
    public void DisablePropertiesRequirement()
    {
      if (_obj.Assignee == null)
        _obj.State.Properties.Assignee.IsRequired = false;
      if (_obj.Deadline == null)
        _obj.State.Properties.Deadline.IsRequired = false;
    }
    
    /// <summary>
    /// Изменить простое поручение.
    /// </summary>
    /// <returns>True - процесс корректировки успешно запустился, иначе - false.</returns>
    public virtual bool ChangeSimpleActionItem()
    {
      // Получить настройки модуля Делопроизводство для вычисления срока соисполнителей по умолчанию.
      var settings = Functions.Module.GetSettings();
      var deadlineShiftInDays = -settings.ControlRelativeDeadlineInDays ?? 0;
      var deadlineShiftInHours = -settings.ControlRelativeDeadlineInHours ?? 0;
      
      // Инициализировать изменения поручения.
      var changes = Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      changes.InitiatorOfChange = Users.Current;
      changes.OldAssignee = _obj.Assignee;
      changes.OldSupervisor = _obj.Supervisor;
      changes.OldDeadline = _obj.Deadline;
      changes.OldCoAssignees = _obj.CoAssignees.Select(a => a.Assignee).ToList();
      changes.CoAssigneesOldDeadline = _obj.CoAssigneesDeadline;
      changes.NewAssignee = changes.OldAssignee;
      changes.NewSupervisor = changes.OldSupervisor;
      changes.NewDeadline = changes.OldDeadline;
      changes.NewCoAssignees = changes.OldCoAssignees;
      changes.CoAssigneesNewDeadline = changes.CoAssigneesOldDeadline;
      changes.ChangeContext = Constants.ActionItemExecutionTask.ChangeContext.Simple;
      
      // Получить заголовок и текст диалога корректировки.
      var dialogTitle = string.Empty;
      var dialogText = string.Empty;
      if (_obj.ActionItemType == ActionItemType.Component)
      {
        var changeDialogInfo = this.GetDialogTitleAndText(_obj);
        dialogTitle = changeDialogInfo.DialogTitle;
        dialogText = changeDialogInfo.DialogText;
      }
      else
      {
        dialogTitle = ActionItemExecutionTasks.Resources.ChangeActionItem;
        dialogText = ActionItemExecutionTasks.Resources.ChangeActionItemDialogInfo;
      }
      
      var dialogOpenDate = Calendar.Now;
      var helpCode = Constants.ActionItemExecutionTask.ActionItemHelpCode;
      var dialog = Dialogs.CreateInputDialog(dialogTitle, dialogText);
      
      dialog.HelpCode = helpCode;
      
      // Контролер.
      var supervisor = dialog.AddSelect(_obj.Info.Properties.Supervisor.LocalizedName, false, _obj.Supervisor)
        .Where(s => s.Status == CoreEntities.DatabookEntry.Status.Active);
      supervisor.IsRequired = _obj.Supervisor != null;
      
      // Исполнитель.
      var assignee = dialog.AddSelect(_obj.Info.Properties.Assignee.LocalizedName, true, _obj.Assignee)
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active);
      assignee.IsEnabled = _obj.ExecutionState != ExecutionState.OnControl;
      
      // Срок исполнителя.
      var deadline = dialog.AddDate(_obj.Info.Properties.Deadline.LocalizedName, false, _obj.Deadline).AsDateTime();
      deadline.IsEnabled = _obj.ExecutionState != ExecutionState.OnControl;
      deadline.IsRequired = _obj.HasIndefiniteDeadline != true;
      
      // Соисполнители.
      var coAssignees = dialog.AddSelectMany(_obj.Info.Properties.CoAssignees.LocalizedName, false, _obj.CoAssignees.Select(x => x.Assignee).ToArray());
      coAssignees.IsEnabled = false;
      coAssignees.IsVisible = false;
      var coAssigneesText = dialog
        .AddMultilineString(_obj.Info.Properties.CoAssignees.LocalizedName, false, GetEmployeesText(coAssignees.Value))
        .RowsCount(3);
      coAssigneesText.IsEnabled = false;
      var addCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.AddCoAssignees);
      addCoAssignees.IsEnabled = _obj.ExecutionState != ExecutionState.OnControl;
      var deleteCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.RemoveCoAssignees);
      deleteCoAssignees.IsEnabled = _obj.ExecutionState != ExecutionState.OnControl;
      
      // Срок соисполнителей.
      var coAssigneesDeadline = dialog.AddDate(ActionItemExecutionTasks.Resources.CoAssigneesDeadlineDialog, false, _obj.CoAssigneesDeadline).AsDateTime();
      var coAssigneesExist = coAssignees.Value.Any();
      var coAssigneesHaveOldDeadline = changes.CoAssigneesOldDeadline.HasValue;
      coAssigneesDeadline.IsEnabled = coAssigneesExist && _obj.ExecutionState != ExecutionState.OnControl;
      coAssigneesDeadline.IsRequired = coAssigneesExist && coAssigneesHaveOldDeadline;
      
      // Обоснование.
      var editingReason = dialog.AddMultilineString(ActionItemExecutionTasks.Resources.EditingReason, true).RowsCount(2);
      
      var changeButton = dialog.Buttons.AddCustom(ActionItemExecutionTasks.Resources.Change);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(
        args =>
        {
          var supervisorChanged = !Equals(changes.OldSupervisor, changes.NewSupervisor);
          var deadlineChanged = changes.OldDeadline != changes.NewDeadline;
          var assigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee);
          var coAssigneesChanged = !changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees);
          var coAssigneeDeadlineChanged = changes.CoAssigneesOldDeadline != changes.CoAssigneesNewDeadline;
          
          CheckDeadlines(changes, deadline, coAssigneesDeadline, args);
          changeButton.IsEnabled = supervisorChanged || deadlineChanged || assigneeChanged || coAssigneeDeadlineChanged || coAssigneesChanged;

          var coAssigneesExistNow = changes.NewCoAssignees.Any();
          coAssigneesDeadline.IsEnabled = coAssigneesExistNow && _obj.ExecutionState != ExecutionState.OnControl;
          coAssigneesDeadline.IsRequired = coAssigneesExistNow && (coAssigneesHaveOldDeadline || changes.OldDeadline.HasValue || changes.NewDeadline.HasValue);
        });
      
      // Контролер.
      supervisor.SetOnValueChanged(
        (args) =>
        {
          changes.NewSupervisor = args.NewValue;
        });
      
      // Исполнитель.
      assignee.SetOnValueChanged(
        (args) =>
        {
          changes.NewAssignee = args.NewValue;
        });
      
      // Срок.
      deadline.SetOnValueChanged(
        (args) =>
        {
          changes.NewDeadline = args.NewValue;
          if (!coAssigneesDeadline.Value.HasValue && coAssignees.Value.Any())
          {
            coAssigneesDeadline.Value = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(args.NewValue,
                                                                                                     deadlineShiftInDays,
                                                                                                     deadlineShiftInHours);
          }
        });
      
      // Соисполнители.
      coAssignees.SetOnValueChanged(
        (args) =>
        {
          coAssigneesText.Value = GetEmployeesText(args.NewValue);
          changes.NewCoAssignees = args.NewValue.ToList();
          
          var coAssigneesExistNow = coAssignees.Value.Any();
          DateTime? newCoAssigneesDeadline;
          
          if (!coAssigneesExistNow)
          {
            newCoAssigneesDeadline = null;
          }
          else
          {
            newCoAssigneesDeadline = coAssigneesDeadline.Value ?? _obj.CoAssigneesDeadline;
            
            // Вычислить срок соисполнителей по умолчанию.
            if (newCoAssigneesDeadline == null)
            {
              newCoAssigneesDeadline = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(deadline.Value,
                                                                                                    deadlineShiftInDays,
                                                                                                    deadlineShiftInHours);
            }
          }
          
          coAssigneesDeadline.IsEnabled = coAssigneesExistNow;
          coAssigneesDeadline.IsRequired = coAssigneesExistNow;
          coAssigneesDeadline.Value = newCoAssigneesDeadline;
        });

      // Добавление соисполнителей.
      addCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = Company.PublicFunctions.Employee.Remote.GetEmployees()
            .Where(ca => ca.Status == CoreEntities.DatabookEntry.Status.Active)
            .ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForAdd);
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            var newCoAssignees = new List<IEmployee>();
            newCoAssignees.AddRange(coAssignees.Value);
            newCoAssignees.AddRange(selectedEmployees);
            coAssignees.Value = newCoAssignees.Distinct();
          }
        });
      
      // Удаление соисполнителей.
      deleteCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = coAssignees.Value.ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForDelete);
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            var newCoAssignees = new List<IEmployee>();
            foreach (var coAssignee in coAssignees.Value)
            {
              if (!selectedEmployees.Contains(coAssignee))
                newCoAssignees.Add(coAssignee);
            }
            coAssignees.Value = newCoAssignees;
          }
        });
      
      // Срок соисполнителя.
      coAssigneesDeadline.SetOnValueChanged(
        (args) =>
        {
          changes.CoAssigneesNewDeadline = args.NewValue;
        });
      
      // Причина корректировки.
      editingReason.SetOnValueChanged(
        (args) =>
        {
          changes.EditingReason = args.NewValue;
        });
      
      // Нажатие любой кнопки диалога.
      dialog.SetOnButtonClick(
        (args) =>
        {
          if (!Equals(args.Button, changeButton))
            return;
          
          CheckDeadlines(changes, deadline, coAssigneesDeadline, args);
          
          var errorMessage = Functions.ActionItemExecutionTask.Remote.CheckActionItemEditInDialog(_obj, assignee.Value,
                                                                                                  deadline.Value,
                                                                                                  dialogOpenDate);
          if (!string.IsNullOrWhiteSpace(errorMessage))
            args.AddError(errorMessage);
          
          if (string.IsNullOrWhiteSpace(changes.EditingReason) && !string.IsNullOrEmpty(changes.EditingReason))
            args.AddError(ActionItemExecutionTasks.Resources.EmptyEditingReason, editingReason);
          
        });
      
      // Показ диалога.
      if (dialog.Show() == changeButton)
      {
        // Показать диалог выдачи прав на вложения из группы "Дополнительно",
        // если у кого-то из участников нет на них прав.
        var accessRightGranted = this.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), changes);
        if (accessRightGranted == false)
          return false;

        // Обработать изменения поручения.
        Functions.ActionItemExecutionTask.Remote.ChangeSimpleActionItem(_obj, changes);
        
        // Создание подзадачек соисполнителям и пунктов поручений.
        // Асинхронное событие вызываем после выполнения ChangeSimpleActionItem, чтобы сохранились все изменения _obj.
        Functions.Module.Remote.ExecuteApplyActionItemLockIndependentChanges(changes, _obj.Id, _obj.OnEditGuid);
        
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Изменить составное поручение.
    /// </summary>
    /// <returns>True - процесс корректировки успешно запустился, иначе - false.</returns>
    public virtual bool ChangeCompoundActionItem()
    {
      // Инициализировать изменения поручения.
      var changes = Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      changes.OldCoAssignees = new List<IEmployee>();
      changes.NewCoAssignees = new List<IEmployee>();
      changes.InitiatorOfChange = Users.Current;
      changes.ChangeContext = Constants.ActionItemExecutionTask.ChangeContext.Compound;
      
      // Диалог корректировки.
      var dialogOpenDate = Calendar.Now;
      var supervisorText = string.Empty;
      if (_obj.IsUnderControl == true)
        supervisorText = _obj.Supervisor != null
          ? ActionItemExecutionTasks.Resources.SupervisorFormat(_obj.Supervisor.Person.ShortName)
          : ActionItemExecutionTasks.Resources.ActionItemOnControl;
      
      if (!string.IsNullOrWhiteSpace(supervisorText) && supervisorText.Last() != '.')
        supervisorText += '.';
      
      var deadlineText = string.Empty;
      if (_obj.HasIndefiniteDeadline == true)
        deadlineText = ActionItemExecutionTasks.Resources.ActionItemWithoutDeadline;
      else
        deadlineText = _obj.FinalDeadline != null
          ? (_obj.FinalDeadline.Value.HasTime() ? _obj.FinalDeadline.Value.ToString("dd.MM.yyyy H:mm") : _obj.FinalDeadline.Value.ToShortDateString())
          : ActionItemExecutionTasks.Resources.NotSpecified;
      var dialogText = ActionItemExecutionTasks.Resources.ChangeCompoundActionItemDialogTextFormat(deadlineText, supervisorText);
      var dialog = Dialogs.CreateInputDialog(ActionItemExecutionTasks.Resources.ChangeCompoundActionItem, dialogText);
      dialog.HelpCode = Constants.ActionItemExecutionTask.ActionItemHelpCode;

      // Пункты поручения.
      var selectedPartTasks = _obj.ActionItemParts.Select(x => x.ActionItemPartExecutionTask).ToList<IActionItemExecutionTask>();
      var actionItemPartsText = dialog
        .AddMultilineString(ActionItemExecutionTasks.Resources.Parts, false, ActionItemExecutionTasks.Resources.AllParts)
        .RowsCount(6);
      actionItemPartsText.IsEnabled = false;
      var deleteActionItemParts = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.ExcludeParts);
      
      // Скрытый контрол для увеличения ширины карточки.
      var fakeControl = dialog.AddString("1234567890123456789012345", false);
      fakeControl.IsVisible = false;
      
      // Контролёр.
      var supervisor = dialog.AddSelect(_obj.Info.Properties.Supervisor.LocalizedName, false, Employees.Null)
        .Where(s => s.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Срок.
      var deadline = dialog.AddDate(ActionItemExecutionTasks.Resources.Deadline, false).AsDateTime();
      
      // Причина корректировки.
      var editingReason = dialog.AddMultilineString(ActionItemExecutionTasks.Resources.EditingReason, true).RowsCount(2);
      
      var changeButton = dialog.Buttons.AddCustom(ActionItemExecutionTasks.Resources.Change);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(e =>
                          {
                            var deadlineChanged = changes.OldDeadline != changes.NewDeadline;
                            var supervisorChanged = !Equals(changes.OldSupervisor, changes.NewSupervisor);
                            var selectedPartsExist = selectedPartTasks.Count > 0;
                            
                            // Доступность кнопки "Скорректировать".
                            changeButton.IsEnabled = (deadlineChanged || supervisorChanged) && selectedPartsExist;
                            
                            // Валидация нового срока.
                            CheckDeadlines(changes, deadline, null, e);
                          });
      
      // Исключение пунктов для корректировки.
      deleteActionItemParts.SetOnExecute(
        () =>
        {
          var deletedActionItemParts = selectedPartTasks.ShowSelectMany(ActionItemExecutionTasks.Resources.SpecifyParts);
          if (deletedActionItemParts != null && deletedActionItemParts.Any())
          {
            foreach (var part in deletedActionItemParts)
              selectedPartTasks.Remove(part);
            actionItemPartsText.Value = GetActionItemPartsText(selectedPartTasks, _obj);
          }
        });
      
      // Контролер.
      supervisor.SetOnValueChanged(
        (args) =>
        {
          changes.NewSupervisor = args.NewValue;
        });
      
      // Срок.
      deadline.SetOnValueChanged(
        (args) =>
        {
          changes.NewDeadline = args.NewValue;
        });
      
      // Причина корректировки.
      editingReason.SetOnValueChanged(
        (args) =>
        {
          changes.EditingReason = args.NewValue;
        });
      
      // Нажатие любой кнопки диалога.
      dialog.SetOnButtonClick(
        (args) =>
        {
          if (!Equals(args.Button, changeButton))
            return;
          
          // Валидация наличия изменений.
          CheckActionItemParts(selectedPartTasks, changes, args);
          
          var errorMessage = Functions.ActionItemExecutionTask.Remote.CheckCompoundActionItemEditInDialog(_obj,
                                                                                                          selectedPartTasks,
                                                                                                          supervisor.Value,
                                                                                                          null,
                                                                                                          deadline.Value,
                                                                                                          dialogOpenDate);
          if (!string.IsNullOrWhiteSpace(errorMessage))
            args.AddError(errorMessage);
          
          if (string.IsNullOrWhiteSpace(changes.EditingReason) && !string.IsNullOrEmpty(changes.EditingReason))
            args.AddError(ActionItemExecutionTasks.Resources.EmptyEditingReason, editingReason);
        });
      
      // Показ диалога.
      if (dialog.Show() == changeButton)
      {
        // Показать диалог выдачи прав на вложения из группы "Дополнительно",
        // если у кого-то из участников нет на них прав.
        var accessRightGranted = this.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), changes);
        if (accessRightGranted == false)
          return false;
        
        // Записать в структуру ИД задач по выбранным для корректировки пунктам.
        changes.TaskIds = selectedPartTasks.Select(t => t.Id).ToList();
        changes.ActionItemPartsText = GetActionItemPartsNotifyText(selectedPartTasks, _obj);
        
        // Обработать изменения поручения.
        Functions.ActionItemExecutionTask.Remote.ChangeCompoundActionItem(_obj, changes);
        
        // Асинхронное событие вызываем после выполнения ChangeCompoundActionItem, чтобы сохранились все изменения _obj.
        Functions.Module.Remote.ChangeCompoundActionItemAsync(changes, _obj.Id, _obj.OnEditGuid);
        
        return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Изменить пункт составного поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт составного поручения.</param>
    public virtual void ChangeCompoundActionItemPart(IActionItemExecutionTaskActionItemParts actionItemPart)
    {
      // Получить настройки модуля Делопроизводство для вычисления срока соисполнителей по умолчанию.
      var settings = Functions.Module.GetSettings();
      var deadlineShiftInDays = -settings.ControlRelativeDeadlineInDays ?? 0;
      var deadlineShiftInHours = -settings.ControlRelativeDeadlineInHours ?? 0;
      
      // Инициализировать изменения поручения.
      var changes = Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      var actionItemPartExecutionTask = actionItemPart.ActionItemPartExecutionTask;
      changes.InitiatorOfChange = Users.Current;
      changes.OldAssignee = actionItemPartExecutionTask.Assignee;
      changes.NewAssignee = changes.OldAssignee;
      changes.OldDeadline = actionItemPartExecutionTask.Deadline;
      changes.NewDeadline = changes.OldDeadline;
      changes.OldCoAssignees = actionItemPartExecutionTask.CoAssignees.Select(a => a.Assignee).ToList();
      changes.NewCoAssignees = changes.OldCoAssignees;
      changes.OldSupervisor = actionItemPartExecutionTask.Supervisor;
      changes.NewSupervisor = changes.OldSupervisor;
      changes.CoAssigneesOldDeadline = actionItemPartExecutionTask.CoAssigneesDeadline;
      changes.CoAssigneesNewDeadline = changes.CoAssigneesOldDeadline;
      changes.ChangeContext = Constants.ActionItemExecutionTask.ChangeContext.Part;

      // Получить заголовок и текст диалога корректировки
      var changeDialogInfo = this.GetDialogTitleAndText(actionItemPartExecutionTask);
      var dialogTitle = changeDialogInfo.DialogTitle;
      var dialogText = changeDialogInfo.DialogText;
      
      var dialogOpenDate = Calendar.Now;
      var helpCode = Constants.ActionItemExecutionTask.ActionItemHelpCode;
      var dialog = Dialogs.CreateInputDialog(dialogTitle, dialogText);
      dialog.HelpCode = helpCode;
      
      // Контролер.
      var supervisor = dialog.AddSelect(actionItemPart.Info.Properties.Supervisor.LocalizedName, false, changes.OldSupervisor)
        .Where(s => s.Status == CoreEntities.DatabookEntry.Status.Active);
      supervisor.IsRequired = actionItemPartExecutionTask.Supervisor != null;
      
      // Исполнитель.
      var assignee = dialog.AddSelect(actionItemPart.Info.Properties.Assignee.LocalizedName, true, actionItemPartExecutionTask.Assignee)
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active);
      assignee.IsEnabled = actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
      
      // Срок исполнителя.
      var deadline = dialog.AddDate(actionItemPart.Info.Properties.Deadline.LocalizedName, false, changes.OldDeadline).AsDateTime();
      deadline.IsEnabled = actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
      deadline.IsRequired = actionItemPartExecutionTask.HasIndefiniteDeadline != true;
      
      // Соисполнители.
      var coAssignees = dialog.AddSelectMany(actionItemPart.Info.Properties.CoAssignees.LocalizedName, false, changes.OldCoAssignees.ToArray());
      coAssignees.IsEnabled = false;
      coAssignees.IsVisible = false;
      var coAssigneesText = dialog
        .AddMultilineString(actionItemPart.Info.Properties.CoAssignees.LocalizedName, false, string.Join("; ", coAssignees.Value.Select(x => x.Person.ShortName)))
        .RowsCount(3);
      coAssigneesText.IsEnabled = false;
      var addCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.AddCoAssignees);
      addCoAssignees.IsEnabled = actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
      var deleteCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.RemoveCoAssignees);
      deleteCoAssignees.IsEnabled = actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
      
      // Срок соисполнителей.
      var coAssigneesDeadline = dialog.AddDate(ActionItemExecutionTasks.Resources.CoAssigneesDeadlineDialog, false, changes.CoAssigneesOldDeadline).AsDateTime();
      var coAssigneesExist = coAssignees.Value.Any();
      var coAssigneesHaveOldDeadline = changes.CoAssigneesOldDeadline.HasValue;
      coAssigneesDeadline.IsEnabled = coAssigneesExist && actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
      coAssigneesDeadline.IsRequired = coAssigneesExist && coAssigneesHaveOldDeadline && actionItemPartExecutionTask.Deadline != null;
      
      // Обоснование.
      var editingReason = dialog.AddMultilineString(ActionItemExecutionTasks.Resources.EditingReason, true).RowsCount(2);
      
      var changeButton = dialog.Buttons.AddCustom(ActionItemExecutionTasks.Resources.Change);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(
        args =>
        {
          var supervisorChanged = !Equals(changes.OldSupervisor, changes.NewSupervisor);
          var deadlineChanged = changes.OldDeadline != changes.NewDeadline;
          var assigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee);
          var coAssigneesChanged = !changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees);
          var coAssigneeDeadlineChanged = changes.CoAssigneesOldDeadline != changes.CoAssigneesNewDeadline;
          
          CheckDeadlines(changes, deadline, coAssigneesDeadline, args);
          changeButton.IsEnabled = supervisorChanged || deadlineChanged || assigneeChanged || coAssigneeDeadlineChanged || coAssigneesChanged;

          var coAssigneesExistNow = changes.NewCoAssignees.Any();
          coAssigneesDeadline.IsEnabled = coAssigneesExistNow && actionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl;
          coAssigneesDeadline.IsRequired = coAssigneesExistNow && (coAssigneesHaveOldDeadline || changes.OldDeadline.HasValue || changes.NewDeadline.HasValue);
        });
      
      // Контролер.
      supervisor.SetOnValueChanged(
        (args) =>
        {
          changes.NewSupervisor = args.NewValue;
        });
      
      // Исполнитель.
      assignee.SetOnValueChanged(
        (args) =>
        {
          changes.NewAssignee = args.NewValue;
        });
      
      // Срок.
      deadline.SetOnValueChanged(
        (args) =>
        {
          changes.NewDeadline = args.NewValue;
          if (!coAssigneesDeadline.Value.HasValue && coAssignees.Value.Any())
          {
            coAssigneesDeadline.Value = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(args.NewValue,
                                                                                                     deadlineShiftInDays,
                                                                                                     deadlineShiftInHours);
          }
        });
      
      // Соисполнители.
      coAssignees.SetOnValueChanged(
        (args) =>
        {
          coAssigneesText.Value = string.Join("; ", coAssignees.Value.Select(x => x.Person.ShortName));
          changes.NewCoAssignees = args.NewValue.ToList();
          
          var coAssigneesExistNow = coAssignees.Value.Any();
          DateTime? newCoAssigneesDeadline;
          
          if (!coAssigneesExistNow)
          {
            newCoAssigneesDeadline = null;
          }
          else
          {
            newCoAssigneesDeadline = coAssigneesDeadline.Value ?? actionItemPartExecutionTask.CoAssigneesDeadline;
            
            // Вычислить срок соисполнителей по умолчанию.
            if (newCoAssigneesDeadline == null)
            {
              newCoAssigneesDeadline = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(deadline.Value,
                                                                                                    deadlineShiftInDays,
                                                                                                    deadlineShiftInHours);
            }
          }
          
          coAssigneesDeadline.IsEnabled = coAssigneesExistNow;
          coAssigneesDeadline.IsRequired = coAssigneesExistNow && coAssigneesHaveOldDeadline && actionItemPartExecutionTask.Deadline != null;
          coAssigneesDeadline.Value = newCoAssigneesDeadline;
        });
      
      // Добавление соисполнителей.
      addCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = Company.PublicFunctions.Employee.Remote.GetEmployees()
            .Where(ca => ca.Status == CoreEntities.DatabookEntry.Status.Active)
            .ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForAdd).ToList();
          
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            selectedEmployees.AddRange(coAssignees.Value);
            coAssignees.Value = selectedEmployees.Distinct();
          }
        });
      
      // Удаление соисполнителей.
      deleteCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = coAssignees.Value.ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForDelete);
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            var currentCoAssignees = coAssignees.Value.ToList();
            
            foreach (var employee in selectedEmployees)
            {
              currentCoAssignees.Remove(employee);
            }
            
            coAssignees.Value = currentCoAssignees;
          }
        });
      
      // Срок соисполнителя.
      coAssigneesDeadline.SetOnValueChanged(
        (args) =>
        {
          changes.CoAssigneesNewDeadline = args.NewValue;
        });
      
      // Причина корректировки.
      editingReason.SetOnValueChanged(
        (args) =>
        {
          changes.EditingReason = args.NewValue;
        });
      
      // Нажатие любой кнопки диалога.
      dialog.SetOnButtonClick(
        (args) =>
        {
          if (!Equals(args.Button, changeButton))
            return;
          
          CheckDeadlines(changes, deadline, coAssigneesDeadline, args);
          
          var errorMessage = Functions.ActionItemExecutionTask.Remote.CheckActionItemPartEditInDialog(actionItemPart.ActionItemExecutionTask,
                                                                                                      actionItemPart.ActionItemPartExecutionTask,
                                                                                                      assignee.Value, deadline.Value,
                                                                                                      dialogOpenDate);
          if (!string.IsNullOrWhiteSpace(errorMessage))
            args.AddError(errorMessage);
          
          if (string.IsNullOrWhiteSpace(changes.EditingReason) && !string.IsNullOrEmpty(changes.EditingReason))
            args.AddError(ActionItemExecutionTasks.Resources.EmptyEditingReason, editingReason);
        });
      
      // Показ диалога.
      if (dialog.Show() == changeButton)
      {
        // Показать диалог выдачи прав на вложения из группы "Дополнительно",
        // если у кого-то из участников нет на них прав.
        var accessRightGranted = this.ShowDialogGrantAccessRights(actionItemPartExecutionTask,
                                                                  actionItemPartExecutionTask.OtherGroup.All.ToList(), changes);
        if (accessRightGranted == false)
          return;
        
        // Протащить изменения в грид.
        // Контролера и срок обновить, только если они менялись, т.к. могли изначально браться из общих полей.
        if (!Equals(changes.OldSupervisor, changes.NewSupervisor))
          actionItemPart.Supervisor = supervisor.Value;
        if (!Equals(changes.OldDeadline, changes.NewDeadline))
          actionItemPart.Deadline = deadline.Value;
        actionItemPart.Assignee = assignee.Value;
        actionItemPart.CoAssigneesDeadline = coAssigneesDeadline.Value;
        Functions.ActionItemExecutionTask.DeletePartsCoAssignees(_obj, actionItemPart);
        Functions.ActionItemExecutionTask.AddPartsCoAssignees(_obj, actionItemPart, changes.NewCoAssignees);
        Functions.ActionItemExecutionTask.Remote.SetActionItemChangeDeadlinesParams(_obj, changes);
        _obj.Save();
        
        // Обработать изменения пункта поручения.
        Functions.ActionItemExecutionTask.Remote.ChangeSimpleActionItem(actionItemPartExecutionTask, changes);
        
        // Создание подзадачек соисполнителям и пунктов поручений.
        // Асинхронное событие вызываем после выполнения ChangeSimpleActionItem, чтобы сохранились все изменения actionItemPartExecutionTask.
        Functions.Module.Remote.ExecuteApplyActionItemLockIndependentChanges(changes, actionItemPartExecutionTask.Id, actionItemPartExecutionTask.OnEditGuid);
        
        // Показать уведомление об успешной корректировке.
        Dialogs.NotifyMessage(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeActionItemSuccess);
      }
    }
    
    /// <summary>
    /// Проверить сроки исполнителя и соисполнителя.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="assigneeDeadline">Поле диалога срок исполнителя.</param>
    /// <param name="coAssigneeDeadline">Поле диалога срок соисполнителя.</param>
    /// <param name="args">Аргументы события диалога.</param>
    public virtual void CheckDeadlines(IActionItemChanges changes,
                                       CommonLibrary.IDateDialogValue assigneeDeadline,
                                       CommonLibrary.IDateDialogValue coAssigneeDeadline,
                                       CommonLibrary.BaseInputDialogEventArgs args)
    {
      var assigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee);
      var deadlineChanged = changes.OldDeadline != changes.NewDeadline;
      // В случае корректировки соисполнителей проверка корректности сроков соисполнителей необходима, только если добавились новые соисполнители.
      var coAssigneesAdded = !changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees) && changes.NewCoAssignees.Except(changes.OldCoAssignees).Any();
      var coAssigneeDeadlineChanged = changes.CoAssigneesOldDeadline != changes.CoAssigneesNewDeadline;
      
      var needValidateDeadline = assigneeChanged || deadlineChanged;
      var needValidateCoAssigneesDeadline = coAssigneesAdded || coAssigneeDeadlineChanged;
      var needValidateLinkDeadlines = deadlineChanged || coAssigneeDeadlineChanged;
      
      if (needValidateLinkDeadlines)
      {
        // Срок соисполнителей не может быть больше срока ответственного исполнителя.
        if (!Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(changes.NewDeadline, changes.CoAssigneesNewDeadline))
          args.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError, new[] { assigneeDeadline, coAssigneeDeadline });
      }
      
      if (needValidateDeadline)
      {
        if (!Docflow.PublicFunctions.Module.CheckDeadline(changes.NewAssignee ?? Users.Current, changes.NewDeadline, Calendar.Now))
          args.AddError(ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday, assigneeDeadline);

        var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(changes.NewAssignee ?? Users.Current, changes.NewDeadline);
        if (!string.IsNullOrEmpty(warnMessage))
          args.AddWarning(warnMessage);
        
        // Предупреждение на установку даты больше даты основного поручения.
        var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
        if (parentAssignment != null && changes.NewDeadline.HasValue && parentAssignment.Deadline.HasValue &&
            Docflow.PublicFunctions.Module.CheckDeadline(changes.NewDeadline, parentAssignment.Deadline))
          args.AddWarning(ActionItemExecutionTasks.Resources.DeadlineSubActionItemExecutionFormat(parentAssignment.Deadline.Value.ToUserTime().ToShortDateString()));
      }
      
      if (needValidateCoAssigneesDeadline)
      {
        // Срок соисполнителей должен быть больше или равен текущей дате.
        if (!Docflow.PublicFunctions.Module.CheckCoAssigneesDeadline(changes.NewCoAssignees.ToList(), changes.CoAssigneesNewDeadline))
          args.AddError(ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday, coAssigneeDeadline);

        // Срок выполнения соисполнителей выпадает на выходной день.
        foreach (IEmployee coAssignee in changes.NewCoAssignees)
        {
          var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(coAssignee, changes.CoAssigneesNewDeadline);
          if (!string.IsNullOrEmpty(warnMessage))
            args.AddWarning(warnMessage);
        }
      }
    }
    
    /// <summary>
    /// Проверить наличие изменений при корректировке пунктов.
    /// </summary>
    /// <param name="actionItemParts">Пункты поручения.</param>
    /// <param name="changes">Изменения в пунктах.</param>
    /// <param name="args">Аргументы события диалога.</param>
    public virtual void CheckActionItemParts(List<IActionItemExecutionTask> actionItemParts, IActionItemChanges changes, CommonLibrary.BaseInputDialogEventArgs args)
    {
      var actionItemPartsInProcess = actionItemParts.Where(x => x.Status == Sungero.Workflow.Task.Status.InProcess);
      if (actionItemPartsInProcess.Count() == 0)
        return;
      
      if (_obj.HasIndefiniteDeadline != true && actionItemPartsInProcess.All(x => x.Deadline == changes.NewDeadline))
        args.AddError(ActionItemExecutionTasks.Resources.ActionItemOldDeadlineEqualsNew);
      
      if (actionItemPartsInProcess.All(x => Equals(x.Supervisor, changes.NewSupervisor) && x.Supervisor != null))
        args.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemOldSupervisorEqualsNew);
    }
    
    /// <summary>
    /// Заполнить пункт составного поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт составного поручения.</param>
    public virtual void FillCompoundActionItemPart(IActionItemExecutionTaskActionItemParts actionItemPart)
    {
      var settings = Functions.Module.GetSettings();
      var isSupervisorChanges = false;
      var isAssigneeChanges = false;
      var isDeadlineChanges = false;
      var isCoAssigneesChanges = false;
      var isCoAssigneesDeadlineChanges = false;
      var isActionItemTextChanges = false;
      var isAddItemPart = actionItemPart == null;
      var title = isAddItemPart ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AddCompoundActionItemPart :
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.EditCompoundActionItemPartFormat(actionItemPart.Number);
      var supervisorDefault = isAddItemPart ? _obj.Supervisor : actionItemPart.Supervisor ?? _obj.Supervisor;
      isSupervisorChanges = !isAddItemPart && actionItemPart.Supervisor == null && _obj.Supervisor != null;
      var assigneeDefault = isAddItemPart ? Sungero.Company.Employees.Null : actionItemPart.Assignee;
      var deadlineDefault = isAddItemPart ? _obj.FinalDeadline : actionItemPart.Deadline ?? _obj.FinalDeadline;
      isDeadlineChanges = !isAddItemPart && actionItemPart.Deadline == null && _obj.FinalDeadline != null;
      var coAssigneesDefault = isAddItemPart ? new List<IEmployee>() : Functions.ActionItemExecutionTask.GetPartCoAssignees(_obj, actionItemPart.PartGuid);
      
      DateTime? coAssigneesDeadlineDefault = null;
      if (!isAddItemPart && coAssigneesDefault.Any())
      {
        isCoAssigneesDeadlineChanges = actionItemPart.CoAssigneesDeadline == null && deadlineDefault != null;
        coAssigneesDeadlineDefault = actionItemPart.CoAssigneesDeadline;
      }
      
      var actionItemPartDefault = isAddItemPart ? string.Empty : actionItemPart.ActionItemPart;
      var titleButton = isAddItemPart ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AddButtonDialog :
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.EditButtonDialog;
      var dialog = Dialogs.CreateInputDialog(title);
      dialog.HelpCode = isAddItemPart ? Constants.ActionItemExecutionTask.AddActionItemHelpCode : Constants.ActionItemExecutionTask.EditActionItemHelpCode;
      
      var underControl = _obj.IsUnderControl == true;
      var supervisor = dialog.AddSelect(_obj.Info.Properties.Supervisor.LocalizedName, underControl, supervisorDefault)
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active);
      supervisor.IsEnabled = underControl;

      var assignee = dialog.AddSelect(_obj.Info.Properties.Assignee.LocalizedName, true, assigneeDefault)
        .Where(a => a.Status == CoreEntities.DatabookEntry.Status.Active);
      
      var deadline = dialog.AddDate(_obj.Info.Properties.Deadline.LocalizedName, false, deadlineDefault).AsDateTime();
      deadline.IsEnabled = _obj.HasIndefiniteDeadline != true;
      deadline.IsRequired = deadline.IsEnabled;
      
      var coAssignees = dialog.AddSelectMany(_obj.Info.Properties.CoAssignees.LocalizedName, false, coAssigneesDefault.ToArray());
      coAssignees.IsEnabled = false;
      coAssignees.IsVisible = false;
      var coAssigneesText = dialog
        .AddMultilineString(_obj.Info.Properties.CoAssignees.LocalizedName, false, Docflow.PublicFunctions.Module.GetCoAssigneesNames(coAssigneesDefault, false))
        .RowsCount(PublicConstants.Module.CoAssigneesTextRowsCount);
      coAssigneesText.IsEnabled = false;
      
      var addCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.AddCoAssignees);
      var deleteCoAssignees = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.RemoveCoAssignees);
      var coAssigneesDeadline = dialog.AddDate(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineDialog, false, coAssigneesDeadlineDefault).AsDateTime();
      coAssigneesDeadline.IsEnabled = coAssigneesDefault.Any();
      coAssigneesDeadline.IsRequired = coAssigneesDefault.Any() && _obj.HasIndefiniteDeadline != true;
      var actionItemPartText = dialog
        .AddMultilineString(_obj.Info.Properties.ActionItemParts.Properties.ActionItemPart.LocalizedName, false, actionItemPartDefault)
        .RowsCount(PublicConstants.Module.ActionItemPartTextRowsCount);
      
      var fillButton = dialog.Buttons.AddCustom(titleButton);
      dialog.Buttons.AddCancel();
      
      dialog.SetOnRefresh(
        (args) =>
        {
          if (deadline.Value.HasValue)
          {
            if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee.Value ?? Users.Current, deadline.Value, Calendar.Now))
              args.AddError(ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday, deadline);
            else
            {
              var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(assignee.Value ?? Users.Current, deadline.Value);
              if (!string.IsNullOrEmpty(warnMessage))
                args.AddWarning(warnMessage);
            }
          }
          
          if (coAssigneesDeadline.Value.HasValue)
          {
            // Срок соисполнителей должен быть больше или равен текущей дате.
            if (!Docflow.PublicFunctions.Module.CheckCoAssigneesDeadline(coAssignees.Value.ToList(), coAssigneesDeadline.Value))
              args.AddError(ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday);

            // Срок выполнения соисполнителей выпадает на выходной день.
            foreach (IEmployee coAssignee in coAssignees.Value)
            {
              var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(coAssignee, coAssigneesDeadline.Value);
              if (!string.IsNullOrEmpty(warnMessage))
                args.AddWarning(warnMessage);
            }
          }
          var assigneeDeadline = deadline.Value.HasValue ? deadline.Value : _obj.FinalDeadline;
          
          if (assigneeDeadline != null && coAssigneesDeadline.Value.HasValue && !Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(assigneeDeadline, coAssigneesDeadline.Value))
            args.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError);
          
          fillButton.IsEnabled = isSupervisorChanges || isAssigneeChanges || isDeadlineChanges || isCoAssigneesChanges || isCoAssigneesDeadlineChanges || isActionItemTextChanges;
        });
      
      // Контролер.
      supervisor.SetOnValueChanged(
        (args) =>
        {
          isSupervisorChanges = !Equals(args.NewValue, supervisorDefault);
        });
      
      // Исполнитель.
      assignee.SetOnValueChanged(
        (args) =>
        {
          isAssigneeChanges = !Equals(args.NewValue, assigneeDefault);
        });
      
      // Срок исполнителя.
      deadline.SetOnValueChanged(
        (args) =>
        {
          isDeadlineChanges = !Equals(args.NewValue, deadlineDefault);
          if (!coAssigneesDeadline.Value.HasValue && coAssignees.Value.Any())
          {
            coAssigneesDeadline.Value = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(args.NewValue, -settings.ControlRelativeDeadlineInDays ?? 0, -settings.ControlRelativeDeadlineInHours ?? 0);
          }
        });
      
      // Соисполнители.
      coAssignees.SetOnValueChanged(
        (args) =>
        {
          coAssigneesText.Value = Docflow.PublicFunctions.Module.GetCoAssigneesNames(coAssignees.Value.ToList(), false);
          isCoAssigneesChanges = !coAssigneesDefault.SequenceEqual(coAssignees.Value.ToList());
          
          var coAssigneesExist = coAssignees.Value.Any();
          coAssigneesDeadline.IsRequired = coAssigneesExist && _obj.HasIndefiniteDeadline != true;
          coAssigneesDeadline.IsEnabled = coAssigneesExist;
          
          if (!coAssigneesExist)
            coAssigneesDeadline.Value = null;
        });
      
      // Срок соисполнителей.
      coAssigneesDeadline.SetOnValueChanged(
        (args) =>
        {
          isCoAssigneesDeadlineChanges = !Equals(args.NewValue, coAssigneesDeadlineDefault);
        });
      
      // Текст поручения.
      actionItemPartText.SetOnValueChanged(
        (args) =>
        {
          isActionItemTextChanges = !Equals(args.NewValue, actionItemPartDefault ?? string.Empty);
        });
      
      #region Гиперссылки на добавление и удаление соисполнителей
      
      addCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = Company.PublicFunctions.Employee.Remote.GetEmployees()
            .Where(ca => ca.Status == CoreEntities.DatabookEntry.Status.Active)
            .ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForAdd).ToList();
          
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            var sourceCoAssignees = coAssignees.Value.ToList();
            sourceCoAssignees.AddRange(selectedEmployees);
            coAssignees.Value = sourceCoAssignees.Distinct();
            
            if (!coAssigneesDeadline.Value.HasValue && coAssignees.Value.Any())
            {
              coAssigneesDeadline.Value = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(deadline.Value, -settings.ControlRelativeDeadlineInDays ?? 0, -settings.ControlRelativeDeadlineInHours ?? 0);
            }
          }
        });
      
      deleteCoAssignees.SetOnExecute(
        () =>
        {
          var selectedEmployees = coAssignees.Value.ShowSelectMany(ActionItemExecutionTasks.Resources.СhooseCoAssigneesForDelete);
          if (selectedEmployees != null && selectedEmployees.Any())
          {
            var currentCoAssignees = coAssignees.Value.ToList();
            
            foreach (var employee in selectedEmployees)
            {
              currentCoAssignees.Remove(employee);
            }
            
            coAssignees.Value = currentCoAssignees;
          }
        });
      
      #endregion
      
      dialog.SetOnButtonClick(
        args =>
        {
          if (args.Button == fillButton)
          {
            if (deadline.Value.HasValue && !Docflow.PublicFunctions.Module.CheckDeadline(assignee.Value ?? Users.Current, deadline.Value, Calendar.Now))
            {
              args.AddError(ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday, deadline);
              return;
            }

            if (coAssigneesDeadline.Value.HasValue && !Docflow.PublicFunctions.Module.CheckCoAssigneesDeadline(coAssignees.Value.ToList(), coAssigneesDeadline.Value))
            {
              args.AddError(ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday);
              return;
            }
            
            var assigneeDeadline = deadline.Value.HasValue ? deadline.Value : _obj.FinalDeadline;
            
            if (assigneeDeadline != null && coAssigneesDeadline.Value.HasValue && !Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(assigneeDeadline, coAssigneesDeadline.Value))
            {
              args.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError);
              return;
            }
            
            if (args.IsValid)
            {
              if (isAddItemPart)
                Functions.ActionItemExecutionTask.AddActionItemPart(_obj, assignee.Value, deadline.Value, actionItemPartText.Value, coAssignees.Value.ToList(), coAssigneesDeadline.Value, supervisor.Value);
              else
                Functions.ActionItemExecutionTask.EditActionItemPart(_obj, actionItemPart, assignee.Value, deadline.Value, actionItemPartText.Value, coAssignees.Value.ToList(), coAssigneesDeadline.Value, supervisor.Value);
            }
          }
        });
      
      dialog.Show();
    }
    
    /// <summary>
    /// Проверить возможность корректировки поручения.
    /// </summary>
    /// <returns>True - корректировка возможна, иначе - false.</returns>
    public virtual bool CanChangeActionItem()
    {
      // Корректировать можно только поручения, созданные вручную, либо пункты составного поручения.
      // Простые поручения соисполнителям корректировать нельзя.
      if (_obj.ActionItemType != ActionItemType.Main && _obj.ActionItemType != ActionItemType.Component)
        return false;
      
      // Корректировать можно, только если есть права на изменение поручения.
      if (!_obj.AccessRights.CanUpdate())
        return false;
      
      // Корректировка недоступна в десктоп-клиенте.
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
        return false;
      
      // Возможность корректировки появилась только в 3 версии схемы
      // и только для поручений, находящихся в работе.
      return _obj.GetStartedSchemeVersion() >= LayerSchemeVersions.V3 &&
        _obj.Status == Sungero.Workflow.Task.Status.InProcess;
    }
    
    /// <summary>
    /// Показать диалог выдачи прав на вложения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="changes">Изменения в поручении.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.
    /// Null, если диалог показан не был.</returns>
    [Public]
    public virtual bool? ShowDialogGrantAccessRights(ITask task,
                                                     List<Domain.Shared.IEntity> attachments,
                                                     IActionItemChanges changes)
    {
      var newPerformers = new List<IRecipient>();
      if (changes.NewAssignee != null)
        newPerformers.Add(changes.NewAssignee);
      if (changes.NewSupervisor != null)
        newPerformers.Add(changes.NewSupervisor);
      if (changes.NewCoAssignees != null && changes.NewCoAssignees.Any())
        newPerformers.AddRange(changes.NewCoAssignees);
      
      return Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(task, attachments, newPerformers);
    }
    
    /// <summary>
    /// Получить строковое представление списка сотрудников.
    /// </summary>
    /// <param name="employees">Сотрудники.</param>
    /// <returns>Строковое представление списка сотрудников.</returns>
    private static string GetEmployeesText(IEnumerable<IEmployee> employees)
    {
      return string.Join("; ", employees.Select(x => x.Person.ShortName));
    }
    
    /// <summary>
    /// Получить текстовое представление пунктов составного поручения для корректировки.
    /// </summary>
    /// <param name="selectedActionItemTasks">Подзадачи, выбранные для исключения из корректировки.</param>
    /// <param name="mainActionItemExecutionTask">Основное составное поручение.</param>
    /// <returns>Текстовое представление пунктов.</returns>
    private static string GetActionItemPartsText(IEnumerable<IActionItemExecutionTask> selectedActionItemTasks,
                                                 IActionItemExecutionTask mainActionItemExecutionTask)
    {
      // Если ничего не выбрано.
      var selectedPartsCount = selectedActionItemTasks.Count();
      if (selectedPartsCount == 0)
        return ActionItemExecutionTasks.Resources.PartsNotSpecified;
      
      // Если выбраны все пункты.
      if (selectedPartsCount == mainActionItemExecutionTask.ActionItemParts.Count())
        return ActionItemExecutionTasks.Resources.AllParts;
      
      // Если выбрано меньше или равно половины.
      var selectedLessThanHalf = selectedPartsCount * 2 <= mainActionItemExecutionTask.ActionItemParts.Count();
      if (selectedLessThanHalf)
      {
        var actionItemPartsText = GetActionItemPartListText(selectedActionItemTasks, mainActionItemExecutionTask);
        return string.Join(Environment.NewLine, actionItemPartsText);
      }
      
      // Если выбрано больше половины.
      if (!selectedLessThanHalf)
      {
        // Замкнуть список для ускорения работы при количестве пунктов, близком к 100.
        var excludedActionItemTasks = mainActionItemExecutionTask.ActionItemParts
          .Where(x => !selectedActionItemTasks.Contains(x.ActionItemPartExecutionTask))
          .Select(x => x.ActionItemPartExecutionTask)
          .ToList();
        
        var actionItemPartsText = GetActionItemPartListText(excludedActionItemTasks, mainActionItemExecutionTask);
        return string.Format(ActionItemExecutionTasks.Resources.AllPartsExclude, string.Join(Environment.NewLine, actionItemPartsText));
      }
      
      // Иначе - пустая строка.
      return string.Empty;
    }
    
    /// <summary>
    /// Получить описание пунктов составного поручения, которые были изменены.
    /// </summary>
    /// <param name="selectedActionItemTasks">Выбранные для корректировки пункты.</param>
    /// <param name="mainActionItemExecutionTask">Основное составное поручение.</param>
    /// <returns>Описание пунктов, которые были изменены.</returns>
    private static string GetActionItemPartsNotifyText(IEnumerable<IActionItemExecutionTask> selectedActionItemTasks,
                                                       IActionItemExecutionTask mainActionItemExecutionTask)
    {
      // Если ничего не выбрано.
      var selectedPartsCount = selectedActionItemTasks.Count();
      if (selectedPartsCount == 0)
        return string.Empty;
      
      // Если выбраны все пункты.
      if (selectedPartsCount == mainActionItemExecutionTask.ActionItemParts.Count())
        return ActionItemExecutionTasks.Resources.AllParts;
      
      // Если выбрано меньше или равно половины.
      var selectedLessThanHalf = selectedPartsCount * 2 <= mainActionItemExecutionTask.ActionItemParts.Count();
      if (selectedLessThanHalf)
      {
        var actionItemPartsText = GetActionItemPartListText(selectedActionItemTasks, mainActionItemExecutionTask);
        return string.Format(ActionItemExecutionTasks.Resources.ChangedParts, string.Join(Environment.NewLine, actionItemPartsText));
      }
      
      // Если выбрано больше половины.
      if (!selectedLessThanHalf)
      {
        // Замкнуть список для ускорения работы при количестве пунктов, близком к 100.
        var excludedActionItemTasks = mainActionItemExecutionTask.ActionItemParts
          .Where(x => !selectedActionItemTasks.Contains(x.ActionItemPartExecutionTask))
          .Select(x => x.ActionItemPartExecutionTask)
          .ToList();
        
        var actionItemPartsText = GetActionItemPartListText(excludedActionItemTasks, mainActionItemExecutionTask);
        return string.Format(ActionItemExecutionTasks.Resources.AllPartsExclude, string.Join(Environment.NewLine, actionItemPartsText));
      }
      
      // Иначе - пустая строка.
      return string.Empty;
    }
    
    /// <summary>
    /// Получить текстовое представление списка пунктов составного поручения.
    /// </summary>
    /// <param name="actionItemTasks">Подзадачи, чьи пункты нужно получить.</param>
    /// <param name="mainActionItemExecutionTask">Основное составное поручение.</param>
    /// <returns>Текстовое представление списка пунктов.</returns>
    private static List<string> GetActionItemPartListText(IEnumerable<IActionItemExecutionTask> actionItemTasks,
                                                          IActionItemExecutionTask mainActionItemExecutionTask)
    {
      var sortedActionItemParts = mainActionItemExecutionTask.ActionItemParts
        .Where(p => actionItemTasks.Contains(p.ActionItemPartExecutionTask))
        .OrderBy(p => p.Number)
        .Select(p => ActionItemExecutionTasks.Resources.ShortPartViewTemplateFormat(p.Number,
                                                                                    p.ActionItemPartExecutionTask.Assignee.Person.ShortName,
                                                                                    GetDeadlineTextForActionItemPart(p),
                                                                                    p.ActionItemPartExecutionTask.ActiveText));
      
      var actionItemPartsText = new List<string>();
      foreach (var actionItemPart in sortedActionItemParts)
      {
        string actionItemPartText = actionItemPart;
        
        if (actionItemPartText.Length > Constants.ActionItemExecutionTask.ActionItemPartTextMaxLength)
          actionItemPartText = actionItemPartText.Substring(0, Constants.ActionItemExecutionTask.ActionItemPartTextMaxLength) +
            Sungero.RecordManagement.ActionItemExecutionTasks.Resources.Ellipsis_ShortActionItemPart;
        
        actionItemPartsText.Add(actionItemPartText);
      }
      
      return actionItemPartsText;
    }

    /// <summary>
    /// Получить срок соисполнителей по умолчанию.
    /// </summary>
    /// <param name="deadline">Срок.</param>
    /// <returns>Срок соисполнителей.</returns>
    public virtual DateTime? SetDefaultCoAssigneesDeadline(DateTime? deadline)
    {
      if (deadline.HasValue)
      {
        var relativeDeadline = deadline.Value.AddWorkingDays(Users.Current, -Constants.ActionItemExecutionTask.ControlRelativeDeadline);

        if (!deadline.Value.HasTime())
          relativeDeadline = relativeDeadline.Date;

        return Docflow.PublicFunctions.Module.CheckDeadline(Users.Current, relativeDeadline, Calendar.Now) ? relativeDeadline : deadline;
      }

      return null;
    }
    
    /// <summary>
    /// Получить текстовое представление срока для пункта составного поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт.</param>
    /// <returns>Текстовое представление срока.</returns>
    private static string GetDeadlineTextForActionItemPart(IActionItemExecutionTaskActionItemParts actionItemPart)
    {
      return actionItemPart.ActionItemPartExecutionTask.Deadline?.ToShortDateString()
        ?? RecordManagement.Resources.ActionItemIndefiniteDeadline;
    }
  }
}
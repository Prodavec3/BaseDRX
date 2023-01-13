using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.Structures.ActionItemExecutionTask;

namespace Sungero.RecordManagement.Shared
{
  partial class ActionItemExecutionTaskFunctions
  {

    /// <summary>
    /// Удалить всех соисполнителей для пункта поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт поручения.</param>
    [Public]
    public void DeletePartsCoAssignees(Sungero.RecordManagement.IActionItemExecutionTaskActionItemParts actionItemPart)
    {
      var partsCoAssignees = _obj.PartsCoAssignees.Where(p => p.PartGuid == actionItemPart.PartGuid).ToList();
      
      foreach (var partCoAssignees in partsCoAssignees)
      {
        _obj.PartsCoAssignees.Remove(partCoAssignees);
      }
      
      actionItemPart.CoAssignees = null;
    }

    /// <summary>
    /// Добавить соисполнителей для пункта поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт поручения.</param>
    /// <param name="coAssignees">Соисполнители.</param>
    [Public]
    public void AddPartsCoAssignees(Sungero.RecordManagement.IActionItemExecutionTaskActionItemParts actionItemPart, List<Sungero.Company.IEmployee> coAssignees)
    {
      foreach (var coAssignee in coAssignees)
      {
        var item = _obj.PartsCoAssignees.AddNew();
        item.CoAssignee = coAssignee;
        item.PartGuid = actionItemPart.PartGuid;
      }
      
      actionItemPart.CoAssignees = Docflow.PublicFunctions.Module.GetCoAssigneesNames(coAssignees, true);
    }
    
    /// <summary>
    /// Добавить пункт поручения.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="deadline">Срок исполнителя.</param>
    /// <param name="actionItemPart">Пункт поручения.</param>
    /// <param name="coAssignees">Соисполнители.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <param name="supervisor">Контролер.</param>
    [Public]
    public void AddActionItemPart(Sungero.Company.IEmployee assignee, DateTime? deadline, string actionItemPart, List<Sungero.Company.IEmployee> coAssignees, DateTime? coAssigneesDeadline, Sungero.Company.IEmployee supervisor)
    {
      var actionItem = _obj.ActionItemParts.AddNew();
      actionItem.ActionItemPart = actionItemPart;
      actionItem.Assignee = assignee;
      actionItem.Deadline = deadline;
      actionItem.CoAssigneesDeadline = coAssigneesDeadline;
      actionItem.Supervisor = supervisor;
      Functions.ActionItemExecutionTask.AddPartsCoAssignees(_obj, actionItem, coAssignees);
    }
    
    /// <summary>
    /// Редактировать пункт поручения.
    /// </summary>
    /// <param name="actionItemPart">Пункт поручения.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="deadline">Срок исполнителя.</param>
    /// <param name="actionItemPartText">Текст поручения.</param>
    /// <param name="coAssignees">Соисполнители.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <param name="supervisor">Контролер.</param>
    [Public]
    public void EditActionItemPart(Sungero.RecordManagement.IActionItemExecutionTaskActionItemParts actionItemPart, Sungero.Company.IEmployee assignee, DateTime? deadline, string actionItemPartText,
                                   List<Sungero.Company.IEmployee> coAssignees, DateTime? coAssigneesDeadline, Sungero.Company.IEmployee supervisor)
    {
      actionItemPart.ActionItemPart = actionItemPartText;
      actionItemPart.Assignee = assignee;
      actionItemPart.Deadline = deadline;
      actionItemPart.CoAssigneesDeadline = coAssigneesDeadline;
      actionItemPart.Supervisor = supervisor;
      Functions.ActionItemExecutionTask.DeletePartsCoAssignees(_obj, actionItemPart);
      Functions.ActionItemExecutionTask.AddPartsCoAssignees(_obj, actionItemPart, coAssignees);
    }

    /// <summary>
    /// Установить срок соисполнителей по умолчанию.
    /// </summary>
    public virtual void SetDefaultCoAssigneesDeadline()
    {
      if (!_obj.CoAssigneesDeadline.HasValue && _obj.Deadline.HasValue && _obj.CoAssignees.Any())
      {
        var settings = Functions.Module.GetSettings();
        _obj.CoAssigneesDeadline = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(_obj.Deadline, -settings.ControlRelativeDeadlineInDays ?? 0, -settings.ControlRelativeDeadlineInHours ?? 0);
      }
    }

    /// <summary>
    /// Синхронизировать первые 1000 символов текста поручения в прикладное поле.
    /// </summary>
    /// <remarks>Нужно для корректного отображения поручения в списках и папках.</remarks>
    public virtual void SynchronizeActiveText()
    {
      var actionItemPropertyMaxLength = _obj.Info.Properties.ActionItem.Length;
      var cutActiveText = _obj.ActiveText != null && _obj.ActiveText.Length > actionItemPropertyMaxLength
        ? _obj.ActiveText.Substring(0, actionItemPropertyMaxLength)
        : _obj.ActiveText;
      
      if (_obj.ActionItem != cutActiveText)
        _obj.ActionItem = cutActiveText;
    }

    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;
      
      _obj.State.Properties.Deadline.IsRequired = (_obj.Info.Properties.Deadline.IsRequired || !isComponentResolution)
        && _obj.HasIndefiniteDeadline != true;
      _obj.State.Properties.Assignee.IsRequired = _obj.Info.Properties.Assignee.IsRequired || !isComponentResolution;
      _obj.State.Properties.CoAssigneesDeadline.IsRequired = _obj.CoAssignees.Any() && !isComponentResolution
        && _obj.HasIndefiniteDeadline != true;
      
      // Проверить заполненность контролера, если поручение на контроле.
      _obj.State.Properties.Supervisor.IsRequired = (_obj.Info.Properties.Supervisor.IsRequired || _obj.IsUnderControl == true) && !isComponentResolution;
    }
    
    /// <summary>
    /// Форматирует резолюцию в формате, необходимом для темы задачи/задания.
    /// </summary>
    /// <param name="actionItem">Резолюция.</param>
    /// <param name="hasDocument">Будет ли документ в теме задачи (т.е. нужно ли обрезать резолюцию).</param>
    /// <returns>Отформатированная резолюция.</returns>
    [Public]
    public static string FormatActionItemForSubject(string actionItem, bool hasDocument)
    {
      if (string.IsNullOrEmpty(actionItem))
        return string.Empty;
      
      // Убрать переносы.
      var formattedActionItem = actionItem.Replace(Environment.NewLine, " ").Replace("\n", " ");
      // Убрать двойные пробелы.
      formattedActionItem = formattedActionItem.Replace("   ", " ").Replace("  ", " ");
      
      // Обрезать размер резолюции для повышения информативности темы.
      if (formattedActionItem.Length > 50 && hasDocument)
        formattedActionItem = actionItem.Substring(0, 50) + Sungero.RecordManagement.ActionItemExecutionTasks.Resources.Ellipsis_FormattedActionItem;
      
      if (hasDocument)
        formattedActionItem = ActionItemExecutionTasks.Resources.SubtaskSubjectPostfixFormat(formattedActionItem);
      
      return formattedActionItem;
    }

    /// <summary>
    /// Получить тему поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="beginningSubject">Изначальная тема.</param>
    /// <returns>Сформированная тема поручения.</returns>
    public static string GetActionItemExecutionSubject(IActionItemExecutionTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      var autoSubject = Docflow.Resources.AutoformatTaskSubject.ToString();
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = beginningSubject.ToString();
        var actionItem = task.ActionItem;
        
        // Добавить резолюцию в тему.
        if (!string.IsNullOrWhiteSpace(actionItem))
        {
          var hasDocument = task.DocumentsGroup.OfficialDocuments.Any();
          var formattedResolution = Functions.ActionItemExecutionTask.FormatActionItemForSubject(actionItem, hasDocument);

          // Конкретно у уведомления о старте составного поручения - всегда рисуем с кавычками.
          if (!hasDocument && subject == ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreatedCompound.ToString())
            formattedResolution = string.Format("\"{0}\"", formattedResolution);

          subject += string.Format(" {0}", formattedResolution);
        }
        
        // Добавить ">> " для тем подзадач.
        var isNotMainTask = task.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Main;
        if (isNotMainTask)
          subject = string.Format(">> {0}", subject);
        
        // Добавить имя документа, если поручение с документом.
        var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        
        subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject != beginningSubject)
          return subject;
      }
      
      return autoSubject;
    }
    
    /// <summary>
    /// Получить ведущее задание на исполнение, в рамках которого было создано поручение.
    /// </summary>
    /// <returns>Ведущее задание на исполнение.</returns>
    [Public]
    public virtual IActionItemExecutionAssignment GetParentAssignment()
    {
      // Составное поручение.
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      if (parentTask != null && parentTask.IsCompoundActionItem == true)
        return ActionItemExecutionAssignments.As(parentTask.ParentAssignment);
      
      // Поручение соисполнителю или подчиненное поручение.
      if (_obj.ActionItemType == ActionItemType.Additional ||
          (_obj.ActionItemType == ActionItemType.Main && _obj.ParentAssignment != null))
        return ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      
      return null;
    }
    
    /// <summary>
    /// Проверить поручение для исполнителей на просроченность.
    /// </summary>
    /// <returns>True, если поручение просрочено.</returns>
    [Public]
    public virtual bool CheckOverdueActionItemExecutionTask()
    {
      if (_obj.IsCompoundActionItem != true)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Assignee, _obj.Deadline, Calendar.Now))
          return true;
      }
      else
      {
        // Проверить корректность срока исполнителя.
        if (_obj.ActionItemParts.Any(j => !Docflow.PublicFunctions.Module.CheckDeadline(j.Assignee, j.Deadline, Calendar.Now)))
          return true;

        // Проверить корректность Общего срока.
        if (_obj.FinalDeadline != null && _obj.ActionItemParts.Any(p => p.Deadline == null) &&
            !Docflow.PublicFunctions.Module.CheckDeadline(_obj.FinalDeadline, Calendar.Now))
          return true;
      }
      
      return false;
    }
    
    /// <summary>
    ///  Проверить корректность срока соисполнителей.
    /// </summary>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <returns>True, если срок соисполнителей больше текущей даты.</returns>
    [Public]
    public virtual bool CheckCoAssigneesDeadline(DateTime? coAssigneesDeadline)
    {
      if (_obj.IsCompoundActionItem != true)
        return _obj.CoAssignees.All(c => Docflow.PublicFunctions.Module.CheckDeadline(c.Assignee, coAssigneesDeadline, Calendar.Now));
      
      return true;
    }
    
    /// <summary>
    ///  Проверить корректность срока соисполнителей.
    /// </summary>
    /// <returns>Список пунктов поручения, срок соисполнителей которых больше текущей даты.</returns>
    [Public]
    public virtual List<IActionItemExecutionTaskActionItemParts> CheckActionItemPartsCoAssigneesDeadline()
    {
      return _obj.ActionItemParts.Where(j => !Docflow.PublicFunctions.Module.CheckCoAssigneesDeadline(this.GetPartCoAssignees(j.PartGuid), j.CoAssigneesDeadline)).ToList();
    }
    
    /// <summary>
    ///  Проверить корректность срока исполнителей.
    /// </summary>
    /// <returns>Список пунктов поручения, срок исполнителей которых больше текущей даты.</returns>
    [Public]
    public virtual List<IActionItemExecutionTaskActionItemParts> CheckActionItemPartsAssigneesDeadline()
    {
      return _obj.ActionItemParts.Where(j => !Docflow.PublicFunctions.Module.CheckDeadline(j.Assignee, j.Deadline, Calendar.Now)).ToList();
    }

    /// <summary>
    /// Валидация старта задачи на исполнение поручения.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <param name="startedFromUI">Признак того, что задача была стартована через UI.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateActionItemExecutionTaskStart(Sungero.Core.IValidationArgs e, bool startedFromUI)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start ValidateActionItemExecutionTaskStart.", _obj.Id);
      
      var authorId = _obj.Author != null ? _obj.Author.Id : -1;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start validate task author, Author (ID={1}).", _obj.Id, authorId);
      var isValid = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
      
      // Проверить, что возможность отправки поручений без срока включена в настройках.
      if (startedFromUI || _obj.ActionItemType == ActionItemType.Main)
        isValid = isValid && this.ValidateActionItemWithoutDeadline(e);
      
      // Проверить корректность заполнения свойства Выдал.
      isValid = isValid && this.ValidateActionItemAssignedBy(e);
      
      // Проверить количество исполнителей по поручению.
      isValid = isValid && this.ValidateActionItemAssigneesCount(e);

      // Только при старте через UI.
      if (startedFromUI)
      {
        // Проверить корректность срока.
        isValid = isValid && this.ValidateActionItemDeadline(e);
        
        // Простое поручение. Срок соисполнителей должен быть больше или равен текущей дате.
        isValid = isValid && this.ValidateActionItemCoAssigneesDeadline(e);
        
        // Сложное поручение. Срок соисполнителей должен быть больше или равен текущей дате.
        isValid = isValid && this.ValidateActionItemPartsCoAssigneesDeadline(e);
      }
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End ValidateActionItemExecutionTaskStart.", _obj.Id);
      
      return isValid;
    }
    
    /// <summary>
    /// Проверить корректность заполнения свойства Выдал.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если свойство Выдал заполнено корректно, иначе - false.</returns>
    public virtual bool ValidateActionItemAssignedBy(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      var assignedById = _obj.AssignedBy != null ? _obj.AssignedBy.Id : -1;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start validate task AssignedBy, AssignedBy (ID={1}).", _obj.Id, assignedById);
      if (!(Sungero.Company.Employees.Current == null && Users.Current.IncludedIn(Roles.Administrators)))
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Validate is AssignedBy can be resolution author, AssignedBy (ID={1}).", _obj.Id, assignedById);
        if (!Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), _obj.AssignedBy))
        {
          e.AddError(_obj.Info.Properties.AssignedBy, ActionItemExecutionTasks.Resources.ActionItemCanNotAssignedByUser);
          isValid = false;
        }
      }
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End validate task AssignedBy, AssignedBy (ID={1}).", _obj.Id, assignedById);
      return isValid;
    }
    
    /// <summary>
    /// Проверить, что возможность отправки поручений без срока включена в настройках.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если можно отправить поручение без срока, иначе - false.</returns>
    public virtual bool ValidateActionItemWithoutDeadline(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      if (_obj.HasIndefiniteDeadline == true && !Functions.Module.AllowActionItemsWithIndefiniteDeadline())
      {
        e.AddError(_obj.Info.Properties.HasIndefiniteDeadline, ActionItemExecutionTasks.Resources.ActionItemWithoutDeadlineDenied);
        isValid = false;
      }
      return isValid;
    }
    
    /// <summary>
    /// Проверить количество исполнителей по поручению.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если количество исполнителей не превышено, иначе - false.</returns>
    public virtual bool ValidateActionItemAssigneesCount(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Validate assignees count.", _obj.Id);
      if (_obj.ActionItemParts.Count() + _obj.CoAssignees.Count() > Constants.ActionItemExecutionTask.MaxActionItemAssignee)
      {
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemAsigneeTooMatchFormat(Constants.ActionItemExecutionTask.MaxActionItemAssignee));
        isValid = false;
      }
      return isValid;
    }
    
    /// <summary>
    /// Проверить корректность срока.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если срок поручения корректный, иначе - false.</returns>
    public virtual bool ValidateActionItemDeadline(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start CheckOverdueActionItemExecutionTask.", _obj.Id);
      if (Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(_obj))
      {
        var notValidAssigneeDeadlineItems = Functions.ActionItemExecutionTask.CheckActionItemPartsAssigneesDeadline(_obj);
        if (_obj.IsCompoundActionItem == true && notValidAssigneeDeadlineItems.Any())
        {
          foreach (var item in notValidAssigneeDeadlineItems)
            e.AddError(item, _obj.Info.Properties.ActionItemParts.Properties.Deadline,
                       Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday,
                       new[] { _obj.Info.Properties.ActionItemParts.Properties.Deadline });
        }
        else
          e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AssigneeDeadlineLessThanToday);
        isValid = false;
      }
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End CheckOverdueActionItemExecutionTask.", _obj.Id);
      return isValid;
    }
    
    /// <summary>
    /// Проверить простое поручение. Срок соисполнителей должен быть больше или равен текущей дате.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если срок соисполнителей корректный, иначе - false.</returns>
    public virtual bool ValidateActionItemCoAssigneesDeadline(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start CheckCoAssigneesDeadline. IsCompoundActionItem = {1}", _obj.Id, _obj.IsCompoundActionItem);
      if (!Functions.ActionItemExecutionTask.CheckCoAssigneesDeadline(_obj, _obj.CoAssigneesDeadline))
      {
        e.AddError(ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday);
        isValid = false;
      }
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End CheckCoAssigneesDeadline.", _obj.Id);
      return isValid;
    }
    
    /// <summary>
    /// Проверить пункты поручения. Срок соисполнителей должен быть больше или равен текущей дате.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True - если срок соисполнителей корректный, иначе - false.</returns>
    public virtual bool ValidateActionItemPartsCoAssigneesDeadline(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start CheckActionItemPartsCoAssigneesDeadline. IsCompoundActionItem = {1}", _obj.Id, _obj.IsCompoundActionItem);
      var notValidCoAssigneeDeadlineItems = Functions.ActionItemExecutionTask.CheckActionItemPartsCoAssigneesDeadline(_obj);
      if (notValidCoAssigneeDeadlineItems.Any())
      {
        foreach (var item in notValidCoAssigneeDeadlineItems)
          e.AddError(item, _obj.Info.Properties.ActionItemParts.Properties.CoAssignees,
                     ActionItemExecutionTasks.Resources.CoAssigneeDeadlineLessThanToday, new[] { _obj.Info.Properties.ActionItemParts.Properties.CoAssignees });
        isValid = false;
      }
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End CheckActionItemPartsCoAssigneesDeadline.", _obj.Id);
      return isValid;
    }
    
    /// <summary>
    /// Валидация сохранения задачи на исполнение поручения.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateActionItemExecutionTaskSave(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      
      // Проверить заполненность Общего срока (а также корректность), исполнителей, текста поручения у составного поручения.
      var isCompoundActionItem = _obj.IsCompoundActionItem ?? false;
      if (isCompoundActionItem)
      {
        if (_obj.ActionItemParts.Count == 0)
        {
          e.AddError(_obj.Info.Properties.ActionItemParts, ActionItemExecutionTasks.Resources.ActionItemsNotFilled);
          isValid = false;
        }
        
        if (_obj.Supervisor == null && _obj.ActionItemParts.Any(i => i.Supervisor == null) && _obj.IsUnderControl == true)
        {
          e.AddError(_obj.Info.Properties.Supervisor, Sungero.RecordManagement.ActionItemExecutionTasks.Resources.EmptySupervisor);
          isValid = false;
        }
        
        if (_obj.FinalDeadline == null && _obj.HasIndefiniteDeadline != true && _obj.ActionItemParts.Any(i => i.Deadline == null))
        {
          e.AddError(_obj.Info.Properties.FinalDeadline, ActionItemExecutionTasks.Resources.EmptyFinalDeadline);
          isValid = false;
        }
        
        if (string.IsNullOrWhiteSpace(_obj.ActiveText) && _obj.ActionItemParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
        {
          e.AddError(ActionItemExecutionTasks.Resources.EmptyActionItem);
          isValid = false;
        }
        
        // Проверить сроки соисполнителей.
        var parts = _obj.ActionItemParts.Where(itemPart => itemPart.Deadline == null && itemPart.CoAssigneesDeadline != null &&
                                               !Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(_obj.FinalDeadline, itemPart.CoAssigneesDeadline)).ToList();
        if (parts.Any())
          foreach (var part in parts)
            e.AddError(part, _obj.Info.Properties.ActionItemParts.Properties.CoAssignees,
                       RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError, new[] { _obj.Info.Properties.ActionItemParts.Properties.CoAssignees });
        
        var notValidCoAssigneeDeadlineItems = _obj.ActionItemParts.Where(item => _obj.PartsCoAssignees.Any(i => i.PartGuid == item.PartGuid) && item.CoAssigneesDeadline == null);
        if (_obj.HasIndefiniteDeadline != true && notValidCoAssigneeDeadlineItems.Any())
        {
          foreach (var item in notValidCoAssigneeDeadlineItems)
            e.AddError(item, _obj.Info.Properties.ActionItemParts.Properties.CoAssignees,
                       Sungero.RecordManagement.ActionItemExecutionTasks.Resources.EmptyCoAssigneeDeadline, new[] { _obj.Info.Properties.ActionItemParts.Properties.CoAssignees });
          isValid = false;
        }
      }
      else if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        // Проверить заполненность текста простого поручения.
        e.AddError(ActionItemExecutionTasks.Resources.EmptyActiveText);
        isValid = false;
      }
      else
      {
        if (_obj.Status == Sungero.RecordManagement.ActionItemExecutionTask.Status.Draft)
        {
          if (!Docflow.PublicFunctions.Module.CheckAssigneesDeadlines(_obj.Deadline, _obj.CoAssigneesDeadline))
          {
            e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CoAssigneesDeadlineError);
            isValid = false;
          }
        }
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Проверить, завершена ли задача на исполнение поручения.
    /// </summary>
    /// <returns>True, если задача на исполнение поручения завершена, иначе - False.</returns>
    public virtual bool IsActionItemExecutionTaskCompleted()
    {
      return Docflow.PublicFunctions.Module.IsTaskCompleted(_obj);
    }
    
    /// <summary>
    /// Получить группу регистрации документа на исполнение.
    /// </summary>
    /// <returns>Группа регистрации документа на исполнение.</returns>
    public virtual Sungero.Docflow.IRegistrationGroup GetExecutingDocumentRegistrationGroup()
    {
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return null;
      
      return Docflow.PublicFunctions.OfficialDocument.GetRegistrationGroup(document);
    }
    
    /// <summary>
    /// Получить соисполнителей по пункту поручения.
    /// </summary>
    /// <param name="partGuid">Идентификатор пункта поручения.</param>
    /// <returns>Список соисполнителей.</returns>
    public virtual List<Company.IEmployee> GetPartCoAssignees(string partGuid)
    {
      return _obj.PartsCoAssignees.Where(p => p.PartGuid == partGuid).Select(p => p.CoAssignee).ToList();
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    public virtual void SynchronizeAddendaAndAttachmentsGroup()
    {
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
      {
        _obj.AddendaGroup.All.Clear();
        _obj.AddedAddenda.Clear();
        _obj.RemovedAddenda.Clear();
        return;
      }

      // Документы, связанные связью Приложение с основным документом.
      var documentAddenda = Docflow.PublicFunctions.Module.GetAddenda(document);
      // Документы в группе Приложения.
      var taskAddenda = Functions.ActionItemExecutionTask.GetAddendaGroupAttachments(_obj);
      // Документы в коллекции добавленных вручную документов.
      var taskAddedAddenda = this.GetAddedAddenda();
      
      // Удалить из гр. Приложения документы, которые не связаны связью "Приложение" и не добавлены вручную.
      var addendaToRemove = taskAddenda.Except(documentAddenda).Where(x => !taskAddedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToRemove)
      {
        _obj.AddendaGroup.All.Remove(addendum);
        this.RemovedAddendaRemove(addendum);
      }
      
      // Добавить документы, связанные связью типа Приложение с основным документом.
      var taskRemovedAddenda = this.GetRemovedAddenda();
      var addendaToAdd = documentAddenda.Except(taskAddenda).Where(x => !taskRemovedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToAdd)
      {
        _obj.AddendaGroup.All.Add(addendum);
        this.AddedAddendaRemove(addendum);
      }
    }
    
    /// <summary>
    /// Получить вложения группы "Приложения".
    /// </summary>
    /// <returns>Вложения группы "Приложения".</returns>
    public virtual List<IElectronicDocument> GetAddendaGroupAttachments()
    {
      return _obj.AddendaGroup.OfficialDocuments
        .Select(x => ElectronicDocuments.As(x))
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, добавленных в группу "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    public virtual List<int> GetAddedAddenda()
    {
      return _obj.AddedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, удаленных из группы "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    public virtual List<int> GetRemovedAddenda()
    {
      return _obj.RemovedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void AddedAddendaAppend()
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Append to AddedAddenda from assignments.", _obj.Id);
      var addedAttachments = Docflow.PublicFunctions.Module.GetAddedAddendaFromAssignments(_obj, Constants.ActionItemExecutionTask.AddendaGroupGuid);
      foreach (var attachment in addedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.AddedAddendaAppend(attachment);
        this.RemovedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void RemovedAddendaAppend()
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Append to RemovedAddenda from assignments.", _obj.Id);
      var removedAttachments = Docflow.PublicFunctions.Module.GetRemovedAddendaFromAssignments(_obj, Constants.ActionItemExecutionTask.AddendaGroupGuid);
      foreach (var attachment in removedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.RemovedAddendaAppend(attachment);
        this.AddedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, добавленный в группу "Приложения".</param>
    public virtual void AddedAddendaAppend(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var addedAddendaItem = _obj.AddedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (addedAddendaItem == null)
      {
        _obj.AddedAddenda.AddNew().AddendumId = addendum.Id;
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Append to AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Из коллекции добавленных вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void AddedAddendaRemove(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var addedAddendaItem = _obj.AddedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (addedAddendaItem != null)
      {
        _obj.AddedAddenda.Remove(addedAddendaItem);
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Remove from AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Из коллекции удаленных вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void RemovedAddendaRemove(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      var removedAddendaItem = _obj.RemovedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (removedAddendaItem != null)
      {
        _obj.RemovedAddenda.Remove(removedAddendaItem);
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Remove from RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, удаленный вручную из группы "Приложения".</param>
    public virtual void RemovedAddendaAppend(IElectronicDocument addendum)
    {
      if (addendum == null)
        return;
      
      if (_obj.RemovedAddenda.Any(x => x.AddendumId == addendum.Id))
        return;
      
      _obj.RemovedAddenda.AddNew().AddendumId = addendum.Id;
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Append to RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
    }
    
    #endregion
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Server;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskActionItemObserversObserverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ActionItemObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ActionItemExecutionTaskCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ExecutionState);
      e.Without(_info.Properties.Report);
      e.Without(_info.Properties.ActualDate);
      e.Without(_info.Properties.ReportNote);
      e.Without(_info.Properties.AbortingReason);
      e.Without(_info.Properties.ActionItemType);
      e.Without(_info.Properties.OnEdit);
      e.Without(_info.Properties.OnEditGuid);
      e.Without(_info.Properties.Started);
      var hasIndefiniteDeadline = _source.HasIndefiniteDeadline == true && Functions.Module.AllowActionItemsWithIndefiniteDeadline();
      e.Map(_info.Properties.HasIndefiniteDeadline, hasIndefiniteDeadline);
      
      if (hasIndefiniteDeadline)
        e.Without(_info.Properties.CoAssigneesDeadline);
    }
  }

  partial class ActionItemExecutionTaskAssignedByPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> AssignedByFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var resolutionAuthorsIds = Docflow.PublicFunctions.Module.Remote.UsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault()).Select(x => x.Id).ToList();
      return query.Where(x => resolutionAuthorsIds.Contains(x.Id));
    }
  }

  partial class ActionItemExecutionTaskFilteringServerHandler<T>
  {
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Вернуть нефильтрованный список, если нет фильтра. Он будет использоваться во всех Get() и GetAll().
      var filter = _filter;
      if (filter == null)
        return query;
      
      e.DisableCheckRights = true;
      
      // Не показывать не стартованные поручения.
      query = query.Where(l => l.Status != Sungero.Workflow.Task.Status.Draft);
      
      // Не показывать составные поручения (только подзадачи).
      query = query.Where(j => j.IsCompoundActionItem == false);
      
      // Фильтр по статусу.
      var statuses = new List<Enumeration>();
      if (filter.OnExecution)
      {
        statuses.Add(ExecutionState.OnExecution);
        statuses.Add(ExecutionState.OnControl);
        statuses.Add(ExecutionState.OnRework);
      }
      
      if (filter.Executed)
      {
        statuses.Add(ExecutionState.Executed);
        statuses.Add(ExecutionState.Aborted);
      }
      
      if (statuses.Any())
        query = query.Where(q => q.ExecutionState != null && statuses.Contains(q.ExecutionState.Value));
      
      // Фильтры "Поручения где я", "По сотруднику".
      var currentUser = Users.Current;
      
      // Сформировать списки пользователей для фильтрации.
      var authors = new List<IUser>();
      var assignees = new List<IUser>();
      var supervisors = new List<IUser>();
      
      if (filter.Author != null)
        authors.Add(filter.Author);
      if (filter.Assignee != null)
        assignees.Add(filter.Assignee);
      if (filter.Supervisor != null)
        supervisors.Add(filter.Supervisor);
      
      // Наложить фильтр по всем замещениям, если не указаны фильтры по текущему или выбранному сотруднику.
      if (!Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        var allSubstitutes = Substitutions.ActiveSubstitutedUsers.ToList();
        allSubstitutes.Add(Users.Current);
        query = query.Where(j => allSubstitutes.Contains(j.AssignedBy) || allSubstitutes.Contains(j.Assignee) ||
                            j.CoAssignees.Any(p => allSubstitutes.Contains(p.Assignee)) ||
                            allSubstitutes.Contains(j.Supervisor) || allSubstitutes.Contains(j.StartedBy) ||
                            j.ActionItemObservers.Any(o => Recipients.AllRecipientIds.Contains(o.Observer.Id)));
      }
      
      query = query.Where(j => (!authors.Any() || authors.Contains(j.AssignedBy)) &&
                          (!assignees.Any() || assignees.Contains(j.Assignee) || j.CoAssignees.Any(p => assignees.Contains(p.Assignee))) &&
                          (!supervisors.Any() || supervisors.Contains(j.Supervisor)));
      
      // Фильтр по соблюдению сроков.
      var now = Calendar.Now;
      var today = Calendar.UserToday;
      var tomorrow = today.AddDays(1);
      if (filter.Overdue)
        query = query.Where(j => j.Status != Workflow.Task.Status.Aborted && j.HasIndefiniteDeadline != true &&
                            ((j.ActualDate == null && j.Deadline < now && j.Deadline != today && j.Deadline != tomorrow) ||
                             (j.ActualDate != null && j.ActualDate > j.Deadline)));
      // Фильтр по плановому сроку.
      if (filter.LastMonth)
      {
        var lastMonthBeginDate = today.AddDays(-30);
        var lastMonthBeginDateNextDay = lastMonthBeginDate.AddDays(1);
        var lastMonthBeginDateWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(lastMonthBeginDate);

        query = query.Where(j => ((j.Deadline >= lastMonthBeginDateWithTime && j.Deadline < now) ||
                                  j.Deadline == lastMonthBeginDate || j.Deadline == lastMonthBeginDateNextDay || j.Deadline == today) &&
                            j.Deadline != tomorrow);
      }

      if (filter.ManualPeriod)
      {
        if (filter.DateRangeFrom != null)
        {
          var dateRangeFromNextDay = filter.DateRangeFrom.Value.AddDays(1);
          var dateFromWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.DateRangeFrom.Value);
          query = query.Where(j => j.HasIndefiniteDeadline == true ||
                              j.Deadline >= dateFromWithTime ||
                              j.Deadline == filter.DateRangeFrom.Value ||
                              j.Deadline == dateRangeFromNextDay);
        }
        if (filter.DateRangeTo != null)
        {
          var dateRangeNextDay = filter.DateRangeTo.Value.AddDays(1);
          var dateTo = filter.DateRangeTo.Value.EndOfDay().FromUserTime();
          query = query.Where(j => j.HasIndefiniteDeadline != true &&
                              ((j.Deadline < dateTo || j.Deadline == filter.DateRangeTo.Value) &&
                               j.Deadline != dateRangeNextDay));
        }
      }
      
      return query;
    }
  }

  partial class ActionItemExecutionTaskServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      // Проверить, что было изменение сроков, которое необходимо отразить в истории.
      var taskParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (!taskParams.ContainsKey(PublicConstants.ActionItemExecutionTask.ChangeDeadlinesWriteInHistoryParamName))
        return;
      
      // Сформировать комментарий для записи в историю поручения.
      var changeDeadlineComment = Functions.ActionItemExecutionTask.GetActionItemChangeDeadlineHistoryText(_obj, taskParams);
      
      // Если изменились только сроки, сформировать одну строку записи в историю (подменить строку записи по умолчанию),
      // иначе информацию об изменении сроков записать отдельной строкой.
      var changeDeadlineOperationText = Constants.ActionItemExecutionTask.Operation.ChangeDeadline;
      if (taskParams.ContainsKey(PublicConstants.ActionItemExecutionTask.ChangeOnlyDeadlinesWriteInHistoryParamName))
      {
        e.Operation = new Enumeration(changeDeadlineOperationText);
        e.Comment = changeDeadlineComment;
      }
      else
      {
        e.Write(new Enumeration(changeDeadlineOperationText), null, changeDeadlineComment);
      }
      
      // Очистить параметры, сигнализирующие об изменении сроков,
      // чтобы не было ложных срабатываний при последующих сохранениях записей в историю.
      taskParams.Remove(PublicConstants.ActionItemExecutionTask.ChangeDeadlinesWriteInHistoryParamName);
      taskParams.Remove(PublicConstants.ActionItemExecutionTask.ChangeOnlyDeadlinesWriteInHistoryParamName);
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      var taskIsCompleted = Functions.ActionItemExecutionTask.IsActionItemExecutionTaskCompleted(_obj);
      if (taskIsCompleted && _obj.ActionItemType == ActionItemType.Component)
        Functions.ActionItemExecutionTask.ExecuteParentActionItemExecutionTaskMonitorings(_obj);
    }
    
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      _obj.ExecutionState = ExecutionState.Aborted;
      
      // Если прекращён черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      // Обновить статус исполнения документа - исполнен, статус контроля исполнения - снято с контроля.
      if (_obj.DocumentsGroup.OfficialDocuments.Any())
        Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
      
      // Прекратить задачи на запрос отчета, созданные из текущей задачи.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTask(_obj);
      
      // Прекратить задачи на запрос отчета, созданные из родительского задания.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromAssignmentToAssignee(_obj,
                                                                                               ActionItemExecutionAssignments.As(_obj.ParentAssignment),
                                                                                               _obj.Assignee);
      
      // Прекратить задачи на запрос отчета, созданные из составного поручения исполнителю пункта.
      if (ActionItemExecutionTasks.As(_obj.ParentTask)?.IsCompoundActionItem == true)
      {
        Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTaskToAssignee(_obj,
                                                                                           ActionItemExecutionTasks.As(_obj.ParentTask),
                                                                                           _obj.Assignee);
      }
      
      // При программном вызове не выполнять рекурсивную остановку подзадач.
      if (!e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI))
        return;
      
      // Рекурсивно прекратить подзадачи.
      Functions.Module.AbortSubtasksAndSendNotices(_obj);
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      // Очистить причину прекращения и статус.
      _obj.AbortingReason = string.Empty;
      _obj.ExecutionState = null;
      _obj.OnEditGuid = string.Empty;
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      // Очистить свойство созданных заданий у свойств-коллекций.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        foreach (var assignee in _obj.CoAssignees)
        {
          assignee.AssignmentCreated = false;
        }
      }
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        foreach (var part in _obj.ActionItemParts)
        {
          part.AssignmentCreated = false;
        }
      }
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start BeforeStart.", _obj.Id);
      
      // Если задача была стартована через UI, то проверяем корректность срока.
      bool startedFromUI;
      if (e.Params.TryGetValue(PublicConstants.ActionItemExecutionTask.CheckDeadline, out startedFromUI) && startedFromUI)
        e.Params.Remove(PublicConstants.ActionItemExecutionTask.CheckDeadline);
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskStart(_obj, e, startedFromUI))
        return;

      // Задать текст в переписке.
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set ActiveText.", _obj.Id);
      if (_obj.IsCompoundActionItem == true)
      {
        _obj.ActiveText = string.IsNullOrWhiteSpace(_obj.ActiveText) ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.DefaultActionItem : _obj.ActiveText;
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      }
      else if (_obj.ActionItemType != ActionItemType.Component)
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemThreadSubject;

      if (_obj.ActionItemType == ActionItemType.Component)
      {
        // Синхронизировать текст пункта составного поручения в прикладное поле.
        Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);
        
        // При рестарте поручения обновляется текст, срок и исполнитель в табличной части составного поручения.
        Functions.ActionItemExecutionTask.SynchronizeActionItemPart(_obj, false);
      }
      
      if (_obj.ActionItemType == ActionItemType.Additional)
        _obj.ActiveText = ActionItemExecutionTasks.Resources.SentToCoAssignee;
      
      // Выдать права на изменение для возможности прекращения задачи.
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Grant access right to task.", _obj.Id);
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Synchronize primary document if task is draft resolution.", _obj.Id);
      if (_obj.IsDraftResolution == true && !_obj.DocumentsGroup.OfficialDocuments.Any())
        if (ReviewDraftResolutionAssignments.Is(_obj.ParentAssignment))
          _obj.DocumentsGroup.OfficialDocuments.Add(ReviewDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
        else
          _obj.DocumentsGroup.OfficialDocuments.Add(PreparingDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End BeforeStart", _obj.Id);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start Created.", _obj.Id);
      
      _obj.ActionItemType = ActionItemType.Main;
      
      _obj.OnEdit = false;
      _obj.OnEditGuid = string.Empty;
      
      if (!_obj.State.IsCopied)
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Not Copied.", _obj.Id);
        
        _obj.NeedsReview = false;
        _obj.IsUnderControl = false;
        _obj.IsCompoundActionItem = false;
        _obj.HasIndefiniteDeadline = false;
        
        // Заполнение из персональных настроек происходит в методе создания подчиненного поручения.
        _obj.IsAutoExec = false;
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        var employee = Employees.As(_obj.Author);
        if (employee != null)
        {
          Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Author is employee (ID={1}).", _obj.Id, employee.Id);
          var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(employee);
          if (settings != null)
          {
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Has PersonalSettings (ID={1}).", _obj.Id, settings.Id);
            _obj.IsUnderControl = settings.FollowUpActionItem;
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). GetResolutionAuthor.", _obj.Id);
            var resolutionAuthor = Docflow.PublicFunctions.PersonalSetting.GetResolutionAuthor(settings);
            Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set AssignedBy.", _obj.Id);
            _obj.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), resolutionAuthor)
              ? resolutionAuthor
              : null;
          }
        }
      }
      else
      {
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Copied.", _obj.Id);
        
        if (_obj.Author != null && _obj.AssignedBy != null && !_obj.Author.Equals(_obj.AssignedBy))
          _obj.Author = Users.As(_obj.AssignedBy);
        
        // Сброс отметок о создании заданий соисполнителям.
        if (_obj.CoAssignees.Count > 0)
          foreach (var assignee in _obj.CoAssignees)
            assignee.AssignmentCreated = false;
        
        // Сброс отметок о создании заданий по частям составного поручения.
        if (_obj.IsCompoundActionItem == true)
          foreach (var part in _obj.ActionItemParts)
        {
          part.AssignmentCreated = false;
          part.ActionItemPartExecutionTask = null;
        }
        
        // Сброс индивидуального контролера в пунктах составного поручения.
        if (!_obj.IsUnderControl.Value)
          foreach (var part in _obj.ActionItemParts)
            part.Supervisor = null;
        
        // Сброс сроков исполнителя и соисполнителей в пунктах поручения.
        if (_obj.HasIndefiniteDeadline.Value)
          foreach (var part in _obj.ActionItemParts)
        {
          part.Deadline = null;
          part.CoAssigneesDeadline = null;
        }
        
        // Сброс результатов исполнения.
        if (_obj.ResultGroup.All.Any())
          _obj.ResultGroup.All.Clear();
      }
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set Subject.", _obj.Id);
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End Created.", _obj.Id);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start BeforeSave.", _obj.Id);
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskSave(_obj, e))
        return;

      var isCompoundActionItem = _obj.IsCompoundActionItem == true;
      if (isCompoundActionItem)
      {
        if (string.IsNullOrWhiteSpace(_obj.ActiveText) && !_obj.ActionItemParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
          _obj.ActiveText = ActionItemExecutionTasks.Resources.DefaultActionItem;
      }
      
      // Синхронизировать текст поручения в прикладное поле.
      if (_obj.ActionItemType != ActionItemType.Additional)
        Functions.ActionItemExecutionTask.SynchronizeActiveText(_obj);

      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
      {
        // Выдать права по каждой группе в отдельности, так как AllAttachments включает в себя удаленные до сохранения документы. Bug 181206.
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Start GrantManualReadRightForAttachments.", _obj.Id);
        var allAttachments = _obj.DocumentsGroup.All.ToList();
        allAttachments.AddRange(_obj.AddendaGroup.All);
        allAttachments.AddRange(_obj.OtherGroup.All);
        allAttachments.AddRange(_obj.ResultGroup.All);
        
        Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, allAttachments);
        Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End GrantManualReadRightForAttachments.", _obj.Id);
      }
      
      if (_obj.State.Properties.IsCompoundActionItem.IsChanged)
      {
        if (isCompoundActionItem)
        {
          // Очистить ненужные свойства в составном поручении.
          _obj.Assignee = null;
          _obj.CoAssignees.Clear();
          _obj.Deadline = null;
          
          // Заменить первый символ на прописной.
          foreach (var job in _obj.ActionItemParts)
            job.ActionItemPart = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(job.ActionItemPart);
        }
        else
        {
          // Очистить ненужные свойства в несоставном поручении.
          _obj.ActionItemParts.Clear();
        }
      }
      
      // Заполнить тему.
      var defaultSubject = isCompoundActionItem ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, defaultSubject);
      if (subject == Docflow.Resources.AutoformatTaskSubject)
        subject = defaultSubject;
      
      // Не перезаписывать тему, если не изменилась.
      if (subject != _obj.Subject)
        _obj.Subject = subject;
      
      // Задать текст в переписке.
      var threadSubject = string.Empty;
      if (isCompoundActionItem)
        threadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      else if (_obj.ActionItemType != ActionItemType.Component)
        threadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemThreadSubject;
      
      // Не перезаписывать текст в переписке без необходимости, чтобы избежать блокировок.
      if (!string.IsNullOrEmpty(threadSubject) && threadSubject != _obj.ThreadSubject)
        _obj.ThreadSubject = threadSubject;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End BeforeSave.", _obj.Id);
    }
  }
}
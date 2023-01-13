using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.Structures.ActionItemExecutionTask;
using Sungero.Security;
using Sungero.Workflow;
using Sungero.Workflow.Task;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionTaskFunctions
  {
    #region Предметное отображение
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <returns>Контрол состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      // Определить главное поручение и построить его состояние.
      var mainActionItemExecutionTask = this.GetMainActionItemExecutionTask();

      var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
      stateViewModel.Tasks = new List<IActionItemExecutionTask>() { mainActionItemExecutionTask };
      stateViewModel = GetAllActionItems(stateViewModel);
      return Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(mainActionItemExecutionTask, _obj, stateViewModel, null, false, true);
    }

    /// <summary>
    /// Найти самое верхнее поручение.
    /// </summary>
    /// <returns>Самое верхнее поручение.</returns>
    public IActionItemExecutionTask GetMainActionItemExecutionTask()
    {
      var mainActionItemExecutionTask = _obj;
      ITask currentTask = _obj;
      while (currentTask.ParentTask != null || currentTask.ParentAssignment != null)
      {
        currentTask = currentTask.ParentTask ?? currentTask.ParentAssignment.Task;
        if (ActionItemExecutionTasks.Is(currentTask))
          mainActionItemExecutionTask = ActionItemExecutionTasks.As(currentTask);
      }
      return mainActionItemExecutionTask;
    }
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрол состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      // Проекты резолюций будут добавлены вместе с задачей на рассмотрение.
      if (_obj.IsDraftResolution == true)
        return StateView.Create();
      
      if (!_obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return StateView.Create();
      
      return Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(_obj, null, null, null, false, true);
    }
    
    /// <summary>
    /// Построить модель состояния поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Новое подпоручение.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    /// <param name="draftAssignee">Исполнитель в черновике.</param>
    /// <param name="draftActionItem">Поручение в черновике.</param>
    /// <param name="draftDeadline">Срок в черновике.</param>
    /// <param name="draftNumber">Номер поручения в черновике составного.</param>
    /// <param name="skipResolutionBlock">Пропустить блок резолюции.</param>
    /// <param name="withHighlight">Выделять цветом основной блок.</param>
    /// <returns>Модель состояния.</returns>
    [Public, Obsolete("Используйте метод Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView")]
    public static Sungero.Core.StateView GetActionItemStateView(IActionItemExecutionTask task,
                                                                IActionItemExecutionTask openedTask,
                                                                Structures.ActionItemExecutionTask.IStateViewModel stateViewModel,
                                                                IEmployee draftAssignee = null,
                                                                string draftActionItem = "",
                                                                DateTime? draftDeadline = null,
                                                                int? draftNumber = null,
                                                                bool skipResolutionBlock = false,
                                                                bool withHighlight = true)
    {
      var stateView = StateView.Create();
      
      if (task == null && openedTask == null)
        return stateView;
      
      if (task == null)
        return Functions.ActionItemExecutionTask.GetDraftActionItemExecutionTaskStateView(openedTask,
                                                                                          stateViewModel,
                                                                                          draftAssignee,
                                                                                          openedTask.Assignee,
                                                                                          draftActionItem,
                                                                                          draftDeadline,
                                                                                          draftNumber,
                                                                                          !openedTask.IsCompoundActionItem.Value,
                                                                                          skipResolutionBlock,
                                                                                          withHighlight);
      return Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(task,
                                                                                   openedTask,
                                                                                   stateViewModel,
                                                                                   draftNumber,
                                                                                   skipResolutionBlock,
                                                                                   withHighlight);
    }
    
    /// <summary>
    /// Построить модель состояния черновика поручения.
    /// </summary>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    /// <param name="draftAssignee">Исполнитель в черновике.</param>
    /// <param name="draftSupervisor">Контролер в черновике.</param>
    /// <param name="draftActionItem">Текст поручения в черновике.</param>
    /// <param name="draftDeadline">Срок в черновике.</param>
    /// <param name="draftNumber">Номер поручения в черновике составного.</param>
    /// <param name="additional">Поручение соисполнителя.</param>
    /// <param name="skipResolutionBlock">Пропустить блок резолюции.</param>
    /// <param name="withHighlight">Выделять цветом основной блок.</param>
    /// <returns>Модель состояния.</returns>
    [Public]
    public virtual Sungero.Core.StateView GetDraftActionItemExecutionTaskStateView(Structures.ActionItemExecutionTask.IStateViewModel stateViewModel,
                                                                                   IEmployee draftAssignee = null,
                                                                                   IEmployee draftSupervisor = null,
                                                                                   string draftActionItem = "",
                                                                                   DateTime? draftDeadline = null,
                                                                                   int? draftNumber = null,
                                                                                   bool additional = false,
                                                                                   bool skipResolutionBlock = false,
                                                                                   bool withHighlight = true)
    {
      var stateView = StateView.Create();
      
      if (stateViewModel == null)
        stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();

      if (stateViewModel.Tasks == null || stateViewModel.Tasks.Count == 0)
      {
        stateViewModel.Tasks = new List<IActionItemExecutionTask>();
        stateViewModel = GetAllActionItems(stateViewModel);
      }
      
      // Стили.
      var isDraft = true;
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);

      if (stateViewModel.StatusesCache == null)
        stateViewModel.StatusesCache = new Dictionary<Enumeration?, string>();
      
      var isCompoundDraftActionItem = _obj.IsCompoundActionItem == true;
      var component = isCompoundDraftActionItem;
      var underControl = !isCompoundDraftActionItem || _obj.IsUnderControl == true;
      
      var taskBlock = stateView.AddBlock();
      
      // Для поручения соисполнителю сменить иконку.
      if (additional)
        taskBlock.AssignIcon(ActionItemExecutionAssignments.Info.Actions.CreateChildActionItem, StateBlockIconSize.Large);
      else if (taskBlock.Entity != null)
        taskBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      else
        taskBlock.AssignIcon(Docflow.ApprovalRuleBases.Resources.ActionItemTask, StateBlockIconSize.Large);

      // Статус.
      var status = GetStatusInfo(null, stateViewModel.StatusesCache);
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
      
      // Заголовок.
      var hasCoAssignees = false;
      var isCompound = false;
      var header = GetHeader(null, additional, component, hasCoAssignees, isCompound, _obj, draftNumber);
      taskBlock.AddLabel(header, headerStyle);
      taskBlock.AddLineBreak();

      // Кому.
      this.AddPerformerLabel(taskBlock, draftAssignee, performerDeadlineStyle);
      
      // Срок.
      this.AddDeadlineLabel(taskBlock, draftDeadline, performerDeadlineStyle);
      
      // Контролёр.
      this.AddSupervisorLabel(taskBlock, draftSupervisor, performerDeadlineStyle, underControl);
      
      // Разделитель.
      this.AddSeparatorLabel(taskBlock);
      taskBlock.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
      
      // Текст поручения.
      this.AddActionItemTextLabel(taskBlock, draftActionItem, labelStyle);
      
      // Раскрыть поручение, если это черновик.
      taskBlock.IsExpanded = true;
      
      // Если есть развернутые подчиненные поручения, то развернуть и это.
      if (taskBlock.ChildBlocks.Where(b => b.IsExpanded == true).Any())
        taskBlock.IsExpanded = true;

      return stateView;
    }
    
    /// <summary>
    /// Построить модель состояния поручения.
    /// </summary>
    /// <param name="openedTask">Новое подпоручение.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    /// <param name="draftNumber">Номер поручения в черновике составного.</param>
    /// <param name="skipResolutionBlock">Пропустить блок резолюции.</param>
    /// <param name="withHighlight">Выделять цветом основной блок.</param>
    /// <returns>Модель состояния.</returns>
    [Public]
    public virtual Sungero.Core.StateView GetActionItemExecutionTaskStateView(IActionItemExecutionTask openedTask,
                                                                              Structures.ActionItemExecutionTask.IStateViewModel stateViewModel,
                                                                              int? draftNumber = null,
                                                                              bool skipResolutionBlock = false,
                                                                              bool withHighlight = true)
    {
      var stateView = StateView.Create();
      
      if (stateViewModel == null)
        stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();

      if (stateViewModel.Tasks == null || stateViewModel.Tasks.Count == 0)
      {
        stateViewModel.Tasks = new List<IActionItemExecutionTask>() { _obj };
        stateViewModel = GetAllActionItems(stateViewModel);
      }
      
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      
      // Стили.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var grayStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, true);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);

      if (stateViewModel.StatusesCache == null)
        stateViewModel.StatusesCache = new Dictionary<Enumeration?, string>();

      // Добавить блок по резолюции, если поручение в рамках рассмотрения.
      // Блок добавить только для самого верхнего поручения.
      if (_obj.MainTask != null && !skipResolutionBlock &&
          Equals(_obj, this.GetMainActionItemExecutionTask()))
      {
        StateView reviewState;
        if (DocumentReviewTasks.Is(_obj.MainTask))
        {
          if (_obj.ParentAssignment != null && DocumentReviewTasks.Is(_obj.ParentAssignment.Task))
            // Добавить блок информации о резолюции без добавления дополнительного блока поручения.
            reviewState = Functions.DocumentReviewTask.GetDocumentReviewStateView(DocumentReviewTasks.As(_obj.ParentAssignment.Task), false);
          else
            reviewState = Functions.DocumentReviewTask.GetStateView(DocumentReviewTasks.As(_obj.MainTask));
          
          foreach (var block in reviewState.Blocks)
            stateView.AddBlock(block);
        }
        else if (Docflow.ApprovalTasks.Is(_obj.MainTask))
        {
          this.AddReviewBlock(stateView, stateViewModel.Tasks, stateViewModel.Assignments);
        }
      }
      
      var main = _obj.ActionItemType == ActionItemType.Main;
      var additional = _obj.ActionItemType == ActionItemType.Additional;
      var component = _obj.ActionItemType == ActionItemType.Component;
      var underControl = _obj.IsUnderControl == true;
      var hasCoAssignees = _obj.CoAssignees.Any();
      var isCompound = _obj.IsCompoundActionItem == true;

      // Не выводить задачу, если она была стартована до последнего рестарта главной, если это не черновик.
      if (!isDraft)
      {
        var parentTask = Tasks.Null;
        if (_obj.ActionItemType == ActionItemType.Component)
          parentTask = _obj.ParentTask;
        else if (_obj.ActionItemType == ActionItemType.Additional)
          parentTask = _obj.ParentAssignment.Task;
        
        if (parentTask != null && parentTask.Started.HasValue && _obj.Started.HasValue && parentTask.Started > _obj.Started)
          return StateView.Create();
      }

      // Добавить заголовок с информацией по отправителю поручения.
      if (main)
      {
        var text = ActionItemExecutionTasks.Resources.StateViewActionItemOnExecution;
        if (_obj.ParentAssignment != null && ActionItemExecutionAssignments.Is(_obj.ParentAssignment))
          text = ActionItemExecutionTasks.Resources.StateViewSubordinateActionItemSent;
        var comment = Docflow.PublicFunctions.Module.GetFormatedUserText(text);
        
        if (_obj.Started.HasValue)
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, _obj.Author, comment, _obj.Started.Value, _obj, string.Empty, _obj.StartedBy);
        else
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, _obj.Author, Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated, _obj.Created.Value, _obj, string.Empty, _obj.Author);
      }
      
      var taskBlock = stateView.AddBlock();
      
      if (!_obj.State.IsInserted)
        taskBlock.Entity = _obj;
      
      if (Equals(_obj, openedTask) && withHighlight)
        Docflow.PublicFunctions.Module.MarkBlock(taskBlock);
      
      // Для поручения соисполнителю сменить иконку.
      if (additional)
        taskBlock.AssignIcon(ActionItemExecutionAssignments.Info.Actions.CreateChildActionItem, StateBlockIconSize.Large);
      else if (isCompound)
        taskBlock.AssignIcon(ActionItemExecutionTasks.Resources.CompoundActionItem, StateBlockIconSize.Large);
      else if (taskBlock.Entity != null)
        taskBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      else
        taskBlock.AssignIcon(Docflow.ApprovalRuleBases.Resources.ActionItemTask, StateBlockIconSize.Large);

      // Статус.
      var status = GetStatusInfo(_obj, stateViewModel.StatusesCache);
      
      // Для непрочитанных заданий указать это.
      if (_obj.Status == Workflow.Task.Status.InProcess)
      {
        var actionItemExecution = stateViewModel.Assignments
          .Where(a => Equals(a.Task.Id, _obj.Id) && ActionItemExecutionAssignments.Is(a))
          .OrderByDescending(a => a.Created)
          .FirstOrDefault();
        if (actionItemExecution != null && actionItemExecution.IsRead == false)
          status = Docflow.ApprovalTasks.Resources.StateViewUnRead.ToString();
      }
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
      
      // Заголовок.
      var header = GetHeader(_obj, additional, component, hasCoAssignees, isCompound, openedTask, draftNumber);
      taskBlock.AddLabel(header, headerStyle);
      taskBlock.AddLineBreak();
      
      // Задержка исполнения.
      var deadline = GetDeadline(_obj, isCompound);
      if (deadline.HasValue &&
          (_obj.ExecutionState == ExecutionState.OnExecution ||
           _obj.ExecutionState == ExecutionState.OnControl ||
           _obj.ExecutionState == ExecutionState.OnRework))
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(taskBlock, deadline.Value, _obj.Assignee ?? Users.Current);
      
      // Добавить информацию по главному поручению, поручению соисполнителю и подпоручению составного.
      if (!isCompound)
      {
        // Кому.
        this.AddPerformerLabel(taskBlock, _obj.Assignee, performerDeadlineStyle);
        
        // Срок.
        this.AddDeadlineLabel(taskBlock, deadline, performerDeadlineStyle);
        
        // Контролёр.
        this.AddSupervisorLabel(taskBlock, _obj.Supervisor, performerDeadlineStyle, underControl);
        
        // Разделитель.
        this.AddSeparatorLabel(taskBlock);
        taskBlock.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Для подпоручений соисполнителю текст в переписке не соответствует поручению, поэтому берем текст родительской задачи.
        var actionItem = string.Empty;
        if (additional && _obj.ParentAssignment != null)
          actionItem = _obj.ParentAssignment.Task.ActiveText;
        else
          actionItem = _obj.ActiveText;
        
        // Отчет по исполнению поручения и текст поручения.
        var report = GetReportInfo(_obj, stateViewModel.Assignments);
        if (!string.IsNullOrWhiteSpace(report))
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), grayStyle);
          
          taskBlock.AddLineBreak();
          taskBlock.AddLabel(report, labelStyle);
        }
        else
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), labelStyle);
        }
        
        // Добавить подпоручения.
        AddAssignmentTasks(taskBlock, _obj, openedTask, stateViewModel);
      }
      else
      {
        // Добавить информацию по главному поручению составного.
        // Общий срок.
        this.AddFinalDeadlineLabel(taskBlock, deadline, performerDeadlineStyle);
        
        // Контролёр.
        this.AddSupervisorLabel(taskBlock, _obj.Supervisor, performerDeadlineStyle, underControl);
        
        // Разделитель.
        this.AddSeparatorLabel(taskBlock);
        
        // Общий текст составного поручения.
        var actionItem = _obj.ActiveText;
        if (_obj.Status != Sungero.Workflow.Task.Status.Draft && _obj.Status != Sungero.Workflow.Task.Status.InProcess)
          this.AddActionItemTextLabel(taskBlock, actionItem, grayStyle);
        else
          this.AddActionItemTextLabel(taskBlock, actionItem, labelStyle);
        
        // Добавить подпоручения составного поручения и подпоручения к ним.
        AddComponentSubTasks(taskBlock, _obj, openedTask, stateViewModel);
        taskBlock.NeedGroupChildren = true;
      }
      
      taskBlock.IsExpanded = false;
      
      // Раскрыть поручение, если оно в работе, на приёмке, это черновик или это открытое поручение.
      if (isDraft || _obj.Status == Workflow.Task.Status.InProcess ||
          _obj.Status == Workflow.Task.Status.UnderReview || Equals(_obj, openedTask))
        taskBlock.IsExpanded = true;
      
      // Если есть развернутые подчиненные поручения, то развернуть и это.
      if (taskBlock.ChildBlocks.Where(b => b.IsExpanded == true).Any())
        taskBlock.IsExpanded = true;

      return stateView;
    }
    
    /// <summary>
    /// Добавить лейбл Кому.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="style">Стиль.</param>
    public virtual void AddPerformerLabel(Sungero.Core.StateBlock block, IEmployee assignee, Sungero.Core.StateBlockLabelStyle style)
    {
      var assigneeName = assignee != null ? Company.PublicFunctions.Employee.GetShortName(assignee, false) : ActionItemExecutionTasks.Resources.StateViewNotSpecified;
      var performerInfo = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewTo, assigneeName);
      block.AddLabel(performerInfo, style);
    }

    /// <summary>
    /// Добавить лейбл Срок.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="style">Стиль.</param>
    public virtual void AddDeadlineLabel(Sungero.Core.StateBlock block, DateTime? deadline, Sungero.Core.StateBlockLabelStyle style)
    {
      var deadlineLabel = deadline.HasValue ? Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()) : Resources.ActionItemIndefiniteDeadline;
      var deadlineInfo = string.Format(" {0}: {1} ", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineLabel);
      block.AddLabel(deadlineInfo, style);
    }
    
    /// <summary>
    /// Добавить лейбл Общий срок.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="deadline">Срок контейнера составного поручения.</param>
    /// <param name="style">Стиль.</param>
    public virtual void AddFinalDeadlineLabel(Sungero.Core.StateBlock block, DateTime? deadline, Sungero.Core.StateBlockLabelStyle style)
    {
      if (_obj.HasIndefiniteDeadline == true)
      {
        block.AddLabel(string.Format("{0}: {1}",
                                     Docflow.OfficialDocuments.Resources.StateViewFinalDeadline,
                                     Resources.ActionItemIndefiniteDeadline), style);
        return;
      }
      
      if (_obj.FinalDeadline.HasValue)
        deadline = _obj.FinalDeadline;
      
      if (deadline.HasValue)
        block.AddLabel(string.Format("{0}: {1}",
                                     Docflow.OfficialDocuments.Resources.StateViewFinalDeadline,
                                     Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime())), style);
    }
    
    /// <summary>
    /// Добавить лейбл Контролер.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="supervisor">Контролер.</param>
    /// <param name="style">Стиль.</param>
    /// <param name="underControl">На контроле.</param>
    /// <param name="component">Составное.</param>
    [Obsolete("Используйте метод AddSupervisorLabel без параметра Составной.")]
    public virtual void AddSupervisorLabel(Sungero.Core.StateBlock block, IEmployee supervisor, Sungero.Core.StateBlockLabelStyle style, bool underControl, bool component)
    {
      if (underControl && !component && supervisor != null)
      {
        var supervisorInfo = string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(supervisor, false));
        block.AddLabel(supervisorInfo.Trim(), style);
      }
    }

    /// <summary>
    /// Добавить лейбл Контролер.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="supervisor">Контролер.</param>
    /// <param name="style">Стиль.</param>
    /// <param name="underControl">На контроле.</param>
    public virtual void AddSupervisorLabel(Sungero.Core.StateBlock block, IEmployee supervisor, Sungero.Core.StateBlockLabelStyle style, bool underControl)
    {
      if (underControl && supervisor != null)
      {
        var supervisorInfo = string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(supervisor, false));
        block.AddLabel(supervisorInfo.Trim(), style);
      }
    }
    
    /// <summary>
    /// Добавить лейбл Разделитель.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    public virtual void AddSeparatorLabel(Sungero.Core.StateBlock block)
    {
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      block.AddLineBreak();
      block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
      block.AddLineBreak();
    }
    
    /// <summary>
    /// Добавить лейбл Текст поручения.
    /// </summary>
    /// <param name="block">Блок контрола состояния.</param>
    /// <param name="actionItem">Текст поручения.</param>
    /// <param name="style">Стиль.</param>
    public virtual void AddActionItemTextLabel(Sungero.Core.StateBlock block, string actionItem, Sungero.Core.StateBlockLabelStyle style)
    {
      block.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), style);
    }
    
    /// <summary>
    /// Заполнение модели контрола состояния задачи на исполнение поручения.
    /// </summary>
    /// <param name="model">Модель контрола состояния.</param>
    /// <returns>Заполненная (полностью или частично) модель контрола состояния.</returns>
    public static Structures.ActionItemExecutionTask.IStateViewModel GetAllActionItems(Structures.ActionItemExecutionTask.IStateViewModel model)
    {
      if (model.Tasks == null)
        model.Tasks = new List<IActionItemExecutionTask>();
      if (model.Assignments == null)
        model.Assignments = new List<IAssignment>();
      
      var tasksIds = model.Tasks.Select(p => p.Id).ToList();
      var assignmentsIds = model.Assignments.Select(p => p.Id).ToList();
      
      // Подзадачи - пункты составного поручения.
      var subtasks = ActionItemExecutionTasks.GetAll(t => t.ParentTask != null && tasksIds.Contains(t.ParentTask.Id) && !tasksIds.Contains(t.Id)).ToList();
      model.Tasks.AddRange(subtasks);

      // Подзадачи - подчиненные поручения и поручения соисполнителям.
      var assignments = Assignments.GetAll(a => tasksIds.Contains(a.Task.Id) && !assignmentsIds.Contains(a.Id)).ToList();
      model.Assignments.AddRange(assignments);
      assignmentsIds = assignments.Select(a => a.Id).ToList();
      var assignmentSubtasks = ActionItemExecutionTasks.GetAll(t => t.ParentAssignment != null &&
                                                               assignmentsIds.Contains(t.ParentAssignment.Id)).ToList();
      model.Tasks.AddRange(assignmentSubtasks);
      
      if (subtasks.Any() || assignmentSubtasks.Any())
        GetAllActionItems(model);

      Logger.DebugFormat("ActionItemsView: tasks count: {0}", model.Tasks.Count.ToString());
      return model;
    }

    /// <summary>
    /// Получить статус выполнения поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="statusesCache">Кэш статусов.</param>
    /// <returns>Статус.</returns>
    public static string GetStatusInfo(IActionItemExecutionTask task,
                                       System.Collections.Generic.Dictionary<Enumeration?, string> statusesCache)
    {
      Enumeration? status = null;
      if (task == null || task.Status == Workflow.Task.Status.Draft)
      {
        status = Workflow.Task.Status.Draft;
      }
      else if (task.ExecutionState != null && task.IsCompoundActionItem != true)
      {
        status = task.ExecutionState == ExecutionState.OnRework ? ExecutionState.OnExecution : task.ExecutionState;
      }
      else if (task.Status == Workflow.Task.Status.InProcess)
      {
        status = ExecutionState.OnExecution;
      }
      else if (task.Status == Workflow.Task.Status.Aborted)
      {
        status = ExecutionState.Aborted;
      }
      else if (task.Status == Workflow.Task.Status.Suspended)
      {
        status = Workflow.AssignmentBase.Status.Suspended;
      }
      else if (task.Status == Workflow.Task.Status.Completed)
      {
        status = ExecutionState.Executed;
      }
      
      return GetLocalizedValue(status, statusesCache);
    }

    /// <summary>
    /// Получить локализованное значение перечисления.
    /// </summary>
    /// <param name="value">Значение перечисления.</param>
    /// <param name="statusesCache">Кэш статусов.</param>
    /// <returns>Локализованное значение.</returns>
    private static string GetLocalizedValue(Enumeration? value, Dictionary<Enumeration?, string> statusesCache)
    {
      string localizedStatus = string.Empty;
      if (!statusesCache.TryGetValue(value.Value, out localizedStatus))
      {
        localizedStatus = value == Workflow.Task.Status.Draft ?
          Workflow.Tasks.Info.Properties.Status.GetLocalizedValue(value) :
          ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(value);
        statusesCache.Add(value.Value, localizedStatus);
      }

      return localizedStatus;
    }
    
    /// <summary>
    /// Получить заголовок блока поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="additional">Задача соисполнителю.</param>
    /// <param name="component">Задача составного поручения.</param>
    /// <param name="hasCoAssignees">Есть соисполнители.</param>
    /// <param name="isCompound">Составное поручение.</param>
    /// <param name="openedTask">Черновик.</param>
    /// <param name="number">Номер подпункта поручения.</param>
    /// <returns>Заголовок.</returns>
    public static string GetHeader(IActionItemExecutionTask task, bool additional, bool component, bool hasCoAssignees, bool isCompound,
                                   IActionItemExecutionTask openedTask, int? number)
    {
      var header = ActionItemExecutionTasks.Resources.StateViewActionItem;
      if (additional)
        header = ActionItemExecutionTasks.Resources.StateViewActionItemForCoAssignee;
      else
      {
        if (isCompound)
          header = ActionItemExecutionTasks.Resources.StateViewCompoundActionItem;
        
        if (hasCoAssignees)
          header = ActionItemExecutionTasks.Resources.StateViewActionItemForResponsible;
        
        if (component)
        {
          if (number != null)
            return string.Format("{0}{1}", ActionItemExecutionTasks.Resources.StateViewActionItemPart, number);
          else
            header = ActionItemExecutionTasks.Resources.StateViewActionItemPart;
        }
      }
      return header;
    }

    /// <summary>
    /// Получить срок поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="isCompound">Составное.</param>
    /// <returns>Срок.</returns>
    public static DateTime? GetDeadline(IActionItemExecutionTask task, bool isCompound)
    {
      // Срок обычного поручения.
      if (task.MaxDeadline.HasValue)
        return task.MaxDeadline.Value;
      
      // Срок составного поручения.
      if (isCompound)
        return task.ActionItemParts.Select(p => p.Deadline ?? task.FinalDeadline).Max();
      
      return null;
    }
    
    /// <summary>
    /// Получить отчет по поручению.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="assignments">Задания.</param>
    /// <returns>Отчет.</returns>
    public static string GetReportInfo(IActionItemExecutionTask task, List<IAssignment> assignments)
    {
      if (task == null)
        return string.Empty;
      
      var actionItemExecution = assignments.Where(a => ActionItemExecutionAssignments.Is(a))
        .Where(a => Equals(a.Task.Id, task.Id))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      if (actionItemExecution != null && actionItemExecution.Status == Workflow.AssignmentBase.Status.Completed)
        return string.Format("{0}: {1}", ActionItemExecutionTasks.Resources.StateViewReport,
                             Docflow.PublicFunctions.Module.GetFormatedUserText(actionItemExecution.ActiveText));
      
      return string.Empty;
    }
    
    /// <summary>
    /// Добавить блоки подпоручений.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Новое подпоручение.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    public static void AddAssignmentTasks(Sungero.Core.StateBlock block, IActionItemExecutionTask task,
                                          IActionItemExecutionTask openedTask,
                                          Structures.ActionItemExecutionTask.IStateViewModel stateViewModel)
    {
      if (task == null)
        return;
      
      // Добавить ещё не созданные подзадачи черновика.
      if (Equals(task, openedTask) && openedTask.Status == Workflow.Task.Status.Draft)
      {
        var childBlocks = openedTask.CoAssignees
          .SelectMany(a => Functions.ActionItemExecutionTask
                      .GetDraftActionItemExecutionTaskStateView(openedTask, stateViewModel, a.Assignee, openedTask.Assignee, openedTask.ActiveText,
                                                                openedTask.CoAssigneesDeadline ?? openedTask.MaxDeadline, null,
                                                                !openedTask.IsCompoundActionItem.Value, false, true)
                      .Blocks);
        foreach (var childBlock in childBlocks)
          block.AddChildBlock(childBlock);
        block.IsExpanded = true;
        return;
      }
      
      var subTasks = stateViewModel.Tasks
        .Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task.Id, task.Id))
        .Where(t => t.Started >= task.Started)
        .OrderBy(t => t.Started)
        .ToList();
      
      // Добавить вывод черновика подпоручения.
      if (openedTask != null && openedTask.ParentAssignment != null &&
          Equals(task, openedTask.ParentAssignment.Task) &&
          !subTasks.Any(st => Equals(openedTask, st)))
        subTasks.Add(openedTask);
      
      var blocks = subTasks.SelectMany(t => Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(t, openedTask,
                                                                                                                  stateViewModel,
                                                                                                                  null, false, true).Blocks);
      foreach (var childBlock in blocks)
        block.AddChildBlock(childBlock);
      block.IsExpanded = subTasks.Any(t => t.Status == Workflow.Task.Status.InProcess || t.Status == Workflow.Task.Status.Draft) ||
        block.ChildBlocks.Any(b => b.IsExpanded);
    }
    
    /// <summary>
    /// Добавить блоки подпоручений составного поручения.
    /// </summary>
    /// <param name="stateBlock">Схема.</param>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Черновик.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    public static void AddComponentSubTasks(Sungero.Core.StateBlock stateBlock, IActionItemExecutionTask task,
                                            IActionItemExecutionTask openedTask,
                                            Structures.ActionItemExecutionTask.IStateViewModel stateViewModel)
    {
      if (task == null)
        return;
      
      // Добавить ещё не созданные подзадачи черновика.
      if (Equals(task, openedTask) && openedTask.Status == Workflow.Task.Status.Draft)
      {
        var draftTaskParts = openedTask.ActionItemParts.OrderBy(a => a.Number);
        foreach (var part in draftTaskParts)
        {
          var itemStateView = Functions.ActionItemExecutionTask.GetDraftActionItemExecutionTaskStateView(
            openedTask, stateViewModel, part.Assignee, part.Supervisor ?? openedTask.Supervisor,
            string.IsNullOrEmpty(part.ActionItemPart) ? openedTask.ActiveText : part.ActionItemPart,
            part.Deadline ?? openedTask.FinalDeadline,
            part.Number, false, false, true);
          
          var itemBlock = itemStateView.Blocks.FirstOrDefault();
          foreach (var coAssignee in Functions.ActionItemExecutionTask.GetPartCoAssignees(task, part.PartGuid))
          {
            var block = Functions.ActionItemExecutionTask.GetDraftActionItemExecutionTaskStateView(
              openedTask, stateViewModel, coAssignee, part.Assignee, string.IsNullOrEmpty(part.ActionItemPart) ? openedTask.ActiveText : part.ActionItemPart,
              part.CoAssigneesDeadline, null, true, false, true).Blocks.FirstOrDefault();
            itemBlock.AddChildBlock(block);
            itemBlock.IsExpanded = true;
          }
          stateBlock.AddChildBlock(itemBlock);
          stateBlock.IsExpanded = true;
        }
        return;
      }
      
      foreach (var partTask in task.ActionItemParts.OrderBy(pt => pt.Number))
      {
        var currentPartTask = partTask.ActionItemPartExecutionTask;
        if (currentPartTask == null || stateBlock.ChildBlocks.Any(b => b.HasEntity(currentPartTask)))
          continue;
        
        var childBlocks = Functions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(currentPartTask,
                                                                                                openedTask, stateViewModel,
                                                                                                draftNumber: partTask.Number,
                                                                                                false, true).Blocks;
        foreach (var block in childBlocks)
          stateBlock.AddChildBlock(block);
      }
    }
    
    /// <summary>
    /// Добавить блок информации о рассмотрении документа руководителем.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="tasks">Задачи.</param>
    /// <param name="assignments">Задания.</param>
    /// <returns>Полученный блок.</returns>
    public Sungero.Core.StateBlock AddReviewBlock(Sungero.Core.StateView stateView, List<IActionItemExecutionTask> tasks, List<IAssignment> assignments)
    {
      var reviewAssignmentBase = assignments.Where(a => Docflow.ApprovalReviewAssignments.Is(a))
        .Where(a => Equals(a.Task.Id, _obj.MainTask.Id))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      if (reviewAssignmentBase == null)
        return null;

      var reviewAssignment = Docflow.ApprovalReviewAssignments.As(reviewAssignmentBase);
      
      // Добавить блок информации по отправителю.
      var text = Docflow.ApprovalTasks.Resources.StateViewDocumentSentForApproval;
      var task = reviewAssignment.Task;
      Docflow.PublicFunctions.OfficialDocument
        .AddUserActionBlock(stateView, task.Author, text, task.Started.Value, task, string.Empty, task.StartedBy);
      
      var author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(reviewAssignment.Performer, reviewAssignment.CompletedBy);
      var actionItems = tasks
        .Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task.Id, reviewAssignment.Task.Id) && t.Status != Workflow.Task.Status.Draft)
        .OrderBy(t => t.Started);
      var isCompleted = reviewAssignment.Status == Workflow.AssignmentBase.Status.Completed;

      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle();
      var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      // Добавить блок. Установить иконку и сущность.
      var block = stateView.AddBlock();
      block.Entity = reviewAssignment;
      if (isCompleted)
        block.AssignIcon(reviewAssignment.Info.Actions.AddResolution, StateBlockIconSize.Large);
      else
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);

      // Рассмотрение руководителем ещё в работе.
      if (!isCompleted)
      {
        // Добавить заголовок.
        block.AddLabel(Docflow.Resources.StateViewDocumentReview, headerStyle);
        block.AddLineBreak();
        Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Info.Properties.Status.GetLocalizedValue(reviewAssignment.Status));
        var employeeName = Employees.Is(reviewAssignment.Performer) ?
          Company.PublicFunctions.Employee.GetShortName(Employees.As(reviewAssignment.Performer), false) :
          reviewAssignment.Performer.Name;
        var headerText = string.Format("{0}: {1} ",
                                       Docflow.Resources.StateViewAddressee,
                                       employeeName);
        
        if (reviewAssignment.Deadline != null)
        {
          var deadlineText = string.Format(" {0}: {1}",
                                           Docflow.OfficialDocuments.Resources.StateViewDeadline,
                                           Docflow.PublicFunctions.Module.ToShortDateShortTime(reviewAssignment.Deadline.Value.ToUserTime()));
          headerText = headerText + deadlineText;
        }
        
        block.AddLabel(headerText, performerStyle);
        
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, reviewAssignment.Deadline.Value, reviewAssignment.Performer);
      }
      else
      {
        // Рассмотрение завершено.
        // Добавить заголовок.
        var resolutionDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(reviewAssignment.Completed.Value.ToUserTime());
        block.AddLabel(Docflow.Resources.StateViewResolution, headerStyle);
        block.AddLineBreak();
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     RecordManagement.DocumentReviewTasks.Resources.StateViewAuthor,
                                     author,
                                     Docflow.OfficialDocuments.Resources.StateViewDate,
                                     resolutionDate), performerStyle);
        block.AddLineBreak();
        block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Если поручения не созданы, значит, рассмотрение выполнено с результатом "Вынести резолюцию" или "Принято к сведению".
        // В старых задачах поручение и рассмотрение не связаны, поэтому обрабатываем такие случаи как резолюцию.
        if (!actionItems.Any())
        {
          var comment = Docflow.PublicFunctions.Module.GetFormatedUserText(reviewAssignment.Texts.Last().Body);
          block.AddLabel(comment);
          block.AddLineBreak();
        }
        else
        {
          // Добавить информацию по каждому поручению.
          foreach (var actionItem in actionItems)
          {
            if (actionItem.IsCompoundActionItem == true)
            {
              foreach (var item in actionItem.ActionItemParts)
              {
                AddActionItemInfo(block, item.ActionItemPartExecutionTask, author);
              }
            }
            else
            {
              AddActionItemInfo(block, actionItem, author);
            }
          }
        }
      }
      return block;
    }
    
    /// <summary>
    /// Добавить информацию о созданном поручении в резолюцию.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <param name="author">Автор.</param>
    public static void AddActionItemInfo(Sungero.Core.StateBlock block, IActionItemExecutionTask actionItem, string author)
    {
      block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
      
      block.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem.ActiveText));
      block.AddLineBreak();
      
      // Исполнители.
      var performerStyle = Sungero.Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var info = string.Empty;

      if (actionItem.CoAssignees.Any())
        info += string.Format("{0}: {1}",
                              Docflow.Resources.StateViewResponsible,
                              Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false));
      else
        info += string.Format("{0}: {1}", Docflow.Resources.StateViewAssignee, Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false));
      
      // Срок.
      var deadlineText = actionItem.MaxDeadline.HasValue ? Docflow.PublicFunctions.Module.ToShortDateShortTime(actionItem.MaxDeadline.Value.ToUserTime()) : Resources.ActionItemIndefiniteDeadline;
      info += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineText);
      
      // Контролер.
      if (actionItem.IsUnderControl == true)
        info += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(actionItem.Supervisor, false));
      
      // Соисполнители.
      if (actionItem.CoAssignees.Any())
      {
        info += Environment.NewLine;
        info += string.Format("{0}: {1}",
                              Docflow.Resources.StateViewCoAssignees,
                              string.Join(", ", actionItem.CoAssignees.Select(c => Company.PublicFunctions.Employee.GetShortName(c.Assignee, false))));
        // Срок.
        var coAssigneesDeadlineText = actionItem.CoAssigneesDeadline.HasValue ? Docflow.PublicFunctions.Module.ToShortDateShortTime(actionItem.CoAssigneesDeadline.Value.ToUserTime()) :
          Resources.ActionItemIndefiniteDeadline;
        info += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, coAssigneesDeadlineText);
      }
      
      block.AddLabel(info, performerStyle);
      block.AddLineBreak();
      block.AddLineBreak();
    }
    
    #endregion
    
    #region Изменение поручения
    
    /// <summary>
    /// Изменить простое поручение.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    [Public, Remote]
    public virtual void ChangeSimpleActionItem(IActionItemChanges changes)
    {
      // Запомнить текущие задания на исполнение и приемку.
      var oldExecutionAssignment = this.GetActualActionItemExecutionAssignment();
      var oldSupervisorAssignment = this.GetActualActionItemSupervisorAssignment();
      
      // Изменить значения в задаче.
      this.UpdateSimpleActionItemTask(_obj, changes);

      var canEdit = ActionItemExecutionTasks.GetAll().Where(a => a.Id == _obj.Id && (a.OnEditGuid == null || a.OnEditGuid == string.Empty)).Any();
      
      if (canEdit)
        _obj.OnEditGuid = Guid.NewGuid().ToString();
      else
        throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess);
      
      _obj.Save();
      
      var addressees = new List<IUser>();
      
      try
      {
        // Получить список заинтересованных в изменении поручения для отправки уведомления.
        // Находится здесь, чтобы учитывать состояние поручения до изменений.
        addressees = this.GetActionItemChangeNotificationAddressees(changes, oldExecutionAssignment != null);
        
        // Переадресовать измененные задания.
        this.ForwardChangedAssignments(changes, oldExecutionAssignment, oldSupervisorAssignment);
        
        // Обработать смену срока в существующем задании на исполнение.
        if (!Equals(changes.OldDeadline, changes.NewDeadline) &&
            Equals(changes.OldAssignee, changes.NewAssignee) &&
            oldExecutionAssignment != null)
        {
          // Прокинуть срок в задание исполнителя, если задача не заблокирована.
          this.ChangeExecutionAssignmentDeadline(changes.NewDeadline, oldExecutionAssignment);
          
          // Прекратить запросы на продление срока от исполнителя, у которого сменился срок.
          this.AbortDeadlineExtensionTasks(_obj);
        }
        
        // Прекратить неактуальные запросы отчетов от контролера, от отв. исполнителя, к отв. исполнителю.
        this.AbortReportRequestTasks(changes, oldExecutionAssignment);
      }
      catch (Exception e)
      {
        _obj.OnEditGuid = string.Empty;
        _obj.Save();
        throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemChangeError, e);
      }

      // Прекращение подзадач удаленным соисполнителям (поручения, запросы отчетов, запросы продления сроков).
      this.AbortDeletedCoAssigneeTasks(changes);

      // Разослать уведомления об изменении поручения.
      this.SendActionItemChangeNotifications(changes, addressees);
    }
    
    /// <summary>
    /// Изменить составное поручение.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    [Public, Remote]
    public virtual void ChangeCompoundActionItem(IActionItemChanges changes)
    {
      // Установить начало режима корректировки поручения.
      var onEdit = ActionItemExecutionTasks.GetAll().Where(a => a.Id == _obj.Id && (a.OnEditGuid ?? string.Empty) != string.Empty).Any();
      if (!onEdit)
      {
        _obj.OnEditGuid = Guid.NewGuid().ToString();
      }
      else
      {
        throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess);
      }
      
      // Протащить изменения в основную задачу (в карточку и грид).
      this.UpdateCompoundActionItemTask(_obj, changes);
      _obj.Save();
    }
    
    /// <summary>
    /// Проверить возможность изменения поручения перед показом диалога корректировки.
    /// </summary>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    [Remote(IsPure = true)]
    public virtual string CheckActionItemEditBeforeDialog()
    {
      // Проверить, что поручение находится в работе.
      var actionItemInWorkErrorText = this.CheckActionItemInProcess();
      if (!string.IsNullOrEmpty(actionItemInWorkErrorText))
        return actionItemInWorkErrorText;
      
      // Проверить, что поручение никем не корректируется.
      var actionItemNotInChangingProcessErrorText = Functions.Module.CheckActionItemNotInChangingProcess(new List<IActionItemExecutionTask>() { _obj });
      if (!string.IsNullOrEmpty(actionItemNotInChangingProcessErrorText))
        return actionItemNotInChangingProcessErrorText;
      
      // Проверить, что по поручению созданы все актуальные задания.
      var actionItemAssignmentsCreatedErrorText = Functions.Module.CheckActionItemAssignmentsCreated(new List<IActionItemExecutionTask>() { _obj });
      if (!string.IsNullOrEmpty(actionItemAssignmentsCreatedErrorText))
        return actionItemAssignmentsCreatedErrorText;
      
      // Проверить, что поручение никем не заблокировано.
      var actionItemNotLockedErrorText = this.CheckActionItemNotLocked();
      if (!string.IsNullOrEmpty(actionItemNotLockedErrorText))
        return actionItemNotLockedErrorText;
      
      return null;
    }
    
    /// <summary>
    /// Проверить возможность изменения пункта поручения до открытия диалога корректировки.
    /// </summary>
    /// <param name="actionItemPartExecutionTask">Задача по пункту поручения.</param>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    [Remote(IsPure = true)]
    public virtual string CheckActionItemPartEditBeforeDialog(IActionItemExecutionTask actionItemPartExecutionTask)
    {
      return this.CheckActionItemPartEdit(actionItemPartExecutionTask);
    }
    
    /// <summary>
    /// Проверить возможность изменения пункта поручения при нажатии подтверждения в диалоге корректировки.
    /// </summary>
    /// <param name="actionItemPartExecutionTask">Задача по пункту поручения.</param>
    /// <param name="newAssignee">Новый исполнитель.</param>
    /// <param name="deadline">Новый срок.</param>
    /// <param name="dialogOpenDate">Дата открытия диалога.</param>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    [Remote(IsPure = true)]
    public virtual string CheckActionItemPartEditInDialog(IActionItemExecutionTask actionItemPartExecutionTask,
                                                          IEmployee newAssignee, DateTime? deadline,
                                                          DateTime? dialogOpenDate)
    {
      // На приемке нельзя менять ответственного исполнителя / срок в пункте поручения.
      if (actionItemPartExecutionTask.IsCompoundActionItem != true &&
          (!Equals(actionItemPartExecutionTask.Assignee, newAssignee) || !Equals(actionItemPartExecutionTask.Deadline, deadline)) &&
          Functions.ActionItemExecutionTask.CheckActionItemOnControl(actionItemPartExecutionTask))
        return ActionItemExecutionTasks.Resources.ActionItemOnControlCannotChangeAssignee;
      
      // Проверить, что пункт поручения не был изменен, пока был открыт диалог.
      var actionItemChangedErrorText = Functions.Module
        .CheckActionItemNotChanged(new List<int>() { actionItemPartExecutionTask.Id }, dialogOpenDate);
      if (!string.IsNullOrEmpty(actionItemChangedErrorText))
        return actionItemChangedErrorText;
      
      var errorMessage = this.CheckActionItemPartEdit(actionItemPartExecutionTask);
      if (!string.IsNullOrWhiteSpace(errorMessage))
        return errorMessage;
      
      return null;
    }
    
    /// <summary>
    /// Проверить возможность изменения пункта поручения.
    /// </summary>
    /// <param name="actionItemPartExecutionTask">Задача по пункту поручения.</param>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    [Remote(IsPure = true)]
    public virtual string CheckActionItemPartEdit(IActionItemExecutionTask actionItemPartExecutionTask)
    {
      // Проверить, что пункт поручения уже создан.
      if (actionItemPartExecutionTask == null)
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить, что пункт поручения находится в работе.
      var actionItemInWorkErrorText = Functions.ActionItemExecutionTask.CheckActionItemInProcess(actionItemPartExecutionTask);
      if (!string.IsNullOrEmpty(actionItemInWorkErrorText))
        return actionItemInWorkErrorText;
      
      // Проверить, что головное поручение никем не корректируется.
      var mainActionItem = ActionItemExecutionTasks.As(actionItemPartExecutionTask.ParentTask);
      var mainActionItemNotInChangingProcessErrorText = Functions.Module.CheckCurrentActionItemNotInChangingProcess(new List<int>() { mainActionItem.Id });
      if (!string.IsNullOrEmpty(mainActionItemNotInChangingProcessErrorText))
        return mainActionItemNotInChangingProcessErrorText;
      
      // Проверить, что пункт поручения никем не корректируется (в том числе и подпоручения соисполнителям пункта).
      var actionItemNotInChangingProcessErrorText = Functions.Module.CheckActionItemNotInChangingProcess(new List<IActionItemExecutionTask>() { actionItemPartExecutionTask });
      if (!string.IsNullOrEmpty(actionItemNotInChangingProcessErrorText))
        return actionItemNotInChangingProcessErrorText;
      
      // Проверить, что по пункту поручения созданы все актуальные задания.
      var actionItemAssignmentsCreatedErrorText = Functions.Module.CheckActionItemAssignmentsCreated(new List<IActionItemExecutionTask>() { actionItemPartExecutionTask });
      if (!string.IsNullOrEmpty(actionItemAssignmentsCreatedErrorText))
        return actionItemAssignmentsCreatedErrorText;

      // Проверить, что головное поручение никем не заблокировано.
      var mainActionItemNotLockedErrorText = Functions.ActionItemExecutionTask.CheckActionItemNotLocked(mainActionItem);
      if (!string.IsNullOrEmpty(mainActionItemNotLockedErrorText))
        return mainActionItemNotLockedErrorText;
      
      // Проверить, что пункт поручения никем не заблокирован.
      var actionItemNotLockedErrorText = Functions.ActionItemExecutionTask.CheckActionItemNotLocked(actionItemPartExecutionTask);
      if (!string.IsNullOrEmpty(actionItemNotLockedErrorText))
        return actionItemNotLockedErrorText;
      
      return null;
    }
    
    /// <summary>
    /// Проверить возможность изменения поручения в диалоге корректировки.
    /// </summary>
    /// <param name="newAssignee">Новый исполнитель.</param>
    /// <param name="deadline">Новый срок.</param>
    /// <param name="dialogOpenDate">Дата открытия диалога.</param>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    /// <remarks>Параметр "Исполнитель" неактуален для главного составного поручения.</remarks>
    [Remote(IsPure = true)]
    public virtual string CheckActionItemEditInDialog(IEmployee newAssignee, DateTime? deadline, DateTime? dialogOpenDate)
    {
      // На приемке нельзя менять ответственного исполнителя / срок исполнения поручения.
      if (_obj.IsCompoundActionItem != true &&
          (!Equals(_obj.Assignee, newAssignee) || !Equals(_obj.Deadline, deadline)) &&
          this.CheckActionItemOnControl())
        return ActionItemExecutionTasks.Resources.ActionItemOnControlCannotChangeAssignee;
      
      // Проверить, что поручение не было изменено, пока был открыт диалог.
      var actionItemChangedErrorText = Functions.Module.CheckActionItemNotChanged(new List<int> { _obj.Id }, dialogOpenDate);
      if (!string.IsNullOrEmpty(actionItemChangedErrorText))
        return actionItemChangedErrorText;
      
      // Проверить, что поручение находится в работе.
      var actionItemInWorkErrorText = this.CheckActionItemInProcess();
      if (!string.IsNullOrEmpty(actionItemInWorkErrorText))
        return actionItemInWorkErrorText;
      
      // Проверить, что поручение никем не корректируется.
      var actionItemNotInChangingProcessErrorText = Functions.Module.CheckActionItemNotInChangingProcess(new List<IActionItemExecutionTask>() { _obj });
      if (!string.IsNullOrEmpty(actionItemNotInChangingProcessErrorText))
        return actionItemNotInChangingProcessErrorText;
      
      // Проверить, что по поручению созданы все актуальные задания.
      var actionItemAssignmentsCreatedErrorText = Functions.Module.CheckActionItemAssignmentsCreated(new List<IActionItemExecutionTask>() { _obj });
      if (!string.IsNullOrEmpty(actionItemAssignmentsCreatedErrorText))
        return actionItemAssignmentsCreatedErrorText;
      
      // Проверить, что поручение никем не заблокировано.
      var actionItemNotLockedErrorText = this.CheckActionItemNotLocked();
      if (!string.IsNullOrEmpty(actionItemNotLockedErrorText))
        return actionItemNotLockedErrorText;
      
      return null;
    }
    
    /// <summary>
    /// Проверить возможность изменения составного поручения в диалоге массовой корректировки.
    /// </summary>
    /// <param name="actionItemPartTasks">Пункты поручения.</param>
    /// <param name="newSupervisor">Новый контролер.</param>
    /// <param name="newAssignee">Новый исполнитель.</param>
    /// <param name="deadline">Новый срок.</param>
    /// <param name="dialogOpenDate">Дата открытия диалога.</param>
    /// <returns>Текст ошибки или пустую строку, если ошибок нет.</returns>
    [Remote(IsPure = true)]
    public virtual string CheckCompoundActionItemEditInDialog(List<IActionItemExecutionTask> actionItemPartTasks, IEmployee newSupervisor,
                                                              IEmployee newAssignee, DateTime? deadline, DateTime? dialogOpenDate)
    {
      // Проверить, что хотя бы один из выбранных пунктов находится не на приемке.
      var actionItemPartTasksInProcess = actionItemPartTasks.Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess).ToList();
      if (!Equals(_obj.Deadline, deadline) && actionItemPartTasksInProcess.Any() &&
          actionItemPartTasksInProcess.All(p => p.ExecutionState == ExecutionState.OnControl))
        return ActionItemExecutionTasks.Resources.ActionItemSelectedPartsOnControlCannotChangeDeadline;

      // Проверить, что головное поручение не было изменено, пока был открыт диалог.
      if (!string.IsNullOrEmpty(Functions.Module.CheckActionItemNotChanged(new List<int> { _obj.Id }, dialogOpenDate)))
        return ActionItemExecutionTasks.Resources.ActionItemWasChanged;
      
      // Проверить, что головное поручение находится в работе.
      if (!string.IsNullOrEmpty(this.CheckActionItemInProcess()))
        return ActionItemExecutionTasks.Resources.ActionItemExecuted;
      
      // Проверить, что хотя бы один из выбранных пунктов находится в работе.
      if (actionItemPartTasks.All(p => p.Status != Sungero.Workflow.Task.Status.InProcess))
        return ActionItemExecutionTasks.Resources.ActionItemSelectedPartsExecuted;
      
      // Проверить, что головное поручение никем не заблокировано.
      var actionItemNotLockedErrorText = this.CheckActionItemNotLocked();
      if (!string.IsNullOrEmpty(actionItemNotLockedErrorText))
        return actionItemNotLockedErrorText;
      
      // Проверить только выбранные пункты, которые в работе, так как корректироваться будут только они.
      foreach (var actionItemPartTask in actionItemPartTasksInProcess)
      {
        // Проверить, что пункт никем не заблокирован.
        actionItemNotLockedErrorText = Functions.ActionItemExecutionTask.CheckActionItemNotLocked(actionItemPartTask);
        if (!string.IsNullOrEmpty(actionItemNotLockedErrorText))
          return actionItemNotLockedErrorText;
      }
      
      // Проверить, что пункт не был изменен, пока был открыт диалог.
      var tasksIds = actionItemPartTasksInProcess.Select(t => t.Id).ToList();
      if (!string.IsNullOrEmpty(Functions.Module.CheckActionItemNotChanged(tasksIds, dialogOpenDate)))
        return ActionItemExecutionTasks.Resources.ActionItemWasChanged;
      
      // Проверить, что в пунктах созданы все актуальные задания.
      if (!string.IsNullOrEmpty(Functions.Module.CheckActionItemAssignmentsCreated(actionItemPartTasksInProcess)))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить, что пункты никем не корректируются и головное поручение никем не корректируется.
      actionItemPartTasksInProcess.Add(_obj);
      if (!string.IsNullOrEmpty(Functions.Module.CheckActionItemNotInChangingProcess(actionItemPartTasksInProcess)))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      return null;
    }

    /// <summary>
    /// Проверить, что поручение находится в работе (не завершено и не прекращено).
    /// </summary>
    /// <returns>Текст ошибки, если завершено или прекращено. Иначе пустую строку.</returns>
    public virtual string CheckActionItemInProcess()
    {
      var actionItemInProcess = _obj.Status == Sungero.Workflow.Task.Status.InProcess;
      if (!actionItemInProcess)
        return ActionItemExecutionTasks.Resources.ActionItemExecuted;
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что работы по поручению находятся на приемке.
    /// </summary>
    /// <returns>True - если работы на приемке, иначе - false.</returns>
    /// <remarks>Метод нужен для того, чтобы переполучить поручение и сравнить его актуальный статус.</remarks>
    public virtual bool CheckActionItemOnControl()
    {
      return _obj.ExecutionState == ExecutionState.OnControl;
    }

    /// <summary>
    /// Проверить, что карточка поручения не заблокирована другими пользователями.
    /// </summary>
    /// <returns>Текст ошибки, если заблокирована. Иначе пустую строку.</returns>
    public virtual string CheckActionItemNotLocked()
    {
      var taskLockInfo = Locks.GetLockInfo(_obj);
      if (taskLockInfo.IsLockedByOther)
        return ActionItemExecutionTasks.Resources.ActionItemExecutionTaskLockedByUserFormat(taskLockInfo.OwnerName);
      
      return null;
    }
    
    /// <summary>
    /// Установить параметры, с помощью которых формируется текст записи в историю поручения при корректировке сроков.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    [Public, Remote]
    public virtual void SetActionItemChangeDeadlinesParams(IActionItemChanges changes)
    {
      var needChangeDeadlines = false;
      var needChangeOnlyDeadlines = false;
      
      if (changes.OldDeadline != changes.NewDeadline ||
          (changes.NewCoAssignees != null && changes.NewCoAssignees.Any() &&
           changes.OldCoAssignees != null && changes.OldCoAssignees.Any() &&
           changes.CoAssigneesOldDeadline != changes.CoAssigneesNewDeadline))
      {
        needChangeDeadlines = true;
        
        // При массовой корректировке новое значение контролера будет пустым, если он не был изменен.
        // При этом старое значение может быть заполнено, если пункт на контроле.
        // И казаться будет, что контролер изменился, хотя это не так.
        // Поэтому добавлена дополнительная проверка, что новый контролер заполнен, в ситуации, когда новый и старый контролер не равны.
        if (Equals(changes.OldAssignee, changes.NewAssignee) &&
            changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees) &&
            (Equals(changes.OldSupervisor, changes.NewSupervisor) || !Equals(changes.OldSupervisor, changes.NewSupervisor) && changes.NewSupervisor == null))
          needChangeOnlyDeadlines = true;
      }
      
      this.SetActionItemChangeDeadlinesParams(needChangeDeadlines, needChangeOnlyDeadlines);
    }
    
    /// <summary>
    /// Установить параметры, с помощью которых формируется текст записи в историю поручения при изменении только сроков.
    /// </summary>
    /// <param name="needChangeDeadlines">Признак изменения сроков.</param>
    /// <param name="needChangeOnlyDeadlines">Признак изменения только сроков.</param>
    [Public]
    public virtual void SetActionItemChangeDeadlinesParams(bool needChangeDeadlines, bool needChangeOnlyDeadlines)
    {
      if (needChangeDeadlines)
        ((Domain.Shared.IExtendedEntity)_obj).Params[PublicConstants.ActionItemExecutionTask.ChangeDeadlinesWriteInHistoryParamName] = true;
      if (needChangeOnlyDeadlines)
        ((Domain.Shared.IExtendedEntity)_obj).Params[PublicConstants.ActionItemExecutionTask.ChangeOnlyDeadlinesWriteInHistoryParamName] = true;
    }
    
    /// <summary>
    /// Получить текст для записи в историю поручения информации об изменении сроков.
    /// </summary>
    /// <param name="actionItemParams">Параметры поручения.</param>
    /// <returns>Текст.</returns>
    public virtual string GetActionItemChangeDeadlineHistoryText(System.Collections.Generic.Dictionary<string, object> actionItemParams)
    {
      var deadlineComment = string.Empty;
      var coAssigneesDeadlineComment = string.Empty;
      var changeDeadlineText = string.Empty;
      
      if (_obj.IsCompoundActionItem.Value)
      {
        deadlineComment = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeCompoundActionItemHistory;
      }
      else
      {
        var deadlineChanged = !Equals(_obj.Deadline, _obj.State.Properties.Deadline.PreviousValue);
        var coAssigneesDeadlineChanged = !Equals(_obj.CoAssigneesDeadline, _obj.State.Properties.CoAssigneesDeadline.OriginalValue);
        
        // Сформировать текст об изменении срока исполнителя.
        if (deadlineChanged)
          deadlineComment = Functions.ActionItemExecutionTask.GenerateChangedDeadlineText(_obj,
                                                                                          _obj.State.Properties.Deadline.OriginalValue,
                                                                                          _obj.Deadline,
                                                                                          false);
        
        // Сформировать текст об изменении срока соисполнителей.
        if (coAssigneesDeadlineChanged)
          coAssigneesDeadlineComment = Functions.ActionItemExecutionTask.GenerateChangedDeadlineText(_obj,
                                                                                                     _obj.State.Properties.CoAssigneesDeadline.OriginalValue,
                                                                                                     _obj.CoAssigneesDeadline,
                                                                                                     true);
      }
      
      // Сформировать комментарий для записи в историю.
      if (!string.IsNullOrEmpty(deadlineComment) && !string.IsNullOrEmpty(coAssigneesDeadlineComment))
        changeDeadlineText = string.Format("{0}. {1}", deadlineComment, coAssigneesDeadlineComment);
      else if (!string.IsNullOrEmpty(deadlineComment))
        changeDeadlineText = string.Format("{0}", deadlineComment);
      else if (!string.IsNullOrEmpty(coAssigneesDeadlineComment))
        changeDeadlineText = string.Format("{0}", coAssigneesDeadlineComment);
      
      return changeDeadlineText;
    }
    
    /// <summary>
    /// Изменить свойства простой задачи на исполнение поручения или пункта поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void UpdateSimpleActionItemTask(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(task, changes);
      
      // Изменение контролёра.
      this.ChangeSupervisor(task, changes);
      
      // Изменение ответственного исполнителя.
      this.ChangeAssignee(task, changes);
      
      // Изменение срока ответственного исполнителя.
      if (changes.NewDeadline != null)
      {
        task.Deadline = changes.NewDeadline;
        task.HasIndefiniteDeadline = false;
      }
      
      // Изменение срока соисполнителей.
      this.ChangeCoAssigneesDeadline(task, changes);
      
      // Добавление соисполнителей.
      this.AddNewCoAssignees(task, changes);
      
      // Удаление соисполнителей.
      this.DeleteCoAssignees(task, changes);
    }
    
    /// <summary>
    /// Изменить свойства контейнера составного поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void UpdateCompoundActionItemTask(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      var allPartsSelected = changes.TaskIds != null && changes.TaskIds.Count() == task.ActionItemParts.Count();
      var allPartsInProcess = _obj.ActionItemParts.All(p => p.ActionItemPartExecutionTask.Status == Workflow.Task.Status.InProcess);
      var allPartsNotOnControl = _obj.ActionItemParts.All(p => p.ActionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl);
      
      Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(task, changes);
      
      // Изменение контролёра.
      if (allPartsSelected && (task.IsUnderControl != true || task.IsUnderControl == true && allPartsInProcess))
        this.ChangeSupervisor(task, changes);
      
      // Изменение общего срока.
      if (changes.NewDeadline != null && allPartsSelected && allPartsInProcess && allPartsNotOnControl)
      {
        task.FinalDeadline = changes.NewDeadline;
        task.HasIndefiniteDeadline = false;
      }
      
      // Массовая корректировка пунктов, обновление контролёра и сроков в гриде.
      var actionItemPartsTasks = ActionItemExecutionTasks.GetAll().Where(t => changes.TaskIds.Contains(t.Id) && t.Status == Sungero.RecordManagement.ActionItemExecutionTask.Status.InProcess).ToList();
      foreach (var actionItemPartTask in actionItemPartsTasks)
      {
        var actionItemPart = _obj.ActionItemParts.Where(x => Equals(x.ActionItemPartExecutionTask, actionItemPartTask)).FirstOrDefault();
        var oldTaskDeadline = actionItemPart.Deadline;
        if (changes.NewDeadline != changes.OldDeadline)
        {
          actionItemPart.Deadline = changes.NewDeadline;
          actionItemPart.CoAssigneesDeadline = this.GetCoAssigneeDeadline(actionItemPartTask, changes, oldTaskDeadline);
        }
        if (changes.NewSupervisor != null)
          actionItemPart.Supervisor = changes.NewSupervisor;
      }
    }
    
    /// <summary>
    /// Изменить свойства пункта составного поручения при массовой корректировке.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void UpdateActionItemPartTask(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      // Изменение контролёра.
      this.ChangeSupervisor(task, changes);
      
      // Изменение сроков пункта составного.
      var taskInProcess = task.Status == Workflow.Task.Status.InProcess;
      if (changes.NewDeadline != null && changes.NewDeadline != changes.OldDeadline && taskInProcess && task.ExecutionState != ExecutionState.OnControl)
      {
        Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(task, changes);
        
        var oldTaskDeadline = task.Deadline;
        // Изменение срока ответственного исполнителя.
        this.ChangeAssigneeDeadline(task, changes);
        
        // Изменение cрока соисполнителей.
        this.ChangeCoAssigneeDeadline(task, changes, oldTaskDeadline);
      }
    }
    
    /// <summary>
    /// Изменить срок соисполнителя согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="oldTaskDeadline">Старый срок.</param>
    public virtual void ChangeCoAssigneeDeadline(IActionItemExecutionTask task, IActionItemChanges changes, DateTime? oldTaskDeadline)
    {
      var actionItemPart = _obj.ActionItemParts.Where(x => Equals(x.ActionItemPartExecutionTask, task)).FirstOrDefault();
      if (task.CoAssignees.Any() && actionItemPart != null)
        task.CoAssigneesDeadline = actionItemPart.CoAssigneesDeadline;
    }
    
    /// <summary>
    /// Изменить срок исполнителя согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void ChangeAssigneeDeadline(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      var deadlineWasIndefinite = task.HasIndefiniteDeadline == true;
      task.HasIndefiniteDeadline = false;
      task.Deadline = changes.NewDeadline;
    }
    
    /// <summary>
    /// Получить новый срок соисполнителя согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="oldTaskDeadline">Старый срок исполнителя.</param>
    /// <returns>Новый срок соисполнителя.</returns>
    public virtual DateTime? GetCoAssigneeDeadline(IActionItemExecutionTask task, IActionItemChanges changes, DateTime? oldTaskDeadline)
    {
      DateTime? newCoAssigneeDeadline;
      if (task.HasIndefiniteDeadline != true)
      {
        newCoAssigneeDeadline = Docflow.PublicFunctions.Module.GetNewCoAssigneeDeadline(oldTaskDeadline, task.CoAssigneesDeadline,
                                                                                        changes.NewDeadline, Employees.As(changes.InitiatorOfChange));
      }
      else
      {
        // Если поручение ранее было бессрочным, то срок соисполнителя взять по умолчанию.
        var settings = Functions.Module.GetSettings();
        var deadlineShiftInDays = -settings.ControlRelativeDeadlineInDays ?? 0;
        var deadlineShiftInHours = -settings.ControlRelativeDeadlineInHours ?? 0;
        newCoAssigneeDeadline = Docflow.PublicFunctions.Module.GetDefaultCoAssigneesDeadline(changes.NewDeadline,
                                                                                             deadlineShiftInDays,
                                                                                             deadlineShiftInHours);
      }
      
      return newCoAssigneeDeadline;
    }
    
    /// <summary>
    /// Изменить контролера согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void ChangeSupervisor(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      if (changes.NewSupervisor != null)
      {
        // Галочка "На контроле" сбрасывает значение контролера, поэтому ее надо установить перед изменением этого поля.
        task.IsUnderControl = true;
        task.Supervisor = changes.NewSupervisor;
      }
    }
    
    /// <summary>
    /// Изменить исполнителя согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void ChangeAssignee(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      if (changes.NewAssignee != null)
        task.Assignee = changes.NewAssignee;
    }
    
    /// <summary>
    /// Изменить срок соисполнителя согласно изменениям.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void ChangeCoAssigneesDeadline(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      if (changes.CoAssigneesNewDeadline != null)
      {
        task.CoAssigneesDeadline = changes.CoAssigneesNewDeadline;
      }
    }
    
    /// <summary>
    /// Удалить старых соисполнителей из задачи на исполнение поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void DeleteCoAssignees(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      int assigneeNumber = 0;
      while (assigneeNumber < task.CoAssignees.Count())
      {
        var assignee = task.CoAssignees.ElementAt(assigneeNumber);
        if (!changes.NewCoAssignees.Contains(assignee.Assignee))
          task.CoAssignees.Remove(assignee);
        else
          assigneeNumber++;
      }
    }
    
    /// <summary>
    /// Добавить соисполнителей в задачу на исполнение поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void AddNewCoAssignees(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      if (changes.NewCoAssignees != null)
      {
        foreach (var assignee in changes.NewCoAssignees.Except(changes.OldCoAssignees))
        {
          var newAssignee = task.CoAssignees.AddNew();
          newAssignee.Assignee = assignee;
        }
      }
    }
    
    /// <summary>
    /// Изменить свойства задачи на исполнение поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="changes">Изменения в поручении.</param>
    [Obsolete("Разделена на 3 функции: UpdateCompoundActionItemTask, UpdateSimpleActionItemTask, UpdateActionItemPartTask")]
    public virtual void UpdateActionItemTask(IActionItemExecutionTask task, IActionItemChanges changes)
    {
      var isCompound = task.IsCompoundActionItem == true;
      var isComponent = task.ActionItemType == ActionItemType.Component;
      var isMassChange = changes.TaskIds != null && changes.TaskIds.Any();
      var taskInProcess = task.Status == Workflow.Task.Status.InProcess;
      var allPartsSelected = changes.TaskIds != null && changes.TaskIds.Count() == task.ActionItemParts.Count();
      var allPartsInProccess = _obj.ActionItemParts.All(p => p.ActionItemPartExecutionTask.Status == Workflow.Task.Status.InProcess);
      
      // Изменение контролёра или постановка на контроль в первый раз.
      if (changes.NewSupervisor != null)
      {
        if (!isCompound || isCompound && allPartsSelected && allPartsInProccess)
        {
          // Галочка "На контроле" сбрасывает значение контролера, поэтому ее надо установить перед изменением этого поля.
          task.IsUnderControl = true;
          task.Supervisor = changes.NewSupervisor;
        }
        
        // Массовая корректировка пунктов, обновление грида.
        if (isComponent && isMassChange && taskInProcess)
        {
          var actionItemPart = _obj.ActionItemParts.Where(x => x.ActionItemPartExecutionTask.Equals(task)).FirstOrDefault();
          actionItemPart.Supervisor = changes.NewSupervisor;
        }
      }
      
      // Изменение основного исполнителя.
      if (changes.NewAssignee != null)
        task.Assignee = changes.NewAssignee;
      
      // Изменение сроков простого поручения, контейнера и пункта составного.
      if (changes.NewDeadline != null)
      {
        // Изменение срока ответственного исполнителя.
        var oldTaskDeadline = task.Deadline;
        if (isCompound)
        {
          if (allPartsSelected && allPartsInProccess &&
              _obj.ActionItemParts.All(p => p.ActionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl))
          {
            task.FinalDeadline = changes.NewDeadline;
          }
        }
        else if (!isComponent || isComponent && task.ExecutionState != ExecutionState.OnControl)
        {
          task.Deadline = changes.NewDeadline;
        }
        
        // Изменение сроков при массовой корректировке.
        if (isComponent && isMassChange)
        {
          var actionItemPart = _obj.ActionItemParts.Where(x => Equals(x.ActionItemPartExecutionTask, task)).FirstOrDefault();
          
          // Срок соисполнителя (вычисляется по дельте).
          if (task.CoAssignees.Any())
          {
            var newCoAssigneeDeadline = Docflow.PublicFunctions.Module.GetNewCoAssigneeDeadline(oldTaskDeadline, task.CoAssigneesDeadline,
                                                                                                changes.NewDeadline, task.Assignee);
            task.CoAssigneesDeadline = newCoAssigneeDeadline;
            
            // Изменение срока соисполнителей в гриде (массовая корректировка).
            actionItemPart.CoAssigneesDeadline = newCoAssigneeDeadline;
          }
          
          // Срок исполнителя (в гриде) меняется, только когда пункт не на приемке.
          if (taskInProcess && actionItemPart.ActionItemPartExecutionTask.ExecutionState != ExecutionState.OnControl)
            actionItemPart.Deadline = changes.NewDeadline;
        }
      }
      
      // Изменение срока соисполнителей.
      if (changes.CoAssigneesNewDeadline != null)
        task.CoAssigneesDeadline = changes.CoAssigneesNewDeadline;
      
      // Удаление соисполнителей.
      if (!isMassChange)
      {
        int assigneeNumber = 0;
        while (assigneeNumber < task.CoAssignees.Count())
        {
          var assignee = task.CoAssignees.ElementAt(assigneeNumber);
          if (!changes.NewCoAssignees.Contains(assignee.Assignee))
            task.CoAssignees.Remove(assignee);
          else
            assigneeNumber++;
        }
      }
      
      // Добавление соисполнителей.
      if (changes.NewCoAssignees != null)
      {
        foreach (var assignee in changes.NewCoAssignees.Except(changes.OldCoAssignees))
        {
          var newAssignee = task.CoAssignees.AddNew();
          newAssignee.Assignee = assignee;
        }
      }
    }
    
    /// <summary>
    /// Прекратить подзадачи по удаленным соисполнителям.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <remarks>Прекращаются поручения, запросы отчетов, запросы продления сроков.</remarks>
    public virtual void AbortDeletedCoAssigneeTasks(IActionItemChanges changes)
    {
      foreach (var assignee in changes.OldCoAssignees.Except(changes.NewCoAssignees))
      {
        var task = ActionItemExecutionTasks.GetAll()
          .Where(t => t.ParentAssignment != null && Equals(_obj, t.ParentAssignment.Task) && Equals(assignee, t.Assignee))
          .Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess)
          .Where(t => t.ActionItemType == ActionItemType.Additional)
          .FirstOrDefault();
        
        if (task != null)
        {
          this.AbortDeadlineExtensionTasks(task);
          this.AbortReportRequestTasksToOldCoAssignee(task);
          task.Abort();
        }
      }
    }
    
    /// <summary>
    /// Прекратить подзадачи на запрос продления срока по указанному поручению.
    /// </summary>
    /// <param name="task">Поручение.</param>
    public virtual void AbortDeadlineExtensionTasks(IActionItemExecutionTask task)
    {
      var assignments = ActionItemExecutionAssignments.GetAll()
        .Where(j => Equals(j.Task, task))
        .Where(j => j.TaskStartId == _obj.StartId);
      
      foreach (var assignment in assignments)
      {
        var extendDeadlineTasks = Docflow.DeadlineExtensionTasks.GetAll()
          .Where(t => Equals(t.ParentAssignment, assignment) &&
                 (t.Status == Workflow.Task.Status.InProcess ||
                  t.Status == Workflow.Task.Status.Draft));
        foreach (var extendDeadlineTask in extendDeadlineTasks)
          extendDeadlineTask.Abort();
      }
    }
    
    /// <summary>
    /// Прекратить подзадачи на запрос отчета по поручению удаленному соисполнителю.
    /// </summary>
    /// <param name="task">Поручение соисполнителю.</param>
    public virtual void AbortReportRequestTasksToOldCoAssignee(IActionItemExecutionTask task)
    {
      // Прекратить запросы отчета, созданные из задачи соисполнителю.
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTask(task);

      // Прекратить запросы отчета, созданные из задания отв. исполнителя.
      var assignment = this.GetActionItemExecutionAssignment();
      Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromAssignmentToAssignee(task, assignment, task.Assignee);
    }
    
    /// <summary>
    /// Разослать уведомления об изменении поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SendActionItemChangeNotifications(IActionItemChanges changes, List<IUser> addressees)
    {
      if (!addressees.Any())
        return;
      
      var noticeSubject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ActionItemChanged);
      var activeText = this.GetActionItemChangeNotificationText(changes);
      Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticeSubject,
                                                                 addressees,
                                                                 _obj,
                                                                 activeText,
                                                                 changes.InitiatorOfChange,
                                                                 ActionItemExecutionTasks.Resources.ActionItemExecutionChangeNotification);
    }
    
    /// <summary>
    /// Получить список заинтересованных в изменении поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="oldExecutionAssignmentInProcess">Признак того, что старое задание на исполнение поручения находится в работе.</param>
    /// <returns>Список пользователей, кого необходимо уведомить.</returns>
    public virtual List<IUser> GetActionItemChangeNotificationAddressees(IActionItemChanges changes,
                                                                         bool oldExecutionAssignmentInProcess)
    {
      var addressees = new List<IUser>();

      var deadlineChanged = !Equals(changes.OldDeadline, changes.NewDeadline);
      var assigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee);
      var supervisorChanged = !Equals(changes.OldSupervisor, changes.NewSupervisor);
      var coAssigneesChanged = !changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees);
      var coAssigneesDeadlineChanged = !Equals(changes.CoAssigneesOldDeadline, changes.CoAssigneesNewDeadline);
      
      // Изменение срока поручения и срока соисполнителей.
      if (deadlineChanged || coAssigneesDeadlineChanged)
      {
        addressees.Add(changes.NewAssignee);
        addressees.Add(changes.NewSupervisor);
        foreach (var assignee in changes.NewCoAssignees)
          addressees.Add(assignee);
      }
      
      // Изменение исполнителя.
      if (assigneeChanged && oldExecutionAssignmentInProcess)
      {
        addressees.Add(changes.OldAssignee);
        addressees.Add(changes.NewSupervisor);
        foreach (var coAssignee in changes.NewCoAssignees)
        {
          // Отправляем только тем соисполнителям, у которых задание на исполнении или на доработке.
          var coAssigneeAssignmentInWork = ActionItemExecutionTasks.GetAll()
            .Where(t => t.ParentAssignment != null && Equals(_obj, t.ParentAssignment.Task) && Equals(coAssignee, t.Assignee))
            .Where(t => t.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
                   t.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework)
            .Any();
          if (coAssigneeAssignmentInWork)
            addressees.Add(coAssignee);
        }
      }
      
      // Изменение контролера.
      if (supervisorChanged)
      {
        addressees.Add(changes.OldSupervisor);
        if (oldExecutionAssignmentInProcess && changes.NewSupervisor != null)
          addressees.Add(changes.NewSupervisor);
        
        // Уведомить ответственного исполнителя, если он не менялся и еще не выполнил задание.
        if (!assigneeChanged && oldExecutionAssignmentInProcess)
          addressees.Add(changes.NewAssignee);
      }
      
      // Изменение соисполнителей.
      if (coAssigneesChanged)
      {
        // Уведомить ответственного исполнителя, если самому исполнителю не будет нового задания.
        if (!assigneeChanged)
          addressees.Add(changes.NewAssignee);
        
        // Уведомить удаленных соисполнителей (вновь добавленных не надо, так как им придет подзадача).
        var deletedCoAssignees = changes.OldCoAssignees.Except(changes.NewCoAssignees);
        foreach (var assignee in deletedCoAssignees)
          addressees.Add(assignee);
      }
      
      // Добавить автора на случай, если поручение меняет не он.
      // Если текущее простое поручение - это пункт составного, то автора берем из основной задачи,
      // т.к. в пункт поручения инициатор основной задачи не прописывается.
      var mainTask = _obj.ActionItemType == ActionItemType.Component
        ? ActionItemExecutionTasks.As(_obj.ParentTask)
        : _obj;
      
      var author = mainTask.StartedBy.IsSystem != true ? mainTask.StartedBy : mainTask.AssignedBy;
      if (!Equals(Users.Current, author))
        addressees.Add(author);
      
      // Устранить дублирование адресатов, в том числе убрать себя.
      addressees = addressees.Distinct().Where(a => a != null && a.IsSystem != true).ToList();
      if (addressees.Contains(Employees.Current))
        addressees.Remove(Employees.Current);
      
      return addressees;
    }
    
    /// <summary>
    /// Получить список заинтересованных в изменении составного поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <returns>Список пользователей, кого необходимо уведомить.</returns>
    public virtual List<IUser> GetCompoundActionItemChangeNotificationAddressees(IActionItemChanges changes)
    {
      var addressees = new List<IUser>();
      
      // Уведомление старому контролеру.
      addressees.Add(changes.OldSupervisor);
      
      // Уведомление новому контролеру.
      addressees.Add(changes.NewSupervisor);
      
      // Уведомления исполнителям пунктов поручения, которые не выполнили еще свое задание.
      var actionItemPartExecutionTasksIds = _obj.ActionItemParts
        .Select(p => p.ActionItemPartExecutionTask.Id)
        .Where(p => changes.TaskIds.Contains(p));
      var actionItemPartExecutionAssignments = ActionItemExecutionAssignments.GetAll().Where(x => actionItemPartExecutionTasksIds.Contains(x.Task.Id));
      var assignmentsPerformers = actionItemPartExecutionAssignments.Where(x => x.Status == RecordManagement.ActionItemExecutionAssignment.Status.InProcess)
        .Select(x => x.Performer);
      addressees.AddRange(assignmentsPerformers);
      
      // Уведомления соисполнителям пунктов поручения, которые не выполнили еще свое задание.
      var coAssigneesAssignments = ActionItemExecutionAssignments.GetAll().Where(x => actionItemPartExecutionAssignments.Contains(x.Task.ParentAssignment) &&
                                                                                 ActionItemExecutionTasks.As(x.Task).ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
      var assignmentsCoAssigneesPerformers = coAssigneesAssignments.Where(x => x.Status == RecordManagement.ActionItemExecutionAssignment.Status.InProcess)
        .Select(x => x.Performer);
      addressees.AddRange(assignmentsCoAssigneesPerformers);
      
      // Уведомить инд. контролеров пунктов поручения.
      if (changes.TaskIds.Count > 0)
      {
        var changedPartsSupervisors = _obj.ActionItemParts
          .Where(x => changes.TaskIds.Contains(x.ActionItemPartExecutionTask.Id))
          .Select(x => x.ActionItemPartExecutionTask.Supervisor);
        addressees.AddRange(changedPartsSupervisors);
      }
      
      // Добавить автора на случай, если поручение меняет не он.
      var author = _obj.StartedBy.IsSystem != true ? _obj.StartedBy : _obj.AssignedBy;
      if (!Equals(Users.Current, author))
        addressees.Add(author);
      
      // Устранить дублирование адресатов, в том числе убрать текущего пользователя и автора корректировки.
      addressees = addressees.Distinct().Where(a => a != null).ToList();
      if (addressees.Contains(Employees.Current))
        addressees.Remove(Employees.Current);
      if (addressees.Contains(changes.InitiatorOfChange))
        addressees.Remove(changes.InitiatorOfChange);
      
      return addressees;
    }
    
    /// <summary>
    /// Получить текст уведомления об изменении поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <returns>Текст изменений в поручении.</returns>
    public virtual string GetActionItemChangeNotificationText(IActionItemChanges changes)
    {
      var text = new List<string>();
      
      var deadlineChanged = !Equals(changes.OldDeadline, changes.NewDeadline);
      var assigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee);
      var supervisorChanged = !Equals(changes.OldSupervisor, changes.NewSupervisor);
      var coAssigneesChanged = changes.OldCoAssignees != null && !changes.OldCoAssignees.SequenceEqual(changes.NewCoAssignees);
      // Удалённые соисполнители.
      var deletedCoAssignees = changes.OldCoAssignees.Except(changes.NewCoAssignees);
      // Добавленные соисполнители.
      var addedCoAssignees = changes.NewCoAssignees.Except(changes.OldCoAssignees);
      var coAssigneeDeadlineChanged = !Equals(changes.CoAssigneesOldDeadline, changes.CoAssigneesNewDeadline);
      
      // Пункты, которые были изменены.
      if (!string.IsNullOrWhiteSpace(changes.ActionItemPartsText))
      {
        text.Add(changes.ActionItemPartsText + "\n");
      }
      
      // Изменение контролера.
      if (supervisorChanged)
      {
        if (changes.OldSupervisor != null && changes.NewSupervisor != null)
        {
          var oldSupervisorHyperlink = Hyperlinks.Get(changes.OldSupervisor);
          var newSupervisorHyperlink = Hyperlinks.Get(changes.NewSupervisor);
          text.Add(ActionItemExecutionTasks.Resources.SupervisorChangedFromToFormat(oldSupervisorHyperlink, newSupervisorHyperlink));
        }
        
        // Постановка на контроль.
        if (changes.OldSupervisor == null && changes.NewSupervisor != null)
        {
          var newSupervisorHyperlink = Hyperlinks.Get(changes.NewSupervisor);
          text.Add(ActionItemExecutionTasks.Resources.TaskPutUnderSupervisionFormat(newSupervisorHyperlink));
        }
      }
      
      // Изменение исполнителя.
      if (assigneeChanged)
      {
        var oldAssigneeHyperlink = Hyperlinks.Get(changes.OldAssignee);
        var newAssigneeHyperlink = Hyperlinks.Get(changes.NewAssignee);
        text.Add(ActionItemExecutionTasks.Resources.AssigneeChangedFromToFormat(oldAssigneeHyperlink, newAssigneeHyperlink));
      }
      
      // Изменение срока.
      if (deadlineChanged)
        text.Add(this.GenerateChangedDeadlineText(changes.OldDeadline, changes.NewDeadline, false));
      
      // Изменение соисполнителей.
      if (coAssigneesChanged)
      {
        if (deletedCoAssignees.Any())
        {
          var deletedCoAssigneesHyperlink = deletedCoAssignees
            .Select(a => Hyperlinks.Get(a));
          
          text.Add(ActionItemExecutionTasks.Resources.CoAssigneesDeletedFormat(string.Join("; ", deletedCoAssigneesHyperlink)));
        }
        
        if (addedCoAssignees.Any())
        {
          var addedCoAssigneesHyperlink = addedCoAssignees
            .Select(a => Hyperlinks.Get(a));
          text.Add(ActionItemExecutionTasks.Resources.CoAssigneesAddedFormat(string.Join("; ", addedCoAssigneesHyperlink)));
        }
      }
      
      // Изменение срока соисполнителей.
      if (coAssigneeDeadlineChanged && changes.CoAssigneesNewDeadline != null)
        text.Add(this.GenerateChangedDeadlineText(changes.CoAssigneesOldDeadline, changes.CoAssigneesNewDeadline, true));
      
      // Причина корректировки.
      if (!string.IsNullOrWhiteSpace(changes.EditingReason))
        text.Add(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.EditingReasonTextFormat(changes.EditingReason));
      
      if (assigneeChanged || deletedCoAssignees.Any() || deadlineChanged)
        text.Add(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.StopAssignmentSubtask);
      
      return string.Join(Environment.NewLine, text);
    }
    
    /// <summary>
    /// Сформировать текст об изменении срока.
    /// </summary>
    /// <param name="oldDeadline">Старый срок.</param>
    /// <param name="newDeadline">Новый срок.</param>
    /// <param name="isCoAssigneeDeadline">Формируется текст об изменении срока соисполнителей.</param>
    /// <returns>Текст.</returns>
    public virtual string GenerateChangedDeadlineText(DateTime? oldDeadline, DateTime? newDeadline, bool isCoAssigneeDeadline)
    {
      var utcOffset = Calendar.UtcOffset.TotalHours;
      var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
      
      var oldDeadlineHasTime = false;
      var newDeadlineHasTime = false;
      var oldDeadlineText = string.Empty;
      var newDeadlineText = string.Empty;
      
      if (oldDeadline.HasValue)
      {
        oldDeadlineHasTime = oldDeadline.Value.HasTime();
        oldDeadlineText = oldDeadlineHasTime
          ? oldDeadline.Value.ToString("dd.MM.yyyy H:mm")
          : oldDeadline.Value.ToString("dd.MM.yyyy");
      }
      
      if (newDeadline.HasValue)
      {
        newDeadlineHasTime = newDeadline.Value.HasTime();
        newDeadlineText = newDeadlineHasTime
          ? newDeadline.Value.ToString("dd.MM.yyyy H:mm")
          : newDeadline.Value.ToString("dd.MM.yyyy");
      }
      
      // Если и старый и новый сроки со временем, то часовой пояс выводить только один раз, после нового срока.
      if (newDeadlineHasTime)
        newDeadlineText = string.Format("{0:g} (UTC{1})", newDeadlineText, utcOffsetLabel);
      else
        if (oldDeadlineHasTime)
          oldDeadlineText = string.Format("{0:g} (UTC{1})", oldDeadlineText, utcOffsetLabel);
      
      if (isCoAssigneeDeadline)
      {
        if (oldDeadline.HasValue)
          return ActionItemExecutionTasks.Resources.CoAssigneesDeadlineChangedFormat(oldDeadlineText, newDeadlineText);
        else
          return ActionItemExecutionTasks.Resources.CoAssigneesDeadlineSetFormat(newDeadlineText);
      }
      else
      {
        if (oldDeadline.HasValue)
          return ActionItemExecutionTasks.Resources.DeadlineChangedFormat(oldDeadlineText, newDeadlineText);
        else
          return ActionItemExecutionTasks.Resources.AssigneeDeadlineSetFormat(newDeadlineText);
      }
    }
    
    /// <summary>
    /// Изменить срок в задании на исполнение поручения.
    /// </summary>
    /// <param name="deadline">Новый срок.</param>
    /// <param name="executionAssignment">Задание на исполнение поручения.</param>
    public virtual void ChangeExecutionAssignmentDeadline(DateTime? deadline,
                                                          IAssignment executionAssignment)
    {
      if (!Locks.GetLockInfo(executionAssignment).IsLocked)
      {
        executionAssignment.Deadline = deadline;
        executionAssignment.Save();
      }
    }
    
    /// <summary>
    /// Переадресовать подчиненные задания, у которых изменился исполнитель или контролер.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="oldExecutionAssignment">Старое задание на исполнение поручения.</param>
    /// <param name="oldSupervisorAssignment">Старое задание на контроль исполнения.</param>
    public virtual void ForwardChangedAssignments(IActionItemChanges changes,
                                                  IAssignment oldExecutionAssignment,
                                                  IAssignment oldSupervisorAssignment)
    {
      if (oldExecutionAssignment != null && changes.NewAssignee != null && !Equals(oldExecutionAssignment.Performer, changes.NewAssignee))
        this.ForwardAssignment(oldExecutionAssignment, changes.NewAssignee, changes.NewDeadline);
      
      if (oldSupervisorAssignment != null && changes.NewSupervisor != null && !Equals(oldSupervisorAssignment.Performer, changes.NewSupervisor))
        this.ForwardAssignment(oldSupervisorAssignment, changes.NewSupervisor);
    }
    
    /// <summary>
    /// Переадресовать задание новому исполнителю и попытаться прекратить задание старому.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="performer">Новый исполнитель.</param>
    /// <remarks>Если "старое задание" заблокировано, то будет выполнена только переадресация,
    /// а прекращение будет в рамках схемы подзадачи соисполнителю.</remarks>
    public virtual void ForwardAssignment(IAssignment assignment, IUser performer)
    {
      assignment.Forward(performer);
      if (!Locks.GetLockInfo(assignment).IsLocked)
        assignment.Abort();
    }
    
    /// <summary>
    /// Переадресовать задание новому исполнителю и попытаться прекратить задание старому.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="performer">Новый исполнитель.</param>
    /// <param name="deadline">Новый срок.</param>
    /// <remarks>Если "старое задание" заблокировано, то будет выполнена только переадресация,
    /// а прекращение будет в рамках схемы подзадачи соисполнителю.</remarks>
    public virtual void ForwardAssignment(IAssignment assignment, IUser performer, DateTime? deadline)
    {
      assignment.Forward(performer, deadline);
      if (!Locks.GetLockInfo(assignment).IsLocked)
        assignment.Abort();
    }
    
    /// <summary>
    /// Скорректировать дерево поручений соисполнителям в соответствии с новым ответственным исполнителем или сроком.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    public virtual void ChangeCoAssigneesActionItemsTree(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes)
    {
      var newExecutionAssignment = this.GetActionItemExecutionAssignment();
      var allAssignments = this.GetActionItemExecutionAssignments();
      var coAssigneeTasks = new List<IActionItemExecutionTask>();
      foreach (var assignment in allAssignments)
        coAssigneeTasks.AddRange(this.GetCoAssigneeActionItemExecutionTasks(assignment));
      
      foreach (var task in coAssigneeTasks)
      {
        // При массовой корректировке данная переменная всегда равна false, так как исполнитель не меняется
        // (в структуре это обозначено тем, что и OldAssignee и NewAssignee будут равны null).
        var parentActionItemAssigneeChanged = !Equals(changes.OldAssignee, changes.NewAssignee) && !Equals(task.Supervisor, _obj.Assignee);
        bool coAssigneeDeadlineChanged;
        if (changes.ChangeContext == Constants.ActionItemExecutionTask.ChangeContext.Compound)
          coAssigneeDeadlineChanged = changes.NewDeadline != null && changes.OldDeadline != changes.NewDeadline && task.Deadline != _obj.CoAssigneesDeadline;
        else
          coAssigneeDeadlineChanged = changes.CoAssigneesNewDeadline != null && changes.CoAssigneesOldDeadline != changes.CoAssigneesNewDeadline && task.Deadline != _obj.CoAssigneesDeadline;

        var coAssigneeNewDeadline = _obj.CoAssigneesDeadline;
        var taskInProcess = task.Status == Workflow.Task.Status.InProcess;
        
        if (!parentActionItemAssigneeChanged && !coAssigneeDeadlineChanged)
          continue;
        
        // Найти задание контролёру до смены контролера в задаче.
        var supervisorAssignment = Functions.ActionItemExecutionTask.GetActualActionItemSupervisorAssignment(task);
        
        // Перецепить все подпоручения соисполнителям от старого задания исполнителю к новому заданию.
        if (parentActionItemAssigneeChanged)
          ((IInternalTask)task).ParentAssignment = newExecutionAssignment;
        
        // Сменить контролера в подпоручении соисполнителю.
        if (parentActionItemAssigneeChanged && taskInProcess)
        {
          task.Supervisor = _obj.Assignee;
          task.AssignedBy = _obj.Assignee;
        }
        
        if (coAssigneeDeadlineChanged && taskInProcess)
        {
          // Смена срока в задании на исполнение соисполнителя.
          var executionAssignment = Functions.ActionItemExecutionTask.GetActualActionItemExecutionAssignment(task);
          if (executionAssignment != null && !Locks.GetLockInfo(executionAssignment).IsLocked)
          {
            executionAssignment.Deadline = coAssigneeNewDeadline;
            executionAssignment.Save();
          }
          
          // Прекращение задач на продление срока.
          this.AbortDeadlineExtensionTasks(task);
          
          // Прокидывание срока соисполнителей в подпоручение соисполнителю.
          task.HasIndefiniteDeadline = false;
          task.Deadline = coAssigneeNewDeadline;
          
          Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(task, true, true);
        }
        
        // Установить режим корректировки и сохранить.
        if (taskInProcess)
          task.OnEditGuid = Guid.NewGuid().ToString();
        
        task.Save();
        
        if (!taskInProcess)
          continue;
        
        // Если корректируется только срок, то актуализировать свойство "Плановый срок" в активном задании на приемку.
        if (!parentActionItemAssigneeChanged && coAssigneeDeadlineChanged &&
            supervisorAssignment != null && !Locks.GetLockInfo(supervisorAssignment).IsLocked)
        {
          supervisorAssignment.ScheduledDate = changes.CoAssigneesNewDeadline;
          supervisorAssignment.Save();
        }
        
        // Рестартовать запросы продления срока для подзадачи соисполнителю, если срок не был скорректирован "сверху".
        if (parentActionItemAssigneeChanged && !coAssigneeDeadlineChanged)
          Functions.ActionItemExecutionTask.RestartDeadlineExtensionTasks(task, _obj.Assignee);

        // Если в подзадаче соисполнителю есть задание на приемку, то его надо переадресовать.
        if (parentActionItemAssigneeChanged && supervisorAssignment != null)
          this.ForwardAssignment(supervisorAssignment, task.Supervisor);
        
        // Запустить асинхронный обработчик для отработки следующих ситуаций:
        // - чтобы выдать права контролеру при его изменении, если поручение в этот момент находилось на исполнении;
        // - чтобы прекратить старые задания, которые были заблочены при переадресации и не смогли вовремя прекратиться.
        Functions.Module.ExecuteApplyActionItemLockIndependentChanges(changes, task.Id, task.OnEditGuid);
      }
    }
    
    /// <summary>
    /// Сменить контролера в поручении соисполнителю и перецепить поручение к новому родительскому заданию.
    /// </summary>
    /// <param name="newParentAssignment">Новое родительское задание.</param>
    /// <param name="newSupervisor">Новый контролер.</param>
    [Obsolete("При рефакторинге отказались от этого метода.")]
    public virtual void ChangeCoAssigneeActionItemExecutionTaskSupervisor(IAssignment newParentAssignment, IEmployee newSupervisor)
    {
      ((IInternalTask)_obj).ParentAssignment = newParentAssignment;
      if (_obj.Status == Sungero.Workflow.Task.Status.InProcess)
      {
        _obj.Supervisor = newSupervisor;
        _obj.AssignedBy = newSupervisor;
      }
    }
    
    /// <summary>
    /// Прекратить задание старому исполнителю (при переадресации).
    /// </summary>
    public virtual void AbortActionItemExecutionAssignment()
    {
      var newExecutionAssignment = this.GetActionItemExecutionAssignment();
      var parentActionItemAssigneeChanged = newExecutionAssignment != null && newExecutionAssignment.ForwardedFrom != null;
      if (parentActionItemAssigneeChanged &&
          newExecutionAssignment.ForwardedFrom.Status == Sungero.Workflow.Assignment.Status.InProcess)
        newExecutionAssignment.ForwardedFrom.Abort();
    }
    
    /// <summary>
    /// Прекратить задание старому контролеру (при переадресации).
    /// </summary>
    /// <remarks>Для ручных поручений такое задание будет всегда максимум одно
    /// (так как корректировать поручение до прекращения задания контролеру
    /// и последующего сброса в false признака OnEdit нельзя).
    /// А вот для автоматических поручений соисполнителям из-за того,
    /// что в головной задаче могут скорректировать отв. исполнителя
    /// (который одновременно является и контролером в подпоручениях соисполнителей),
    /// может возникнуть ситуация частой смены контролера, а значит, переадресации заданий на приемку.
    /// Если при этом какое-либо прекращаемое задание будет заблокировано на момент переадресации,
    /// то при следующей переадресации будет уже 2 или более "старых" заданий, которые нужно прекращать.</remarks>
    public virtual void AbortActionItemSupervisorAssignments()
    {
      var assignmentsInProcess = this.GetActionItemSupervisorAssignmentsInProcess();
      if (assignmentsInProcess.Any())
      {
        var actualSupervisorAssignment = this.GetActualActionItemSupervisorAssignment();
        if (actualSupervisorAssignment != null)
          assignmentsInProcess.Remove(actualSupervisorAssignment);
        
        foreach (var assignment in assignmentsInProcess)
          assignment.Abort();
      }
    }
    
    /// <summary>
    /// Прекратить указанные задачи на запрос отчета по поручению.
    /// </summary>
    /// <param name="reportRequestTasks">Список задач.</param>
    public virtual void AbortReportRequestTasks(IQueryable<IStatusReportRequestTask> reportRequestTasks)
    {
      foreach (var reportRequestTask in reportRequestTasks)
        reportRequestTask.Abort();
    }
    
    /// <summary>
    /// Прекратить неактуальные запросы отчетов.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <param name="oldExecutionAssignment">Старое задание на исполнение поручения.</param>
    public virtual void AbortReportRequestTasks(IActionItemChanges changes,
                                                IAssignment oldExecutionAssignment)
    {
      if (oldExecutionAssignment != null && changes.NewAssignee != null && !Equals(oldExecutionAssignment.Performer, changes.NewAssignee))
      {
        // Если это простое поручение, прекратить запросы отчета от старого исполнителя.
        // Запросы отчета к исполнителю, направленные непосредственно из задачи, прекратятся в схеме, блок 4 "Выполнение задания".
        this.AbortReportRequestTasksFromOldAssignee(ActionItemExecutionAssignments.As(oldExecutionAssignment), changes);
        
        // Если это подчиненное простое поручение, прекратить запросы отчета к старому исполнителю из родительского задания.
        if (_obj.ParentAssignment != null && ActionItemExecutionAssignments.Is(_obj.ParentAssignment))
          this.AbortReportRequestTasksToSubActionItemAssignee(changes);
      }
      
      // Если это пункт составного поручения, прекратить запросы отчета к старому исполнителю.
      if (oldExecutionAssignment != null && _obj.ActionItemType == ActionItemType.Component && !Equals(oldExecutionAssignment.Performer, changes.NewAssignee))
        this.AbortReportRequestTasksToOldActionItemPartAssignee(changes);
      
      // Прекратить запросы отчета от старого контролера.
      if (changes.OldSupervisor != null && !Equals(changes.OldSupervisor, changes.NewSupervisor))
        this.AbortReportRequestTasksFromOldSupervisor(changes.OldSupervisor);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению от старого ответственного исполнителя.
    /// </summary>
    /// <param name="oldExecutionAssignment">Старое задание ответственному исполнителю.</param>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void AbortReportRequestTasksFromOldAssignee(IActionItemExecutionAssignment oldExecutionAssignment,
                                                               IActionItemChanges changes)
    {
      // Прекратить все запросы отчета, созданные ответственным исполнителем из задания.
      this.AbortReportRequestTasksCreatedFromAssignmentByAuthor(oldExecutionAssignment, changes.OldAssignee);
      
      // Прекратить все запросы отчета, созданные ответственным исполнителем из подзадач соисполнителям.
      foreach (var coAssignee in changes.OldCoAssignees)
      {
        var task = ActionItemExecutionTasks.GetAll()
          .Where(t => Equals(t.ParentAssignment, oldExecutionAssignment) && Equals(t.Assignee, coAssignee) && Equals(t.Author, changes.OldAssignee))
          .Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess)
          .Where(t => t.ActionItemType == ActionItemType.Additional)
          .FirstOrDefault();
        
        if (task != null)
          this.AbortReportRequestTasksCreatedFromTaskByAuthor(task, changes.OldAssignee);
      }
    }
    
    /// <summary>
    /// Прекратить запросы отчета, направленные старому исполнителю пункта составного поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void AbortReportRequestTasksToOldActionItemPartAssignee(IActionItemChanges changes)
    {
      var mainTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      if (mainTask == null || mainTask.IsCompoundActionItem != true)
        return;
      
      this.AbortReportRequestTasksCreatedFromTaskToAssignee(mainTask, changes.OldAssignee);
    }
    
    /// <summary>
    /// Прекратить запросы отчета, направленные старому исполнителю простого подчиненного поручения.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    public virtual void AbortReportRequestTasksToSubActionItemAssignee(IActionItemChanges changes)
    {
      this.AbortReportRequestTasksCreatedFromAssignmentToAssignee(ActionItemExecutionAssignments.As(_obj.ParentAssignment),
                                                                  changes.OldAssignee);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению от старого контролера простого поручения или пункта составного.
    /// </summary>
    /// <param name="oldSupervisor">Старый контролер.</param>
    public virtual void AbortReportRequestTasksFromOldSupervisor(IEmployee oldSupervisor)
    {
      // Прекратить все запросы отчета, созданные старым контролером из головной задачи простого поручения либо пункта составного.
      this.AbortReportRequestTasksCreatedFromTaskByAuthor(_obj, oldSupervisor);
      
      // Прекратить неактуальные запросы отчета, созданные старым контролером из головной задачи составного поручения.
      if (_obj.ActionItemType == ActionItemType.Component)
        Functions.ActionItemExecutionTask.AbortReportRequestTasksFromOldCompoundActionItemSupervisors(ActionItemExecutionTasks.As(_obj.ParentTask), new List<IEmployee> { oldSupervisor });
    }
    
    /// <summary>
    /// Прекратить неактуальные запросы отчета от предыдущих контролеров в составном поручении.
    /// </summary>
    /// <param name="oldSupervisors">Предыдущие контролеры.</param>
    public virtual void AbortReportRequestTasksFromOldCompoundActionItemSupervisors(List<IEmployee> oldSupervisors)
    {
      var oldSupervisorsIds = oldSupervisors.Distinct().Select(x => x.Id);
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(x => Equals(x.ParentTask, _obj) &&
               oldSupervisorsIds.Contains(x.Author.Id) &&
               x.Status == Workflow.Task.Status.InProcess);
      
      foreach (var reportRequestTask in reportRequestTasks)
      {
        if (!ActionItemExecutionTasks.GetAll().Any(t => Equals(t.Supervisor, reportRequestTask.Author)
                                                   && Equals(t.Assignee, reportRequestTask.Assignee)
                                                   && t.Status == Workflow.Task.Status.InProcess))
          reportRequestTask.Abort();
      }
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению, созданные из текущей задачи на исполнение поручения.
    /// </summary>
    public virtual void AbortReportRequestTasksCreatedFromTask()
    {
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(t => Equals(t.ParentTask, _obj) &&
               t.Status == Workflow.Task.Status.InProcess);
      
      this.AbortReportRequestTasks(reportRequestTasks);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению, созданные из задачи на исполнение поручения и стартованные от заданного автора.
    /// </summary>
    /// <param name="task">Поручение, из которого созданы запросы отчета.</param>
    /// <param name="author">Автор запроса.</param>
    public virtual void AbortReportRequestTasksCreatedFromTaskByAuthor(IActionItemExecutionTask task,
                                                                       IEmployee author)
    {
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(t => Equals(t.ParentTask, task) &&
               Equals(t.Author, author) &&
               t.Status == Workflow.Task.Status.InProcess);
      
      this.AbortReportRequestTasks(reportRequestTasks);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению, созданные из родительского задания и стартованные от заданного автора.
    /// </summary>
    /// <param name="assignment">Родительское задание.</param>
    /// <param name="author">Автор запроса.</param>
    public virtual void AbortReportRequestTasksCreatedFromAssignmentByAuthor(IActionItemExecutionAssignment assignment,
                                                                             IEmployee author)
    {
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(t => Equals(t.ParentAssignment, assignment) &&
               Equals(t.Author, author) &&
               t.Status == Workflow.Task.Status.InProcess);
      
      this.AbortReportRequestTasks(reportRequestTasks);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению, созданные из задачи на исполнение поручения и стартованные заданному исполнителю.
    /// </summary>
    /// <param name="task">Поручение, из которого созданы запросы отчета.</param>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void AbortReportRequestTasksCreatedFromTaskToAssignee(IActionItemExecutionTask task,
                                                                         IEmployee assignee)
    {
      // Убедиться, что не осталось других поручений в работе с тем же исполнителем. Если остались такие поручения - запросы не прекращаем.
      if (task == null || this.GetOtherActionItemExecutionTasksWithSameAssignee(task, assignee).Any())
        return;
      
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(t => Equals(t.ParentTask, task) &&
               Equals(t.Assignee, assignee) &&
               t.Status == Workflow.Task.Status.InProcess);
      
      this.AbortReportRequestTasks(reportRequestTasks);
    }
    
    /// <summary>
    /// Прекратить запросы отчета по поручению, созданные из родительского задания и стартованные заданному исполнителю.
    /// </summary>
    /// <param name="assignment">Родительское задание.</param>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void AbortReportRequestTasksCreatedFromAssignmentToAssignee(IActionItemExecutionAssignment assignment,
                                                                               IEmployee assignee)
    {
      if (assignment == null || this.GetOtherActionItemExecutionTasksWithSameAssignee(assignment, assignee).Any())
        return;
      
      var reportRequestTasks = StatusReportRequestTasks.GetAll()
        .Where(t => Equals(t.ParentAssignment, assignment) &&
               Equals(t.Assignee, assignee) &&
               t.Status == Workflow.Task.Status.InProcess);
      
      this.AbortReportRequestTasks(reportRequestTasks);
    }
    
    /// <summary>
    /// Получить другие поручения с тем же исполнителем.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <returns>Другие поручения с тем же исполнителем.</returns>
    private IQueryable<IActionItemExecutionTask> GetOtherActionItemExecutionTasksWithSameAssignee(IActionItemExecutionTask task, IEmployee assignee)
    {
      return ActionItemExecutionTasks.GetAll(t => Equals(t.Assignee, assignee) &&
                                             Equals(t.ParentTask, task) &&
                                             t.Id != _obj.Id &&
                                             t.Status == Workflow.Task.Status.InProcess);
    }
    
    /// <summary>
    /// Получить другие поручения с тем же исполнителем.
    /// </summary>
    /// <param name="assignment">Задание на исполнение поручения.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <returns>Другие поручения с тем же исполнителем.</returns>
    private IQueryable<IActionItemExecutionTask> GetOtherActionItemExecutionTasksWithSameAssignee(IActionItemExecutionAssignment assignment, IEmployee assignee)
    {
      return ActionItemExecutionTasks.GetAll(t => Equals(t.Assignee, assignee) &&
                                             Equals(t.ParentAssignment, assignment) &&
                                             t.Id != _obj.Id &&
                                             t.Status == Workflow.Task.Status.InProcess);
    }
    
    /// <summary>
    /// Рестартовать подзадачи на запрос продления срока.
    /// </summary>
    /// <param name="actualSupervisor">Актуальный контролер, у которого запрашиваем новое продление.</param>
    public virtual void RestartDeadlineExtensionTasks(IEmployee actualSupervisor)
    {
      // Ищем задания, из которых запрашивали продление.
      var assignments = ActionItemExecutionAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => j.TaskStartId == _obj.StartId)
        .Where(j => j.Performer == _obj.Assignee);
      
      foreach (var assignment in assignments)
      {
        var deadlineExtensionTasks = Docflow.DeadlineExtensionTasks.GetAll()
          .Where(t => Equals(t.ParentAssignment, assignment) &&
                 t.Status == Workflow.Task.Status.InProcess);
        foreach (var deadlineExtensionTask in deadlineExtensionTasks)
        {
          if (deadlineExtensionTask.Assignee.Equals(actualSupervisor))
            continue;
          deadlineExtensionTask.Abort();
          var newDeadlineExtensionTask = Docflow.DeadlineExtensionTasks.CreateAsSubtask(assignment);
          newDeadlineExtensionTask.Assignee = actualSupervisor;
          newDeadlineExtensionTask.NewDeadline = deadlineExtensionTask.NewDeadline;
          newDeadlineExtensionTask.ActiveText = deadlineExtensionTask.ActiveText;
          newDeadlineExtensionTask.Author = deadlineExtensionTask.Author;
          newDeadlineExtensionTask.Subject = deadlineExtensionTask.Subject;
          newDeadlineExtensionTask.Save();
          Workflow.SpecialFolders.GetOutbox(newDeadlineExtensionTask.Author).Items.Add(newDeadlineExtensionTask);
          newDeadlineExtensionTask.Start();
        }
      }
    }
    
    #endregion
    
    #region Корректировка
    
    /// <summary>
    /// Мониторинг создания задания исполнителю или контролёру.
    /// </summary>
    /// <returns>True - если задания созданы, поручение полностью завершено или составное поручение.</returns>
    public virtual bool AssignmentsCreated()
    {
      // В составном не бывает заданий исполнителю/контролёру.
      if (_obj.IsCompoundActionItem ?? false)
        return true;
      
      // Задание исполнителю.
      var executionAssignment = this.GetActualActionItemExecutionAssignment();
      if (executionAssignment != null)
        return true;
      
      // Задание контролёру (после корректировки).
      var supervisorAssignment = this.GetActualActionItemSupervisorAssignment();
      if (supervisorAssignment != null)
        return true;
      
      // Поручение завершено полностью.
      if (_obj.ExecutionState == ExecutionState.Executed)
        return true;
      
      // Переповтор.
      return false;
    }
    
    /// <summary>
    /// Создать задачу на исполнение поручения.
    /// </summary>
    public virtual void CreateActionItemExecutionTask()
    {
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.TaskSubject);
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      
      Functions.ActionItemExecutionTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      // Задания соисполнителям.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        var performer = _obj.CoAssignees.FirstOrDefault(ca => ca.AssignmentCreated != true);
        
        var parentAssignment = Functions.ActionItemExecutionTask.GetActionItemExecutionAssignment(_obj);
        
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Additional;
        
        // Синхронизировать вложения.
        Functions.Module.SynchronizeAttachmentsToActionItem(_obj, actionItemExecution);
        
        // Задать текст.
        actionItemExecution.Texts.Last().IsAutoGenerated = true;
        
        // Задать поручение.
        actionItemExecution.ActionItem = _obj.ActionItem;
        
        // Задать тему.
        actionItemExecution.Subject = subject;
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = performer.Assignee;
        actionItemExecution.IsUnderControl = true;
        actionItemExecution.Supervisor = _obj.Assignee;
        actionItemExecution.AssignedBy = _obj.Assignee;
        
        // Задать срок.
        actionItemExecution.Deadline = _obj.CoAssigneesDeadline ?? _obj.Deadline;
        actionItemExecution.MaxDeadline = _obj.CoAssigneesDeadline ?? _obj.Deadline;
        actionItemExecution.HasIndefiniteDeadline = _obj.HasIndefiniteDeadline == true && actionItemExecution.Deadline == null;
        
        actionItemExecution.Start();
        
        performer.AssignmentCreated = true;
        _obj.Save();
      }
      
      // Составное поручение.
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        var job = _obj.ActionItemParts.FirstOrDefault(aip => aip.AssignmentCreated != true);
        
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(_obj);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Component;
        
        // Синхронизировать вложения.
        Functions.Module.SynchronizeAttachmentsToActionItem(_obj, actionItemExecution);
        
        // Задать поручение и текст.
        actionItemExecution.ActiveText = string.IsNullOrWhiteSpace(job.ActionItemPart) ? _obj.ActiveText : job.ActionItemPart;
        
        // Задать тему.
        actionItemExecution.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, ActionItemExecutionTasks.Resources.TaskSubject);
        actionItemExecution.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemWithNumberThreadSubject;
        
        // Задать соисполнителей.
        foreach (var coAssignee in Functions.ActionItemExecutionTask.GetPartCoAssignees(_obj, job.PartGuid))
          actionItemExecution.CoAssignees.AddNew().Assignee = coAssignee;
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = job.Assignee;
        actionItemExecution.HasIndefiniteDeadline = _obj.HasIndefiniteDeadline == true;
        actionItemExecution.IsUnderControl = _obj.IsUnderControl;
        actionItemExecution.Supervisor = job.Supervisor ?? _obj.Supervisor;
        actionItemExecution.Author = _obj.Author;
        actionItemExecution.AssignedBy = _obj.AssignedBy;

        // Задать срок.
        var actionItemDeadline = job.Deadline.HasValue ? job.Deadline : _obj.FinalDeadline;
        actionItemExecution.Deadline = actionItemDeadline;
        actionItemExecution.MaxDeadline = actionItemDeadline;
        
        // Задать срок соисполнителям.
        if (job.CoAssigneesDeadline.HasValue)
          actionItemExecution.CoAssigneesDeadline = job.CoAssigneesDeadline;
        
        actionItemExecution.Start();
        
        // Добавить составные подзадачи в исходящее.
        if (actionItemExecution.Status == Sungero.Workflow.Task.Status.InProcess)
          Sungero.Workflow.SpecialFolders.GetOutbox(_obj.StartedBy).Items.Add(actionItemExecution);
        
        // Записать ссылку на поручение в составное поручение.
        job.ActionItemPartExecutionTask = actionItemExecution;
        
        job.AssignmentCreated = true;
        _obj.Save();
      }
    }
    
    /// <summary>
    /// Скорректировать поручение.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    public virtual void ApplyActionItemLockIndependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes)
    {
      // Выполнить действия корректировки поручения,
      // не связанные с ожиданием разблокировки заданий текущего поручения пользователями.
      if (string.IsNullOrEmpty(_obj.OnEditGuid))
        return;

      // Если скорректирован пункт составного поручения отдельно от основного,
      // синхронизировать изменения в грид основного составного поручения.
      if (_obj.ActionItemType == ActionItemType.Component && (ActionItemExecutionTasks.As(_obj.ParentTask).OnEditGuid ?? string.Empty) == string.Empty)
        Functions.ActionItemExecutionTask.SynchronizeActionItemPart(_obj, true);
      
      if (_obj.Supervisor != null)
      {
        // Рестарт запроса продления срока от исполнителя контролеру.
        Functions.ActionItemExecutionTask.RestartDeadlineExtensionTasks(_obj, _obj.Supervisor);
        
        // Если задача не на приемке и при этом есть контролер, то выдать ему права на вложения и задачи.
        if (_obj.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
            _obj.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework ||
            _obj.ExecutionState == null && _obj.IsCompoundActionItem == true)
        {
          // Выдать права на вложенные документы.
          Functions.ActionItemExecutionTask.GrantAccessRightsToAttachmentsWithSave(_obj, _obj.ResultGroup.All.ToList(), false);
          Functions.ActionItemExecutionTask.GrantAccessRightsToAttachmentsWithSave(_obj, _obj.DocumentsGroup.All.ToList(), false);
          Functions.ActionItemExecutionTask.GrantAccessRightsToAttachmentsWithSave(_obj, _obj.AddendaGroup.All.ToList(), false);
          
          // Выдать права на изменение задачи для возможности ее прекращения.
          Functions.ActionItemExecutionTask.GrantAccessRightToTaskWithSave(_obj, _obj);
        }
      }
      
      // Скорректировать дерево поручений соисполнителей в соответствии с новым ответственным исполнителем или сроком.
      Functions.ActionItemExecutionTask.ChangeCoAssigneesActionItemsTree(_obj, changes);
      
      // Обновить статус исполнения документа.
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
      // Сохранить документ явно, т.к. в асинхронном обработчике нет автоматического сохранения всех изменений.
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null && document.State.IsChanged)
        document.Save();
    }
    
    /// <summary>
    /// Завершить корректировку.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    public virtual void ApplyActionItemLockDependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes)
    {
      // Выполнить завершающие действия корректировки,
      // связанные с ожиданием разблокировки заданий текущего поручения пользователями.
      if (string.IsNullOrEmpty(_obj.OnEditGuid))
        return;
      
      // Выдать права на изменение задания на исполнение для возможности корректировки исполнителя контролером.
      // Делаем, если состояние поручения На исполнении или На доработке и есть задание на исполнение.
      // Не нужно делать для главного составного поручения.
      // Задание исполнителя может быть заблокировано, поэтому делаем в этом блоке.
      var executionAssignment = Functions.ActionItemExecutionTask.GetActualActionItemExecutionAssignment(_obj);
      if (_obj.IsCompoundActionItem != true &&
          (_obj.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
           _obj.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework) &&
          executionAssignment != null && _obj.Supervisor != null)
        Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(executionAssignment, _obj);
      
      // Актуализировать срок в задании на исполнение,
      // если в момент корректировки срока в поручении задание было заблокировано.
      if (executionAssignment != null && _obj.Deadline != executionAssignment.Deadline)
      {
        executionAssignment.Deadline = _obj.Deadline;
        executionAssignment.Save();
      }
      
      // Актуализировать срок в задании на приемку,
      // если в момент корректировки срока в поручении задание было заблокировано.
      var supervisorAssignment = Functions.ActionItemExecutionTask.GetActualActionItemSupervisorAssignment(_obj);
      if (supervisorAssignment != null && _obj.Deadline != supervisorAssignment.ScheduledDate)
      {
        supervisorAssignment.ScheduledDate = _obj.Deadline;
        supervisorAssignment.Save();
      }
      
      // Прекратить задание старому исполнителю,
      // если в момент корректировки исполнителя в поручении задание было заблокировано.
      Functions.ActionItemExecutionTask.AbortActionItemExecutionAssignment(_obj);
      
      // Прекратить задание старому контролеру,
      // если в момент корректировки контролера в поручении задание было заблокировано.
      Functions.ActionItemExecutionTask.AbortActionItemSupervisorAssignments(_obj);
      
      _obj.OnEditGuid = string.Empty;
      _obj.Save();
      Logger.DebugFormat("ApplyActionItemLockDependentChanges: done clean OnEditGuid for action item execution task with id {0}.", _obj.Id);
    }
    
    /// <summary>
    /// Задания соисполнителям созданы.
    /// </summary>
    /// <returns>True - если задания созданы по всем соисполнителям, иначе - False.</returns>
    public virtual bool AreAssignmentsCreated()
    {
      if (_obj.CoAssignees == null && _obj.ActionItemParts == null)
        return true;
      
      return !_obj.CoAssignees.Any(ca => ca.AssignmentCreated != true) &&
        !_obj.ActionItemParts.Any(aip => aip.AssignmentCreated != true);
    }
    
    #endregion
    
    /// <summary>
    /// Установить статусы в документе из поручения.
    /// </summary>
    public virtual void SetDocumentStates()
    {
      Logger.Debug("ActionItemExecutionTask({0}). SetDocumentStates start");
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document == null || !document.AccessRights.CanUpdate())
        return;

      var tasksWithDocument = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetCreatedActionItems(document).ToList();
      var firstLevelTasks = Sungero.Docflow.PublicFunctions.OfficialDocument.GetFirstLevelActionItems(document);

      Enumeration? executionState = Docflow.OfficialDocument.ExecutionState.WithoutExecut;
      Enumeration? controlExecutionState = Docflow.OfficialDocument.ControlExecutionState.WithoutControl;
      
      var inProcess = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.OnExecution ||
                                            t.ExecutionState == ExecutionState.OnRework ||
                                            t.ExecutionState == ExecutionState.OnControl)
        .ToList();
      
      // Добавить составные поручения, если хотя бы один пункт поручения в процессе исполнения.
      var compoundTasks = firstLevelTasks.Where(i => i.IsCompoundActionItem.Value == true);
      inProcess.AddRange(compoundTasks.Where(t => t.ActionItemParts.Any(i => i.ActionItemPartExecutionTask == null ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnExecution ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnRework ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnControl)));
      
      if (inProcess.Any())
      {
        executionState = Docflow.OfficialDocument.ExecutionState.OnExecution;

        if (inProcess.Any(t => t.IsUnderControl == true))
        {
          controlExecutionState = inProcess.Any(t => t.IsUnderControl == true &&
                                                t.Importance == Sungero.RecordManagement.ActionItemExecutionTask.Importance.High)
            ? Docflow.OfficialDocument.ControlExecutionState.SpecialControl
            : Docflow.OfficialDocument.ControlExecutionState.OnControl;
        }
      }
      else
      {
        var executeTasks = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.Executed).ToList();
        /* compoundTasks.Cast<ITask>() нужен для того, чтобы не возникало ошибок в приведении типов
         * между t.ParentTask (ITask) и compoundTasks (IActionItemExecutionTask).
         */
        executeTasks.AddRange(tasksWithDocument.Where(t => compoundTasks.Cast<ITask>().Contains(t.ParentTask) &&
                                                      t.ExecutionState == ExecutionState.Executed));
        
        if (executeTasks.Any())
        {
          executionState = Docflow.OfficialDocument.ExecutionState.Executed;
          if (executeTasks.Any(t => t.IsUnderControl == true))
            controlExecutionState = Docflow.OfficialDocument.ControlExecutionState.ControlRemoved;
        }
      }
      
      if (firstLevelTasks.All(t => t.ExecutionState == ExecutionState.Aborted) && firstLevelTasks.Count > 0)
      {
        executionState = Docflow.OfficialDocument.ExecutionState.Aborted;
        controlExecutionState = null;
      }
      
      Logger.DebugFormat("ActionItemExecutionTask({0}). SetDocumentStates. New document execution state: {1}.", _obj.Id, executionState);
      Functions.Module.SetDocumentExecutionState(_obj, document, executionState);
      
      if (document.ExecutionState != ExecutionState.OnExecution &&
          document.ExecutionState != ExecutionState.Executed)
        controlExecutionState = null;
      
      Logger.DebugFormat("ActionItemExecutionTask({0}). SetDocumentStates. New document control execution state: {1}.", _obj.Id, controlExecutionState);
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetControlExecutionState(document, controlExecutionState);
      Logger.Debug("ActionItemExecutionTask({0}). SetDocumentStates end");
    }
    
    /// <summary>
    /// Получить незавершенные подчиненные поручения.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить незавершенные.</param>
    /// <returns>Список незавершенных подчиненных поручений.</returns>
    [Remote(IsPure = true)]
    public static List<IActionItemExecutionTask> GetSubActionItemExecutions(Sungero.RecordManagement.IActionItemExecutionAssignment entity)
    {
      return ActionItemExecutionTasks.GetAll()
        .Where(t => entity != null && t.ParentAssignment == entity)
        .Where(t => t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional ||
               t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Main)
        .Where(t => t.Status.Value == Workflow.Task.Status.InProcess)
        .ToList();
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задания.
    /// </summary>
    /// <param name="assignment">Задание, для которого проверить.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(IAssignment assignment)
    {
      var subActionItemExecutions = ActionItemExecutionTasks.GetAll()
        .Where(ai => Equals(ai.ParentAssignment, assignment));
      if (!subActionItemExecutions.Any())
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Any();
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи, с определенным значением жизненного цикла.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <param name="status">Статус поручений.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task, Enumeration status)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Any(a => a.Status == status);
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи, с определенным значением жизненного цикла, с учетом, что "Выдал" адресат.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <param name="status">Статус поручений.</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task, Enumeration status, Sungero.Company.IEmployee addressee)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Where(a => Equals(addressee, a.AssignedBy))
        .Any(a => a.Status == status);
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Получить задания исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="task"> Поручение, для которого требуется получить задания.</param>
    /// <returns>Задания исполнителей, не завершивших работу по поручению.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItems(Sungero.RecordManagement.IActionItemExecutionTask task)
    {
      return ActionItemExecutionAssignments
        .GetAll()
        .Where(a => task.IsCompoundActionItem == true && Equals(task, a.Task.ParentTask) ||
               task.IsCompoundActionItem != true && Equals(task, a.Task))
        .Where(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
    }
    
    /// <summary>
    /// Получить все задания по пунктам составного поручения.
    /// </summary>
    /// <param name="task"> Поручение, для которого требуется получить задания.</param>
    /// <returns>Задания по пунктам составного поручения.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItemPartAssignments(Sungero.RecordManagement.IActionItemExecutionTask task)
    {
      return ActionItemExecutionAssignments
        .GetAll()
        .Where(a => Equals(task, a.Task.ParentTask))
        .OrderByDescending(a => a.Status == Workflow.Task.Status.InProcess);
    }
    
    /// <summary>
    /// Получить список поручений для формирования блока резолюции задачи на согласование.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="status">Статус поручений (исключаемый).</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>Список поручений.</returns>
    [Remote(IsPure = true), Public]
    public static List<ITask> GetActionItemsForResolution(ITask task, Enumeration status, IEmployee addressee)
    {
      var actionItems = RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => Equals(t.ParentAssignment.Task, task) && t.Status != status && Equals(t.AssignedBy, addressee))
        .OrderBy(t => t.Started);
      
      var actionItemList = new List<ITask>();
      
      foreach (var actionItem in actionItems)
      {
        if (actionItem.IsCompoundActionItem == true)
        {
          foreach (var item in actionItem.ActionItemParts)
          {
            actionItemList.Add(item.ActionItemPartExecutionTask);
          }
        }
        else
        {
          actionItemList.Add(actionItem);
        }
      }
      
      return actionItemList;
    }

    /// <summary>
    /// Сформировать вспомогательную информацию по поручению для задачи на согласование.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Вспомогательная информация по поручению для задачи на согласование.</returns>
    [Remote(IsPure = true), Public]
    public static List<string> ActionItemInfoProvider(ITask task)
    {
      var result = new string[4];
      var actionItem = ActionItemExecutionTasks.As(task);
      if (task != null)
      {
        // Отчет пользователя. result[0]
        result[0] += actionItem.ActiveText;
        
        // Исполнители. result[1]
        if (actionItem.CoAssignees.Any())
          result[1] += string.Format("{0}: {1}, {2}: {3}",
                                     Docflow.Resources.StateViewResponsible,
                                     Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false),
                                     Docflow.Resources.StateViewCoAssignees,
                                     string.Join(", ", actionItem.CoAssignees.Select(c => Company.PublicFunctions.Employee.GetShortName(c.Assignee, false))));
        else
          result[1] += string.Format("{0}: {1}", Docflow.Resources.StateViewAssignee, Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false));
        
        // Срок. result[2]
        if (actionItem.MaxDeadline.HasValue)
          result[2] += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, Docflow.PublicFunctions.Module.ToShortDateShortTime(actionItem.MaxDeadline.Value.ToUserTime()));
        
        // Контролер. result[3]
        if (actionItem.IsUnderControl == true)
        {
          result[3] += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(actionItem.Supervisor, false));
        }
      }
      return result.ToList();
    }
    
    /// <summary>
    /// Получить исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IUser> GetActionItemsPerformers(Sungero.RecordManagement.IActionItemExecutionTask entity)
    {
      return GetActionItems(entity).Select(p => p.Performer);
    }
    
    /// <summary>
    /// Выдать права на вложения поручения.
    /// </summary>
    /// <param name="attachmentGroup"> Группа вложения.</param>
    /// <param name="needGrantAccessRightsToPerformer"> Нужно ли выдать права исполнителю.</param>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Obsolete("Используйте метод GrantAccessRightsToAttachments.")]
    public virtual void GrantRightsToAttachments(List<IEntity> attachmentGroup, bool needGrantAccessRightsToPerformer)
    {
      foreach (var item in attachmentGroup)
      {
        if (ElectronicDocuments.Is(item))
        {
          if (_obj.Author != null)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Author, DefaultAccessRightsTypes.Read);
          
          if (_obj.AssignedBy != null)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.AssignedBy, DefaultAccessRightsTypes.Read);
          
          if (_obj.Supervisor != null)
          {
            var accessRightType = item.AccessRights.CanUpdate(_obj.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Supervisor, accessRightType);
          }
          
          if (_obj.Assignee != null && needGrantAccessRightsToPerformer)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Assignee, DefaultAccessRightsTypes.Read);
          
          foreach (var observer in _obj.ActionItemObservers)
          {
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, observer.Observer, DefaultAccessRightsTypes.Read);
          }
        }
      }
    }
    
    /// <summary>
    /// Выдать права на вложения поручения с сохранением.
    /// </summary>
    /// <param name="attachmentGroup"> Группа вложения.</param>
    /// <param name="needGrantAccessRightsToPerformer"> Нужно ли выдать права исполнителю.</param>
    public virtual void GrantAccessRightsToAttachmentsWithSave(List<IEntity> attachmentGroup, bool needGrantAccessRightsToPerformer)
    {
      this.GrantAccessRightsToAttachments(attachmentGroup, needGrantAccessRightsToPerformer);
      foreach (var item in attachmentGroup)
        item.AccessRights.Save();
      _obj.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на вложения поручения.
    /// </summary>
    /// <param name="attachmentGroup"> Группа вложения.</param>
    /// <param name="needGrantAccessRightsToPerformer"> Нужно ли выдать права исполнителю.</param>
    public virtual void GrantAccessRightsToAttachments(List<IEntity> attachmentGroup, bool needGrantAccessRightsToPerformer)
    {
      foreach (var item in attachmentGroup)
      {
        if (ElectronicDocuments.Is(item))
        {
          if (_obj.Author != null)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Author, DefaultAccessRightsTypes.Read);
          
          if (_obj.AssignedBy != null)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.AssignedBy, DefaultAccessRightsTypes.Read);
          
          if (_obj.Supervisor != null)
          {
            var accessRightType = item.AccessRights.CanUpdate(_obj.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Supervisor, accessRightType);
          }
          
          if (_obj.Assignee != null && needGrantAccessRightsToPerformer)
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Assignee, DefaultAccessRightsTypes.Read);
          
          foreach (var observer in _obj.ActionItemObservers)
          {
            Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, observer.Observer, DefaultAccessRightsTypes.Read);
          }
        }
      }
    }
    
    /// <summary>
    /// Выдать права исполнителю на вложения поручения.
    /// </summary>
    /// <param name="attachmentGroup"> Группа вложения.</param>
    public virtual void GrantAccessRightsToAssignee(List<IEntity> attachmentGroup)
    {
      foreach (var item in attachmentGroup)
      {
        if (ElectronicDocuments.Is(item) && _obj.Assignee != null)
          Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(item, _obj.Assignee, DefaultAccessRightsTypes.Read);
      }
    }
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <returns>Поручение.</returns>
    [Remote(PackResultEntityEagerly = true)]
    public virtual IActionItemExecutionTask CreateActionItemExecutionFromExecution(Sungero.RecordManagement.IActionItemExecutionAssignment actionItemAssignment)
    {            
      return PublicFunctions.Module.CreateActionItemExecutionFromExecution(actionItemAssignment);
    }
    
    /// <summary>
    /// Выдать права на задачу контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего) с сохранением.
    /// </summary>
    /// <param name="targetTask">Текущая задача.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущую задачу с правами.</returns>
    public static IEntity GrantAccessRightToTaskWithSave(IEntity targetTask, ITask sourceTask)
    {
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(targetTask, sourceTask);
      targetTask.AccessRights.Save();
      
      return targetTask;
    }
    
    /// <summary>
    /// Выдать права на задачу контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
    /// </summary>
    /// <param name="targetTask">Текущая задача.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущую задачу с правами.</returns>
    public static IEntity GrantAccessRightToTask(IEntity targetTask, ITask sourceTask)
    {
      if (targetTask == null || sourceTask == null)
        return null;
      
      if (!ActionItemExecutionTasks.Is(sourceTask))
        sourceTask = GetLeadTaskToTask(sourceTask);
      
      var leadPerformers = Functions.ActionItemExecutionTask.GetLeadActionItemExecutionPerformers(ActionItemExecutionTasks.As(sourceTask));
      foreach (var performer in leadPerformers)
        Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(targetTask, performer, DefaultAccessRightsTypes.Change);
      
      return targetTask;
    }
    
    /// <summary>
    /// Выдать права на задание контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
    /// </summary>
    /// <param name="targetAssignment">Текущее задание.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущее задание с правами.</returns>
    [Remote, Public]
    public static IAssignment GrantAccessRightToAssignment(IAssignment targetAssignment, ITask sourceTask)
    {
      GrantAccessRightToTask(targetAssignment, sourceTask);
      targetAssignment.AccessRights.Save();
      return targetAssignment;
    }
    
    /// <summary>
    /// Получить всех контролеров, инициаторов (включая группу регистрации) ведущих задач.
    /// </summary>
    /// <param name="actionItemExecution">Поручение.</param>
    /// <returns>Список контролеров, инициаторов.</returns>
    public static List<IRecipient> GetLeadActionItemExecutionPerformers(Sungero.RecordManagement.IActionItemExecutionTask actionItemExecution)
    {
      var leadPerformers = new List<IRecipient>();
      var taskAuthors = new List<IRecipient>();
      ITask parentTask = actionItemExecution;
      
      while (true)
      {
        if (parentTask.StartedBy != null)
          taskAuthors.Add(parentTask.StartedBy);
        
        if (ActionItemExecutionTasks.Is(parentTask))
        {
          var parentActionItemExecution = ActionItemExecutionTasks.As(parentTask);
          taskAuthors.Add(parentActionItemExecution.Author);
          if (parentActionItemExecution.Supervisor != null)
            leadPerformers.Add(parentActionItemExecution.Supervisor);
          if (parentActionItemExecution.AssignedBy != null)
            leadPerformers.Add(parentActionItemExecution.AssignedBy);
        }
        else if (DocumentReviewTasks.Is(parentTask))
        {
          var parentDocumentReview = DocumentReviewTasks.As(parentTask);
          taskAuthors.Add(parentDocumentReview.Author);
        }
        else if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        {
          // TODO Добавить исполнителей согласования.
          var parentApprovalTask = Sungero.Docflow.ApprovalTasks.As(parentTask);
          taskAuthors.Add(parentApprovalTask.Author);
        }
        
        if (Equals(parentTask.MainTask, parentTask))
          break;
        parentTask = GetLeadTaskToTask(parentTask);
      }
      
      leadPerformers.AddRange(taskAuthors);
      var registrationGroup = Functions.ActionItemExecutionTask.GetExecutingDocumentRegistrationGroup(actionItemExecution);
      if (registrationGroup != null)
        leadPerformers.Add(registrationGroup);
      
      return leadPerformers.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить ведущую задачу задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Ведущая задача.</returns>
    public static ITask GetLeadTaskToTask(ITask task)
    {
      if (task.ParentAssignment != null)
        return task.ParentAssignment.Task;
      else
        return task.ParentTask ?? task.MainTask;
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();
      
      var registrationGroup = Functions.ActionItemExecutionTask.GetExecutingDocumentRegistrationGroup(_obj);
      if (registrationGroup != null)
        assignees.Add(registrationGroup);
      
      if (_obj.Assignee != null)
        assignees.Add(_obj.Assignee);
      
      if (_obj.Supervisor != null)
        assignees.Add(_obj.Supervisor);
      
      if (_obj.AssignedBy != null)
        assignees.Add(_obj.AssignedBy);
      
      assignees.AddRange(_obj.CoAssignees.Where(o => o.Assignee != null).Select(o => o.Assignee));
      assignees.AddRange(_obj.ActionItemParts.Where(o => o.Assignee != null).Select(o => o.Assignee));
      assignees.AddRange(_obj.PartsCoAssignees.Where(o => o.CoAssignee != null).Select(o => o.CoAssignee));
      assignees.AddRange(_obj.ActionItemParts.Where(o => o.Supervisor != null).Select(o => o.Supervisor));
      assignees.AddRange(_obj.ActionItemObservers.Where(o => o.Observer != null).Select(o => o.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Проверить документ на вхождение в обязательную группу вложений.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ обязателен.</returns>
    public virtual bool DocumentInRequredGroup(Docflow.IOfficialDocument document)
    {
      return _obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(d, document));
    }
    
    /// <summary>
    /// Добавить получателей в группу исполнителей поручения, исключая дублирующиеся записи.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    /// <returns>Если возникли ошибки/хинты, возвращает текст ошибки, иначе - пустая строка.</returns>
    [Public, Remote]
    public string SetRecipientsToAssignees(IRecipient recipient)
    {
      var error = string.Empty;
      var performers = new List<IRecipient> { recipient };
      var employees = Company.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(performers);
      if (employees.Count > Constants.ActionItemExecutionTask.MaxCompoundGroup)
        return ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup);
      
      var currentPerformers = _obj.ActionItemParts.Select(x => x.Assignee);
      employees = employees.Except(currentPerformers).ToList();
      
      foreach (var employee in employees)
        _obj.ActionItemParts.AddNew().Assignee = employee;
      
      return error;
    }
    
    /// <summary>
    /// Получить состояние исполнения документа исключительно по этой задаче.
    /// </summary>
    /// <returns>Состояние исполнения документа исключительно по этой задаче.</returns>
    public virtual Enumeration? GetDocumentExecutionState()
    {
      // Статус "На исполнении".
      if (_obj.ExecutionState == Sungero.RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
          _obj.ExecutionState == Sungero.RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework ||
          _obj.ExecutionState == Sungero.RecordManagement.ActionItemExecutionTask.ExecutionState.OnControl)
        return Sungero.Docflow.OfficialDocument.ExecutionState.OnExecution;
      
      // Статус "Исполнен".
      if (_obj.ExecutionState == Sungero.RecordManagement.ActionItemExecutionTask.ExecutionState.Executed)
        return Sungero.Docflow.OfficialDocument.ExecutionState.Executed;
      
      // Статус "Прекращено".
      if (_obj.ExecutionState == Sungero.RecordManagement.ActionItemExecutionTask.ExecutionState.Aborted)
        return Sungero.Docflow.OfficialDocument.ExecutionState.Aborted;
      
      return null;
    }
    
    /// <summary>
    /// Добавить документы из группы "Результаты исполнения" в ведущее задание на исполнение.
    /// </summary>
    [Public, Remote]
    public virtual void SynchronizeResultGroup()
    {
      var parentAssignment = Functions.ActionItemExecutionTask.GetParentAssignment(_obj);
      if (parentAssignment != null && parentAssignment.Status != Workflow.Assignment.Status.Completed)
      {
        var documentGroup = parentAssignment.ResultGroup.OfficialDocuments;
        foreach (var document in _obj.ResultGroup.OfficialDocuments)
        {
          if (!documentGroup.Contains(document))
            documentGroup.Add(document);
        }
        parentAssignment.Save();

        // Выдать права на вложенные документы.
        var parentActionItem = ActionItemExecutionTasks.As(parentAssignment.Task);
        if (parentActionItem != null)
          Functions.ActionItemExecutionTask.GrantAccessRightsToAttachments(parentActionItem, parentAssignment.ResultGroup.All.ToList(), false);
      }
    }
    
    /// <summary>
    /// Добавить отчет исполнителей из подчиненных поручений в ведущее задание на исполнение.
    /// </summary>
    public virtual void SynchronizeResultActiveText()
    {
      var parentAssignment = Functions.ActionItemExecutionTask.GetParentAssignment(_obj);
      if (parentAssignment == null || parentAssignment.Status == Workflow.Assignment.Status.Completed)
        return;
      
      // Получить все подчиненные поручения.
      var subActionItems = ActionItemExecutionTasks.GetAll()
        .Where(t => Equals(t.ParentAssignment, parentAssignment) &&
               parentAssignment.Task.StartId == t.ParentStartId)
        .ToList();
      
      // Получить все пункты составных подпоручений.
      var compoundTasks = subActionItems.Where(i => i.IsCompoundActionItem == true);
      var subActionItemParts = ActionItemExecutionTasks.GetAll()
        .Where(t => ActionItemExecutionTasks.Is(t.ParentTask))
        .Where(t => compoundTasks.Contains(ActionItemExecutionTasks.As(t.ParentTask)) &&
               t.ParentTask.StartId == t.ParentStartId)
        .ToList();
      subActionItems.AddRange(subActionItemParts);
      
      var completedSubActionItems = subActionItems
        .Where(x => x.Status == Sungero.Workflow.Task.Status.Completed)
        .ToList();
      completedSubActionItems.Add(_obj);
      
      // Получить все задания на исполнение.
      var assignments = ActionItemExecutionAssignments.GetAll()
        .Where(x => ActionItemExecutionTasks.Is(x.Task) &&
               completedSubActionItems.Contains(ActionItemExecutionTasks.As(x.Task)) &&
               Equals(x.TaskStartId, x.Task.StartId))
        .ToList();
      
      // Сформировать общий отчет.
      var activeTextItems = new List<string>();
      activeTextItems.Add(this.GetParentAssignmentOwnActiveText(parentAssignment));
      activeTextItems.AddRange(this.GetSubActionItemsActiveTexts(assignments));
      var separator = string.Format("{0}{0}", Environment.NewLine);
      parentAssignment.ActiveText = string.Join(separator, activeTextItems);
      parentAssignment.Save();
    }
    
    /// <summary>
    /// Получить собственную часть ActiveText ведущего задания.
    /// </summary>
    /// <param name="assignment">Ведущее задание.</param>
    /// <returns>Собственная часть ActiveText ведущего задания.</returns>
    public virtual string GetParentAssignmentOwnActiveText(IActionItemExecutionAssignment assignment)
    {
      if (string.IsNullOrWhiteSpace(assignment.ActiveText))
        return ActionItemExecutionTasks.Resources.ActionItemExecutionExecutedLabel;
      return assignment.ActiveText;
    }
    
    /// <summary>
    /// Получить коллекцию ActiveText по подчиненным поручениям.
    /// </summary>
    /// <param name="assignments">Подчиненные задания на исполнение поручения.</param>
    /// <returns>Коллекция ActiveText по подчиненным поручениям.</returns>
    /// <remarks>Для каждого поручения ActiveText будет преобразован к формату
    /// Фамилия И.О.:_ActiveText.</remarks>
    public virtual List<string> GetSubActionItemsActiveTexts(List<IActionItemExecutionAssignment> assignments)
    {
      return assignments
        .GroupBy(x => Company.PublicFunctions.Employee.GetShortName(ActionItemExecutionTasks.As(x.Task).Assignee, true))
        .Select(x => string.Format("{0}: {1}",
                                   x.Key,
                                   string.Join(Environment.NewLine, x.OrderBy(a => a.Task.Id).ThenBy(a => a.IterationId).Select(a => a.ActiveText))))
        .ToList();
    }

    /// <summary>
    /// Выполнить ведущее задание на исполнение поручения.
    /// </summary>
    [Public, Remote]
    public virtual void CompleteParentAssignment()
    {
      var assignment = Functions.ActionItemExecutionTask.GetParentAssignment(_obj);
      if (assignment != null && assignment.Status != Workflow.Assignment.Status.Completed)
      {
        Logger.DebugFormat("ActionItemExecutionAssignment(ID={0}) completed automatically from ActionItemExecutionTask(ID={1}). (Result=Done)",
                           assignment.Id,
                           _obj.Id);
        assignment.Complete(Sungero.RecordManagement.ActionItemExecutionAssignment.Result.Done);
      }
    }
    
    /// <summary>
    /// После выполнения ведущего задания на исполнение поручения заполнить в нем свойство "Выполнил" исполнителем задания.
    /// </summary>
    [Public, Remote]
    public virtual void SetCompletedByInParentAssignment()
    {
      var assignment = Functions.ActionItemExecutionTask.GetParentAssignment(_obj);
      var currentUser = Users.Current;
      var performer = assignment.Performer;
      if (assignment != null && assignment.Status == Workflow.Assignment.Status.Completed &&
          currentUser != null && currentUser.IsSystem == true && Equals(currentUser, assignment.CompletedBy))
      {
        Logger.DebugFormat("ActionItemExecutionAssignment(ID={0}) performer: {1}(ID={2}).", assignment.Id, performer.DisplayValue, performer.Id);
        Logger.DebugFormat("ActionItemExecutionAssignment(ID={0}) completed by {1}(ID={2}). Set CompletedBy to {3}(ID={4}).",
                           assignment.Id,
                           currentUser.DisplayValue, currentUser.Id,
                           performer.DisplayValue, performer.Id);
        assignment.CompletedBy = performer;
        assignment.Save();
      }
    }
    
    /// <summary>
    /// Выполнить блоки мониторинга составного поручения.
    /// </summary>
    public virtual void ExecuteParentActionItemExecutionTaskMonitorings()
    {
      var task = _obj.ParentTask;
      if (task == null || !ActionItemExecutionTasks.Is(task))
        return;
      var actionItem = ActionItemExecutionTasks.As(task);
      if (actionItem.IsCompoundActionItem == true &&
          Functions.ActionItemExecutionTask.AllActionItemPartsAreCompleted(actionItem))
      {
        Logger.DebugFormat("ActionItemExecutionTask(ID={0}) Call ExecuteAllMonitoringBlocks of ParentTask(ID={1})", _obj.Id, actionItem.Id);
        actionItem.Blocks.ExecuteAllMonitoringBlocks();
      }
    }
    
    /// <summary>
    /// Проверить, выполнены ли все пункты составного поручения.
    /// </summary>
    /// <returns>True, если все пункты составного поручения выполнены, иначе - False.</returns>
    [Remote(IsPure = true)]
    public virtual bool AllActionItemPartsAreCompleted()
    {
      return !ActionItemExecutionTasks.GetAll(j => Equals(j.ParentTask, _obj) &&
                                              j.Status.Value != Workflow.Task.Status.Aborted &&
                                              j.Status.Value != Workflow.Task.Status.Completed &&
                                              j.Status.Value != Workflow.Task.Status.Draft &&
                                              j.ParentStartId == _obj.StartId).Any();
    }
    
    /// <summary>
    /// Проверить, выполнены ли все пункты составного поручения, кроме текущего.
    /// </summary>
    /// <returns>True, если все пункты составного поручения, кроме текущего, выполнены, иначе - False.</returns>
    [Remote(IsPure = true)]
    public virtual bool AllOtherActionItemPartsAreCompleted()
    {
      if (_obj.ParentTask == null)
        throw AppliedCodeException.Create(string.Format("ActionItemExecutionTask (ID = {0}) parent task is null.", _obj.Id));
      
      return !ActionItemExecutionTasks.GetAll(j => Equals(j.ParentTask, _obj.ParentTask) &&
                                              j.Status.Value != Workflow.Task.Status.Aborted &&
                                              j.Status.Value != Workflow.Task.Status.Completed &&
                                              j.Status.Value != Workflow.Task.Status.Draft &&
                                              j.Id != _obj.Id &&
                                              j.ParentStartId == _obj.ParentTask.StartId).Any();
    }
    
    /// <summary>
    /// Проверить, можно ли автоматически выполнить ведущее поручение.
    /// </summary>
    /// <returns>True, если можно автоматически выполнить ведущее поручение, иначе - False.</returns>
    public virtual bool CanAutoExecParentAssignment()
    {
      // У составного поручения IsAutoExec не пробрасывается в пункты и имеет смысл только для задачи-контейнера.
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      if (parentTask == null && _obj.IsAutoExec == false ||
          parentTask != null && parentTask.IsCompoundActionItem == true && parentTask.IsAutoExec == false)
        return false;
      
      return !this.GetOtherNotCompletedActionItemExecutionSubTasks().Any();
    }
    
    /// <summary>
    /// Проверить, выполнены ли все подчиненные поручения, кроме текущего.
    /// </summary>
    /// <returns>True, если все подчиненные поручения, кроме текущего, выполнены, иначе - False.</returns>
    [Public, Remote(IsPure = true), Obsolete("Используйте метод GetOtherNotCompletedActionItemExecutionSubTasks")]
    public virtual bool AllOtherActionItemExecutionSubTasksAreCompleted()
    {
      return !this.GetOtherNotCompletedActionItemExecutionSubTasks().Any();
    }
    
    /// <summary>
    /// Получить все невыполненные подчиненные поручения, кроме текущего.
    /// </summary>
    /// <returns>Список невыполненных подчиненных поурчений, кроме текущего.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IActionItemExecutionTask> GetOtherNotCompletedActionItemExecutionSubTasks()
    {
      var result = new List<IActionItemExecutionTask>();
      
      var assignment = Functions.ActionItemExecutionTask.GetParentAssignment(_obj);
      if (assignment == null)
        return result;
      
      // Проверить наличие других невыполненных подчиненных поручений у ведущего задания.
      var otherActionItems = Functions.ActionItemExecutionTask.GetSubActionItemExecutions(assignment)
        .Where(x => x.Id != _obj.Id).ToList();
      
      var notCompoundActionItems = otherActionItems.Where(x => x.IsCompoundActionItem != true).ToList();
      result.AddRange(notCompoundActionItems);
      
      // Получить незавершенные пункты составных подчиненных поручений.
      var compoundActionItems = otherActionItems.Where(x => x.IsCompoundActionItem == true).ToList();
      foreach (var actionItem in compoundActionItems)
      {
        if (Functions.ActionItemExecutionTask.AllActionItemPartsAreCompleted(actionItem))
          continue;
        
        if (!actionItem.ActionItemParts.Any(p => p.ActionItemPartExecutionTask != null &&
                                            p.ActionItemPartExecutionTask.Id == _obj.Id))
        {
          result.Add(actionItem);
          continue;
        }
        
        if (!Functions.ActionItemExecutionTask.AllOtherActionItemPartsAreCompleted(_obj))
          result.Add(actionItem);
      }
      
      return result.ToList();
    }

    /// <summary>
    /// Получить задание на исполнение.
    /// </summary>
    /// <returns>Задание на исполнение.</returns>
    /// <remarks>Сортировка по дате создания нужна для того,
    /// чтобы выбиралось актуальное задание по текущему пользователю,
    /// так как возможна ситуация, что в результате корректировок он становился исполнителем более одного раза.</remarks>
    public virtual IActionItemExecutionAssignment GetActionItemExecutionAssignment()
    {
      return ActionItemExecutionAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => Equals(j.Performer, _obj.Assignee))
        .Where(j => j.TaskStartId == _obj.StartId)
        .OrderByDescending(j => j.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на приемку.
    /// </summary>
    /// <returns>Задание на приемку.</returns>
    /// <remarks>Сортировка по дате создания нужна для того,
    /// чтобы выбиралось актуальное задание по текущему пользователю,
    /// так как возможна ситуация, что в результате корректировок он становился контролером более одного раза.</remarks>
    public virtual IActionItemSupervisorAssignment GetActionItemSupervisorAssignment()
    {
      return ActionItemSupervisorAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => Equals(j.Performer, _obj.Supervisor))
        .Where(j => j.TaskStartId == _obj.StartId)
        .OrderByDescending(j => j.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить активное задание на исполнение актуальному исполнителю поручения.
    /// </summary>
    /// <returns>Задание на исполнение.</returns>
    public virtual IActionItemExecutionAssignment GetActualActionItemExecutionAssignment()
    {
      return ActionItemExecutionAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => Equals(j.Performer, _obj.Assignee))
        .Where(j => j.TaskStartId == _obj.StartId)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить активное задание на приемку актуальному контролеру поручения.
    /// </summary>
    /// <returns>Задание на приемку.</returns>
    public virtual IActionItemSupervisorAssignment GetActualActionItemSupervisorAssignment()
    {
      return ActionItemSupervisorAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => Equals(j.Performer, _obj.Supervisor))
        .Where(j => j.TaskStartId == _obj.StartId)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить все задания на приемку по текущей задаче, находящиеся в работе.
    /// </summary>
    /// <returns>Задания на приемку.</returns>
    public virtual List<IActionItemSupervisorAssignment> GetActionItemSupervisorAssignmentsInProcess()
    {
      return ActionItemSupervisorAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => j.TaskStartId == _obj.StartId)
        .ToList();
    }
    
    /// <summary>
    /// Получить все задания на исполнение по текущей задаче.
    /// </summary>
    /// <returns>Задания на исполнение.</returns>
    public virtual List<IActionItemExecutionAssignment> GetActionItemExecutionAssignments()
    {
      return ActionItemExecutionAssignments.GetAll()
        .Where(j => Equals(j.Task, _obj))
        .Where(j => j.TaskStartId == _obj.StartId)
        .ToList();
    }
    
    /// <summary>
    /// Получить все поручения соисполнителям.
    /// </summary>
    /// <param name="parentAssignment">Родительское задание.</param>
    /// <returns>Поручения соисполнителям.</returns>
    public virtual List<IActionItemExecutionTask> GetCoAssigneeActionItemExecutionTasks(IAssignment parentAssignment)
    {
      return ActionItemExecutionTasks.GetAll()
        .Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task, _obj))
        .Where(t => t.ActionItemType == ActionItemType.Additional)
        .Where(t => Equals(t.ParentAssignment, parentAssignment))
        .ToList();
    }
    
    /// <summary>
    /// Получить актуальных контролера и исполнителя поручения.
    /// </summary>
    /// <returns>Список, состоящий из контролера и исполнителя поручения.</returns>
    public virtual List<IEmployee> GetActualSupervisorAndAssignee()
    {
      var employees = new List<IEmployee>();
      
      if (_obj.Supervisor != null)
        employees.Add(_obj.Supervisor);
      if (_obj.Assignee != null)
        employees.Add(_obj.Assignee);
      
      return employees;
    }
    
    /// <summary>
    /// Проверить необходимость отправки уведомления контролеру.
    /// </summary>
    /// <returns>True, если требуется отправка уведомления.</returns>
    public virtual bool NeedSendSupervisorNotice()
    {
      return _obj.Supervisor != null && _obj.Author != _obj.Supervisor && _obj.StartedBy != _obj.Supervisor
        && (_obj.ActionItemType == ActionItemType.Main || _obj.ActionItemType == ActionItemType.Component && !Equals(_obj.Supervisor, ActionItemExecutionTasks.As(_obj.ParentTask).Supervisor));
    }
    
    /// <summary>
    /// Заполнить срок выполнения задания на приёмку контролёром в днях и часах относительно даты выполнения поручения.
    /// </summary>
    /// <param name="e">Аргументы задания.</param>
    public virtual void SetControlRelativeDeadline(Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      var settings = Functions.Module.GetSettings();
      if (settings.ControlRelativeDeadlineInDays.HasValue)
        e.Block.RelativeDeadlineDays = settings.ControlRelativeDeadlineInDays;
      if (settings.ControlRelativeDeadlineInHours.HasValue)
        e.Block.RelativeDeadlineHours = settings.ControlRelativeDeadlineInHours;
      if (!settings.ControlRelativeDeadlineInDays.HasValue && !settings.ControlRelativeDeadlineInHours.HasValue)
        e.Block.RelativeDeadlineDays = Constants.ActionItemExecutionTask.ControlRelativeDeadline;
    }
    
    /// <summary>
    /// Синхронизировать пункт поручения в грид основного составного поручения.
    /// </summary>
    /// <param name="needSaveParentTask">Признак необходимости сохранения родительской задачи контейнера.</param>
    public virtual void SynchronizeActionItemPart(bool needSaveParentTask)
    {
      var parentTask = ActionItemExecutionTasks.As(_obj.ParentTask);
      var needChangeDeadlines = false;
      var needChangeOnlyDeadlines = true;
      
      var actionItem = parentTask.ActionItemParts.FirstOrDefault(s => Equals(s.ActionItemPartExecutionTask, _obj));
      // Обновить текст поручения, если изменен индивидуальный текст или указан общий текст вместо индивидуального.
      if (actionItem.ActionItemExecutionTask.ActiveText != _obj.ActiveText && actionItem.ActionItemPart != _obj.ActiveText ||
          actionItem.ActionItemExecutionTask.ActiveText == _obj.ActiveText && !string.IsNullOrEmpty(actionItem.ActionItemPart))
      {
        actionItem.ActionItemPart = _obj.ActiveText;
        needChangeOnlyDeadlines = false;
      }
      
      // Обновить срок поручения, если он изменен.
      var deadlineIsCommon = actionItem.Deadline == null && actionItem.ActionItemExecutionTask.FinalDeadline == _obj.Deadline;
      var deadlineChanged = actionItem.Deadline != _obj.Deadline && !deadlineIsCommon;
      if (deadlineChanged)
      {
        actionItem.Deadline = _obj.Deadline;
        needChangeDeadlines = true;
      }
      
      // Обновить исполнителя, если он изменен.
      if (!Equals(actionItem.ActionItemExecutionTask.Assignee, _obj.Assignee) && !Equals(actionItem.Assignee, _obj.Assignee))
      {
        actionItem.Assignee = _obj.Assignee;
        needChangeOnlyDeadlines = false;
      }
      
      // Обновить соисполнителей, если они изменены.
      var actionItemPartsCoassignees = Functions.ActionItemExecutionTask.GetPartCoAssignees(parentTask, actionItem.PartGuid)
        .OrderBy(p => p.Id).ToList();
      var taskCoAssignees = _obj.CoAssignees.Select(p => p.Assignee).OrderBy(p => p.Id).ToList();
      
      if (!actionItemPartsCoassignees.SequenceEqual(taskCoAssignees))
      {
        var partsCoAssignees = parentTask.PartsCoAssignees;
        var items = partsCoAssignees.Where(c => c.PartGuid == actionItem.PartGuid).ToList();
        foreach (var item in items)
          partsCoAssignees.Remove(item);
        
        foreach (var taskCoAssignee in taskCoAssignees)
        {
          var part = partsCoAssignees.AddNew();
          part.PartGuid = actionItem.PartGuid;
          part.CoAssignee = taskCoAssignee;
        }
        
        actionItem.CoAssignees = Docflow.PublicFunctions.Module.GetCoAssigneesNames(taskCoAssignees, true);
        needChangeOnlyDeadlines = false;
      }
      
      // Обновить срок соиcполнителя поручения.
      if (actionItem.CoAssigneesDeadline != _obj.CoAssigneesDeadline)
      {
        actionItem.CoAssigneesDeadline = _obj.CoAssigneesDeadline;
        needChangeDeadlines = true;
      }
      
      // Обновить контролера, если он изменен.
      var supervisorIsCommon = actionItem.Supervisor == null && Equals(actionItem.ActionItemExecutionTask.Supervisor, _obj.Supervisor);
      var supervisorChanged = !Equals(actionItem.Supervisor, _obj.Supervisor) && !supervisorIsCommon;
      if (supervisorChanged)
      {
        actionItem.Supervisor = _obj.Supervisor;
        needChangeOnlyDeadlines = false;
      }
      
      // В событии BeforeStart сохранение не нужно.
      if (needSaveParentTask)
      {
        Functions.ActionItemExecutionTask.SetActionItemChangeDeadlinesParams(parentTask, needChangeDeadlines, needChangeOnlyDeadlines);
        parentTask.Save();
      }
    }
    
    /// <summary>
    /// Десериализация параметров асинхронного обработчика по изменению поручения.
    /// </summary>
    /// <param name="oldSupervisor">Старый контролёр.</param>
    /// <param name="newSupervisor">Новый контролёр.</param>
    /// <param name="oldAssignee">Старый исполнитель.</param>
    /// <param name="newAssignee">Новый исполнитель.</param>
    /// <param name="oldDeadline">Старый срок исполнителя.</param>
    /// <param name="newDeadline">Новый срок исполнителя.</param>
    /// <param name="oldCoAssignees">Старые соисполнители.</param>
    /// <param name="newCoAssignees">Новые соисполнители.</param>
    /// <param name="coAssigneeOldDeadline">Старый срок соисполнителей.</param>
    /// <param name="coAssigneeNewDeadline">Новый срок соисполнителей.</param>
    /// <param name="editingReason">Причина корректировки.</param>
    /// <param name="additionalInfo">Дополнительная информация для использования в перекрытиях.</param>
    /// <param name="taskIds">Список ИД задач (пунктов составного) для корректировки.</param>
    /// <param name="actionItemPartsText">Текстовое представление выбранных пунктов поручения.</param>
    /// <param name="initiatorOfChange">Пользователь, корректирующий поручение.</param>
    /// <param name="changeContext">Контекст вызова корректировки.</param>
    /// <returns>Структура с изменениями поручения.</returns>
    public virtual IActionItemChanges DeserializeActionItemChanges(int oldSupervisor, int newSupervisor, int oldAssignee, int newAssignee, DateTime oldDeadline, DateTime newDeadline,
                                                                   string oldCoAssignees, string newCoAssignees, DateTime coAssigneeOldDeadline, DateTime coAssigneeNewDeadline,
                                                                   string editingReason, string additionalInfo, string taskIds,
                                                                   string actionItemPartsText, int initiatorOfChange, string changeContext)
    {
      var changes = RecordManagement.Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      
      changes.OldSupervisor = oldSupervisor > 0 ? Company.Employees.Get(oldSupervisor) : null;
      changes.NewSupervisor = newSupervisor > 0 ? Company.Employees.Get(newSupervisor) : null;
      changes.OldAssignee = oldAssignee > 0 ? Company.Employees.Get(oldAssignee) : null;
      changes.NewAssignee = newAssignee > 0 ? Company.Employees.Get(newAssignee) : null;
      changes.OldDeadline = DateTime.Compare(oldDeadline, DateTime.MinValue) != 0 ? (DateTime?)oldDeadline : null;
      changes.NewDeadline = DateTime.Compare(newDeadline, DateTime.MinValue) != 0 ? (DateTime?)newDeadline : null;
      var splittedOldCoAssignees = oldCoAssignees.Split(',');
      var splittedNewCoAssignees = newCoAssignees.Split(',');
      changes.OldCoAssignees = Company.Employees.GetAll().Where(emp => splittedOldCoAssignees.Contains(emp.Id.ToString())).ToList();
      changes.NewCoAssignees = Company.Employees.GetAll().Where(emp => splittedNewCoAssignees.Contains(emp.Id.ToString())).ToList();
      changes.CoAssigneesOldDeadline = DateTime.Compare(coAssigneeOldDeadline, DateTime.MinValue) != 0 ? (DateTime?)coAssigneeOldDeadline : null;
      changes.CoAssigneesNewDeadline = DateTime.Compare(coAssigneeNewDeadline, DateTime.MinValue) != 0 ? (DateTime?)coAssigneeNewDeadline : null;
      changes.EditingReason = editingReason;
      changes.AdditionalInfo = additionalInfo;
      
      if (!string.IsNullOrEmpty(taskIds))
      {
        var splittedTaskIds = taskIds.Split(',');
        changes.TaskIds = Workflow.Tasks.GetAll().Where(t => splittedTaskIds.Contains(t.Id.ToString())).Select(t => t.Id).ToList();
      }
      else
        changes.TaskIds = new List<int>();
      
      changes.ActionItemPartsText = actionItemPartsText;
      changes.InitiatorOfChange = Users.Get(initiatorOfChange);
      changes.ChangeContext = changeContext;
      
      return changes;
    }

    #region Устаревшие методы корректировки

    /// <summary>
    /// Проверить, что по поручению уже созданы все актуальные задания, и его можно корректировать.
    /// </summary>
    /// <returns>Текст ошибки, если задания не созданы. Иначе пустую строку.</returns>
    [Obsolete("Используйте метод RecordManagement.Functions.Module.CheckActionItemAssignmentsCreated")]
    public virtual string CheckActionItemAssignmentsCreated()
    {
      return Functions.Module.CheckActionItemAssignmentsCreated(new List<IActionItemExecutionTask>() { _obj });
    }
    
    /// <summary>
    /// Проверить, что у всех поручений есть как минимум одно стартованное задание.
    /// </summary>
    /// <param name="tasks">Поручения.</param>
    /// <returns>True, если у всех поручений есть задания. Иначе False.</returns>
    [Obsolete("Используйте метод RecordManagement.Functions.Module.CheckAllAssignmentsOnTasksStarted")]
    public virtual bool CheckAllAssignmentsOnTasksStarted(IQueryable<IActionItemExecutionTask> tasks)
    {
      return Functions.Module.CheckAllAssignmentsOnTasksStarted(tasks);
    }
    
    /// <summary>
    /// Получить дату последнего изменения поручения.
    /// </summary>
    /// <returns>Дата последнего изменения поручения.</returns>
    [Obsolete("Используйте метод RecordManagement.Functions.Module.GetLastActionItemChangeDate")]
    public virtual DateTime? GetLastActionItemChangeDate()
    {
      return Functions.Module.GetLastActionItemChangeDate(new List<int>() { _obj.Id });
    }
    
    /// <summary>
    /// Проверить, что поручение (в том числе подпоручения соисполнителям, пункты составного и подпоручения соисполнителям пунктов)
    /// не корректируется в текущий момент.
    /// </summary>
    /// <returns>Текст ошибки, если корректируется. Иначе пустую строку.</returns>
    [Obsolete("Используйте метод RecordManagement.Functions.Module.CheckActionItemNotInChangingProcess")]
    public virtual string CheckActionItemNotInChangingProcess()
    {
      return Functions.Module.CheckActionItemNotInChangingProcess(new List<IActionItemExecutionTask>() { _obj });
    }
    
    /// <summary>
    /// Проверить, что поручение не было изменено с момента указанной даты.
    /// </summary>
    /// <param name="lastActionItemChangeDate">Дата последнего изменения поручения.</param>
    /// <returns>Текст ошибки, если поручение было изменено. Иначе пустую строку.</returns>
    [Obsolete("Используйте метод RecordManagement.Functions.Module.CheckActionItemNotChanged")]
    public virtual string CheckActionItemNotChanged(DateTime? lastActionItemChangeDate)
    {
      return Functions.Module.CheckActionItemNotChanged(new List<int>() { _obj.Id }, lastActionItemChangeDate);
    }
    
    #endregion
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentsGroup.OfficialDocuments.SingleOrDefault();
      if (primaryDocument == null)
        return;
      
      Logger.DebugFormat("ActionItemExecutionTask (ID = {0}). Add relation with type Addendum to primary document (ID = {1})",
                         _obj.Id, primaryDocument.Id);
      var taskAddenda = _obj.AddendaGroup.All
        .Where(x => Docflow.OfficialDocuments.Is(x))
        .Where(x => !Equals(x, primaryDocument))
        .Select(x => Docflow.OfficialDocuments.As(x))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x))
        .ToList();
      Docflow.PublicFunctions.OfficialDocument.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, taskAddenda);
    }
    
    #endregion
  }
}
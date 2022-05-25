using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewTaskFunctions
  {
    #region Контрол "Состояние"

    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить модель состояния задачи на рассмотрение документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрол состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.DocumentForReviewGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      return this.GetDocumentReviewStateView(true);
    }
    
    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <param name="addActionItemExecutionBlocks">Добавлять блок информации о поручениях и проектах резолюции.</param>
    /// <returns>Схема модели состояния.</returns>
    [Public]
    public Sungero.Core.StateView GetDocumentReviewStateView(bool? addActionItemExecutionBlocks)
    {
      var stateView = StateView.Create();
      
      // Пропустить задачу-контейнер.
      if (_obj.Addressees.Count > 1 && _obj.Started.HasValue)
        return stateView;
      
      var comment = Docflow.PublicFunctions.Module.GetTaskUserComment(_obj, Resources.ConsiderationText);
      
      if (_obj.Started.HasValue)
      {
        var startedBy = _obj.StartedBy;
        if (_obj.StartedBy.IsSystem == true)
          startedBy = _obj.MainTask != null ? _obj.MainTask.StartedBy : _obj.Author;
        
        Docflow.PublicFunctions.OfficialDocument.AddUserActionBlock(stateView, _obj.Author,
                                                                    DocumentReviewTasks.Resources.StateViewDocumentSent,
                                                                    _obj.Started.Value, _obj,
                                                                    comment, startedBy);
      }
      else
      {
        Docflow.PublicFunctions.OfficialDocument.AddUserActionBlock(stateView, _obj.Author,
                                                                    Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated,
                                                                    _obj.Created.Value, _obj,
                                                                    comment, _obj.Author);
      }
      
      var startDate = this.GetIterationStartDate();
      var managerBlock = this.AddReviewManagerBlock(stateView, startDate);
      if (managerBlock != null)
      {
        this.AddPreraringDraftResolutionBlock(managerBlock, startDate);
        this.AddReviewResolutionBlock(managerBlock, startDate);
      }
      
      // Добавить блок информации о поручениях и вложенных проектах резолюций, созданных в рамках рассмотрения.
      if (addActionItemExecutionBlocks == true)
        this.AddActionItemExecutionBlocks(stateView);
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить статус выполнения задания.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="style">Стиль.</param>
    /// <param name="assignment">Задание.</param>
    private void AddAssignmentStatusInfoToRight(StateBlock block, Sungero.Core.StateBlockLabelStyle style, IAssignment assignment)
    {
      // Добавить колонку справа, если всего одна колонка (main).
      var rightContent = block.Contents.LastOrDefault();
      if (block.Contents.Count() <= 1)
        rightContent = block.AddContent();
      else
        rightContent.AddLineBreak();

      rightContent.AddLabel(Assignments.Info.Properties.Status.GetLocalizedValue(assignment.Status), style);
    }
    
    /// <summary>
    /// Добавить блок информации о рассмотрении документа руководителем.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Полученный блок.</returns>
    private StateBlock AddReviewManagerBlock(StateView stateView, DateTime startDate)
    {
      var managerAssignment = this.GetManagerAssignment(startDate);
      var resolutionAssignment = this.GetPreparingDraftResolutionAssignment(startDate);
      
      var author = string.Empty;
      if (managerAssignment != null)
        author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(managerAssignment.Performer, managerAssignment.CompletedBy);
      else if (_obj.Addressee != null)
        author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(_obj.Addressee, _obj.Addressee);
      
      var actionItems = ActionItemExecutionTasks.GetAll()
        .Where(t => (t.ParentAssignment != null && (Equals(t.ParentAssignment.Task, _obj) || Equals(t.ParentAssignment, managerAssignment))) &&
               t.Status != Workflow.Task.Status.Draft &&
               Equals(t.AssignedBy, DocumentReviewTasks.As(_obj).Addressee))
        .OrderBy(t => t.Started);
      var isCompleted = (managerAssignment != null && managerAssignment.Status == Workflow.AssignmentBase.Status.Completed) ||
        (resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isReworkResolution = managerAssignment != null && ReviewDraftResolutionAssignments.Is(managerAssignment) &&
        managerAssignment.Result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution &&
        !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      // Добавить блок. Установить иконку и сущность.
      var block = stateView.AddBlock();
      block.Entity = _obj;
      if (isCompleted && !isReworkResolution)
        block.AssignIcon(ReviewManagerAssignments.Info.Actions.AddResolution, StateBlockIconSize.Large);
      else
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);

      // Рассмотрение руководителем ещё в работе.
      if (!isCompleted || isReworkResolution)
      {
        // Добавить заголовок.
        block.AddLabel(Docflow.Resources.StateViewDocumentReview, headerStyle);
        block.AddLineBreak();
        if (managerAssignment != null && !isReworkResolution)
        {
          if (managerAssignment.Status == Workflow.AssignmentBase.Status.Aborted)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
          else if (managerAssignment.IsRead == false)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
          else
            this.AddAssignmentStatusInfoToRight(block, labelStyle, managerAssignment);
        }
        else if (_obj.Status == Workflow.Task.Status.Completed)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewCompleted);
        }
        else if (_obj.Status == Workflow.Task.Status.Aborted)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
        }
        
        // Адресат.
        if (_obj.Addressee != null)
          block.AddLabel(string.Format("{0}: {1}",
                                       Docflow.Resources.StateViewAddressee,
                                       Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, false)), performerStyle);

        var deadline = managerAssignment != null && !isReworkResolution ?
          managerAssignment.Deadline : _obj.MaxDeadline;
        var deadlineString = deadline.HasValue ?
          Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()) :
          Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;

        block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineString),
                       performerStyle);
        
        // Информация о задержке выполнения.
        if (_obj.Status == Workflow.Task.Status.InProcess || _obj.Status == Workflow.Task.Status.UnderReview)
        {
          if (!isReworkResolution && managerAssignment != null && managerAssignment.Deadline.HasValue)
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, managerAssignment.Deadline.Value, managerAssignment.Performer);
          else if (resolutionAssignment != null && resolutionAssignment.Deadline.HasValue)
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, resolutionAssignment.Deadline.Value, resolutionAssignment.Performer);
        }
      }
      else if (managerAssignment != null || resolutionAssignment != null)
      {
        // Рассмотрение завершено.
        // Добавить заголовок.
        var completionDate = managerAssignment == null ? resolutionAssignment.Completed.Value.ToUserTime() : managerAssignment.Completed.Value.ToUserTime();
        var resolutionDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(completionDate);
        block.AddLabel(Docflow.Resources.StateViewResolution, headerStyle);
        block.AddLineBreak();
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     DocumentReviewTasks.Resources.StateViewAuthor,
                                     author,
                                     Docflow.OfficialDocuments.Resources.StateViewDate,
                                     resolutionDate), performerStyle);
        block.AddLineBreak();
        block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Если поручения не созданы, или рассмотрение выполнено с результатом "Вынести резолюцию" или "Принято к сведению" и помощник сам не отправлял поручения в работу.
        // В старых задачах поручение и рассмотрение не связаны, поэтому обрабатываем такие случаи как резолюцию.
        if (!actionItems.Any() || (managerAssignment != null && managerAssignment.Result != RecordManagement.ReviewManagerAssignment.Result.AddAssignment &&
                                   managerAssignment.Result != RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution &&
                                   !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)))
        {
          var comment = resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment ?
            Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body) :
            Docflow.PublicFunctions.Module.GetFormatedUserText(managerAssignment.Texts.Last().Body);
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
                if (item.ActionItemPartExecutionTask != null)
                  Functions.ActionItemExecutionTask.AddActionItemInfo(block, item.ActionItemPartExecutionTask, author);
              }
            }
            else
            {
              Functions.ActionItemExecutionTask.AddActionItemInfo(block, actionItem, author);
            }
          }
        }
      }
      return block;
    }
    
    /// <summary>
    /// Добавить блок информации о создании поручения по резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    private void AddReviewResolutionBlock(StateBlock parentBlock, DateTime startDate)
    {
      var resolutionAssignment = ReviewResolutionAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Created >= startDate)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.StateViewSendActionItemOnResolution, string.Empty);
    }
    
    /// <summary>
    /// Добавить блок информации о подготовке проекта резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    private void AddPreraringDraftResolutionBlock(StateBlock parentBlock, DateTime startDate)
    {
      var resolutionAssignment = this.GetPreparingDraftResolutionAssignment(startDate);
      
      var result = string.Empty;
      if (this.GetManagerAssignment(startDate) == null && resolutionAssignment != null &&
          resolutionAssignment.Status == Workflow.AssignmentBase.Status.Completed &&
          resolutionAssignment.Result != RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)
        result = Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body);
      
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.PreparingDraftResolution, result);
    }
    
    /// <summary>
    /// Добавить блок информации о поручениях и вложенных проектах резолюций, созданных в рамках рассмотрения.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    private void AddActionItemExecutionBlocks(StateView stateView)
    {
      // Поручения.
      var actionItemExecutions = ActionItemExecutionTasks
        .GetAll(t => t.ParentAssignment != null &&
                t.ParentAssignment.Task != null &&
                t.ParentAssignment.Task.Id == _obj.Id)
        .ToList();
      // Проекты резолюции.
      var draftResolutions = _obj.ResolutionGroup.ActionItemExecutionTasks
        .Where(x => Equals(x.AssignedBy, _obj.Addressee) && x.IsDraftResolution == true);
      actionItemExecutions.AddRange(draftResolutions);
      foreach (var actionItemExecution in actionItemExecutions)
      {
        if (stateView.Blocks.Any(b => b.HasEntity(actionItemExecution)))
          continue;
        
        // Сформировать схему представления поручения без добавления дополнительного блока резолюции.
        var actionItemTask = ActionItemExecutionTasks.As(actionItemExecution);
        var actionItemStateView = PublicFunctions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(actionItemTask,
                                                                                                 null, null, null, true, false);
        foreach (var block in actionItemStateView.Blocks)
          stateView.AddBlock(block);
      }
    }
    
    /// <summary>
    /// Получить дату начала текущей итерации рассмотрения.
    /// </summary>
    /// <returns>Дата начала текущей итерации рассмотрения.</returns>
    private DateTime GetIterationStartDate()
    {
      if (!_obj.Started.HasValue)
        return _obj.Created.Value;
      
      var startDate = _obj.Started.Value;
      var lastForwardedAsg = Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => a.Result == RecordManagement.ReviewManagerAssignment.Result.Forward)
        .OrderByDescending(a => a.Completed.Value)
        .FirstOrDefault();
      
      if (lastForwardedAsg != null)
        startDate = lastForwardedAsg.Completed.Value;
      
      return startDate;
    }
    
    /// <summary>
    /// Получить задание руководителю.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание руководителю.</returns>
    private IAssignment GetManagerAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => ReviewManagerAssignments.Is(a) || ReviewDraftResolutionAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на подготовку проекта резолюции.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание на подготовку проекта резолюции.</returns>
    private IAssignment GetPreparingDraftResolutionAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => PreparingDraftResolutionAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Добавить блок с заданием.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="header">Заголовок блока.</param>
    /// <param name="result">Результат выполнения задания.</param>
    private void AddAssignmentBlock(StateBlock parentBlock, IAssignment assignment, string header, string result)
    {
      if (assignment != null && (assignment.Status != Workflow.AssignmentBase.Status.Completed || !string.IsNullOrEmpty(result)))
      {
        var isDraft = _obj.Status == Workflow.Task.Status.Draft;
        var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
        
        parentBlock.IsExpanded = true;
        var block = parentBlock.AddChildBlock();
        
        block.Entity = assignment;
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
        
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.IsRead == false)
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
        else
          this.AddAssignmentStatusInfoToRight(block, labelStyle, assignment);

        var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
        var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
        
        block.AddLabel(header, headerStyle);
        block.AddLineBreak();

        var resolutionPerformerName = Employees.Is(assignment.Performer) ?
          Company.PublicFunctions.Employee.GetShortName(Employees.As(assignment.Performer), false) :
          assignment.Performer.Name;
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     Docflow.OfficialDocuments.Resources.StateViewTo,
                                     resolutionPerformerName,
                                     Docflow.OfficialDocuments.Resources.StateViewDeadline,
                                     Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime())), performerStyle);
        
        if (!string.IsNullOrEmpty(result))
        {
          var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
          
          block.AddLineBreak();
          block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
          block.AddLineBreak();
          block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
          block.AddLabel(result);
        }
        else
        {
          // Информация о задержке выполнения.
          if (_obj.Status == Workflow.Task.Status.InProcess || _obj.Status == Workflow.Task.Status.UnderReview)
            Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
        }
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить результат выполнения задания руководителю с последней итерации.
    /// </summary>
    /// <param name="task">Задача "рассмотрение входящего".</param>
    /// <returns>Результат задания руководителю.</returns>
    public static Enumeration? GetLastAssignmentResult(IDocumentReviewTask task)
    {
      var lastAssignments = Assignments.GetAll(c => Equals(c.Task, task) && c.Status == Sungero.Workflow.Assignment.Status.Completed)
        .OrderByDescending(c => c.Completed);
      if (!lastAssignments.Any())
        return null;
      else
        return lastAssignments.First().Result.Value;
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="assignees">Исполнители.</param>
    public virtual void GrantRightForAttachmentsToAssignees(List<IRecipient> assignees)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightForAttachmentsToAssignees.", _obj.Id);
      foreach (var assignee in assignees)
      {
        this.GrantRightsOnDocumentForReview(assignee);
        this.GrantRightsOnAddendaGroup(assignee);
      }
      
      // Дополнительно обновляем права наблюдателей.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
      Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), _obj.ResolutionObservers.Select(o => o.Observer));
    }
    
    /// <summary>
    /// Выдать права на документ для рассмотрения.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void GrantRightsOnDocumentForReview(IRecipient assignee)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnDocumentForReview.", _obj.Id);
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      var rights = this.GetCreationContextAttachmentRights();
      Logger.DebugFormat("DocumentReviewTask({0}) Grant rights({1}) on document({2}) for assignee({3})",
                         _obj.Id, rights, document.Id, assignee.Id);
      Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(document, assignee, rights);
    }
    
    /// <summary>
    /// Выдать права на документы группы "Приложения".
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    public virtual void GrantRightsOnAddendaGroup(IRecipient assignee)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnAddendaGroup.", _obj.Id);
      // На приложения - на изменение, но не выше, чем у инициатора.
      var attachments = _obj.AddendaGroup.All;
      var rights = this.GetCreationContextAttachmentRights();
      foreach (var attachment in attachments.Where(a => a.Info.AccessRightsMode != Metadata.AccessRightsMode.Type))
      {
        Logger.DebugFormat("DocumentReviewTask({0}). Grant rights({1}) on document({2}) for assignee({3})",
                           _obj.Id, rights, attachment.Id, assignee.Id);
        this.GrantRightsOnAddendum(attachment, assignee, _obj.Author, rights);
      }
    }
    
    /// <summary>
    /// Выдать права на документ группы "Приложения".
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="recipient">Субъект прав.</param>
    /// <param name="taskAuthor">Инициатор задачи.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    public virtual void GrantRightsOnAddendum(IEntity entity, IRecipient recipient, IRecipient taskAuthor, Guid accessRightsType)
    {
      Logger.DebugFormat("DocumentReviewTask({0}). GrantRightsOnAddendum.", _obj.Id);
      var rightType = accessRightsType == DefaultAccessRightsTypes.Change && entity.AccessRights.CanUpdate(taskAuthor)
        ? DefaultAccessRightsTypes.Change
        : DefaultAccessRightsTypes.Read;
      Logger.DebugFormat("DocumentReviewTask({0}) Grant rights({1}) on addendum({2}) for recipient({3})",
                         _obj.Id, rightType, entity.Id, recipient.Id);
      Docflow.PublicFunctions.Module.GrantAccessRightsOnEntity(entity, recipient, rightType);
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var documentReview = DocumentReviewTasks.As(_obj);
      if (documentReview == null)
        return assignees;
      
      var document = documentReview.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var clerk = Docflow.PublicFunctions.Module.Remote.GetClerk(document);
      
      var recipients = documentReview.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      foreach (var recipient in recipients)
      {
        assignees.Add(recipient);
        var secretary = Docflow.PublicFunctions.Module.GetSecretary(recipient) ?? clerk;
        assignees.Add(secretary ?? documentReview.Author);
      }
      
      assignees.AddRange(documentReview.ResolutionObservers.Where(o => o.Observer != null).Select(o => o.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Обновить адресата после переадресации.
    /// </summary>
    /// <param name="newAddressee">Новый адресат.</param>
    public void UpdateReviewTaskAfterForward(IEmployee newAddressee)
    {
      _obj.Addressee = newAddressee;
    }
    
    /// <summary>
    /// Получить делопроизводителя для отправки поручений.
    /// </summary>
    /// <returns>Исполнитель задания по отправке поручения.</returns>
    public IUser GetClerkToSendActionItem()
    {
      var author = _obj.Author;
      var addressee = Employees.As(_obj.Addressee);
      var secretary = Employees.Null;
      
      // Личный секретарь адресата (руководителя).
      if (addressee != null)
        secretary = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      
      // Ответственный за группу регистрации, либо инициатор.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (secretary == null && document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable)
        secretary = Docflow.PublicFunctions.Module.Remote.GetClerk(document);
      
      return secretary ?? author;
    }

    /// <summary>
    /// Отправить проект резолюции на исполнение.
    /// </summary>
    /// <param name="parentAssignment">Задание на рассмотрение.</param>
    [Remote, Public]
    public void StartActionItemsForDraftResolution(IAssignment parentAssignment)
    {
      parentAssignment.Save();
      // TODO Shklyaev: переделать метод, когда сделают 65004.
      foreach (var draftResolution in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft))
      {
        // Очистить все вложения и заполнить заново, чтобы корректно отработала синхронизация вновь добавленных документов.
        var officialDocuments = draftResolution.DocumentsGroup.OfficialDocuments.ToList();
        draftResolution.DocumentsGroup.OfficialDocuments.Clear();
        
        var addendaDocuments = draftResolution.AddendaGroup.OfficialDocuments.ToList();
        draftResolution.AddendaGroup.OfficialDocuments.Clear();
        
        var othersGroup = draftResolution.OtherGroup.All.ToList();
        draftResolution.OtherGroup.All.Clear();
        
        ((Sungero.Workflow.IInternalTask)draftResolution).ParentAssignment = parentAssignment;
        ((Sungero.Workflow.IInternalTask)draftResolution).MainTask = parentAssignment.MainTask;
        draftResolution.Save();
        
        foreach (var attachment in othersGroup)
        {
          draftResolution.OtherGroup.All.Add(attachment);
          
          var participants = Sungero.Docflow.PublicFunctions.Module.Remote.GetTaskAssignees(draftResolution).ToList();
          foreach (var participant in participants)
            attachment.AccessRights.Grant(participant, DefaultAccessRightsTypes.Read);
          attachment.AccessRights.Save();
        }
        
        foreach (var attachment in officialDocuments)
          draftResolution.DocumentsGroup.OfficialDocuments.Add(attachment);
        
        foreach (var attachment in addendaDocuments)
          draftResolution.AddendaGroup.OfficialDocuments.Add(attachment);
        
        draftResolution.Save();
        ((Domain.Shared.IExtendedEntity)draftResolution).Params[PublicConstants.ActionItemExecutionTask.CheckDeadline] = true;
        draftResolution.Start();
      }
    }
    
    /// <summary>
    /// Рекурсивно завершить все подзадачи на рассмотрение.
    /// </summary>
    public virtual void AbortDocumentReviewSubTasks()
    {
      var subTasks = Functions.Module.GetSubtasksForTaskRecursive(_obj);
      foreach (var subTask in subTasks.Where(t => DocumentReviewTasks.Is(t)))
        subTask.Abort();
    }
    
    /// <summary>
    /// Получить выполненные подзадачи на рассмотрение документа.
    /// </summary>
    /// <returns>Выполненные подзадачи на рассмотрение документа для текущей задачи.</returns>
    public virtual IQueryable<IDocumentReviewTask> GetCompletedDocumentReviewSubTasks()
    {
      var subTasksByParentTask = Functions.Module.GetSubtasksForTaskByParentTask(_obj, Sungero.Workflow.Task.Status.Completed)
        .Where(t => DocumentReviewTasks.Is(t))
        .Select(t => DocumentReviewTasks.As(t));
      
      var subTasksByParentAssignment = Functions.Module.GetSubtasksForTaskByParentAssignment(_obj, Sungero.Workflow.Task.Status.Completed)
        .Where(t => DocumentReviewTasks.Is(t))
        .Select(t => DocumentReviewTasks.As(t));
      
      var subTasks = new List<IDocumentReviewTask>();
      subTasks.AddRange(subTasksByParentTask);
      subTasks.AddRange(subTasksByParentAssignment);
      return subTasks.AsQueryable();
    }
    
    /// <summary>
    /// Получить состояние исполнения документа исключительно по этой задаче.
    /// </summary>
    /// <returns>Состояние исполнения документа исключительно по этой задаче.</returns>
    public virtual Enumeration? GetDocumentExecutionState()
    {
      if (_obj.Status == Sungero.Workflow.Task.Status.Aborted)
        return null;
      
      var preparingDraftResolutionAssignments = PreparingDraftResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewDraftResolutionAssignments = ReviewDraftResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewManagerAssignments = ReviewManagerAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var reviewResolutionAssignments = ReviewResolutionAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id &&
               x.Task.StartId == _obj.StartId);
      var actionItemsInProcess = ActionItemExecutionTasks.GetAll()
        .Where(t => Equals(t.MainTask, _obj.MainTask))
        .Where(t => t.Status == Workflow.Task.Status.InProcess);
      var asgIdsOnExecution = reviewDraftResolutionAssignments.Where(x => x.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution).Select(x => x.Id).ToList();
      asgIdsOnExecution.AddRange(reviewManagerAssignments.Where(x => x.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.AddAssignment).Select(x => x.Id));
      asgIdsOnExecution.AddRange(reviewResolutionAssignments.Where(x => x.Status == Workflow.AssignmentBase.Status.Completed).Select(x => x.Id));
      
      // Статус "На исполнении".
      if (asgIdsOnExecution.Any() && actionItemsInProcess.Any(y => asgIdsOnExecution.Contains(y.ParentAssignment.Id)))
        return Sungero.Docflow.OfficialDocument.ExecutionState.OnExecution;
      
      // Статус "Отправка на исполнение".
      if (reviewResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess))
        return Sungero.Docflow.OfficialDocument.ExecutionState.Sending;
      
      // Статус "На рассмотрении".
      if (reviewManagerAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess) ||
          reviewDraftResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess) ||
          preparingDraftResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.InProcess))
        return Sungero.Docflow.OfficialDocument.ExecutionState.OnReview;
      
      // Статус "Не требует исполнения".
      if (reviewDraftResolutionAssignments.Any(x => x.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Informed) ||
          reviewManagerAssignments.Any(x => x.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.Explored) ||
          preparingDraftResolutionAssignments.Any(x => x.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored) ||
          reviewResolutionAssignments.Any(x => x.Status == Workflow.AssignmentBase.Status.Completed &&
                                          !actionItemsInProcess.Any(y => y.ParentAssignment.Id == x.Id)))
        return Sungero.Docflow.OfficialDocument.ExecutionState.WithoutExecut;
      
      return null;
    }
    
    /// <summary>
    /// Определить может ли автор задачи готовить проекты резолюций.
    /// </summary>
    /// <returns>True - может, False - не может.</returns>
    /// <remarks>Автор задачи может готовить проект резолюции если:
    /// <para>- является помощником с таким правом как минимум для одного из адресатов;</para>
    /// <para>- замещает помощника с таким правом как минимум для одного из адресатов.</para>
    /// </remarks>
    public virtual bool CanAuthorPrepareResolution()
    {
      var addressees = _obj.Addressees
        .Select(x => x.Addressee)
        .ToList();
      var assistants = addressees
        .SelectMany(x => Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(x));
      var authorSubstitute = Sungero.Company.PublicFunctions.Module.GetUsersSubstitutedBy(_obj.Author);
      
      return assistants.Any(x => Equals(x.Assistant, _obj.Author)) || assistants.Any(x => authorSubstitute.Contains(x.Assistant));
    }
    
    /// <summary>
    /// Перенести вложения из головной задачи в задачу на рассмотрение.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    [Public]
    public virtual void SynchronizeParentTaskAttachments(Sungero.Docflow.IApprovalTask approvalTask)
    {
      if (approvalTask == null)
        return;
      
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (document != null)
      {
        if (!_obj.DocumentForReviewGroup.OfficialDocuments.Contains(document))
          _obj.DocumentForReviewGroup.OfficialDocuments.Add(document);
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      }
      
      foreach (var addInformation in approvalTask.OtherGroup.All)
        _obj.OtherGroup.All.Add(addInformation);
    }
    
    /// <summary>
    /// Синхронизировать вложения задачи в указанную задачу на рассмотрение.
    /// </summary>
    /// <param name="task">Задача на рассмотрение, в которую требуется синхронизировать вложения.</param>
    public virtual void SynchronizeAttachmentsToReviewTask(IDocumentReviewTask task)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        task.DocumentForReviewGroup.OfficialDocuments.Clear();
        task.DocumentForReviewGroup.OfficialDocuments.Add(document);
      }
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      
      foreach (var attachment in _obj.OtherGroup.All)
      {
        /* Документ может быть уже добавлен в OtherGroup.
         * Например, при добавлении входящего письма основным документом
         * в OtherGroup будет добавлено исходящее письмо, ответом на которое было входящее.
         * Повторное добавление документа вызывает ошибку #147320.
         */
        if (task.OtherGroup.All.Any(x => x.Id == attachment.Id))
          continue;
        task.OtherGroup.All.Add(attachment);
      }
    }
    
    /// <summary>
    /// Выполнить блоки мониторинга родительской задачи на согласование по регламенту.
    /// </summary>
    public virtual void ExecuteParentApprovalTaskMonitorings()
    {
      var task = _obj.ParentTask;
      if (task != null && Docflow.ApprovalTasks.Is(task))
      {
        var approvalTask = Docflow.ApprovalTasks.As(task);
        var link = Docflow.PublicFunctions.ApprovalReviewTaskStage.GetApprovalTaskExternalLink(approvalTask);
        if (link != null)
        {
          var reviewTaskId = 0;
          int.TryParse(link.ExternalEntityId, out reviewTaskId);

          if (reviewTaskId == _obj.Id)
            approvalTask.Blocks.ExecuteAllMonitoringBlocks();
        }
      }
    }
    
    /// <summary>
    /// Выполнить блоки мониторинга родительской задачи на рассмотрение документа.
    /// </summary>
    public virtual void ExecuteParentDocumentReviewTaskMonitorings()
    {
      var task = _obj.ParentTask;
      if (task != null && DocumentReviewTasks.Is(task))
      {
        var reviewTask = DocumentReviewTasks.As(task);
        if (Functions.DocumentReviewTask.AllDocumentReviewSubTasksAreCompleted(reviewTask))
        {
          Logger.DebugFormat("DocumentReviewTask(ID={0}) Call ExecuteAllMonitoringBlocks of ParentTask(ID={1})", _obj.Id, reviewTask.Id);
          reviewTask.Blocks.ExecuteAllMonitoringBlocks();
        }
      }
    }
    
    /// <summary>
    /// Проверить, выполнены ли все подчиненные задачи на рассмотрение документа.
    /// </summary>
    /// <returns>True, если все подчиненные поручения выполнены, иначе - False.</returns>
    public virtual bool AllDocumentReviewSubTasksAreCompleted()
    {
      var subTasks = DocumentReviewTasks.GetAll()
        .Where(x => Equals(x.ParentTask, _obj))
        .ToList();
      var result = subTasks.All(t => Functions.DocumentReviewTask.IsDocumentReviewTaskCompleted(t));
      Logger.DebugFormat("DocumentReviewTask(ID={0}) AllDocumentReviewSubTasksAreCompleted = {1}", _obj.Id, result);
      return result;
    }
    
    /// <summary>
    /// Получить права доступа на документ в зависимости от контекста.
    /// </summary>
    /// <returns>Права доступа на основной документ в зависимости от контекста.</returns>
    /// <remarks>Если задача на рассмотрение была запущена из согласования по регламенту, то Чтение, иначе - Изменение.</remarks>
    public virtual Guid GetCreationContextAttachmentRights()
    {
      return Docflow.ApprovalTasks.Is(_obj.MainTask) ? DefaultAccessRightsTypes.Read : DefaultAccessRightsTypes.Change;
    }
  }
}
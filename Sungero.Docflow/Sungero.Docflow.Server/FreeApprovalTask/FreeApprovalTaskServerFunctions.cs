using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.FreeApprovalTask;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class FreeApprovalTaskFunctions
  {
    #region Контрол "Состояние"
    
    /// <summary>
    /// Построить модель контрола состояния процесса согласования.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния.</returns>
    public Sungero.Core.StateView GetStateView(IElectronicDocument document)
    {
      if (_obj.ForApprovalGroup.ElectronicDocuments.Any(d => Equals(document, d)) ||
          _obj.AddendaGroup.ElectronicDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить модель состояния процесса согласования.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      // Добавить заголовок.
      var stateView = StateView.Create();
      
      var taskBeginText = _obj.Texts.OrderBy(t => t.Created).FirstOrDefault();
      var comment = taskBeginText != null ? taskBeginText.Body : string.Empty;
      comment = comment != FreeApprovalTasks.Resources.ApprovalText ? comment : string.Empty;
      if (_obj.Started.HasValue)
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, ApprovalTasks.Resources.StateViewDocumentSentForApproval, _obj.Started.Value, _obj, comment, _obj.StartedBy);
      else
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, ApprovalTasks.Resources.StateViewTaskDrawCreated, _obj.Created.Value, _obj, comment, _obj.Author);
      
      // Добавить основной блок для задачи.
      var taskBlock = this.AddTaskBlock(stateView);
      
      // Получить все задания по задаче.
      var taskAssignments = Assignments.GetAll().Where(a => Equals(a.Task, _obj)).ToList();
      
      // Определить итерации.
      var iterations = Functions.Module.GetIterationDates(_obj);
      
      // Обработать каждую итерацию.
      foreach (var iteration in iterations)
      {
        var date = iteration.Date;
        var hasReworkBefore = iteration.IsRework;
        var hasRestartBefore = iteration.IsRestart;
        
        var nextIteration = iterations.Where(d => d.Date > date).FirstOrDefault();
        var nextDate = Calendar.Now;
        
        if (nextIteration != null)
        {
          nextDate = nextIteration.Date;
        }
        
        // Получить задания в рамках круга согласования.
        var assignments = taskAssignments
          .Where(a => a.Created >= date && a.Created < nextDate)
          .OrderBy(a => a.Created)
          .ToList();
        
        // Если нет заданий, то перейти к следующей итерации.
        if (!assignments.Any())
          continue;
        
        // Добавить блок отправки.
        this.AddSendForApprovalBlock(taskBlock, assignments.First(), hasReworkBefore, hasRestartBefore);
        
        StateBlock parentBlock = null;
        
        // Выделить этап согласования.
        var approvalAssignments = assignments.Where(a => FreeApprovalAssignments.Is(a));
        if (approvalAssignments.Any())
        {
          // Вставить блок для группировки этапа согласования.
          var iterationBlock = taskBlock.AddChildBlock();
          iterationBlock.NeedGroupChildren = true;
          iterationBlock.IsExpanded = approvalAssignments.Any(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
          
          // Заголовок блока итерации.
          iterationBlock.AddLabel(ApprovalTasks.Resources.StateViewApprovalStage, Docflow.PublicFunctions.Module.CreateHeaderStyle());
          iterationBlock.AddLineBreak();
          
          // Добавить информацию по исполнителям группы согласования.
          var performersLabel = string.Join(", ", approvalAssignments
                                            .Select(a => a.Performer)
                                            .Distinct()
                                            .Select(a => (Sungero.Company.Employees.Is(a) ?
                                                          Company.PublicFunctions.Employee.GetShortName(Sungero.Company.Employees.As(a), false) :
                                                          a.Name)));
          iterationBlock.AddLabel(performersLabel, Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
          
          parentBlock = iterationBlock;
        }
        
        // Добавить задания.
        assignments = assignments.OrderByDescending(a => a.Result.HasValue).ThenBy(a => a.Completed).ToList();
        this.AddAssignmentsBlocks(parentBlock, taskBlock, assignments);
        
        // Установить иконку и статус для группировки этапа согласования.
        var endStatus = string.Empty;
        if (parentBlock != null)
        {
          // Иконка по умолчанию.
          parentBlock.AssignIcon(FreeApprovalTasks.Resources.FreeApproveStage, StateBlockIconSize.Large);
          
          if (approvalAssignments.Any(a => a.Result == Docflow.FreeApprovalAssignment.Result.ForRework))
          {
            parentBlock.AssignIcon(ApprovalTasks.Resources.Notapprove, StateBlockIconSize.Large);
            endStatus = ApprovalTasks.Resources.StateViewNotApproved;
          }
          else if (!approvalAssignments.Any(a => a.Result != Docflow.FreeApprovalAssignment.Result.Approved))
          {
            parentBlock.AssignIcon(ApprovalTasks.Resources.Approve, StateBlockIconSize.Large);
            endStatus = ApprovalTasks.Resources.StateViewEndorsed;
          }
          
          if (assignments.Any(a => a.Status == Workflow.AssignmentBase.Status.Aborted))
          {
            parentBlock.AssignIcon(StateBlockIconType.Abort, StateBlockIconSize.Large);
            endStatus = ApprovalTasks.Resources.StateViewAborted;
          }
          
          Functions.Module.AddInfoToRightContent(parentBlock, parentBlock.IsExpanded ? ApprovalTasks.Resources.StateViewInProcess : endStatus);
        }
      }
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить блок задачи согласования.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock AddTaskBlock(Sungero.Core.StateView stateView)
    {
      var taskBlock = stateView.AddBlock();
      
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var noteStyle = Functions.Module.CreateNoteStyle(isDraft);
      
      taskBlock.Entity = _obj;
      taskBlock.AssignIcon(OfficialDocuments.Info.Actions.SendForFreeApproval, StateBlockIconSize.Large);
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      taskBlock.AddLabel(FreeApprovalTasks.Resources.StateViewFreeApproval, headerStyle);
      taskBlock.AddLineBreak();
      var deadline = _obj.MaxDeadline.HasValue ?
        Functions.Module.ToShortDateShortTime(_obj.MaxDeadline.Value.ToUserTime()) :
        OfficialDocuments.Resources.StateViewWithoutTerm;

      taskBlock.AddLabel(string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDeadline, deadline), noteStyle);
      
      this.AddStatusInfoToRight(taskBlock, labelStyle);
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить блок отправки на круг согласования.
    /// </summary>
    /// <param name="parentBlock">Родительский блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="hasReworkBefore">После доработки.</param>
    /// <param name="hasRestartBefore">После рестарта.</param>
    public void AddSendForApprovalBlock(Sungero.Core.StateBlock parentBlock, IAssignment assignment, bool hasReworkBefore, bool hasRestartBefore)
    {
      if (!hasReworkBefore && !hasRestartBefore)
        return;
      
      var comment = Functions.Module.GetAssignmentUserComment(assignment);
      var textTemplate = ApprovalTasks.Resources.StateViewDocumentSentForReApproval;
      
      if (hasRestartBefore)
      {
        comment = Functions.Module.GetTaskUserComment(_obj, assignment.Created.Value, FreeApprovalTasks.Resources.ApprovalText);
        textTemplate = ApprovalTasks.Resources.StateViewDocumentSentAfterRestart;
      }
      
      Docflow.PublicFunctions.OfficialDocument
        .AddUserActionBlock(parentBlock, _obj.Author, textTemplate, assignment.Modified.Value, _obj, comment, _obj.StartedBy);
    }
    
    /// <summary>
    /// Добавить задания свободного согласования.
    /// </summary>
    /// <param name="parentBlock">Блок группировки.</param>
    /// <param name="taskBlock">Блок задачи.</param>
    /// <param name="assignments">Список заданий в рамках этапа согласования.</param>
    public void AddAssignmentsBlocks(Sungero.Core.StateBlock parentBlock, Sungero.Core.StateBlock taskBlock, List<IAssignment> assignments)
    {
      foreach (var assignment in assignments)
      {
        var noteStyle = Functions.Module.CreateNoteStyle();
        var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
        var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
        
        var performerAndDeadlineAndStatus = this.GetPerformerAndDeadlineAndStatus(assignment);
        var performer = performerAndDeadlineAndStatus.Performer;
        var deadline = performerAndDeadlineAndStatus.Deadline;
        var status = performerAndDeadlineAndStatus.Status;
        
        if (string.IsNullOrWhiteSpace(performer))
          continue;

        StateBlock block;
        if (parentBlock != null && FreeApprovalAssignments.Is(assignment))
          block = parentBlock.AddChildBlock();
        else
          block = taskBlock.AddChildBlock();

        this.SetIcon(block, assignment);
        
        block.Entity = assignment;
        
        // Заголовок блока с заданием о завершении или доработке.
        if (!FreeApprovalAssignments.Is(assignment))
        {
          var blockLabel = string.Empty;
          if (FreeApprovalFinishAssignments.Is(assignment))
            blockLabel = FreeApprovalTasks.Resources.StateViewCompleteApprovalAssignment;
          else if (FreeApprovalReworkAssignments.Is(assignment))
            blockLabel = FreeApprovalTasks.Resources.StateViewReworkByRemarks;
          
          block.AddLabel(blockLabel, Docflow.PublicFunctions.Module.CreateHeaderStyle());
          block.AddLineBreak();
        }
        
        // Заполнить участников.
        var performerLabel = string.Format("{0}{1}", performer, deadline);
        block.AddLabel(performerLabel, performerDeadlineStyle);
        
        var comment = Docflow.Functions.Module.GetAssignmentUserComment(assignment);
        if (!string.IsNullOrWhiteSpace(comment))
        {
          block.AddLineBreak();
          block.AddLabel(Constants.Module.SeparatorText, separatorStyle);
          block.AddLineBreak();
          block.AddEmptyLine(Constants.Module.EmptyLineMargin);
          block.AddLabel(comment, noteStyle);
        }
        
        // Заполнить статус задания и просрочку.
        Functions.Module.AddInfoToRightContent(block, status);
        
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.Deadline.HasValue)
          Functions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      }
    }
    
    /// <summary>
    /// Установить иконку задания.
    /// </summary>
    /// <param name="block">Блок задания.</param>
    /// <param name="assignment">Задание.</param>
    public void SetIcon(Sungero.Core.StateBlock block, IAssignment assignment)
    {
      // Размер иконок: большой, если не задание согласования.
      var iconSize = StateBlockIconSize.Large;
      if (FreeApprovalAssignments.Is(assignment))
        iconSize = StateBlockIconSize.Small;
      
      block.AssignIcon(StateBlockIconType.OfEntity, iconSize);

      if (assignment.Result != null)
      {
        // Согласовано.
        if (assignment.Result == Docflow.FreeApprovalAssignment.Result.Approved)
        {
          iconSize = StateBlockIconSize.Small;
          block.AssignIcon(ApprovalTasks.Resources.Approve, iconSize);
        }
        
        // На доработку.
        if (assignment.Result == Docflow.FreeApprovalAssignment.Result.ForRework)
        {
          iconSize = StateBlockIconSize.Small;
          block.AssignIcon(ApprovalTasks.Resources.Notapprove, iconSize);
        }
        
        // На повторное согласование.
        if (assignment.Result == Docflow.FreeApprovalReworkAssignment.Result.Reworked)
        {
          block.AssignIcon(StateBlockIconType.User, iconSize);
          block.ShowBorder = false;
          block.DockType = DockType.Bottom;
        }
        
        // Выполнено.
        if (assignment.Result == Docflow.FreeApprovalFinishAssignment.Result.Complete)
        {
          block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);
        }
        
        // Переадресовано.
        if (assignment.Result == Docflow.FreeApprovalAssignment.Result.Forward)
        {
          block.AssignIcon(FreeApprovalTasks.Resources.Forward, iconSize);
        }
        
        // Прекращено.
        if (assignment.Status == Workflow.AssignmentBase.Status.Aborted)
        {
          block.AssignIcon(StateBlockIconType.Abort, iconSize);
        }
      }
      
      // Прекращено, остановлено по ошибке.
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
    }
    
    /// <summary>
    /// Добавить статус согласования.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="style">Стиль.</param>
    public void AddStatusInfoToRight(Sungero.Core.StateBlock block, Sungero.Core.StateBlockLabelStyle style)
    {
      var status = string.Empty;
      if (_obj.Status == Workflow.Task.Status.InProcess)
        status = ApprovalTasks.Resources.StateViewInProcess;
      else if (_obj.Status == Workflow.Task.Status.Completed)
        status = ApprovalTasks.Resources.StateViewCompleted;
      else if (_obj.Status == Workflow.Task.Status.Aborted)
        status = ApprovalTasks.Resources.StateViewAborted;
      else if (_obj.Status == Workflow.Task.Status.Suspended)
        status = ApprovalTasks.Resources.StateViewSuspended;
      else if (_obj.Status == Workflow.Task.Status.Draft)
        status = ApprovalTasks.Resources.StateViewDraft;
      
      Functions.Module.AddInfoToRightContent(block, status, style);
    }
    
    /// <summary>
    /// Получить заголовок.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Информация о задании: исполнитель, срок, результат - отформатированная для предметного отображения.</returns>
    public Structures.FreeApprovalTask.AssignmentInfo GetPerformerAndDeadlineAndStatus(IAssignment assignment)
    {
      var performerName = PublicFunctions.OfficialDocument.GetAuthor(assignment.Performer, assignment.CompletedBy);
      var actionLabel = string.Empty;
      var emptyResult = Structures.FreeApprovalTask.AssignmentInfo.Create(string.Empty, string.Empty, string.Empty);
      
      // Задание завершено.
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed)
      {
        // Согласование.
        if (FreeApprovalAssignments.Is(assignment))
        {
          if (assignment.Result == Docflow.FreeApprovalAssignment.Result.Approved)
            actionLabel = ApprovalTasks.Resources.StateViewEndorsed;
          else if (assignment.Result == Docflow.FreeApprovalAssignment.Result.ForRework)
            actionLabel = ApprovalTasks.Resources.StateViewNotApproved;
          else if (assignment.Result == Docflow.FreeApprovalAssignment.Result.Forward)
            actionLabel = ApprovalTasks.Resources.StateViewForwarded;
          else
            return emptyResult;
        }
        
        // Задание на завершение согласования.
        if (FreeApprovalFinishAssignments.Is(assignment))
        {
          if (assignment.Result == Docflow.FreeApprovalFinishAssignment.Result.Complete)
            actionLabel = ApprovalTasks.Resources.StateViewDone;
          else
            return emptyResult;
        }
        
        // Прекращение на доработке.
        if (FreeApprovalReworkAssignments.Is(assignment) && assignment.Status == Workflow.AssignmentBase.Status.Aborted)
        {
          actionLabel = ApprovalTasks.Resources.StateViewAborted;
        }
        
        var completed = Functions.Module.ToShortDateShortTime(assignment.Completed.Value.ToUserTime());
        
        if (!string.IsNullOrWhiteSpace(actionLabel))
          return Structures.FreeApprovalTask.AssignmentInfo.Create(string.Format("{0} ", performerName), string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDate, completed), actionLabel);
      }
      
      // Задание в работе или прекращено.
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess ||
          assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        var status = ApprovalTasks.Resources.StateViewAborted.ToString();
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess)
          status = assignment.IsRead == true ? ApprovalTasks.Resources.StateViewInProcess : ApprovalTasks.Resources.StateViewUnRead;
        
        var deadline = assignment.Deadline.HasValue ?
          Functions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime()) :
          OfficialDocuments.Resources.StateViewWithoutTerm;
        
        return Structures.FreeApprovalTask.AssignmentInfo.Create(string.Format("{0} ", performerName), string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDeadline, deadline), status);
      }
      
      return emptyResult;
    }
    
    #endregion
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.ForApprovalGroup.ElectronicDocuments.SingleOrDefault();
      if (primaryDocument == null)
        return;
      
      var primaryDocumentAddenda = Functions.Module.GetAddenda(primaryDocument);
      var taskAddenda = _obj.AddendaGroup.ElectronicDocuments.Where(x => !Equals(x, primaryDocument));
      var notRelatedToPrimaryDocumentTaskAddenda = taskAddenda.Except(primaryDocumentAddenda);
      foreach (var addendum in notRelatedToPrimaryDocumentTaskAddenda)
      {
        var addendumIsAlreadyRelatedToThePrimary = Sungero.Content.DocumentRelations.GetAll()
          .Any(x => Equals(x.Source, primaryDocument) && Equals(x.Target, addendum) ||
               Equals(x.Source, addendum) && Equals(x.Target, primaryDocument));
        var addendumAsAddendum = Addendums.As(addendum);
        if (addendumIsAlreadyRelatedToThePrimary ||
            addendumAsAddendum != null && !Equals(addendumAsAddendum.LeadingDocument, primaryDocument))
          continue;
        
        var relationAdded = addendum.Relations.AddFrom(Docflow.PublicConstants.Module.AddendumRelationName, primaryDocument);
        addendum.Relations.Save();
        if (relationAdded)
          Logger.DebugFormat("FreeApprovalTask (ID = {0}). Success: add relation with type Addendum from primary document (ID = {1}) to addendum (ID = {2})",
                             _obj.Id, primaryDocument.Id, addendum.Id);
        else
          Logger.DebugFormat("FreeApprovalTask (ID = {0}). Failed: add relation with type Addendum from primary document (ID = {1}) to addendum (ID = {2})",
                             _obj.Id, primaryDocument.Id, addendum.Id);
      }
    }
    
    /// <summary>
    /// Получить список операций по всем операциям, относящимся к данной группе вложений из истории.
    /// </summary>
    /// <param name="groupId">ИД группы вложений.</param>
    /// <returns>Список, содержащий историю операций по данной группе вложений.</returns>
    [Remote]
    public virtual Structures.Module.AttachmentHistoryEntries GetAttachmentHistoryEntriesByGroupId(Guid groupId)
    {
      return Docflow.Functions.Module.GetAttachmentHistoryEntriesByGroupId(_obj, groupId);
    }
    
    #endregion
    
    /// <summary>
    /// Раскрывание групп в пользователей с дублями.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    /// <returns>Список реципиентов.</returns>
    public static List<IRecipient> GetUsersFromGroups(IRecipient recipient)
    {
      var sourceGroup = Groups.As(recipient);
      var recipientList = new List<IRecipient>();
      
      if (sourceGroup == null)
      {
        recipientList.Add(recipient);
      }
      else
      {
        var users = Roles.GetAllUsersInGroup(sourceGroup).Where(x => x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active && x.Login != null && x.IsSystem != true);
        foreach (var user in users)
          recipientList.Add(user);
      }
      
      return recipientList;
    }
    
    /// <summary>
    /// Получить номер последней итерации заданий на согласование.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Номер итерации.</returns>
    public static int? GetApprovalAssignmentLastIterationId(IFreeApprovalTask task)
    {
      return FreeApprovalAssignments.GetAll(asg => asg.Task.Equals(task) && asg.TaskStartId == task.StartId).Select(asg => asg.IterationId).Max();
    }

    /// <summary>
    /// Получить все завершенные задания последней итерации с результатом на доработку, для которых не созданы уведомления.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="lastIterationId">ИД последней итерации.</param>
    /// <returns>Список заданий.</returns>
    public static List<IFreeApprovalAssignment> GetLastAssignmentWithoutNotice(IFreeApprovalTask task, int? lastIterationId)
    {
      return FreeApprovalAssignments.GetAll(asg => asg.Task.Equals(task) &&
                                            asg.TaskStartId == task.StartId &&
                                            asg.IterationId == lastIterationId &&
                                            asg.Status == Sungero.Workflow.AssignmentBase.Status.Completed &&
                                            asg.Result == Sungero.Docflow.FreeApprovalAssignment.Result.ForRework &&
                                            !FreeApprovalNotifications.GetAll().Any(n => Equals(n.LinkedFreeApprovalAssignment, asg))).ToList();
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="performers">Исполнители.</param>
    public static void GrantRightForAttachmentsToPerformers(IFreeApprovalTask task, List<IRecipient> performers)
    {
      foreach (var performer in performers)
      {
        // На основной документ - на изменение.
        var approvalDocument = task.ForApprovalGroup.ElectronicDocuments.First();
        if (!approvalDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
          approvalDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
        
        // На приложения - на изменение, но не выше, чем у инициатора.
        foreach (var document in task.AddendaGroup.ElectronicDocuments)
        {
          if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
            continue;
          
          var rightType = document.AccessRights.CanUpdate(task.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
          document.AccessRights.Grant(performer, rightType);
        }
      }
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var freeApprovalTask = FreeApprovalTasks.As(_obj);
      if (freeApprovalTask == null)
        return assignees;
      
      assignees.AddRange(freeApprovalTask.Approvers.Where(a => a.Approver != null).Select(a => a.Approver));
      
      var reworkAssignment = FreeApprovalReworkAssignments
        .GetAll(asg => Equals(asg.Task, _obj) && asg.TaskStartId == _obj.StartId)
        .OrderByDescending(asg => asg.Created)
        .FirstOrDefault();
      if (reworkAssignment != null)
      {
        assignees.AddRange(reworkAssignment.Approvers.Where(a => a.Approver != null).Select(a => a.Approver));
      }

      assignees.AddRange(freeApprovalTask.Observers.Where(a => a.Observer != null).Select(a => a.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Добавить в грид задания на доработке нового сотрудника из задания.
    /// </summary>
    /// <param name="e">Обработчик блока.</param>
    /// <param name="assignment">Задание, которое надо обработать.</param>
    public static void AddToReworkAssigneeNewApprover(Sungero.Docflow.Server.FreeApprovalReworkAssignmentArguments e,
                                                      Sungero.Docflow.IFreeApprovalAssignment assignment)
    {
      var newApprover = e.Block.Approvers.FirstOrDefault(a => Equals(a.Approver, assignment.Performer)) ?? e.Block.Approvers.AddNew();
      newApprover.Approver = Sungero.Company.Employees.As(assignment.Performer);
      if (newApprover.Approver == null)
      {
        e.Block.Approvers.Remove(newApprover);
        return;
      }
      
      if (assignment.Result == Sungero.Docflow.FreeApprovalAssignment.Result.Approved)
      {
        newApprover.Approved = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Approved.IsApproved;
        newApprover.Action = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendNotice;
      }
      else if (assignment.Result == Sungero.Docflow.FreeApprovalAssignment.Result.Forward)
      {
        newApprover.Approved = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Approved.NotApproved;
        newApprover.Action = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.DoNotSend;
      }
      else
      {
        newApprover.Approved = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Approved.NotApproved;
        newApprover.Action = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendForApproval;
      }
    }
  }
}
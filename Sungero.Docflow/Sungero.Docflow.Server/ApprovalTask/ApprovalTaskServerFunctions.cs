using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.ApprovalTask;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Workflow;
using ExchDocumentType = Sungero.Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType;
using ReviewResults = Sungero.Docflow.ApprovalReviewAssignment.Result;

namespace Sungero.Docflow.Server
{
  partial class ApprovalTaskFunctions
  {
    #region Контрол "Состояние"
    
    /// <summary>
    /// Получить список заданий по задаче.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список заданий.</returns>
    [Remote]
    public static List<IAssignment> GetTaskAssigments(ITask task)
    {
      return Assignments.GetAll(x => Equals(x.Task, task)).ToList();
    }
    
    /// <summary>
    /// Построить модель контрола состояния документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.DocumentGroup.OfficialDocuments.Any(d => Equals(document, d)) ||
          _obj.AddendaGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Заполнить блок "Выполнение сценария".
    /// </summary>
    /// <param name="stage">Этап регламента.</param>
    /// <param name="taskBlock">Блок задачи.</param>
    public virtual void SetFunctionBlockProperties(IApprovalRuleBaseStages stage, Sungero.Core.StateBlock taskBlock)
    {
      var functionStageBase = Docflow.ApprovalFunctionStageBases.As(stage.StageBase);
      
      if (functionStageBase == null)
        return;
      
      var functionStatusInfo = Structures.ApprovalRuleBase.StageStatusInfo.Create(false, true, false);
      
      var functionBlock = taskBlock.AddChildBlock();
      functionBlock.AssignIcon(ApprovalTasks.Resources.Function, StateBlockIconSize.Large);

      var blockName = Docflow.Functions.ApprovalFunctionStageBase.GetStateViewBlockName(functionStageBase, _obj, functionStatusInfo);
      functionBlock.AddLabel(blockName, Docflow.PublicFunctions.Module.CreateHeaderStyle());
      
      functionBlock.AddLineBreak();
      
      Docflow.Functions.ApprovalFunctionStageBase.AddStateViewBlockPerformers(functionStageBase, _obj, functionBlock, functionStatusInfo);
      
      functionBlock.AddLineBreak();
      
      var deadlineDescription = Docflow.Functions.ApprovalFunctionStageBase.GetStateViewBlockDeadline(functionStageBase, _obj, functionStatusInfo);
      
      if (!string.IsNullOrEmpty(deadlineDescription))
        functionBlock.AddLabel(deadlineDescription, Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
      
      Functions.Module.AddInfoToRightContent(functionBlock, ApprovalTasks.Resources.StateViewInProcess);
      
      if (functionStageBase.DeadlineInDays != null || functionStageBase.DeadlineInHours != null)
      {
        var deadline = Docflow.Functions.ApprovalStageBase.GetStageMaxDeadline(stage.StageBase, _obj, Calendar.Now, true);
        Functions.OfficialDocument.AddDeadlineHeaderToRight(functionBlock, deadline, null);
      }
    }
    
    /// <summary>
    /// Заполнить блок "задание на контроль возврата".
    /// </summary>
    /// <param name="stage">Этап регламента.</param>
    /// <param name="taskBlock">Блок задачи.</param>
    public virtual void SetApprovalCheckReturnBlockProperties(IApprovalRuleBaseStages stage, Sungero.Core.StateBlock taskBlock)
    {
      var delay = stage.Stage.StartDelayDays;
      if (delay.HasValue && delay > 0)
      {
        var activeReturnAssignments = ApprovalCheckReturnAssignments.GetAll(a => Equals(a.Task, _obj) &&
                                                                            a.Status == Workflow.AssignmentBase.Status.InProcess);
        if (!activeReturnAssignments.Any())
        {
          var block = taskBlock.AddChildBlock();
          block.AssignIcon(ApprovalTasks.Resources.WaitControl, StateBlockIconSize.Large);
          block.AddLabel(ApprovalTasks.Resources.StateViewWaitForCheckReturn, Docflow.PublicFunctions.Module.CreateHeaderStyle());
          block.AddLineBreak();
          block.AddLabel(string.Format("{0}: {1}",
                                       ApprovalTasks.Resources.StateViewAssignmentCreationTerms,
                                       Functions.Module.ToShortDateShortTime(_obj.ControlReturnStartDate.Value.ToUserTime())),
                         Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
          
          Functions.Module.AddInfoToRightContent(block, ApprovalTasks.Resources.StateViewInProcess);
        }
      }
    }
    
    /// <summary>
    /// Построить модель контрола состояния задачи на согласование по регламенту.
    /// </summary>
    /// <returns>Модель контрола состояния задачи на согласование по регламенту.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      // Добавить заголовок отправки для стартованной задачи.
      var stateView = StateView.Create();
      
      var iterations = Functions.Module.GetIterationDates(_obj);
      
      var comment = Functions.Module.GetTaskUserComment(_obj, ApprovalTasks.Resources.ApprovalText);
      
      var startedByUser = Sungero.CoreEntities.Users.As(Sungero.Workflow.WorkflowHistories.GetAll()
                                                        .Where(h => h.EntityId == _obj.Id)
                                                        .Where(h => h.Operation == Sungero.Workflow.WorkflowHistory.Operation.Start)
                                                        .OrderBy(h => h.HistoryDate)
                                                        .Select(h => h.User)
                                                        .FirstOrDefault());
      
      if (_obj.Started.HasValue)
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, ApprovalTasks.Resources.StateViewDocumentSentForApproval, _obj.Started.Value, _obj, comment, startedByUser);
      else
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, ApprovalTasks.Resources.StateViewTaskDrawCreated, _obj.Created.Value, _obj, comment, _obj.Author);
      
      // Добавить основной блок для задачи.
      var taskBlock = this.AddTaskBlock(stateView);
      
      // Получить все задания по задаче.
      var taskAssignments = Assignments.GetAll(a => Equals(a.Task, _obj)).OrderBy(a => a.Created).ToList();
      
      foreach (var iteration in iterations)
      {
        var date = iteration.Date;
        var hasReworkBefore = iteration.IsRework;
        var hasRestartBefore = iteration.IsRestart;
        
        var nextIteration = iterations.Where(d => d.Date > date).FirstOrDefault();
        var nextDate = Calendar.Now;
        var hasRestartAfter = false;
        
        var isLastRound = nextIteration == null;
        if (!isLastRound)
        {
          nextDate = nextIteration.Date;
          hasRestartAfter = nextIteration.IsRestart;
        }
        
        // Получить задания в рамках круга согласования.
        var iterationAssignments = taskAssignments
          .Where(a => a.Created >= date && a.Created < nextDate)
          .OrderBy(a => a.Created)
          .ToList();
        
        if (!iterationAssignments.Any())
          continue;
        
        var firstAssignment = iterationAssignments.First();
        if (hasReworkBefore)
        {
          var reworkComment = Functions.Module.GetAssignmentUserComment(firstAssignment);
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(taskBlock, firstAssignment.Performer, ApprovalTasks.Resources.StateViewDocumentSentForReApproval,
                                firstAssignment.Completed.Value, _obj, reworkComment, firstAssignment.CompletedBy);
        }
        
        if (hasRestartBefore)
        {
          var restartComment = Functions.Module.GetTaskUserComment(_obj, firstAssignment.Created.Value, ApprovalTasks.Resources.ApprovalText);
          
          var restartedByUser = Sungero.CoreEntities.Users.As(Sungero.Workflow.WorkflowHistories.GetAll()
                                                              .Where(h => h.EntityId == firstAssignment.Task.Id)
                                                              .Where(h => h.HistoryDate.Between(date, nextDate))
                                                              .Where(h => h.Operation == Sungero.Workflow.WorkflowHistory.Operation.Restart)
                                                              .Select(h => h.User)
                                                              .FirstOrDefault());
          
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(taskBlock, firstAssignment.Author, ApprovalTasks.Resources.StateViewDocumentSentAfterRestart,
                                _obj.Started.Value, _obj, restartComment, restartedByUser);
        }
        
        if (!isLastRound)
        {
          // Добавить блок группировки для круга согласования.
          var roundBlock = taskBlock.AddChildBlock();
          roundBlock.AddLabel(ApprovalTasks.Resources.StateViewApprovalRound, Functions.Module.CreateStyle(true, true));
          roundBlock.AssignIcon(ApprovalTasks.Resources.OldApprove, StateBlockIconSize.Large);
          
          var roundStatus = hasRestartAfter ? ApprovalTasks.Resources.StateViewAborted : ApprovalTasks.Resources.StateViewNotApproved;
          Functions.Module.AddInfoToRightContent(roundBlock, roundStatus);
          
          this.AddAssignmentsBlocks(roundBlock, taskAssignments, iterationAssignments, StateBlockIconSize.Small);
        }
        else
          this.AddAssignmentsBlocks(taskBlock, taskAssignments, iterationAssignments, StateBlockIconSize.Large);
      }
      
      if (_obj.Status == Workflow.Task.Status.InProcess)
      {
        var checkReturnStage = _obj.ApprovalRule.Stages.FirstOrDefault(s => s.Number == _obj.StageNumber && s.StageType == Docflow.ApprovalStage.StageType.CheckReturn);
        if (checkReturnStage != null)
          this.SetApprovalCheckReturnBlockProperties(checkReturnStage, taskBlock);
        
        var functionStage = _obj.ApprovalRule.Stages.FirstOrDefault(s => s.Number == _obj.StageNumber && s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function);
        if (functionStage != null)
          this.SetFunctionBlockProperties(functionStage, taskBlock);
      }

      return stateView;
    }
    
    /// <summary>
    /// Добавить в контрол состояния блок задачи на согласование.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Добавленный блок.</returns>
    private StateBlock AddTaskBlock(StateView stateView)
    {
      var taskBlock = stateView.AddBlock();
      
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      
      taskBlock.Entity = _obj;
      taskBlock.AssignIcon(OfficialDocuments.Info.Actions.SendForApproval, StateBlockIconSize.Large);
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      taskBlock.AddLabel(ApprovalTasks.Resources.Approval, headerStyle);
      taskBlock.AddLineBreak();
      taskBlock.AddLabel(ApprovalTasks.Resources.StateViewApprovalRule, labelStyle);
      taskBlock.AddHyperlink(_obj.ApprovalRule.Name, Hyperlinks.Get(_obj.ApprovalRule));
      if ((_obj.Status == Workflow.Task.Status.InProcess || _obj.Status == Workflow.Task.Status.Draft) && _obj.MaxDeadline.HasValue)
      {
        var deadline = Functions.Module.ToShortDateShortTime(_obj.MaxDeadline.Value.ToUserTime());
        taskBlock.AddLabel(string.Format(" {0}: {1}", ApprovalTasks.Resources.StateViewExpectedDeadline, deadline), labelStyle);
      }
      
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
      
      Functions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить в блок контрола состояния блоки заданий.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="taskAssignments">Все задания по задаче.</param>
    /// <param name="roundAssignments">Задания в рамках круга согласования.</param>
    /// <param name="iconSize">Размер иконки.</param>
    private void AddAssignmentsBlocks(StateBlock block,
                                      List<IAssignment> taskAssignments,
                                      List<IAssignment> roundAssignments,
                                      StateBlockIconSize iconSize)
    {
      // Блок группировки согласования.
      var approvalAssignmentList = new List<IAssignment>();
      roundAssignments = roundAssignments.OrderBy(a => a.Id).ToList();
      foreach (var assignment in roundAssignments)
      {
        // Признак прекращенного конкурентного задания по контролю возврата. Только при условии, что есть хоть одно выполненное задание.
        var isCompletedAbortedControl = ApprovalCheckReturnAssignments.Is(assignment) &&
          (roundAssignments.Any(a => ApprovalCheckReturnAssignments.Is(a) && a.Status == Workflow.AssignmentBase.Status.Completed) &&
           assignment.Status == Workflow.AssignmentBase.Status.Aborted);
        
        // Для согласований добавить группировочный блок.
        var isApprovalBlock = ApprovalAssignments.Is(assignment) || ApprovalManagerAssignments.Is(assignment);
        if (isApprovalBlock)
          approvalAssignmentList.Add(assignment);
        else if (!isCompletedAbortedControl)
          this.AddAssignmentBlock(block, assignment, false, false, iconSize);
        
        // Следующее задание.
        var nextAssignments = taskAssignments.Where(a => a.Created >= assignment.Created && a.Id > assignment.Id);
        var nextAssignment = nextAssignments.OrderBy(a => a.Created).FirstOrDefault();
        
        // Завершить формирование группы согласования.
        var nextAssignmentIsNotApproval = nextAssignment == null || !roundAssignments.Contains(nextAssignment) ||
          (!ApprovalAssignments.Is(nextAssignment) && !ApprovalManagerAssignments.Is(nextAssignment));
        if (nextAssignmentIsNotApproval && isApprovalBlock)
        {
          this.AddApprovalStageBlocks(block, approvalAssignmentList, nextAssignment, iconSize);
          approvalAssignmentList.Clear();
        }
      }
    }
    
    /// <summary>
    /// Добавить в блок контрола состояния дочерний блок группировки этапа согласования.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="assignments">Задания этапа согласования.</param>
    /// <param name="nextAssignment">Следующее задание.</param>
    /// <param name="iconSize">Размер иконки.</param>
    private void AddApprovalStageBlocks(StateBlock block, List<IAssignment> assignments, IAssignment nextAssignment, StateBlockIconSize iconSize)
    {
      if (!assignments.Any())
        return;
      
      // Добавить блок группировки этапа согласования.
      var approvalStageBlock = block.AddChildBlock();
      approvalStageBlock.NeedGroupChildren = true;
      approvalStageBlock.AddLabel(ApprovalTasks.Resources.StateViewApprovalStage, Docflow.PublicFunctions.Module.CreateHeaderStyle());
      approvalStageBlock.AddLineBreak();
      approvalStageBlock.IsExpanded = assignments.Any(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
      
      // Добавить информацию по исполнителям группы согласования.
      var performersLabel = string.Join(", ", assignments.Select(a => Company.PublicFunctions.Employee.GetShortName(Employees.As(a.Performer), false)));
      approvalStageBlock.AddLabel(performersLabel, Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
      
      // Установить иконку для группы и добавить статус.
      var hasAbort = assignments.Any(a => a.Status == Workflow.AssignmentBase.Status.Aborted || a.Status == Workflow.AssignmentBase.Status.Suspended);
      var isApproved = assignments.All(a => a.Result == Docflow.ApprovalAssignment.Result.Approved || a.Result == Docflow.ApprovalAssignment.Result.Forward ||
                                       a.Result == Docflow.ApprovalAssignment.Result.WithSuggestions);
      var hasRework = assignments.Any(a => a.Result == Docflow.ApprovalAssignment.Result.ForRevision);
      
      var lastAssignment = assignments.OrderByDescending(a => a.Created).First();
      var taskAbortHistories = WorkflowHistories.GetAll()
        .Where(h => h.EntityId == _obj.Id &&
               h.Operation == Sungero.Workflow.WorkflowHistory.Operation.Abort &&
               h.HistoryDate >= lastAssignment.Created);
      var hasTaskAbort = nextAssignment == null ?
        taskAbortHistories.Any() :
        taskAbortHistories.Any(h => h.HistoryDate <= nextAssignment.Created);
      
      var status = this.SetGroupIconAndGetGroupStatus(approvalStageBlock, isApproved, hasRework, nextAssignment, hasAbort, hasTaskAbort, iconSize);
      Functions.Module.AddInfoToRightContent(approvalStageBlock, status);
      
      // Добавить задания этапа.
      var orderedAssignments = assignments.OrderByDescending(a => a.Result.HasValue).ThenBy(a => a.Completed);
      foreach (var assignment in orderedAssignments)
      {
        this.AddAssignmentBlock(approvalStageBlock, assignment, true, false, iconSize);
      }
    }
    
    /// <summary>
    /// Добавить в блок контрола состояния дочерний блок задания.
    /// </summary>
    /// <param name="parentBlock">Ведущий блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="isApprovalBlock">Признак: согласование или нет.</param>
    /// <param name="isResolutionBlock">Признак: блок резолюции или нет.</param>
    /// <param name="iconSize">Размер иконки.</param>
    /// <returns>Блок с заданием.</returns>
    private StateBlock AddAssignmentBlock(StateBlock parentBlock, IAssignment assignment, bool isApprovalBlock, bool isResolutionBlock, StateBlockIconSize iconSize)
    {
      // Добавить отдельный блок резолюции для внесения результата рассмотрения или схлопнутых заданий.
      var needAddResolution = false;
      if (!isResolutionBlock && ApprovalReviewAssignments.Is(assignment) &&
          (assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.Informed ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddActionItem ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.Abort))
      {
        var reviewAssignment = ApprovalReviewAssignments.As(assignment);
        if (reviewAssignment.CollapsedStagesTypesRe.Count > 1)
        {
          needAddResolution = true;
        }
        else
        {
          // Для не схлопнутого задания рассмотрения заменить его на резолюцию.
          isResolutionBlock = true;
          needAddResolution = false;
        }
      }
      
      // Стили.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle();
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      
      // Исполнитель, срок, статус.
      var performerAndDeadlineAndStatus = this.GetPerformerAndDeadlineAndStatus(assignment, isResolutionBlock);
      var performer = performerAndDeadlineAndStatus.PerformerShortName;
      var deadline = performerAndDeadlineAndStatus.Deadline;
      var status = performerAndDeadlineAndStatus.Status;
      if (string.IsNullOrWhiteSpace(performer))
        return null;

      // Добавить блок.
      var block = parentBlock.AddChildBlock();
      block.Entity = assignment;
      
      // Установить иконку.
      this.SetIcon(block, assignment, isApprovalBlock, isResolutionBlock, iconSize);

      // Заполнить основное содержимое.
      if (isApprovalBlock)
      {
        block.AddLabel(performer, performerDeadlineStyle);
        block.AddLabel(deadline, performerDeadlineStyle);
      }
      else
      {
        // Заполнить заголовок.
        var header = this.GetHeader(assignment, isResolutionBlock);
        block.AddLabel(header, headerStyle);

        // Заполнить "Кому".
        var performerLabel = assignment.Status != Workflow.AssignmentBase.Status.Completed ?
          string.Format("{0}: {1}{2}", OfficialDocuments.Resources.StateViewTo, performer, deadline) :
          string.Format("{0}{1}", performer, deadline);
        
        // Для резолюции указать адресата вместо исполнителя.
        if (isResolutionBlock)
        {
          var task = ApprovalTasks.As(assignment.Task);
          var addresseeShortName = Company.PublicFunctions.Employee.GetShortName(task.Addressee, false);
          performerLabel = string.Format("{0}: {1} {2}", ApprovalTasks.Resources.StateViewAuthor, addresseeShortName, deadline);
        }
        
        block.AddLineBreak();
        block.AddLabel(performerLabel, performerDeadlineStyle);
        
        if (ApprovalSendingAssignments.Is(assignment) ||
            GetCollapsedStagesTypes(assignment).Contains(Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.Sending))
        {
          var task = ApprovalTasks.As(assignment.Task);
          if (task != null)
          {
            var isManyAddresseesOutgoingDocument = OutgoingDocumentBases.Is(task.DocumentGroup.OfficialDocuments.FirstOrDefault()) &&
              OutgoingDocumentBases.As(task.DocumentGroup.OfficialDocuments.FirstOrDefault()).IsManyAddressees == true;
            
            if (isManyAddresseesOutgoingDocument || task.DeliveryMethod != null)
            {
              var service = string.Empty;
              var method = string.Empty;
              
              if (isManyAddresseesOutgoingDocument)
                method = ApprovalRuleBases.Resources.StateViewSendToManyAddressees;
              else if (task.DeliveryMethod != null)
              {
                service = (task.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange && task.ExchangeService != null) ?
                  task.ExchangeService.Name :
                  string.Empty;
                method = task.DeliveryMethod.Name;
              }
              block.AddLineBreak();
              var note = ApprovalRuleBases.Resources.StateViewSendNoteFormat(method, service);
              block.AddLabel(note, Docflow.Functions.Module.CreateNoteStyle());
            }
          }
        }
      }

      // Заполнить примечание.
      this.AddComment(block, assignment, isResolutionBlock);
      
      // Заполнить статус.
      if (!string.IsNullOrWhiteSpace(status))
        Functions.Module.AddInfoToRightContent(block, status);
      
      // Заполнить просрочку.
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess)
        Functions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      
      // Добавить блок резолюции.
      if (needAddResolution)
        this.AddAssignmentBlock(parentBlock, assignment, false, true, iconSize);
      
      return block;
    }
    
    /// <summary>
    /// Получить список схлопнутых типов этапов.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Список схлопнутых этапов.</returns>
    private static List<Enumeration?> GetCollapsedStagesTypes(IAssignment assignment)
    {
      // Для каждого задания берем свою дочернюю коллекцию, т.к. теперь они везде имеют разные названия.
      var stagesTypes = new List<Enumeration?>();
      
      if (ApprovalPrintingAssignments.Is(assignment))
        stagesTypes = ApprovalPrintingAssignments.As(assignment).CollapsedStagesTypesPr.Select(c => c.StageType).ToList();
      
      if (ApprovalRegistrationAssignments.Is(assignment))
        stagesTypes = ApprovalRegistrationAssignments.As(assignment).CollapsedStagesTypesReg.Select(c => c.StageType).ToList();
      
      if (ApprovalSendingAssignments.Is(assignment))
        stagesTypes = ApprovalSendingAssignments.As(assignment).CollapsedStagesTypesSen.Select(c => c.StageType).ToList();
      
      if (ApprovalSigningAssignments.Is(assignment))
        stagesTypes = ApprovalSigningAssignments.As(assignment).CollapsedStagesTypesSig.Select(c => c.StageType).ToList();
      
      if (ApprovalReviewAssignments.Is(assignment))
        stagesTypes = ApprovalReviewAssignments.As(assignment).CollapsedStagesTypesRe.Select(c => c.StageType).ToList();
      
      if (ApprovalExecutionAssignments.Is(assignment))
        stagesTypes = ApprovalExecutionAssignments.As(assignment).CollapsedStagesTypesExe.Select(c => c.StageType).ToList();
      
      return stagesTypes;
    }
    
    /// <summary>
    /// Получить заголовок.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="isResolutionBlock">Признак: блок резолюции или нет.</param>
    /// <returns>Заголовок.</returns>
    public string GetHeader(IAssignment assignment, bool isResolutionBlock)
    {
      if (!isResolutionBlock &&
          (ApprovalPrintingAssignments.Is(assignment) || ApprovalRegistrationAssignments.Is(assignment) ||
           ApprovalSendingAssignments.Is(assignment) || ApprovalSigningAssignments.Is(assignment) ||
           ApprovalReviewAssignments.Is(assignment) || ApprovalExecutionAssignments.Is(assignment)))
      {
        var stagesTypes = GetCollapsedStagesTypes(assignment);
        
        if (stagesTypes.Count > 1)
        {
          var stages = new List<string>();
          
          // Используем foreach, так как Linq не работает с такой конструкцией.
          var header = string.Empty;
          foreach (var stage in stagesTypes)
          {
            var stageHeader = ApprovalReviewAssignments.Info.Properties.CollapsedStagesTypesRe.Properties.StageType
              .GetLocalizedValue(stage).ToLower();
            stages.Add(stageHeader);
          }
          header = string.Join(", ", stages);
          return Functions.Module.ReplaceFirstSymbolToUpperCase(header);
        }
      }
      
      var actionLabel = string.Empty;
      
      // Согласование.
      if (ApprovalAssignments.Is(assignment) || ApprovalManagerAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewApprovalProcess;

      // Подписание.
      if (ApprovalSigningAssignments.Is(assignment))
      {
        // Для подтверждения подписания указать это.
        var signAssignment = ApprovalSigningAssignments.As(assignment);
        if (signAssignment.IsConfirmSigning == true)
          actionLabel = ApprovalTasks.Resources.StateViewApprovedConfirmation;
        else
          actionLabel = ApprovalTasks.Resources.StateViewSigning;
      }

      // Регистрация.
      if (ApprovalRegistrationAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewRegistration;

      // Контроль возврата от контрагента.
      if (ApprovalCheckReturnAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewCheckReturn;
      
      // Доработка после согласования или после задания с доработкой.
      if (ApprovalReworkAssignments.Is(assignment))
        actionLabel = Functions.ApprovalTask.IsSignatoryAbortTask(assignment.Task, assignment.Created) || Functions.ApprovalTask.IsAddresseeAbortTask(assignment.Task, assignment.Created) ?
          ApprovalTasks.Resources.StateViewAbortApprovalAssignment :
          Functions.ApprovalTask.IsExternalSignatoryAbortTask(assignment.Task, assignment.Created) ?
          ApprovalTasks.Resources.StateViewDocumentReworkAfterExternalAbort : ApprovalTasks.Resources.StateViewDocumentRework;
      
      // Печать документа.
      if (ApprovalPrintingAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewPrintDocument;
      
      // Рассмотрение адресатом.
      if (ApprovalReviewAssignments.Is(assignment))
      {
        if (isResolutionBlock)
          actionLabel = ApprovalTasks.Resources.StateViewResolution;
        else
        {
          // Для внесения результата рассмотрения указать это.
          var reviewAssignment = ApprovalReviewAssignments.As(assignment);
          if (reviewAssignment.IsResultSubmission == true)
            actionLabel = ApprovalTasks.Resources.StateViewResultSubmission;
          else
            actionLabel = ApprovalTasks.Resources.StateViewReview;
        }
      }
      
      // Создание поручений.
      if (ApprovalExecutionAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewExecution;

      // Отправка контрагенту.
      if (ApprovalSendingAssignments.Is(assignment))
        actionLabel = ApprovalTasks.Resources.StateViewSendToCounterParty;
      
      // Задание.
      if (ApprovalSimpleAssignments.Is(assignment) || ApprovalCheckingAssignments.Is(assignment))
      {
        var assignmentSubject = ApprovalSimpleAssignments.Is(assignment) ?
          ApprovalSimpleAssignments.As(assignment).StageSubject :
          ApprovalCheckingAssignments.As(assignment).StageSubject;
        
        if (string.IsNullOrWhiteSpace(assignmentSubject))
          actionLabel = OfficialDocuments.Resources.StateViewAssignment;
        else
          return string.Format("{0}. {1}", OfficialDocuments.Resources.StateViewAssignment, assignmentSubject);
      }
      
      return actionLabel;
    }
    
    /// <summary>
    /// Получить исполнителя, срок и статус.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="isResolutionBlock">Признак: блок резолюции или нет.</param>
    /// <returns>Структура короткое имя исполнителя, срок, статус.</returns>
    public Structures.ApprovalTask.StateViewAssignmentInfo GetPerformerAndDeadlineAndStatus(IAssignment assignment, bool isResolutionBlock)
    {
      var performerName = PublicFunctions.OfficialDocument.GetAuthor(assignment.Performer, assignment.CompletedBy);
      var actionLabel = string.Empty;
      var emptyResult = Structures.ApprovalTask.StateViewAssignmentInfo.Create(string.Empty, string.Empty, string.Empty);

      #region Завершенные задания
      
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed)
      {
        var completed = Functions.Module.ToShortDateShortTime(assignment.Completed.Value.ToUserTime());
        
        // Согласование.
        if (ApprovalAssignments.Is(assignment) || ApprovalManagerAssignments.Is(assignment))
        {
          if (assignment.Result == Docflow.ApprovalAssignment.Result.Approved)
            actionLabel = ApprovalTasks.Resources.StateViewEndorsed;
          else if (assignment.Result == Docflow.ApprovalAssignment.Result.ForRevision)
            actionLabel = ApprovalTasks.Resources.StateViewNotApproved;
          else if (assignment.Result == Docflow.ApprovalAssignment.Result.Forward)
            actionLabel = ApprovalTasks.Resources.StateViewForwarded;
          else if (assignment.Result == Docflow.ApprovalAssignment.Result.WithSuggestions)
            actionLabel = ApprovalTasks.Resources.StateViewEndorsedWithSuggestions;
          else
            return emptyResult;
        }
        
        // Подписание.
        if (ApprovalSigningAssignments.Is(assignment))
        {
          if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.Sign)
            actionLabel = ApprovalTasks.Resources.StateViewApproved;
          else if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.ConfirmSign)
            actionLabel = ApprovalTasks.Resources.StateViewDone;
          else if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.ForRevision)
            actionLabel = ApprovalTasks.Resources.StateViewNotApproved;
          else if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.Abort)
            actionLabel = ApprovalTasks.Resources.SigningRefused;
          else
            return emptyResult;
        }
        
        // Регистрация.
        if (ApprovalRegistrationAssignments.Is(assignment))
        {
          actionLabel = ApprovalTasks.Resources.StateViewDone;
        }
        
        // Прекращение на доработке.
        if (ApprovalReworkAssignments.Is(assignment) && assignment.Status == Sungero.Workflow.AssignmentBase.Status.Aborted)
        {
          actionLabel = ApprovalTasks.Resources.StateViewAborted;
        }
        
        // Переадресация на доработке.
        if (ApprovalReworkAssignments.Is(assignment) && assignment.Result == Docflow.ApprovalReworkAssignment.Result.Forward)
        {
          actionLabel = Sungero.Docflow.ApprovalTasks.Resources.StateViewReworkForwarded;
        }
        
        // Контроль возврата.
        if (ApprovalCheckReturnAssignments.Is(assignment))
        {
          if (assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.Signed)
            actionLabel = ApprovalTasks.Resources.StateViewSignedByCounterparty;
          else if (assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.NotSigned)
            actionLabel = ApprovalTasks.Resources.StateViewNotSignedByCounterparty;
          else
            return emptyResult;
        }
        
        // Печать документа.
        if (ApprovalPrintingAssignments.Is(assignment))
          actionLabel = ApprovalTasks.Resources.StateViewDone;
        
        // Рассмотрение адресатом.
        if (ApprovalReviewAssignments.Is(assignment))
        {
          // Для резолюции вернуть пустой статус.
          if (isResolutionBlock)
            return Structures.ApprovalTask.StateViewAssignmentInfo.Create(string.Format("{0} ", performerName),
                                                                          string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDate, completed),
                                                                          string.Empty);
          
          // Для внесения результата рассмотрения указать это.
          if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.ForRework)
            actionLabel = ApprovalTasks.Resources.StateViewNotApproved;
          else
            actionLabel = ApprovalTasks.Resources.StateViewDone;
        }
        
        // Создание поручений.
        if (ApprovalExecutionAssignments.Is(assignment))
          actionLabel = ApprovalTasks.Resources.StateViewDone;

        // Отправка контрагенту.
        if (ApprovalSendingAssignments.Is(assignment))
          actionLabel = ApprovalTasks.Resources.StateViewDone;
        
        // Задание.
        if (ApprovalSimpleAssignments.Is(assignment) || ApprovalCheckingAssignments.Is(assignment))
          actionLabel = ApprovalTasks.Resources.StateViewDone;

        if (!string.IsNullOrWhiteSpace(actionLabel))
          return Structures.ApprovalTask.StateViewAssignmentInfo.Create(string.Format("{0} ", performerName),
                                                                        string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDate, completed),
                                                                        actionLabel);
      }
      
      #endregion
      
      #region Задания в работе
      
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess ||
          assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        var asgStatus = ApprovalTasks.Resources.StateViewAborted;
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess)
          asgStatus = assignment.IsRead == true ? ApprovalTasks.Resources.StateViewInProcess : ApprovalTasks.Resources.StateViewUnRead;
        var deadline = Functions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime());
        return Structures.ApprovalTask.StateViewAssignmentInfo.Create(string.Format("{0} ", performerName),
                                                                      string.Format("{0}: {1}", OfficialDocuments.Resources.StateViewDeadline, deadline),
                                                                      asgStatus);
      }
      
      #endregion
      
      return emptyResult;
    }
    
    /// <summary>
    /// Установить иконку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="isApprovalBlock">Признак блока согласования.</param>
    /// <param name="isResolutionBlock">Признак: блок резолюции или нет.</param>
    /// <param name="iconMainSize">Размер иконки.</param>
    private void SetIcon(StateBlock block, IAssignment assignment, bool isApprovalBlock, bool isResolutionBlock, StateBlockIconSize iconMainSize)
    {
      var iconSize = isApprovalBlock ? StateBlockIconSize.Small : iconMainSize;
      
      // Иконка по умолчанию.
      block.AssignIcon(StateBlockIconType.OfEntity, iconSize);

      // Прекращено, остановлено по ошибке.
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return;
      }
      
      if (assignment.Result == null)
        return;
      
      // Согласовано.
      if (assignment.Result == Docflow.ApprovalAssignment.Result.Approved)
      {
        block.AssignIcon(ApprovalTasks.Resources.Approve, iconSize);
      }
      
      // Согласовано с замечаниями.
      if (assignment.Result == Docflow.ApprovalAssignment.Result.WithSuggestions)
      {
        block.AssignIcon(ApprovalTasks.Resources.SignWithRemarks, iconSize);
      }
      
      // Переадресовано.
      if (assignment.Result == Docflow.ApprovalAssignment.Result.Forward)
      {
        block.AssignIcon(FreeApprovalTasks.Resources.Forward, iconSize);
      }
      
      // На доработку.
      if (assignment.Result == Docflow.ApprovalCheckingAssignment.Result.ForRework)
      {
        block.AssignIcon(ApprovalTasks.Resources.Rework, iconSize);
      }
      
      // Не согласовано.
      if (assignment.Result == Docflow.ApprovalAssignment.Result.ForRevision ||
          assignment.Result == Docflow.ApprovalReviewAssignment.Result.ForRework)
      {
        block.AssignIcon(ApprovalTasks.Resources.Notapprove, iconSize);
      }
      
      // Задание выполнено.
      if (assignment.Result == Docflow.ApprovalSimpleAssignment.Result.Complete ||
          assignment.Result == Docflow.ApprovalCheckingAssignment.Result.Accept)
      {
        block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);
      }
      
      // Подписано, подписано контрагентом.
      if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.Sign ||
          assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.Signed ||
          assignment.Result == Docflow.ApprovalSigningAssignment.Result.ConfirmSign)
      {
        block.AssignIcon(ApprovalTasks.Resources.Sign, iconSize);
      }
      
      // На повторное согласование.
      if (assignment.Result == Docflow.ApprovalReworkAssignment.Result.ForReapproving)
      {
        block.AssignIcon(StateBlockIconType.User, iconSize);
      }
      
      // Зарегистрировано.
      if (ApprovalRegistrationAssignments.Is(assignment) &&
          assignment.Result == Docflow.ApprovalRegistrationAssignment.Result.Execute)
      {
        block.AssignIcon(OfficialDocuments.Info.Actions.Register, iconSize);
      }
      
      // Распечатано.
      if (ApprovalPrintingAssignments.Is(assignment) &&
          assignment.Result == Docflow.ApprovalPrintingAssignment.Result.Execute)
      {
        block.AssignIcon(ApprovalTasks.Resources.Print, iconSize);
      }
      
      // Рассмотрено.
      if (ApprovalReviewAssignments.Is(assignment) &&
          (assignment.Result == Docflow.ApprovalReviewAssignment.Result.Informed ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddActionItem ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution ||
           assignment.Result == Docflow.ApprovalReviewAssignment.Result.Abort))
      {
        if (isResolutionBlock)
        {
          block.AssignIcon(ApprovalReviewAssignments.Info.Actions.AddResolution, iconSize);
          if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.Abort)
            block.AssignIcon(StateBlockIconType.Abort, iconSize);
        }
        else
        {
          // Для каждого задания берем свою дочернюю коллекцию, т.к. теперь они везде имеют разные названия.
          var stagesTypes = new List<Enumeration?> { };
          
          if (ApprovalPrintingAssignments.Is(assignment))
            stagesTypes = ApprovalPrintingAssignments.As(assignment).CollapsedStagesTypesPr.Select(c => c.StageType).ToList();
          
          if (ApprovalRegistrationAssignments.Is(assignment))
            stagesTypes = ApprovalRegistrationAssignments.As(assignment).CollapsedStagesTypesReg.Select(c => c.StageType).ToList();
          
          if (ApprovalSendingAssignments.Is(assignment))
            stagesTypes = ApprovalSendingAssignments.As(assignment).CollapsedStagesTypesSen.Select(c => c.StageType).ToList();
          
          if (ApprovalSigningAssignments.Is(assignment))
            stagesTypes = ApprovalSigningAssignments.As(assignment).CollapsedStagesTypesSig.Select(c => c.StageType).ToList();
          
          if (ApprovalReviewAssignments.Is(assignment))
            stagesTypes = ApprovalReviewAssignments.As(assignment).CollapsedStagesTypesRe.Select(c => c.StageType).ToList();
          
          if (ApprovalExecutionAssignments.Is(assignment))
            stagesTypes = ApprovalExecutionAssignments.As(assignment).CollapsedStagesTypesExe.Select(c => c.StageType).ToList();

          if (stagesTypes.Count > 1)
          {
            // Используем foreach, так как Linq не работает с такой конструкцией.
            foreach (var stage in stagesTypes)
            {
              if (stage == Docflow.ApprovalRuleBaseStages.StageType.Register)
              {
                block.AssignIcon(OfficialDocuments.Info.Actions.Register, iconSize);
                break;
              }
              
              if (stage == Docflow.ApprovalRuleBaseStages.StageType.Print)
              {
                block.AssignIcon(ApprovalTasks.Resources.Print, iconSize);
                break;
              }
              
              if (stage == Docflow.ApprovalRuleBaseStages.StageType.Execution)
              {
                block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);
                break;
              }
            }
          }
          else
            block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);
        }
      }
      
      // Создание поручений выполнено.
      if (ApprovalExecutionAssignments.Is(assignment) &&
          (assignment.Result == Docflow.ApprovalExecutionAssignment.Result.Complete))
      {
        block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);
      }
      
      // Прекращено, не подписано контрагентом.
      if ((!ApprovalReviewAssignments.Is(assignment) && assignment.Result == Docflow.ApprovalSigningAssignment.Result.Abort) ||
          assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.NotSigned)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
      }
    }
    
    /// <summary>
    /// Добавить комментарий к блоку.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="isResolutionBlock">Признак: блок резолюции или нет.</param>
    private void AddComment(StateBlock block, IAssignment assignment, bool isResolutionBlock)
    {
      var comment = Functions.Module.GetAssignmentUserComment(assignment);
      
      if (assignment.Status != Workflow.AssignmentBase.Status.Completed)
        return;
      
      if (ApprovalReviewAssignments.Is(assignment))
      {
        // Для блока резолюции добавить информацию по поручениям.
        if (isResolutionBlock && assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddActionItem)
        {
          var actionItems = Functions.Module.GetActionItemsForResolution(assignment.Task, Workflow.Task.Status.Draft, ApprovalTasks.As(assignment.Task).Addressee);
          if (actionItems.Any())
          {
            block.AddLineBreak();
            block.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
            
            // Добавить информацию по каждому поручению.
            foreach (var actionItem in actionItems)
            {
              AddActionItemInfo(block, actionItem);
            }
            return;
          }
        }
        
        // Для рассмотрения добавить комментарий "Принято к сведению", если его нет.
        if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.Informed &&
            string.IsNullOrWhiteSpace(comment))
          comment = ApprovalTasks.Resources.Informed;
        
        // Для рассмотрения добавить комментарий "Отправлено на исполнение", если его нет.
        if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddActionItem &&
            string.IsNullOrWhiteSpace(comment))
          comment = ApprovalTasks.Resources.SentForExecution;
      }
      
      if (!string.IsNullOrWhiteSpace(comment))
      {
        block.AddLineBreak();
        block.AddLabel(Constants.Module.SeparatorText, Docflow.PublicFunctions.Module.CreateSeparatorStyle());
        block.AddLineBreak();
        block.AddEmptyLine(Constants.Module.EmptyLineMargin);
        if (isResolutionBlock)
          block.AddLabel(Docflow.Functions.Module.TrimEndNewLines(comment));
        else
          block.AddLabel(Docflow.Functions.Module.TrimEndNewLines(comment), Functions.Module.CreateNoteStyle());
      }
    }
    
    /// <summary>
    /// Добавить информацию о созданном поручении в резолюцию.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="actionItem">Поручение.</param>
    public static void AddActionItemInfo(Sungero.Core.StateBlock block, ITask actionItem)
    {
      var infos = Functions.Module.ActionItemInfoProvider(actionItem).ToArray();
      
      if (infos.Length == 0)
        return;
      
      block.AddEmptyLine(Constants.Module.EmptyLineMargin);
      
      // Отчет пользователя.
      block.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(infos[0]));
      block.AddLineBreak();
      
      // Исполнители.
      var performerStyle = Sungero.Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var info = string.Empty;
      info += infos[1];
      
      // Срок.
      info += infos[2];
      
      // Контролер.
      info += infos[3];
      
      block.AddLabel(info, performerStyle);
      block.AddLineBreak();
      block.AddLineBreak();
    }
    
    /// <summary>
    /// Установить иконку группы заданий на согласование.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="isApproved">Группа согласована.</param>
    /// <param name="hasRework">Имеются ли доработки.</param>
    /// <param name="nextAssignment">Следующее задание.</param>
    /// <param name="hasAbort">Признак: было ли прекращение в группе.</param>
    /// <param name="hasTaskAbort">Признак: прекращена ли задача.</param>
    /// <param name="iconSize">Размер иконки.</param>
    /// <returns>Статус группы согласований.</returns>
    private string SetGroupIconAndGetGroupStatus(StateBlock block, bool isApproved, bool hasRework, IAssignment nextAssignment,
                                                 bool hasAbort, bool hasTaskAbort, StateBlockIconSize iconSize)
    {
      // Установить иконку "В работе".
      block.AssignIcon(ApprovalTasks.Resources.ApproveStage, iconSize);
      var status = ApprovalTasks.Resources.StateViewInProcess;
      
      // Установить иконку доработки, если была хоть одна.
      if (hasRework)
      {
        block.AssignIcon(ApprovalTasks.Resources.Notapprove, iconSize);
        return ApprovalTasks.Resources.StateViewNotApproved;
      }
      
      // Установить иконку "Выполнено", если все согласны.
      if (isApproved)
      {
        block.AssignIcon(ApprovalTasks.Resources.Approve, iconSize);
        return ApprovalTasks.Resources.StateViewEndorsed;
      }
      
      if (!hasAbort)
        return status;
      
      // Если есть прекращенные задания и доработка не была выполнена, то круг был прекращен.
      if (nextAssignment != null && !nextAssignment.Result.HasValue &&
          Equals(nextAssignment.BlockUid, Constants.Module.ApprovalReworkAssignmentBlockUid))
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return ApprovalTasks.Resources.StateViewAborted;
      }
      
      // Если следующего задания нет и задача прекращена, то круг прекращен.
      if (nextAssignment == null &&
          (_obj.Status == Workflow.Task.Status.Aborted || _obj.Status == Workflow.Task.Status.Suspended))
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return ApprovalTasks.Resources.StateViewAborted;
      }
      
      // Если между созданием текущей и следующей задачи было прекращение, то круг был прекращен.
      if (nextAssignment != null && hasTaskAbort)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return ApprovalTasks.Resources.StateViewAborted;
      }
      
      return status;
    }
    
    #endregion
    
    #region Регламент
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      var approvers = _obj.AddApprovers.Select(a => a.Approver).ToList();
      if (_obj.Status != Sungero.Docflow.ApprovalTask.Status.Draft)
        approvers = _obj.AddApproversExpanded.Select(a => a.Approver).ToList();
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj, approvers, _obj.Signatory, _obj.Addressee, _obj.DeliveryMethod, _obj.ExchangeService, true);
    }
    
    #endregion
    
    #region Проверка на прочитанность документа
    
    /// <summary>
    /// Получить время, когда пользователь последний раз видел тело последней версии документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="employee">Пользователь.</param>
    /// <returns>Время.</returns>
    [Public]
    public static DateTime? GetDocumentLastViewDate(IElectronicDocument document, IUser employee)
    {
      var lastVersionNumber = document.Versions.Max(v => v.Number);
      return document.History.GetAll()
        .Where(h => h.User.Equals(Users.Current) && h.VersionNumber == lastVersionNumber)
        .Where(h =>
               (h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.UpdateVerBody) ||
               (h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.Import) ||
               (h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.CreateVersion) ||
               (h.Action == CoreEntities.History.Action.Create && h.Operation == null) ||
               (h.Action == CoreEntities.History.Action.Read && h.Operation == Content.DocumentHistory.Operation.ReadVerBody) ||
               (h.Action == CoreEntities.History.Action.Read && h.Operation == Content.DocumentHistory.Operation.Export))
        .Max(h => h.HistoryDate);
    }
    
    /// <summary>
    /// Был ли обновлен документ с момента последнего просмотра.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если пользователь не видел актуальное содержимое документа, иначе false.</returns>
    [Public, Remote(IsPure = true)]
    public static bool DocumentHasBodyUpdateAfterLastView(Sungero.Content.IElectronicDocument document)
    {
      if (!document.HasVersions)
        return false;
      
      var lastVersionNumber = document.Versions.Max(v => v.Number);
      var lastViewDate = GetDocumentLastViewDate(document, Users.Current);

      // С момента последнего просмотра мной, были ли изменения другими этой версии.
      return lastViewDate == null ||
        document.History.GetAll().Any(
          h => !h.User.Equals(Users.Current) &&
          h.HistoryDate > lastViewDate &&
          h.VersionNumber == lastVersionNumber &&
          ((h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.UpdateVerBody) ||
           (h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.Import) ||
           (h.Action == CoreEntities.History.Action.Update && h.Operation == Content.DocumentHistory.Operation.CreateVersion) ||
           (h.Action == CoreEntities.History.Action.Create && h.Operation == null)));
    }
    
    /// <summary>
    /// Был ли документ просмотрен сотрудником.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если пользователь просматривал документ, иначе false.</returns>
    [Remote(IsPure = true)]
    public static bool DocumenHasBeenViewed(Sungero.Content.IElectronicDocument document)
    {
      if (!document.HasVersions)
        return false;
      return GetDocumentLastViewDate(document, Users.Current) != null;
    }
    
    #endregion
    
    #region Схлопывание
    
    /// <summary>
    /// Получить список типов этапов схлопывания.
    /// </summary>
    /// <returns>Список типов этапов, которые можно схлопнуть.</returns>
    public static List<Enumeration> GetAvailableToCollapseStageTypes()
    {
      return new List<Enumeration>()
      {
        Docflow.ApprovalStage.StageType.Sending,
        Docflow.ApprovalStage.StageType.Print,
        Docflow.ApprovalStage.StageType.Register,
        Docflow.ApprovalStage.StageType.Execution,
        Docflow.ApprovalStage.StageType.Sign,
        Docflow.ApprovalStage.StageType.Review
      };
    }
    
    /// <summary>
    /// Определить, нужно ли схлапывание для этапа задачи.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <param name="stage">Этап согласования.</param>
    /// <returns>True, если этап схлапывается.</returns>
    public static bool NeedCollapse(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var mayCollapsedStageTypes = GetAvailableToCollapseStageTypes();

      var collapsedStages = GetCollapsedStages(task, stage);
      if (collapsedStages == null || !collapsedStages.Any(s => !Equals(s, stage)))
        return false;
      
      var mainStageTypeIndex = collapsedStages.Max(s => mayCollapsedStageTypes.IndexOf(s.Stage.StageType.Value));
      var mainStageType = mayCollapsedStageTypes[mainStageTypeIndex];
      
      if (!Equals(mainStageType, stage.StageType))
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Получить схлопываемые этапы.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Схлапываемые этапы.</returns>
    public static List<Structures.Module.DefinedApprovalStageLite> GetCollapsedStages(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      // Этап точно не схлопнется, если он не схлапываемого типа.
      var mayCollapsedStageTypes = GetAvailableToCollapseStageTypes();
      if (!mayCollapsedStageTypes.Contains(stage.StageType.Value))
        return new List<Structures.Module.DefinedApprovalStageLite>() { stage };
      
      // Получить этапы, которые могут быть схлопнуты с текущим.
      var stagePerformer = Docflow.PublicFunctions.ApprovalStage.GetStagePerformer(task, stage.Stage, null, null);
      var stages = Functions.ApprovalTask.GetBaseStages(task).BaseStages;
      var mayCollapsedStages = stages
        .Where(s => mayCollapsedStageTypes.Any(t => Equals(t, s.StageType)))
        .Where(s => ApprovalStages.Is(s.StageBase))
        .Where(s => Equals(Docflow.PublicFunctions.ApprovalStage.GetStagePerformer(task, ApprovalStages.As(s.StageBase), null, null), stagePerformer))
        .ToList();
      
      // Если схлопывать этап не с чем, то вернуть список с одним исходным этапом.
      if (!mayCollapsedStages.Any(s => !Equals(s, stage)))
        return mayCollapsedStages.Select(s => Functions.ApprovalRuleBase.CastToDefinedApprovalStageLite(s)).ToList();
      
      // Найти границы доступных для схлапывания этапов.
      var baseStage = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stage);
      var currentStageIndex = stages.IndexOf(baseStage);
      var firstStageIndex = stages.IndexOf(mayCollapsedStages.OrderBy(x => stages.IndexOf(x)).First());
      var lastStageIndex = stages.IndexOf(mayCollapsedStages.OrderByDescending(x => stages.IndexOf(x)).First());
      
      // Получить список схлапываемых этапов. Не схлопывать "бумажные" этапы рассмотрения и подписания с предыдущими.
      var collapsedStages = new List<Structures.Module.DefinedApprovalStageLite>();
      for (var i = firstStageIndex; i <= lastStageIndex; ++i)
      {
        var ruleStage = stages[i];
        var ruleStageLite = Functions.ApprovalRuleBase.CastToDefinedApprovalStageLite(ruleStage);
        if (ruleStageLite == null)
        {
          if (collapsedStages.Contains(stage))
            break;
          else
          {
            collapsedStages.Clear();
            continue;
          }
        }
        
        var stageMayCollapsed = mayCollapsedStages.Contains(ruleStage);
        
        // Добавить в схлапываемые, если он входит в доступные для схлапывания этапы.
        if (stageMayCollapsed &&
            (ruleStageLite.StageType != StageType.Sign || ruleStageLite.Stage.IsConfirmSigning != true) &&
            (ruleStageLite.StageType != StageType.Review || ruleStageLite.Stage.IsResultSubmission != true))
        {
          collapsedStages.Add(ruleStageLite);
          continue;
        }
        
        // Закончить поиск этапов, если искомый этап в списке схлапываемых.
        if (collapsedStages.Contains(stage))
          break;
        
        // Очистить список схлопываемых этапов, если в нём нет искомого этапа.
        collapsedStages.Clear();
        
        // Добавить этап после очистки, если он входит в доступные для схлапывания.
        if (stageMayCollapsed)
          collapsedStages.Add(ruleStageLite);
      }
      
      // Если в списке схлапываемых этапов есть создание поручений, то убрать его, если оно не нужно.
      var executionStage = collapsedStages.FirstOrDefault(s => s.Stage.StageType == StageType.Execution);
      if (executionStage != null)
      {
        var isExecutionNeeded = Functions.ApprovalExecutionAssignment.NeedExecutionAssignment(task);
        if (!isExecutionNeeded && stage.Stage.StageType == StageType.Execution)
          return new List<Structures.Module.DefinedApprovalStageLite>() { stage };
        
        if (!isExecutionNeeded)
          collapsedStages.Remove(executionStage);
      }
      
      // Если отправка не требуется, исключить ее из схлапываемых этапов.
      var sendingStage = collapsedStages.FirstOrDefault(x => x.StageType == StageType.Sending);
      var hasSigningStage = collapsedStages.Any(s => s.StageType == StageType.Sign);
      if (sendingStage != null && Functions.ApprovalTask.NeedSkipSendingStage(task, hasSigningStage))
        collapsedStages.Remove(sendingStage);
      
      return collapsedStages;
    }
    
    /// <summary>
    /// Определить схлопнутый срок в днях.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Число дней.</returns>
    public static int? CollapsedDeadlineInDays(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var collapsedStages = GetCollapsedStages(task, stage);
      if (!collapsedStages.Any(s => !Equals(s, stage)))
        return stage.Stage.DeadlineInDays;
      
      var deadline = collapsedStages.Select(s => s.Stage).Sum(s => s.DeadlineInDays);
      Logger.DebugFormat("CollapsedDeadlineInDays: Task {0}, stage{1}:{2}, deadline = {3}.", task.Id, stage.Number, stage.StageType, deadline);
      return deadline;
    }
    
    /// <summary>
    /// Определить схлопнутый срок в часах.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Число часов.</returns>
    public static int? CollapsedDeadlineInHours(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var collapsedStages = GetCollapsedStages(task, stage);
      if (!collapsedStages.Any(s => !Equals(s, stage)))
        return stage.Stage.DeadlineInHours;
      
      var deadline = collapsedStages.Select(s => s.Stage).Sum(s => s.DeadlineInHours);
      Logger.DebugFormat("CollapsedDeadlineInHours: Task {0}, stage{1}:{2}, deadline = {3}.", task.Id, stage.Number, stage.StageType, deadline);
      return deadline;
    }
    
    /// <summary>
    /// Получить схлопнутую тему задания.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Исполнитель.</returns>
    public static string GetCollapsedSubject(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var subject = string.Empty;
      var collapsedStageTypes = GetCollapsedStagesTypes(task, stage).Distinct();
      var document = task.DocumentGroup.OfficialDocuments.First();
      
      // Сформировать тему, следуя порядку этапов в правиле.
      foreach (var stageType in collapsedStageTypes)
      {
        // Регистрация.
        if (stageType == Docflow.ApprovalStage.StageType.Register)
          subject = string.Join(", ", subject, ApprovalTasks.Resources.RegistrationAsgSubject);

        // Печать.
        if (stageType == Docflow.ApprovalStage.StageType.Print)
        {
          var lastCollapsedStage = GetCollapsedStages(task, stage).LastOrDefault();
          var nextStage = Functions.ApprovalRuleBase.GetNextStage(task.ApprovalRule, document, lastCollapsedStage, task);
          if (nextStage != null)
          {
            var nextStageType = nextStage.Stage.StageType;
            var needSkipNextSignStage = Functions.ApprovalTask.NeedSkipSignStage(task, nextStage, task.Signatory, task.Addressee);
            
            // Если следующий этап - подписание, то указать в теме необходимость передать на подписание.
            if ((nextStageType == Docflow.ApprovalStage.StageType.Sign ||
                 nextStageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign) &&
                !needSkipNextSignStage)
            {
              subject = string.Join(", ", subject, ApprovalTasks.Resources.PrintAndTransferAsgSubject);
            }
            else if (nextStageType == Docflow.ApprovalStage.StageType.Review ||
                     nextStageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ReviewingResult ||
                     needSkipNextSignStage)
            {
              // Если следующий этап - рассмотрение, то указать в теме необходимость передать на рассмотрение.
              subject = string.Join(", ", subject, ApprovalTasks.Resources.PrintAndTransferToReviewAsgSubject);
            }
            else
              subject = string.Join(", ", subject, ApprovalTasks.Resources.PrintAsgSubject);
          }
          else
            subject = string.Join(", ", subject, ApprovalTasks.Resources.PrintAsgSubject);
        }
        
        // Подтверждение подписания.
        if (stageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.ConfirmSigningSubject);
        }
        
        // Подписание.
        if (stageType == Docflow.ApprovalStage.StageType.Sign)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.SignAsgSubject);
        }

        // Отправка КА.
        if (stageType == Docflow.ApprovalStage.StageType.Sending)
        {
          if (document.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
              collapsedStageTypes.Contains(Docflow.ApprovalStage.StageType.Sign) ||
              collapsedStageTypes.Contains(Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign) ||
              document.ExternalApprovalState != Docflow.OfficialDocument.ExternalApprovalState.Signed)
            subject = string.Join(", ", subject, ApprovalTasks.Resources.SendToCounterparty);
        }
        
        // Рассмотрение.
        if (stageType == Docflow.ApprovalStage.StageType.Review)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.ReviewAsgSubject);
        }
        
        // Обработка резолюции.
        if (stageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ReviewingResult)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.SpecifyReviewingResultAsgSubject);
        }
        
        // Создание поручений.
        if (stageType == Docflow.ApprovalStage.StageType.Execution)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.ExecutionAsgSubject);
        }
      }
      
      subject = Functions.Module.ReplaceFirstSymbolToUpperCase(subject.TrimStart(new[] { ',', ' ' }));
      return Docflow.PublicFunctions.Module.TrimSpecialSymbols("{0}: {1}", subject, task.DocumentGroup.OfficialDocuments.First().Name);
    }
    
    /// <summary>
    /// Получить схлопнутую тему задания.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Исполнитель.</returns>
    public static string GetCollapsedThreadSubject(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var subject = string.Empty;
      var collapsedStageTypes = GetCollapsedStagesTypes(task, stage).Distinct();
      var document = task.DocumentGroup.OfficialDocuments.First();
      
      // Сформировать тему, следуя порядку этапов в правиле.
      foreach (var stageType in collapsedStageTypes)
      {
        // Регистрация.
        if (stageType == Docflow.ApprovalStage.StageType.Register)
          subject = string.Join(", ", subject, Docflow.ApprovalRegistrationAssignments.Info.LocalizedName);

        // Печать.
        if (stageType == Docflow.ApprovalStage.StageType.Print)
        {
          subject = string.Join(", ", subject, Docflow.ApprovalPrintingAssignments.Info.LocalizedName);
        }
        
        // Подтверждение подписания.
        if (stageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.ConfirmSigningThreadSubject);
        }
        
        // Подписание.
        if (stageType == Docflow.ApprovalStage.StageType.Sign)
        {
          subject = string.Join(", ", subject, Docflow.ApprovalSigningAssignments.Info.LocalizedName);
        }

        // Отправка КА.
        if (stageType == Docflow.ApprovalStage.StageType.Sending)
        {
          if (document.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
              collapsedStageTypes.Contains(Docflow.ApprovalStage.StageType.Sign) ||
              collapsedStageTypes.Contains(Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign) ||
              document.ExternalApprovalState != Docflow.OfficialDocument.ExternalApprovalState.Signed)
            subject = string.Join(", ", subject, Docflow.ApprovalSendingAssignments.Info.LocalizedName);
        }
        
        // Рассмотрение.
        if (stageType == Docflow.ApprovalStage.StageType.Review)
        {
          subject = string.Join(", ", subject, Docflow.ApprovalReviewAssignments.Info.LocalizedName);
        }
        
        // Обработка резолюции.
        if (stageType == Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ReviewingResult)
        {
          subject = string.Join(", ", subject, ApprovalTasks.Resources.SpecifyReviewingResultAsgThreadSubject);
        }
        
        // Создание поручений.
        if (stageType == Docflow.ApprovalStage.StageType.Execution)
        {
          subject = string.Join(", ", subject, Docflow.ApprovalExecutionAssignments.Info.LocalizedName);
        }
      }
      subject = subject.ToLower();
      subject = Functions.Module.ReplaceFirstSymbolToUpperCase(subject.TrimStart(new[] { ',', ' ' }));
      return subject;
    }
    
    /// <summary>
    /// Получить список схлопнутых типов этапов.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <returns>Список схлопнутых этапов.</returns>
    public static List<Enumeration?> GetCollapsedStagesTypes(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage)
    {
      var stagesTypes = new List<Enumeration?>() { };
      var collapsedStages = GetCollapsedStages(task, stage);
      
      // Сформировать список типов этапов, следуя порядку в правиле.
      foreach (var collapsedStage in collapsedStages)
      {
        var stageType = collapsedStage.Stage.StageType;
        
        // Для подтверждения подписания указать это.
        if (stageType == Docflow.ApprovalStage.StageType.Sign)
        {
          var confirmBy = Functions.ApprovalStage.GetConfirmByForSignatory(collapsedStage.Stage, task.Signatory, task);
          if (confirmBy != null)
          {
            stagesTypes.Add(Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ConfirmSign);
            continue;
          }
        }
        
        // Для обработки резолюции указать это.
        if (stageType == Docflow.ApprovalStage.StageType.Review)
        {
          var assistant = Functions.ApprovalStage.GetAddresseeAssistantForResultSubmission(collapsedStage.Stage, task.Addressee, task);
          if (assistant != null)
          {
            stagesTypes.Add(Docflow.ApprovalSendingAssignmentCollapsedStagesTypesSen.StageType.ReviewingResult);
            continue;
          }
        }
        
        stagesTypes.Add(stageType);
      }
      
      return stagesTypes.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить схлопнутый результат выполнения задания.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="result">Результат подписания.</param>
    /// <returns>Исполнитель.</returns>
    public static CommonLibrary.LocalizedString GetCollapsedResult(IApprovalTask task, Enumeration? result)
    {
      var resultText = Sungero.Commons.Resources.Empty;
      var baseStage = task.ApprovalRule.Stages.FirstOrDefault(s => s.Number == task.StageNumber);
      var stage = Structures.Module.DefinedApprovalStageLite.Create(baseStage.Stage, baseStage.Number, baseStage.StageType);
      var collapsedStageTypes = GetCollapsedStages(task, stage).Select(s => s.StageType).Distinct();
      
      // Сформировать результат, следуя порядку этапов в правиле.
      foreach (var stageType in collapsedStageTypes)
      {
        // Регистрация.
        if (stageType == Docflow.ApprovalStage.StageType.Register)
        {
          var document = task.DocumentGroup.OfficialDocuments.First();
          var registrationNumber = document.RegistrationNumber;
          var documentRegister = document.DocumentRegister;
          if (registrationNumber != null && documentRegister != null)
          {
            var registerName = documentRegister.DisplayName;
            if (registerName.Length > 50)
              registerName = Sungero.Docflow.ApprovalTasks.Resources.Ellipsis_RegisterNameFormat(registerName.Substring(0, 50));
            resultText.AppendLine(ApprovalTasks.Resources.DocumentRegisteredFromNumberInDocumentRegisterFormat(registrationNumber, registerName));
          }
        }

        // Печать.
        if (stageType == Docflow.ApprovalStage.StageType.Print)
          resultText.AppendLine(ApprovalTasks.Resources.DocumentPrinted);
        
        // Подписание.
        if (stageType == Docflow.ApprovalStage.StageType.Sign)
        {
          if (result == Docflow.ApprovalSigningAssignment.Result.Sign)
            resultText.AppendLine(ApprovalTasks.Resources.DocumentSigned);
          else if (result == Docflow.ApprovalSigningAssignment.Result.ConfirmSign)
            resultText.AppendLine(ApprovalTasks.Resources.DocumentSigningConfirmed);
          else if (collapsedStageTypes.Any(s => s == StageType.Review))
            resultText.AppendLine(ApprovalTasks.Resources.DocumentSigned);
          else if (result == Docflow.ApprovalSigningAssignment.Result.ForRevision)
            return ApprovalTasks.Resources.ForRework;
          else
            return ApprovalTasks.Resources.DeniedToSignDocument;
        }
        
        // Отправка КА.
        if (stageType == Docflow.ApprovalStage.StageType.Sending)
        {
          var document = task.DocumentGroup.OfficialDocuments.First();
          if (document.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
              collapsedStageTypes.Contains(Docflow.ApprovalStage.StageType.Sign) ||
              document.ExternalApprovalState != Docflow.OfficialDocument.ExternalApprovalState.Signed)
            resultText.AppendLine(ApprovalTasks.Resources.DocumentSended);
        }
        
        // Рассмотрение.
        if (stageType == Docflow.ApprovalStage.StageType.Review)
        {
          var assistant = Functions.ApprovalStage.GetAddresseeAssistantForResultSubmission(stage.Stage, task.Addressee, task);

          if (result == ReviewResults.AddResolution)
          {
            // Сменить результат выполнения, если это внесение результата рассмотрения.
            var resultString = assistant != null ? ApprovalTasks.Resources.PassedResolutionEntered : ApprovalTasks.Resources.ResolutionPassed;
            resultText.AppendLine(resultString);
          }
          else if (result == ReviewResults.AddActionItem)
          {
            resultText.AppendLine(ApprovalTasks.Resources.SentForExecution);
          }
          else if (result == ReviewResults.Informed)
          {
            // Сменить результат выполнения, если это внесение результата рассмотрения.
            var resultString = assistant != null ? ApprovalTasks.Resources.InformedResultEntered : ApprovalTasks.Resources.Informed;
            resultText.AppendLine(resultString);
          }
          else if (result == ReviewResults.ForRework)
          {
            return ApprovalTasks.Resources.ForRework;
          }
          else
          {
            return ApprovalTasks.Resources.DeniedToReviewDocument;
          }
        }
        
        // Создание поручений.
        if (stageType == Docflow.ApprovalStage.StageType.Execution)
        {
          // Для схлопнутых заданий не выводить результат создания поручений.
          if (collapsedStageTypes.Count() == 1)
            resultText.AppendLine(ApprovalTasks.Resources.Done);
        }
      }
      
      return resultText;
    }
    
    #endregion
    
    #region Проверка схлопнутости этапа с другим
    
    /// <summary>
    /// Проверить, схлапывается ли текущий этап с указанным типом этапа.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="currentStage">Текущий этап.</param>
    /// <param name="specificStageType">Целевой тип.</param>
    /// <returns>True, если схлопнут, иначе false.</returns>
    [Remote(IsPure = true)]
    public static bool CurrentStageCollapsedWithSpecificStage(IApprovalTask task, Structures.Module.DefinedApprovalStageLite currentStage, Enumeration specificStageType)
    {
      if (currentStage == null)
        return false;
      
      var collapsedStages = GetCollapsedStages(task, currentStage);
      return collapsedStages.Any(s => s.StageType == specificStageType);
    }
    
    /// <summary>
    /// Проверить, схлапывается ли текущий этап с указанным типом этапа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="currentStageNumber">Текущий номер этапа.</param>
    /// <param name="specificStageType">Целевой тип.</param>
    /// <returns>True, если схлопнут, иначе false.</returns>
    [Remote(IsPure = true)]
    public static bool CurrentStageCollapsedWithSpecificStage(IApprovalTask task, int? currentStageNumber, Enumeration specificStageType)
    {
      var currentStage = task.ApprovalRule.Stages.Where(s => s.Number == currentStageNumber).FirstOrDefault();
      if (currentStage != null)
      {
        var stage = Structures.Module.DefinedApprovalStageLite.Create(currentStage.Stage, currentStage.Number, currentStage.StageType);
        return CurrentStageCollapsedWithSpecificStage(task, stage, specificStageType);
      }
      return false;
    }
    
    #endregion
    
    #region Пропуск этапов
    
    /// <summary>
    /// Необходимо ли пропустить этап подписания.
    /// </summary>
    /// <param name="stage">Запись этапа в правиле.</param>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>True, если необходимо, иначе false.</returns>
    public bool NeedSkipSignStage(Structures.Module.DefinedApprovalStageLite stage,
                                  Sungero.Company.IEmployee signatory,
                                  Sungero.Company.IEmployee addressee)
    {
      if (stage.Stage.StageType != Sungero.Docflow.ApprovalStage.StageType.Sign)
        return false;
      
      // Пропустить, если следующим этапом идет рассмотрение и исполнитель совпадает.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var nextStageNumber = Functions.ApprovalRuleBase.GetNextStageNumber(_obj.ApprovalRule, document, stage.Number, _obj);
      if (nextStageNumber == null || !nextStageNumber.Number.HasValue)
        return false;

      var reviewStage = _obj.ApprovalRule.Stages
        .Where(s => s.Number == nextStageNumber.Number.Value)
        .Where(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Review)
        .FirstOrDefault();
      
      if (reviewStage == null)
        return false;

      signatory = signatory ?? Functions.ApprovalStage.GetStagePerformer(_obj, stage.Stage);
      addressee = addressee ?? Functions.ApprovalStage.GetStagePerformer(_obj, reviewStage.Stage);
      if (Equals(signatory, addressee))
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Необходимо ли пропустить этап отправки контрагенту.
    /// </summary>
    /// <param name="isCollapsedWithSigning">Схлопнут ли текущий этап с подписанием.</param>
    /// <returns>True, если необходимо, иначе false.</returns>
    public bool NeedSkipSendingStage(bool isCollapsedWithSigning)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      // Если КА в МКДО ожидает подписи от нас или уже подписан двумя сторонами, то контроль возврата не нужен.
      if ((document.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired ||
           document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Signed) &&
          !(document.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
            isCollapsedWithSigning))
        return true;
      
      return document.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed &&
        !(document.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
          isCollapsedWithSigning);
    }
    
    #endregion
    
    #region Выдача документа
    
    /// <summary>
    /// Обновить статус согласования документа и добавить записи о выдаче документа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="returnResponsibleID">Ответственный за возврат.</param>
    public static void IssueDocument(IApprovalTask task, int returnResponsibleID)
    {
      // Статус документа и выдачу обновить, только если этап схлопнут с отправкой контрагенту.
      if (!CurrentStageCollapsedWithSpecificStage(task, task.StageNumber, Docflow.ApprovalStage.StageType.Sending))
        return;
      
      // Подписан нами: на согласовании, оба оригинала у КА.
      // Двумя сторонами: статус не меняем, оригинал у КА.
      var returnStage = Functions.ApprovalTask.GetStages(task).Stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.CheckReturn);
      var document = task.DocumentGroup.OfficialDocuments.First();
      if (document.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed &&
          document.InternalApprovalState != InternalApprovalState.Signed)
        return;
      
      if (returnStage != null)
      {
        var recipients = returnStage.Stage.Recipients.Where(a => a.Recipient != null).Select(b => b.Recipient).ToList<IRecipient>();
        if (recipients.Any())
        {
          var responsibleEmployee = Company.PublicFunctions.Module.GetEmployeesFromRecipients(recipients).FirstOrDefault();
          if (responsibleEmployee != null)
            returnResponsibleID = responsibleEmployee.Id;
        }
        else
        {
          var roles = returnStage.Stage.ApprovalRoles.Where(r => r.ApprovalRole != null);
          if (roles.Any())
          {
            var roleEmployee = Functions.ApprovalRoleBase.GetRolePerformer(roles.First().ApprovalRole, task);
            if (roleEmployee != null)
              returnResponsibleID = roleEmployee.Id;
          }
        }
      }
      
      // Если документ отправлен через сервис обмена и ответ еще не получен, не создаем новые записи выдачи.
      var trackings = document.Tracking.Where(x => x.ReturnResult == null && x.ReturnTask == null && x.ExternalLinkId != null).ToList();
      var tracking = trackings.Where(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement).LastOrDefault();
      if (tracking != null)
      {
        tracking.ReturnTask = task;
        
        // Переписать ответственного за возврат.
        if (returnStage != null)
          tracking.DeliveredTo = Sungero.Company.Employees.Get(returnResponsibleID);
        
        // Обработка приложений, отправленных через сервис обмена.
        foreach (var addendum in task.AddendaGroup.OfficialDocuments.Where(x => x.Tracking.Any(t => t.ReturnResult == null && t.ReturnTask == null && t.ExternalLinkId != null)).ToList())
        {
          var addendumTracking = addendum.Tracking.LastOrDefault(x => x.ReturnResult == null && x.ReturnTask == null && x.ExternalLinkId != null && x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement);
          
          if (addendumTracking != null)
          {
            addendumTracking.ReturnTask = task;
            
            if (returnStage != null)
              addendumTracking.DeliveredTo = tracking.DeliveredTo;
          }
        }
      }
      
      if (trackings.Any())
        return;

      // Если документ не подписан нами на момент отправки, то отправить 2 экземпляра с возвратом.
      if (returnStage != null && document.InternalApprovalState == InternalApprovalState.OnApproval)
        Functions.ApprovalTask.IssueDocumentToCounterparty(task, document, returnStage.Stage.DeadlineInDays, returnResponsibleID, Docflow.OfficialDocumentTracking.Action.Endorsement);
      else
        Functions.ApprovalTask.IssueDocumentToCounterparty(null, document, null, returnResponsibleID, Docflow.OfficialDocumentTracking.Action.Sending);
      
      if (returnStage != null && document.ExternalApprovalState != ExternalApprovalState.Signed)
        Functions.ApprovalTask.IssueDocumentToCounterparty(task, document, returnStage.Stage.DeadlineInDays, returnResponsibleID, Docflow.OfficialDocumentTracking.Action.Endorsement);
    }
    
    /// <summary>
    /// Выдать документ контрагенту.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="document">Документ.</param>
    /// <param name="days">Дней до планируемого возврата.</param>
    /// <param name="performerId">Id ответственного.</param>
    /// <param name="action">Действие (отправка контрагенту или согласование с контрагентом).</param>
    /// <remarks>Если не указать количество дней (null или 0), срок возврата указан не будет.</remarks>
    public static void IssueDocumentToCounterparty(IApprovalTask task, IOfficialDocument document, int? days, int performerId, Enumeration action)
    {
      var daysHasValue = days.HasValue && days != 0;
      Logger.DebugFormat("IssueDocumentToCounterparty: Task {0} with document {1} must be issued to {2} days with action = {3}",
                         task == null ? 0 : task.Id, document.Id, daysHasValue ? days.Value : 0, action.Value);
      var issue = document.Tracking.AddNew();
      var performer = Sungero.Company.Employees.Get(performerId);
      issue.DeliveredTo = performer;
      issue.Action = action;
      issue.DeliveryDate = Calendar.GetUserToday(performer);
      issue.IsOriginal = true;
      issue.ReturnTask = task;
      issue.Note = daysHasValue ? ApprovalTasks.Resources.CommentOnEndorsement : ApprovalTasks.Resources.CommentSigned;
      if (daysHasValue)
        issue.ReturnDeadline = Calendar.Now.AddWorkingDays(performer, days.Value).ToUserTime(performer).Date;
      else
        issue.ReturnDeadline = null;
    }

    #endregion
    
    #region Права

    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="performers">Исполнители.</param>
    /// <remarks>Сейчас не используется, оставлен для совместимости.</remarks>
    [Obsolete("Следует использовать GrantAccessRightsForAttachments.")]
    public static void GrantRightForAttachmentsToPerformers(IApprovalTask task, List<IRecipient> performers)
    {
      Functions.ApprovalTask.GrantAccessRightsForAttachments(task, performers);
    }

    /// <summary>
    /// Выдать права на вложения исполнителям заданий, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="assignments">Задания.</param>
    public virtual void GrantAccessRightsForAttachments(List<IAssignment> assignments)
    {
      var performers = assignments
        .Select(a => a.Performer)
        .Distinct()
        .ToList<IRecipient>();
      Functions.ApprovalTask.GrantAccessRightsForAttachments(_obj, performers);
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="recipients">Исполнители.</param>
    public virtual void GrantAccessRightsForAttachments(List<IRecipient> recipients)
    {
      Logger.Debug("Start GrantAccessRightsForAttachments");
      var stage = _obj.ApprovalRule.Stages
        .FirstOrDefault(s => s.Number == _obj.StageNumber);
      
      var stageRightsType = DefaultAccessRightsTypes.Change;
      
      if (stage != null)
      {
        var stageLite = Structures.Module.DefinedApprovalStageLite.Create(stage.Stage, stage.Number, stage.StageType);
        var collapsedStages = GetCollapsedStages(_obj, stageLite);
        var namesCollapsedStages = string.Join(", ", collapsedStages.Select(s => s.StageType.ToString()).ToList());
        
        Logger.DebugFormat("GetCollapsedStages: {0}", namesCollapsedStages);
        
        if (collapsedStages.All(s => s.Stage.RightType == Docflow.ApprovalStage.RightType.Read))
        {
          stageRightsType = DefaultAccessRightsTypes.Read;
          Logger.Debug("StageRightsType = Read");
        }
        
        if (collapsedStages.Any(s => s.Stage.RightType == Docflow.ApprovalStage.RightType.FullAccess))
        {
          stageRightsType = DefaultAccessRightsTypes.FullAccess;
          Logger.Debug("StageRightsType = FullAccess");
        }
      }
      
      Logger.DebugFormat("StageRightsTypeGuid = {0}", stageRightsType.ToString());
      
      foreach (var recipient in recipients)
      {
        Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, recipient, stageRightsType, false);
      }
      
      Logger.Debug("Done GrantAccessRightsForAttachments");
    }
    
    /// <summary>
    /// Установить права на вложения.
    /// </summary>
    /// <param name="recipient">Получатель прав.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    /// <param name="withRestrict">Удалить предыдущие права.</param>
    /// <remarks>Если у recipient есть личные права Доступ запрещен, установка прав на вложения произведена не будет.</remarks>
    public virtual void SetAccessRightsForAttachments(IRecipient recipient, Guid accessRightsType, bool withRestrict)
    {
      if (withRestrict && Equals(recipient, _obj.Author))
        Functions.ApprovalTask.SaveTaskInitiatorAccessRights(_obj, withRestrict);
      
      var approvalDocument = _obj.DocumentGroup.OfficialDocuments.First();
      if (!approvalDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Forbidden, recipient))
      {
        // В строгом и усиленном строгом режиме прав права на документ не понижаем, так как возникнут проблемы с выдачей прав в дальнейшем.
        if (withRestrict && approvalDocument.AccessRights.StrictMode == AccessRightsStrictMode.None)
          approvalDocument.AccessRights.RevokeAll(recipient);
        
        Functions.ApprovalTask.GrantAccessRightsOnDocument(_obj, approvalDocument, recipient, accessRightsType);
      }
      
      foreach (var document in _obj.AddendaGroup.OfficialDocuments)
      {
        if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Forbidden, recipient))
          continue;
        
        var rightType = accessRightsType;
        
        // Понизить выдаваемые права на приложения при необходимости.
        if (rightType != DefaultAccessRightsTypes.Read && !document.AccessRights.CanManage(_obj.Author) &&
            !_obj.RevokedDocumentsRights.Any(r => r.DocumentId == document.Id && r.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.FullAccess))
        {
          if (document.AccessRights.CanUpdate(_obj.Author) ||
              _obj.RevokedDocumentsRights.Any(r => r.DocumentId == document.Id && r.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Edit))
          {
            Logger.Debug("Trim granted access rights for addendum: Change");
            rightType = DefaultAccessRightsTypes.Change;
          }
          
          if (!document.AccessRights.CanUpdate(_obj.Author) &&
              !_obj.RevokedDocumentsRights.Any(r => r.DocumentId == document.Id && (r.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Edit ||
                                                                                    r.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.FullAccess)))
          {
            Logger.Debug("Trim granted access rights for addendum: Read");
            rightType = DefaultAccessRightsTypes.Read;
          }
        }
        
        // В строгом и усиленном строгом режиме прав права на приложения не понижаем, так как возникнут проблемы с выдачей прав в дальнейшем.
        if (withRestrict && document.AccessRights.StrictMode == AccessRightsStrictMode.None)
          document.AccessRights.RevokeAll(recipient);

        Functions.ApprovalTask.GrantAccessRightsOnDocument(_obj, document, recipient, rightType);
      }
    }
    
    /// <summary>
    /// Вернуть права инициатору после прекращения задачи.
    /// </summary>
    public virtual void GrantAccessRightsForAttachmentsToInitiatorOnAbort()
    {
      Logger.Debug("Start GrantAccessRightsForAttachmentsToInitiatorOnAbort");
      var approvalDocument = _obj.DocumentGroup.OfficialDocuments.First();
      var stageRightsType = DefaultAccessRightsTypes.Change;
      Functions.ApprovalTask.GrantAccessRightsOnDocument(_obj, approvalDocument, _obj.Author, stageRightsType);
      approvalDocument.Save();
      
      foreach (var document in _obj.AddendaGroup.OfficialDocuments)
      {
        var rightsType = document.AccessRights.CanUpdate(_obj.Author) ||
          _obj.RevokedDocumentsRights.Any(r => r.DocumentId == document.Id && r.RightType != Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Read) ?
          stageRightsType : DefaultAccessRightsTypes.Read;
        Functions.ApprovalTask.GrantAccessRightsOnDocument(_obj, document, _obj.Author, rightsType);
        document.Save();
      }
      
      Logger.Debug("Done GrantAccessRightsForAttachmentsToInitiatorOnAbort");
    }
    
    /// <summary>
    /// Ограничить права на документы у исполнителей заданий.
    /// </summary>
    /// <param name="assignments">Задания.</param>
    public virtual void RestrictAccessRightsForAssignmentsPerformers(List<IAssignment> assignments)
    {
      foreach (var assignment in assignments)
        Functions.ApprovalTask.RestrictAccessRightsForAssignmentPerformer(_obj, assignment);
    }
    
    /// <summary>
    /// Ограничить права на документы у исполнителя задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    public virtual void RestrictAccessRightsForAssignmentPerformer(IAssignment assignment)
    {
      var stage = _obj.ApprovalRule.Stages
        .FirstOrDefault(s => s.Number == _obj.StageNumber);
      if (stage == null)
        return;
      
      var stageLite = Structures.Module.DefinedApprovalStageLite.Create(stage.Stage, stage.Number, stage.StageType);
      var collapsedStages = GetCollapsedStages(_obj, stageLite);
      
      var lastStageWithRestrict = collapsedStages.FindLastIndex(s => s.Stage.NeedRestrictPerformerRights == true);
      var lastFullStage = collapsedStages.FindLastIndex(s => s.Stage.NeedRestrictPerformerRights != true && s.Stage.RightType == Docflow.ApprovalStage.RightType.FullAccess);
      var lastEditStage = collapsedStages.FindLastIndex(s => s.Stage.NeedRestrictPerformerRights != true && s.Stage.RightType == Docflow.ApprovalStage.RightType.Edit);
      
      if (lastStageWithRestrict != -1)
      {
        if (lastStageWithRestrict > Math.Max(lastFullStage, lastEditStage))
        {
          Logger.DebugFormat("RestrictAccessRightsForAssignmentPerformer: id assignment = {0}, set rights = Read", assignment.Id);
          Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, assignment.Performer, DefaultAccessRightsTypes.Read, true);
        }
        else if (lastStageWithRestrict > lastFullStage)
        {
          Logger.DebugFormat("RestrictAccessRightsForAssignmentPerformer: id assignment = {0}, set rights = Change", assignment.Id);
          Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, assignment.Performer, DefaultAccessRightsTypes.Change, true);
        }
      }
    }
    
    /// <summary>
    /// Выдать права на документ, не дублируя существующие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="recipient">Получатель прав.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    public virtual void GrantAccessRightsOnDocument(IOfficialDocument document, IRecipient recipient, Guid accessRightsType)
    {
      if (document.AccessRights.IsGrantedDirectly(accessRightsType, recipient) ||
          (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, recipient) && accessRightsType != DefaultAccessRightsTypes.FullAccess) ||
          document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, recipient))
      {
        Logger.DebugFormat("Already Granted Access Rights {0} For Attachments {1}, performer: {2}", accessRightsType, document.Id, recipient.Id);
        return;
      }
      
      // Если выдан "Доступ запрещен" на документ, то дополнительно права не выдавать.
      if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Forbidden, recipient))
      {
        Logger.DebugFormat("Granted Access Rights {0} For Attachments {1}, performer: {2}", DefaultAccessRightsTypes.Forbidden, document.Id, recipient.Id);
        return;
      }
      
      if ((accessRightsType == DefaultAccessRightsTypes.Change || accessRightsType == DefaultAccessRightsTypes.FullAccess) &&
          document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, recipient))
      {
        Logger.DebugFormat("Revoke Read Access Rights For Attachments {0}, performer: {1}", document.Id, recipient.Id);
        document.AccessRights.Revoke(recipient, DefaultAccessRightsTypes.Read);
      }
      
      if (accessRightsType == DefaultAccessRightsTypes.FullAccess &&
          document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, recipient))
      {
        Logger.DebugFormat("Revoke Change Access Rights For Attachments {0}, performer: {1}", document.Id, recipient.Id);
        document.AccessRights.Revoke(recipient, DefaultAccessRightsTypes.Change);
      }
      
      Logger.DebugFormat("Grant Access Rights {0} For Attachments {1}, performer: {2}", accessRightsType, document.Id, recipient.Id);
      document.AccessRights.Grant(recipient, accessRightsType);
    }
    
    /// <summary>
    /// Ограничить права инициатора на вложения при старте.
    /// </summary>
    public virtual void RestrictAccessRightsForAttachmentsToInitiatorOnStart()
    {
      if (_obj.ApprovalRule.NeedRestrictInitiatorRights == true)
        Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, _obj.Author, DefaultAccessRightsTypes.Read, true);
    }
    
    /// <summary>
    /// Ограничить права на вложения ответственного за доработку.
    /// </summary>
    /// <param name="performer">Ответственный за доработку.</param>
    public virtual void RestrictAccessRightsForAttachmentsToReworkPerformer(IRecipient performer)
    {
      if (_obj.ApprovalRule.NeedRestrictInitiatorRights == true)
        Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, performer, DefaultAccessRightsTypes.Read, true);
    }
    
    /// <summary>
    /// Ограничить права инициатора на вложения при доработке.
    /// </summary>
    public virtual void RestrictAccessRightsForAttachmentsToInitiatorOnRework()
    {
      if (_obj.ApprovalRule.NeedRestrictInitiatorRights == true)
        Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, _obj.Author, DefaultAccessRightsTypes.Read, true);
      else
        Functions.ApprovalTask.GrantAccessRightsForAttachments(_obj, new List<IRecipient>() { _obj.Author });
    }
    
    /// <summary>
    /// Сохранить права инициатора задачи перед отбором.
    /// </summary>
    [Obsolete("Используйте метод SaveTaskInitiatorAccessRights(bool isStrictMode)")]
    public virtual void SaveTaskInitiatorAccessRights()
    {
      this.SaveTaskInitiatorAccessRights(false);
    }
    
    /// <summary>
    /// Сохранить права инициатора задачи перед отбором.
    /// </summary>
    /// <param name="isStrictMode">Учитывать строгий режим.</param>
    public virtual void SaveTaskInitiatorAccessRights(bool isStrictMode)
    {
      var documents = new List<IEntity>();
      // В строгом и усиленном строгом режиме прав не сохраняем права, так как они не понижаются.
      documents.AddRange(isStrictMode ? _obj.DocumentGroup.All.Where(d => d.AccessRights.StrictMode == AccessRightsStrictMode.None) : _obj.DocumentGroup.All);
      documents.AddRange(isStrictMode ? _obj.AddendaGroup.All.Where(a => a.AccessRights.StrictMode == AccessRightsStrictMode.None) : _obj.AddendaGroup.All);
      
      foreach (var document in documents)
      {
        Enumeration? rightsType = null;
        if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, _obj.Author))
          rightsType = Docflow.ApprovalTaskRevokedDocumentsRights.RightType.FullAccess;
        else if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, _obj.Author))
          rightsType = Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Edit;
        else if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, _obj.Author))
          rightsType = Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Read;
        
        if (rightsType != null)
        {
          Logger.DebugFormat("SaveTaskInitiatorAccessRights: Check Access Rights DocumentId = {0} , RightType: {1}", document.Id, rightsType.ToString());

          var revokedRights = _obj.RevokedDocumentsRights.SingleOrDefault(r => r.DocumentId == document.Id);
          
          if (revokedRights != null &&
              (revokedRights.RightType == rightsType ||
               revokedRights.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.FullAccess ||
               revokedRights.RightType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Edit &&
               rightsType == Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Read))
          {
            Logger.DebugFormat("SaveTaskInitiatorAccessRights: Already Have Access Rights DocumentId = {0} , RightType: {1}", document.Id, revokedRights.RightType.ToString());
            continue;
          }

          Logger.DebugFormat("SaveTaskInitiatorAccessRights: Save Access Rights DocumentId = {0} , RightType: {1}", document.Id, rightsType.ToString());
          if (revokedRights == null)
            revokedRights = _obj.RevokedDocumentsRights.AddNew();
          
          revokedRights.DocumentId = document.Id;
          revokedRights.RightType = rightsType;
        }
      }
    }
    
    /// <summary>
    /// Выдать права на документ и приложения при пропуске этапа согласования.
    /// </summary>
    /// <param name="stage">Этап.</param>
    /// <param name="recipient">Получатель прав.</param>
    public virtual void SetSkippedRecipientAccessRights(IApprovalStage stage, IRecipient recipient)
    {
      if (stage.NeedRestrictPerformerRights == true)
        Functions.ApprovalTask.SetAccessRightsForAttachments(_obj, recipient, DefaultAccessRightsTypes.Read, true);
    }
    
    #endregion
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (primaryDocument == null)
        return;
      
      Logger.DebugFormat("ApprovalTask (ID = {0}). Add relation with type Addendum from primary document (ID = {1})",
                         _obj.Id, primaryDocument.Id);
      var taskAddenda = _obj.AddendaGroup.OfficialDocuments
        .Where(x => !Equals(x, primaryDocument))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x))
        .ToList();
      Docflow.PublicFunctions.OfficialDocument.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, taskAddenda);
    }
    
    /// <summary>
    /// Получить список операций по всем операциям, относящимся к данной группе вложений из истории.
    /// </summary>
    /// <param name="groupId">ИД группы вложений.</param>
    /// <returns>Список, содержащий историю операций по данной группе вложений.</returns>
    [Remote]
    public virtual Structures.Module.AttachmentHistoryEntries GetAttachmentHistoryEntriesByGroupId(Guid groupId)
    {
      var taskGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
      var taskHistory = Sungero.Workflow.WorkflowHistories.GetAll()
        .Where(h => h.EntityId.HasValue && _obj.Id == h.EntityId.Value && taskGuid == h.EntityType).ToList();
      var taskAssignments = Sungero.Workflow.Assignments.GetAll()
        .Where(x => Equals(x.Task, _obj)).ToList();
      var taskAssignmentsIds = taskAssignments.Select(x => x.Id).ToList();
      var taskAssignmentsNameGuids = taskAssignments.Select(x => x.GetEntityMetadata().GetOriginal().NameGuid).Distinct().ToList();
      var taskAssignmentsHistory = Sungero.Workflow.WorkflowHistories.GetAll()
        .Where(h => h.EntityId.HasValue && taskAssignmentsIds.Contains(h.EntityId.Value) && taskAssignmentsNameGuids.Contains(h.EntityType));
      
      var attachmentHistoryEntries = Functions.Module.ParseAttachmentsHistory(taskHistory.Union(taskAssignmentsHistory));
      attachmentHistoryEntries.Added = attachmentHistoryEntries.Added.Where(x => x.GroupId == groupId).ToList();
      attachmentHistoryEntries.Removed = attachmentHistoryEntries.Removed.Where(x => x.GroupId == groupId).ToList();
      
      return attachmentHistoryEntries;
    }
    
    #endregion
    
    /// <summary>
    /// Обновить статусы документа при прекращении задачи.
    /// </summary>
    /// <param name="setObsolete">Признак установки статуса "Устаревший".</param>
    public virtual void SetDocumentStateAborted(bool setObsolete)
    {
      Logger.DebugFormat("SetDocumentStateAborted: set task {0} document state aborted with obsolete = {1}", _obj.Id, setObsolete);
      Functions.ApprovalTask.UpdateApprovalState(_obj, Docflow.OfficialDocument.InternalApprovalState.Aborted);
      
      if (setObsolete)
      {
        var document = _obj.DocumentGroup.OfficialDocuments.First();
        var isActive = document.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active;
        Sungero.Docflow.PublicFunctions.OfficialDocument.SetObsolete(document, isActive);
      }
    }
    
    /// <summary>
    /// Обновить статусы документа при прекращении задачи асинхронно.
    /// </summary>
    /// <param name="setObsolete">Признак установки статуса "Устаревший".</param>
    /// <param name="needSetState">Признак установки статуса.</param>
    /// <param name="needGrantAccessRightsOnDocument">Признак необходимости восстановить права инициатора.</param>
    public virtual void SetDocumentStateAbortedAsync(bool setObsolete, bool needSetState, bool needGrantAccessRightsOnDocument)
    {
      var asyncHandler = Docflow.AsyncHandlers.SetDocumentStateAborted.Create();
      asyncHandler.TaskId = _obj.Id;
      asyncHandler.SetObsolete = setObsolete;
      asyncHandler.AbortedDate = Calendar.Now;
      asyncHandler.NeedSetState = needSetState;
      asyncHandler.NeedGrantAccessRightsOnDocument = needGrantAccessRightsOnDocument;
      
      asyncHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Проверка, что проверяемый этап идет до определенного этапа в регламенте.
    /// </summary>
    /// <param name="firstStageType">Проверяемый этап.</param>
    /// <param name="secondStageType">Этап, который должен идти после проверяемого.</param>
    /// /// <param name="allowAdditionalApprovers">Признак этапа с дополнительными согласующими.</param>
    /// <returns>True, если проверяемый этап идет до определенного этапа в регламенте.</returns>
    [Remote(IsPure = true)]
    public bool CheckSequenceOfCoupleStages(Enumeration firstStageType, Enumeration secondStageType, bool allowAdditionalApprovers)
    {
      var stagesSequence = Functions.ApprovalTask.GetStages(_obj);
      
      var lastStageWithFirstType = stagesSequence.Stages.Where(st => st.Stage.StageType == firstStageType).LastOrDefault();
      var lastStageWithSecondType = stagesSequence.Stages.Where(st => st.Stage.StageType == secondStageType && st.Stage.AllowAdditionalApprovers == allowAdditionalApprovers).LastOrDefault();
      
      if (lastStageWithFirstType == null)
        return false;
      
      if (lastStageWithSecondType == null)
        return false;
      
      var lastStageWithFirstTypeNumber = stagesSequence.Stages.IndexOf(lastStageWithFirstType);
      var lastStageWithSecondTypeNumber = stagesSequence.Stages.IndexOf(lastStageWithSecondType);
      
      return lastStageWithFirstTypeNumber < lastStageWithSecondTypeNumber;
    }
    
    /// <summary>
    /// Проверка наличия прав на подпись документов во вложении у сотрудника, выбранного в качестве подписывающего.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="stages">Этапы согласования в правильном порядке.</param>
    /// <returns>True - если выбранный подписывающий имеет право подписи документа или
    /// в случае, если поле "На подпись" не заполнено (для обычной валидации).
    /// False - если у выбранного сотрудника нет права подписи.
    /// </returns>
    [Remote(IsPure = true)]
    public bool CheckSignatory(IEmployee signatory,  System.Collections.Generic.List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return false;

      var hasSignStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Docflow.ApprovalStage.StageType.Sign, document, stages);
      if (!hasSignStage)
        return true;
      
      if (signatory != null)
        return Functions.OfficialDocument.CanSignByEmployee(document, signatory);
      else
        return false;
    }
    
    /// <summary>
    /// Получить последнее задание по задаче.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="assignmentCreated">Дата создания задания на доработку.</param>
    /// <returns>Последнее задание по задаче.</returns>
    public static IAssignment GetLastTaskAssigment(ITask task, DateTime? assignmentCreated)
    {
      var taskAssignments = Assignments.GetAll().Where(o => Equals(o.Task, task));

      if (assignmentCreated.HasValue)
        taskAssignments = taskAssignments.Where(o => o.Created < assignmentCreated);
      
      var lastAssignment = taskAssignments.OrderByDescending(o => o.Created).FirstOrDefault();
      
      return lastAssignment;
    }
    
    /// <summary>
    /// Получить список всех заданий текущей задачи, параллельных указанному заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Список заданий.</returns>
    public virtual List<IAssignment> GetParallelAssignments(IAssignment assignment)
    {
      var assignments = new List<IAssignment>();
      if (assignment == null)
        return assignments;
      
      assignments = Functions.ApprovalTask.GetTaskAssigments(_obj);
      return assignments
        .Where(a => Equals(a.BlockUid, assignment.BlockUid))
        .Where(a => a.TaskStartId == assignment.TaskStartId && a.IterationId == assignment.IterationId)
        .ToList();
    }
    
    /// <summary>
    /// Проверка, не отказал ли подписывающий.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="assignmentCreated">Дата создания задания на доработку.</param>
    /// <returns>True, если подписание завершилось с результатом "Отказать".</returns>
    public static bool IsSignatoryAbortTask(ITask task, DateTime? assignmentCreated)
    {
      var lastAssignment = GetLastTaskAssigment(task, assignmentCreated);
      if (lastAssignment == null || !ApprovalSigningAssignments.Is(lastAssignment))
        return false;
      
      return lastAssignment.Result == Docflow.ApprovalSigningAssignment.Result.Abort;
    }
    
    /// <summary>
    /// Проверка, не отказал ли КА в подписании.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="assignmentCreated">Дата создания задания на доработку.</param>
    /// <returns>True, если подписание завершилось с результатом "Отказать".</returns>
    public static bool IsExternalSignatoryAbortTask(ITask task, DateTime? assignmentCreated)
    {
      var lastAssignment = GetLastTaskAssigment(task, assignmentCreated);
      if (lastAssignment == null || !ApprovalCheckReturnAssignments.Is(lastAssignment))
        return false;
      
      return lastAssignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.NotSigned;
    }
    
    /// <summary>
    /// Проверка, запрошено ли УОУ контрагентом.
    /// </summary>
    /// <param name="document">Документ для согласования.</param>
    /// <returns>True, если пришло УОУ.</returns>
    public static bool IsInvoiceAmendmentRequest(IOfficialDocument document)
    {
      var documentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(document);
      if (documentInfo == null)
        return false;
      
      if (documentInfo.BuyerAcceptanceStatus == Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected)
        return false;
      
      var serviceDocument = documentInfo.ServiceDocuments
        .Where(x => x.Date != null && (x.DocumentType == ExchDocumentType.Reject || x.DocumentType == ExchDocumentType.IReject))
        .OrderByDescending(x => x.Date)
        .FirstOrDefault();
      
      if (serviceDocument == null)
        return false;
      
      return serviceDocument.DocumentType == ExchDocumentType.IReject;
    }
    
    /// <summary>
    /// Проверка, не отказал ли рассматривающий.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="assignmentCreated">Дата создания задания на доработку.</param>
    /// <returns>True, если рассмотрение завершилось с результатом "Отказать".</returns>
    public static bool IsAddresseeAbortTask(ITask task, DateTime? assignmentCreated)
    {
      var lastAssignment = GetLastTaskAssigment(task, assignmentCreated);
      if (lastAssignment == null || !ApprovalReviewAssignments.Is(lastAssignment))
        return false;
      
      return lastAssignment == null ? false : lastAssignment.Result == Docflow.ApprovalReviewAssignment.Result.Abort;
    }
    
    /// <summary>
    /// Проверка, что статус вложения Отозван.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <returns>True, если документ во вложении со статусом "Отозван".</returns>
    public static bool IsAttachmentObsolete(ITask task)
    {
      var lastAssignment = GetLastTaskAssigment(task, null);
      var isAttachmentObsolete = lastAssignment.AllAttachments.Where(x => OfficialDocuments.Is(x) && OfficialDocuments.As(x).ExchangeState == Docflow.OfficialDocument.ExchangeState.Obsolete).Any();
      return isAttachmentObsolete;
    }
    
    /// <summary>
    /// Получить признак наличия согласования автором задачи или исполнителем задания доработки.
    /// </summary>
    /// <param name="assignee">Автор задачи или исполнитель задания доработки.</param>
    /// <param name="approvers">Список согласующих, в который может попасть инициатор.</param>
    /// <returns>Признак согласования инициатором и признак необходимости усиленной подписи.</returns>
    [Remote(IsPure = true)]
    public Structures.ApprovalTask.ApprovalStatus AuthorMustApproveDocument(IUser assignee, List<IRecipient> approvers)
    {
      var stages = Functions.ApprovalTask.GetStages(_obj).Stages;
      var approvalStages = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Approvers);
      var managerStage = stages.FirstOrDefault(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Manager);
      
      var approvalWithAssignee = approvalStages
        .Where(s => Functions.ApprovalStage.GetStagePerformers(_obj, s.Stage, approvers).Contains(assignee))
        .ToList();
      if (managerStage != null && Equals(Functions.ApprovalStage.GetStagePerformer(_obj, managerStage.Stage), assignee))
        approvalWithAssignee.Add(managerStage);
      
      return Structures.ApprovalTask.ApprovalStatus
        .Create(approvalWithAssignee.Any(), approvalWithAssignee.Any(a => a.Stage.NeedStrongSign == true));
    }
    
    /// <summary>
    /// Проверить, согласован ли пользователем документ в рамках последней итерации согласования.
    /// </summary>
    /// <param name="user">Пользователь, чья подпись проверяется.</param>
    /// <returns>True, если имеется согласующая валидная подпись.</returns>
    public bool HasValidSignature(IUser user)
    {
      // Определить дату старта новой итерации.
      var lastRework = ApprovalReworkAssignments
        .GetAll(a => Equals(a.MainTask, _obj) && a.Status == Workflow.AssignmentBase.Status.Completed)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      if (lastRework != null)
        Logger.DebugFormat("Find last rework assignment id {0}.", lastRework.Id);
      
      var lastIterationDate = (lastRework != null && lastRework.Created > _obj.Started) ? lastRework.Created : _obj.Started;
      Logger.DebugFormat("Find last iteration date {0}.", lastIterationDate);
      
      // Найти исполнителей заданий на согласование.
      var approvalAssignments = Assignments
        .GetAll(a => Equals(a.MainTask, _obj) &&
                a.Created >= lastIterationDate &&
                (ApprovalAssignments.Is(a) ||
                 ApprovalManagerAssignments.Is(a) ||
                 ApprovalSigningAssignments.Is(a) ||
                 ApprovalReworkAssignments.Is(a)));
      var approvers = approvalAssignments
        .Select(a => a.CompletedBy)
        .ToList();
      approvers.AddRange(approvalAssignments.Select(a => a.Performer));
      if (lastRework == null || Equals(lastRework.Performer, _obj.Author))
        approvers.Add(_obj.Author);
      
      // Если проверяемый пользователь не выполнял задания согласования,
      // то считаем, что он не мог согласовать документ в рамках процесса.
      if (!approvers.Contains(user))
      {
        Logger.DebugFormat("No assignment for user Id {0}.", user.Id);
        return false;
      }

      var isReworkPerformer = lastRework != null && Equals(lastRework.CompletedBy, user);
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null || document.LastVersion == null)
      {
        if (Equals(_obj.StartedBy, user) && lastRework == null || isReworkPerformer)
          return true;
        
        var approvalAssignment = approvalAssignments.Where(a => Equals(a.CompletedBy, user)).OrderByDescending(i => i.Modified).FirstOrDefault();
        if (approvalAssignment != null)
          Logger.DebugFormat("Find approval assignment id {0} with approved {1}.", approvalAssignment.Id, approvalAssignment.Result == Docflow.ApprovalAssignment.Result.Approved);
        
        return approvalAssignment == null ? false : approvalAssignment.Result == Docflow.ApprovalAssignment.Result.Approved;
      }
      
      var hasUserSignature = Signatures.Get(document.LastVersion)
        .Any(s => Equals(s.SubstitutedUser ?? s.Signatory, user) && s.SignatureType != SignatureType.NotEndorsing && s.IsValid);
      return hasUserSignature;
    }
    
    /// <summary>
    /// Обновить статус согласования основного документа в задаче на согласование по регламенту.
    /// </summary>
    /// <param name="state">Новый статус.</param>
    public virtual void UpdateApprovalState(Enumeration? state)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      Functions.ApprovalTask.UpdateDocumentApprovalState(_obj, document, state);
    }
    
    /// <summary>
    /// Обновить статус согласования приложений в задаче на согласование по регламенту.
    /// </summary>
    /// <param name="assignment">Текущее задание.</param>
    /// <param name="state">Новый статус.</param>
    public virtual void UpdateAddendaApprovalState(IAssignment assignment, Enumeration? state)
    {
      var addenda = _obj.AddendaGroup.OfficialDocuments.Where(a => a.HasVersions).ToList();
      foreach (var document in addenda)
      {
        var hasApprovalSign = Functions.ApprovalTask.DocumentHasApprovalSignature(_obj, document, assignment);
        if (hasApprovalSign)
          Functions.ApprovalTask.UpdateDocumentApprovalState(_obj, document, state);
      }
    }
    
    /// <summary>
    /// Обновить статус согласования документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="state">Новый статус.</param>
    public virtual void UpdateDocumentApprovalState(IOfficialDocument document, Enumeration? state)
    {
      var currentState = document.InternalApprovalState;
      
      if (!Memos.Is(document) && state == Docflow.Memo.InternalApprovalState.Reviewed)
      {
        Logger.DebugFormat("UpdateApprovalState: change state Reviewed -> Signed");
        state = InternalApprovalState.Signed;
      }
      
      // Не меняем статус подписанного документа.
      if (currentState == InternalApprovalState.Signed && !(Memos.Is(document) && state == Docflow.Memo.InternalApprovalState.Reviewed))
      {
        Logger.DebugFormat("UpdateApprovalState: Task {0}, document {1} already signed.", _obj.Id, document.Id);
        return;
      }
      
      // Не меняем статус рассмотренного документа.
      if (currentState == Docflow.Memo.InternalApprovalState.Reviewed)
      {
        Logger.DebugFormat("UpdateApprovalState: Task {0}, document {1} already reviewed.", _obj.Id, document.Id);
        return;
      }
      
      Logger.DebugFormat("UpdateApprovalState: Task {3}, document {0}, {1} -> {2}", document.Id, currentState, state, _obj.Id);
      
      if (document.InternalApprovalState != state)
        document.InternalApprovalState = state;
    }
    
    /// <summary>
    /// Установить подписанта для основного документа.
    /// </summary>
    /// <param name="assignment">Текущее задание.</param>
    /// <param name="signatory">Подписывающий.</param>
    public virtual void SetDocumentSignatory(IAssignment assignment, IEmployee signatory)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var hasApprovalSign = Functions.ApprovalTask.DocumentHasApprovalSignature(_obj, document, assignment);
      if (!ApprovalReviewAssignments.Is(assignment) || hasApprovalSign || ApprovalReviewAssignments.As(assignment).IsResultSubmission == true)
        Functions.OfficialDocument.SetDocumentSignatory(document, signatory);
    }
    
    /// <summary>
    /// Установить подписанта для приложений.
    /// </summary>
    /// <param name="assignment">Текущее задание.</param>
    /// <param name="signatory">Подписывающий.</param>
    public virtual void SetAddendaSignatory(IAssignment assignment, IEmployee signatory)
    {
      var addenda = _obj.AddendaGroup.OfficialDocuments.Where(a => a.HasVersions).ToList();
      foreach (var document in addenda)
      {
        var hasApprovalSign = Functions.ApprovalTask.DocumentHasApprovalSignature(_obj, document, assignment);
        if (hasApprovalSign)
          Functions.OfficialDocument.SetDocumentSignatory(document, signatory);
      }
    }
    
    /// <summary>
    /// Проверить, что на документе в рамках задания была установлена утверждающая подпись.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="assignment">Задание.</param>
    /// <returns>Признак утверждающей подписи.</returns>
    public virtual bool DocumentHasApprovalSignature(IOfficialDocument document, IAssignment assignment)
    {
      return Signatures.Get(document.LastVersion).Any(s => s.SignatureType == SignatureType.Approval &&
                                                      s.Signatory != null && Equals(s.Signatory, assignment.CompletedBy));
    }
    
    /// <summary>
    /// Обработчик изменения правила согласования.
    /// </summary>
    /// <param name="rule">Новое правило.</param>
    /// <param name="stages">Список этапов согласования.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void ApprovalRuleChanged(IApprovalRuleBase rule, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var baseStages = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stages);
      this.ApprovalRuleChanged(rule, baseStages);
    }
    
    /// <summary>
    /// Обработчик изменения правила согласования.
    /// </summary>
    /// <param name="rule">Новое правило.</param>
    /// <param name="stages">Список этапов согласования.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void ApprovalRuleChanged(IApprovalRuleBase rule, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      this.UpdateReglamentApprovers(rule, stages);
      _obj.AddApprovers.Clear();
      _obj.AddApproversExpanded.Clear();
      _obj.Signatory = null;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var memo = Memos.As(document);
      if (memo == null)
      {
        _obj.IsManyAddressees = null;
        _obj.Addressee = null;
        _obj.Addressees.Clear();
      }
      
      if (rule == null)
        return;
      
      var hasConditionWithSignatoryRole = false;
      var hasConditionWithSignAssistantRole = false;
      var hasConditionWithPrintRespRole = false;
      List<Sungero.Docflow.IApprovalRuleBaseConditions> conditions = null;
      
      if (document != null)
      {
        // Список достижимых условий в правиле согласования.
        conditions = Functions.ApprovalRuleBase.GetConditions(rule, document, _obj);
        
        hasConditionWithSignatoryRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(rule, conditions, Docflow.ApprovalRoleBase.Type.Signatory);
        hasConditionWithSignAssistantRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(rule, conditions, Docflow.ApprovalRoleBase.Type.SignAssistant);
        hasConditionWithPrintRespRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(rule, conditions, Docflow.ApprovalRoleBase.Type.PrintResp);
      }
      
      var signingStage = stages.Where(s => s.StageType == Docflow.ApprovalStage.StageType.Sign).FirstOrDefault();
      if (signingStage != null || hasConditionWithSignatoryRole || hasConditionWithSignAssistantRole || hasConditionWithPrintRespRole)
        _obj.Signatory = Functions.Module.GetPerformerSignatory(_obj);
      
      var reviewStage = stages.Where(s => s.StageType == Docflow.ApprovalStage.StageType.Review).FirstOrDefault();
      
      // Заполнить адресатов из документа.
      if (memo != null)
      {
        var hasReviewStage = reviewStage != null;
        var hasReviewTaskStage = Functions.ApprovalRuleBase.HasApprovalReviewTaskStage(rule, document, stages);
        var hasConditionManyAddressees = Functions.ApprovalRuleBase.HasApprovalCondition(rule, document, _obj, Docflow.Condition.ConditionType.ManyAddressees);
        var hasConditionWithAddresseeRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(rule, conditions, Docflow.ApprovalRoleBase.Type.Addressee);
        var hasConditionWithAddrAssistantRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(rule, conditions, Docflow.ApprovalRoleBase.Type.AddrAssistant);
        
        var addresseesIsVisible = hasReviewTaskStage || (!hasReviewStage && hasConditionManyAddressees);
        var addresseeIsVisible = !addresseesIsVisible && (hasReviewStage || hasConditionWithAddresseeRole || hasConditionWithAddrAssistantRole);
        
        Functions.ApprovalTask.SychronizeMemoAddressees(_obj, memo);
        
        if (addresseeIsVisible && _obj.Addressees.Count > 1)
          Functions.ApprovalTask.ClearAddresseesAndFillFirstAddressee(_obj);
      }
      
      // Заполнить адресата из этапа рассмотрения, если указан выделенный сотрудник.
      if (reviewStage != null && ApprovalStages.Is(reviewStage.StageBase))
      {
        var reviewApprovalStage = ApprovalStages.As(reviewStage.StageBase);
        if (reviewApprovalStage.AssigneeType == Sungero.Docflow.ApprovalStage.AssigneeType.Employee &&
            reviewApprovalStage.Assignee != null &&
            Company.Employees.Is(reviewApprovalStage.Assignee))
          _obj.Addressee = Company.Employees.As(reviewApprovalStage.Assignee);
      }
      
      var hasSendStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Docflow.ApprovalStage.StageType.Sending, document, stages) ||
        Functions.ApprovalRuleBase.HasApprovalCondition(_obj.ApprovalRule, document, _obj, Docflow.ConditionBase.ConditionType.DeliveryMethod);
      if (hasSendStage == true)
      {
        _obj.ExchangeService = Functions.ApprovalTask.GetExchangeServices(_obj).DefaultService;
        var outgoingDocument = OutgoingDocumentBases.As(document);
        if (_obj.ExchangeService != null)
          _obj.DeliveryMethod = Functions.MailDeliveryMethod.GetExchangeDeliveryMethod();
        else if (outgoingDocument != null && outgoingDocument.IsManyAddressees != true)
          _obj.DeliveryMethod = document.DeliveryMethod;
      }
      else
      {
        _obj.DeliveryMethod = null;
        _obj.ExchangeService = null;
      }
      
    }
    
    /// <summary>
    /// Обработчик изменения правила согласования.
    /// </summary>
    /// <param name="rule">Новое правило.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void ApprovalRuleChanged(IApprovalRuleBase rule)
    {
      var stages = Functions.ApprovalTask.GetBaseStages(_obj).BaseStages;
      this.ApprovalRuleChanged(rule, stages);
    }
    
    /// <summary>
    /// Обновить список обязательных согласующих.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="stages">Список этапов согласования.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void UpdateReglamentApprovers(IApprovalRuleBase rule, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var baseStages = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stages);
      this.UpdateReglamentApprovers(rule, baseStages);
    }
    
    /// <summary>
    /// Обновить список обязательных согласующих.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="stages">Список этапов согласования.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void UpdateReglamentApprovers(IApprovalRuleBase rule, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      if (rule == null)
      {
        _obj.ReqApprovers.Clear();
        return;
      }
      
      var reqApprovers = new List<Company.IEmployee>();
      reqApprovers.AddRange(Functions.ApprovalTask.GetAllRequiredApprovers(_obj, stages));
      
      // Включить руководителя в список обязательных согласующих.
      var managerStage = stages.Where(s => s.StageType == Docflow.ApprovalStage.StageType.Manager).FirstOrDefault();
      if (managerStage != null && ApprovalStages.Is(managerStage.StageBase))
      {
        var manager = Functions.ApprovalStage.GetRemoteStagePerformer(_obj, ApprovalStages.As(managerStage.StageBase));
        if (manager != null && !manager.Equals(_obj.Author))
          reqApprovers.Add(manager);
      }
      
      if (!_obj.ReqApprovers.Select(a => a.Approver).SequenceEqual(reqApprovers))
      {
        _obj.ReqApprovers.Clear();
        foreach (var approver in reqApprovers)
          _obj.ReqApprovers.AddNew().Approver = approver;
      }
    }
    
    /// <summary>
    /// Обновить список обязательных согласующих.
    /// </summary>
    /// <param name="rule">Правило.</param>
    [Remote(PackResultEntityEagerly = true)]
    public virtual void UpdateReglamentApprovers(IApprovalRuleBase rule)
    {
      var stages = Functions.ApprovalTask.GetStages(_obj).Stages;
      this.UpdateReglamentApprovers(rule, stages);
    }

    /// <summary>
    /// Получить всех обязательных сотрудников процесса согласования.
    /// </summary>
    /// <returns>Список обязательных сотрудников.</returns>
    public List<IEmployee> GetAllRequiredApprovers()
    {
      var stages = Functions.ApprovalTask.GetStages(_obj).Stages;
      return this.GetAllRequiredApprovers(stages);
    }
    
    /// <summary>
    /// Получить всех обязательных сотрудников процесса согласования.
    /// </summary>
    /// <param name="stages">Список этапов согласования.</param>
    /// <returns>Обязательные сотрудники.</returns>
    public virtual List<IEmployee> GetAllRequiredApprovers(List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var baseStages = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stages);
      return this.GetAllRequiredApprovers(baseStages);
    }
    
    /// <summary>
    /// Получить всех обязательных сотрудников процесса согласования.
    /// </summary>
    /// <param name="stages">Список этапов согласования.</param>
    /// <returns>Обязательные сотрудники.</returns>
    public virtual List<IEmployee> GetAllRequiredApprovers(List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      var approversStages = stages
        .Where(s => s.StageType == Docflow.ApprovalStage.StageType.Approvers)
        .Where(s => ApprovalStages.Is(s.StageBase))
        .Select(s => ApprovalStages.As(s.StageBase))
        .ToList();
      
      var recipients = new List<IRecipient>();
      foreach (var stage in approversStages)
      {
        // Сотрудники/группы.
        if (stage.Recipients.Any())
          recipients.AddRange(stage.Recipients
                              .Where(rec => rec.Recipient != null)
                              .Select(rec => rec.Recipient)
                              .ToList());
        
        // Роли согласования.
        if (stage.ApprovalRoles.Any())
        {
          // Обработка ролей с одним участником.
          recipients.AddRange(stage.ApprovalRoles
                              .Where(r => r.ApprovalRole != null && r.ApprovalRole.Type != Docflow.ApprovalRoleBase.Type.Approvers)
                              .Select(r => Functions.ApprovalRoleBase.GetRolePerformer(r.ApprovalRole, _obj))
                              .Where(r => r != null)
                              .ToList());
          
          // Обработка ролей с несколькими участниками.
          recipients.AddRange(stage.ApprovalRoles
                              .Where(r => r.ApprovalRole != null)
                              .SelectMany(r => Functions.ApprovalRoleBase.GetRolePerformers(r.ApprovalRole, _obj)));
        }
      }

      var performers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(recipients).Distinct().ToList();
      var assignments = ApprovalAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && Equals(a.TaskStartId, _obj.StartId))
        .ToList();
      
      // Поиск обязательных.
      foreach (var assignment in assignments)
      {
        if (!_obj.AddApproversExpanded.Any(x => Equals(x.Approver, assignment.Performer)))
        {
          performers.Add(Employees.As(assignment.Performer));
          performers = performers.Distinct().ToList();
        }
      }

      var start = assignments.Count + 1;
      while (start > assignments.Count)
      {
        start = assignments.Count;
        var delete = new List<IAssignment>();
        foreach (var assignment in assignments)
        {
          if (assignment.ForwardedTo == null)
            continue;
          
          if (performers.Contains(assignment.Performer))
          {
            performers.AddRange(assignment.ForwardedTo.Select(u => Employees.As(u)));
            performers = performers.Distinct().ToList();
            delete.Add(assignment);
          }
        }
        assignments.RemoveAll(a => delete.Contains(a));
      }

      return performers;
    }
    
    /// <summary>
    /// Определить текущий этап.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stageType">Тип этапа.</param>
    /// <returns>Текущий этап, либо null, если этапа нет (или это не тот этап).</returns>
    public static Structures.Module.DefinedApprovalStageLite GetStage(IApprovalTask task, Enumeration stageType)
    {
      var stage = task.ApprovalRule.Stages
        .Where(s => s.Stage != null)
        .Where(s => s.Stage.StageType == stageType)
        .FirstOrDefault(s => s.Number == task.StageNumber);
      
      if (stage != null)
        return Structures.Module.DefinedApprovalStageLite.Create(stage.Stage, stage.Number, stage.StageType);
      
      return null;
    }
    
    /// <summary>
    /// Определить, необходим ли контроль возврата.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True, если необходим контроль возврата.</returns>
    public static bool NeedControlReturn(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.First();
      
      // Если КА в МКДО ожидает подписи от нас или уже подписан двумя сторонами, то контроль возврата не нужен.
      if (document.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired ||
          document.ExchangeState == Docflow.OfficialDocument.ExchangeState.Signed)
        return false;
      
      // Если КА подписал, то контроля возврата не нужен.
      return document.ExternalApprovalState != Docflow.OfficialDocument.ExternalApprovalState.Signed;
    }
    
    /// <summary>
    /// Получить локализованное имя результата согласования по подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <param name="emptyIfNotValid">Вернуть пустую строку, если подпись не валидна.</param>
    /// <returns>Локализованный результат подписания.</returns>
    public static string GetEndorsingResultFromSignature(Sungero.Domain.Shared.ISignature signature, bool emptyIfNotValid)
    {
      var result = string.Empty;
      
      if (emptyIfNotValid && !signature.IsValid)
        return result;
      
      switch (signature.SignatureType)
      {
        case SignatureType.Approval:
          result = ApprovalTasks.Resources.ApprovalFormApproved;
          break;
        case SignatureType.Endorsing:
          result = Docflow.Functions.Module.HasApproveWithSuggestionsMark(signature.Comment)
            ? ApprovalTasks.Resources.ApprovalFormEndorsedWithSuggestions
            : ApprovalTasks.Resources.ApprovalFormEndorsed;
          break;
        case SignatureType.NotEndorsing:
          result = ApprovalTasks.Resources.ApprovalFormNotEndorsed;
          break;
      }
      
      return result;
    }
    
    /// <summary>
    /// Заполнить SQL таблицу для формирования отчета "Лист согласования".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="reportSessionId">Идентификатор отчета.</param>
    public static void UpdateApprovalSheetReportTable(IOfficialDocument document, string reportSessionId)
    {
      var filteredSignatures = new Dictionary<string, ISignature>();
      
      var setting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var showNotApproveSign = setting != null ? setting.ShowNotApproveSign == true : false;
      
      foreach (var version in document.Versions.OrderByDescending(v => v.Created))
      {
        var versionSignatures = Signatures.Get(version).Where(s => (showNotApproveSign || s.SignatureType != SignatureType.NotEndorsing)
                                                              && s.IsExternal != true
                                                              && !filteredSignatures.ContainsKey(GetSignatureKey(s, version.Number.Value)));
        var lastSignaturesInVersion = versionSignatures
          .GroupBy(v => GetSignatureKey(v, version.Number.Value))
          .Select(grouping => grouping.Where(s => s.SigningDate == grouping.Max(last => last.SigningDate)).First());
        
        foreach (ISignature signature in lastSignaturesInVersion)
        {
          filteredSignatures.Add(GetSignatureKey(signature, version.Number.Value), signature);
          if (!signature.IsValid)
            foreach (var error in signature.ValidationErrors)
              Logger.DebugFormat("UpdateApprovalSheetReportTable: reportSessionId {0}, document {1}, version {2}, signatory {3}, substituted user {7}, signature {4}, with error {5} - {6}",
                                 reportSessionId, document.Id, version.Number,
                                 signature.Signatory != null ? signature.Signatory.Name : signature.SignatoryFullName, signature.Id, error.ErrorType, error.Message,
                                 signature.SubstitutedUser != null ? signature.SubstitutedUser.Name : string.Empty);
          
          // Dmitriev_IA: signature.AdditionalInfo формируется в Employee в действии "Получение информации о подписавшем".
          //              Может содержать лишние пробелы в должности сотрудника. US 89747.
          var employeeName = string.Empty;
          var additionalInfos = (signature.AdditionalInfo ?? string.Empty)
            .Split(new char[] { '|' }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .ToList();
          if (signature.SubstitutedUser == null)
          {
            var additionalInfo = additionalInfos.FirstOrDefault();
            employeeName = string.Format("<b>{1}</b>{0}", signature.SignatoryFullName, AddEndOfLine(additionalInfo)).Trim();
          }
          else
          {
            if (additionalInfos.Count() == 3)
            {
              // Замещающий.
              var signatoryAdditionalInfo = additionalInfos[0];
              if (!string.IsNullOrEmpty(signatoryAdditionalInfo))
                signatoryAdditionalInfo = AddEndOfLine(string.Format("<b>{0}</b>", signatoryAdditionalInfo));
              var signatoryText = AddEndOfLine(string.Format("{0}{1}", signatoryAdditionalInfo, signature.SignatoryFullName));
              
              // Замещаемый.
              var substitutedUserAdditionalInfo = additionalInfos[1];
              if (!string.IsNullOrEmpty(substitutedUserAdditionalInfo))
                substitutedUserAdditionalInfo = AddEndOfLine(string.Format("<b>{0}</b>", substitutedUserAdditionalInfo));
              var substitutedUserText = string.Format("{0}{1}", substitutedUserAdditionalInfo, signature.SubstitutedUserFullName);
              
              // Замещающий за замещаемого.
              var onBehalfOfText = AddEndOfLine(ApprovalTasks.Resources.OnBehalfOf);
              employeeName = string.Format("{0}{1}{2}", signatoryText, onBehalfOfText, substitutedUserText);
            }
            else if (additionalInfos.Count() == 2)
            {
              // Замещающий / Система.
              var signatoryText = AddEndOfLine(signature.SignatoryFullName);
              
              // Замещаемый.
              var substitutedUserAdditionalInfo = additionalInfos[0];
              if (!string.IsNullOrEmpty(substitutedUserAdditionalInfo))
                substitutedUserAdditionalInfo = AddEndOfLine(string.Format("<b>{0}</b>", substitutedUserAdditionalInfo));
              var substitutedUserText = string.Format("{0}{1}", substitutedUserAdditionalInfo, signature.SubstitutedUserFullName);
              
              // Система за замещаемого.
              var onBehalfOfText = AddEndOfLine(ApprovalTasks.Resources.OnBehalfOf);
              employeeName = string.Format("{0}{1}{2}", signatoryText, onBehalfOfText, substitutedUserText);
            }
            else
            {
              // Замещающий / Система.
              var signatoryText = AddEndOfLine(signature.SignatoryFullName);
              
              // Замещаемый.
              var substitutedUserText = signature.SubstitutedUserFullName;
              
              // Система за замещаемого.
              var onBehalfOfText = AddEndOfLine(ApprovalTasks.Resources.OnBehalfOf);
              employeeName = string.Format("{0}{1}{2}", signatoryText, onBehalfOfText, substitutedUserText);
            }
          }
          
          var commandText = Queries.ApprovalSheetReport.InsertIntoApprovalSheetReportTable;
          
          using (var command = SQL.GetCurrentConnection().CreateCommand())
          {
            var separator = ", ";
            var errorString = Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
            var signErrors = string.IsNullOrWhiteSpace(errorString)
              ? Reports.Resources.ApprovalSheetReport.SignStatusActive
              : Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(errorString.ToLower());
            var resultString = Functions.ApprovalTask.GetEndorsingResultFromSignature(signature, false);
            var comment = Docflow.Functions.Module.HasApproveWithSuggestionsMark(signature.Comment)
              ? Docflow.Functions.Module.RemoveApproveWithSuggestionsMark(signature.Comment)
              : signature.Comment;
            command.CommandText = commandText;
            SQL.AddParameter(command, "@reportSessionId",  reportSessionId, System.Data.DbType.String);
            SQL.AddParameter(command, "@employeeName",  employeeName, System.Data.DbType.String);
            SQL.AddParameter(command, "@resultString",  resultString, System.Data.DbType.String);
            SQL.AddParameter(command, "@comment",  comment, System.Data.DbType.String);
            SQL.AddParameter(command, "@signErrors",  signErrors, System.Data.DbType.String);
            SQL.AddParameter(command, "@versionNumber",  version.Number, System.Data.DbType.Int32);
            SQL.AddParameter(command, "@SignatureDate",  signature.SigningDate.FromUtcTime(), System.Data.DbType.DateTime);
            
            command.ExecuteNonQuery();
          }
        }
      }
    }

    /// <summary>
    /// Получить ключ для подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <param name="versionNumber">Номер версии.</param>
    /// <returns>Ключ для подписи.</returns>
    private static string GetSignatureKey(ISignature signature, int versionNumber)
    {
      // Если подпись не "несогласующая", она должна схлапываться вне версий.
      if (signature.SignatureType != SignatureType.NotEndorsing)
        versionNumber = 0;
      
      if (signature.Signatory != null)
      {
        if (signature.SubstitutedUser != null && !signature.SubstitutedUser.Equals(signature.Signatory))
          return string.Format("{0} - {1}:{2}:{3}", signature.Signatory.Id, signature.SubstitutedUser.Id, signature.SignatureType == SignatureType.Approval, versionNumber);
        else
          return string.Format("{0}:{1}:{2}", signature.Signatory.Id, signature.SignatureType == SignatureType.Approval, versionNumber);
      }
      else
        return string.Format("{0}:{1}:{2}", signature.SignatoryFullName, signature.SignatureType == SignatureType.Approval, versionNumber);
    }

    /// <summary>
    /// Добавить перенос в конец строки, если она не пуста.
    /// </summary>
    /// <param name="row">Строка.</param>
    /// <returns>Результирующая строка.</returns>
    private static string AddEndOfLine(string row)
    {
      return string.IsNullOrWhiteSpace(row) ? string.Empty : row + Environment.NewLine;
    }

    /// <summary>
    /// Получить плановых исполнителей.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      return this.GetTaskAssignees(true);
    }
    
    /// <summary>
    /// Получить плановых исполнителей.
    /// </summary>
    /// <param name="withObservers">Включать в результат наблюдателей.</param>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAssignees(bool withObservers)
    {
      var assignees = new List<IRecipient>();

      var approvalTask = ApprovalTasks.As(_obj);
      if (approvalTask == null)
        return assignees;

      var stages = Functions.ApprovalTask.GetStages(approvalTask).Stages.Where(s => s.Stage != null).Select(s => s.Stage);
      foreach (var stage in stages)
      {
        var stageType = stage.StageType;
        
        // Задания с одним исполнителем.
        if (stageType == StageType.Manager || stageType == StageType.Print || stageType == StageType.Sign ||
            stageType == StageType.Register || stageType == StageType.Sending ||
            stageType == StageType.Execution || stageType == StageType.Review)
        {
          var assignee = Functions.ApprovalStage.GetStagePerformer(approvalTask, stage);
          if (assignee != null)
            assignees.Add(assignee);
        }
        
        // Задания с несколькими исполнителями.
        if (stageType == StageType.Approvers ||
            stageType == StageType.CheckReturn)
        {
          var stageAssignees = Functions.ApprovalStage.GetStagePerformers(approvalTask, stage);
          if (stageAssignees.Any())
            assignees.AddRange(stageAssignees);
        }
        
        // Для заданий и уведомлений права выдать на группы/роли, а не конкретным исполнителям.
        if (stageType == StageType.SimpleAgr || stageType == StageType.Notice)
        {
          var stageAssignees = Functions.ApprovalStage.GetStageRecipients(stage, approvalTask);
          if (stageAssignees.Any())
            assignees.AddRange(stageAssignees);
        }
      }
      
      // Если есть многоадресное рассмотрение, то добавить адресатов из задачи.
      var approvalReviewTaskStages = Functions.ApprovalTask.GetBaseStages(approvalTask).BaseStages
        .Where(s => s.StageBase != null && ApprovalReviewTaskStages.Is(s.StageBase));
      if (approvalReviewTaskStages.Any())
      {
        var addressees = approvalTask.Addressees.Where(x => x.Addressee != null).Select(x => x.Addressee);
        assignees.AddRange(addressees);
      }
      
      if (withObservers)
      {
        var observers = approvalTask.Observers.Where(a => a.Observer != null).Select(a => a.Observer);
        assignees.AddRange(observers);
      }
      
      return assignees.Distinct().ToList();
    }

    /// <summary>
    /// Получение последнего задания на доработку.
    /// </summary>
    /// <returns>Последнее задание на доработку.</returns>
    public IApprovalReworkAssignment GetLastReworkAssignment()
    {
      return ApprovalReworkAssignments
        .GetAll(a => Equals(a.Task, _obj) && a.Created > _obj.Started)
        .OrderByDescending(asg => asg.Created)
        .FirstOrDefault();
    }

    /// <summary>
    /// Заполнить список согласующих в задании доп.согласующих.
    /// </summary>
    /// <param name="block">Блок доработки.</param>
    /// <param name="approvers">Список согласующих.</param>
    /// <param name="isRequiredApprovers">Признак, обязательные согласующие или нет.</param>
    public void FillApproversList(Sungero.Docflow.Server.ApprovalReworkAssignmentBlock block,
                                  List<IEmployee> approvers,
                                  bool isRequiredApprovers)
    {
      var approvalAssignments = ApprovalAssignments.GetAll(a => Equals(a.Task, _obj) && a.Created >= _obj.Started).ToList();
      var lastReworkAssignment = Functions.ApprovalTask.GetLastReworkAssignment(_obj);
      
      // Обновить список согласующих.
      foreach (var approver in approvers)
      {
        if (block.Approvers != null && block.Approvers.Any(a => Equals(a.Approver, approver)))
          continue;
        
        var newApprover = block.Approvers.AddNew();
        newApprover.Approver = approver;
        newApprover.IsRequiredApprover = isRequiredApprovers;
        
        // Согласовал или не согласовал. Если задания не было, то не согласовали.
        var approvalAssignment = approvalAssignments
          .Where(a => Equals(a.Performer, approver))
          .OrderByDescending(i => i.Modified)
          .FirstOrDefault();
        var forwarded = approvalAssignment != null && approvalAssignment.Result == Sungero.Docflow.ApprovalAssignment.Result.Forward;
        var approved = approvalAssignment != null && (approvalAssignment.Result == Sungero.Docflow.ApprovalAssignment.Result.Approved ||
                                                      approvalAssignment.Result == Sungero.Docflow.ApprovalAssignment.Result.WithSuggestions);
        if (approved)
          newApprover.Approved = Docflow.ApprovalReworkAssignmentApprovers.Approved.IsApproved;
        else if (forwarded)
          newApprover.Approved = Docflow.ApprovalReworkAssignmentApprovers.Approved.Forwarded;
        else
          newApprover.Approved = Docflow.ApprovalReworkAssignmentApprovers.Approved.NotApproved;
        
        // Было ли задание с момента последней доработки.
        var hasAssignmentAfterLastRework = approvalAssignment != null &&
          (lastReworkAssignment == null || approvalAssignment.Created >= lastReworkAssignment.Created);
        
        // Если уменьшающийся круг запрещен, то действие может быть только - Отправить на согласование.
        if (_obj.ApprovalRule.IsSmallApprovalAllowed != true)
        {
          // Исключая переадресацию.
          if (forwarded)
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
          else
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
          continue;
        }
        
        // Предыдущее действие, выбранное на доработке.
        Enumeration? lastApproverAction = null;
        if (lastReworkAssignment != null)
        {
          var lastReworkAssignmentGridApprover = lastReworkAssignment.Approvers.FirstOrDefault(app => Equals(app.Approver, approver));
          if (lastReworkAssignmentGridApprover != null)
            lastApproverAction = lastReworkAssignmentGridApprover.Action;
        }
        
        // Новое или повторное согласование (плюс переадресации -- когда в гриде было без отправки, а задание всё-таки есть).
        if (lastApproverAction == null || lastApproverAction == Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval ||
            (lastApproverAction != Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval && hasAssignmentAfterLastRework))
        {
          if (forwarded)
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
          else if (approved && hasAssignmentAfterLastRework)
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.SendNotice;
          else
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
          
          continue;
        }
        
        // В предыдущий раз не отправляли.
        if (lastApproverAction == Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend)
        {
          newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
          continue;
        }
        
        // В предыдущий раз отправили уведомление.
        if (lastApproverAction == Docflow.ApprovalReworkAssignmentApprovers.Action.SendNotice)
        {
          var notice = ApprovalNotifications
            .GetAll(a => Equals(a.Task, _obj) && a.Created >= lastReworkAssignment.Created)
            .FirstOrDefault(a => Equals(a.Performer, approver));
          if (notice != null)
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
          else
            newApprover.Action = Docflow.ApprovalReworkAssignmentApprovers.Action.SendNotice;
          continue;
        }
      }
    }

    /// <summary>
    /// Получить всех сотрудников, которые участвовали в согласовании и подписании.
    /// </summary>
    /// <returns>Список пользователей.</returns>
    [Public]
    public virtual List<IUser> GetAllApproversAndSignatories()
    {
      var approvalBlocks = new[] { "3", "6", "9" };
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Status == Sungero.Workflow.AssignmentBase.Status.Completed)
        .Where(a => approvalBlocks.Contains(a.BlockUid ?? "0"))
        .Select(a => a.Performer)
        .Distinct().ToList();
    }

    /// <summary>
    /// Получить сервисы обмена.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true)]
    public Structures.ApprovalTask.ExchangeServies GetExchangeServices()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        if (Docflow.OutgoingDocumentBases.Is(document) && Docflow.OutgoingDocumentBases.As(document).IsManyAddressees == true)
          return Structures.ApprovalTask.ExchangeServies.Create(new List<ExchangeCore.IExchangeService>(), null);
        
        if (Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document))
        {
          var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(document);
          if (info != null && info.Box.Status == CoreEntities.DatabookEntry.Status.Active)
          {
            var service = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box);
            return Structures.ApprovalTask.ExchangeServies.Create(new List<ExchangeCore.IExchangeService>() { service }, service);
          }
        }
        
        // Если есть хоть один контрагент с МКДО, но нет контрагента без МКДО.
        var parties = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparties(document);
        if (parties != null && parties.Any(p => p.Status == CoreEntities.DatabookEntry.Status.Active))
        {
          if (Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true)
          {
            var documentBox = Docflow.AccountingDocumentBases.As(document).BusinessUnitBox;
            if (documentBox.Status == CoreEntities.DatabookEntry.Status.Active)
            {
              var defaultService = Docflow.AccountingDocumentBases.As(document).BusinessUnitBox.ExchangeService;
              var services = new List<ExchangeCore.IExchangeService>() { defaultService };
              return Structures.ApprovalTask.ExchangeServies.Create(services, defaultService);
            }
          }
          
          var lines = parties.SelectMany(p => p.ExchangeBoxes.Where(b => b.Status == Parties.CounterpartyExchangeBoxes.Status.Active &&
                                                                    Equals(b.Box.BusinessUnit, document.BusinessUnit) &&
                                                                    b.Box.Status == CoreEntities.DatabookEntry.Status.Active)).ToList();
          var hasPartyWithoutActiveExchange = parties.Any(p => p.ExchangeBoxes
                                                          .Where(b => Equals(b.Box.BusinessUnit, document.BusinessUnit))
                                                          .All(b => b.Status != Parties.CounterpartyExchangeBoxes.Status.Active));
          if (lines.Any() && !hasPartyWithoutActiveExchange)
          {
            var services = lines
              .Select(l => l.Box.ExchangeService)
              .Distinct()
              .ToList();
            return Structures.ApprovalTask.ExchangeServies.Create(services, services.FirstOrDefault());
          }
        }
        
        if (parties == null || !parties.Any())
        {
          var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes();
          
          var businessUnit = document.BusinessUnit;
          if (businessUnit == null)
            businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Company.Employees.Current);
          var services = boxes.Where(b => Equals(b.BusinessUnit, businessUnit)).Select(x => x.ExchangeService).ToList()
            .Distinct()
            .ToList();
          
          return Structures.ApprovalTask.ExchangeServies.Create(services, services.FirstOrDefault());
        }
      }
      return Structures.ApprovalTask.ExchangeServies.Create(new List<ExchangeCore.IExchangeService>(), null);
    }
    
    /// <summary>
    /// Создать кеш параметров показа карточки задачи на согласование по регламенту.
    /// </summary>
    [Remote(IsPure = true)]
    public virtual void CreateParamsCache()
    {
      var refreshParameters = Functions.ApprovalTask.GetFullStagesInfoForRefresh(_obj);
      Functions.ApprovalTask.SetRefreshParams(_obj, (Domain.Shared.IExtendedEntity)_obj, refreshParameters);
      var lockInfo = Locks.GetLockInfo(_obj);
      if (_obj.Status == Sungero.Docflow.ApprovalTask.Status.Draft && _obj.AccessRights.CanUpdate() && !(lockInfo != null && lockInfo.IsLockedByOther))
      {
        var isVisibleAndEnabled = _obj.State.Properties.DeliveryMethod.IsVisible && _obj.State.Properties.DeliveryMethod.IsEnabled;
        if (isVisibleAndEnabled && (_obj.DeliveryMethod == null || _obj.DeliveryMethod.Sid != Constants.MailDeliveryMethod.Exchange))
        {
          var param = ((Domain.Shared.IExtendedEntity)_obj).Params;
          if (!param.ContainsKey(Constants.ApprovalTask.NeedShowExchangeServiceHint))
          {
            var show = Functions.ApprovalTask.GetExchangeServices(_obj).DefaultService != null;
            param[Constants.ApprovalTask.NeedShowExchangeServiceHint] = show;
          }
        }
      }
    }
    
    /// <summary>
    /// Помечает задачу для отправки на доработку, если не удалось вычислить исполнителя этапа.
    /// </summary>
    /// <param name="stage">Этап, исполнителя которого не удалось вычислить.</param>
    public void FillReworkReasonWhenAssigneeNotFound(IApprovalStage stage)
    {
      _obj.IsStageAssigneeNotFound = true;
      var hyperlink = Hyperlinks.Get(stage);
      _obj.ReworkReason = ApprovalTasks.Resources.ReworkReasonWhenAssigneeNotFoundFormat(hyperlink);
    }
    
    /// <summary>
    /// Обновить способ доставки в задаче, документе, гриде адресатов исходящего письма.
    /// </summary>
    /// <param name="deliveryMethod">Способ доставки.</param>
    public void RefreshDeliveryMethod(IMailDeliveryMethod deliveryMethod)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var outgoingDocument = OutgoingDocumentBases.As(document);
        if (outgoingDocument != null && outgoingDocument.IsManyAddressees != true)
          document.DeliveryMethod = deliveryMethod;
      }
      
      _obj.DeliveryMethod = deliveryMethod;
    }
    
    /// <summary>
    /// Обновить доп. согласующих в задаче.
    /// </summary>
    /// <param name="approvers">Список доп. согласующих.</param>
    public virtual void UpdateAdditionalApprovers(List<IRecipient> approvers)
    {
      var taskApprovers = _obj.AddApproversExpanded.Select(a => a.Approver).ToList();
      if (approvers.Except(taskApprovers).Any() || taskApprovers.Except(approvers).Any())
      {
        _obj.AddApproversExpanded.Clear();
        foreach (var approver in approvers)
          _obj.AddApproversExpanded.AddNew().Approver = approver;
      }
      
      var taskAddApprovers = _obj.AddApprovers.Select(a => a.Approver).ToList();
      if (approvers.Except(taskAddApprovers).Any() || taskAddApprovers.Except(approvers).Any())
      {
        _obj.AddApprovers.Clear();
        foreach (var approver in approvers)
          _obj.AddApprovers.AddNew().Approver = approver;
      }
      return;
    }
    
    /// <summary>
    /// Получить ожидаемый срок по задаче.
    /// </summary>
    /// <returns>Срок по задаче.</returns>
    [Remote(IsPure = true)]
    public DateTime? GetExpectedDate()
    {
      var stages = Functions.ApprovalTask.GetBaseStages(_obj).BaseStages;
      return this.GetExpectedDate(null, stages);
    }
    
    /// <summary>
    /// Получить ожидаемый срок по задаче.
    /// </summary>
    /// <param name="currentAssignment">Текущее задание.</param>
    /// <param name="stages">Список этапов согласования.</param>
    /// <returns>Срок по задаче.</returns>
    [Remote(IsPure = true)]
    public virtual DateTime? GetExpectedDate(IAssignment currentAssignment, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      if (_obj.ApprovalRule == null)
        return null;

      var currentStage = stages.FirstOrDefault(s => s.Number == (_obj.StageNumber ?? 0));
      var maxDeadline = Calendar.Now;
      var notStartedStages = stages;
      var firstNotStartedStage = currentStage;
      
      if (currentStage != null)
      {
        var assignments = Functions.ApprovalTask.GetTaskAssigments(_obj);
        if (currentAssignment != null)
          assignments.Add(currentAssignment);

        var assignmentsInProcess = assignments.Where(x => x.Status == Sungero.Docflow.ApprovalTask.Status.InProcess).ToList();
        if (assignmentsInProcess.Any())
          maxDeadline = this.GetMaxAssignmentDeadline(currentStage.StageBase, assignments);
        else
          currentStage = stages.TakeWhile(s => s != currentStage).LastOrDefault();
        
        if (currentStage != null)
          notStartedStages = notStartedStages.SkipWhile(s => s != currentStage).Skip(1).ToList();
      }
      
      foreach (IApprovalStageBase stage in notStartedStages.Select(s => s.StageBase).ToList())
      {
        // Уведомления не влияют на срок.
        var isNotice = ApprovalStages.As(stage) != null && ApprovalStages.As(stage).StageType == Sungero.Docflow.ApprovalStage.StageType.Notice;
        if (isNotice)
          continue;
        
        var inProcess = firstNotStartedStage == null ? false : Equals(firstNotStartedStage.StageBase, stage);
        maxDeadline = Functions.ApprovalStageBase.GetStageMaxDeadline(stage, _obj, maxDeadline, inProcess);
      }
      
      return maxDeadline;
    }
    
    /// <summary>
    /// Получить максимальный срок выполнения по заданиям.
    /// </summary>
    /// <param name="stageBase">Текущий этап.</param>
    /// <param name="assignments">Список заданий.</param>
    /// <returns>Максимальный срок.</returns>
    public virtual DateTime GetMaxAssignmentDeadline(IApprovalStageBase stageBase, List<IAssignment> assignments)
    {
      var maxDeadline = Calendar.Now;
      var assignmentsInProcess = assignments.Where(x => x.Status == Sungero.Docflow.ApprovalTask.Status.InProcess).ToList();
      
      if (!assignmentsInProcess.Any())
        return maxDeadline;
      
      var maxAsgDeadline = maxDeadline;
      foreach (var assignment in assignmentsInProcess.Where(d => d.Deadline.HasValue))
      {
        var currentDeadline = assignment.Deadline.Value.HasTime() ? assignment.Deadline.Value :
          assignment.Deadline.Value.EndOfDay().FromUserTime(assignment.Performer);
        if (currentDeadline > maxAsgDeadline)
          maxAsgDeadline = currentDeadline;
      }
      
      if (maxAsgDeadline > maxDeadline)
        maxDeadline = maxAsgDeadline;
      
      if (!ApprovalStages.Is(stageBase))
        return maxDeadline;
      
      var stage = ApprovalStages.As(stageBase);
      
      // Если задания идут по последовательному этапу, то могут быть созданы ещё задания, которые стоит учесть.
      if (stage.Sequence == Sungero.Docflow.ApprovalStage.Sequence.Serially &&
          stage.Assignee == null &&
          stage.ApprovalRole == null)
      {
        var assignment = assignmentsInProcess.First();
        var allEmployees = Functions.ApprovalStage.GetStagePerformers(_obj, stage);
        var employeeWithAssignments = assignments
          .Where(a => Equals(a.BlockUid, assignment.BlockUid) &&
                 a.IterationId == assignment.IterationId && a.TaskStartId == assignment.TaskStartId)
          .Select(a => a.Performer)
          .ToList();
        var nextAssignees = allEmployees.Except(employeeWithAssignments);
        try
        {
          foreach (var assignee in nextAssignees)
          {
            if (stage.DeadlineInDays.HasValue)
              maxDeadline = maxDeadline.AddWorkingDays(assignee, stage.DeadlineInDays.Value);
            if (stage.DeadlineInHours.HasValue)
              maxDeadline = maxDeadline.AddWorkingHours(assignee, stage.DeadlineInHours.Value);
          }

        }
        catch (AppliedCodeException)
        {
          return maxDeadline;
        }
      }
      return maxDeadline;
    }
    
    /// <summary>
    /// Определить этапы для текущей задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Отсортированный список этапов, подходящих по условиям.</returns>
    [Remote(PackResultEntityEagerly = true, IsPure = true)]
    public static Structures.Module.DefinedApprovalStages GetStages(IApprovalTask task)
    {
      if (task.ApprovalRule != null)
        return Functions.ApprovalRuleBase.GetStages(task.ApprovalRule, task.DocumentGroup.OfficialDocuments.FirstOrDefault(), task);
      else
        return Structures.Module.DefinedApprovalStages.Create(null, false, string.Empty);
    }
    
    /// <summary>
    /// Определить базовые этапы для текущей задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Отсортированный список этапов, подходящих по условиям.</returns>
    [Remote(PackResultEntityEagerly = true, IsPure = true)]
    public static Structures.Module.DefinedApprovalBaseStages GetBaseStages(IApprovalTask task)
    {
      if (task.ApprovalRule != null)
        return Functions.ApprovalRuleBase.GetBaseStages(task.ApprovalRule, task.DocumentGroup.OfficialDocuments.FirstOrDefault(), task);
      else
        return Structures.Module.DefinedApprovalBaseStages.Create(null, false, string.Empty);
    }
    
    /// <summary>
    /// Получить данные по этапам согласования для обновления формы.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Структура с данными по этапам согласования.</returns>
    [Remote(IsPure = true), Obsolete("Используйте метод GetFullStagesInfoForRefresh.")]
    public static Structures.ApprovalTask.RefreshParameters GetStagesInfoForRefresh(IApprovalTask task)
    {
      return GetFullStagesInfoForRefresh(task);
    }
    
    /// <summary>
    /// Получить данные по этапам согласования для обновления формы.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stages">Список этапов согласования.</param>
    /// <returns>Структура с данными по этапам согласования.</returns>
    [Remote(IsPure = true), Obsolete("Используйте метод GetFullStagesInfoForRefresh.")]
    public static Structures.ApprovalTask.RefreshParameters GetStagesInfoForRefresh(IApprovalTask task, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var fullStages = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stages);
      return GetFullStagesInfoForRefresh(task, fullStages);
    }
    
    /// <summary>
    /// Получить данные по базовым этапам согласования для обновления формы.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Структура с данными по этапам согласования.</returns>
    [Remote(IsPure = true)]
    public static Structures.ApprovalTask.RefreshParameters GetFullStagesInfoForRefresh(IApprovalTask task)
    {
      var stages = Functions.ApprovalTask.GetBaseStages(task).BaseStages;
      return GetFullStagesInfoForRefresh(task, stages);
    }
    
    /// <summary>
    /// Получить данные по базовым этапам согласования для обновления формы.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stages">Список базовых этапов согласования.</param>
    /// <returns>Структура с данными по базовым этапам согласования.</returns>
    [Remote(IsPure = true)]
    public static Structures.ApprovalTask.RefreshParameters GetFullStagesInfoForRefresh(IApprovalTask task, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      var info = Structures.ApprovalTask.RefreshParameters.Create();
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(task))
        /* Если сотрудник не видит документ (отбор прав / строгие права), то показывать ему дополнительные поля в карточке не нужно.
         * Вернуть только что созданную структуру, чтобы избежать лишних вычислений доступности/видимости/обязательности.
         * Все поля типа bool, предназначенные для признаков доступности/видимости/обязательности, будут инициализированы в false.
         */
        return info;
      
      info.HasDocumentAndCanRead = true;
      info.ForwardPerformerIsVisible = Functions.ApprovalTask.SchemeVersionSupportsRework(task);
      // Если в регламенте запрещен уменьшающийся круг рецензентов, то не даем изменять действие в гриде.
      info.ApproversActionIsEnabled = task.ApprovalRule != null && task.ApprovalRule.IsSmallApprovalAllowed == true;
      info.ApproversIsVisible = true;
      
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (task.ApprovalRule != null)
      {
        var hasConditionAddressee = task.ApprovalRule.Conditions.Any(c => c.Condition.ConditionType == Sungero.Docflow.Condition.ConditionType.Addressee);
        info.AddresseeIsEnabled = !hasConditionAddressee;
        info.AddresseesIsEnabled = !hasConditionAddressee;
      }
      
      var isExchange = task.DeliveryMethod != null && task.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      info.ExchangeServiceIsEnabled = isExchange;
      if (isExchange && OfficialDocuments.Is(document))
      {
        if (document.Versions.Any())
        {
          var isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
          var isFormalizedDocument = Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true;
          info.DeliveryMethodIsEnabled = !isIncomingDocument;
          info.ExchangeServiceIsEnabled = !(isIncomingDocument || isFormalizedDocument);
        }
      }
      
      var hasAddApproversStage = false;
      var hasReviewStage = false;
      var hasReviewTaskStage = false;
      var hasSignStage = false;
      var hasSendStage = false;
      var hasConditionWithSignatoryRole = false;
      var hasConditionWithAddresseeRole = false;
      var hasConditionWithSignAssistantRole = false;
      var hasConditionWithAddrAssistantRole = false;
      var hasConditionWithPrintRespRole = false;
      var hasConditionManyAddressees = false;
      
      if (task.ApprovalRule != null)
      {
        hasAddApproversStage = stages
          .Where(s => ApprovalStages.Is(s.StageBase))
          .Any(s => s.StageType == Docflow.ApprovalStage.StageType.Approvers && ApprovalStages.As(s.StageBase).AllowAdditionalApprovers == true);
        hasReviewStage = Functions.ApprovalRuleBase.HasApprovalStage(task.ApprovalRule, Docflow.ApprovalStage.StageType.Review, document, stages);
        hasReviewTaskStage = Functions.ApprovalRuleBase.HasApprovalReviewTaskStage(task.ApprovalRule, document, stages);
        hasSignStage = Functions.ApprovalRuleBase.HasApprovalStage(task.ApprovalRule, Docflow.ApprovalStage.StageType.Sign, document, stages);
        hasSendStage = Functions.ApprovalRuleBase.HasApprovalStage(task.ApprovalRule, Docflow.ApprovalStage.StageType.Sending, document, stages) ||
          Functions.ApprovalRuleBase.HasApprovalCondition(task.ApprovalRule, document, task, Docflow.ConditionBase.ConditionType.DeliveryMethod);
        
        if (document != null)
        {
          // Список достижимых условий в правиле согласования.
          var conditions = Functions.ApprovalRuleBase.GetConditions(task.ApprovalRule, document, task);
          
          hasConditionWithSignatoryRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(task.ApprovalRule, conditions, Docflow.ApprovalRoleBase.Type.Signatory);
          hasConditionWithAddresseeRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(task.ApprovalRule, conditions, Docflow.ApprovalRoleBase.Type.Addressee);
          hasConditionWithSignAssistantRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(task.ApprovalRule, conditions, Docflow.ApprovalRoleBase.Type.SignAssistant);
          hasConditionWithAddrAssistantRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(task.ApprovalRule, conditions, Docflow.ApprovalRoleBase.Type.AddrAssistant);
          hasConditionWithPrintRespRole = Functions.ApprovalRuleBase.HasApprovalConditionWithRole(task.ApprovalRule, conditions, Docflow.ApprovalRoleBase.Type.PrintResp);
          hasConditionManyAddressees = Functions.ApprovalRuleBase.HasApprovalCondition(task.ApprovalRule, document, task, Docflow.Condition.ConditionType.ManyAddressees);
        }
      }
      
      info.AddApproversIsVisible = hasAddApproversStage;
      info.AddresseesIsVisible = hasReviewTaskStage || (!hasReviewStage && hasConditionManyAddressees);
      info.AddresseeIsVisible = !info.AddresseesIsVisible && (hasReviewStage || hasConditionWithAddresseeRole || hasConditionWithAddrAssistantRole);
      info.SignatoryIsVisible = hasSignStage || hasConditionWithSignatoryRole || hasConditionWithSignAssistantRole || hasConditionWithPrintRespRole;
      info.DeliveryMethodIsVisible = hasSendStage;
      info.ExchangeServiceIsVisible = hasSendStage;
      
      info.AddresseeIsRequired = hasReviewStage && info.AddresseeIsVisible;
      info.AddresseesIsRequired = hasReviewTaskStage;
      info.SignatoryIsRequired = hasSignStage;
      info.ExchangeServiceIsRequired = task.DeliveryMethod != null && task.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      
      return info;
    }
    
    /// <summary>
    /// Получить список доступных исполнителей доработки.
    /// </summary>
    /// <returns>Список доступных исполнителей доработки.</returns>
    [Public, Remote(IsPure = true)]
    public virtual List<IEmployee> GetReworkPerformers()
    {
      var recipients = this.GetTaskAssignees(false);
      var stage = _obj.ApprovalRule.Stages.Where(s => s.Number == _obj.StageNumber).FirstOrDefault();
      if (stage != null && stage.Stage.ReworkPerformerType == ReworkPerformerType.EmployeeRole)
        recipients.Add(stage.Stage.ReworkPerformer);
      
      if (_obj.ApprovalRule.ReworkPerformerType == Docflow.ApprovalRuleBase.ReworkPerformerType.EmployeeRole && _obj.ApprovalRule.ReworkPerformer != null)
        recipients.Add(_obj.ApprovalRule.ReworkPerformer);
      
      var performers = Company.PublicFunctions.Module.GetEmployeesFromRecipients(recipients);
      performers.Add(Employees.As(_obj.Author));

      var reworkPerformers = ApprovalReworkAssignments
        .GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created > _obj.Started)
        .Select(a => Employees.As(a.Performer))
        .ToList();
      
      performers.AddRange(reworkPerformers);
      
      if (_obj.ApprovalRule.ReworkPerformerType == Docflow.ApprovalRuleBase.ReworkPerformerType.ApprovalRole && _obj.ApprovalRule.ReworkApprovalRole != null)
      {
        var rolePerformer = Functions.ApprovalRoleBase.GetRolePerformer(_obj.ApprovalRule.ReworkApprovalRole, _obj);
        if (rolePerformer != null)
          performers.Add(rolePerformer);
      }
      if (stage != null && stage.Stage.ReworkPerformerType == ReworkPerformerType.ApprovalRole)
      {
        var stagePerformer = Functions.ApprovalRoleBase.GetRolePerformer(stage.Stage.ReworkApprovalRole, _obj);
        if (stagePerformer != null)
          performers.Add(stagePerformer);
      }
      
      return performers
        .Distinct()
        .OrderBy(p => p.Name)
        .ToList();
    }
    
    /// <summary>
    /// Вычислить исполнителя задания на доработку.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <returns>Исполнитель.</returns>
    public virtual IEmployee GetReworkPerformer(IApprovalStage stage)
    {
      if (_obj.ReworkPerformer != null)
        return _obj.ReworkPerformer;
      
      if (stage != null && stage.ReworkPerformerType != Docflow.ApprovalStage.ReworkPerformerType.FromRule)
      {
        if (stage.ReworkPerformerType == Docflow.ApprovalStage.ReworkPerformerType.EmployeeRole)
        {
          if (Employees.Is(stage.ReworkPerformer))
            return Employees.As(stage.ReworkPerformer);
          else if (Roles.Is(stage.ReworkPerformer))
          {
            var performer = Roles.As(stage.ReworkPerformer).RecipientLinks.FirstOrDefault();
            if (performer != null && Employees.Is(performer.Member))
              return Employees.As(performer.Member);
          }
        }
        else if (stage.ReworkPerformerType == Docflow.ApprovalStage.ReworkPerformerType.ApprovalRole)
        {
          var rolePerformer = Functions.ApprovalRoleBase.GetRolePerformer(stage.ReworkApprovalRole, _obj);
          if (rolePerformer != null)
            return rolePerformer;
        }
        else if (stage.ReworkPerformerType == Docflow.ApprovalStage.ReworkPerformerType.Author)
          return Employees.As(_obj.Author);
      }
      
      if (_obj.ApprovalRule.ReworkPerformerType == Docflow.ApprovalRuleBase.ReworkPerformerType.ApprovalRole && _obj.ApprovalRule.ReworkApprovalRole != null)
      {
        var rolePerformer = Functions.ApprovalRoleBase.GetRolePerformer(_obj.ApprovalRule.ReworkApprovalRole, _obj);
        if (rolePerformer != null)
          return rolePerformer;
      }
      
      if (_obj.ApprovalRule.ReworkPerformerType == Docflow.ApprovalRuleBase.ReworkPerformerType.EmployeeRole && _obj.ApprovalRule.ReworkPerformer != null)
      {
        if (Employees.Is(_obj.ApprovalRule.ReworkPerformer))
          return Employees.As(_obj.ApprovalRule.ReworkPerformer);
        else if (Roles.Is(_obj.ApprovalRule.ReworkPerformer))
        {
          var performer = Roles.As(_obj.ApprovalRule.ReworkPerformer).RecipientLinks.FirstOrDefault();
          if (performer != null && Employees.Is(performer.Member))
            return Employees.As(performer.Member);
        }
      }
      
      return Employees.As(_obj.Author);
    }
    
    /// <summary>
    /// Получить параметры для отправки на доработку.
    /// </summary>
    /// <param name="stageNumber">Номер этапа.</param>
    /// <returns>Параметры доработки.</returns>
    [Remote, Obsolete("Используйте метод GetAssignmentReworkParameters.")]
    public virtual Structures.ApprovalTask.ReworkParameters GetReworkParameters(int stageNumber)
    {
      return Functions.ApprovalTask.GetAssignmentReworkParameters(_obj, stageNumber);
    }
    
    /// <summary>
    /// Получить исполнителя последнего задания.
    /// </summary>
    /// <returns>Исполнитель последнего задания.</returns>
    public virtual IUser GetLastAssignmentPerformer()
    {
      IUser performer = null;
      // Получить предыдущее задание.
      var lastAssignment = Functions.ApprovalTask.GetLastTaskAssigment(_obj, null);
      
      // Если это подписание, то инициатор - подписывающий.
      if (ApprovalSigningAssignments.Is(lastAssignment))
      {
        var signAssignment = ApprovalSigningAssignments.As(lastAssignment);
        performer = signAssignment.Performer;
      }
      // Если это рассмотрение, то инициатор - адресат.
      if (ApprovalReviewAssignments.Is(lastAssignment))
      {
        var reviewAssignment = ApprovalReviewAssignments.As(lastAssignment);
        performer = reviewAssignment.Performer;
      }
      if (ApprovalCheckReturnAssignments.Is(lastAssignment))
      {
        var checkAssignment = ApprovalCheckReturnAssignments.GetAll()
          .Where(a => Equals(a.Task, _obj) && a.Result == Docflow.ApprovalCheckReturnAssignment.Result.NotSigned)
          .OrderByDescending(o => o.Completed).FirstOrDefault();
        performer = checkAssignment.Performer;
      }
      if (ApprovalAssignments.Is(lastAssignment))
      {
        var apprAssignment = ApprovalAssignments.GetAll()
          .Where(a => Equals(a.Task, _obj) && a.Result == Docflow.ApprovalAssignment.Result.ForRevision)
          .OrderByDescending(o => o.Completed).FirstOrDefault();
        performer = apprAssignment.Performer;
      }
      if (ApprovalManagerAssignments.Is(lastAssignment))
      {
        var apprAssignment = ApprovalManagerAssignments.GetAll()
          .Where(a => Equals(a.Task, _obj) && a.Result == Docflow.ApprovalManagerAssignment.Result.ForRevision)
          .OrderByDescending(o => o.Completed).FirstOrDefault();
        performer = apprAssignment.Performer;
      }
      
      return performer ?? _obj.Author;
    }
    
    /// <summary>
    /// Удалить элемент очереди для этапа функции.
    /// </summary>
    public virtual void DeleteFunctionQueueItem()
    {
      var queueItem = ApprovalFunctionQueueItems.GetAll(q => q.TaskId == _obj.Id && q.TaskStartId == _obj.StartId).FirstOrDefault();
      if (queueItem != null)
      {
        ApprovalFunctionQueueItems.Delete(queueItem);
        Logger.DebugFormat("Delete queue item. TaskId {0}, StartId {1}", _obj.Id, _obj.StartId);
      }
    }
    
    /// <summary>
    /// Получить этап выполнения сценария.
    /// </summary>
    /// <returns>Этап выполнения сценария.</returns>
    public virtual Sungero.Docflow.IApprovalFunctionStageBase GetFunctionStage()
    {
      var stage = _obj.ApprovalRule.Stages
        .Where(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function)
        .FirstOrDefault(s => s.Number == _obj.StageNumber);

      if (stage != null)
        return ApprovalFunctionStageBases.As(stage.StageBase);

      return null;
    }
    
    /// <summary>
    /// Вызов логики прекращения для пройденных сценариев.
    /// </summary>
    public virtual void AbortPassedFunctionStages()
    {
      Logger.DebugFormat("Start abort approval function stages in task id {0}", _obj.Id);
      var passedFunctionStages = this.GetPassedFunctionStages();

      foreach (var functionStage in passedFunctionStages)
      {
        try
        {
          Logger.DebugFormat("Start aborting approval function stage '{0}'", functionStage.StageBase.Name);
          var approvalFunctionStageBase = ApprovalFunctionStageBases.As(functionStage.StageBase);
          Functions.ApprovalFunctionStageBase.Abort(approvalFunctionStageBase, _obj, functionStage.Number.Value);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Aborting approval function stage '{0}' completed with error.", ex, functionStage.StageBase.Name);
        }
      }
      Logger.DebugFormat("Done abort approval function stages in task id {0}", _obj.Id);
    }
    
    /// <summary>
    /// Вызов логики доработки для пройденных сценариев.
    /// </summary>
    public virtual void ReworkPassedFunctionStages()
    {
      Logger.DebugFormat("Start rework in approval function stages, task id {0}", _obj.Id);
      var passedFunctionStages = this.GetPassedFunctionStages();

      foreach (var functionStage in passedFunctionStages)
      {
        try
        {
          Logger.DebugFormat("Start rework in approval function stage '{0}'", functionStage.StageBase.Name);
          var approvalFunctionStageBase = ApprovalFunctionStageBases.As(functionStage.StageBase);
          Functions.ApprovalFunctionStageBase.Rework(approvalFunctionStageBase, _obj, functionStage.Number.Value);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Rework in approval function stage '{0}' completed with error.", ex, functionStage.StageBase.Name);
        }
      }
      Logger.DebugFormat("Done rework in approval function stages, task id {0}", _obj.Id);
    }
    
    /// <summary>
    /// Получить список пройденных этапов с выполнением сценария.
    /// </summary>
    /// <returns>Список пройденных этапов.</returns>
    public virtual List<Sungero.Docflow.Structures.Module.DefinedApprovalBaseStageLite> GetPassedFunctionStages()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var stages = Functions.ApprovalRuleBase.GetBaseStages(_obj.ApprovalRule, document, _obj).BaseStages;
      var currentStage = stages.FirstOrDefault(s => s.Number == (_obj.StageNumber ?? 0));
      
      var passedFunctionStages = stages.TakeWhile(s => s != currentStage)
        .Where(s => s.StageType == Sungero.Docflow.ApprovalRuleStages.StageType.Function).ToList();
      
      if (currentStage != null && currentStage.StageType == Sungero.Docflow.ApprovalRuleStages.StageType.Function)
        passedFunctionStages.Add(currentStage);
      
      return passedFunctionStages;
    }
    
    /// <summary>
    /// Получить дату последенего изменения задачи.
    /// </summary>
    /// <returns>Дата последнего изменения задачи.</returns>
    [Remote(IsPure = true)]
    public virtual DateTime? GetApprovalTaskModified()
    {
      return ApprovalTasks.GetAll().Where(t => t.Id == _obj.Id).Select(t => t.Modified).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить значение параметра "Разрешить согласование с замечаниями" из настроек этапа.
    /// </summary>
    /// <param name="stageNumber">Номер этапа.</param>
    /// <returns>True, если согласование с замечаниями разрешено, иначе False.</returns>
    [Remote(IsPure = true)]
    public virtual bool GetApprovalWithSuggestionsParameter(int stageNumber)
    {
      var item = _obj.ApprovalRule.Stages.Where(s => s.Number == stageNumber).FirstOrDefault();
      if (item == null)
        return false;
      
      return item.Stage.AllowApproveWithSuggestions ?? false;
    }
    
    /// <summary>
    /// Отправить уведомления о прекращении задачи на согласование по регламенту.
    /// </summary>
    public virtual void SendApprovalAbortNotice()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      var subject = string.Empty;
      var threadSubject = string.Empty;
      // Отправить уведомления о прекращении.
      using (Sungero.Core.CultureInfoExtensions.SwitchTo(TenantInfo.Culture))
      {
        threadSubject = ApprovalTasks.Resources.AbortNoticeSubject;
        if (document != null)
          subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, threadSubject, Docflow.PublicFunctions.Module.TrimSpecialSymbols(document.Name));
        else
        {
          var approvalTaskSubject = string.Format("{0}{1}", _obj.Subject.Substring(0, 1).ToLower(), _obj.Subject.Remove(0, 1));
          subject = string.Format("{0} {1}", ApprovalTasks.Resources.AbortApprovalTask, Docflow.PublicFunctions.Module.TrimSpecialSymbols(approvalTaskSubject));
        }
      }
      
      var allApprovers = ApprovalAssignments.GetAll(asg => asg.Task == _obj && asg.IsRead.Value).Select(app => app.Performer).ToList();
      allApprovers.AddRange(ApprovalManagerAssignments.GetAll(asg => asg.Task == _obj && asg.IsRead.Value).Select(app => app.Performer).ToList());
      allApprovers.AddRange(ApprovalSigningAssignments.GetAll(asg => asg.Task == _obj && asg.IsRead.Value).Select(app => app.Performer).ToList());
      var author = _obj.Author;
      var reworkAssignment = Functions.ApprovalTask.GetLastReworkAssignment(_obj);
      if (reworkAssignment != null)
      {
        allApprovers.Add(reworkAssignment.Performer);
        if (!Equals(_obj.Author, reworkAssignment.Performer))
        {
          allApprovers.Add(_obj.Author);
          author = reworkAssignment.Performer;
        }
      }
      allApprovers.Remove(Users.Current);
      if (allApprovers.Any())
        Functions.Module.SendNoticesAsSubtask(subject, allApprovers, _obj, _obj.AbortingReason, author, threadSubject);
    }
    
    /// <summary>
    /// Создать и запустить асинхронный обработчик выполнения сценария в согласовании по регламенту.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    public virtual void ExecuteApprovalFunctionAsyncHandler(IApprovalFunctionQueueItem queueItem)
    {
      // При сохранении очереди поставится блокировка. Даже если асинхронный обработчик запустится раньше чем транзакция в блоке завершится,
      // он уйдет в переповтор, ProcessingStatus не перезапишется.
      if (_obj.GetStartedSchemeVersion() == LayerSchemeVersions.V4)
      {
        queueItem.ProcessingStatus = Docflow.ApprovalFunctionQueueItem.ProcessingStatus.Started;
        queueItem.Save();
      }
      var approvalFunctionHandler = Docflow.AsyncHandlers.ExecuteApprovalFunction.Create();
      approvalFunctionHandler.QueueItemId = queueItem.Id;
      approvalFunctionHandler.ExecuteAsync();
      Logger.DebugFormat("Create async handler. Id {0}, TaskId {1}, StartId {2}", queueItem.Id, _obj.Id, _obj.StartId);
    }
  }
}
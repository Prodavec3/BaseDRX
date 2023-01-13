using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;
using Sungero.Docflow.ApprovalStage;
using Sungero.Domain.Shared;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class ApprovalRuleBaseFunctions
  {
    #region Описание регламента
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <param name="assignment">Задание, по которому нужен регламент.</param>
    /// <returns>Регламент.</returns>
    [Public]
    public static Sungero.Core.StateView GetStagesStateView(Workflow.IAssignment assignment)
    {
      var task = ApprovalTasks.As(assignment.Task);
      var approvers = task.AddApproversExpanded.Select(a => a.Approver).ToList();
      return GetStagesStateView(task, approvers, task.Signatory, task.Addressee, task.DeliveryMethod, task.ExchangeService);
    }
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <param name="assignment">Задание, по которому нужен регламент.</param>
    /// <returns>Регламент.</returns>
    [Public]
    public static Sungero.Core.StateView GetStagesStateView(Workflow.INotice assignment)
    {
      var task = ApprovalTasks.As(assignment.Task);
      var approvers = task.AddApproversExpanded.Select(a => a.Approver).ToList();
      return GetStagesStateView(task, approvers, task.Signatory, task.Addressee, task.DeliveryMethod, task.ExchangeService);
    }
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="additionalApprovers">Доп. согласующие.</param>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="addressee">Адресат.</param>
    /// <param name="deliveryMethod">Способ доставки.</param>
    /// <param name="exchangeService">Сервис обмена.</param>
    /// <returns>Регламент.</returns>
    [Public]
    public static Sungero.Core.StateView GetStagesStateView(IApprovalTask task,
                                                            List<Sungero.CoreEntities.IRecipient> additionalApprovers,
                                                            Sungero.Company.IEmployee signatory,
                                                            Sungero.Company.IEmployee addressee,
                                                            IMailDeliveryMethod deliveryMethod,
                                                            ExchangeCore.IExchangeService exchangeService)
    {
      return GetStagesStateView(task, additionalApprovers, signatory, addressee, deliveryMethod, exchangeService, false);
    }
    
    /// <summary>
    /// Заполнить блок "Выполнение сценария".
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <param name="baseStageLite">Определяемый этап.</param>
    /// <param name="statusInfo">Информация о состоянии этапа.</param>
    /// <param name="stageNumber">Номер этапа.</param>
    public virtual void SetFunctionBlockProperties(Sungero.Core.StateBlock block, IApprovalTask task, Docflow.Structures.Module.DefinedApprovalBaseStageLite baseStageLite,
                                                   Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo, int stageNumber)
    {
      var taskIsAborted = task.Status == Workflow.Task.Status.Aborted || task.Status == Workflow.Task.Status.Suspended;
      var functionStage = task.ApprovalRule.Stages
        .Where(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function)
        .FirstOrDefault(s => s.Number == task.StageNumber);
      
      var functionStageBase = Docflow.ApprovalFunctionStageBases.As(baseStageLite.StageBase);
      if (functionStageBase == null)
        return;
      
      block.Entity = baseStageLite.StageBase;
      
      SetIcon(baseStageLite.StageType, block);
      
      var functionHeader = string.Format("{0}. {1}", stageNumber, Docflow.Functions.ApprovalFunctionStageBase.GetStateViewBlockName(functionStageBase, task, statusInfo));

      block.AddLabel(functionHeader, Functions.Module.CreateHeaderStyle());
      block.AddLineBreak();
      
      Docflow.Functions.ApprovalFunctionStageBase.AddStateViewBlockPerformers(functionStageBase, task, block, statusInfo);
      
      var status = string.Empty;
      if (statusInfo.IsLast)
        status = ApprovalRuleBases.Resources.StateViewStatusCompleted;
      else if (statusInfo.InProcess)
        status = taskIsAborted ? ApprovalRuleBases.Resources.StateViewStatusAborted : ApprovalRuleBases.Resources.StateViewStatusInProcess;
      
      if (!string.IsNullOrEmpty(status))
        Functions.Module.AddInfoToRightContent(block, status);
      
      var noteStyle = Functions.Module.CreateNoteStyle();
      var deadline = Docflow.Functions.ApprovalFunctionStageBase.GetStateViewBlockDeadline(functionStageBase, task, statusInfo);
      if (!statusInfo.IsLast && !string.IsNullOrEmpty(deadline) && !taskIsAborted)
        Functions.Module.AddInfoToRightContent(block, deadline, noteStyle);
      
      if (functionStage != null && baseStageLite.Number == functionStage.Number && task.Status == Workflow.Task.Status.InProcess)
        Functions.Module.MarkBlock(block);
    }

    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="additionalApprovers">Доп. согласующие.</param>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="addressee">Адресат.</param>
    /// <param name="deliveryMethod">Способ доставки.</param>
    /// <param name="exchangeService">Сервис обмена.</param>
    /// <param name="withApprovalRule">Надо ли отображать регламент и ожидаемый срок.</param>
    /// <returns>Регламент.</returns>
    [Public]
    public static Sungero.Core.StateView GetStagesStateView(IApprovalTask task,
                                                            List<Sungero.CoreEntities.IRecipient> additionalApprovers,
                                                            Sungero.Company.IEmployee signatory,
                                                            Sungero.Company.IEmployee addressee,
                                                            IMailDeliveryMethod deliveryMethod,
                                                            ExchangeCore.IExchangeService exchangeService,
                                                            bool withApprovalRule)
    {
      var stateView = StateView.Create();
      stateView.IsPrintable = true;
      
      if (task.ApprovalRule == null || !Employees.Is(task.Author))
        return stateView;
      
      // Подписывающего взять из задачи, если не передано другого.
      if (signatory == null)
        signatory = task.Signatory;
      
      // Адресата взять из задачи, если не передано другого.
      if (addressee == null)
        addressee = task.Addressee;
      
      var assignment = Workflow.Assignments.GetAll()
        .Where(a => Equals(a.Task, task) && a.Status == Workflow.AssignmentBase.Status.InProcess).FirstOrDefault();
      
      // Для каждого этапа создать свой блок.
      var baseStages = Functions.ApprovalTask.GetBaseStages(task);
      var stages = Functions.ApprovalRuleBase.CastToDefinedApprovalStages(baseStages);
      var stageNumber = 1;
      
      if (baseStages.IsConditionsDefined && withApprovalRule)
      {
        var block = stateView.AddBlock();
        block.ShowBorder = false;
        block.AddHyperlink(task.ApprovalRule.Name, Hyperlinks.Get(task.ApprovalRule));
        if (task.Status == Docflow.ApprovalTask.Status.InProcess || task.Status == Docflow.ApprovalTask.Status.Draft)
        {
          var expectedDate = task.Status != Docflow.ApprovalTask.Status.Draft ? task.MaxDeadline : Docflow.Functions.ApprovalTask.GetExpectedDate(task, null, baseStages.BaseStages);
          if (expectedDate != null)
          {
            var rightContent = block.AddContent();
            rightContent.AddLabel(string.Format("{0} - {1}", Sungero.Docflow.ApprovalTasks.Resources.StateViewMaxDeadline, expectedDate.Value.ToUserTime().ToString("g")));
          }
        }
      }
      
      // Статус задачи.
      var taskInProcess = task.Status == Workflow.Task.Status.InProcess;
      var taskIsAborted = task.Status == Workflow.Task.Status.Aborted || task.Status == Workflow.Task.Status.Suspended;
      
      foreach (var ruleBaseStage in baseStages.BaseStages)
      {
        var status = string.Empty;
        
        if (ruleBaseStage.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function)
        {
          var functionStatusInfo = GetStatusInfo(task, ruleBaseStage, baseStages.BaseStages);
          var functionBlock = stateView.AddBlock();
          Docflow.Functions.ApprovalRuleBase.SetFunctionBlockProperties(task.ApprovalRule, functionBlock, task, ruleBaseStage, functionStatusInfo, stageNumber);
          
          stageNumber++;
          
          continue;
        }
        
        var ruleStage = stages.Stages.Where(s => s.Number == ruleBaseStage.Number).FirstOrDefault();
        if (ruleStage == null)
        {
          continue;
        }
        
        var stage = ruleStage.Stage;
        
        // Определить исполнителей.
        var performersAndEmptyPerformersLabel = GetBlockPerformers(task, stage, signatory, addressee, additionalApprovers);
        var emptyPerformersLabel = performersAndEmptyPerformersLabel.Message;
        
        // Коллекция сотрудников для проверок.
        var performersEmployees = performersAndEmptyPerformersLabel.Employees;
        
        // Коллекция реципиентов для вывода в блоке, содержит развернутые группы, пустые группы и группы более 5 человек.
        var performersRecipients = performersAndEmptyPerformersLabel.Recipient;
        
        // Скрыть блок согласования с доп. согласующими, если его уже нельзя заполнить, а в нём никого нет.
        if (stage.StageType == StageType.Approvers
            && stage.AllowAdditionalApprovers == true
            && !ApprovalReworkAssignments.Is(assignment)
            && !ApprovalManagerAssignments.Is(assignment)
            && !additionalApprovers.Any()
            && task.Status != Workflow.Task.Status.Draft
            && !performersAndEmptyPerformersLabel.Recipient.Any())
          continue;
        
        // Скрыть блок с отправкой КА, если его не было и не будет.
        if (stage.StageType == StageType.Sending)
        {
          var hasControlReturnAfterSending = HasControlReturnAfterSending(stages.Stages, ruleStage.Number ?? 0);
          var showSendStage = NeedShowSendingStage(task, ruleStage, stages.Stages);
          
          if (!showSendStage && hasControlReturnAfterSending)
            continue;
        }
        
        // Получить информацию по статусу этапа.
        var statusInfo = GetStatusInfo(task, ruleBaseStage, baseStages.BaseStages);
        var isLast = statusInfo.IsLast;
        var inProcess = statusInfo.InProcess;
        var isNext = statusInfo.IsNext;
        
        // Скрыть блок с контролем возврата, если его не было и не будет.
        if (stage.StageType == StageType.CheckReturn)
        {
          var showControlReturnStage = NeedShowControlReturnStage(task, ruleStage, statusInfo);
          
          if (!showControlReturnStage)
            continue;
        }
        
        // Скрыть блок создания поручений, если нет исполнителя.
        if (stage.StageType == StageType.Execution &&
            task.Status != Workflow.Task.Status.Draft &&
            !performersEmployees.Any(p => p != null))
          continue;
        
        #region Статусы задачи и этапов
        
        var isParallel = stage.Sequence != Sequence.Serially;
        
        if (isLast)
        {
          var needSkipStage = false;
          
          // Пропуск этапа подписания.
          if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Sign)
            needSkipStage = Functions.ApprovalTask.NeedSkipSignStage(task, ruleStage, signatory, addressee);
          
          // Пропуск этапа создания поручений.
          if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Execution)
            needSkipStage = !Functions.ApprovalExecutionAssignment.NeedExecutionAssignment(task);
          
          // Пропуск этапа согласования с руководителем.
          if (stage.StageType == StageType.Manager)
            needSkipStage = performersEmployees.Contains(Employees.As(task.Author));
          
          // Пропуск этапа согласования с инициатором.
          if (stage.StageType == StageType.Approvers)
            needSkipStage = !performersEmployees.Any(p => !Equals(p, Employees.As(task.Author)));
          
          if (!needSkipStage && performersEmployees.Any(p => p != null))
            status = ApprovalRuleBases.Resources.StateViewStatusCompleted;
          else
            continue;
        }
        else if (inProcess)
          status = taskIsAborted ? ApprovalRuleBases.Resources.StateViewStatusAborted : ApprovalRuleBases.Resources.StateViewStatusInProcess;
        
        #endregion
        
        var stageBlock = stateView.AddBlock();
        stageBlock.Entity = stage;
        
        // Установить иконку.
        SetIcon(stage.StageType, stageBlock);
        
        // Заголовок.
        var header = Functions.ApprovalRuleBase.GetStageBlockHeader(task.ApprovalRule, stage);
        
        // Примечание.
        var note = string.Empty;
        var startNote = string.Empty;
        
        // Сменить описание, если это подтверждение подписания.
        if (stage.StageType == StageType.Sign)
        {
          var confirmBy = Functions.ApprovalStage.GetConfirmByForSignatory(stage, signatory, task);
          if (confirmBy != null)
          {
            header = ApprovalRuleBases.Resources.StateViewSignHeader;
            performersRecipients.Clear();
            performersRecipients.Add(confirmBy);
            var signatoryName = signatory != null
              ? Company.PublicFunctions.Employee.GetShortName(signatory, false)
              : ApprovalRuleBases.Resources.StateViewSignNone;
            note = ApprovalRuleBases.Resources.StateViewSignSignatoryFormat(signatoryName);
          }
        }
        
        // Сменить описание, если это внесение результата рассмотрения адресатом.
        if (stage.StageType == StageType.Review &&
            addressee != null)
        {
          var assistant = Functions.ApprovalStage.GetAddresseeAssistantForResultSubmission(stage, addressee, task);
          if (assistant != null)
          {
            header = ApprovalRuleBases.Resources.StateViewFillingReviewingResultHeader;
            performersRecipients.Clear();
            performersRecipients.Add(assistant);
            var addresseeName = Company.PublicFunctions.Employee.GetShortName(addressee, false);
            note = ApprovalRuleBases.Resources.StateViewAddresseeFormat(addresseeName);
          }
        }
        
        // Добавить способ доставки, если он указан.
        if (stage.StageType == StageType.Sending)
        {
          if (OutgoingDocumentBases.Is(task.DocumentGroup.OfficialDocuments.FirstOrDefault()) &&
              OutgoingDocumentBases.As(task.DocumentGroup.OfficialDocuments.FirstOrDefault()).IsManyAddressees == true)
          {
            var method = ApprovalRuleBases.Resources.StateViewSendToManyAddressees;
            note = ApprovalRuleBases.Resources.StateViewSendNoteFormat(method, string.Empty);
          }
          else if (deliveryMethod != null)
          {
            var service = (deliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange && exchangeService != null) ? exchangeService.Name : string.Empty;
            var method = deliveryMethod.Name;
            note = ApprovalRuleBases.Resources.StateViewSendNoteFormat(method, service);
          }
        }
        
        #region Срок
        
        var deadline = string.Empty;
        
        // Для черновиков исполнитель минимум 1, чтобы не было срока в 0 дней.
        var notCompletedPerformers = isParallel ? 1 : (performersEmployees.Count == 0 ? 1 : performersEmployees.Count);

        // Вычислить срок. Для заданий "друг за другом" вычисляем в том числе оставшийся в процессе выполнения срок.
        if (assignment != null && !isParallel && task.StageNumber == ruleStage.Number)
        {
          var allAssignments = Workflow.Assignments
            .GetAll(a => Equals(a.Task, task) &&
                    Equals(a.BlockUid, assignment.BlockUid) &&
                    Equals(a.IterationId, assignment.IterationId) &&
                    Equals(a.TaskStartId, assignment.TaskStartId)).ToList();
          var countCompletedPerformers = allAssignments
            .Where(a => a.Status != Workflow.Assignment.Status.InProcess && assignment.Performer != a.Performer)
            .Select(x => x.Performer)
            .Distinct()
            .Count();
          notCompletedPerformers = notCompletedPerformers - countCompletedPerformers;
        }
        deadline = Functions.ApprovalStage.GetDeadlineDescription(stage, notCompletedPerformers);
        
        #endregion
        
        #region Дополнительная информация
        
        // Добавить информацию по порядку старта заданий.
        if ((stage.StageType == StageType.Approvers) &&
            performersEmployees.Count > 1)
        {
          startNote += !string.IsNullOrEmpty(startNote) ? Environment.NewLine : string.Empty;
          startNote += ApprovalRuleBases.Resources
            .StateViewApproveStartTypeFormat(ApprovalStages.Info.Properties.Sequence.GetLocalizedValue(stage.Sequence).ToLower());
          
          // Добавить пояснение по порядку доработки.
          if (stage.ReworkType == ReworkType.AfterEach)
            note += ApprovalRuleBases.Resources.StateViewApproveReworkAfterEach;
        }
        
        #endregion
        
        // Стили.
        var headerStyle = Functions.Module.CreateHeaderStyle();
        var performerStyle = Functions.Module.CreatePerformerDeadlineStyle();
        var noteStyle = Functions.Module.CreateNoteStyle();
        var emptyLineMargin = Constants.Module.EmptyLineMargin;
        
        #region Заполнение блока
        
        // Заголовок.
        header = string.Format("{0}. {1}", stageNumber, header);
        stageNumber++;
        stageBlock.AddLabel(header, headerStyle);
        
        // Порядок старта.
        if (!string.IsNullOrEmpty(startNote))
        {
          stageBlock.AddLineBreak();
          stageBlock.AddLabel(startNote, noteStyle);
        }
        
        // Отступ от заголовка.
        stageBlock.AddEmptyLine(emptyLineMargin);
        
        // Кому. "Должность - Фамилия И.О." или наименование группы/роли.
        AddPerformersToBlock(stageBlock, performersRecipients, emptyPerformersLabel);
        
        // Примечание.
        if (!string.IsNullOrEmpty(note))
        {
          stageBlock.AddLineBreak();
          stageBlock.AddEmptyLine(emptyLineMargin);
          stageBlock.AddLabel(note, noteStyle);
        }
        
        // Статус. Завершенный этап.
        if (isLast)
        {
          Functions.Module.AddInfoToRightContent(stageBlock, status);
        }
        
        // Статус и срок. Этап в процессе.
        if (inProcess)
        {
          // Выделить этап.
          if (task.Status == Workflow.Task.Status.InProcess)
            Functions.Module.MarkBlock(stageBlock);
          
          Functions.Module.AddInfoToRightContent(stageBlock, status);
          
          // Получить срок текущего задания\заданий.
          if (stage.StageType != StageType.Notice)
          {
            var currentAssignments = Workflow.Assignments.GetAll()
              .Where(a => Equals(a.Task, task) && a.Status == Workflow.AssignmentBase.Status.InProcess);
            
            DateTime? assignmentDeadline = null;
            if (currentAssignments.Any(a => a.Deadline.HasValue))
            {
              assignmentDeadline = currentAssignments.Where(a => a.Deadline.HasValue).Max(a => a.Deadline);
            }
            else if (stage.StageType == Docflow.ApprovalStage.StageType.CheckReturn &&
                     task.ControlReturnStartDate.HasValue && stage.StartDelayDays.HasValue)
            {
              var assignees = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(task, stage);
              foreach (var assignee in assignees)
              {
                var controlReturnAssignmentDeadline = task.ControlReturnStartDate.Value.AddWorkingDays(assignee, -stage.StartDelayDays.Value);
                if (stage.DeadlineInDays.HasValue)
                  controlReturnAssignmentDeadline = controlReturnAssignmentDeadline.AddWorkingDays(assignee, stage.DeadlineInDays.Value);
                if (stage.DeadlineInHours.HasValue)
                  controlReturnAssignmentDeadline = controlReturnAssignmentDeadline.AddWorkingHours(assignee, stage.DeadlineInHours.Value);
                if (assignmentDeadline == null || assignmentDeadline.HasValue && assignmentDeadline < controlReturnAssignmentDeadline)
                  assignmentDeadline = controlReturnAssignmentDeadline;
              }
            }
            
            if (assignmentDeadline.HasValue)
            {
              if (notCompletedPerformers == 1)
              {
                var deadlineLabel = string.Format("{0}: {1}",
                                                  OfficialDocuments.Resources.StateViewDeadline,
                                                  Functions.Module.ToShortDateShortTime(assignmentDeadline.Value.ToUserTime()));
                Functions.Module.AddInfoToRightContent(stageBlock, deadlineLabel, noteStyle);
              }
              else if (!string.IsNullOrEmpty(deadline))
                Functions.Module.AddInfoToRightContent(stageBlock, string.Format("{0} –{1}", OfficialDocuments.Resources.StateViewDeadline, deadline), noteStyle);
            }
            
          }
        }
        
        // Статус и срок. Этап не стартован. Для уведомлений и прекращенных задач срок не выводить.
        if (isNext && !string.IsNullOrEmpty(deadline) && stage.StageType != StageType.Notice && !taskIsAborted)
        {
          Functions.Module.AddInfoToRightContent(stageBlock, string.Format("{0} –{1}", OfficialDocuments.Resources.StateViewDeadline, deadline), noteStyle);
        }
        
        #endregion
      }
      
      #region Неопределенность в условии
      
      // Если в ходе определения регламента возникнет неопределенность в условии.
      if (!baseStages.IsConditionsDefined)
      {
        // То добавим блок с описанием ошибки.
        var errorBlock = stateView.AddBlock();
        var headerStyle = Functions.Module.CreateHeaderStyle();
        var noteStyle = Functions.Module.CreateNoteStyle();
        
        errorBlock.AssignIcon(ApprovalRuleBases.Info.Actions.DeleteEntity, StateBlockIconSize.Large);
        // Неопределенность в условии возникнет и при отсутствии документа в карточке задачи.
        if (task.DocumentGroup.OfficialDocuments.Any())
        {
          errorBlock.AddLabel(ApprovalRuleBases.Resources.StateViewHeaderUndefinedCondition, headerStyle);
          errorBlock.AddLineBreak();
          errorBlock.AddLabel(baseStages.ErrorMessage, noteStyle);
        }
        else
        {
          errorBlock.AddLabel(ApprovalRuleBases.Resources.StateViewHeaderDocumentNone, headerStyle);
        }
      }
      #endregion
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить исполнителей в блок этапа.
    /// </summary>
    /// <param name="block">Блок - куда добавить.</param>
    /// <param name="performers">Исполнители.</param>
    /// <param name="emptyPerformersLabel">Строка, выводимая, если исполнителей в этапе нет.</param>
    public static void AddPerformersToBlock(Sungero.Core.StateBlock block, List<Sungero.CoreEntities.IRecipient> performers, string emptyPerformersLabel)
    {
      var noteStyle = Functions.Module.CreateNoteStyle();
      var performerStyle = Functions.Module.CreatePerformerDeadlineStyle();
      var groupNameStyle = Functions.Module.CreateStyle(true, true);
      
      // Если исполнителей нет, вывести строку об этом.
      if (performers.Count() == 0)
        block.AddLabel(emptyPerformersLabel, noteStyle);
      
      // Вывести исполнителей.
      var firstPerformer = performers.FirstOrDefault();
      foreach (var performer in performers)
      {
        // Если исполнитель не первый - добавить перевод строки.
        if (!Equals(firstPerformer, performer))
          block.AddLineBreak();
        PrintPerformer(block, performer);
      }
    }
    
    /// <summary>
    /// Вывести сотрудника.
    /// </summary>
    /// <param name="block">Блок - куда вывести.</param>
    /// <param name="performer">Сотрудник.</param>
    private static void PrintPerformer(Sungero.Core.StateBlock block, IRecipient performer)
    {
      var performerStyle = Functions.Module.CreatePerformerDeadlineStyle();
      var noteStyle = Functions.Module.CreateNoteStyle();
      var groupNameStyle = Functions.Module.CreateStyle(true, true);
      
      // Сотрудник.
      var employee = Employees.As(performer);
      if (employee != null)
      {
        if (employee.JobTitle != null)
          block.AddLabel(employee.JobTitle.Name.Trim(), noteStyle);
        block.AddLabel(Company.PublicFunctions.Employee.GetShortName(employee, false), performerStyle);
        
        return;
      }
      
      // Группа, роль, подразделение, НОР, группа регистрации.
      var group = Groups.As(performer);
      if (group != null)
      {
        // Если в группе нет исполнителей, то вывести сообщение.
        var groupHaveMembers = Groups.GetAllUsersInGroup(group)
          .Where(r => Employees.Is(r) && r.Status == CoreEntities.DatabookEntry.Status.Active)
          .Select(r => Recipients.As(r)).Any();
        if (!groupHaveMembers)
        {
          block.AddLabel(string.Format("{0}:", group.Name), groupNameStyle);
          block.AddLabel(ApprovalRuleBases.Resources.StateViewApproversNone, noteStyle);
          return;
        }
        
        // Группа, в которой больше 5 человек и в типе этапа "Задание" или "Уведомление".
        block.AddLabel(group.Name, groupNameStyle);
        
        return;
      }
      
      block.AddLabel(performer.Name, groupNameStyle);
    }
    
    /// <summary>
    /// Установить иконку.
    /// </summary>
    /// <param name="stageType">Тип этапа.</param>
    /// <param name="block">Блок.</param>
    public static void SetIcon(Enumeration? stageType, Sungero.Core.StateBlock block)
    {
      // Установить иконку по умолчанию.
      var iconSize = StateBlockIconSize.Large;
      block.AssignIcon(ApprovalRuleBases.Resources.Assignment, iconSize);
      
      if (stageType == null)
        return;
      
      // Согласование.
      if (stageType == StageType.Manager || stageType == StageType.Approvers)
        block.AssignIcon(ApprovalTasks.Resources.ApproveStage, iconSize);
      
      // Подписание.
      if (stageType == StageType.Sign)
        block.AssignIcon(ApprovalTasks.Resources.Sign, iconSize);
      
      // Регистрация.
      if (stageType == StageType.Register)
        block.AssignIcon(OfficialDocuments.Info.Actions.Register, iconSize);
      
      // Рассмотрение.
      if (stageType == StageType.Review)
        block.AssignIcon(ApprovalRuleBases.Resources.Review, iconSize);
      
      // Создание поручений по документу.
      if (stageType == StageType.Execution)
        block.AssignIcon(ApprovalRuleBases.Resources.ActionItemTask, iconSize);
      
      // Печать.
      if (stageType == StageType.Print)
        block.AssignIcon(ApprovalTasks.Resources.Completed, iconSize);

      // Уведомление.
      if (stageType == StageType.Notice)
        block.AssignIcon(ApprovalRuleBases.Resources.Notice, iconSize);
      
      // Контроль возврата.
      if (stageType == StageType.CheckReturn)
        block.AssignIcon(ApprovalRuleBases.Resources.ControlReturn, iconSize);
      
      // Отправка контрагенту.
      if (stageType == StageType.Sending)
        block.AssignIcon(ApprovalRuleBases.Resources.Sending, iconSize);
      
      // Сценарий.
      if (stageType == Docflow.ApprovalRuleBaseStages.StageType.Function)
        block.AssignIcon(ApprovalRuleBases.Resources.Function, iconSize);
    }
    
    /// <summary>
    /// Получить заголовок блока этапа.
    /// </summary>
    /// <param name="stage">Этап.</param>
    /// <returns>Заголовок.</returns>
    public virtual string GetStageBlockHeader(IApprovalStage stage)
    {
      return stage.Name;
    }
    
    /// <summary>
    /// Получить информацию по состоянию этапа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="ruleStage">Определяемый этап.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Информация о состоянии этапа.</returns>
    public static Structures.ApprovalRuleBase.StageStatusInfo GetStatusInfo(IApprovalTask task, Structures.Module.DefinedApprovalBaseStageLite ruleStage, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      // Для завершённой задачи все этапы выполнены.
      if (task.Status == Workflow.Task.Status.Completed)
        return Structures.ApprovalRuleBase.StageStatusInfo.Create(true, false, false);
      
      var ruleStageIndex = stages.IndexOf(ruleStage);
      
      // Определить индекс задания, которое находится в работе.
      var inProcessStage = stages.Where(s => s.Number == task.StageNumber).FirstOrDefault();
      var inProcessStageIndex = stages.IndexOf(inProcessStage);
      var inProcessApprovalStage = Functions.ApprovalRuleBase.CastToDefinedApprovalStageLite(inProcessStage);
      
      // Если задача не стартована или ещё не началось создание заданий, то все этапы не стартованы.
      if (inProcessStageIndex < 0 || task.Status == Workflow.Task.Status.Draft)
        return Structures.ApprovalRuleBase.StageStatusInfo.Create(false, false, true);
      
      var approvalRuleStage = Functions.ApprovalRuleBase.CastToDefinedApprovalStageLite(ruleStage);
      if (approvalRuleStage != null && inProcessApprovalStage != null)
      {
        // Если определяемый этап схлопнут с этапом "в работе", то он также в работе.
        var collapsedStages = Functions.ApprovalTask.GetCollapsedStages(task, approvalRuleStage);
        if (collapsedStages != null  && collapsedStages.Contains(inProcessApprovalStage))
          return Structures.ApprovalRuleBase.StageStatusInfo.Create(false, true, false);
      }
      
      // Прошедший этап.
      var isLast = ruleStageIndex < inProcessStageIndex;
      
      // Этап в работе.
      var inProcess = ruleStageIndex == inProcessStageIndex;
      
      // Будущий этап.
      var isNext = ruleStageIndex > inProcessStageIndex;
      
      return Structures.ApprovalRuleBase.StageStatusInfo.Create(isLast, inProcess, isNext);
    }
    
    /// <summary>
    /// Получить исполнителей блока.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="addressee">Адресат.</param>
    /// <param name="additionalApprovers">Доп. согласующие.</param>
    /// <returns>Структура из коллекции исполнителей, соответствующих исполнителям реципиентов и текстом, на случай пустой коллекции.</returns>
    public static Structures.ApprovalRuleBase.BlockPerformers GetBlockPerformers(IApprovalTask task,
                                                                                 Sungero.Docflow.IApprovalStage stage,
                                                                                 Sungero.Company.IEmployee signatory,
                                                                                 Sungero.Company.IEmployee addressee,
                                                                                 List<Sungero.CoreEntities.IRecipient> additionalApprovers)
    {
      var stageType = stage.StageType;
      var emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewApproverNone;
      var performers = new List<IEmployee>();
      var recipients = new List<IRecipient>();
      
      // Согласование с руководителем.
      if (stage.StageType == StageType.Manager)
      {
        var manager = Functions.ApprovalStage.GetStagePerformer(task, stage);
        performers.Add(manager);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewManagerNone;
      }
      
      // Согласование с обязательными согласующими.
      if (stageType == StageType.Approvers)
      {
        recipients = Functions.ApprovalStage.GetStageRecipients(stage, task, additionalApprovers);
        recipients = FromGroupsToUsers(recipients, true);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewApproversNone;
      }
      
      // Подписание.
      if (stageType == StageType.Sign)
      {
        performers.Add(signatory);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewSignNone;
      }
      
      // Рассмотрение.
      if (stageType == StageType.Review)
      {
        performers.Add(addressee);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewAddresseeNone;
      }
      
      // Создание поручений по документу.
      if (stageType == StageType.Execution)
      {
        var assistant = Functions.ApprovalStage.GetStagePerformer(task, stage, signatory, addressee);
        performers.Add(assistant);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewAssistantNone;
      }
      
      // Регистрация, Отправка контрагенту.
      if (stageType == StageType.Register || stageType == StageType.Sending || stageType == StageType.Print)
      {
        var performer = Functions.ApprovalStage.GetStagePerformer(task, stage, signatory, addressee);
        performers.Add(performer);
      }
      
      // Контроль возврата.
      if (stageType == StageType.CheckReturn)
      {
        recipients = Functions.ApprovalStage.GetStageRecipients(stage, task);
        recipients = FromGroupsToUsers(recipients, true);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewApproversNone;
      }
      
      // Задание, Уведомление.
      if (stageType == StageType.SimpleAgr || stageType == StageType.Notice)
      {
        recipients = Functions.ApprovalStage.GetStageRecipients(stage, task, additionalApprovers);
        recipients = FromGroupsToUsers(recipients, false);
        emptyPerformersLabel = ApprovalRuleBases.Resources.StateViewApproversNone;
      }
      
      // Очистить пустых исполнителей.
      performers = performers.Where(p => p != null).ToList();
      recipients = recipients.Where(p => p != null).ToList();
      
      // Синхронизировать коллекции performers и recipients.
      if (!recipients.Any())
        recipients.AddRange(performers);
      
      if (!performers.Any())
        performers.AddRange(FromGroupsToUsers(recipients, true).Where(r => Employees.Is(r)).Select(r => Employees.As(r)));
      
      return Structures.ApprovalRuleBase.BlockPerformers.Create(performers, recipients, emptyPerformersLabel);
    }
    
    /// <summary>
    /// Достать сотрудников из групп, и добавить их в список к имеющимся исполнителям.
    /// </summary>
    /// <param name="usersAndGroups">Исполнители и группы.</param>
    /// <param name="forceUnpackGroups">Доставать исполнителей безусловно.</param>
    /// <returns>Список исполнителей с раскрытыми группами.</returns>
    private static List<IRecipient> FromGroupsToUsers(List<IRecipient> usersAndGroups, bool forceUnpackGroups)
    {
      // Установить максимальное количество отображаемых исполнителей.
      var maxDisplayedMembers = 5;
      var recipientsToShow = new List<IRecipient>();
      foreach (var recipient in usersAndGroups)
      {
        if (!Groups.Is(recipient))
          recipientsToShow.Add(recipient);
        else
        {
          var group = Groups.As(recipient);
          var groupMembers = Groups.GetAllUsersInGroup(group)
            .Where(r => Employees.Is(r) && r.Status == CoreEntities.DatabookEntry.Status.Active)
            .Select(r => Recipients.As(r));
          
          // Добавить наименование группы для вывода, если исполнителей нет.
          if (groupMembers.Count() == 0)
            recipientsToShow.Add(group);
          else
          {
            // Разворачивать участников безусловно, или если их количество меньше максимального для отображения.
            if (forceUnpackGroups || groupMembers.Count() < maxDisplayedMembers)
              recipientsToShow.AddRange(groupMembers);
            else
              recipientsToShow.Add(group);
          }
        }
      }
      return recipientsToShow.Distinct().ToList();
    }
    
    /// <summary>
    /// Сформировать тему задания\уведомления.
    /// </summary>
    /// <param name="stage">Этап с темой.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Строка вида 'Тема этапа: Название документа'.</returns>
    [Public]
    public static string FormatStageSubject(IApprovalStage stage, Content.IElectronicDocument document)
    {
      var subject = stage.Subject.TrimEnd(new[] { ' ', '.', ':' });
      return Docflow.PublicFunctions.Module.TrimSpecialSymbols("{0}: {1}", subject, document.Name);
    }
    
    #endregion

    /// <summary>
    /// Получить список всех возможных последовательностей этапов.
    /// </summary>
    /// <returns>Список всех возможных последовательностей блоков (список последовательностей состоит из номеров этапов или условий).
    /// Список номеров недостижимых блоков.</returns>
    public Structures.ApprovalRuleBase.StagesVariants GetAllStagesVariants()
    {
      var stages = new List<List<Structures.ApprovalRuleBase.RouteStep>>() { };
      stages.Add(new List<Structures.ApprovalRuleBase.RouteStep>() { });
      var ruleConditions = this.GetRuleConditionsWithTypes();
      
      var firstStage = _obj.Transitions.Select(x => x.SourceStage).FirstOrDefault(s => !_obj.Transitions.Any(t => t.TargetStage.Equals(s)));
      if (_obj.Stages.Count == 1 && !_obj.Conditions.Any() && !firstStage.HasValue)
        firstStage = _obj.Stages.Single().Number;
      stages[0].Add(Structures.ApprovalRuleBase.RouteStep.Create((int)firstStage, true) ?? Structures.ApprovalRuleBase.RouteStep.Create(0, true));
      
      var iterationCount = _obj.Stages.Count() + _obj.Conditions.Count();
      
      for (var iteration = 0; iteration <= iterationCount; iteration++)
      {
        var stagesCount = stages.Count;
        for (var i = 0; i < stagesCount; i++)
        {
          var stageSequence = stages[i];
          
          if (stageSequence.Count == iteration)
          {
            var lastNumber = stageSequence.Last().StepNumber;
            
            if (_obj.Stages.Any(s => s.Number == lastNumber))
            {
              var nextTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber);
              if (nextTransition != null)
                stageSequence.Add(Structures.ApprovalRuleBase.RouteStep.Create(nextTransition.TargetStage.Value, true));
            }
            else
            {
              var hasTrueTransition = this.CheckRoutePossibility(stageSequence, ruleConditions, Structures.ApprovalRuleBase.RouteStep.Create(lastNumber, true));
              var hasFalseTransition = this.CheckRoutePossibility(stageSequence, ruleConditions, Structures.ApprovalRuleBase.RouteStep.Create(lastNumber, false));
              
              if (hasTrueTransition && hasFalseTransition)
              {
                var newStageSequence = new List<Structures.ApprovalRuleBase.RouteStep>() { };
                stages.Add(newStageSequence);
                newStageSequence.AddRange(stageSequence);

                var lastStage = stageSequence.Last();
                stageSequence.Remove(lastStage);
                lastStage = Structures.ApprovalRuleBase.RouteStep.Create(lastStage.StepNumber, true);
                stageSequence.Add(lastStage);

                var trueResultTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber && t.ConditionValue == true);
                if (trueResultTransition != null)
                  stageSequence.Add(Structures.ApprovalRuleBase.RouteStep.Create(trueResultTransition.TargetStage.Value, true));
                
                lastStage = newStageSequence.Last();
                newStageSequence.Remove(lastStage);
                lastStage = Structures.ApprovalRuleBase.RouteStep.Create(lastStage.StepNumber, false);
                newStageSequence.Add(lastStage);

                var falseResultTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber && t.ConditionValue == false);
                if (falseResultTransition != null)
                  newStageSequence.Add(Structures.ApprovalRuleBase.RouteStep.Create(falseResultTransition.TargetStage.Value, true));
              }
              else
              {
                var lastStage = stageSequence.Last();
                stageSequence.Remove(lastStage);
                lastStage = Structures.ApprovalRuleBase.RouteStep.Create(lastStage.StepNumber, hasTrueTransition);
                stageSequence.Add(lastStage);

                var trueResultTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == lastNumber && t.ConditionValue == hasTrueTransition);
                if (trueResultTransition != null)
                  stageSequence.Add(Structures.ApprovalRuleBase.RouteStep.Create(trueResultTransition.TargetStage.Value, hasTrueTransition));
              }
            }
          }
        }
      }
      
      // Получение недостижимых блоков, с учетом исключенных условий.
      var allStages = _obj.Transitions.Select(t => t.SourceStage.Value).ToList();
      allStages.AddRange(_obj.Transitions.Select(t => t.TargetStage.Value).ToList());
      
      var unreachableRoutes = allStages.Distinct()
        .Except(stages.SelectMany(p => p.Select(s => s.StepNumber)).Distinct());
      
      return Structures.ApprovalRuleBase.StagesVariants.Create(stages.Select(x => x.Select(s => s.StepNumber).ToList()).ToList(), unreachableRoutes.ToList());
    }
    
    /// <summary>
    /// Проверка возможности существования маршрута правила.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="ruleConditions">Условия.</param>
    /// <param name="conditionStep">Этап.</param>
    /// <returns>Возможность существования.</returns>
    public virtual bool CheckRoutePossibility(List<Structures.ApprovalRuleBase.RouteStep> route,
                                              List<Structures.ApprovalRuleBase.ConditionRouteStep> ruleConditions,
                                              Structures.ApprovalRuleBase.RouteStep conditionStep)
    {
      var possibleStage = true;
      var routeSteps = route.Select(r => r.StepNumber).ToList();
      var allConditions = ruleConditions
        .Where(c => routeSteps.Contains(c.RouteStep.StepNumber) && c.RouteStep.StepNumber != conditionStep.StepNumber)
        .Select(c => Structures.ApprovalRuleBase.ConditionRouteStep.Create(Structures.ApprovalRuleBase.RouteStep.Create(c.RouteStep.StepNumber,
                                                                                                                        route.First(r => r.StepNumber == c.RouteStep.StepNumber).Branch),
                                                                           c.ConditionType))
        .ToList();
      var conditionType = _obj.Conditions.First(c => c.Number == conditionStep.StepNumber).Condition.ConditionType;
      
      // Проверка условий по сумме.
      if (conditionType == Docflow.ConditionBase.ConditionType.AmountIsMore)
      {
        var amountConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.AmountIsMore).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckAmountConditions(amountConditions, conditionStep);
      }
      
      // Проверка условий по валютам.
      if (conditionType == Docflow.ConditionBase.ConditionType.Currency)
      {
        var currencyConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.Currency).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckCurrencyConditions(currencyConditions, conditionStep);
      }
      
      // Проверка условий по контрагенту-нерезиденту.
      if (conditionType == Docflow.ConditionBase.ConditionType.Nonresident)
      {
        var nonresidentConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.Nonresident).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckNonresidentConditions(nonresidentConditions, conditionStep);
      }
      
      // Проверка условий по проектам.
      if (conditionType == Docflow.ConditionBase.ConditionType.ProjectDocument)
      {
        var projectConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.ProjectDocument).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckProjectConditions(projectConditions, conditionStep);
      }
      
      // Проверка условий по методу доставки.
      if (conditionType == Docflow.ConditionBase.ConditionType.DeliveryMethod)
      {
        var deliveryConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.DeliveryMethod).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckDeliveryMethodsConditions(deliveryConditions, conditionStep);
      }
      
      // Проверка условий по видам документов.
      if (conditionType == Docflow.ConditionBase.ConditionType.DocumentKind)
      {
        var documentKindConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.DocumentKind).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckDocumentKindConditions(documentKindConditions, conditionStep);
      }
      
      // Условие роль-роль
      if (conditionType == Docflow.ConditionBase.ConditionType.RolesComparer)
      {
        var conditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.RolesComparer).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckRolesComparerConditions(conditions, conditionStep);
      }
      
      // Условие роль-сотрудник
      if (conditionType == Docflow.ConditionBase.ConditionType.RoleEmpComparer)
      {
        var conditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.RoleEmpComparer).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckRoleEmpComparerConditions(conditions, conditionStep);
      }
      
      // Условие сотрудник входит в роль.
      if (conditionType == Docflow.ConditionBase.ConditionType.EmployeeInRole)
      {
        var conditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.EmployeeInRole).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckRoleEmpComparerConditions(conditions, conditionStep);
      }
      
      // Проверка условий по подписанности документа контрагентом.
      if (conditionType == Docflow.ConditionBase.ConditionType.SignedByCParty)
      {
        var signedByCounterpartyConditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.SignedByCParty).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckSignedByCounterpartyConditions(signedByCounterpartyConditions, conditionStep);
      }
      
      // Проверка условий по наличию приложения с указанным видом.
      if (conditionType == Docflow.ConditionBase.ConditionType.HasAddenda)
      {
        var conditions = allConditions.Where(c => c.ConditionType == Docflow.ConditionBase.ConditionType.HasAddenda).Select(c => c.RouteStep).ToList();
        possibleStage = this.CheckHasAddendaConditions(conditions, conditionStep);
      }
      
      return possibleStage;
    }

    /// <summary>
    /// Получить все условия по типу в данном маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="conditionType">Тип условия.</param>
    /// <returns>Условия.</returns>
    protected List<Structures.ApprovalRuleBase.RouteStep> GetConditionsInRoute(List<Structures.ApprovalRuleBase.RouteStep> route, Enumeration? conditionType)
    {
      return route.Where(e => _obj.Conditions.Any(c => Equals(c.Number, e.StepNumber) && Equals(c.Condition.ConditionType, conditionType))).ToList();
    }

    /// <summary>
    /// Получить все условия и их типы по правилу согласования.
    /// </summary>
    /// <returns>Список условий и их типов.</returns>
    protected List<Structures.ApprovalRuleBase.ConditionRouteStep> GetRuleConditionsWithTypes()
    {
      return _obj.Conditions.Select(x => Structures.ApprovalRuleBase.ConditionRouteStep.Create(Structures.ApprovalRuleBase.RouteStep.Create(x.Number.Value, true),
                                                                                               x.Condition.ConditionType))
        .ToList();
    }
    
    #region Проверка возможности существования маршрутов правила
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по сумме.
    /// </summary>
    /// <param name="allConditions">Все условия в данной ветке.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данной ветки.</returns>
    private bool CheckAmountConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      var amount = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault().Condition.Amount.Value;
      var conditionOperator = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault().Condition.AmountOperator;
      
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previousConditionOperator = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault().Condition.AmountOperator;
        var previousConditionAmount = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault().Condition.Amount.Value;
        
        #region Предыдущий оператор "Больше" и ветка true ИЛИ предыдущий оператор "Меньше или равно" и ветка false
        
        if ((previousConditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan && previousCondition.Branch) ||
            (previousConditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual && !previousCondition.Branch))
        {
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan || conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual) &&
              amount <= previousConditionAmount)
          {
            return condition.Branch;
          }
          
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan || conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual) &&
              amount <= previousConditionAmount)
          {
            return !condition.Branch;
          }
        }
        
        #endregion
        
        #region Предыдущий оператор "Меньше" и ветка true ИЛИ предыдущий оператор "Больше или равно" и ветка false
        
        if ((previousConditionOperator == Docflow.ConditionBase.AmountOperator.LessThan && previousCondition.Branch) ||
            (previousConditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual && !previousCondition.Branch))
        {
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan || conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual) &&
              amount >= previousConditionAmount)
          {
            return !condition.Branch;
          }
          
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan || conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual) &&
              amount >= previousConditionAmount)
          {
            return condition.Branch;
          }
        }
        
        #endregion
        
        #region Предыдущий оператор "Больше или равно" и ветка true ИЛИ предыдущий оператор "Меньше" и ветка false
        
        if ((previousConditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual && previousCondition.Branch) ||
            (previousConditionOperator == Docflow.ConditionBase.AmountOperator.LessThan && !previousCondition.Branch))
        {
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan || conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual) &&
              amount < previousConditionAmount)
          {
            return condition.Branch;
          }
          
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan || conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual) &&
              amount < previousConditionAmount)
          {
            return !condition.Branch;
          }
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan && amount == previousConditionAmount)
            continue;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual && amount == previousConditionAmount)
            return condition.Branch;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan && amount == previousConditionAmount)
            return !condition.Branch;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual && amount == previousConditionAmount)
            continue;
        }
        
        #endregion
        
        #region Предыдущий оператор "Меньше или равно" и ветка true ИЛИ предыдущий оператор "Больше" и ветка false
        
        if ((previousConditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual && previousCondition.Branch) ||
            (previousConditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan && !previousCondition.Branch))
        {
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan || conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual) &&
              amount > previousConditionAmount)
          {
            return !condition.Branch;
          }
          
          if ((conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan || conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual) &&
              amount > previousConditionAmount)
          {
            return condition.Branch;
          }
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterThan && amount == previousConditionAmount)
            return !condition.Branch;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.GreaterOrEqual && amount == previousConditionAmount)
            continue;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.LessThan && amount == previousConditionAmount)
            continue;
          
          if (conditionOperator == Docflow.ConditionBase.AmountOperator.LessOrEqual && amount == previousConditionAmount)
            return condition.Branch;
        }
        
        #endregion
      }
      
      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по контрагенту-нерезиденту.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckNonresidentConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по подписанию документа контрагентом.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckSignedByCounterpartyConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по виду во вложениях.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckHasAddendaConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var current = _obj.Conditions.Where(x => x.Number == condition.StepNumber).Single().Condition;
        var previous = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).Single().Condition;
        
        // Если виды одинаковые - недостижимы ветки с разными результатами
        if (Equals(current.AddendaDocumentKind, previous.AddendaDocumentKind))
          return previousCondition.Branch == condition.Branch;
      }

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по роль-роль.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckRolesComparerConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var current = _obj.Conditions.Where(x => x.Number == condition.StepNumber).Single().Condition;
        var previous = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).Single().Condition;
        var sameRoles = (Equals(current.ApprovalRole, previous.ApprovalRole) &&
                         Equals(current.ApprovalRoleForComparison, previous.ApprovalRoleForComparison)) ||
          (Equals(current.ApprovalRoleForComparison, previous.ApprovalRole) &&
           Equals(current.ApprovalRole, previous.ApprovalRoleForComparison));
        if (sameRoles)
          return previousCondition.Branch == condition.Branch;
      }

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по роль-сотрудник.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckRoleEmpComparerConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var current = _obj.Conditions.Where(x => x.Number == condition.StepNumber).Single().Condition;
        var previous = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).Single().Condition;
        var sameRoleAndRecipient = Equals(current.ApprovalRole, previous.ApprovalRole)
          && Equals(current.RecipientForComparison, previous.RecipientForComparison);
        if (sameRoleAndRecipient)
          return previousCondition.Branch == condition.Branch;
      }

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по проектным документам.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckProjectConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
        return previousCondition.Branch == condition.Branch;

      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по способам доставки.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckDeliveryMethodsConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      var conditionItem = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault();
      var deliveryMethods = conditionItem.Condition.DeliveryMethods.Select(x => x.DeliveryMethod).ToList();
      
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previousConditionItem = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault();
        var previousDeliveryMethods = previousConditionItem.Condition.DeliveryMethods.Select(x => x.DeliveryMethod).ToList();

        var result = CheckConsistencyConditions(deliveryMethods, previousDeliveryMethods, condition, previousCondition);
        if (result != null)
          return result.Value;
      }
      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по валюте.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckCurrencyConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      var conditionItem = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault();
      var currencies = conditionItem.Condition.Currencies.Select(x => x.Currency).ToList();
      
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previousConditionItem = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault();
        var previousCurrencies = previousConditionItem.Condition.Currencies.Select(x => x.Currency).ToList();

        var result = CheckConsistencyConditions(currencies, previousCurrencies, condition, previousCondition);
        if (result != null)
          return result.Value;
      }
      
      return true;
    }
    
    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по виду документа.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    private bool CheckDocumentKindConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      var conditionItem = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault();
      var documentKinds = conditionItem.Condition.ConditionDocumentKinds.Select(x => x.DocumentKind).ToList();
      
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previousConditionItem = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault();
        var previousDocumentKinds = previousConditionItem.Condition.ConditionDocumentKinds.Select(x => x.DocumentKind).ToList();

        var result = CheckConsistencyConditions(documentKinds, previousDocumentKinds, condition, previousCondition);
        if (result != null)
          return result.Value;
      }
      return true;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, хватает ли регистратору прав для регистрации видов документов и есть ли задачи с использованием правила.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <returns>CanRegister и HasTaskInProcess.</returns>
    [Remote(IsPure = true)]
    public static int CanRegisterAndHasTaskInProcess(IApprovalRuleBase rule)
    {
      return (ClerkCanRegisterAllDocumentKinds(rule) ? Constants.ApprovalRuleBase.HintMask.CanRegister : 0) +
        (Functions.ApprovalRuleBase.HasTasksInProcess(rule) ? Constants.ApprovalRuleBase.HintMask.HasTaskInProcess : 0);
    }
    
    /// <summary>
    /// Проверить права регистратора на виды документа в правиле.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <returns>True, если нет проблем с регистрацией выбранных видов документов.</returns>
    /// <remarks>True, если этапа регистрации в правиле нет.</remarks>
    public static bool ClerkCanRegisterAllDocumentKinds(IApprovalRuleBase rule)
    {
      var registerStages = rule.Stages
        .Where(st => st.Stage != null && st.StageType != null)
        .Where(s => s.Stage.StageType == StageType.Register && s.Stage.Assignee != null);
      
      var ruleDocumentKindsDirections = rule.DocumentKinds
        .Select(k => k.DocumentKind.DocumentType.DocumentFlow)
        .Distinct();

      var canRegister = true;
      foreach (var registerStage in registerStages)
      {
        var clerk = GetEmployeeByAssignee(registerStage.Stage.Assignee);
        
        if (ruleDocumentKindsDirections.Contains(Sungero.Docflow.DocumentKind.DocumentFlow.Incoming))
          canRegister = canRegister && IncomingDocumentBases.AccessRights.CanRegister(clerk);

        if (ruleDocumentKindsDirections.Contains(Sungero.Docflow.DocumentKind.DocumentFlow.Outgoing))
          canRegister = canRegister && OutgoingDocumentBases.AccessRights.CanRegister(clerk);
        
        if (ruleDocumentKindsDirections.Contains(Sungero.Docflow.DocumentKind.DocumentFlow.Inner))
          canRegister = canRegister && InternalDocumentBases.AccessRights.CanRegister(clerk);

        if (ruleDocumentKindsDirections.Contains(Sungero.Docflow.DocumentKind.DocumentFlow.Contracts))
          canRegister = canRegister && ContractualDocumentBases.AccessRights.CanRegister(clerk);
        
        if (!canRegister)
          return canRegister;
      }
      return canRegister;
    }
    
    /// <summary>
    /// Получить дублирующие правила.
    /// </summary>
    /// <returns>Правила, конфликтующие с текущим.</returns>
    [Remote(IsPure = true), Public]
    public virtual List<IApprovalRuleBase> GetDoubleRules()
    {
      var conflictedRules = new List<IApprovalRuleBase>();
      var allRules = ApprovalRuleBases.GetAll(r => !Equals(_obj, r) && r.DocumentFlow == _obj.DocumentFlow && r.Status == Docflow.ApprovalRuleBase.Status.Active).ToList();
      
      #region Поиск предыдущей действующей версии
      
      var prevRule = _obj.ParentRule;
      
      while (prevRule != null && prevRule.Status != Docflow.ApprovalRuleBase.Status.Active)
        prevRule = prevRule.ParentRule;

      if (prevRule != null)
        allRules = allRules.Where(r => !Equals(prevRule, r)).ToList();
      
      #endregion
      
      if (_obj.BusinessUnits.Any())
      {
        foreach (var businessUnit in _obj.BusinessUnits)
        {
          conflictedRules.AddRange(allRules.Where(s => s.BusinessUnits.Any(o => o.BusinessUnit == businessUnit.BusinessUnit)).ToList());
        }
      }
      else
      {
        conflictedRules.AddRange(allRules.Where(s => !s.BusinessUnits.Any()).ToList());
      }
      
      allRules = conflictedRules.ToList();
      conflictedRules.Clear();
      
      if (_obj.DocumentKinds.Any())
      {
        foreach (var documentKind in _obj.DocumentKinds)
        {
          conflictedRules.AddRange(allRules.Where(s => s.DocumentKinds.Any(o => o.DocumentKind == documentKind.DocumentKind)).ToList());
        }
      }
      else
      {
        conflictedRules.AddRange(allRules.Where(s => !s.DocumentKinds.Any()).ToList());
      }
      
      allRules = conflictedRules.ToList();
      conflictedRules.Clear();
      
      if (_obj.Departments.Any())
      {
        foreach (var department in _obj.Departments)
        {
          conflictedRules.AddRange(allRules.Where(s => s.Departments.Any(o => o.Department == department.Department)).ToList());
        }
      }
      else
      {
        conflictedRules.AddRange(allRules.Where(s => !s.Departments.Any()).ToList());
      }
      
      allRules = conflictedRules.ToList();
      conflictedRules.Clear();
      
      if (_obj.DocumentGroups.Any())
      {
        foreach (var documentGroup in _obj.DocumentGroups)
        {
          conflictedRules.AddRange(allRules.Where(s => s.DocumentGroups.Any(o => o.DocumentGroup == documentGroup.DocumentGroup)).ToList());
        }
      }
      else
      {
        conflictedRules.AddRange(allRules.Where(s => !s.DocumentGroups.Any()).ToList());
      }
      
      conflictedRules = conflictedRules.Distinct().ToList();
      
      return conflictedRules;
    }
    
    /// <summary>
    /// Проверить возможность использования ролей в маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="showErrors">Выводить хинты или просто проверить валидность.</param>
    /// <param name="verifiedRoles">Список проверяемых типов ролей. Если пустой - проверяются все роли, для которых есть ограничение.</param>
    /// <returns>Возможность использования проверяемых типов ролей.</returns>
    public List<Structures.ApprovalRuleBase.StagesIncorrectRoles> CheckImpossibleRoles(List<int> route, bool showErrors, List<Enumeration?> verifiedRoles)
    {
      var incorrectStagesRoles = new List<Structures.ApprovalRuleBase.StagesIncorrectRoles>();
      
      if (showErrors)
      {
        var routeStages = _obj.Stages.Where(s => route.Any(r => r == s.Number));

        // Роли, которые не могут быть использованы без соответствующих этапов.
        var roles = this.GetRolesForCheck();
        
        if (verifiedRoles.Any())
          roles = roles.Where(r => verifiedRoles.Contains(r)).ToList();

        foreach (var role in roles)
        {
          // Этапы, где исполнителем указана роль.
          var stages = routeStages.Where(s => s.Stage != null)
            .Where(s => (s.Stage.ApprovalRole != null && Equals(s.Stage.ApprovalRole.Type, role)) ||
                   s.Stage.ApprovalRoles.Any(x => Equals(x.ApprovalRole.Type, role)));
          
          if (stages.Any())
          {
            var errorMessage = this.GetErrorMessageForRoleInRoute(role, routeStages);
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
              foreach (var stage in stages)
                incorrectStagesRoles.Add(Structures.ApprovalRuleBase.StagesIncorrectRoles.Create(stage, errorMessage));
            }
          }
        }
      }
      
      // Проверка обращений к проектным ролям после явного условия, что нет проекта в документе.
      {
        var failedStages = new List<Structures.ApprovalRuleBase.StageWithUnsupportedRole>();
        // Поиск условия, после которого нельзя использовать проектные роли.
        for (int i = 0; i < route.Count - 1; i++)
        {
          var current = route[i];
          var transition = _obj.Transitions
            .SingleOrDefault(s => s.SourceStage == current && s.TargetStage == route[i + 1] && s.ConditionValue == false);
          if (transition != null)
          {
            var condition = _obj.Conditions.SingleOrDefault(s => s.Number == current);
            if (condition != null && condition.Condition.ConditionType == Docflow.ConditionBase.ConditionType.ProjectDocument)
            {
              // Поиск этапов, которые нарушают условие.
              for (int j = i + 1; j < route.Count; j++)
              {
                var stage = _obj.Stages.SingleOrDefault(s => s.Number == route[j] && s.Stage != null);
                if (stage == null)
                  continue;
                
                if (Functions.ApprovalStage.HasRole(stage.Stage, Docflow.ApprovalRoleBase.Type.ProjectManager))
                  failedStages.Add(Structures.ApprovalRuleBase.StageWithUnsupportedRole.Create(stage, Docflow.ApprovalRoleBase.Type.ProjectManager));
                
                if (Functions.ApprovalStage.HasRole(stage.Stage, Docflow.ApprovalRoleBase.Type.ProjectAdmin))
                  failedStages.Add(Structures.ApprovalRuleBase.StageWithUnsupportedRole.Create(stage, Docflow.ApprovalRoleBase.Type.ProjectAdmin));
              }
            }
          }
        }
        
        if (failedStages.Any())
        {
          if (showErrors)
          {
            
            foreach (var groupedStage in failedStages.GroupBy(s => s.Role))
            {
              var message = ApprovalRuleBases.Resources.CantUseProjectRoleFormat(ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(groupedStage.Key));
              foreach (var stage in groupedStage)
                incorrectStagesRoles.Add(Structures.ApprovalRuleBase.StagesIncorrectRoles.Create(stage.Stage, message));
            }
          }
        }
      }
      
      return incorrectStagesRoles;
    }
    
    /// <summary>
    /// Получить роли, которые не могут быть использованы без соответствующих этапов.
    /// </summary>
    /// <returns>Список ролей.</returns>
    public virtual System.Collections.Generic.List<Enumeration?> GetRolesForCheck()
    {
      var roles = new List<Enumeration?>();
      roles.AddRange(this.GetSignatoriesRoles());
      roles.AddRange(this.GetAddresseesRoles());
      
      return roles;
    }
    
    /// <summary>
    /// Получить роли подписантов.
    /// </summary>
    /// <returns>Список ролей.</returns>
    public virtual System.Collections.Generic.List<Enumeration?> GetSignatoriesRoles()
    {
      var roles = new List<Enumeration?>();
      roles.Add(Docflow.ApprovalRoleBase.Type.SignAssistant);
      roles.Add(Docflow.ApprovalRoleBase.Type.Signatory);
      
      return roles;
    }
    
    /// <summary>
    /// Получить роли адресатов.
    /// </summary>
    /// <returns>Список ролей.</returns>
    public virtual System.Collections.Generic.List<Enumeration?> GetAddresseesRoles()
    {
      var roles = new List<Enumeration?>();
      roles.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
      roles.Add(Docflow.ApprovalRoleBase.Type.Addressee);
      roles.Add(Docflow.ApprovalRoleBase.Type.Addressees);
      
      return roles;
    }
    
    /// <summary>
    /// Получить сообщение об ошибке валидации роли согласования в маршруте.
    /// </summary>
    /// <param name="roleType">Тип роли согласования.</param>
    /// <param name="routeStages">Этапы маршрута.</param>
    /// <returns>Сообщение об ошибке, пустая строка - если ошибки нет.</returns>
    public virtual string GetErrorMessageForRoleInRoute(Enumeration? roleType, System.Collections.Generic.IEnumerable<Sungero.Docflow.IApprovalRuleBaseStages> routeStages)
    {
      var isSupportableRole = routeStages.SelectMany(s => Functions.ApprovalStageBase.GetSupportableRoles(s.StageBase)).Contains(roleType);
      
      // Роль поддерживается одним из этапов в маршруте, проверка пропускается.
      if (isSupportableRole)
        return string.Empty;
      
      if (this.GetAddresseesRoles().Contains(roleType))
        return Sungero.Docflow.ApprovalRuleBases.Resources.CantUseRolesWithoutReviewStageFormat(ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(roleType));
      else if (this.GetSignatoriesRoles().Contains(roleType))
        return Sungero.Docflow.ApprovalRuleBases.Resources.CantUseRoleWithoutStageFormat(ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(roleType),
                                                                                         ApprovalStages.Info.Properties.StageType.GetLocalizedValue(Docflow.ApprovalStage.StageType.Sign));
      return string.Empty;
    }
    
    /// <summary>
    /// Вставить условие в правило согласования.
    /// </summary>
    /// <param name="rule">Правило согласования.</param>
    /// <param name="condition">Условие.</param>
    /// <param name="stageNumberBeforeCondition">Этап, после которого нужно вставить условие.</param>
    /// <param name="targetStageTrueCase">Этап, в который ведет ветка True.</param>
    /// <param name="targetStageFalseCase">Этап, в который ведет ветка False.</param>
    public static void AddConditionToRule(IApprovalRuleBase rule,
                                          IConditionBase condition,
                                          int stageNumberBeforeCondition,
                                          int targetStageTrueCase,
                                          int targetStageFalseCase)
    {
      var maxStageNumber = 0;
      var maxConditionNumber = 0;
      
      if (rule.Stages.Any())
        maxStageNumber = rule.Stages.Select(s => s.Number).Max() ?? 0;
      if (rule.Conditions.Any())
        maxConditionNumber = rule.Conditions.Select(c => c.Number).Max() ?? 0;
      
      var newConditionNumber = maxStageNumber > maxConditionNumber ?
        ++maxStageNumber :
        ++maxConditionNumber;
      
      var newCondition = rule.Conditions.AddNew();
      newCondition.Condition = condition;
      newCondition.Number = newConditionNumber;
      
      // Удалить переход, вместо которого нужно встроить условие.
      if (stageNumberBeforeCondition != 0)
      {
        var transitionToRemove = rule.Transitions.Where(t => t.SourceStage == stageNumberBeforeCondition).FirstOrDefault();
        rule.Transitions.Remove(transitionToRemove);
        
        // Добавить переход в условие.
        var transitionToCondition = rule.Transitions.AddNew();
        transitionToCondition.SourceStage = stageNumberBeforeCondition;
        transitionToCondition.TargetStage = newConditionNumber;
      }
      
      // Добавить ветку False.
      if (targetStageFalseCase != 0)
      {
        var newFalseTransition = rule.Transitions.AddNew();
        newFalseTransition.SourceStage = newConditionNumber;
        newFalseTransition.TargetStage = targetStageFalseCase;
        newFalseTransition.ConditionValue = false;
      }
      
      // Добавить ветку True.
      if (targetStageTrueCase != 0)
      {
        var newTrueTransition = rule.Transitions.AddNew();
        newTrueTransition.SourceStage = newConditionNumber;
        newTrueTransition.TargetStage = targetStageTrueCase;
        newTrueTransition.ConditionValue = true;
      }
      
      rule.Save();
    }
    
    /// <summary>
    /// Найти задачи в работе по данному правилу.
    /// </summary>
    /// <returns>Задачи в работе.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IApprovalTask> GetTasksInProcess()
    {
      return ApprovalTasks.GetAll(a => a.ApprovalRule == _obj && a.Status != Sungero.Workflow.Task.Status.Draft);
    }
    
    /// <summary>
    /// Проверить, есть ли задачи в работе по правилу.
    /// Права игнорируются.
    /// </summary>
    /// <returns>Есть ли задачи в работе.</returns>
    public bool HasTasksInProcess()
    {
      var commandText = Queries.ApprovalRuleBase.HasTaskInProcess;
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(commandText, _obj.Id);
        var resultCount = command.ExecuteScalar().ToString();
        int result = 0;
        return int.TryParse(resultCount, out result) ? result != 0 : false;
      }
    }
    
    /// <summary>
    /// Получить последний id задания, с результатом ForReapproving для задачи.
    /// </summary>
    /// <param name="taskId">Id задачи.</param>
    /// <returns>Id задания.</returns>
    public static int GetForReapprovingAssignmentResultId(int taskId)
    {
      var commandText = Queries.ApprovalRuleBase.GetForReapprovingAssignmentResultId;
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(commandText, taskId);
        var resultId = command.ExecuteScalar().ToString();
        int result = 0;
        return int.TryParse(resultId, out result) ? result : 0;
      }
    }
    
    /// <summary>
    /// Определить наличие этапа Контроля возврата после этапа Отправки КА.
    /// </summary>
    /// <param name="stages">Список этапов.</param>
    /// <param name="stageNumber">Номер текущего этапа.</param>
    /// <returns>True - присутствует, False - отсутствует.</returns>
    private static bool HasControlReturnAfterSending(List<Structures.Module.DefinedApprovalStageLite> stages, int stageNumber)
    {
      var stageIndex = stages.IndexOf(stages.FirstOrDefault(st => Equals(st.Number, stageNumber)));
      var firstReturnControlAfterStageIndex = stages.IndexOf(stages.FirstOrDefault(st => st.Stage.StageType == StageType.CheckReturn &&
                                                                                   stages.IndexOf(st) > stageIndex));
      var firstSendingAfterSendingIndex = stages.IndexOf(stages.FirstOrDefault(st => st.Stage.StageType == StageType.Sending &&
                                                                               stages.IndexOf(st) > stageIndex));
      
      if (firstReturnControlAfterStageIndex < 0)
        return false;
      
      return (firstSendingAfterSendingIndex < 0 && firstReturnControlAfterStageIndex > 0) ||
        (firstSendingAfterSendingIndex >= 0 && firstReturnControlAfterStageIndex < firstSendingAfterSendingIndex);
    }
    
    /// <summary>
    /// Определить, нужно ли отображать в регламенте блок Отправки КА.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="stages">Все этапы.</param>
    /// <returns>True - нужно, False - не нужно.</returns>
    private static bool NeedShowSendingStage(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var lastReapprovalAssignment = GetForReapprovingAssignmentResultId(task.Id);
      var assignments = Assignments.GetAll().Where(a => a.Task.Id == task.Id);
      if (lastReapprovalAssignment != 0)
        assignments = assignments.Where(x => x.Id > lastReapprovalAssignment);
      
      // Найти выполненное задание по этапу, в том числе среди схлопнутых заданий.
      foreach (var assignment in assignments.ToList())
      {
        // Для каждого задания берем свою дочернюю коллекцию, т.к. они везде имеют разные названия.
        var printingAsg = ApprovalPrintingAssignments.As(assignment);
        if (printingAsg != null && printingAsg.CollapsedStagesTypesPr.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = printingAsg.CollapsedStagesTypesPr.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Print));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == printingAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
        
        var registrationAsg = ApprovalRegistrationAssignments.As(assignment);
        if (registrationAsg != null && registrationAsg.CollapsedStagesTypesReg.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = registrationAsg.CollapsedStagesTypesReg.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Register));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == registrationAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
        
        var sendingAsg = ApprovalSendingAssignments.As(assignment);
        if (sendingAsg != null && sendingAsg.CollapsedStagesTypesSen.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = sendingAsg.CollapsedStagesTypesSen.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Sending));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == sendingAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
        
        var signingAsg = ApprovalSigningAssignments.As(assignment);
        if (signingAsg != null && signingAsg.CollapsedStagesTypesSig.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = signingAsg.CollapsedStagesTypesSig.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Sign || c.StageType == Docflow.ApprovalSigningAssignmentCollapsedStagesTypesSig.StageType.ConfirmSign));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == signingAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
        
        var reviewAsg = ApprovalReviewAssignments.As(assignment);
        if (reviewAsg != null && reviewAsg.CollapsedStagesTypesRe.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = reviewAsg.CollapsedStagesTypesRe.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Review || c.StageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.ReviewingResult));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == reviewAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
        
        var executionAsg = ApprovalExecutionAssignments.As(assignment);
        if (executionAsg != null && executionAsg.CollapsedStagesTypesExe.Any(c => c.StageType == StageType.Sending))
        {
          var collapsed = executionAsg.CollapsedStagesTypesExe.ToList();
          var stageIndex = collapsed.IndexOf(collapsed.Single(c => c.StageType == StageType.Execution));
          var asgIndex = stages.IndexOf(stages.Single(x => x.Number == executionAsg.StageNumber));
          
          if (HasStageInCollapsed(stageIndex, collapsed.Count, asgIndex, stage, stages))
            return true;
        }
      }
      
      if (task.Status == Workflow.Task.Status.Draft ||
          (stages.IndexOf(stage) > stages.FindIndex(x => x.Number == task.StageNumber)))
      {
        var needSignByUs = document.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired ||
          (document.ExchangeState == null &&
           document.ExternalApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed &&
           document.InternalApprovalState != Docflow.OfficialDocument.InternalApprovalState.Signed);
        
        var signing = stages.FirstOrDefault(x => x.StageType == StageType.Sign);
        // Не показывать отправку, только если документ подписан контрагентом и не подписан нами, и не будет этапа подписания до отправки.
        return !(needSignByUs && !(signing != null && stages.IndexOf(signing) < stages.IndexOf(stage)));
      }
      
      return false;
    }
    
    /// <summary>
    /// Проверить, есть ли указанный этап в диапазоне схлопнутых этапов.
    /// </summary>
    /// <param name="collapsedStageIndex">Индекс этапа задания среди схлопнутых.</param>
    /// <param name="collapsedStagesCount">Количество схлопнутых этапов.</param>
    /// <param name="stageIndex">Индекс этапа в рамках правила.</param>
    /// <param name="stage">Проверяемый этап.</param>
    /// <param name="stages">Все этапы.</param>
    /// <returns>True - если этап входит в схлопнутые, False - если нет.</returns>
    private static bool HasStageInCollapsed(int collapsedStageIndex, int collapsedStagesCount, int stageIndex,
                                            Structures.Module.DefinedApprovalStageLite stage, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      for (int i = 0; i < collapsedStagesCount; i++)
      {
        var collapsedStage = stages[stageIndex - collapsedStageIndex + i];
        if (collapsedStage.Number == stage.Number)
          return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Определить, нужно ли отображать в регламенте блок Контроля возврата.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="statusInfo">Статус блока.</param>
    /// <returns>True - нужно, False - не нужно.</returns>
    private static bool NeedShowControlReturnStage(IApprovalTask task, Structures.Module.DefinedApprovalStageLite stage, Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var lastReapprovalAssignment = GetForReapprovingAssignmentResultId(task.Id);
      var assignmentsByTask = Assignments.GetAll().Where(a => a.Task.Id == task.Id).ToList();
      var lastReworkAssignmentIndex = assignmentsByTask.FindIndex(a => a.Id == lastReapprovalAssignment);
      
      // Исключить из рассмотрения все задания, которые были раньше отправленного на доработку.
      if (lastReworkAssignmentIndex > 0)
        assignmentsByTask.RemoveRange(0, lastReworkAssignmentIndex);
      
      var needSignByCA = document.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignAwaited ||
        (document.ExchangeState == null && document.ExternalApprovalState != Docflow.OfficialDocument.ExternalApprovalState.Signed);
      
      // Не отображать блок, если соответствующее задание не найдено среди актуальных заданий, блок не в состоянии "Не стартован" и
      // если документ находится в статусе "Подписан КА", но блок Контроля возврата не в "Завершен".
      return assignmentsByTask.Any(a => ApprovalCheckReturnAssignments.Is(a) && ApprovalCheckReturnAssignments.As(a).StageNumber == stage.Number) ||
        (statusInfo.IsNext && needSignByCA);
    }
    
    /// <summary>
    /// Создать новую версию правила.
    /// </summary>
    /// <returns>Правило согласования.</returns>
    [Remote]
    public virtual IApprovalRuleBase GetOrCreateNextVersion()
    {
      var version = Functions.ApprovalRuleBase.GetNextVersion(_obj);
      if (version != null)
        return version;
      
      version = ApprovalRuleBases.Copy(_obj);
      version.ParentRule = _obj;
      version.VersionNumber = _obj.VersionNumber + 1;
      return version;
    }
    
    #region Получение правил по параметрам
    
    /// <summary>
    /// Получить доступные правила по документу.
    /// </summary>
    /// <param name="document">Документ для подбора правила.</param>
    /// <returns>Все правила, которые подходят к документу.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IApprovalRuleBase> GetAvailableRulesByDocument(Sungero.Docflow.IOfficialDocument document)
    {
      var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
      return GetAvailableRulesByParams(document.DocumentKind.DocumentFlow, document.BusinessUnit,
                                       document.DocumentKind, document.Department, documentGroup);
    }
    
    /// <summary>
    /// Получить доступные правила по параметрам.
    /// </summary>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="documentGroup">Группа документов.</param>
    /// <returns>Доступные по параметрам правила.</returns>
    [Public]
    public static IQueryable<IApprovalRuleBase> GetAvailableRulesByParams(Enumeration? documentFlow,
                                                                          IBusinessUnit businessUnit,
                                                                          IDocumentKind documentKind,
                                                                          IDepartment department,
                                                                          IDocumentGroupBase documentGroup)
    {
      var rules = ApprovalRuleBases.GetAll(r => r.Status == CoreEntities.DatabookEntry.Status.Active);

      if (documentFlow != null)
        rules = rules.Where(r => r.DocumentFlow == documentFlow || r.DocumentFlow == null);
      
      rules = businessUnit != null ?
        rules.Where(r => r.BusinessUnits.Any(o => Equals(o.BusinessUnit, businessUnit)) || !r.BusinessUnits.Any()) :
        rules.Where(r => !r.BusinessUnits.Any());
      
      rules = documentKind != null ?
        rules.Where(r => r.DocumentKinds.Any(o => Equals(o.DocumentKind, documentKind)) || !r.DocumentKinds.Any()) :
        rules.Where(r => !r.DocumentKinds.Any());
      
      rules = department != null ?
        rules.Where(r => r.Departments.Any(o => Equals(o.Department, department)) || !r.Departments.Any()) :
        rules.Where(r => !r.Departments.Any());

      rules = documentGroup != null ?
        rules.Where(r => r.DocumentGroups.Any(o => Equals(o.DocumentGroup, documentGroup)) || !r.DocumentGroups.Any()) :
        rules.Where(r => !r.DocumentGroups.Any());
      
      return rules;
    }
    
    /// <summary>
    /// Получить черновики или действующие правила согласования по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Правила согласования.</returns>
    [Public]
    public static IQueryable<IApprovalRuleBase> GetApprovalRulesByDocumentKind(IDocumentKind documentKind)
    {
      return ApprovalRuleBases.GetAll(r => r.Status == Docflow.ApprovalRuleBase.Status.Active || r.Status == Docflow.ApprovalRuleBase.Status.Draft)
        .Where(r => r.DocumentKinds.Any(k => k.DocumentKind.Id == documentKind.Id) || r.Conditions.Any(c => c.Condition.ConditionDocumentKinds.Any(d => d.DocumentKind.Id == documentKind.Id)));
    }
    
    #endregion
    
    #region Функции для отчета
    
    /// <summary>
    /// Получить подходящие правила по параметрам.
    /// </summary>
    /// <param name="activeRules">Активные правила.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="businessUnitId">НОР.</param>
    /// <param name="documentKindId">Вид документа.</param>
    /// <param name="departmentId">Подразделение.</param>
    /// <param name="documentGroupId">Группа документов.</param>
    /// <returns>Подходящие правила.</returns>
    [Public]
    public static IQueryable<IApprovalRuleBase> GetRulesByParamsIds(List<IApprovalRuleBase> activeRules, Enumeration? documentFlow,
                                                                    int businessUnitId, int documentKindId, int departmentId, int documentGroupId)
    {
      if (documentFlow == null)
        return activeRules.AsQueryable();
      
      return activeRules
        .Where(r => r.DocumentFlow == documentFlow || r.DocumentFlow == null)
        .Where(r => !r.BusinessUnits.Any() ||
               r.BusinessUnits.Any(o => o.BusinessUnit != null && o.BusinessUnit.Id == businessUnitId))
        .Where(r => !r.DocumentKinds.Any() ||
               r.DocumentKinds.Any(o => o.DocumentKind != null && o.DocumentKind.Id == documentKindId))
        .Where(r => !r.Departments.Any() ||
               r.Departments.Any(o => o.Department != null && o.Department.Id == departmentId))
        .Where(r => !r.DocumentGroups.Any() ||
               r.DocumentGroups.Any(o => o.DocumentGroup != null && o.DocumentGroup.Id == documentGroupId))
        .AsQueryable();
    }
    
    /// <summary>
    /// Получить правило по параметрам.
    /// </summary>
    /// <param name="activeRules">Активные правила.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="businessUnitId">НОР.</param>
    /// <param name="documentKindId">Вид документа.</param>
    /// <param name="departmentId">Подразделение.</param>
    /// <param name="documentGroupId">Группа документов.</param>
    /// <returns>Правило, которое имеет наивысший приоритет.</returns>
    [Public]
    public static IApprovalRuleBase GetRuleByParamsIds(List<IApprovalRuleBase> activeRules, Enumeration? documentFlow,
                                                       int businessUnitId, int documentKindId, int departmentId, int documentGroupId)
    {
      var rules = GetRulesByParamsIds(activeRules, documentFlow, businessUnitId, documentKindId, departmentId, documentGroupId);
      return rules.OrderByDescending(r => r.Priority).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сотрудника из реципиента.
    /// </summary>
    /// <param name="assignee">Реципиент.</param>
    /// <returns>Сотрудник.</returns>
    [Public, Remote(IsPure = true)]
    public static IEmployee GetEmployeeByAssignee(IRecipient assignee)
    {
      // Сотрудник.
      if (Employees.Is(assignee))
        return Employees.As(assignee);
      
      // Роль.
      var role = Roles.As(assignee);
      if (role != null)
      {
        // Взять первого реципиента.
        var employee = role.RecipientLinks
          .Select(r => r.Member)
          .Where(m => Employees.Is(m))
          .FirstOrDefault();
        
        return Employees.As(employee);
      }
      
      return null;
    }
    
    /// <summary>
    /// Получить согласующих по параметрам.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="transitions">Переходы.</param>
    /// <returns>Согласующие.</returns>
    [Public]
    public static string GetApproversByRule(IApprovalRuleBase rule, List<int> transitions)
    {
      var allApprovers = string.Empty;
      var separator = string.Format(";{0}", Environment.NewLine);
      
      foreach (var stage in rule.Stages.Where(s => transitions.Contains(s.Number.Value)).Select(s => s.Stage))
      {
        if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Manager)
        {
          if (!string.IsNullOrEmpty(allApprovers))
            allApprovers += separator;
          
          if (stage.AssigneeType == AssigneeType.Employee)
          {
            var employee = GetEmployeeByAssignee(stage.Assignee);
            allApprovers += Company.PublicFunctions.Employee.GetJobTitleWithShortName(employee);
          }
          else
            allApprovers += string.Format("<b>{0}</b>", stage.ApprovalRole.Name);
        }
        
        if (stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Approvers)
        {
          var recipients = stage.Recipients.Where(rec => rec.Recipient != null)
            .Select(rec => rec.Recipient)
            .ToList();
          
          if (recipients.Any())
          {
            var approversNames = new List<string>();
            
            foreach (var recipient in recipients)
              approversNames.Add(recipient.Name);
            
            if (!string.IsNullOrEmpty(allApprovers))
              allApprovers += separator;
            allApprovers += string.Join(separator, approversNames.Select(a => string.Format("<b>{0}</b>", a)));
          }
          var roles = stage.ApprovalRoles.Select(r => r.ApprovalRole);
          if (roles.Any())
          {
            if (!string.IsNullOrEmpty(allApprovers))
              allApprovers += separator;
            allApprovers += string.Join(separator, roles.Select(r => string.Format("<b>{0}</b>", r.Name)));
          }
        }
      }
      
      return allApprovers;
    }
    
    /// <summary>
    /// Получить подписывающих по параметрам.
    /// </summary>
    /// <param name="rule">Правило согласования.</param>
    /// <param name="transitions">Переходы.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="kind">Вид документа.</param>
    /// <param name="minAmount">Ограничение по сумме.</param>
    /// <param name="departments">Подразделения.</param>
    /// <returns>Подписывающие.</returns>
    /// <Remarks>Не используется.</Remarks>
    [Obsolete]
    public static List<Docflow.Structures.SignatureSetting.SignatoriesList> GetSignatoriesByRule(IApprovalRuleBase rule, List<int> transitions, IBusinessUnit businessUnit, IDocumentKind kind, double? minAmount, List<Company.IDepartment> departments)
    {
      var signatories = new List<Docflow.Structures.SignatureSetting.SignatoriesList>();
      if (rule == null)
        return signatories;
      
      var signatoryStage = rule.Stages
        .Where(s => transitions.Contains(s.Number.Value))
        .Select(r => r.Stage)
        .FirstOrDefault(s => s.StageType == Sungero.Docflow.ApprovalStage.StageType.Sign);
      var reviewStage = rule.Stages
        .Where(s => transitions.Contains(s.Number.Value))
        .Select(r => r.Stage)
        .FirstOrDefault(s => s.StageType == Sungero.Docflow.ApprovalStage.StageType.Review);
      if (signatoryStage == null && reviewStage == null)
        return signatories;
      
      var signatoriesByDepartment = new List<Docflow.Structures.SignatureSetting.SignatoryByDepartment>();
      var separator = string.Format(";{0}", Environment.NewLine);
      
      if (signatoryStage != null)
      {
        var settings = Functions.ApprovalRuleBase.GetSignatureSetting(rule, businessUnit, kind, minAmount, departments);
        var addedSignatories = new List<IEmployee>();
        
        foreach (var setting in settings)
        {
          var additional = new List<IEmployee>();
          var signCondition = Docflow.Functions.SignatureSetting.GetSignSettingCondition(setting);
          if (Groups.Is(setting.Recipient))
            additional.AddRange(Groups.GetAllUsersInGroup(Groups.As(setting.Recipient)).Select(r => Employees.As(r)).Where(e => e != null));
          else if (Employees.Is(setting.Recipient))
            additional.Add(Employees.As(setting.Recipient));
          
          additional = additional.Distinct().Except(addedSignatories).ToList();
          if (setting.Departments.Any())
          {
            foreach (var department in setting.Departments.Where(s => departments.Any(d => Equals(d, s.Department))).Select(d => d.Department))
            {
              foreach (var employee in additional)
              {
                var signatoryByDepartment = Docflow.Structures.SignatureSetting.SignatoryByDepartment.Create();
                signatoryByDepartment.Department = department;
                signatoryByDepartment.Employee = employee;
                signatoryByDepartment.Conditions = signCondition;
                signatoryByDepartment.Priority = setting.Priority.Value;
                signatoriesByDepartment.Add(signatoryByDepartment);
              }
            }
          }
          else
          {
            foreach (var employee in additional)
            {
              var signatoryByDepartment = Docflow.Structures.SignatureSetting.SignatoryByDepartment.Create();
              signatoryByDepartment.Employee = employee;
              signatoryByDepartment.Conditions = signCondition;
              signatoriesByDepartment.Add(signatoryByDepartment);
              signatoryByDepartment.Priority = setting.Priority.Value;
            }
          }
        }
        if (!signatoriesByDepartment.Any())
        {
          var signatoryList = Docflow.Structures.SignatureSetting.SignatoriesList.Create();
          signatoryList.Employees = ApprovalRuleBases.Resources.SignatureSettingsNotSet;
          signatories.Add(signatoryList);
        }

        foreach (var department in signatoriesByDepartment.Select(s => s.Department).Distinct())
        {
          var signatoryList = Docflow.Structures.SignatureSetting.SignatoriesList.Create();
          signatoryList.Department = department;
          
          // Подписывающие без условий.
          var employeesWithoutCondition = signatoriesByDepartment.Where(s => (Equals(s.Department, department) || s.Department == null) && string.IsNullOrEmpty(s.Conditions))
            .OrderByDescending(e => e.Priority)
            .Select(s => s.Employee)
            .Distinct();
          
          // Подписывающие с условиями.
          var employeesWithCondition = signatoriesByDepartment
            .Where(s => Equals(s.Department, department) && !string.IsNullOrEmpty(s.Conditions) && !employeesWithoutCondition.Any(e => Equals(e, s.Employee)))
            .Distinct();
          
          foreach (var condition in employeesWithCondition.Select(e => e.Conditions).Distinct())
          {
            var employees = employeesWithCondition.Where(e => e.Conditions == condition).OrderByDescending(e => e.Priority).Distinct();
            signatoryList.Employees += string.Join(", ", employees.Select(e => Company.PublicFunctions.Employee.GetJobTitleWithShortName(e.Employee)));
            signatoryList.Employees += condition + separator;
          }
          signatoryList.Employees += string.Join(", ", employeesWithoutCondition.Select(a => Company.PublicFunctions.Employee.GetJobTitleWithShortName(a)));
          signatories.Add(signatoryList);
        }
      }
      if (reviewStage != null)
      {
        var signatoryList = signatories.Where(s => s.Department == null).FirstOrDefault();
        if (signatoryList == null)
        {
          signatoryList = Docflow.Structures.SignatureSetting.SignatoriesList.Create();
          signatoryList.Employees += ApprovalRuleBases.Resources.Addressee;
          signatories.Add(signatoryList);
        }
        else
          signatoryList.Employees += ApprovalRuleBases.Resources.Addressee;
      }
      if (!signatories.Any())
      {
        var signatoryList = Docflow.Structures.SignatureSetting.SignatoriesList.Create();
        signatoryList.Employees += ApprovalRuleBases.Resources.SignatureSettingsNotSet;
        signatories.Add(signatoryList);
      }
      
      return signatories;
    }
    
    /// <summary>
    /// Получить права подписи.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="kind">Вид документа.</param>
    /// <param name="minAmount">Ограничение по сумме.</param>
    /// <param name="departments">Подразделения.</param>
    /// <returns>Права подписи.</returns>
    /// <Remarks>Не используется.</Remarks>
    [Public, Obsolete]
    public virtual IQueryable<ISignatureSetting> GetSignatureSetting(IBusinessUnit businessUnit, IDocumentKind kind, double? minAmount, List<Company.IDepartment> departments)
    {
      return this.GetSignatureSettingWithoutDocumentFlowFilter(businessUnit, kind, minAmount, departments)
        .Where(s => s.DocumentFlow == _obj.DocumentFlow || s.DocumentFlow == Docflow.SignatureSetting.DocumentFlow.All)
        .OrderByDescending(s => s.Limit);
    }
    
    /// <summary>
    /// Получить права подписи без ограничения по документопотоку.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="kind">Вид документа.</param>
    /// <param name="minAmount">Ограничение по сумме.</param>
    /// <param name="departments">Подразделения.</param>
    /// <returns>Права подписи.</returns>
    public virtual IQueryable<ISignatureSetting> GetSignatureSettingWithoutDocumentFlowFilter(IBusinessUnit businessUnit, IDocumentKind kind, double? minAmount, List<Company.IDepartment> departments)
    {
      var businessUnits = new List<IBusinessUnit>() { businessUnit };
      var kinds = new List<IDocumentKind>() { kind };
      var signatureSettingsWithoutDepartmentsFilter = Functions.SignatureSetting.GetSignatureSettings(businessUnits, kinds)
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || s.Amount > minAmount);
      
      // Оставить только те права подписи, которые подходят под список отделов (в праве подписи нет ограничения по отделу, либо список отделов полностью входит в список отделов, указанный в праве подписи).
      var signatureSettings = signatureSettingsWithoutDepartmentsFilter.ToList();
      foreach (var setting in signatureSettingsWithoutDepartmentsFilter)
      {
        var signatureSettingDepartments = setting.Departments.Select(sd => sd.Department).ToList();
        
        // Если в праве подписи указан список отделов, но список отделов для фильтрации пустой, то право подписи не подходит.
        if (!departments.Any() && signatureSettingDepartments.Any())
        {
          signatureSettings.Remove(setting);
          continue;
        }
      }
      
      return signatureSettings.AsQueryable();
    }
    
    #endregion
    
    #region Проверка версий правила
    
    /// <summary>
    /// Проверить версию правила на уникальность.
    /// </summary>
    /// <returns>True - уникальна, False - не уникальна.</returns>
    public bool IsVersionUnique()
    {
      // Если parentRule == null, то является новым правилом - уникальна.
      if (_obj.ParentRule == null)
        return true;
      
      // Ищем другие уже сохраненные версии родительского правила.
      var otherVersions = ApprovalRuleBases.GetAll(r => r.ParentRule.Equals(_obj.ParentRule) && !r.Equals(_obj));
      
      // Если таких нет, значит, это единственная версия правила на данный момент - уникальна.
      return !otherVersions.Any();
    }
    
    #endregion
    
    /// <summary>
    /// Проверить два условия на непротиворечивость.
    /// </summary>
    /// <param name="conditionTypeItems">Дочерняя коллекция проверяемого условия.</param>
    /// <param name="previousConditionTypeItems">Дочерняя коллекция предыдущего условия.</param>
    /// <param name="condition">Проверяемое условие.</param>
    /// <param name="previousCondition">Предыдущее условие.</param>
    /// <returns>True, если гарантированно достижимо, False, если гарантированно недостижимо, Null, если требуется следующий цикл проверки.</returns>
    protected static bool? CheckConsistencyConditions(IEnumerable<IEntity> conditionTypeItems,
                                                      IEnumerable<IEntity> previousConditionTypeItems,
                                                      Structures.ApprovalRuleBase.RouteStep condition,
                                                      Structures.ApprovalRuleBase.RouteStep previousCondition)
    {
      var currentExceptPrevious = conditionTypeItems.Except(previousConditionTypeItems);
      var previousExceptCurrent = previousConditionTypeItems.Except(conditionTypeItems);
      
      // Если условия идентичные, возможна только одинаковая ветка.
      if (!currentExceptPrevious.Any() && !previousExceptCurrent.Any())
        return previousCondition.Branch == condition.Branch;

      // Если текущее условие содержит все элементы предыдущего условия (и ветка Да), то возможная только ветка Да.
      if (!previousExceptCurrent.Any() && currentExceptPrevious.Count() < conditionTypeItems.Count() && previousCondition.Branch)
        return condition.Branch;

      // Если условия не пересекаются (и ветка Да), возможна только Нет-ветка.
      if (currentExceptPrevious.Count() == conditionTypeItems.Count() && previousExceptCurrent.Count() == previousConditionTypeItems.Count() && previousCondition.Branch)
        return !condition.Branch;

      // Если предыдущее условие содержит все элементы текущего (и ветка Нет), то возможна только ветка Нет.
      if (!currentExceptPrevious.Any() && previousExceptCurrent.Count() < previousConditionTypeItems.Count() && !previousCondition.Branch)
        return !condition.Branch;
      
      return null;
    }
    
    /// <summary>
    /// Получить действующие этапы согласования.
    /// </summary>
    /// <param name="stageType">Тип этапа, который нужно получить.</param>
    /// <returns>Этапы согласования.</returns>
    [Remote]
    public static IQueryable<IApprovalStage> ChartSelectStage(Enumeration? stageType)
    {
      return ApprovalStages.GetAll()
        .Where(s => s.StageType == stageType)
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
    }
    
    /// <summary>
    /// Получить действующие этапы согласования с выполнением сценария.
    /// </summary>
    /// <returns>Этапы согласования с выполнением сценария.</returns>
    [Remote]
    public static IQueryable<IApprovalFunctionStageBase> ChartSelectFunctionStageBase()
    {
      return ApprovalFunctionStageBases.GetAll()
        .Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
    }
    
    /// <summary>
    /// Получить все версии правила.
    /// </summary>
    /// <param name="rule">Правило согласования.</param>
    /// <returns>Список версий правила согласования, который включает и текущее правило (если оно уже было сохранено в БД).</returns>
    [Remote]
    public static List<IApprovalRuleBase> GetAllRuleVersions(IApprovalRuleBase rule)
    {
      var rules = new List<IApprovalRuleBase>();
      
      #region Поиск предыдущих версий
      {
        // Сущность по ссылке уже может быть невалидна, ее нужно переполучить из кеша.
        var prevRule = ApprovalRuleBases.GetAll(x => Equals(x, rule.ParentRule)).FirstOrDefault();
        
        while (prevRule != null)
        {
          rules.Add(prevRule);
          prevRule = ApprovalRuleBases.GetAll(x => Equals(x, prevRule.ParentRule)).FirstOrDefault();
        }
      }
      #endregion
      
      #region Поиск следующих версий
      {
        var nextRule = ApprovalRuleBases.GetAll().Where(x => Equals(x.ParentRule, rule)).FirstOrDefault();
        
        while (nextRule != null)
        {
          rules.Add(nextRule);
          nextRule = ApprovalRuleBases.GetAll().Where(x => Equals(x.ParentRule, nextRule)).FirstOrDefault();
        }
      }
      #endregion
      
      // Добавляем текущее правило, если оно уже сохранено.
      if (!rule.State.IsInserted)
        rules.Add(rule);
      
      // Добавляем сохраненную версию правила, если она появилась в период между созданием и сохранением текущей.
      if (rule.ParentRule != null)
      {
        var otherVersion = ApprovalRuleBases.GetAll(r => Equals(r.ParentRule, rule.ParentRule) && !Equals(r, rule)).FirstOrDefault();
        if (otherVersion != null)
          rules.Add(otherVersion);
      }
      
      return rules;
    }
    
    /// <summary>
    /// Найти предыдущую действующую версию правила.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <returns>Предыдущая действующая версия правила или null.</returns>
    [Remote]
    public static IApprovalRuleBase GetPreviousActiveRule(IApprovalRuleBase rule)
    {
      // Сущность по ссылке уже может быть невалидна и ее нужно переполучить из кеша.
      var prevRule = ApprovalRuleBases.GetAll(x => Equals(x, rule.ParentRule)).FirstOrDefault();
      
      while (prevRule != null && prevRule.Status != Docflow.ApprovalRuleBase.Status.Active)
        prevRule = ApprovalRuleBases.GetAll(x => Equals(x, prevRule.ParentRule)).FirstOrDefault();
      
      return prevRule;
    }

    /// <summary>
    /// Получить новую версию правила согласования.
    /// </summary>
    /// <returns>Правило согласования.</returns>
    [Remote]
    public virtual IApprovalRuleBase GetNextVersion()
    {
      return ApprovalRuleBases.GetAll(r => Equals(r.ParentRule, _obj)).FirstOrDefault();
    }
    
    /// <summary>
    /// Определить этапы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Отсортированный список этапов, подходящих по условиям.</returns>
    [Remote(PackResultEntityEagerly = true, IsPure = true)]
    public virtual Structures.Module.DefinedApprovalStages GetStages(IOfficialDocument document, IApprovalTask task)
    {
      var baseStages = Functions.ApprovalRuleBase.GetBaseStages(_obj, document, task);
      return Functions.ApprovalRuleBase.CastToDefinedApprovalStages(baseStages);
    }
    
    /// <summary>
    /// Определить базовые этапы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Отсортированный список базовых этапов, подходящих по условиям.</returns>
    [Remote(PackResultEntityEagerly = true, IsPure = true)]
    public virtual Structures.Module.DefinedApprovalBaseStages GetBaseStages(IOfficialDocument document, IApprovalTask task)
    {
      var stages = new List<Structures.Module.DefinedApprovalBaseStageLite>() { };
      bool canDefineConditions = false;
      
      // Если не вложен документ.
      if (document == null)
        return Structures.Module.DefinedApprovalBaseStages.Create(stages, canDefineConditions, string.Empty);
      
      if (_obj.Stages.Count() == 1 && !_obj.Transitions.Any())
        return Structures.Module.DefinedApprovalBaseStages.Create(_obj.Stages.Select(s => Structures.Module.DefinedApprovalBaseStageLite.Create(s.StageBase, s.Number, s.StageType)).ToList(), true, string.Empty);

      // Вычисление первого этапа.
      var currentStageNumber = _obj.Transitions.Select(x => x.SourceStage).FirstOrDefault(s => !_obj.Transitions.Any(t => t.TargetStage.Equals(s)));
      
      if (currentStageNumber == null || _obj == null)
        return Structures.Module.DefinedApprovalBaseStages.Create(stages, canDefineConditions, string.Empty);
      
      if (_obj.Stages.Any(x => x.Number == currentStageNumber))
      {
        var stage = _obj.Stages.Single(s => s.Number == currentStageNumber);
        stages.Add(Structures.Module.DefinedApprovalBaseStageLite.Create(stage.StageBase, stage.Number, stage.StageType));
        canDefineConditions = true;
      }
      
      var nextStageNumber = Functions.ApprovalRuleBase.GetNextStageNumber(_obj, document, currentStageNumber, task);
      var error = nextStageNumber.Message;
      
      if (nextStageNumber != null)
        canDefineConditions = nextStageNumber.Number != -1;
      
      while (nextStageNumber.Number != null && nextStageNumber.Number >= 0)
      {
        currentStageNumber = nextStageNumber.Number.Value;
        nextStageNumber = Functions.ApprovalRuleBase.GetNextStageNumber(_obj, document, currentStageNumber, task);
        if (nextStageNumber.Number == -1)
        {
          error = nextStageNumber.Message;
          canDefineConditions = false;
          break;
        }
        
        var stage = _obj.Stages.Single(s => s.Number == currentStageNumber);
        stages.Add(Structures.Module.DefinedApprovalBaseStageLite.Create(stage.StageBase, stage.Number, stage.StageType));
      }
      
      return Structures.Module.DefinedApprovalBaseStages.Create(stages, canDefineConditions, error);
    }
    
    /// <summary>
    /// Получить следующий этап в правиле.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="currentStage">Текущий этап.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Следующий этап.</returns>
    [Remote]
    public Structures.Module.DefinedApprovalStageLite GetNextStage(IOfficialDocument document, Structures.Module.DefinedApprovalStageLite currentStage, IApprovalTask task)
    {
      var stages = this.GetStages(document, task).Stages;
      var currentStageIndex = stages.IndexOf(currentStage);
      if (currentStageIndex < 0)
        return null;
      
      var nexStage = stages.FirstOrDefault(s => stages.IndexOf(s) > currentStageIndex);
      return nexStage;
    }
    
    /// <summary>
    /// Проверить валидность этапов.
    /// </summary>
    /// <param name="stagesVariants">Список всех возможных последовательностей блоков.</param>
    /// <param name="e">Аргументы события До сохранения.</param>
    public virtual void CheckRuleStages(Structures.ApprovalRuleBase.StagesVariants stagesVariants, Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (stagesVariants.UnreachebleSteps.Any())
      {
        foreach (var stage in _obj.Stages.Where(s => stagesVariants.UnreachebleSteps.Any(us => us == s.Number)))
          e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.UnreachableRoutesExists);
        
        foreach (var condition in _obj.Conditions.Where(s => stagesVariants.UnreachebleSteps.Any(us => us == s.Number)))
          e.AddError(condition, ApprovalRuleBases.Info.Properties.Conditions.Properties.Condition, ApprovalRuleBases.Resources.UnreachableRoutesExists);
      }
      
      foreach (var stepsNumbers in stagesVariants.AllSteps)
      {
        foreach (var stage in Functions.ApprovalRuleBase.CheckImpossibleRoles(_obj, stepsNumbers, true, new List<Enumeration?>()))
          e.AddError(stage.Stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, stage.Message);
        
        var stagesSequence = new List<IApprovalRuleBaseStages>() { };
        
        foreach (var number in stepsNumbers)
        {
          var stage = _obj.Stages.FirstOrDefault(s => s.Number == number);
          if (stage != null)
            stagesSequence.Add(stage);
        }
        
        // Проверить отсутствие дублирующихся этапов.
        var additionallyAppStages = stagesSequence.Where(st => st.StageType == StageType.Approvers && st.Stage.AllowAdditionalApprovers == true);
        var managerStages = stagesSequence.Where(st => st.StageType == StageType.Manager);
        var signStages = stagesSequence.Where(st => st.StageType == StageType.Sign);
        var registerStages = stagesSequence.Where(st => st.StageType == StageType.Register);
        var reviewStages = stagesSequence.Where(st => st.StageType == StageType.Review);
        var executionStages = stagesSequence.Where(st => st.StageType == StageType.Execution);
        
        if (additionallyAppStages.Count() > 1)
          foreach (var stage in additionallyAppStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManyAdditionallyApp);
        if (managerStages.Count() > 1)
          foreach (var stage in managerStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManyManager);
        if (signStages.Count() > 1)
          foreach (var stage in signStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManySign);
        if (registerStages.Count() > 1)
          foreach (var stage in registerStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManyRegister);
        if (reviewStages.Count() > 1)
          foreach (var stage in reviewStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManyReview);
        if (executionStages.Count() > 1)
          foreach (var stage in executionStages)
            e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ManyExecution);
        
        // Проверить порядок этапов.
        var approvingMaxStage = stagesSequence
          .Where(s => s.StageType == StageType.Manager ||
                 s.StageType == StageType.Approvers)
          .LastOrDefault();
        var approvingMaxStageNumber = stagesSequence.LastIndexOf(approvingMaxStage);
        var signMinStage = stagesSequence
          .Where(s => s.StageType == StageType.Sign)
          .FirstOrDefault();
        var signMinStageNumber = stagesSequence.IndexOf(signMinStage);
        
        if (signMinStageNumber > -1 && approvingMaxStageNumber > signMinStageNumber)
          e.AddError(approvingMaxStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ApproveBeforeSign);
        
        // Проверить, что этап создания поручений идет после рассмотрения или подписания, при его наличии.
        var reviewStage = stagesSequence
          .Where(s => s.StageType == Sungero.Docflow.ApprovalRuleBaseStages.StageType.Review)
          .LastOrDefault();
        var reviewStageIndex = stagesSequence.IndexOf(reviewStage);
        var signStage = stagesSequence
          .Where(s => s.StageType == Sungero.Docflow.ApprovalRuleBaseStages.StageType.Sign)
          .LastOrDefault();
        var signStageIndex = stagesSequence.IndexOf(signStage);
        var executionStage = stagesSequence
          .Where(s => s.StageType == Sungero.Docflow.ApprovalRuleBaseStages.StageType.Execution)
          .FirstOrDefault();
        var executionStageIndex = stagesSequence.IndexOf(executionStage);
        
        if (executionStageIndex > -1 && reviewStageIndex < 0 && signStageIndex < 0)
          e.AddError(executionStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ExecutionStageWithoutReviewOrSign);
        
        // Этап создания поручений должен быть после "последнего" этапа подписания или рассмотрения.
        if (executionStageIndex > -1 && (executionStageIndex < signStageIndex && executionStageIndex < reviewStageIndex))
          e.AddError(executionStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ReviewAndSignStageShouldBeBeforeExecution);
        else if (executionStageIndex > -1 && (executionStageIndex < signStageIndex && executionStageIndex > reviewStageIndex))
          e.AddError(signStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.SignStageShouldBeBeforeExecution);
        else if (executionStageIndex > -1 && (executionStageIndex < reviewStageIndex && executionStageIndex > signStageIndex))
          e.AddError(reviewStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ReviewStageShouldBeBeforeExecution);
        
        // Этап рассмотрения должен быть после "последнего" этапа подписания.
        reviewStage = stagesSequence
          .Where(s => s.StageType == Sungero.Docflow.ApprovalRuleBaseStages.StageType.Review)
          .FirstOrDefault();
        reviewStageIndex = stagesSequence.IndexOf(reviewStage);
        
        if (reviewStageIndex > -1 && signStageIndex > reviewStageIndex)
          e.AddError(signStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.SignStageShouldBeBeforeReview);
        
        // Валидация этапов.
        foreach (var stage in stagesSequence)
          Functions.ApprovalStageBase.Validate(stage.StageBase, _obj, stagesSequence, stage, e);
      }
      
      if (Functions.ApprovalRuleBase.GetDoubleRules(_obj).Any())
        e.AddWarning(ApprovalRuleBases.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicate);
    }
    
    /// <summary>
    /// Получить роли согласования, допустимые в качестве ответственных за доработку.
    /// </summary>
    /// <returns>Список ролей согласования.</returns>
    public virtual List<IApprovalRoleBase> GetSupportedApprovalRolesForRework()
    {
      return ApprovalRoleBases.GetAll().Where(r => r.Type != Docflow.ApprovalRole.Type.Initiator &&
                                              r.Type != Docflow.ApprovalRole.Type.Approvers &&
                                              r.Type != Docflow.ApprovalRole.Type.ContractResp &&
                                              r.Type != Docflow.ApprovalRole.Type.ContRespManager &&
                                              r.Type != Docflow.ApprovalRole.Type.Addressee &&
                                              r.Type != Docflow.ApprovalRole.Type.AddrAssistant &&
                                              r.Type != Docflow.ApprovalRole.Type.Signatory &&
                                              r.Type != Docflow.ApprovalRole.Type.SignAssistant &&
                                              r.Type != Docflow.ApprovalRole.Type.Addressees)
        .ToList();
    }
    
    /// <summary>
    /// Получить все условия в маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="conditionType">Тип условия.</param>
    /// <returns>Условия.</returns>
    public List<Structures.ApprovalRuleBase.RouteStep> GetConditionsInRoute(List<Structures.ApprovalRuleBase.RouteStep> route, Enumeration conditionType)
    {
      return route.Where(e => _obj.Conditions.Any(c => Equals(c.Number, e.StepNumber) && c.Condition.ConditionType == conditionType))
        .ToList();
    }
  }
}
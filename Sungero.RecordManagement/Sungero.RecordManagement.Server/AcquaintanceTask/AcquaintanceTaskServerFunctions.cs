using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;
using Sungero.RecordManagement.Structures.AcquaintanceTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class AcquaintanceTaskFunctions
  {
    #region Предметное отображение
    
    /// <summary>
    /// Построить модель состояния процесса ознакомления.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Схема модели состояния.</returns>
    public Sungero.Core.StateView GetStateView(IElectronicDocument document)
    {
      if (_obj.DocumentGroup.OfficialDocuments.Any(d => Equals(document, d)) ||
          _obj.AddendaGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить предметное отображение ознакомления.
    /// </summary>
    /// <returns>Предметное отображение ознакомления.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      
      // Блок "От кого".
      var taskBeginText = _obj.Texts.OrderByDescending(t => t.Created).FirstOrDefault();
      var comment = taskBeginText != null ? taskBeginText.Body : string.Empty;
      comment = comment != AcquaintanceTasks.Resources.TaskAutoText ? comment : string.Empty;
      if (_obj.Started.HasValue)
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, AcquaintanceTasks.Resources.StateViewDocumentSendFromAcquaintance, _obj.Started.Value, _obj, comment, _obj.StartedBy);
      else
        Docflow.PublicFunctions.OfficialDocument
          .AddUserActionBlock(stateView, _obj.Author, Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated, _obj.Created.Value, _obj, comment, _obj.Author);
      
      // Блок "Задача".
      var taskBlock = this.AddTaskBlock(stateView);
      
      // "Ознакомление в электронном виде".
      var assignments = AcquaintanceAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= _obj.Started)
        .ToList();
      var acquaintedAssignments = assignments
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => Equals(a.CompletedBy, a.Performer))
        .ToList();
      var attentionAssignments = assignments
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => !Equals(a.CompletedBy, a.Performer))
        .ToList();
      
      // "Ознакомление под собственноручную подпись".
      var isElectronicAcquaintance = _obj.IsElectronicAcquaintance == true;
      if (!isElectronicAcquaintance)
      {
        // Задания, выполненные не лично, считаются также ознакомленными.
        acquaintedAssignments = assignments
          .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
          .ToList();
        
        // Заданий, требующих внимания, нет.
        attentionAssignments = new List<IAcquaintanceAssignment>();
      }
      
      // Блок "Ознакомленные".
      if (acquaintedAssignments.Count > 1)
        this.AddSelfCompletedAssignmentsBlocks(taskBlock, acquaintedAssignments, isElectronicAcquaintance);
      else
        this.AddAssignmentBlock(taskBlock,
                                acquaintedAssignments.FirstOrDefault(),
                                isElectronicAcquaintance,
                                StateBlockIconSize.Large,
                                AcquaintanceTasks.Resources.StateViewAsquaintance);
      
      // Блок "Задания, требующие внимания".
      foreach (var assignment in attentionAssignments)
        this.AddAssignmentBlock(taskBlock,
                                assignment,
                                isElectronicAcquaintance,
                                StateBlockIconSize.Large,
                                AcquaintanceTasks.Resources.StateViewAsquaintance);
      
      // Блок "Задания в работе".
      var inProcessAssignments = assignments
        .Where(a => a.Status != Workflow.AssignmentBase.Status.Completed && a.Status != Workflow.AssignmentBase.Status.Aborted)
        .ToList();
      this.AddInProcessAssignmentsBlock(taskBlock, inProcessAssignments);
      
      // Блок "Прекращенные задания".
      if (_obj.Status == Workflow.Task.Status.Aborted)
      {
        var abortedAssignments = assignments
          .Where(a => a.Status == Workflow.AssignmentBase.Status.Aborted)
          .ToList();
        this.AddInProcessAssignmentsBlock(taskBlock, abortedAssignments);
      }
      
      // Блок "Задание-контроль".
      var finishAssignments = AcquaintanceFinishAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= _obj.Started)
        .ToList();
      foreach (var assignment in finishAssignments)
        this.AddAssignmentBlock(taskBlock,
                                assignment,
                                isElectronicAcquaintance,
                                StateBlockIconSize.Large,
                                AcquaintanceTasks.Resources.StateViewFinishAssignment);
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить блок задачи на ознакомление.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock AddTaskBlock(Sungero.Core.StateView stateView)
    {
      // Стили.
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var noteStyle = Docflow.PublicFunctions.Module.CreateNoteStyle(isDraft);
      
      // Блок задачи.
      var taskBlock = stateView.AddBlock();
      taskBlock.Entity = _obj;
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      
      // Иконка.
      taskBlock.AssignIcon(AcquaintanceTasks.Resources.AcquaintanceTaskIco, StateBlockIconSize.Large);
      
      // Заголовок.
      var header = AcquaintanceTasks.Resources.StateViewDocumentSelfSignAcquaintance;
      if (_obj.IsElectronicAcquaintance == true)
        header = AcquaintanceTasks.Resources.StateViewDocumentElectonicAcquaintance;
      taskBlock.AddLabel(header, headerStyle);
      taskBlock.AddLineBreak();
      
      // Срок.
      var deadline = _obj.Deadline.HasValue ?
        Docflow.PublicFunctions.Module.ToShortDateShortTime(_obj.Deadline.Value.ToUserTime()) :
        Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;
      taskBlock.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadline), noteStyle);
      
      // Статус.
      var status = string.Empty;
      if (_obj.Status == Workflow.Task.Status.InProcess)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewInProcess;
        var onReview = AcquaintanceFinishAssignments.GetAll()
          .Where(a => Equals(a.Task, _obj))
          .Where(a => a.Created >= _obj.Started)
          .Any();
        if (onReview)
          status = AcquaintanceTasks.Resources.StateViewFinish;
      }
      else if (_obj.Status == Workflow.Task.Status.Completed)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewCompleted;
      }
      else if (_obj.Status == Workflow.Task.Status.Aborted)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewAborted;
      }
      else if (_obj.Status == Workflow.Task.Status.Suspended)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewSuspended;
      }
      else if (_obj.Status == Workflow.Task.Status.Draft)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewDraft;
      }
      
      Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить задания на ознакомление.
    /// </summary>
    /// <param name="taskBlock">Блок задачи.</param>
    /// <param name="assignments">Лично выполненные задания на ознакомление.</param>
    /// <param name="isElectronicAcquaintance">Признак электронного ознакомления.</param>
    public void AddSelfCompletedAssignmentsBlocks(Sungero.Core.StateBlock taskBlock,
                                                  List<IAcquaintanceAssignment> assignments,
                                                  bool isElectronicAcquaintance)
    {
      if (!assignments.Any())
        return;
      
      // Группировка.
      var parentBlock = taskBlock.AddChildBlock();
      parentBlock.IsExpanded = false;
      parentBlock.NeedGroupChildren = true;
      
      // Иконка.
      parentBlock.AssignIcon(Docflow.ApprovalTasks.Resources.Completed, StateBlockIconSize.Large);
      
      // Заголовок.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(false);
      parentBlock.AddLabel(AcquaintanceTasks.Resources.StateViewAsquaintance, headerStyle);
      parentBlock.AddLineBreak();
      
      // Исполнители.
      var performers = assignments.Select(a => Sungero.Company.Employees.As(a.Performer)).ToList();
      var performersLabel = Docflow.PublicFunctions.OfficialDocument.GetPerformersInText(performers);
      parentBlock.AddLabel(performersLabel, Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
      
      // Статус.
      var status = AcquaintanceTasks.Resources.StateViewAcquainted;
      Docflow.PublicFunctions.Module.AddInfoToRightContent(parentBlock, status);
      
      // Задания.
      foreach (var assignment in assignments)
        this.AddAssignmentBlock(parentBlock, assignment, isElectronicAcquaintance, StateBlockIconSize.Small, string.Empty);
    }
    
    /// <summary>
    /// Добавить задания на ознакомление.
    /// </summary>
    /// <param name="parentBlock">Родительский блок.</param>
    /// <param name="assignment">Задание на ознакомление.</param>
    /// <param name="isElectronicAcquaintance">Признак электронного ознакомления.</param>
    /// <param name="iconSize">Размер иконки.</param>
    /// <param name="header">Заголовок.</param>
    public void AddAssignmentBlock(Sungero.Core.StateBlock parentBlock,
                                   IAssignment assignment,
                                   bool isElectronicAcquaintance,
                                   Sungero.Core.StateBlockIconSize iconSize,
                                   string header)
    {
      if (assignment == null)
        return;
      
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(false);
      var noteStyle = Docflow.PublicFunctions.Module.CreateNoteStyle();
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      
      // Сущность (ссылка).
      var block = parentBlock.AddChildBlock();
      block.Entity = assignment;
      
      // Иконка.
      block.AssignIcon(Docflow.ApprovalRuleBases.Resources.Assignment, iconSize);
      var selfCompleted = !isElectronicAcquaintance || Equals(assignment.Performer, assignment.CompletedBy);
      if (assignment.Result != null)
      {
        block.AssignIcon(Docflow.ApprovalTasks.Resources.Completed, iconSize);
        if (iconSize == StateBlockIconSize.Small)
          block.AssignIcon(StateBlockIconType.Completed, iconSize);
        
        if (!selfCompleted)
          block.AssignIcon(AcquaintanceTasks.Resources.SubstitutionAccept, iconSize);
      }
      
      // Заголовок.
      if (!string.IsNullOrWhiteSpace(header))
      {
        block.AddLabel(header, headerStyle);
        block.AddLineBreak();
      }
      
      // Исполнитель.
      var performerName = Docflow.PublicFunctions.OfficialDocument.GetAuthor(assignment.Performer, assignment.CompletedBy);
      block.AddLabel(performerName, performerDeadlineStyle);
      
      // Срок.
      var deadline = assignment.Deadline.HasValue
        ? Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime())
        : Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;
      deadline = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadline);
      if (assignment.Completed.HasValue)
      {
        var completed = Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Completed.Value.ToUserTime());
        deadline = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDate, completed);
      }
      block.AddLabel(deadline, performerDeadlineStyle);
      
      // Комментарий.
      var comment = Docflow.PublicFunctions.Module.GetAssignmentUserComment(assignment);
      if (!string.IsNullOrWhiteSpace(comment))
      {
        block.AddLineBreak();
        block.AddLabel(Docflow.PublicConstants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.PublicConstants.Module.EmptyLineMargin);
        block.AddLabel(comment, noteStyle);
      }
      
      // Статус.
      var status = string.Empty;
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewInProcess;
        if (assignment.IsRead != true)
          status = Docflow.ApprovalTasks.Resources.StateViewUnRead;
      }
      
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        status = Docflow.ApprovalTasks.Resources.StateViewAborted.ToString();
      }
      
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed)
      {
        status = AcquaintanceTasks.Resources.StateViewAcquainted;
        if (!selfCompleted || AcquaintanceFinishAssignments.Is(assignment))
          status = AcquaintanceTasks.Resources.StateViewCompleted;
      }
      
      Docflow.PublicFunctions.Module.AddInfoToRightContent(block, status);
      if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.Deadline.HasValue)
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
    }
    
    /// <summary>
    /// Добавить задания в работе.
    /// </summary>
    /// <param name="taskBlock">Блок задачи.</param>
    /// <param name="assignments">Задания на ознакомление в работе.</param>
    public void AddInProcessAssignmentsBlock(Sungero.Core.StateBlock taskBlock, List<IAcquaintanceAssignment> assignments)
    {
      if (!assignments.Any())
        return;
      
      // Группировка.
      var parentBlock = taskBlock.AddChildBlock();
      parentBlock.IsExpanded = false;
      parentBlock.NeedGroupChildren = true;
      
      // Добавить ссылку на задание, если оно одно.
      if (assignments.Count == 1)
        parentBlock.Entity = assignments.FirstOrDefault();
      
      // Иконка.
      var isAborted = assignments.First().Status == Workflow.AssignmentBase.Status.Aborted;
      parentBlock.AssignIcon(Docflow.ApprovalRuleBases.Resources.Assignment, StateBlockIconSize.Large);
      if (isAborted)
        parentBlock.AssignIcon(StateBlockIconType.Abort, StateBlockIconSize.Large);
      
      // Заголовок.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(false);
      parentBlock.AddLabel(AcquaintanceTasks.Resources.StateViewAsquaintance, headerStyle);
      parentBlock.AddLineBreak();
      
      // Исполнители.
      var performers = assignments.Select(a => Sungero.Company.Employees.As(a.Performer)).ToList();
      var performersLabel = Docflow.PublicFunctions.OfficialDocument.GetPerformersInText(performers);
      parentBlock.AddLabel(performersLabel, Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle());
      
      // Статус.
      var status = Docflow.ApprovalTasks.Resources.StateViewInProcess;
      if (isAborted)
        status = Docflow.ApprovalTasks.Resources.StateViewAborted;
      
      Docflow.PublicFunctions.Module.AddInfoToRightContent(parentBlock, status);
    }
    
    #endregion
    
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    [Remote(IsPure = true)]
    public virtual List<StartValidationMessage> GetStartValidationMessage()
    {
      var errors = new List<StartValidationMessage>();
      
      // Проверить наличие документа в задаче и наличие прав на него.
      if (!Functions.AcquaintanceTask.HasDocumentAndCanRead(_obj))
      {
        errors.Add(StartValidationMessage.Create(Docflow.Resources.NoRightsToDocument, false, false));
        return errors;
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Проверить существование тела документа.
      if (_obj.IsElectronicAcquaintance.Value && !document.HasVersions)
        errors.Add(StartValidationMessage.Create(AcquaintanceTasks.Resources.AcquaintanceTaskDocumentWithoutBodyMessage, false, false));
      
      // Валидация подписи документа.
      var validationMessages = document.HasVersions
        ? Functions.Module.GetDocumentSignatureValidationErrors(document.LastVersion, true)
        : new List<string>();
      if (validationMessages.Any())
      {
        validationMessages.Insert(0, RecordManagement.Resources.SignatureValidationErrorMessage);
        errors.Add(StartValidationMessage.Create(string.Join(Environment.NewLine, validationMessages), false, false));
      }
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Deadline, Calendar.Now))
        errors.Add(StartValidationMessage.Create(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, false, false));
      
      // Проверить наличие участников ознакомления.
      var employees = Functions.AcquaintanceTask.GetParticipants(_obj);
      if (employees.Count == 0)
        errors.Add(StartValidationMessage.Create(AcquaintanceTasks.Resources.PerformersCantBeEmpty, false, false));
      
      // Техническое ограничение платформы на запуск задачи для большого числа участников.
      var performersLimit = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsNumbericValue(Constants.AcquaintanceTask.PerformersLimitParamName);
      if (employees.Count > performersLimit)
        errors.Add(StartValidationMessage.Create(AcquaintanceTasks.Resources.TooManyPerformersFormat(performersLimit), false, false));
      
      // Запрещено отправлять ознакомления неавтоматизированным сотрудникам без замещения.
      var notAutomatedEmployees = Company.PublicFunctions.Module.Remote.GetNotAutomatedEmployees(employees);
      if (notAutomatedEmployees.Any())
        errors.Add(StartValidationMessage.Create(AcquaintanceTasks.Resources.NotAutomatedUserWithoutSubstitutionError, true, false));
      
      return errors;
    }
    
    /// <summary>
    /// Получить неавтоматизированных участников ознакомления.
    /// </summary>
    /// <returns>Результат выборки - неавтоматизированные участники ознакомления.</returns>
    [Remote(IsPure = true), Public]
    public virtual IQueryable<Company.IEmployee> GetNotAutomatedParticipants()
    {
      var participants = GetParticipants();
      var notAutomatedParticipants = Company.PublicFunctions.Module.Remote.GetNotAutomatedEmployees(participants);
      return notAutomatedParticipants;
    }
    
    /// <summary>
    /// Получить участников ознакомления.
    /// </summary>
    /// <returns>Участники ознакомления.</returns>
    [Remote(IsPure = true), Public]
    public virtual List<Company.IEmployee> GetParticipants()
    {
      var recipients = _obj.Performers.Select(x => x.Performer).ToList();
      var excludedRecipients = _obj.ExcludedPerformers.Select(x => x.ExcludedPerformer).ToList();
      return GetParticipants(recipients, excludedRecipients);
    }
    
    /// <summary>
    /// Получить участников ознакомления.
    /// </summary>
    /// <param name="recipients">Список исполнителей.</param>
    /// <param name="excludedRecipients">Список исключаемых исполнителей.</param>
    /// <returns>Участники ознакомления.</returns>
    public static List<Company.IEmployee> GetParticipants(List<IRecipient> recipients, List<IRecipient> excludedRecipients)
    {
      var performers = Company.PublicFunctions.Module.GetNotSystemEmployees(recipients);
      var excludedPerformers = Company.PublicFunctions.Module.GetNotSystemEmployees(excludedRecipients);
      
      return performers.Except(excludedPerformers).ToList();
    }
    
    /// <summary>
    /// Получить несистемные активные записи исполнителей.
    /// </summary>
    /// <param name="recipients">Список исполнителей.</param>
    /// <returns>Несистемные активные записи исполнителей.</returns>
    [Obsolete("Используйте метод Company.PublicFunctions.Module.GetNotSystemEmployees()")]
    public static List<Company.IEmployee> GetNonSystemActivePerformers(List<IRecipient> recipients)
    {
      return Company.PublicFunctions.Module.GetNotSystemEmployees(recipients);
    }
    
    /// <summary>
    /// Проверка чтения версии документа пользователем.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>True, если прочитано, иначе - false.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsDocumentVersionReaded(Docflow.IOfficialDocument document, int version)
    {
      return document.History.GetAll()
        .Where(x => x.VersionNumber == version &&
               Equals(Users.Current, x.User) &&
               x.Action == Sungero.CoreEntities.History.Action.Read &&
               x.Operation == Content.DocumentHistory.Operation.ReadVerBody).Any();
    }
    
    /// <summary>
    /// Валидация подписи версии документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Версия документа.</param>
    /// <returns>True, если подпись валидна или отсутствует, иначе - false.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsDocumentVersionSignatureValid(Docflow.IOfficialDocument document, int version)
    {
      var documentVersion = document.Versions.FirstOrDefault(x => x.Number == version);
      
      // Проверяем только утверждающую подпись. Не утверждено = отсутствие подписи.
      var signatures = Signatures.Get(documentVersion).Where(x => x.SignatureType == SignatureType.Approval);
      var hasAnySignature = signatures.Any();
      var hasAnyValidSignature = signatures.Any(x => x.IsValid && !x.ValidationErrors.Any());
      return !hasAnySignature || hasAnyValidSignature;
    }
    
    /// <summary>
    /// Получение версии документа.
    /// </summary>
    /// <returns> Версия документа, если документ без тела - 0.</returns>
    public int GetDocumentVersion()
    {
      // Вернуть номер версии только если у документа есть версии, и статус задачи не "Черновик", иначе - 0.
      var acquaintanceVersion = _obj.AcquaintanceVersions.FirstOrDefault(v => v.IsMainDocument == true);
      if (acquaintanceVersion != null &&
          (_obj.Status == Status.InProcess ||
           _obj.Status == Status.Suspended ||
           _obj.Status == Status.Completed ||
           _obj.Status == Status.Aborted))
        return acquaintanceVersion.Number.Value;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      return document.HasVersions ? document.LastVersion.Number.Value : 0;
    }
    
    /// <summary>
    /// Запомнить участников ознакомления.
    /// </summary>
    public void StoreAcquainters()
    {
      if (!_obj.Status.HasValue || _obj.Status.Value == Workflow.Task.Status.Aborted)
        return;
      
      var participants = AcquaintanceTaskParticipants.GetAll().FirstOrDefault(x => x.TaskId == _obj.Id);
      if (participants != null)
        AcquaintanceTaskParticipants.Delete(participants);
      
      participants = AcquaintanceTaskParticipants.Create();
      participants.TaskId = _obj.Id;
      
      var employees = Functions.AcquaintanceTask.GetParticipants(_obj);
      foreach (var employee in employees)
      {
        var participant = participants.Employees.AddNew();
        participant.Employee = employee;
      }
      participants.Save();
    }
    
    /// <summary>
    /// Получить активных исполнителей по ознакомлению.
    /// </summary>
    /// <returns>Исполнители.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<Company.IEmployee> GetAcquaintancePerformers()
    {
      var performers = AcquaintanceAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id)
        .Where(x => x.Status == Workflow.Assignment.Status.InProcess)
        .Select(x => Company.Employees.As(x.Performer));
      
      return performers.AsQueryable();
    }
    
    /// <summary>
    /// Получить задания на ознакомление для указанных исполнителей.
    /// </summary>
    /// <param name="performers">Исполнители.</param>
    /// <returns>Задания на ознакомление.</returns>
    [Remote(IsPure = true)]
    public virtual List<IAcquaintanceAssignment> GetAcquaintanceAssignments(List<Company.IEmployee> performers)
    {
      var assignments = AcquaintanceAssignments.GetAll()
        .Where(x => x.Task.Id == _obj.Id)
        .Where(x => x.Status == Workflow.Assignment.Status.InProcess)
        .Where(x => performers.Contains(Company.Employees.As(x.Performer)));
      
      return assignments.ToList();
    }
    
    /// <summary>
    /// Проверить, что созданы все задания на ознакомление.
    /// </summary>
    /// <returns>True/False.</returns>
    [Remote(IsPure = true)]
    public virtual bool AllAcquaintanceAssignmentsCreated()
    {
      var storedParticipants = AcquaintanceTaskParticipants.GetAll().FirstOrDefault(x => x.TaskId == _obj.Id);
      if (storedParticipants == null)
        return false;
      
      var taskAcquainters = storedParticipants.Employees.Select(p => p.Employee);
      if (!taskAcquainters.Any())
        return false;
      
      var acquaintanceAssignmentsPerformers = AcquaintanceAssignments.GetAll()
        .Where(x => Equals(x.Task, _obj))
        .Select(x => x.Performer);
      
      var performersWithoutAssignments = taskAcquainters.Except(acquaintanceAssignmentsPerformers);
      return !performersWithoutAssignments.Any();
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Связать с основным документом документы из группы Приложения, если они не были связаны ранее.
    /// </summary>
    public virtual void RelateAddedAddendaToPrimaryDocument()
    {
      var primaryDocument = _obj.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (primaryDocument == null)
        return;
      
      Logger.DebugFormat("AcquaintanceTask (ID = {0}). Add relation with type Addendum to primary document (ID = {1})",
                         _obj.Id, primaryDocument.Id);
      var taskAddenda = _obj.AddendaGroup.OfficialDocuments
        .Where(x => !Equals(x, primaryDocument))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x))
        .ToList();
      Docflow.PublicFunctions.OfficialDocument.RelateDocumentsToPrimaryDocumentAsAddenda(primaryDocument, taskAddenda);
    }
    
    #endregion
  }
}
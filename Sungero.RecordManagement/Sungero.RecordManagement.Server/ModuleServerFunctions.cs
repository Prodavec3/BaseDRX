using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;
using Init = Sungero.RecordManagement.Constants.Module.Initialize;

namespace Sungero.RecordManagement.Server
{

  public class ModuleFunctions
  {
    #region Виджеты
    
    #region Виджет "Поручения"

    /// <summary>
    /// Выбрать поручения для виджета.
    /// </summary>
    /// <param name="onlyOverdue">Только просроченные.</param>
    /// <param name="substitution">Включать замещающих.</param>
    /// <returns>Список поручений.</returns>
    public IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> GetActionItemsToWidgets(bool onlyOverdue, bool substitution)
    {
      var users = substitution ? Substitutions.ActiveSubstitutedUsersWithoutSystem.ToList() : new List<IUser>();
      users.Add(Users.Current);
      var usersIds = users.Select(u => u.Id).ToList();

      return this.GetActionItemsUnderControl(usersIds, onlyOverdue);
    }
    
    /// <summary>
    /// Выбрать поручения, которые нужно проконтролировать.
    /// </summary>
    /// <param name="usersIds">Список Ид сотрудников.</param>
    /// <param name="onlyOverdue">Только просроченные.</param>
    /// <returns>Список поручений.</returns>
    [Public]
    public virtual IQueryable<IActionItemExecutionTask> GetActionItemsUnderControl(List<int> usersIds, bool onlyOverdue)
    {
      var tasks = ActionItemExecutionTasks.GetAll()
        .Where(t => t.Status == Workflow.AssignmentBase.Status.InProcess);

      if (onlyOverdue)
        tasks = tasks.Where(t => t.Deadline.HasValue &&
                            (!t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.UserToday ||
                             t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.Now));

      return tasks.Where(a => a.Supervisor != null && usersIds.Contains(a.Supervisor.Id));
    }

    #endregion

    #region "Динамика исполнения поручений в срок"

    /// <summary>
    /// Получить статистику по исполнению поручений.
    /// </summary>
    /// <param name="performer">Исполнитель, указанный в параметрах виджета.</param>
    /// <returns>Строка с результатом.</returns>
    public List<Structures.Module.ActionItemStatistic> GetActionItemCompletionStatisticForChart(Enumeration performer)
    {
      var periodBegin = Calendar.UserToday.AddMonths(-2).BeginningOfMonth();
      var periodEnd = Calendar.UserToday.EndOfMonth();
      
      var hasData = false;

      var author = Employees.Null;
      if (performer == RecordManagement.Widgets.ActionItemCompletionGraph.Performer.Author)
        author = Company.Employees.Current;

      var statistic = new List<Structures.Module.ActionItemStatistic>();

      var actionItems = Functions.Module.GetActionItemCompletionData(null, null, periodBegin, periodEnd, author, null, null, null, null, false, false);
      while (periodBegin <= Calendar.UserToday)
      {
        periodEnd = periodBegin.EndOfMonth();
        var currentStatistic = this.CalculateActionItemStatistic(actionItems, periodBegin, periodEnd);
        
        if (currentStatistic != null)
          hasData = true;
        
        statistic.Add(Structures.Module.ActionItemStatistic.Create(currentStatistic, periodBegin));
        
        periodBegin = periodBegin.AddMonths(1);
      }
      
      return hasData ? statistic : new List<Structures.Module.ActionItemStatistic>();
    }

    /// <summary>
    /// Получить статистику по исполнению поручений за месяц.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <returns>Статистика за период.</returns>
    private int? CalculateActionItemStatistic(List<Structures.Module.LightActiomItem> actionItems, DateTime beginDate, DateTime endDate)
    {
      var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverEndDate = endDate.EndOfDay().FromUserTime();
      actionItems = actionItems.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                                      (Calendar.Between(t.ActualDate.Value.Date, beginDate.Date, endDate.Date) ||
                                       t.Deadline.HasValue &&
                                       ((t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline.Between(beginDate.Date, endDate.Date) : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                                        t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= beginDate.Date : t.Deadline <= serverBeginDate))) ||
                                      t.Status == Sungero.Workflow.Task.Status.InProcess && t.Deadline.HasValue &&
                                      (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= endDate.Date : t.Deadline <= serverEndDate)).ToList();

      var totalCount = actionItems.Count;
      if (totalCount == 0)
        return null;
      
      var completedInTime = actionItems
        .Where(j => j.Status == Workflow.Task.Status.Completed)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, j.ActualDate.Value, j.Assignee) == 0).Count();
      
      var inProcess = actionItems.Where(j => j.Status == Workflow.Task.Status.InProcess).Count();
      var inProcessOverdue = actionItems
        .Where(j => j.Status == Workflow.Task.Status.InProcess)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, Calendar.Now, j.Assignee) > 0).Count();

      int currentStatistic = 0;
      int.TryParse(Math.Round(totalCount == 0 ? 0 : ((completedInTime + inProcess - inProcessOverdue) * 100.00) / (double)totalCount).ToString(),
                   out currentStatistic);

      return currentStatistic;
    }
    
    /// <summary>
    /// Получить сокращенные ФИО соисполнителей.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <returns>Список сокращенных ФИО соисполнителей.</returns>
    private List<string> GetCoAssigneesShortNames(IActionItemExecutionTask task)
    {
      return task.CoAssignees.Select(ca => ca.Assignee.Person.ShortName).ToList();
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    [Remote]
    public virtual List<Structures.Module.LightActiomItem> GetActionItemCompletionData(DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author)
    {
      return this.GetActionItemCompletionData(null, null, beginDate, endDate, author, null, null, null, null, false, false);
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="documentType">Тип документов во вложениях поручений.</param>
    /// <param name="isMeetingsCoverContext">Признак контекста вызова с обложки совещаний.</param>
    /// <param name="getCoAssignees">Признак необходимости получения соисполнителей.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    public virtual List<Structures.Module.LightActiomItem> GetActionItemCompletionData(Meetings.IMeeting meeting,
                                                                                       IOfficialDocument document,
                                                                                       DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author,
                                                                                       IBusinessUnit businessUnit,
                                                                                       IDepartment department,
                                                                                       IUser performer,
                                                                                       IDocumentType documentType,
                                                                                       bool? isMeetingsCoverContext,
                                                                                       bool getCoAssignees)
    {
      List<Structures.Module.LightActiomItem> tasks = null;
      
      var isAdministratorOrAdvisor = Sungero.Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      var recipientsIds = Substitutions.ActiveSubstitutedUsers.Select(u => u.Id).ToList();
      recipientsIds.Add(Users.Current.Id);
      
      AccessRights.AllowRead(
        () =>
        {
          var query = ActionItemExecutionTasks.GetAll()
            .Where(t => isAdministratorOrAdvisor ||
                   recipientsIds.Contains(t.Author.Id) || recipientsIds.Contains(t.StartedBy.Id) ||
                   t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component &&
                   recipientsIds.Contains(t.MainTask.StartedBy.Id))
            .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
            .Where(t => t.IsCompoundActionItem != true && t.ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
          
          // Если отчёт вызывается не из документа (свойство Документ не заполнено), то даты заполнены и по ним нужно фильтровать.
          // Если же отчёт вызывается из документа, то поручения нужно фильтровать по этому документу во вложении.
          // Если отчет вызывается из Совещания, то поручения нужно фильтровать по протоколам этого совещания.
          
          // Guid группы вложений для документа в поручении.
          var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
          
          if (documentType != null)
          {
            var documents = OfficialDocuments.GetAll(d => d.DocumentKind.DocumentType == documentType);
            
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && documents.Any(m => m.Id == g.AttachmentId)));
          }
          
          if (meeting != null && isMeetingsCoverContext != true)
          {
            var minutesList = Meetings.Minuteses.GetAll(d => Equals(d.Meeting, meeting));
            
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutesList.Any(m => m.Id == g.AttachmentId)));
          }
          else if (document != null)
          {
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && g.AttachmentId == document.Id));
          }
          else
          {
            var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
            var serverEndDate = endDate.Value.EndOfDay().FromUserTime();
            query = query.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                                (Calendar.Between(t.ActualDate.Value.Date, beginDate.Value.Date, endDate.Value.Date) ||
                                 t.Deadline.HasValue &&
                                 ((t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline.Between(beginDate.Value.Date, endDate.Value.Date) : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                                  t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= beginDate.Value.Date : t.Deadline <= serverBeginDate))) ||
                                t.Status == Sungero.Workflow.Task.Status.InProcess && t.Deadline.HasValue &&
                                (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= endDate.Value.Date : t.Deadline <= serverEndDate));
          }
          
          if (isMeetingsCoverContext == true)
          {
            var minutesList = meeting == null ?
              Meetings.Minuteses.GetAll(d => d.Meeting != null) :
              Meetings.Minuteses.GetAll(d => Equals(d.Meeting, meeting));
            
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutesList.Any(m => m.Id == g.AttachmentId)));
          }
          
          // Dmitriev_IA: Проверка вынесена из Select для ускорения получения данных. Если занести проверку в Select, то проверка будет происходить для каждого t.
          if (getCoAssignees)
            tasks = query
              .Select(t => Structures.Module.LightActiomItem.Create(t.Id, t.Status, t.ActualDate, t.Deadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, this.GetCoAssigneesShortNames(t)))
              .ToList();
          else
            tasks = query
              .Select(t => Structures.Module.LightActiomItem.Create(t.Id, t.Status, t.ActualDate, t.Deadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, null))
              .ToList();
        });
      
      if (author != null)
        tasks = tasks.Where(t => Equals(t.Author, author))
          .ToList();
      
      if (businessUnit != null)
        tasks = tasks.Where(t => t.Assignee != null && t.Assignee.Department != null && t.Assignee.Department.BusinessUnit != null &&
                            Equals(t.Assignee.Department.BusinessUnit, businessUnit))
          .ToList();
      
      if (department != null)
        tasks = tasks.Where(t => t.Assignee != null && t.Assignee.Department != null &&
                            Equals(t.Assignee.Department, department))
          .ToList();
      
      if (performer != null)
        tasks = tasks.Where(t => Equals(t.Assignee, performer))
          .ToList();
      
      return tasks;
    }
    
    /// <summary>
    /// Признак того, что для совещания и/или документа были поручения, выполненные в срок.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <returns>True, если были поручения, выполненные в срок, False в противном случае.</returns>
    [Public, Remote]
    public bool ActionItemCompletionDataIsPresent(Meetings.IMeeting meeting, IOfficialDocument document)
    {
      return this.GetActionItemCompletionData(meeting, document, null, null, null, null, null, null, null, false, true).Any();
    }
    
    #endregion

    #endregion

    #region Типы задач

    /// <summary>
    /// Создать задачу по процессу "Рассмотрение входящего".
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Задача по процессу "Рассмотрение входящего".</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static ITask CreateDocumentReview(Sungero.Docflow.IOfficialDocument document)
    {
      var task = CreateDocumentReviewTask(document, Tasks.Null);
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="documentId">ИД документа на рассмотрение.</param>
    /// <param name="addresseeId">ИД адресата.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <returns>ИД задачи на рассмотрение.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual int CreateDocumentReviewTask(int documentId, int? addresseeId, string activeText)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Document with ID ({0}) not found.", documentId));
      
      var addressee = Employees.Null;
      if (addresseeId.HasValue)
      {
        addressee = Employees.GetAll(e => e.Id == addresseeId).FirstOrDefault();
        if (addressee == null)
          throw AppliedCodeException.Create(string.Format("Create review task. Employee with ID ({0}) not found.", addresseeId));
      }
      
      var task = CreateDocumentReviewTask(document, null);
      if (addresseeId.HasValue)
      {
        task.Addressees.Clear();
        task.Addressees.AddNew().Addressee = addressee;
      }
      task.ActiveText = activeText;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <param name="parentTask">Задача-основание.</param>
    /// <param name="addressees">Адресаты.</param>
    /// <returns>Задача на рассмотрение.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IDocumentReviewTask CreateDocumentReviewTask(Sungero.Docflow.IOfficialDocument document, ITask parentTask, List<IEmployee> addressees)
    {
      var task = CreateDocumentReviewTask(document, parentTask);
      Functions.DocumentReviewTask.SetAddressees(task, addressees);
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <param name="parentTask">Задача-основание.</param>
    /// <returns>Задача на рассмотрение.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IDocumentReviewTask CreateDocumentReviewTask(Sungero.Docflow.IOfficialDocument document, ITask parentTask)
    {
      var task = parentTask == null ? DocumentReviewTasks.Create() : DocumentReviewTasks.CreateAsSubtask(parentTask);
      
      task.DocumentForReviewGroup.All.Add(document);
      
      // Выдать права группе регистрации документа.
      if (document.DocumentRegister != null)
      {
        var registrationGroup = document.DocumentRegister.RegistrationGroup;
        
        if (registrationGroup != null)
          task.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }
      
      Functions.DocumentReviewTask.SynchronizeAddressees(task, document);
      
      return task;
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа с указанием задачи-основания.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="addresseeId">ИД адресата.</param>
    /// <param name="parentTaskId">ИД задачи-основания.</param>
    /// <returns>ИД задачи на рассмотрение.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual int CreateDocumentReviewTaskFromParentTask(int documentId, int addresseeId, int parentTaskId)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Document with ID ({0}) not found.", documentId));
      
      var addressee = Employees.GetAll(e => e.Id == addresseeId).FirstOrDefault();
      if (addressee == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Employee with ID ({0}) not found.", addresseeId));
      
      var parentTask = Tasks.GetAll(t => t.Id == parentTaskId).FirstOrDefault();
      if (parentTask == null)
        throw AppliedCodeException.Create(string.Format("Create review task. Parent task with ID ({0}) not found.", parentTaskId));
      
      var task = CreateDocumentReviewTask(document, parentTask);
      
      task.Addressees.Clear();
      task.Addressees.AddNew().Addressee = addressee;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать поручение по документу.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Только для создания самостоятельного поручения.
    /// Для создания подпоручения используется CreateActionItemExecutionTask(document, parentAssignment).</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document)
    {
      return this.CreateActionItemExecution(document, Assignments.Null);
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, int parentAssignmentId)
    {
      return this.CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <param name="resolution">Текст резолюции.</param>
    /// <param name="assignedBy">Пользователь - автор резолюции.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecutionWithResolution(IOfficialDocument document, int parentAssignmentId, string resolution, Sungero.Company.IEmployee assignedBy)
    {
      var newTask = this.CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
      newTask.ActiveText = resolution;
      newTask.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, assignedBy) ? assignedBy : null;
      return newTask;
    }

    /// <summary>
    /// Создать задачу на исполнение поручения по документу.
    /// </summary>
    /// <param name="documentId">ИД документа на рассмотрение.</param>
    /// <param name="assigneeId">ИД адресата.</param>
    /// <param name="isUnderControl">Поручение на контроле.</param>
    /// <param name="supervisorId">ИД контролера.</param>
    /// <param name="coassigneeId">ИД соисполнителя.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <returns>ИД задачи на исполнение поручения.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual int CreateActionItemExecution(int documentId, int assigneeId, bool isUnderControl, int? supervisorId, int? coassigneeId, DateTime deadline, string activeText)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create action item execution task. Document with ID ({0}) not found.", documentId));
      
      var assignee = Employees.GetAll(e => e.Id == assigneeId).FirstOrDefault();
      if (assignee == null)
        throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", assigneeId));
      
      var supervisor = Employees.Null;
      if (isUnderControl)
        if (supervisorId.HasValue)
      {
        supervisor = Employees.GetAll(e => e.Id == supervisorId).FirstOrDefault();
        if (supervisor == null)
          throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", supervisorId));
      }
      else
        throw AppliedCodeException.Create("Create action item execution task. Supervisor is required for action item with contol.");
      
      var coassignee = Employees.Null;
      if (coassigneeId.HasValue)
      {
        coassignee = Employees.GetAll(e => e.Id == coassigneeId).FirstOrDefault();
        if (coassignee == null)
          throw AppliedCodeException.Create(string.Format("Create action item execution task. Employee with ID ({0}) not found.", coassigneeId));
      }
      
      var task = this.CreateActionItemExecution(document, null);
      task.Assignee = assignee;
      task.Deadline = deadline;
      task.IsUnderControl = isUnderControl;
      if (isUnderControl)
        task.Supervisor = supervisor;
      if (coassigneeId.HasValue)
      {
        task.CoAssignees.Clear();
        task.CoAssignees.AddNew().Assignee = coassignee;
      }
      task.ActiveText = activeText;
      task.Save();
      
      return task.Id;
    }
    
    /// <summary>
    /// Создать поручение по документу с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignment">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, IAssignment parentAssignment)
    {
      var parentAssignmentId = parentAssignment != null ? parentAssignment.Id : -1;
      Logger.DebugFormat("Start CreateActionItemExecution, CreateAsSubtask = {0}, Parent assignment (ID={1}).", parentAssignment != null, parentAssignmentId);
      
      var task = parentAssignment == null ? ActionItemExecutionTasks.Create() : ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
      var taskId = task != null ? task.Id : -1;
      
      if (parentAssignment != null)
      {
        Logger.DebugFormat("Start SynchronizeAttachmentsToActionItem from parent task (ID={0}).", parentAssignment.Task.Id);
        Functions.Module.SynchronizeAttachmentsToActionItem(parentAssignment.Task, task);
      }
      else
      {
        Logger.DebugFormat("Start SynchronizeAttachmentsToActionItem from document (ID={0}).", document.Id);
        Functions.Module.SynchronizeAttachmentsToActionItem(document,
                                                            new List<IElectronicDocument>(),
                                                            new List<int>(),
                                                            new List<int>(),
                                                            new List<IEntity>(),
                                                            task);
      }
      Logger.Debug("End SynchronizeAttachmentsToActionItem.");
      
      if (document != null)
      {
        // Выдать права на изменение группе регистрации. Группа регистрации будет взята из журнала документа.
        var documentRegister = document.DocumentRegister;
        if (documentRegister != null && documentRegister.RegistrationGroup != null)
        {
          Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Grant access rights to registration group (ID={1}).", taskId, documentRegister.RegistrationGroup.Id);
          task.AccessRights.Grant(documentRegister.RegistrationGroup, DefaultAccessRightsTypes.Change);
        }
      }

      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Set task Subject.", taskId);
      task.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(task, ActionItemExecutionTasks.Resources.TaskSubject);
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). Task Subject = {1}.", taskId, task.Subject);
      
      Logger.DebugFormat("ActionItemExecutionTask (ID={0}). End CreateActionItemExecution.", taskId);
      return task;
    }

    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <returns>Поручение.</returns>
    [Remote, Public]
    public virtual IActionItemExecutionTask CreateActionItemExecution()
    {
      return ActionItemExecutionTasks.Create();
    }
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <returns>Поручение.</returns>
    [Public]
    public virtual IActionItemExecutionTask CreateActionItemExecutionFromExecution(Sungero.RecordManagement.IActionItemExecutionAssignment actionItemAssignment)
    {
      if (actionItemAssignment == null)
      {
        Logger.Debug("ActionItemExecutionAssignment is null.");
        return ActionItemExecutionTasks.Null;
      }
      
      var actionItemAssignmentId = actionItemAssignment.Id;
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Get documents.", actionItemAssignmentId);
      var document = actionItemAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var task = this.CreateActionItemExecution(document, actionItemAssignment);
      if (task == null)
      {
        Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Task is null.", actionItemAssignmentId);
        return ActionItemExecutionTasks.Null;
      }
      
      var taskId = task.Id;
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Task (ID={1}) created.", actionItemAssignmentId, taskId);
      
      // Для подчиненных поручений заполнить признак автовыполнения из персональных настроек.
      if (actionItemAssignment != null)
      {
        var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Employees.As(task.StartedBy));
        task.IsAutoExec = settings != null && (task.IsUnderControl != true || !Equals(task.Supervisor, task.StartedBy))
          ? settings.IsAutoExecLeadingActionItem
          : false;
      }
      
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set Assignee = null. Task (ID={1}).", actionItemAssignmentId, taskId);
      task.Assignee = null;
      if (actionItemAssignment.Deadline.HasValue &&
          (actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Now ||
           !actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Today))
      {
        Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set Deadline = {1}. Task (ID={2}).", actionItemAssignmentId, actionItemAssignment.Deadline, taskId);
        task.Deadline = actionItemAssignment.Deadline;
      }
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). Set AssignedBy = {1} (ID={2}). Task (ID={3}).",
                         actionItemAssignmentId, Users.Current, Users.Current.Id, taskId);
      task.AssignedBy = Employees.Current;
      Logger.DebugFormat("ActionItemExecutionAssignment (ID={0}). End CreateActionItemExecutionFromExecution.", actionItemAssignmentId);
      
      return task;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <returns>Задача на ознакомление с документом.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTask(IOfficialDocument document)
    {
      var newAcqTask = AcquaintanceTasks.Create();
      newAcqTask.DocumentGroup.OfficialDocuments.Add(document);
      return newAcqTask;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="documentId">ИД документа, который отправляется на ознакомление.</param>
    /// <param name="performerIds">Список участников.</param>
    /// <param name="activeText">Текст задачи.</param>
    /// <param name="isElectronicAcquaintance">Ознакомление в электронном виде.</param>
    /// <param name="deadline">Срок задачи.</param>
    /// <returns>ИД задачи на ознакомление с документом.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual int CreateAcquaintanceTask(int documentId, List<int> performerIds, string activeText, bool isElectronicAcquaintance, DateTime deadline)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        throw AppliedCodeException.Create(string.Format("Create acquaintance task. Document with ID ({0}) not found.", documentId));

      var performers = new List<IEmployee>();
      if (performerIds.Any())
      {
        performers = Employees.GetAll(e => performerIds.Contains(e.Id)).ToList();
        if (!performers.Any())
          throw AppliedCodeException.Create(string.Format("Create acquaintance task. No employee found."));
      }

      var task = CreateAcquaintanceTask(document);
      if (performerIds.Any())
      {
        task.Performers.Clear();
        foreach (var performer in performers)
          task.Performers.AddNew().Performer = performer;
      }
      task.ActiveText = activeText;
      task.IsElectronicAcquaintance = isElectronicAcquaintance;
      task.Deadline = deadline;
      task.Save();

      return task.Id;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <param name="parentAssignment">Задание, из которого создается подзадача.</param>
    /// <returns>Задача на ознакомление по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTaskAsSubTask(IOfficialDocument document, IAssignment parentAssignment)
    {
      var newAcqTask = AcquaintanceTasks.CreateAsSubtask(parentAssignment);
      RecordManagement.PublicFunctions.Module.SynchronizeAttachmentsToAcquaintance(parentAssignment.Task, newAcqTask);
      return newAcqTask;
    }

    #region AbortSubtasksAndSendNotices

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    [Public, Remote]
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem)
    {
      AbortSubtasksAndSendNotices(actionItem, null, string.Empty);
    }

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    /// <param name="performer">Исполнитель, которого не нужно уведомлять.</param>
    /// <param name="abortingReason">Причина прекращения.</param>
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem, IUser performer = null, string abortingReason = "")
    {
      // Собрать всех пользователей, которым нужно выслать уведомления.
      var recipients = new List<Sungero.CoreEntities.IUser>();
      
      // Уведомить актуальных контролера и исполнителя текущего поручения.
      recipients.AddRange(Functions.ActionItemExecutionTask.GetActualSupervisorAndAssignee(actionItem));
      
      // Получить дерево всех подзадач текущего поручения.
      var subTasks = Functions.Module.GetSubtasksForTaskRecursive(actionItem);
      foreach (var subTask in subTasks.Where(t => ActionItemExecutionTasks.Is(t) || DeadlineExtensionTasks.Is(t) ||
                                             Docflow.DeadlineExtensionTasks.Is(t) || StatusReportRequestTasks.Is(t)))
      {
        var actionItemExecutionSubTask = ActionItemExecutionTasks.As(subTask);
        if (actionItemExecutionSubTask != null)
          actionItemExecutionSubTask.AbortingReason = string.IsNullOrEmpty(abortingReason) ? actionItemExecutionSubTask.AbortingReason : abortingReason;

        subTask.Abort();
        
        // Для подзадач-поручений: уведомить актуальных исполнителя и контролера.
        if (actionItemExecutionSubTask != null)
          recipients.AddRange(Functions.ActionItemExecutionTask.GetActualSupervisorAndAssignee(actionItemExecutionSubTask));
        // Для остальных подзадач: уведомить исполнителей всех заданий/уведомлений в подзадаче.
        else
          recipients.AddRange(AssignmentBases.GetAll(a => Equals(a.Task, subTask)).Select(u => u.Performer).ToList());
      }

      // Исключить дубли, текущего пользователя и пользователя из параметра.
      recipients = recipients.Distinct().ToList();
      if (performer != null)
        recipients.Remove(performer);
      else
        recipients.Remove(Users.Current);

      // Выслать уведомление.
      if (recipients.Any())
      {
        var threadSubject = ActionItemExecutionTasks.Resources.NoticeSubjectWithoutDoc;
        var noticesSubject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItem, Sungero.RecordManagement.Resources.TwoSpotTemplateFormat(threadSubject));
        Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticesSubject, recipients, actionItem, actionItem.AbortingReason, performer, threadSubject);
      }
    }
    
    #endregion

    /// <summary>
    /// Рекурсивно получить все незавершенные подзадачи.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить незавершенные подзадачи.</param>
    /// <returns>Список незавершенных подзадач.</returns>
    public static List<ITask> GetSubtasksForTaskRecursive(ITask task)
    {
      var subTasksByParentTask = Functions.Module.GetSubtasksForTaskByParentTask(task, null).ToList();
      var subTasksByParentAssignment = Functions.Module.GetSubtasksForTaskByParentAssignment(task, null).ToList();
      var result = new List<ITask>();
      result.AddRange(subTasksByParentTask);
      result.AddRange(subTasksByParentAssignment);
      foreach (var subTask in subTasksByParentTask)
        result.AddRange(GetSubtasksForTaskRecursive(subTask));
      foreach (var subTask in subTasksByParentAssignment)
        result.AddRange(GetSubtasksForTaskRecursive(subTask));

      return result;
    }
    
    /// <summary>
    /// Получить все подзадачи.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить подзадачи.</param>
    /// <param name="status">Статус подзадач, которые необходимо получить.</param>
    /// <returns>Список подзадач.</returns>
    [Obsolete("Неоптимальная логика, рекомендуется использовать GetSubtasksForTaskByParentTask и GetSubtasksForTaskByParentAssignment")]
    public static IQueryable<ITask> GetSubtasksForTask(ITask task, Enumeration? status)
    {
      if (status == null)
        status = Workflow.Task.Status.InProcess;
      return Tasks.GetAll(x => (x.ParentAssignment != null && Equals(task, x.ParentAssignment.Task) ||
                                x.ParentAssignment == null && Equals(task, x.ParentTask)) &&
                          x.Status == status);
    }
    
    /// <summary>
    /// Получить все подзадачи, привязанные через задачу.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить подзадачи.</param>
    /// <param name="status">Статус подзадач, которые необходимо получить.</param>
    /// <returns>Список подзадач.</returns>
    public static IQueryable<ITask> GetSubtasksForTaskByParentTask(ITask task, Enumeration? status)
    {
      if (status == null)
        status = Workflow.Task.Status.InProcess;
      return Tasks.GetAll()
        .Where(x => x.Status == status)
        .Where(x => x.ParentTask != null && Equals(task, x.ParentTask));
    }
    
    /// <summary>
    /// Получить все подзадачи, привязанные через задания.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить подзадачи.</param>
    /// <param name="status">Статус подзадач, которые необходимо получить.</param>
    /// <returns>Список подзадач.</returns>
    public static IQueryable<ITask> GetSubtasksForTaskByParentAssignment(ITask task, Enumeration? status)
    {
      if (status == null)
        status = Workflow.Task.Status.InProcess;
      return Tasks.GetAll()
        .Where(x => x.Status == status)
        .Where(x => x.ParentAssignment != null && Equals(task, x.ParentAssignment.Task));
    }
    
    /// <summary>
    /// Получить ведущую задачу.
    /// </summary>
    /// <param name="task">Задача, для которой нужно получить ведущую.</param>
    /// <returns>Ведущая задача.</returns>
    public static ITask GetParentTask(ITask task)
    {
      if (task == null)
        return null;
      return task.ParentAssignment != null ? task.ParentAssignment.Task : task.ParentTask;
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие выполнения ведущего задания на исполнение поручения.
    /// </summary>
    /// <param name="actionItemId">ИД поручения.</param>
    /// <param name="parentAssignmentId">ИД ведущего задания на исполнение поручения.</param>
    /// <param name="parentTaskStartId">Количество стартов задачи, в рамках которой создано ведущее задание.</param>
    [Remote]
    public virtual void CompleteParentActionItemExecutionAssignmentAsync(int actionItemId, int parentAssignmentId, int? parentTaskStartId)
    {
      Logger.DebugFormat("CompleteParentActionItemExecutionAssignmentAsync({0}, {1}, {2}): TaskId {0}, ParentAssignmentId {1}, ParentTaskStartId {2}.",
                         actionItemId, parentAssignmentId, parentTaskStartId);
      var completeParentActionItemHandler = RecordManagement.AsyncHandlers.CompleteParentActionItemExecutionAssignment.Create();
      completeParentActionItemHandler.actionItemId = actionItemId;
      completeParentActionItemHandler.parentAssignmentId = parentAssignmentId;
      completeParentActionItemHandler.parentTaskStartId = parentTaskStartId ?? 0;
      completeParentActionItemHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения составного поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    [Public, Remote]
    public virtual void ChangeCompoundActionItemAsync(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, int actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ChangeCompoundActionItemAsync({0}): actionItemTaskId {0}", actionItemTaskId);
      var changeCompoundActionItemHandler = RecordManagement.AsyncHandlers.ChangeCompoundActionItem.Create();
      changeCompoundActionItemHandler.ActionItemTaskId = actionItemTaskId;
      changeCompoundActionItemHandler.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      changeCompoundActionItemHandler.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      changeCompoundActionItemHandler.OldAssignee = changes.OldAssignee?.Id ?? -1;
      changeCompoundActionItemHandler.NewAssignee = changes.NewAssignee?.Id ?? -1;
      changeCompoundActionItemHandler.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      changeCompoundActionItemHandler.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      changeCompoundActionItemHandler.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      changeCompoundActionItemHandler.EditingReason = changes.EditingReason;
      changeCompoundActionItemHandler.AdditionalInfo = changes.AdditionalInfo;
      changeCompoundActionItemHandler.TaskIds = string.Join(",", changes.TaskIds);
      changeCompoundActionItemHandler.ActionItemPartsText = changes.ActionItemPartsText;
      changeCompoundActionItemHandler.OnEditGuid = onEditGuid;
      changeCompoundActionItemHandler.InitiatorOfChange = changes.InitiatorOfChange.Id;
      changeCompoundActionItemHandler.ChangeContext = changes.ChangeContext;
      
      changeCompoundActionItemHandler.ExecuteAsync();
    }
    
    #endregion

    #region Работа с документами

    /// <summary>
    /// Получить виды документов по документопотоку.
    /// </summary>
    /// <param name="direction">Документопоток вида документа.</param>
    /// <returns>Виды документов.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentKind> GetFilteredDocumentKinds(Enumeration direction)
    {
      if (direction == Docflow.DocumentKind.DocumentFlow.Incoming)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Incoming).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Outgoing)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Outgoing).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Inner)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Inner).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Contracts)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Contracts).ToList();
      else
        return null;
    }

    /// <summary>
    /// Получить входящее письмо по ИД.
    /// </summary>
    /// <param name="letterId">ИД письма.</param>
    /// <returns>Если письмо не существует возвращает null.</returns>
    [Remote(IsPure = true)]
    public static IOutgoingDocumentBase GetIncomingLetterById(int letterId)
    {
      return Sungero.Docflow.OutgoingDocumentBases.GetAll().FirstOrDefault(l => l.Id == letterId);
    }
    
    /// <summary>
    /// Провалидировать подписи документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="onlyLastSignature">Проверить только последнюю подпись.</param>
    /// <returns>Если подписи валидны, возвращает пустой список, иначе список ошибок.</returns>
    [Public]
    public static List<string> GetDocumentSignatureValidationErrors(IEntity document, bool onlyLastSignature)
    {
      var validationMessages = new List<string>();
      if (document == null)
        return validationMessages;
      
      var signatures = Signatures.Get(document).Where(s => s.SignatureType == SignatureType.Approval && s.IsExternal != true);
      if (onlyLastSignature)
        signatures = signatures.OrderByDescending(x => x.Id).Take(1);
      
      foreach (var signature in signatures)
      {
        var error = Functions.Module.GetSignatureValidationErrors(signature);
        if (!string.IsNullOrWhiteSpace(error))
          validationMessages.Add(error);
      }
      
      return validationMessages;
    }
    
    /// <summary>
    /// Провалидировать подпись.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Если подпись валидна, возвращает пустую строку, иначе строку с ошибкой.</returns>
    [Public]
    public static string GetSignatureValidationErrors(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return string.Empty;

      var separator = ". ";
      var signatureErrors = Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
      if (string.IsNullOrWhiteSpace(signatureErrors))
        return string.Empty;
      
      var signatory = string.IsNullOrWhiteSpace(signature.SubstitutedUserFullName)
        ? signature.SignatoryFullName
        : RecordManagement.Resources.SignatorySubstituteFormat(signature.SignatoryFullName, signature.SubstitutedUserFullName);
      
      return RecordManagement.Resources.SignatureValidationMessageFormat(signatory,
                                                                         signature.SigningDate,
                                                                         signatureErrors);
    }
    
    /// <summary>
    /// Установить состояние исполнения документа по задаче.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="document">Документ.</param>
    /// <param name="state">Состояние исполнения.</param>
    /// <remarks>Применяется к задачам на рассмотрение документа и исполнения поручений по документу.
    /// При установке статуса принимаются в расчет другие задачи на рассмотрение или исполнение поручения по документу.
    /// </remarks>
    public virtual void SetDocumentExecutionState(ITask task, IOfficialDocument document, Enumeration? state)
    {
      if (task == null || document == null || !document.AccessRights.CanUpdate())
        return;
      
      Enumeration? executionState = state;
      
      Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2})", task.Id, document.Id, state);
      var states = Functions.Module.GetExecutionStateVariants(task, document);
      states = states.Where(t => t != null).Distinct().ToList();
      if (states.Any())
      {
        if (state != null && !states.Contains(state))
          states.Add(state);
        Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2}). ExecutionState variants: {3}",
                           task.Id, document.Id, state, string.Join(", ", states));
        var priorities = PublicFunctions.Module.GetExecutionStatePriorities();
        priorities = priorities.Where(x => states.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        executionState = priorities.OrderByDescending(p => p.Value).FirstOrDefault().Key;
      }
      
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetExecutionState(document, executionState);
      Logger.DebugFormat("RM SetExecutionState(task:{0}, document:{1}, state:{2}). ExecutionState: {2}",
                         task.Id, document.Id, state);
    }
    
    /// <summary>
    /// Установить состояние контроля исполнения документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void SetDocumentControlExecutionState(IOfficialDocument document)
    {
      if (document == null || !document.AccessRights.CanUpdate())
        return;
      
      var controlExecutionState = Sungero.Docflow.PublicFunctions.OfficialDocument.GetControlExecutionState(document);
      Sungero.Docflow.PublicFunctions.OfficialDocument.SetControlExecutionState(document, controlExecutionState);
      Logger.DebugFormat("RM SetControlExecutionState(document:{0}). ControlExecutionState: {1}",
                         document.Id, controlExecutionState);
    }
    
    /// <summary>
    /// Получить возможные варианты статуса исполнения документа.
    /// </summary>
    /// <param name="task">Задача, в рамках которой меняется статус исполнения документа.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Список возможных статусов исполнения документа.</returns>
    public virtual List<Enumeration?> GetExecutionStateVariants(ITask task, IOfficialDocument document)
    {
      /* Статус исполнения документа зависит от:
       * - других рассмотрений, которые выполняются по документу
       * - других рассмотрений, которые были выполнены в рамках многоадресного рассмотрения
       * - поручений по документу, которые выполняются или уже были выполнены
       */
      
      var states = new List<Enumeration?>();
      
      var otherReviewTasks = Sungero.Docflow.PublicFunctions.OfficialDocument.GetDocumentReviewTasks(document)
        .Where(t => t.Id != task.Id)
        .ToList();
      states.AddRange(otherReviewTasks.Select(x => Functions.DocumentReviewTask.GetDocumentExecutionState(x)));
      
      var parentTask = Functions.Module.GetParentTask(task);
      /* Поручение может быть создано в рамках рассмотрения из задачи-контейнера,
       * в этом случае необходимо учесть параллельные ветки задачи-контейнера.
       * Если поручение создано в рамках простого рассмотрения, то это рассмотрение будет учтено в otherReviewTasks.
       */
      if (ActionItemExecutionTasks.Is(task) && parentTask != null && DocumentReviewTasks.Is(parentTask))
        parentTask = Functions.Module.GetParentTask(parentTask);
      if (parentTask != null && DocumentReviewTasks.Is(parentTask))
      {
        var reviewSubTasks = Functions.DocumentReviewTask.GetCompletedDocumentReviewSubTasks(DocumentReviewTasks.As(parentTask))
          .Where(t => t.Id != task.Id)
          .Where(t => t.StartId != null && t.StartId == parentTask.StartId)
          .ToList();
        // Исключить рассмотрение, в рамках которого создано поручение.
        if (ActionItemExecutionTasks.Is(task))
        {
          parentTask = Functions.Module.GetParentTask(task);
          reviewSubTasks = reviewSubTasks.Where(x => x.Id != parentTask.Id).ToList();
        }
        states.AddRange(reviewSubTasks.Select(x => Functions.DocumentReviewTask.GetDocumentExecutionState(x)));
      }
      
      var actionItems = Sungero.Docflow.PublicFunctions.OfficialDocument.GetFirstLevelActionItems(document)
        .Where(t => t.Id != task.Id)
        .ToList();
      states.AddRange(actionItems.Select(x => Functions.ActionItemExecutionTask.GetDocumentExecutionState(x)));
      
      return states;
    }
    
    #endregion

    #region Работа с SQL

    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="format">Формат запроса.</param>
    /// <param name="args">Аргументы запроса, подставляемые в формат.</param>
    public static void ExecuteSQLCommandFormat(string format, params object[] args)
    {
      // Функция дублируется из Docflow, т.к. нельзя исп. params в public-функциях.
      var command = string.Format(format, args);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }

    #endregion

    #region Подпапки входящих
    
    /// <summary>
    /// Применить к списку заданий стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку заданий фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <param name="inProcess">Признак показа заданий "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query,
                                                                                    bool inProcess,
                                                                                    bool shortPeriod,
                                                                                    bool middlePeriod,
                                                                                    bool longPeriod,
                                                                                    bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
      
      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(a => a.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(a => a.Created >= periodBegin || a.Status == Workflow.AssignmentBase.Status.InProcess);

      return query;
    }
    
    /// <summary>
    /// Применить к списку задач стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку задач фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <param name="inProcess">Признак показа задач "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query,
                                                                          bool inProcess,
                                                                          bool shortPeriod,
                                                                          bool middlePeriod,
                                                                          bool longPeriod,
                                                                          bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(t => t.Status == Workflow.Task.Status.InProcess || t.Status == Workflow.Task.Status.Draft);

      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(t => t.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(t => t.Created >= periodBegin || t.Status == Workflow.AssignmentBase.Status.InProcess);
      
      return query;
    }

    #endregion

    /// <summary>
    /// Получить информацию о контроле поручения.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о контролере.</returns>
    [Public]
    public virtual string GetSupervisorInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var supervisor = actionItemTask.Supervisor;
      var isOnControl = actionItemTask.IsUnderControl == true;
      var supervisorLabel = string.Empty;
      if (isOnControl && supervisor != null)
        supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
      return supervisorLabel;
    }
    
    /// <summary>
    /// Данные для печати проекта резолюции.
    /// </summary>
    /// <param name="resolution">Список поручений.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> resolution,
                                                                                                                      string reportSessionId)
    {
      var result = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      foreach (var actionItemTask in resolution)
      {
        // Контролер.
        var supervisor = actionItemTask.Supervisor;
        var isOnControl = actionItemTask.IsUnderControl == true;
        var supervisorLabel = string.Empty;
        if (isOnControl && supervisor != null)
          supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
        
        // Равноправное поручение.
        if (actionItemTask.IsCompoundActionItem == true)
        {
          foreach (var part in actionItemTask.ActionItemParts)
          {
            var partSupervisorLabel = supervisorLabel;
            if (isOnControl && part.Supervisor != null)
              partSupervisorLabel = Company.PublicFunctions.Employee.GetShortName(part.Supervisor, false);
            var deadline = part.Deadline ?? actionItemTask.FinalDeadline ?? null;
            var coAssigneeDeadline = part.CoAssigneesDeadline ?? actionItemTask.CoAssigneesDeadline ?? null;
            var resolutionLabel = string.Join("\r\n", actionItemTask.ActiveText, part.ActionItemPart);
            var subAssignees = Functions.ActionItemExecutionTask.GetPartCoAssignees(actionItemTask, part.PartGuid);
            var data = GetActionItemDraftResolutionReportData(part.Assignee,
                                                              subAssignees,
                                                              deadline,
                                                              coAssigneeDeadline,
                                                              resolutionLabel,
                                                              partSupervisorLabel,
                                                              reportSessionId);
            result.Add(data);
          }
        }
        else
        {
          // Поручение с соисполнителями.
          var deadline = actionItemTask.Deadline ?? null;
          var coAssigneeDeadline = actionItemTask.CoAssigneesDeadline ?? null;
          var subAssignees = actionItemTask.CoAssignees.Select(a => a.Assignee).ToList();
          var data = GetActionItemDraftResolutionReportData(actionItemTask.Assignee,
                                                            subAssignees,
                                                            deadline,
                                                            coAssigneeDeadline,
                                                            actionItemTask.ActiveText,
                                                            supervisorLabel,
                                                            reportSessionId);
          result.Add(data);
        }
      }
      return result;
    }
    
    /// <summary>
    /// Получение данных поручения для отчета Проект резолюции.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="subAssignees">Соисполнители.</param>
    /// <param name="deadline">Срок исполнения.</param>
    /// <param name="coAssigneeDeadline">Срок соисполнителей.</param>
    /// <param name="actionItem">Текст поручения.</param>
    /// <param name="supervisorLabel">Контролёр.</param>
    /// <param name="reportSessionId">Ид сессии.</param>
    /// <returns>Данные поручения.</returns>
    public static Structures.DraftResolutionReport.DraftResolutionReportParameters GetActionItemDraftResolutionReportData(IEmployee assignee,
                                                                                                                          List<IEmployee> subAssignees,
                                                                                                                          DateTime? deadline,
                                                                                                                          DateTime? coAssigneeDeadline,
                                                                                                                          string actionItem,
                                                                                                                          string supervisorLabel,
                                                                                                                          string reportSessionId)
    {
      var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
      data.ReportSessionId = reportSessionId;
      
      // Исполнители и срок.
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(assignee, false);
      if (subAssignees != null && subAssignees.Any())
        assigneeShortName = string.Format("<u>{0}</u>{1}{2}", assigneeShortName, Environment.NewLine,
                                          string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
      
      data.PerformersLabel = assigneeShortName;
      if (!Equals(deadline, null))
        data.Deadline = deadline.Value.HasTime() ? deadline.Value.ToUserTime().ToString("g") : deadline.Value.ToString("d");
      else
        data.Deadline = Resources.ActionItemIndefiniteDeadline;
      
      // Срок соисполнителей.
      var formattedCoAssigneeDeadline = string.Empty;
      if (!Equals(coAssigneeDeadline, null))
      {
        formattedCoAssigneeDeadline = coAssigneeDeadline.Value.HasTime() ? coAssigneeDeadline.Value.ToUserTime().ToString("g") : coAssigneeDeadline.Value.ToString("d");
        data.Deadline = string.Join("\n", data.Deadline, formattedCoAssigneeDeadline + Sungero.RecordManagement.Resources.CoAssignees);
      }
      
      // Поручение.
      data.ResolutionLabel = actionItem;
      
      // Контролёр.
      data.SupervisorLabel = supervisorLabel;
      
      return data;
    }
    
    /// <summary>
    /// Исключить из наблюдателей системных пользователей.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <returns>Отфильтрованный результат запроса.</returns>
    [Public]
    public IQueryable<Sungero.CoreEntities.IRecipient> ObserversFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query)
    {
      var systemRecipientsSid = Company.PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);
      return query.Where(x => !systemRecipientsSid.Contains(x.Sid.Value));
    }

    /// <summary>
    /// Получить константу срока рассмотрения документа по умолчанию в днях.
    /// </summary>
    /// <returns>Константу срока рассмотрения документа по умолчанию в днях.</returns>
    [RemoteAttribute]
    public virtual int GetDocumentReviewDefaultDays()
    {
      return Constants.Module.DocumentReviewDefaultDays;
    }
    
    /// <summary>
    /// Получить отфильтрованные журналы регистрации для отчета.
    /// </summary>
    /// <param name="direction">Документопоток.</param>
    /// <returns>Журналы регистрации.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentRegister> GetFilteredDocumentRegistersForReport(Enumeration direction)
    {
      var needFilterDocumentRegisters = !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      return Docflow.PublicFunctions.DocumentRegister.Remote.GetFilteredDocumentRegisters(direction, null, needFilterDocumentRegisters).ToList();
    }
    
    /// <summary>
    /// Удалить поручения.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    [Remote]
    public static void DeleteActionItemExecutionTasks(List<IActionItemExecutionTask> actionItems)
    {
      foreach (var draftResolution in actionItems)
        ActionItemExecutionTasks.Delete(draftResolution);
    }
    
    /// <summary>
    /// Получить списки ознакомления.
    /// </summary>
    /// <returns>Списки ознакомления.</returns>
    [Public, Remote(IsPure = true)]
    public IQueryable<IAcquaintanceList> GetAcquaintanceLists()
    {
      return AcquaintanceLists.GetAll()
        .Where(a => a.Status == Sungero.RecordManagement.AcquaintanceList.Status.Active);
    }
    
    /// <summary>
    /// Создать список ознакомления.
    /// </summary>
    /// <returns>Список ознакомления.</returns>
    [Public, Remote]
    public IAcquaintanceList CreateAcquaintanceList()
    {
      return AcquaintanceLists.Create();
    }
    
    /// <summary>
    /// Получить поручение по ИД.
    /// </summary>
    /// <param name="id">ИД задачи.</param>
    /// <returns>Поручение.</returns>
    [Remote]
    public IActionItemExecutionTask GetActionitemById(int id)
    {
      return ActionItemExecutionTasks.GetAll(t => Equals(t.Id, id)).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить исполнителей задачи на ознакомление.
    /// </summary>
    /// <param name="recipients">Участники.</param>
    /// <returns>Исполнители.</returns>
    [Obsolete("Используйте метод Company.PublicFunctions.Module.GetEmployeesFromRecipients()")]
    public virtual List<IEmployee> GetAcquaintanceTaskPerformers(List<IRecipient> recipients)
    {
      return Company.PublicFunctions.Module.GetEmployeesFromRecipients(recipients);
    }
    
    /// <summary>
    /// Получить статус выполнения задания на ознакомление.
    /// </summary>
    /// <param name="assignment">Задание на ознакомление.</param>
    /// <param name="isElectronicAcquaintance">Признак "Электронное ознакомление".</param>
    /// <param name="isCompleted">Признак завершённости задачи.</param>
    /// <returns>Статус выполнения задания на ознакомление.</returns>
    public virtual string GetAcquaintanceAssignmentState(IAcquaintanceAssignment assignment,
                                                         bool isElectronicAcquaintance,
                                                         bool isCompleted)
    {
      if (!isCompleted)
        return string.Empty;
      
      if (Equals(assignment.CompletedBy, assignment.Performer) || !isElectronicAcquaintance)
        return Reports.Resources.AcquaintanceReport.AcquaintedState;

      return Reports.Resources.AcquaintanceReport.CompletedState;
    }
    
    /// <summary>
    /// Получить все приложения по задачам ознакомления с документом.
    /// </summary>
    /// <param name="tasks">Задачи.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(List<IAcquaintanceTask> tasks)
    {
      var addenda = new List<IElectronicDocument>();
      var addendaIds = tasks.SelectMany(x => x.AcquaintanceVersions)
        .Where(x => x.IsMainDocument != true)
        .Select(x => x.DocumentId);
      
      var documentAddenda = tasks.SelectMany(x => x.AddendaGroup.OfficialDocuments)
        .Where(x => addendaIds.Contains(x.Id))
        .Distinct()
        .ToList();
      addenda.AddRange(documentAddenda);

      return addenda;
    }
    
    /// <summary>
    /// Получить все приложения по задаче ознакомления с документом.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(IAcquaintanceTask task)
    {
      return this.GetAcquintanceTaskAddendas(new List<IAcquaintanceTask> { task });
    }
    
    /// <summary>
    /// Получить список всех получателей.
    /// </summary>
    /// <returns>Список всех получателей.</returns>
    [Obsolete, Public, Remote(IsPure = true)]
    public IQueryable<IRecipient> GetAllRecipients()
    {
      return Sungero.CoreEntities.Recipients.GetAll();
    }
    
    /// <summary>
    /// Получить значение поля Адресат в отчете Журнал исходящих документов.
    /// </summary>
    /// <param name="letterId">ИД исходящего письма.</param>
    /// <returns>Значение поля Адресат.</returns>
    [Public]
    public string GetOutgoingDocumentReportAddressee(int letterId)
    {
      var outgoingLetter = Sungero.Docflow.OutgoingDocumentBases.Get(letterId);
      if (outgoingLetter == null)
        return string.Empty;
      if (outgoingLetter.Addressees.Count < 5)
      {
        var addresseeList = new List<string>();
        foreach (var addressee in outgoingLetter.Addressees.OrderBy(a => a.Number))
        {
          var addresseeString = addressee.Addressee == null
            ? addressee.Correspondent.Name
            : string.Concat(addressee.Correspondent.Name, "\n", addressee.Addressee.Name);

          addresseeList.Add(addresseeString);
        }
        return string.Join("\n\n", addresseeList);
      }
      else
        return Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(
          OutgoingLetters.Resources.CorrespondentToManyAddressees.ToString().Trim());
    }
    
    /// <summary>
    /// Получить данные для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="actionItems">Поручения.</param>
    /// <param name="reportSessionId">Ид отчета.</param>
    /// <param name="textResolution">Текстовая резолюция.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> actionItems, string reportSessionId, string textResolution)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      if (actionItems.Any())
      {
        reportData = PublicFunctions.Module.GetDraftResolutionReportData(actionItems, reportSessionId);
      }
      else
      {
        // Если нет поручений, то берём текстовую резолюцию.
        reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
        var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
        data.ReportSessionId = reportSessionId;
        data.PerformersLabel = textResolution;
        reportData.Add(data);
      }
      return reportData;
    }
    
    /// <summary>
    /// Получить представление документа для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns> Представление.</returns>
    [Public]
    public virtual string GetDraftResolutionReportDocumentShortName(Docflow.IOfficialDocument document)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        if (document.RegistrationDate.HasValue)
          documentShortName += Docflow.OfficialDocuments.Resources.DateFrom + document.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format(" ({0} {1})", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
        else
          documentShortName += string.Format(" {0} {1}", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
      }
      return documentShortName;
    }
    
    /// <summary>
    /// Получить представление документа для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <returns>Представление.</returns>
    [Public]
    public virtual string GetActionItemPrintReportDocumentShortName(Docflow.IOfficialDocument document, Sungero.Workflow.IAssignment actionItem)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        // "К документу".
        documentShortName += Reports.Resources.ActionItemPrintReport.ToDocument;
        
        // Номер.
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        // Дата.
        if (document.RegistrationDate.HasValue)
          documentShortName += string.Format("{0}{1}", Docflow.OfficialDocuments.Resources.DateFrom, document.RegistrationDate.Value.ToString("d"));
        
        // ИД и разделитель /.
        documentShortName += string.Format(" ({0} {1}) / ", Reports.Resources.ActionItemPrintReport.DocumentID, document.Id.ToString());
      }
      
      // ИД поручения.
      documentShortName += string.Format("{0} {1}", Reports.Resources.ActionItemPrintReport.ActionItemID, actionItem.Id.ToString());
      
      return documentShortName;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters> GetActionItemPrintReportData(IActionItemExecutionTask actionItemTask, string reportId)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters>();
      
      // Контролёр.
      var supervisor = this.GetSupervisorInfoForActionItem(actionItemTask);
      // От кого.
      var fromAuthor = this.GetAuthorLineInfoForActionItem(actionItemTask);
      var actionItemText = string.Empty;
      IEmployee assignee = null;
      DateTime? deadline;
      DateTime? coAssigneesDeadline = null;
      var subAssignees = new List<IEmployee>() { };

      if (actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
      {
        var task = ActionItemExecutionTasks.As(actionItemTask.ParentTask);
        var part = task.ActionItemParts.Where(n => Equals(n.ActionItemPartExecutionTask, actionItemTask)).FirstOrDefault();
        actionItemText = string.Join("\r\n", task.ActiveText, part.ActionItemPart);
        assignee = actionItemTask.Assignee;
        deadline = part.Deadline ?? task.FinalDeadline ?? Calendar.Now;
        subAssignees = Functions.ActionItemExecutionTask.GetPartCoAssignees(task, part.PartGuid);
        coAssigneesDeadline = part.CoAssigneesDeadline;
      }
      else
      {
        var task = actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional ? ActionItemExecutionTasks.As(actionItemTask.ParentAssignment.Task) : actionItemTask;
        // Поручение с соисполнителями.
        actionItemText = task.ActiveText;
        if (task.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component &&
            task.ParentTask.ActiveText != task.ActiveText)
          actionItemText = string.Join("\r\n", task.ParentTask.ActiveText, task.ActiveText);
        
        subAssignees = task.CoAssignees.Select(a => a.Assignee).ToList();
        assignee = task.Assignee;
        deadline = actionItemTask.Deadline ?? Calendar.Now;
        if (!(actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional))
          coAssigneesDeadline = task.CoAssigneesDeadline;
      }
      
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(assignee, false);
      
      var formattedDeadline = string.Empty;
      if (deadline != null)
        formattedDeadline = deadline.Value.HasTime() ? deadline.Value.ToUserTime().ToString("g") : deadline.Value.ToString("d");
      if (actionItemTask.HasIndefiniteDeadline.Value)
        formattedDeadline = Resources.ActionItemIndefiniteDeadline;
      var formattedCoAssigneesDeadline = string.Empty;
      if (subAssignees != null && subAssignees.Any())
      {
        assigneeShortName = string.Format("<u>{0}</u>{1}{2}",
                                          assigneeShortName,
                                          Environment.NewLine,
                                          string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
        coAssigneesDeadline = coAssigneesDeadline ?? null;
        if (coAssigneesDeadline != null)
          formattedCoAssigneesDeadline = coAssigneesDeadline.Value.HasTime() ? coAssigneesDeadline.Value.ToUserTime().ToString("g") : coAssigneesDeadline.Value.ToString("d");
      }

      var data = this.GetActionItemPrintReportData(assigneeShortName, formattedDeadline, formattedCoAssigneesDeadline, fromAuthor, supervisor, actionItemText, reportId);
      reportData.Add(data);
      return reportData;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="assigneeShortName">Кому.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <param name="fromAuthor">От кого.</param>
    /// <param name="supervisor">Контролер.</param>
    /// <param name="actionItemText">Текст поручения.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Структура для отчета.</returns>
    [Public]
    public virtual Structures.ActionItemPrintReport.ActionItemPrintReportParameters GetActionItemPrintReportData(string assigneeShortName, string deadline, string coAssigneesDeadline, string fromAuthor, string supervisor,
                                                                                                                 string actionItemText, string reportId)
    {
      var data = new Structures.ActionItemPrintReport.ActionItemPrintReportParameters();
      data.ReportSessionId = reportId;
      data.Performer = assigneeShortName;
      data.Deadline = deadline;
      data.CoAssigneesDeadline = coAssigneesDeadline;
      data.ActionItemText = actionItemText;
      data.Supervisor = supervisor;
      data.FromAuthor = fromAuthor;
      
      return data;
    }
    
    /// <summary>
    /// Получить цепочку сотрудников, выдавших поручение.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о выдавших поручение.</returns>
    [Public]
    public virtual string GetAuthorLineInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var authorInfo = Company.PublicFunctions.Employee.GetShortName(Employees.As(actionItemTask.AssignedBy), false);
      var currentTask = Workflow.Tasks.As(actionItemTask);
      var parentTask = currentTask.ParentTask != null ? currentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      while (ActionItemExecutionTasks.As(parentTask) != null && currentTask != parentTask)
      {
        if (ActionItemExecutionTasks.As(currentTask).ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
          authorInfo = string.Format("{0} -> {1}", Company.PublicFunctions.Employee.GetShortName(Employees.As(ActionItemExecutionTasks.As(parentTask).AssignedBy), false), authorInfo);
        
        currentTask = parentTask;
        parentTask = parentTask.ParentTask != null ? parentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      }
      return authorInfo;
    }
    
    /// <summary>
    /// Создать параметры модуля.
    /// </summary>
    public virtual void CreateSettings()
    {
      var recordManagementSettings = RecordManagementSettings.Create();
      recordManagementSettings.Name = RecordManagementSettings.Info.LocalizedName;
      recordManagementSettings.AllowActionItemsWithIndefiniteDeadline = false;
      recordManagementSettings.AllowAcquaintanceBySubstitute = false;
      recordManagementSettings.ControlRelativeDeadlineInDays = 1;
      recordManagementSettings.Save();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    [Public, Remote]
    public virtual void ExecuteApplyActionItemLockIndependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, int actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges({0}): actionItemTaskId {0}", actionItemTaskId);
      var asyncChangeActionItem = RecordManagement.AsyncHandlers.ApplyActionItemLockIndependentChanges.Create();
      asyncChangeActionItem.ActionItemTaskId = actionItemTaskId;
      asyncChangeActionItem.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      asyncChangeActionItem.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      asyncChangeActionItem.OldAssignee = changes.OldAssignee?.Id ?? -1;
      asyncChangeActionItem.NewAssignee = changes.NewAssignee?.Id ?? -1;
      asyncChangeActionItem.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.EditingReason = changes.EditingReason;
      asyncChangeActionItem.AdditionalInfo = changes.AdditionalInfo;
      asyncChangeActionItem.OnEditGuid = onEditGuid;
      asyncChangeActionItem.InitiatorOfChange = changes.InitiatorOfChange.Id;
      asyncChangeActionItem.ChangeContext = changes.ChangeContext;
      asyncChangeActionItem.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать и выполнить асинхронное событие изменения поручения.
    /// </summary>
    /// <param name="changes">Изменения.</param>
    /// <param name="actionItemTaskId">Ид задачи.</param>
    /// <param name="onEditGuid">Guid поручения.</param>
    public virtual void ExecuteApplyActionItemLockDependentChanges(RecordManagement.Structures.ActionItemExecutionTask.IActionItemChanges changes, int actionItemTaskId, string onEditGuid)
    {
      Logger.DebugFormat("ApplyActionItemLockDependentChanges({0}): actionItemTaskId {0}", actionItemTaskId);
      var asyncChangeActionItem = RecordManagement.AsyncHandlers.ApplyActionItemLockDependentChanges.Create();
      asyncChangeActionItem.ActionItemTaskId = actionItemTaskId;
      asyncChangeActionItem.OldSupervisor = changes.OldSupervisor?.Id ?? -1;
      asyncChangeActionItem.NewSupervisor = changes.NewSupervisor?.Id ?? -1;
      asyncChangeActionItem.OldAssignee = changes.OldAssignee?.Id ?? -1;
      asyncChangeActionItem.NewAssignee = changes.NewAssignee?.Id ?? -1;
      asyncChangeActionItem.OldDeadline = changes.OldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.NewDeadline = changes.NewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.OldCoAssignees = string.Join(",", changes.OldCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.NewCoAssignees = string.Join(",", changes.NewCoAssignees.Select(x => x.Id).ToList());
      asyncChangeActionItem.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline ?? DateTime.MinValue;
      asyncChangeActionItem.EditingReason = changes.EditingReason;
      asyncChangeActionItem.AdditionalInfo = changes.AdditionalInfo;
      asyncChangeActionItem.OnEditGuid = onEditGuid;
      asyncChangeActionItem.InitiatorOfChange = changes.InitiatorOfChange.Id;
      asyncChangeActionItem.ChangeContext = changes.ChangeContext;
      asyncChangeActionItem.ExecuteAsync();
    }

    /// <summary>
    /// Проверить, что по поручению уже созданы все актуальные задания, и его можно корректировать.
    /// </summary>
    /// <param name="tasks">Список задач.</param>
    /// <returns>Текст ошибки, если задания не созданы. Иначе пустую строку.</returns>
    public virtual string CheckActionItemAssignmentsCreated(List<IActionItemExecutionTask> tasks)
    {
      var error = ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;

      // По простому поручению не созданы подзадачи соисполнителям.
      if (tasks.Any(t => t.CoAssignees.Any(ca => ca.AssignmentCreated != true)))
        return error;

      // По составному поручению не созданы подзадачи по пунктам.
      if (tasks.Any(t => t.ActionItemParts.Any(aip => aip.AssignmentCreated != true)))
        return error;
      
      // По составному поручению не созданы подзадачи соисполнителям.
      if (tasks.Any(t => t.ActionItemParts.Any(aip => aip.ActionItemPartExecutionTask == null ||
                                               (aip.ActionItemPartExecutionTask.Status == Sungero.Workflow.Task.Status.InProcess &&
                                                aip.ActionItemPartExecutionTask.CoAssignees.Any(ca => ca.AssignmentCreated != true)))))
        return error;

      var onExecutionTasksIds = tasks
        .Where(j => j.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnExecution ||
               j.ExecutionState == RecordManagement.ActionItemExecutionTask.ExecutionState.OnRework)
        .Select(t => t.Id)
        .ToList();
      
      // Проверить для каждой задачи, что поручение на исполнении и есть задания на исполнение в работе.
      var executionAssignmentsCount = ActionItemExecutionAssignments.GetAll()
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => onExecutionTasksIds.Contains(j.Task.Id))
        .Where(j => Equals(j.Performer, ActionItemExecutionTasks.As(j.Task).Assignee))
        .Where(j => j.TaskStartId == j.Task.StartId)
        .Count();

      if (executionAssignmentsCount != onExecutionTasksIds.Count)
        return error;

      var tasksIds = tasks.Select(t => t.Id).ToList();
      var actionItemExecutionTasksInProcess = ActionItemExecutionTasks.GetAll()
        .Where(t => t.Status == Workflow.Task.Status.InProcess);
      
      // В задачах соисполнителю должно быть хотя бы одно задание.
      var coAssigneeTasks = actionItemExecutionTasksInProcess
        .Where(t => t.ParentAssignment != null && tasksIds.Contains(t.ParentAssignment.Task.Id))
        .Where(t => t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
      var allAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(coAssigneeTasks);
      
      if (!allAssignmentsStarted)
        return error;
      
      // В пунктах составного поручения должно быть хотя бы одно задание.
      var compoundActionItemTasks = actionItemExecutionTasksInProcess
        .Where(t => tasksIds.Contains(t.ParentTask.Id) && t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Component);
      var allCompoundAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(compoundActionItemTasks);
      
      if (!allCompoundAssignmentsStarted)
        return error;
      
      // В задачах соисполнителям пунктов составного поручения должно быть хотя бы одно задание.
      var compoundActionItemCoAssigneeTasks = actionItemExecutionTasksInProcess
        .Where(t => t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional &&
               t.ParentAssignment != null && tasksIds.Contains(t.ParentAssignment.Task.ParentTask.Id));
      var allCompoundCoAssigneeAssignmentsStarted = this.CheckAllAssignmentsOnTasksStarted(compoundActionItemCoAssigneeTasks);
      
      if (!allCompoundCoAssigneeAssignmentsStarted)
        return error;

      return null;
    }
    
    /// <summary>
    /// Проверить, что поручение (в том числе подпоручения соисполнителям, пункты составного и подпоручения соисполнителям пунктов)
    /// не корректируется в текущий момент.
    /// </summary>
    /// <param name="tasks">Список задач.</param>
    /// <returns>Текст ошибки, если корректируется. Иначе пустую строку.</returns>
    public virtual string CheckActionItemNotInChangingProcess(List<IActionItemExecutionTask> tasks)
    {
      // Проверить пункты составного поручения, подпоручения соисполнителям пунктов и подпоручения соисполнителям текущих поручений.
      var tasksToCheck = tasks
        .SelectMany(t => t.ActionItemParts)
        .Where(x => x.ActionItemPartExecutionTask != null)
        .Where(x => x.ActionItemPartExecutionTask.Status == Sungero.Workflow.Task.Status.InProcess)
        .Select(x => x.ActionItemPartExecutionTask)
        .ToList();
      
      tasksToCheck.AddRange(tasks);
      
      // Проверить пункты составного поручения и текущие поручения.
      if (tasksToCheck.Any(x => (x.OnEditGuid ?? string.Empty) != string.Empty))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить подпоручения соисполнителям пунктов и подпоручения соисполнителям текущих поручений.
      var tasksToCheckIds = tasksToCheck.Select(t => t.Id).ToList();
      if (!this.CheckCoAssigneeActionItemsNotInChangingProcess(tasksToCheckIds))
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      // Проверить главные составные поручения, если корректируется пункт.
      if (tasks.Where(t => t.ActionItemType == ActionItemType.Component).Any())
      {
        var mainActionItemsIds = tasks.Where(t => t.ParentTask != null).Select(t => t.ParentTask.Id).ToList();
        var mainActionItemNotInChangingProcessErrorText = this.CheckCurrentActionItemNotInChangingProcess(mainActionItemsIds);
        if (!string.IsNullOrEmpty(mainActionItemNotInChangingProcessErrorText))
          return mainActionItemNotInChangingProcessErrorText;
      }
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что подпоручения соисполнителям не корректируются в текущий момент.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>True - ни одно из подпоручений не корректируется.
    /// False - часть подпоручений корректируются.</returns>
    public virtual bool CheckCoAssigneeActionItemsNotInChangingProcess(List<int> tasksIds)
    {
      var executionAssignmentIds = ActionItemExecutionAssignments.GetAll()
        .Where(j => tasksIds.Contains(j.Task.Id))
        .Where(j => Equals(j.Performer, ActionItemExecutionTasks.As(j.Task).Assignee))
        .Where(j => j.TaskStartId == j.Task.StartId)
        .OrderByDescending(j => j.Created)
        .Select(j => j.Id)
        .ToList();
      
      var anyCoAssigneesTasksOnEdit = ActionItemExecutionTasks.GetAll()
        .Where(t => t.ActionItemType == ActionItemType.Additional)
        .Where(t => t.ParentAssignment != null)
        .Where(t => executionAssignmentIds.Contains(t.ParentAssignment.Id))
        .Where(t => (t.OnEditGuid ?? string.Empty) != string.Empty)
        .Any();
      
      return !anyCoAssigneesTasksOnEdit;
    }
    
    /// <summary>
    /// Проверить, что поручение не корректируется в текущий момент.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>Текст ошибки, если корректируется. Иначе пустую строку.</returns>
    public virtual string CheckCurrentActionItemNotInChangingProcess(List<int> tasksIds)
    {
      var onEdit = ActionItemExecutionTasks.GetAll().Where(a => tasksIds.Contains(a.Id))
        .Any(a => (a.OnEditGuid ?? string.Empty) != string.Empty);
      if (onEdit)
        return ActionItemExecutionTasks.Resources.ActionItemIsAlreadyInChangingProcess;
      
      return null;
    }
    
    /// <summary>
    /// Проверить, что у всех поручений есть как минимум одно стартованное задание.
    /// </summary>
    /// <param name="tasks">Поручения.</param>
    /// <returns>True, если у всех поручений есть задания. Иначе False.</returns>
    public virtual bool CheckAllAssignmentsOnTasksStarted(IQueryable<IActionItemExecutionTask> tasks)
    {
      var tasksIds = tasks.Select(t => t.Id).ToList();
      if (tasksIds.Count == 0)
        return true;
      
      var tasksCount = Assignments.GetAll().Where(a => tasksIds.Contains(a.Task.Id)).Select(a => a.Task.Id).Distinct().Count();
      if (tasksIds.Count != tasksCount)
        return false;
      
      return true;
    }
    
    /// <summary>
    /// Скопировать изменения в поручении в новый экземпляр структуры.
    /// </summary>
    /// <param name="changes">Изменения в поручении.</param>
    /// <returns>Скопированные изменения.</returns>
    public virtual Structures.ActionItemExecutionTask.IActionItemChanges CopyActionItemChangesStructure(Structures.ActionItemExecutionTask.IActionItemChanges changes)
    {
      var copiedChanges = Structures.ActionItemExecutionTask.ActionItemChanges.Create();
      copiedChanges.OldSupervisor = changes.OldSupervisor;
      copiedChanges.NewSupervisor = changes.NewSupervisor;
      copiedChanges.OldAssignee = changes.OldAssignee;
      copiedChanges.NewAssignee = changes.NewAssignee;
      copiedChanges.OldDeadline = changes.OldDeadline;
      copiedChanges.NewDeadline = changes.NewDeadline;
      copiedChanges.OldCoAssignees = changes.OldCoAssignees;
      copiedChanges.NewCoAssignees = changes.NewCoAssignees;
      copiedChanges.CoAssigneesOldDeadline = changes.CoAssigneesOldDeadline;
      copiedChanges.CoAssigneesNewDeadline = changes.CoAssigneesNewDeadline;
      copiedChanges.EditingReason = changes.EditingReason;
      copiedChanges.AdditionalInfo = changes.AdditionalInfo;
      copiedChanges.TaskIds = changes.TaskIds;
      copiedChanges.ActionItemPartsText = changes.ActionItemPartsText;
      copiedChanges.InitiatorOfChange = changes.InitiatorOfChange;
      copiedChanges.ChangeContext = changes.ChangeContext;
      
      return copiedChanges;
    }
    
    /// <summary>
    /// Проверить, что ни одно поручение не было изменено с момента указанной даты.
    /// </summary>
    /// <param name="tasksIds">Список Id поручений.</param>
    /// <param name="lastActionItemChangeDate">Дата последнего изменения поручений.</param>
    /// <returns>Текст ошибки, если хотя бы одно поручение было изменено. Иначе null.</returns>
    public virtual string CheckActionItemNotChanged(List<int> tasksIds, DateTime? lastActionItemChangeDate)
    {
      if (!lastActionItemChangeDate.HasValue)
        return null;
      
      var actualLastActionItemChangeDate = Functions.Module.GetLastActionItemChangeDate(tasksIds);
      Logger.DebugFormat("lastActionItemChangeDate: {0}, actualLastActionItemChangeDate {1}", lastActionItemChangeDate, actualLastActionItemChangeDate);
      if (lastActionItemChangeDate < actualLastActionItemChangeDate)
        return RecordManagement.ActionItemExecutionTasks.Resources.ActionItemWasChanged;
      
      return null;
    }
    
    /// <summary>
    /// Получить максимальную дату последнего изменения поручений из списка.
    /// </summary>
    /// <param name="tasksIds">Список Id задач.</param>
    /// <returns>Максимальная дата последнего изменения поручений из списка.</returns>
    public virtual DateTime? GetLastActionItemChangeDate(List<int> tasksIds)
    {
      return ActionItemExecutionTasks.GetAll()
        .Where(t => tasksIds.Contains(t.Id))
        .Select(t => t.Modified)
        .OrderByDescending(t => t)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить активные задания на ознакомление.
    /// </summary>
    /// <param name="assignmentsIds">ИД заданий на ознакомление, записанные в виде строки через запятую.</param>
    /// <returns>Задания на ознакомление.</returns>
    public virtual List<IAcquaintanceAssignment> GetActiveAcquaintanceAssignments(string assignmentsIds)
    {
      var splittedAssignmentsIds = assignmentsIds.Split(',');
      var assignments = AcquaintanceAssignments.GetAll()
        .Where(x => splittedAssignmentsIds.Contains(x.Id.ToString()))
        .Where(x => x.Status != Workflow.Assignment.Status.Completed &&
               x.Status != Workflow.Assignment.Status.Aborted)
        .ToList();
      return assignments;
    }
  }
  
}
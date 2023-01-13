using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.RecordManagement.ReviewManagerAssignment;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewTaskRouteHandlers
  {

    #region 30. Удаление проектов резолюции
    
    public virtual void Script30Execute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Script30Execute", _obj.Id);
      
      // Удалить проекты резолюции, если в "Выдал" указан не адресат.
      Functions.DocumentReviewTask.DeleteDraftActionItems(_obj);
    }
    
    #endregion
    
    #region 27. Задание на доработку рассмотрения
    
    public virtual void StartBlock27(Sungero.RecordManagement.Server.ReviewReworkAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock27", _obj.Id);
      
      // Добавить инициатора в качестве исполнителя.
      e.Block.Performers.Add(_obj.Author);
      
      // Вычислить дедлайн задания - 4 часа.
      e.Block.RelativeDeadlineHours = 4;
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.NeedToRework, document.Name);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
      
      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
    }
    
    public virtual void StartAssignment27(Sungero.RecordManagement.IReviewReworkAssignment assignment, Sungero.RecordManagement.Server.ReviewReworkAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartAssignment27", _obj.Id);
      
      var lastAssignmentSentForRework = Functions.DocumentReviewTask.GetLastAssignmentSentForRework(_obj);
      if (lastAssignmentSentForRework != null)
        assignment.Author = lastAssignmentSentForRework.Performer;
    }
    
    public virtual void CompleteAssignment27(Sungero.RecordManagement.IReviewReworkAssignment assignment, Sungero.RecordManagement.Server.ReviewReworkAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) CompleteAssignment27", _obj.Id);
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == RecordManagement.ReviewReworkAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      
      // Заполнить коллекции добавленных и удаленных вручную документов в задаче.
      Functions.DocumentReviewTask.AddedAddendaAppend(_obj);
      Functions.DocumentReviewTask.RemovedAddendaAppend(_obj);
    }
    
    public virtual void EndBlock27(Sungero.RecordManagement.Server.ReviewReworkAssignmentEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock27", _obj.Id);
    }
    
    #endregion
    
    #region 26. Удаление ненужных проектов резолюции
    
    public virtual void Script26Execute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Script26Execute", _obj.Id);
      
      // Удалить проекты резолюции, если их создал не помощник одного из адресатов или его замещающий.
      if (_obj.NeedDeleteActionItems == true)
      {
        Logger.DebugFormat("DocumentReviewTask({0}) Script26Execute NeedDeleteActionItems", _obj.Id);
        Functions.DocumentReviewTask.DeleteDraftActionItems(_obj);
      }
    }
    
    #endregion
    
    #region 20. Указан один адресат?
    
    public virtual bool Decision20Result()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Decision20Result ({1})", _obj.Id, _obj.Addressees.Count == 1);
      return _obj.Addressees.Count == 1;
    }
    
    #endregion
    
    #region 21. Созданы ли всем адресатам задачи на рассмотрение?
    
    public virtual bool Decision21Result()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Decision21Result ({1})", _obj.Id, _obj.Addressees.All(x => x.TaskCreated == true));
      return _obj.Addressees.All(x => x.TaskCreated == true);
    }
    
    #endregion
    
    #region 22. Подзадачи на рассмотрение
    
    public virtual void Script22Execute()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Script22Execute", _obj.Id);
      
      var documentReviewTask = DocumentReviewTasks.CreateAsSubtask(_obj);
      Logger.DebugFormat("DocumentReviewTask({0}) Script22Execute Create as subtask {1}", _obj.Id, documentReviewTask.Id);
      
      documentReviewTask.Importance = _obj.Importance;
      Functions.Module.SynchronizeAttachmentsToDocumentReview(_obj, documentReviewTask);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      
      // Задать тему, текст задачи.
      documentReviewTask.Subject = string.Format(">> {0}", _obj.Subject);
      documentReviewTask.ThreadSubject = _obj.ThreadSubject;
      documentReviewTask.ActiveText = _obj.ActiveText;
      
      // Задать адресата и инициатора.
      // Очистить Адресатов, которые могли заполниться из документа.
      documentReviewTask.Addressees.Clear();
      var addressee = _obj.Addressees.FirstOrDefault(t => t.TaskCreated != true);
      var newAddressee = documentReviewTask.Addressees.AddNew();
      newAddressee.Addressee = addressee.Addressee;
      documentReviewTask.Author = _obj.Author;
      
      // Синхронизировать вложенные проекты резолюции.
      var canAuthorPrepareResolution = Functions.DocumentReviewTask.CanAuthorPrepareResolution(documentReviewTask);
      foreach (var resolution in _obj.ResolutionGroup.ActionItemExecutionTasks)
      {
        if (canAuthorPrepareResolution && documentReviewTask.Addressees.Any(x => Equals(resolution.AssignedBy, x.Addressee)))
          documentReviewTask.ResolutionGroup.ActionItemExecutionTasks.Add(resolution);
      }
      
      // Задать срок.
      documentReviewTask.Deadline = _obj.Deadline;
      documentReviewTask.MaxDeadline = _obj.MaxDeadline;
      
      documentReviewTask.Start();
      Logger.DebugFormat("DocumentReviewTask({0}) Script22Execute Subtask {1} started", _obj.Id, documentReviewTask.Id);
      
      addressee.TaskCreated = true;
    }
    
    #endregion
    
    #region 23. Мониторинг завершения рассмотрения
    
    public virtual void StartBlock23(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      e.Block.Period = TimeSpan.FromHours(Constants.DocumentReviewTask.CheckCompletionMonitoringPeriodInHours);
    }
    
    public virtual bool Monitoring23Result()
    {
      var result = Functions.DocumentReviewTask.AllDocumentReviewSubTasksAreCompleted(_obj);
      Logger.DebugFormat("DocumentReviewTask({0}) Monitoring23Result ({1})", _obj.Id, result);
      return result;
    }
    
    #endregion
    
    #region 17. Задачу создал помощник и она не переадресована?
    
    public virtual bool Decision17Result()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Decision17Result", _obj.Id);
      
      var lastAssignment = Assignments.GetAll(x => Equals(x.Task, _obj)).OrderByDescending(x => x.Created).FirstOrDefault();
      var isForwarded = lastAssignment != null &&
        (lastAssignment.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.Forward ||
         lastAssignment.Result == Sungero.RecordManagement.ReviewReworkAssignment.Result.Forward);
      
      var isRework = Sungero.RecordManagement.ReviewReworkAssignments.Is(lastAssignment);
      var canAuthorPrepareResolution = Functions.DocumentReviewTask.CanAuthorPrepareResolution(_obj);
      
      var result = (!isForwarded || isForwarded && isRework) && canAuthorPrepareResolution;
      Logger.DebugFormat("DocumentReviewTask({0}) Decision17Result {1}", _obj.Id, result);
      return result;
    }
    
    #endregion
    
    #region 15. Проект резолюции уже подготовлен?
    
    public virtual bool Decision15Result()
    {
      Logger.DebugFormat("DocumentReviewTask({0}) Decision15Result ({1})", _obj.Id, _obj.ResolutionGroup.ActionItemExecutionTasks.Any());
      return _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }
    
    #endregion
    
    #region 10. Помощник готовит проект резолюции?
    
    public virtual bool Decision10Result()
    {
      var addressee = Employees.As(_obj.Addressee);
      var result = Company.PublicFunctions.Employee.GetManagerAssistantsWhoPrepareDraftResolution(addressee).Any();
      Logger.DebugFormat("DocumentReviewTask({0}) Decision10Result {1}", _obj.Id, result);
      return result;
    }
    
    #endregion
    
    #region 9. Уведомление наблюдателям

    public virtual void StartBlock9(Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock9", _obj.Id);
      
      // Добавить наблюдателей задачи в качестве исполнителей уведомления.
      foreach (var observer in _obj.ResolutionObservers)
        e.Block.Performers.Add(observer.Observer);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      var subject = DocumentReviewTasks.Resources.DocumentConsiderationStartedFormat(document.Name);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);

      // Выдать наблюдателям права на вложения.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
      Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.DocumentForReviewGroup.All.Concat(_obj.AddendaGroup.All).ToList(),
                                                                                                 e.Block.Performers);
    }

    public virtual void StartNotice9(Sungero.RecordManagement.IReviewObserversNotification notice, Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartNotice9", _obj.Id);
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewBeginingNoticeThreadSubject;
    }

    public virtual void EndBlock9(Sungero.RecordManagement.Server.ReviewObserversNotificationEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock9", _obj.Id);
    }
    
    #endregion

    #region 2. Рассмотрение руководителем
    
    public virtual void StartBlock2(Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock2", _obj.Id);
      
      // Добавить адресата в качестве исполнителя.
      e.Block.Performers.Add(_obj.Addressee);
      
      // Установить срок и тему.
      if (_obj.Deadline.HasValue && _obj.Started.HasValue)
      {
        var deadline = Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Author);
        var deadlineInHour = WorkingTime.GetDurationInWorkingHours(_obj.Started.Value, deadline, _obj.Author);
        Logger.DebugFormat("DocumentReviewTask({0}) deadline in author timezone: {1}", _obj.Id, _obj.Deadline.Value.ToUserTime(_obj.Author));
        Logger.DebugFormat("DocumentReviewTask({0}) deadline in performer timezone: {1}", _obj.Id, _obj.Deadline.Value.ToUserTime(_obj.Addressee));
        e.Block.RelativeDeadlineHours = deadlineInHour > 0 ? deadlineInHour : 1;
      }
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReviewDocument, document.Name);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }

    public virtual void StartAssignment2(Sungero.RecordManagement.IReviewManagerAssignment assignment, Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartAssignment2", _obj.Id);
      
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnReview);
      Functions.Module.SetDocumentControlExecutionState(document);
    }

    public virtual void CompleteAssignment2(Sungero.RecordManagement.IReviewManagerAssignment assignment, Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) CompleteAssignment2 ({1})", _obj.Id, assignment.Result);
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Result.AddResolution)
        _obj.ResolutionText = assignment.ActiveText;
      
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Result.Explored)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      // Обновить статус исполнения - на исполнении.
      if (assignment.Result == Result.AddAssignment)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnExecution);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
    }

    public virtual void EndBlock2(Sungero.RecordManagement.Server.ReviewManagerAssignmentEndBlockEventArguments e)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(e.CreatedAssignments.Select(a => a.Id).ToList());
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock2", _obj.Id);
    }
    
    #endregion
    
    #region 3. Уведомление наблюдателям

    public virtual void StartBlock3(Sungero.RecordManagement.Server.ReviewObserverNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock3", _obj.Id);
      
      // Добавить наблюдателей задачи в качестве исполнителей уведомления.
      foreach (var observer in _obj.ResolutionObservers)
        e.Block.Performers.Add(observer.Observer);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);

      // Выдать наблюдателям права на вложения.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
      Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
    }

    public virtual void StartNotice3(Sungero.RecordManagement.IReviewObserverNotification notice, Sungero.RecordManagement.Server.ReviewObserverNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartNotice3", _obj.Id);
      
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }

    public virtual void EndBlock3(Sungero.RecordManagement.Server.ReviewObserverNotificationEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock3", _obj.Id);
    }
    
    #endregion
    
    #region 4. Уведомление делопроизводителю

    public virtual void StartBlock4(Sungero.RecordManagement.Server.ReviewClerkNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock4", _obj.Id);
      
      // Отправляется только в случае, если руководитель выполнил задание с результатом "Ознакомлен", "Отправлено на исполнение".
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result != RecordManagement.ReviewManagerAssignment.Result.AddResolution && result != RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
      {
        e.Block.Performers.Add(_obj.Author);
        
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
        
        Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
        Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);

        // Выдать наблюдателям права на вложения.
        Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
        Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
      }
    }

    public virtual void StartNotice4(Sungero.RecordManagement.IReviewClerkNotification notice, Sungero.RecordManagement.Server.ReviewClerkNotificationArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartNotice4", _obj.Id);
      
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }

    public virtual void EndBlock4(Sungero.RecordManagement.Server.ReviewClerkNotificationEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock4", _obj.Id);
    }
    
    #endregion
    
    #region 19. Уведомление инициатору
    
    public virtual void StartBlock19(Sungero.Workflow.Server.NoticeArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock19", _obj.Id);
      
      // Отправляется только в случае, если руководитель выполнил задание с результатом "Вынесена резолюция".
      // И поручение создает не инициатор.
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      
      if ((result == RecordManagement.ReviewManagerAssignment.Result.AddResolution || result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution) &&
          Functions.DocumentReviewTask.GetClerkToSendActionItem(_obj) != _obj.Author)
      {
        e.Block.Performers.Add(_obj.Author);
        
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
        
        Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
        Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);

        // Выдать наблюдателям права на вложения.
        Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
        Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
      }
      
    }
    
    public virtual void StartNotice19(Sungero.Workflow.INotice notice, Sungero.Workflow.Server.NoticeArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartNotice19", _obj.Id);
      
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }
    
    public virtual void EndBlock19(Sungero.Workflow.Server.NoticeEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock19", _obj.Id);
    }
    
    #endregion
    
    #region 5. Делопроизводителю требуется создать поручения?

    public virtual bool Decision5Result()
    {
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      return result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution ||
        result == RecordManagement.ReviewManagerAssignment.Result.AddResolution;
    }
    
    #endregion

    #region 6. Создание поручения делопроизводителем
    
    public virtual void StartBlock6(Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Performers.Add(Functions.DocumentReviewTask.GetClerkToSendActionItem(_obj));
      
      // Тема.
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.CreateAssignment, document.Name);
      
      // Установить срок на оформление поручений 4 часа.
      e.Block.RelativeDeadlineHours = 4;
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);

      // Исключаем автора так как требуется не повышать ему права (147926).
      var performers = e.Block.Performers.Where(x => !Equals(x, _obj.Author)).ToList();
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, performers);
    }

    public virtual void StartAssignment6(Sungero.RecordManagement.IReviewResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      assignment.ResolutionText = _obj.ResolutionText;
      
      // Установить "От" как исполнителя рассмотрения.
      assignment.Author = _obj.Addressee;
      
      // Обновить статус исполнения - отправка на исполнение.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.Sending);
      Functions.Module.SetDocumentControlExecutionState(document);
    }

    public virtual void CompleteAssignment6(Sungero.RecordManagement.IReviewResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      // Если поручения не созданы, то изменить статус исполнения - не требует исполнения.
      if (!ActionItemExecutionTasks.GetAll(t => t.Status == Workflow.Task.Status.InProcess && Equals(t.ParentAssignment, assignment)).Any())
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      else
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnExecution);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
    }

    public virtual void EndBlock6(Sungero.RecordManagement.Server.ReviewResolutionAssignmentEndBlockEventArguments e)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(e.CreatedAssignments.Select(a => a.Id).ToList());
    }
    
    #endregion
    
    #region 11. Создание и доработка проектов резолюций
    
    public virtual void StartBlock11(Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock11", _obj.Id);
      
      var addressee = Employees.As(_obj.Addressee);
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      // Добавить помощника адресата в качестве исполнителя.
      e.Block.Performers.Add(assistant);
      
      // Вычислить дедлайн задания.
      // На подготовку проекта резолюции 4 часа.
      e.Block.RelativeDeadlineHours = 4;
      
      // Проставляем признак того, что задание для доработки.
      var lastReview = Assignments
        .GetAll(a => Equals(a.Task, _obj) && Equals(a.TaskStartId, _obj.StartId))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      if (lastReview != null && ReviewDraftResolutionAssignments.Is(lastReview) &&
          lastReview.Result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        e.Block.IsRework = true;
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      var addresseeShortName = Company.PublicFunctions.Employee.GetShortName(addressee, false);
      if (result != RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.PrepareDraftResolutionFormat(document.Name,
                                                                                                                                       addresseeShortName));
      else
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReworkPrepareDraftResolutionFormat(document.Name,
                                                                                                                                             addresseeShortName));
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
      
      // Выдать права помощнику руководителя, чтобы он мог удалять приложения в задании на подготовку/доработку проекта резолюции.
      Functions.DocumentReviewTask.GrantRightsOnTaskForSecretary(_obj);
    }

    public virtual void StartAssignment11(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartAssignment11", _obj.Id);
      
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnReview);
      Functions.Module.SetDocumentControlExecutionState(document);
      
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
      {
        assignment.ThreadSubject = Sungero.RecordManagement.DocumentReviewTasks.Resources.ReworkDraftResolutionThreadSubject;
        assignment.Author = _obj.Addressee;
      }
    }

    public virtual void CompleteAssignment11(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) CompleteAssignment11", _obj.Id);
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      
      // Удалить проект резолюции.
      if (assignment.NeedDeleteActionItems == true)
      {
        // Для всех адресатов.
        if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored)
          Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
        // Для неактуальных адресатов.
        else if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.SendForReview)
          Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks.Where(x => _obj.Addressees.All(a => !Equals(a.Addressee, x.AssignedBy))).ToList());
      }
      
      // Обновить статус исполнения - не требует исполнения.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      // Заполнить коллекции добавленных и удаленных вручную документов в задаче.
      Functions.DocumentReviewTask.AddedAddendaAppend(_obj);
      Functions.DocumentReviewTask.RemovedAddendaAppend(_obj);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }

    public virtual void EndBlock11(Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentEndBlockEventArguments e)
    {
      Docflow.PublicFunctions.Module.ExecuteWaitAssignmentMonitoring(e.CreatedAssignments.Select(a => a.Id).ToList());
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock11", _obj.Id);
    }
    
    #endregion
    
    #region 12. Рассмотрение руководителем проектов резолюций
    
    public virtual void StartBlock12(Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartBlock12", _obj.Id);
      
      // Добавить адресата в качестве исполнителя.
      e.Block.Performers.Add(_obj.Addressee);
      
      // Установить срок и тему.
      if (_obj.Deadline.HasValue && _obj.Started.HasValue)
      {
        var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Author);
        var deadlineInHour = WorkingTime.GetDurationInWorkingHours(_obj.Started.Value, deadline, _obj.Author);
        e.Block.RelativeDeadlineHours = deadlineInHour > 0 ? deadlineInHour : 1;
      }
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReviewDocument, document.Name);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      Functions.DocumentReviewTask.RelateAddedAddendaToPrimaryDocument(_obj);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }
    
    public virtual void StartAssignment12(Sungero.RecordManagement.IReviewDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) StartAssignment12", _obj.Id);
      
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnReview);
      Functions.Module.SetDocumentControlExecutionState(document);
    }
    
    public virtual void CompleteAssignment12(Sungero.RecordManagement.IReviewDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) CompleteAssignment12", _obj.Id);
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        _obj.ResolutionText = assignment.ActiveText;
      
      // Обновить статус исполнения - на исполнении.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.OnExecution);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Informed)
      {
        Functions.Module.SetDocumentExecutionState(_obj, document, ExecutionState.WithoutExecut);
        Functions.Module.SetDocumentControlExecutionState(document);
      }
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      
      // Удалить проект резолюции для предыдущего адресата.
      if (assignment.NeedDeleteActionItems == true)
      {
        var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
        _obj.ResolutionGroup.ActionItemExecutionTasks.Clear();
        Functions.Module.DeleteActionItemExecutionTasks(actionItems);
      }
    }
    
    public virtual void EndBlock12(Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentEndBlockEventArguments e)
    {
      Logger.DebugFormat("DocumentReviewTask({0}) EndBlock12", _obj.Id);
    }
    
    #endregion
    
    #region 13. Уведомление помощнику руководителя
    
    public virtual void StartBlock13(Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      var addressee = Employees.As(_obj.Addressee);
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      // Добавить помощника в качестве исполнителя, если он не делопроизводитель.
      if (!Equals(assistant, _obj.Author))
        e.Block.Performers.Add(assistant);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      if (document.ExecutionState == ExecutionState.OnExecution)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
      else if (document.ExecutionState == ExecutionState.WithoutExecut)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ManagerIsInformed, document.Name);
      
      Functions.DocumentReviewTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.DocumentReviewTask.SynchronizeAddendaToDraftResolution(_obj);
      
      // Выдать помощнику права на вложения.
      Logger.DebugFormat("DocumentReviewTask({0}). GrantReadAccessRightsForAttachments.", _obj.Id);
      Docflow.PublicFunctions.Module.GrantReadAccessRightsForAttachmentsConsideringCurrentRights(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
    }
    
    public virtual void StartNotice13(Sungero.RecordManagement.IReviewObserversNotification notice, Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }
    
    public virtual void EndBlock13(Sungero.RecordManagement.Server.ReviewObserversNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 8. Конец

    public virtual void StartReviewAssignment8(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion
    
  }
}
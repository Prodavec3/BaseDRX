using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewTaskStage;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.RecordManagement;

namespace Sungero.Docflow.Server
{
  partial class ApprovalReviewTaskStageFunctions
  {
    /// <summary>
    /// Запуск задачи на рассмотрение документа в процессе согласования по регламенту.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Результат выполнения кода.</returns>
    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(IApprovalTask approvalTask)
    {
      Logger.DebugFormat("ApprovalReviewTaskStage. Start execute approval stage for document review, approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                         approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
      
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (document == null)
      {
        Logger.ErrorFormat("ApprovalReviewTaskStage. Primary document not found. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}).",
                           approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetErrorResult(Docflow.Resources.PrimaryDocumentNotFoundError);
      }
      
      var addressees = approvalTask.Addressees.Select(x => x.Addressee).ToList();
      if (!addressees.Any())
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Addressees is empty. Approval task (ID={0}) (StartId={2}) (Iteration={3}) (StageNumber={4}).",
                           approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetErrorResult(ApprovalReviewTaskStages.Resources.ApprovalTaskAddresseesIsEmpty);
      }
      
      if (Locks.GetLockInfo(document).IsLockedByOther)
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Document locked. Approval task (ID={0}), Document (ID={1}), Locked By ({2}).",
                           approvalTask.Id, document.Id, Locks.GetLockInfo(document).LoginId);
        return this.GetRetryResult(string.Empty);
      }
      
      var reviewTask = RecordManagement.PublicFunctions.Module.Remote.CreateDocumentReviewTask(document, approvalTask, addressees);
      try
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Start review task (ID={0}), approval task (ID={1}) (StartId={2}) (Iteration={3}) (StageNumber={4}) for document (ID={5}).",
                           reviewTask.Id, approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber, document.Id);
        // Указывать автором рассмотрения инициатора согласования (Author).
        reviewTask.Author = approvalTask.Author;
        Sungero.RecordManagement.PublicFunctions.Module.SynchronizeAttachmentsToDocumentReview(approvalTask, reviewTask);
        this.SetDocumentReviewTaskDeadline(reviewTask);
        reviewTask.Start();
        LinkApprovalTaskAndDocumentReviewTask(approvalTask, reviewTask);
        this.SetMemoInternalApprovalStateOnDocumentReviewTaskStarted(approvalTask);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ApprovalReviewTaskStage. Review task start error. Review task (ID={0}), approval task (ID={1}) (StartId={2}) (Iteration={3}) (StageNumber={4}) for document (ID={5})",
                           ex, reviewTask.Id, approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber, document.Id);
        return this.GetRetryResult(string.Empty);
      }
      
      return this.GetSuccessResult();
    }
    
    /// <summary>
    /// Проверить состояние задачи на рассмотрение документа.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Состояние этапа.</returns>
    public override Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult CheckCompletionState(IApprovalTask approvalTask)
    {
      var result = base.CheckCompletionState(approvalTask);
      var link = GetApprovalTaskExternalLink(approvalTask);
      if (link == null)
      {
        Logger.ErrorFormat("ApprovalReviewTaskStage. Review task link for approval task not found. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3})",
                           approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        // Использовать результат, при котором не найдена задача на рассмотрение,
        // потому что его сообщение об ошибке выходит на инициатора и инициатору важно именно это.
        return this.GetErrorResult(ApprovalReviewTaskStages.Resources.ReviewTaskNotFound);
      }
      
      var reviewTask = GetReviewTaskByExternalLink(link);
      if (reviewTask == null)
      {
        Logger.ErrorFormat("ApprovalReviewTaskStage. Review task for approval task not found. Review task (ID={0}), approval task (ID={1}) (StartId={2}) (Iteration={3}) (StageNumber={4})",
                           link.ExternalEntityId, approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetErrorResult(ApprovalReviewTaskStages.Resources.ReviewTaskNotFound);
      }
      
      if (_obj.WaitReviewTaskCompletion != true)
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Do not wait until Document Review Task (ID={0}) completed. Approval task (ID={1}) (StartId={2}) (Iteration={3}) (StageNumber={4})",
                           reviewTask.Id, approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber);
        return this.GetSuccessResult();
      }
      
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (Locks.GetLockInfo(document).IsLockedByOther)
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Document locked. Approval task (ID={0}), Document (ID={1}), Locked By ({2}).",
                           approvalTask.Id, document.Id, Locks.GetLockInfo(document).LoginId);
        return this.GetRetryResult(string.Empty);
      }
      
      var taskCompleted = reviewTask.Status == Workflow.Task.Status.Completed ||
        reviewTask.Status == Workflow.Task.Status.Aborted;
      if (taskCompleted)
      {
        this.SetMemoInternalApprovalStateOnDocumentReviewTaskCompleted(approvalTask, reviewTask);
        return this.GetSuccessResult();
      }
      
      return this.GetRetryResult(string.Empty);
    }
    
    /// <summary>
    /// Связать задачу на согласование по регламенту и задачу на рассмотрение документа с помощью ссылки.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="reviewTask">Задача на рассмотрение документа.</param>
    public static void LinkApprovalTaskAndDocumentReviewTask(IApprovalTask approvalTask, RecordManagement.IDocumentReviewTask reviewTask)
    {
      var externalLink = Domain.ModuleFunctions.CreateExternalLink();
      externalLink.EntityId = approvalTask.Id;
      externalLink.EntityTypeGuid = approvalTask.GetEntityMetadata().GetOriginal().NameGuid;
      externalLink.ExternalEntityId = reviewTask.Id.ToString();
      externalLink.ExternalEntityTypeId = reviewTask.GetEntityMetadata().GetOriginal().NameGuid.ToString();
      externalLink.AdditionalInfo = GetApprovalTaskAdditionalInfoKey(approvalTask);
      externalLink.IsDeleted = false;
      externalLink.Save();
      
      Logger.DebugFormat("ApprovalReviewTaskStage. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}) linked with review task (ID={4}))",
                         approvalTask.Id, approvalTask.StartId, approvalTask.Iteration, approvalTask.StageNumber, reviewTask.Id);
    }
    
    /// <summary>
    /// Найти ссылку с задачей на рассмотрение документа для задачи на согласование по регламенту.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Ссылка с задачей на рассмотрение документа.</returns>
    [Public]
    public static Sungero.Domain.Shared.IExternalLink GetApprovalTaskExternalLink(IApprovalTask approvalTask)
    {
      var additionalInfo = GetApprovalTaskAdditionalInfoKey(approvalTask);
      return Docflow.PublicFunctions.Module.GetExternalLink(approvalTask, additionalInfo);
    }

    /// <summary>
    /// Найти ссылку с задачей на рассмотрение документа для задачи на согласование по регламенту.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="stageNumber">Номер этапа.</param>
    /// <returns>Ссылка с задачей на рассмотрение документа.</returns>
    [Public]
    public static Sungero.Domain.Shared.IExternalLink GetApprovalTaskExternalLink(IApprovalTask approvalTask, int? stageNumber)
    {
      var additionalInfo = GetApprovalTaskAdditionalInfoKey(approvalTask, stageNumber);
      return Docflow.PublicFunctions.Module.GetExternalLink(approvalTask, additionalInfo);
    }

    /// <summary>
    /// Получить задачу на рассмотрение документа.
    /// </summary>
    /// <param name="link">Ссылка с задачей на рассмотрение документа.</param>
    /// <returns>Задача на рассмотрение документа.</returns>
    public static IDocumentReviewTask GetReviewTaskByExternalLink(Sungero.Domain.Shared.IExternalLink link)
    {
      var reviewTaskId = 0;
      int.TryParse(link.ExternalEntityId, out reviewTaskId);
      return RecordManagement.DocumentReviewTasks.GetAll().SingleOrDefault(x => x.Id == reviewTaskId);
    }
    
    /// <summary>
    /// Сформировать дополнительную информацию для ссылки, связывающей задачу на согласование по регламенту с задачей на рассмотрение документа.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Дополнительная информация для ссылки, связывающей задачу на согласование по регламенту с задачей на рассмотрение документа.</returns>
    public static string GetApprovalTaskAdditionalInfoKey(IApprovalTask approvalTask)
    {
      return GetApprovalTaskAdditionalInfoKey(approvalTask, approvalTask.StageNumber);
    }
    
    /// <summary>
    /// Сформировать дополнительную информацию для ссылки, связывающей задачу на согласование по регламенту с задачей на рассмотрение документа.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="stageNumber">Номер этапа.</param>
    /// <returns>Дополнительная информация для ссылки, связывающей задачу на согласование по регламенту с задачей на рассмотрение документа.</returns>
    public static string GetApprovalTaskAdditionalInfoKey(IApprovalTask approvalTask, int? stageNumber)
    {
      return string.Format("{0}_{1}_{2}_{3}", Docflow.Constants.ApprovalReviewTaskStage.ApprovalReviewTaskStageLinkCode,
                           approvalTask.StartId, approvalTask.Iteration, stageNumber);
    }
    
    /// <summary>
    /// Установить внутренний статус согласования служебной записки при старте задачи на рассмотрение.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    public virtual void SetMemoInternalApprovalStateOnDocumentReviewTaskStarted(IApprovalTask approvalTask)
    {
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (!Memos.Is(document))
        return;
      
      Logger.DebugFormat("ApprovalReviewTaskStage. Set document state. Approval task (ID={0}), Document (ID={1}), State = PendingReview.", approvalTask.Id, document.Id);
      Memos.As(document).InternalApprovalState = Docflow.Memo.InternalApprovalState.PendingReview;
      document.Save();
    }
    
    /// <summary>
    /// Установить внутренний статус согласования служебной записки при завершении задачи на рассмотрение.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="reviewTask">Задача на рассмотрении документа.</param>
    public virtual void SetMemoInternalApprovalStateOnDocumentReviewTaskCompleted(IApprovalTask approvalTask, RecordManagement.IDocumentReviewTask reviewTask)
    {
      var document = approvalTask.DocumentGroup.OfficialDocuments.SingleOrDefault();
      if (!Memos.Is(document))
        return;
      
      var memo = Memos.As(document);
      if (reviewTask.Status == Workflow.Task.Status.Completed)
      {
        Logger.DebugFormat("ApprovalReviewTaskStage. Set document state. Approval task (ID={0}), Document (ID={1}), State = Reviewed.", approvalTask.Id, document.Id);
        memo.InternalApprovalState = Docflow.Memo.InternalApprovalState.Reviewed;
        memo.Save();
        return;
      }
    }
    
    /// <summary>
    /// Установить срок задачи на рассмотрение.
    /// </summary>
    /// <param name="task">Задача на рассмотрение документа.</param>
    public virtual void SetDocumentReviewTaskDeadline(RecordManagement.IDocumentReviewTask task)
    {
      RecordManagement.PublicFunctions.DocumentReviewTask.SetDeadline(task, _obj.DeadlineInDays, _obj.DeadlineInHours);
    }
    
    /// <summary>
    /// Получить ожидаемый срок выполнения этапа.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <param name="maxDeadline">Срок.</param>
    /// <param name="stageInProcess">Признак нахождения этапа в процессе выполнения.</param>
    /// <returns>Ожидаемый срок выполнения этапа.</returns>
    public override DateTime GetStageMaxDeadline(IApprovalTask task, DateTime maxDeadline, bool stageInProcess)
    {
      if (_obj.WaitReviewTaskCompletion != true)
        return maxDeadline;
      
      DateTime? deadline = null;
      if (stageInProcess)
        deadline = this.GetMaxDeadlineFromReviewTask(task);
      
      if (deadline == null)
        return base.GetStageMaxDeadline(task, maxDeadline, stageInProcess);
      
      return deadline.Value;
    }
    
    /// <summary>
    /// Получить срок задачи на рассмотрение документа.
    /// </summary>
    /// <param name="task">Задача на согласование, в рамках которой запущена задача на рассмотрение.</param>
    /// <returns>Срок задачи на рассмотрение документа.</returns>
    public virtual DateTime? GetMaxDeadlineFromReviewTask(IApprovalTask task)
    {
      var link = GetApprovalTaskExternalLink(task);
      if (link != null)
      {
        var reviewTask = GetReviewTaskByExternalLink(link);
        if (reviewTask != null)
          return reviewTask.MaxDeadline.Value;
      }
      return null;
    }
    
    public override string GetStateViewBlockName(IApprovalTask task, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      if (_obj.WaitReviewTaskCompletion != true)
        return Sungero.Docflow.ApprovalReviewTaskStages.Resources.SendReviewTaskStageBlockName;
      
      return Sungero.Docflow.ApprovalReviewTaskStages.Resources.ReviewTaskStageBlockName;
    }
    
    public override void AddStateViewBlockPerformers(IApprovalTask task, Sungero.Core.StateBlock block, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      var addressees = task.Addressees
        .Where(x => x != null)
        .OrderBy(x => x.Id)
        .Select(x => Recipients.As(x.Addressee))
        .Distinct()
        .ToList();
      
      Functions.ApprovalRuleBase.AddPerformersToBlock(block, addressees, OfficialDocuments.Resources.StateViewSystem.ToString());
    }
    
    public override string GetStateViewBlockDeadline(IApprovalTask task, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      if (_obj.WaitReviewTaskCompletion != true)
        return string.Empty;
      
      if (statusInfo.InProcess)
      {
        var deadline = this.GetMaxDeadlineFromReviewTask(task);
        if (deadline != null)
        {
          return string.Format("{0}: {1}",
                               OfficialDocuments.Resources.StateViewDeadline,
                               Functions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()));
        }
      }
      
      var deadlineDescription = Functions.ApprovalStageBase.GetDeadlineDescription(_obj, 1, " ", false, true);
      return string.Format("{0} –{1}", OfficialDocuments.Resources.StateViewDeadline, deadlineDescription);
    }
    
    public override void Abort(IApprovalTask task, int stageNumber)
    {
      var link = GetApprovalTaskExternalLink(task, stageNumber);
      if (link != null)
      {
        var reviewTask = GetReviewTaskByExternalLink(link);
        if (reviewTask != null && reviewTask.Status == Workflow.Task.Status.InProcess)
        {
          reviewTask.Abort();
          Logger.DebugFormat("ApprovalReviewTaskStage. Abort DocumentReviewTask from main task. Approval task (ID={0}) (StartId={1}) (Iteration={2}) (StageNumber={3}), DocumentReviewTask (ID={4}))",
                             task.Id, task.StartId, task.Iteration, task.StageNumber, reviewTask.Id);
        }
      }
    }
    
    public override void Validate(IApprovalRuleBase rule, List<IApprovalRuleBaseStages> stagesSequence, IApprovalRuleBaseStages stage, Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.Validate(rule, stagesSequence, stage, e);
      
      var reviewTaskStages = stagesSequence.Where(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function && ApprovalReviewTaskStages.Is(s.StageBase));
      
      // В одной ветке правила должно быть не более одного этапа рассмотрения несколькими адресатами.
      if (reviewTaskStages.Count() > 1 && stagesSequence.Contains(stage))
        e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalReviewTaskStages.Resources.ManyReviewStages);
      
      // В одной ветке правила может быть этап рассмотрения адресатом или несколькими адресатами.
      var reviewStages = stagesSequence.Where(st => st.StageType == Docflow.ApprovalRuleBaseStages.StageType.Review);
      if (reviewStages.Count() > 0)
      {
        var errorStages = reviewTaskStages.ToList();
        errorStages.AddRange(reviewStages);
        foreach (var errorStage in errorStages)
          e.AddError(errorStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalReviewTaskStages.Resources.ReviewStageAndReviewTaskStageExist);
      }
      
      // В одной ветке правила может быть этап рассмотрения несколькими адресатами или создания поручений по документу.
      var executionStages = stagesSequence.Where(st => st.StageType == Docflow.ApprovalRuleBaseStages.StageType.Execution);
      if (executionStages.Count() > 0)
      {
        var errorStages = reviewTaskStages.ToList();
        errorStages.AddRange(executionStages);
        foreach (var errorStage in errorStages)
          e.AddError(errorStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalReviewTaskStages.Resources.ExecutionAndReviewTaskStageExist);
      }
      
      // В одной ветке правила после этапа отправки на рассмотрение не должно быть других этапов кроме задания и уведомления.
      var afterReviewTaskStages = stagesSequence
        .SkipWhile(s => !(s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function && ApprovalReviewTaskStages.Is(s.StageBase)))
        .Skip(1);
      
      var afterReviewTaskErrorStages = afterReviewTaskStages.Where(s => s.StageType != Docflow.ApprovalRuleBaseStages.StageType.Notice
                                                                   && s.StageType != Docflow.ApprovalRuleBaseStages.StageType.SimpleAgr
                                                                   && s.StageType != Docflow.ApprovalRuleBaseStages.StageType.Function);
      
      if (afterReviewTaskErrorStages.Any())
      {
        foreach (var errorStage in afterReviewTaskErrorStages)
          e.AddError(errorStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalReviewTaskStages.Resources.AfterReviewTaskInvalidStages);
      }
    }
    
    public override List<Enumeration?> GetSupportableRoles()
    {
      var roles = base.GetSupportableRoles();
      roles.Add(Docflow.ApprovalRoleBase.Type.AddrAssistant);
      roles.Add(Docflow.ApprovalRoleBase.Type.Addressee);
      roles.Add(Docflow.ApprovalRoleBase.Type.Addressees);
      
      return roles;
    }
  }
}
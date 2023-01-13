using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Выполнить действия по корректировке поручений, которые связаны с ожиданием разблокировки заданий текущего поручения.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void ApplyActionItemLockDependentChanges(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ApplyActionItemLockDependentChangesInvokeArgs args)
    {
      Logger.DebugFormat("ApplyActionItemLockDependentChanges: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      
      var taskInProcess = actionItemTask.Status == Workflow.Task.Status.InProcess;
      if (!taskInProcess || actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        if (!taskInProcess)
        {
          actionItemTask.OnEditGuid = string.Empty;
          actionItemTask.Save();
        }
        
        args.Retry = false;
        Logger.DebugFormat("ApplyActionItemLockDependentChanges. Task with id {0} not in process or already changing.", args.ActionItemTaskId);
        return;
      }
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   string.Empty, string.Empty, args.InitiatorOfChange, args.ChangeContext);
      Functions.ActionItemExecutionTask.ApplyActionItemLockDependentChanges(actionItemTask, changes);
      Logger.DebugFormat("ApplyActionItemLockDependentChanges: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }
    
    /// <summary>
    /// Выполнить действия по корректировке поручений, которые не связаны с ожиданием разблокировки заданий текущего поручения.
    /// </summary>
    /// <param name="args">Аргументы асинхронного обработчика.</param>
    public virtual void ApplyActionItemLockIndependentChanges(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ApplyActionItemLockIndependentChangesInvokeArgs args)
    {
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      
      if (actionItemTask.Status != Workflow.Task.Status.InProcess || actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        args.Retry = false;
        Logger.DebugFormat("ApplyActionItemLockIndependentChanges. Task with id {0} not in process or already changing.", args.ActionItemTaskId);
        return;
      }
      
      if (!Functions.ActionItemExecutionTask.AssignmentsCreated(actionItemTask))
      {
        args.Retry = true;
        Logger.DebugFormat("ApplyActionItemLockIndependentChanges: assignments not created for task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
        return;
      }
      
      while (!Functions.ActionItemExecutionTask.AreAssignmentsCreated(actionItemTask))
        Functions.ActionItemExecutionTask.CreateActionItemExecutionTask(actionItemTask);
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   string.Empty, string.Empty, args.InitiatorOfChange, args.ChangeContext);
      
      Functions.ActionItemExecutionTask.ApplyActionItemLockIndependentChanges(actionItemTask, changes);
      Functions.Module.ExecuteApplyActionItemLockDependentChanges(changes, actionItemTask.Id, actionItemTask.OnEditGuid);
      Logger.DebugFormat("ApplyActionItemLockIndependentChanges: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }

    public virtual void ChangeCompoundActionItem(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ChangeCompoundActionItemInvokeArgs args)
    {
      Logger.DebugFormat("ChangeCompoundActionItem: start async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
      
      var actionItemTask = RecordManagement.ActionItemExecutionTasks.Get(args.ActionItemTaskId);
      if (actionItemTask.Status != Workflow.Task.Status.InProcess || actionItemTask.OnEditGuid != args.OnEditGuid)
      {
        args.Retry = false;
        Logger.DebugFormat("ChangeCompoundActionItem. Task with id {0} not in process or already changing.", args.ActionItemTaskId);
        return;
      }
      
      var changes = Functions.ActionItemExecutionTask.DeserializeActionItemChanges(actionItemTask, args.OldSupervisor, args.NewSupervisor, args.OldAssignee, args.NewAssignee,
                                                                                   args.OldDeadline, args.NewDeadline, args.OldCoAssignees, args.NewCoAssignees,
                                                                                   args.CoAssigneesOldDeadline, args.CoAssigneesNewDeadline, args.EditingReason, args.AdditionalInfo,
                                                                                   args.TaskIds, args.ActionItemPartsText, args.InitiatorOfChange, args.ChangeContext);
      
      // Инициализировать адресатов.
      var addressees = new List<IUser>();
      
      try
      {
        // Получить список заинтересованных в изменении поручения для отправки уведомления.
        // Находится здесь, чтобы учитывать состояние поручения до изменений.
        addressees = Functions.ActionItemExecutionTask.GetCompoundActionItemChangeNotificationAddressees(actionItemTask, changes);
        var oldSupervisors = new List<Company.IEmployee>();
        
        // Закешировать список пунктов для сокращения числа обращений к SQL.
        var actionItemPartTasks = ActionItemExecutionTasks.GetAll()
          .Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess)
          .Where(t => changes.TaskIds.Contains(t.Id))
          .ToList();
        
        // Протащить изменения в выбранные пункты поручения, которые еще в работе/на приемке.
        foreach (var actionItemPartTask in actionItemPartTasks)
        {
          if (actionItemPartTask == null)
            continue;
          
          var oldSupervisorAssignment = Functions.ActionItemExecutionTask.GetActualActionItemSupervisorAssignment(actionItemPartTask);
          
          // Сохранить предыдущих контролера и срок перед их обновлением в карточке
          // для корректной отработки прекращения запросов отчета.
          var oldSupervisor = actionItemPartTask.Supervisor;
          var oldDeadline = actionItemPartTask.Deadline;
          
          // Если для данного пункта значения новых контролера и срока совпадают со старыми, то корректировку делать не нужно.
          var deadlineChanged = changes.NewDeadline != null && !Equals(oldDeadline, changes.NewDeadline);
          var supervisorChanged = changes.NewSupervisor != null && !Equals(oldSupervisor, changes.NewSupervisor);
          if (!deadlineChanged && !supervisorChanged)
            continue;
          
          // Создать структуру для текущего пункта копированием из общей структуры, с одной лишь разницей в данных -
          // значения старого срока и старого контролера заполнить данными из пункта, а не оставлять пустыми, как в changes.
          var partChanges = Functions.Module.CopyActionItemChangesStructure(changes);
          partChanges.OldDeadline = oldDeadline;
          partChanges.OldSupervisor = oldSupervisor;
          
          if (oldSupervisor != null && supervisorChanged)
            oldSupervisors.Add(oldSupervisor);
          
          Functions.ActionItemExecutionTask.UpdateActionItemPartTask(actionItemTask, actionItemPartTask, partChanges);
          actionItemPartTask.OnEditGuid = Guid.NewGuid().ToString();
          actionItemPartTask.Save();
          
          try
          {
            // Переадресовать измененное задание контролеру.
            Functions.ActionItemExecutionTask.ForwardChangedAssignments(actionItemTask, partChanges, null, oldSupervisorAssignment);
            
            // Обработать смену срока в задании на исполнение для текущего пункта поручения.
            var executionAssignment = Functions.ActionItemExecutionTask.GetActualActionItemExecutionAssignment(actionItemPartTask);
            if (partChanges.NewDeadline != null && deadlineChanged && executionAssignment != null)
            {
              // Прокинуть срок в задание исполнителя, если задача не заблокирована.
              Functions.ActionItemExecutionTask.ChangeExecutionAssignmentDeadline(actionItemTask, partChanges.NewDeadline, executionAssignment);
              
              // Прекратить запросы на продление срока от исполнителя, у которого сменился срок.
              Functions.ActionItemExecutionTask.AbortDeadlineExtensionTasks(actionItemTask, actionItemPartTask);
            }
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat("ChangeCompoundActionItem. Error while processing task with id {0}:{1}.", args.ActionItemTaskId, ex.Message);
            throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemChangeError);
          }
          
          Functions.Module.ExecuteApplyActionItemLockIndependentChanges(partChanges, actionItemPartTask.Id, actionItemPartTask.OnEditGuid);
          
          // Прекратить неактуальные запросы отчета от предыдущего контролера из пункта поручения.
          if (oldSupervisor != null)
            Functions.ActionItemExecutionTask.AbortReportRequestTasksCreatedFromTaskByAuthor(actionItemTask, actionItemPartTask, oldSupervisor);
        }
        
        // Прекратить неактуальные запросы отчетов от предыдущих контролеров из основной задачи.
        Functions.ActionItemExecutionTask.AbortReportRequestTasksFromOldCompoundActionItemSupervisors(actionItemTask, oldSupervisors);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ChangeCompoundActionItem. Error while processing task with id {0}:{1}.", args.ActionItemTaskId, ex.Message);
        throw AppliedCodeException.Create(ActionItemExecutionTasks.Resources.ActionItemChangeError);
      }
      
      // Разослать уведомления об изменении поручения.
      Functions.ActionItemExecutionTask.SendActionItemChangeNotifications(actionItemTask, changes, addressees);
      Functions.Module.ExecuteApplyActionItemLockIndependentChanges(changes, actionItemTask.Id, actionItemTask.OnEditGuid);
      
      Logger.DebugFormat("ChangeCompoundActionItem: done async for action item execution task with id {0}, retry iteration {1}.", args.ActionItemTaskId, args.RetryIteration);
    }

    public virtual void CompleteParentActionItemExecutionAssignment(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.CompleteParentActionItemExecutionAssignmentInvokeArgs args)
    {
      var formattedArgs = string.Format("{0}, {1}, {2}", args.actionItemId, args.parentAssignmentId, args.parentTaskStartId);
      Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): Start for Action item execution task (ID = {0}).", args.actionItemId, formattedArgs);
      
      var task = ActionItemExecutionTasks.GetAll(t => t.Id == args.actionItemId).FirstOrDefault();
      if (task == null)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Task {0} not found", args.actionItemId, formattedArgs);
        return;
      }
      
      var parentAssignment = ActionItemExecutionAssignments.GetAll(t => t.Id == args.parentAssignmentId).FirstOrDefault();
      if (parentAssignment == null)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Parent assignment {0} not found", args.parentAssignmentId, formattedArgs);
        return;
      }
      
      if (parentAssignment.Status != Workflow.Assignment.Status.InProcess)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): asynchronous handler was terminated. Parent assignment {0} not in process", args.parentAssignmentId, formattedArgs);
        return;
      }
      
      if (parentAssignment.TaskStartId != args.parentTaskStartId)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({2}): asynchronous handler was terminated. StartId was changed. Value in args {0}, value in task {1} ",
                           args.parentTaskStartId, parentAssignment.TaskStartId, formattedArgs);
        return;
      }
      
      // Если задача в работе или на приёмке, не выполнять родительское задание до её завершения.
      if (task.Status == RecordManagement.ActionItemExecutionTask.Status.InProcess ||
          task.Status == RecordManagement.ActionItemExecutionTask.Status.UnderReview)
      {
        args.Retry = true;
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({3}): Action item (ID = {0}) has status {1}. Parent assignment (ID = {2}) not completed.",
                           task.Id, task.Status, parentAssignment.Id, formattedArgs);
        return;
      }
      
      try
      {
        // Добавить документы из группы "Результаты исполнения" в ведущее задание на исполнение.
        Functions.ActionItemExecutionTask.SynchronizeResultGroup(task);
        // Выполнить ведущее задание на исполнение.
        Functions.ActionItemExecutionTask.CompleteParentAssignment(task);
        // Заполнить в ведущем задании на исполнение свойство "Выполнил" исполнителем задания.
        Functions.ActionItemExecutionTask.SetCompletedByInParentAssignment(task);
      }
      catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException)
      {
        Logger.DebugFormat("CompleteParentActionItemExecutionAssignment({1}): parent assignment (ID = {0}) is locked.", parentAssignment.Id, formattedArgs);
        args.Retry = true;
        return;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("CompleteParentActionItemExecutionAssignment({0}): unhandled exception", ex, formattedArgs);
        return;
      }
    }
    
    public virtual void ExcludeFromAcquaintance(Sungero.RecordManagement.Server.AsyncHandlerInvokeArgs.ExcludeFromAcquaintanceInvokeArgs args)
    {
      var assignments = Functions.Module.GetActiveAcquaintanceAssignments(args.AssignmentIds);
      foreach (var assignment in assignments)
      {
        // Не обрабатывать завершённые и прекращённые задания.
        if (assignment.Status == Sungero.Workflow.Assignment.Status.Completed &&
            assignment.Status == Sungero.Workflow.Assignment.Status.Aborted)
          continue;
        
        // Если задание заблокировано, то нужно повторное выполнение обработчика.
        var locksInfo = Locks.GetLockInfo(assignment);
        if (locksInfo.IsLockedByOther)
        {
          args.Retry = true;
          continue;
        }
        
        Logger.DebugFormat("ExcludeFromAcquaintance: acquaintance assignment with id {0} has been excluded async.", assignment.Id);
        assignment.Abort();
      }
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class FreeApprovalTaskRouteHandlers
  {
    #region 12. Нужна доработка?
    
    public virtual bool Decision12Result()
    {
      var currentTaskStartId = _obj.StartId;
      var minAssignmentDate = Assignments.GetAll(a => Equals(a.Task, _obj) && a.TaskStartId == currentTaskStartId).Min(a => a.Created);
      var reworkAssignments = FreeApprovalReworkAssignments.GetAll(a => Equals(a.Task, _obj) && a.TaskStartId == currentTaskStartId);
      
      DateTime lastIterationDate;
      if (reworkAssignments.Any())
      {
        var maxCreated = reworkAssignments.Max(a => a.Created);
        lastIterationDate = maxCreated > minAssignmentDate ? maxCreated.Value : minAssignmentDate.Value;
      }
      else
      {
        lastIterationDate = minAssignmentDate.Value;
      }
      
      var needRework = false;
      var approvalAssignments = FreeApprovalAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Created >= lastIterationDate)
        .ToList();
      foreach (var assignment in approvalAssignments.Where(a => a.Result == Docflow.FreeApprovalAssignment.Result.ForRework))
      {
        var hasApprovedAssignment = approvalAssignments.Any(a => Equals(a.Performer, assignment.Performer) &&
                                                            a.Modified > assignment.Modified &&
                                                            Equals(a.Result, Docflow.FreeApprovalAssignment.Result.Approved));
        if (!hasApprovedAssignment)
        {
          needRework = true;
          break;
        }
      }
      return needRework;
    }
    
    #endregion
    
    #region 2. Cогласование
    
    public virtual void StartBlock2(Sungero.Docflow.Server.FreeApprovalAssignmentArguments e)
    {
      e.Block.IsParallel = true;
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ApproversAsgSubject,
                                                            _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
      
      var reworkAssignments = FreeApprovalReworkAssignments.GetAll(asg => asg.Task.Equals(_obj) && asg.TaskStartId == _obj.StartId);
      
      // Если заданий на доработку нет, то заполняем согласующих из задачи, если есть - то из последнего задания на доработку.
      if (!reworkAssignments.Any())
      {
        if (_obj.MaxDeadline.HasValue)
          e.Block.AbsoluteDeadline = _obj.MaxDeadline.Value;
        foreach (var recipient in _obj.Approvers.OrderBy(apr => apr.Id))
        {
          var approversList = Functions.FreeApprovalTask.GetUsersFromGroups(recipient.Approver);
          foreach (var groupRecipient in approversList)
          {
            if (!e.Block.Performers.Contains(groupRecipient))
              e.Block.Performers.Add(groupRecipient);
          }
        }
        // Выдать права на документы всем согласующим, включая группы.
        Functions.FreeApprovalTask.GrantRightForAttachmentsToPerformers(_obj, _obj.Approvers.Select(apr => apr.Approver).ToList());
      }
      else
      {
        var lastReworkAssignment = reworkAssignments.OrderByDescending(asg => asg.Created).First();
        if (lastReworkAssignment.NewDeadline.HasValue)
          e.Block.AbsoluteDeadline = lastReworkAssignment.NewDeadline.Value;
        _obj.MaxDeadline = lastReworkAssignment.NewDeadline;
        
        foreach (var element in lastReworkAssignment.Approvers.Where(asg => asg.Action == Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendForApproval))
        {
          e.Block.Performers.Add(element.Approver);
        }
        // Выдать права на документы всем согласующим сотрудникам.
        Functions.FreeApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
      }
      
      // Отправить запрос на подготовку предпросмотра для документов.
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }

    public virtual void StartAssignment2(Sungero.Docflow.IFreeApprovalAssignment assignment, Sungero.Docflow.Server.FreeApprovalAssignmentArguments e)
    {
      /* Дополнительные согласующие могут быть добавлены из задания на согласование по действию "Добавить согласующего".
       * Для нового согласующего через Forward будет создано задание на согласование.
       * Выполнится только обработчик StartAssignment2().
       * Задание того, кто добавляет дополнительного согласующего, будет сохранено.
       * Чтобы синхронизация не "потеряла" документы, вложенные в еще не выполненное задание, явно считать их добавленными.
       * Также согласующий после добавления дополнительного согласующего может удалить вложенное им ранее приложение.
       * В этом случае нужно считать его как явно удаленное, чтобы синхронизация не восстанавливала его обратно.
       */
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj);
      Functions.FreeApprovalTask.RemovedAddendaAppend(_obj);
      
      // Обновляем вложения - актуально для согласующих, добавленных в середине процесса.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);

      // Дополнительно выдаем права на случай переадресации.
      Functions.FreeApprovalTask.GrantRightForAttachmentsToPerformers(_obj, new List<IRecipient> { assignment.Performer });
      
      var task = FreeApprovalTasks.As(assignment.Task);
      if (!task.Approvers.Select(x => x.Approver).Contains(assignment.Performer))
      {
        var approver = task.Approvers.AddNew();
        approver.Approver = assignment.Performer;
        task.Save();
      }
    }

    public virtual void CompleteAssignment2(Sungero.Docflow.IFreeApprovalAssignment assignment, Sungero.Docflow.Server.FreeApprovalAssignmentArguments e)
    {
      /* При выполнении задания согласующего учесть не только добавленные, но и удаленные приложения.
       * Согласующий не может в задании удалить приложения, вложенные другими участниками согласования.
       * Если согласующий в свое задание добавляет приложение, а после этого добавляет согласующего по действию "Добавить согласующего",
       * то задание сохраняется и вложенное приложение считается добавленным.
       * Согласующий после сохранения своего задания может удалить вложенное ранее приложение.
       * В этом случае нужно считать его как явно удаленное.
       */
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj);
      Functions.FreeApprovalTask.RemovedAddendaAppend(_obj);
      
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.FreeApprovalTask.RelateAddedAddendaToPrimaryDocument(_obj);
      
      // Выдать права на приложения.
      var recipientsToGrantRights = Functions.Module.GetTaskAssignees(_obj);
      Functions.FreeApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipientsToGrantRights);
      
      var assignmentsInWork = FreeApprovalAssignments.GetAll(asg => asg.Task.Equals(_obj) &&
                                                             asg.TaskStartId == _obj.StartId &&
                                                             asg.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
      
      if (assignment.Result == Sungero.Docflow.FreeApprovalAssignment.Result.ForRework && _obj.ReceiveNotice == true
          && _schemeVersion >= SchemeVersions.V2 && assignmentsInWork.Any())
      {
        var notice = FreeApprovalNotifications.Create(_obj);
        notice.Performer = _obj.Author;
        var lastIterationId = Functions.FreeApprovalTask.GetApprovalAssignmentLastIterationId(_obj);
        var firstApprovalAssignment = Functions.FreeApprovalTask.GetLastAssignmentWithoutNotice(_obj, lastIterationId).First();
        notice.LinkedFreeApprovalAssignment = firstApprovalAssignment;
        notice.Author = firstApprovalAssignment.Performer;
        var subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ReworkNoticeSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
        notice.Subject = subject.Length > FreeApprovalNotifications.Info.Properties.Subject.Length ?
          subject.Substring(0, FreeApprovalNotifications.Info.Properties.Subject.Length) :
          subject;
      }
      
      if (assignment.Result == Sungero.Docflow.FreeApprovalAssignment.Result.Forward)
      {
        assignment.Forward(assignment.Addressee, ForwardingLocation.Next, assignment.AddresseeDeadline);
        var approver = _obj.Approvers.Where(x => Equals(x.Approver, assignment.Performer)).FirstOrDefault();
        _obj.Approvers.Remove(approver);
        
        var newApprover = _obj.Approvers.AddNew();
        newApprover.Approver = assignment.Addressee;
      }
    }

    public virtual void EndBlock2(Sungero.Docflow.Server.FreeApprovalAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 3. Корректировка автором

    public virtual void StartBlock3(Sungero.Docflow.Server.FreeApprovalReworkAssignmentArguments e)
    {
      // Синхронизировать группу приложений документа.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      e.Block.Performers.Add(_obj.Author);
      e.Block.RelativeDeadlineDays = 3;
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.RevisionAsgSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
      if (_obj.MaxDeadline.HasValue)
        e.Block.NewDeadline = _obj.MaxDeadline;
      
      var approvalAssignments = FreeApprovalAssignments.GetAll(asg => asg.Task.Equals(_obj) && asg.TaskStartId == _obj.StartId);
      var reworkAssignments = FreeApprovalReworkAssignments.GetAll(asg => asg.Task.Equals(_obj) && asg.TaskStartId == _obj.StartId);
      var lastIterationId = Functions.FreeApprovalTask.GetApprovalAssignmentLastIterationId(_obj);
      
      // Если заданий на доработку не было, то заполняем всю таблицу из заданий по задаче,
      // если были - то исполнителей берем из последнего задания на доработку, а результат и действие заполняем по последним заданиям на согласование.
      if (!reworkAssignments.Any())
      {
        foreach (var aprAssignment in approvalAssignments.Where(asg => asg.IterationId == lastIterationId).OrderBy(i => i.Created))
        {
          Functions.FreeApprovalTask.AddToReworkAssigneeNewApprover(e, aprAssignment);
        }
      }
      else
      {
        var approvalReworkAssignmentFromLastIteration = reworkAssignments.OrderByDescending(asg => asg.Created).First();
        foreach (var asgApprover in approvalReworkAssignmentFromLastIteration.Approvers.OrderBy(asg => asg.Id))
        {
          // Копируем все значения.
          var newApprover = e.Block.Approvers.FirstOrDefault(a => Equals(a.Approver, asgApprover.Approver)) ?? e.Block.Approvers.AddNew();
          newApprover.Approver = asgApprover.Approver;
          newApprover.Approved = asgApprover.Approved;
          newApprover.Action = asgApprover.Action;
          
          // Если результат согласования изменился \ согласование не было выполнено - перебиваем данные.
          var lastApprovalAsg = approvalAssignments.Where(asg => asg.Performer.Equals(asgApprover.Approver)).OrderByDescending(asg => asg.IterationId).ThenByDescending(asg => asg.Modified).FirstOrDefault();
          if (lastApprovalAsg != null)
          {
            if (lastApprovalAsg.Result == Sungero.Docflow.FreeApprovalAssignment.Result.Approved)
            {
              newApprover.Approved = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Approved.IsApproved;
              newApprover.Action = newApprover.Action == Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendForApproval ?
                Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendNotice : Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.DoNotSend;
            }
            else if (lastApprovalAsg.Result == Sungero.Docflow.FreeApprovalAssignment.Result.Forward)
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
        foreach (var newApproval in approvalAssignments.Where(a => a.IterationId == lastIterationId).OrderBy(i => i.Created))
        {
          Functions.FreeApprovalTask.AddToReworkAssigneeNewApprover(e, newApproval);
        }
      }
    }
    
    public virtual void StartAssignment3(Sungero.Docflow.IFreeApprovalReworkAssignment assignment, Sungero.Docflow.Server.FreeApprovalReworkAssignmentArguments e)
    {
      var lastAssignment = FreeApprovalAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Result == Docflow.FreeApprovalAssignment.Result.ForRework)
        .OrderByDescending(o => o.Completed)
        .FirstOrDefault();
      assignment.Author = lastAssignment.Performer;
    }

    public virtual void CompleteAssignment3(Sungero.Docflow.IFreeApprovalReworkAssignment assignment, Sungero.Docflow.Server.FreeApprovalReworkAssignmentArguments e)
    {
      // Заполнить коллекции добавленных и удаленных вручную документов в задаче.
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj);
      Functions.FreeApprovalTask.RemovedAddendaAppend(_obj);
    }

    public virtual void EndBlock3(Sungero.Docflow.Server.FreeApprovalReworkAssignmentEndBlockEventArguments e)
    {
      
    }
    #endregion

    #region 6. Уведомление инициатору о завершении согласования
    public virtual void StartBlock6(Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      // Синхронизировать группу приложений документа.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      e.Block.Performers.Add(_obj.Author);
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ApprovalCompletedSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
    }

    public virtual void StartNotice6(Sungero.Docflow.IFreeApprovalNotification notice, Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      
    }

    public virtual void EndBlock6(Sungero.Docflow.Server.FreeApprovalNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region 7. Уведомление согласовавшим о новом круге
    public virtual void StartBlock7(Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      // Синхронизировать группу приложений документа.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      Functions.FreeApprovalTask.RelateAddedAddendaToPrimaryDocument(_obj);
      
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.NewApprovalLapSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
      
      var approvalReworkAssignments = FreeApprovalReworkAssignments.GetAll(asg => asg.Task.Equals(_obj) && asg.TaskStartId == _obj.StartId);
      
      if (approvalReworkAssignments.Any())
      {
        var lastApprovalReworkAssignments = approvalReworkAssignments.OrderByDescending(asg => asg.Created).First();
        
        foreach (var recipient in lastApprovalReworkAssignments.Approvers.Where(apr => apr.Action == Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendNotice))
        {
          e.Block.Performers.Add(recipient.Approver);
        }
      }
      
      Functions.Module.GrantReadAccessRightsForAttachments(_obj.ForApprovalGroup.All.Concat(_obj.AddendaGroup.All).ToList(),
                                                           _obj.Observers.Select(o => o.Observer).ToList());
    }

    public virtual void StartNotice7(Sungero.Docflow.IFreeApprovalNotification notice, Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      
    }

    public virtual void EndBlock7(Sungero.Docflow.Server.FreeApprovalNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 8. Задание инициатору о завершении согласования
    
    public virtual void StartBlock8(Sungero.Docflow.Server.FreeApprovalFinishAssignmentArguments e)
    {
      // Синхронизировать группу приложений документа.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      e.Block.Performers.Add(_obj.Author);
      e.Block.RelativeDeadlineDays = 3;
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ApprovalCompletedSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
    }

    public virtual void StartAssignment8(Sungero.Docflow.IFreeApprovalFinishAssignment assignment, Sungero.Docflow.Server.FreeApprovalFinishAssignmentArguments e)
    {
      
    }

    public virtual void CompleteAssignment8(Sungero.Docflow.IFreeApprovalFinishAssignment assignment, Sungero.Docflow.Server.FreeApprovalFinishAssignmentArguments e)
    {
      // Заполнить коллекцию добавленных вручную документов в задаче.
      Functions.FreeApprovalTask.AddedAddendaAppend(_obj);
      
      Functions.FreeApprovalTask.RelateAddedAddendaToPrimaryDocument(_obj);
    }

    public virtual void EndBlock8(Sungero.Docflow.Server.FreeApprovalFinishAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region 11. Уведомление автору об отправке на доработку
    
    public virtual void StartBlock11(Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      // Синхронизировать группу приложений документа.
      var document = _obj.ForApprovalGroup.ElectronicDocuments.FirstOrDefault();
      Functions.FreeApprovalTask.SynchronizeAddendaAndAttachmentsGroup(_obj);
      
      e.Block.Performers.Add(_obj.Author);
      var lastIterationId = Functions.FreeApprovalTask.GetApprovalAssignmentLastIterationId(_obj);
      var firstApprovalAssignment = Functions.FreeApprovalTask.GetLastAssignmentWithoutNotice(_obj, lastIterationId).First();
      e.Block.LinkedFreeApprovalAssignment = firstApprovalAssignment;
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ReworkNoticeSubject, _obj.ForApprovalGroup.ElectronicDocuments.First().Name);
    }

    public virtual void StartNotice11(Sungero.Docflow.IFreeApprovalNotification notice, Sungero.Docflow.Server.FreeApprovalNotificationArguments e)
    {
      notice.Author = e.Block.LinkedFreeApprovalAssignment.Performer;
    }

    public virtual void EndBlock11(Sungero.Docflow.Server.FreeApprovalNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region 5. Нужно задание о завершении согласования?

    public virtual bool Decision5Result()
    {
      return _obj.ReceiveOnCompletion == ReceiveOnCompletion.Assignment;
    }
    
    #endregion

    #region 9. Уведомлять об отправке на доработку?
    
    public virtual bool Decision9Result()
    {
      return _obj.ReceiveNotice.Value;
    }
    
    #endregion
    
    #region 10. Кто-то отправил на доработку?

    public virtual bool Monitoring10Result()
    {
      var lastIterationId = Functions.FreeApprovalTask.GetApprovalAssignmentLastIterationId(_obj);
      
      if (FreeApprovalAssignments.GetAll(asg => asg.Task.Equals(_obj) &&
                                         asg.TaskStartId == _obj.StartId &&
                                         asg.IterationId == lastIterationId &&
                                         asg.Status == Sungero.Workflow.AssignmentBase.Status.InProcess).Any())
      {
        return Functions.FreeApprovalTask.GetLastAssignmentWithoutNotice(_obj, lastIterationId).Any();
      }
      else
        return false;
    }
    
    public virtual void StartBlock10(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 4. Конец
    
    public virtual void StartReviewAssignment4(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion
  }
}
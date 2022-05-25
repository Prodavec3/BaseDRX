using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Contracts.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Агент создания задач по завершению контрольных точек договоров.
    /// </summary>
    public virtual void SendTaskForContractMilestones()
    {
      var contracts = ContractualDocuments.GetAll().Where(c => c.Milestones.Any());
      foreach (var contract in contracts)
      {
        if (!Locks.GetLockInfo(contract).IsLocked)
        {
          foreach (var milestone in contract.Milestones)
          {
            // Создание задач.
            var daysToFinishWorks = milestone.DaysToFinishWorks != null ? milestone.DaysToFinishWorks.Value : 3;
            if (milestone.IsCompleted == false && milestone.Deadline < Calendar.Now.AddWorkingDays(milestone.Performer, daysToFinishWorks) &&
                (milestone.Task == null || milestone.Task.Status == Workflow.SimpleTask.Status.Aborted || milestone.Task.Status == Workflow.SimpleTask.Status.Suspended))
            {
              var subject = string.Format(Sungero.Contracts.Resources.ContractMilestoneTaskSubject, milestone.Name, contract.Name);
              if (subject.Length > Workflow.SimpleTasks.Info.Properties.Subject.Length)
                subject = subject.Substring(0, Workflow.SimpleTasks.Info.Properties.Subject.Length);
              subject = Docflow.PublicFunctions.Module.TrimQuotes(subject);
              
              var attachments = new List<IContractualDocument>() { contract };
              if (SupAgreements.Is(contract))
                attachments.Add(ContractualDocuments.As(contract.LeadingDocument));
              
              var milestoneDeadline = milestone.Deadline.Value.Date < Calendar.GetUserToday(milestone.Performer) ? Calendar.Now.AddWorkingDays(milestone.Performer, 1) : milestone.Deadline.Value;
              var task = Workflow.SimpleTasks.Create(subject, milestoneDeadline, new IUser[] { milestone.Performer }, attachments);
              task.NeedsReview = false;
              
              var contractTypeName = contract.DocumentKind.DocumentType.Name.ToLower();
              task.ActiveText = Sungero.Contracts.Resources.ContractMilestoneTaskTextFormat(CaseConverter.ConvertJobTitleToTargetDeclension(contractTypeName, DeclensionCase.Dative));
              
              if (!string.IsNullOrEmpty(milestone.Note))
                task.ActiveText += "\n" + string.Format(Sungero.Contracts.Resources.ContractMilestoneTaskTextNote, milestone.Note);

              var observers = new List<IUser>();
              // Проверка на наличие дублей наблюдателей.
              if (contract.ResponsibleEmployee != null && !Equals(contract.ResponsibleEmployee, milestone.Performer))
              {
                if (Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(contract.ResponsibleEmployee).MyContractsNotification == true)
                  observers.Add(contract.ResponsibleEmployee);
                var responsibleManager = Docflow.PublicFunctions.Module.Remote.GetManager(contract.ResponsibleEmployee);
                if (responsibleManager != null && !Equals(responsibleManager, milestone.Performer) && !Equals(responsibleManager, contract.ResponsibleEmployee) &&
                    Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(responsibleManager).MySubordinatesContractsNotification == true)
                  observers.Add(responsibleManager);
              }
              var performerManager = Docflow.PublicFunctions.Module.Remote.GetManager(milestone.Performer);
              if (performerManager != null && !Equals(performerManager, milestone.Performer) &&
                  Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(performerManager).MySubordinatesContractsNotification == true &&
                  !observers.Contains(performerManager))
                observers.Add(performerManager);
              
              foreach (var observer in observers)
                task.Observers.AddNew().Observer = observer;

              foreach (var attachment in attachments)
              {
                if (!attachment.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, milestone.Performer))
                  attachment.AccessRights.Grant(milestone.Performer, DefaultAccessRightsTypes.Read);
                if (performerManager != null && !attachment.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, performerManager))
                  attachment.AccessRights.Grant(performerManager, DefaultAccessRightsTypes.Read);
              }

              task.Save();
              task.Start();
              milestone.Task = task;
            }
            
            if (milestone.Task != null)
            {
              // Проставление признака завершенности.
              if (milestone.Task.Status == Workflow.SimpleTask.Status.Completed)
                milestone.IsCompleted = true;
              
              // Актуализация сроков заданий.
              if (milestone.Deadline > milestone.Task.Deadline)
              {
                milestone.Task.Deadline = milestone.Deadline;
                var assignment = Workflow.SimpleAssignments.GetAll().Where(a => Equals(a.Task, milestone.Task)).FirstOrDefault();
                if (assignment != null)
                  assignment.Deadline = milestone.Deadline;
                assignment.Save();
              }
              
              // Прекращение задачи
              if (milestone.IsCompleted == true)
              {
                milestone.Task.Abort();
                milestone.Task = null;
              }
            }
          }
          if (contract.State.IsChanged)
            contract.Save();
        }
      }
    }
    
    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия договоров.
    /// </summary>
    public virtual void SendNotificationForExpiringContracts()
    {
      var command = Queries.Module.CreateTableForExpiringContracts;
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      
      var notifyParams = Docflow.PublicFunctions.Module.GetDefaultExpiringDocsNotificationParams(Constants.Module.NotificationDatabaseKey,
                                                                                                 Constants.Module.ExpiringContractTableName);
      
      var alreadySentDocs = Docflow.PublicFunctions.Module.GetDocumentsWithSendedTask(notifyParams.ExpiringDocTableName);
      
      var contractIds = ContractBases.GetAll()
        .Where(c => !alreadySentDocs.Contains(c.Id))
        .Where(c => c.LifeCycleState == Sungero.Contracts.ContractBase.LifeCycleState.Active ||
                    c.LifeCycleState == Sungero.Contracts.ContractBase.LifeCycleState.Draft)
        .Where(c => c.ResponsibleEmployee != null || c.Author != null)
        .Where(c => notifyParams.LastNotificationReserve.AddDays(c.DaysToFinishWorks.HasValue ? c.DaysToFinishWorks.Value : 0) < c.ValidTill  &&
                    c.ValidTill <= notifyParams.TodayReserve.AddDays(c.DaysToFinishWorks.HasValue ? c.DaysToFinishWorks.Value : 0))
        .Where(c => c.DaysToFinishWorks == null || c.DaysToFinishWorks <= Docflow.PublicConstants.Module.MaxDaysToFinish)
        .Select(c => c.Id)
        .ToList();

      Logger.DebugFormat("Contracts to send notification count = {0}.", contractIds.Count());
      
      for (int i = 0; i < contractIds.Count(); i = i + notifyParams.BatchCount)
      {
        var result = Transactions.Execute(
          () =>
          {
            var contractsPart = ContractBases.GetAll(c => contractIds.Contains(c.Id)).Skip(i).Take(notifyParams.BatchCount).ToList();
            contractsPart = contractsPart.Where(c => notifyParams.LastNotification.ToUserTime(c.ResponsibleEmployee ?? c.Author).AddDays(c.DaysToFinishWorks.HasValue ? c.DaysToFinishWorks.Value : 0) < c.ValidTill &&
                                                     c.ValidTill <= Calendar.GetUserToday(c.ResponsibleEmployee ?? c.Author).AddDays(c.DaysToFinishWorks.HasValue ? c.DaysToFinishWorks.Value : 0))
              .ToList();
            
            if (!contractsPart.Any())
              return;
            
            Docflow.PublicFunctions.Module.ClearIdsFromExpiringDocsTable(notifyParams.ExpiringDocTableName,
                                                                         contractsPart.Select(x => x.Id).ToList());
            Docflow.PublicFunctions.Module.AddExpiringDocumentsToTable(notifyParams.ExpiringDocTableName,
                                                                       contractsPart.Select(x => x.Id).ToList());
            
            foreach (var contract in contractsPart)
            {
              var subject = Docflow.PublicFunctions.Module.TrimQuotes(
                  contract.IsAutomaticRenewal == true ?
                  Resources.AutomaticRenewalContractExpiresFormat(contract.DisplayValue) :
                  Resources.ExpiringContractsSubjectFormat(contract.DisplayValue));
              
              if (subject.Length > Workflow.SimpleTasks.Info.Properties.Subject.Length)
                  subject = subject.Substring(0, Workflow.SimpleTasks.Info.Properties.Subject.Length);
              
              var activeText = Docflow.PublicFunctions.Module.TrimQuotes(
                  contract.IsAutomaticRenewal == true ?
                  Resources.ExpiringContractsRenewalTextFormat(contract.ValidTill.Value.ToShortDateString(), contract.DisplayValue) :
                  Resources.ExpiringContractsTextFormat(contract.ValidTill.Value.ToShortDateString(), contract.DisplayValue));
              
              var performers = Functions.Module.GetNotificationPerformers(contract);
              performers = performers.Where(p => p != null).Distinct().ToList();
              
              var attachments = new List<IElectronicDocument>();
              attachments.Add(contract);
              var related = contract.Relations.GetRelated(Constants.Module.SupAgreementRelationName).ToList();
              attachments.AddRange(related);
              
              notifyParams.TaskParams.Document = contract;
              notifyParams.TaskParams.Subject = subject;
              notifyParams.TaskParams.ActiveText = activeText;
              notifyParams.TaskParams.Performers = performers;
              notifyParams.TaskParams.Attachments = attachments;
              Docflow.PublicFunctions.Module.TrySendExpiringDocNotifications(notifyParams);
            }
          });
      }
      
      if (Docflow.PublicFunctions.Module.IsAllNotificationsStarted(notifyParams.ExpiringDocTableName))
      {
        Docflow.PublicFunctions.Module.UpdateLastNotificationDate(notifyParams);
        Docflow.PublicFunctions.Module.ClearExpiringTable(notifyParams.ExpiringDocTableName, false);
      }
    }
  }
}
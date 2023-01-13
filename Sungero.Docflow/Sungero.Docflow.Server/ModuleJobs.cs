using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;

namespace Sungero.Docflow.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Перемещение содержимого документа в хранилище.
    /// </summary>
    public virtual void TransferDocumentsByStoragePolicy()
    {

      var hasNotDefaultStorage = Sungero.CoreEntities.Storages.GetAll(s => s.IsDefault != true).Any();
      if (!hasNotDefaultStorage)
      {
        Logger.DebugFormat("TransferDocumentsByStoragePolicy: has only default storage.");
        return;
      }
      
      var hasStoragePolicies = Docflow.StoragePolicyBases.GetAll(p => p.Status == Docflow.StoragePolicyBase.Status.Active).Any();
      if (!hasStoragePolicies)
      {
        Logger.DebugFormat("TransferDocumentsByStoragePolicy: has no active storage policies.");
        return;
      }
      
      var now = Calendar.Now;
      var policiesToUpdateRetentionDate = RetentionPolicies.GetAll().Where(p => p.Status == Docflow.RetentionPolicy.Status.Active &&
                                                                           (p.NextRetention == null || p.NextRetention <= now)).ToList();
      
      Sungero.Docflow.Functions.Module.CreateStoragePolicySettings(now);
      
      var documentsToSetStorageList = Sungero.Docflow.Functions.Module.GetDocumentsToTransfer();
      
      Sungero.Docflow.Functions.Module.ExecuteSetDocumentStorage(documentsToSetStorageList);
      
      Logger.DebugFormat("TransferDocumentsByStoragePolicy: drop storage policy settings.");
      var commandText = string.Format(Docflow.Queries.Module.DropTable, Constants.Module.StoragePolicySettingsTableName);
      Sungero.Docflow.Functions.Module.ExecuteSQLCommand(commandText);
      
      foreach (var policy in policiesToUpdateRetentionDate)
      {
        try
        {
          policy.LastRetention = now;
          policy.NextRetention = Sungero.Docflow.PublicFunctions.RetentionPolicy.GetNextRetentionDate(policy.RepeatType, policy.IntervalType, policy.Interval, now);
          policy.Save();
          Logger.DebugFormat("TransferDocumentsByStoragePolicy: update storage policy {0}.", policy.Id);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("TransferDocumentsByStoragePolicy: cannot update storage policy {0}.", ex, policy.Id);
        }
      }
      
    }

    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия доверенностей.
    /// </summary>
    public virtual void SendNotificationForExpiringPowerOfAttorney()
    {
      var createTableCommand = Queries.Module.CreateTableForExpiringPowerOfAttorney;
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(createTableCommand);
      
      var notifyParams = PublicFunctions.Module.GetDefaultExpiringDocsNotificationParams(Constants.Module.ExpiringPowerOfAttorneyLastNotificationKey,
                                                                                         Constants.Module.ExpiringPowerOfAttorneyTableName);
      
      var alreadySentDocs = PublicFunctions.Module.GetDocumentsWithSendedTask(notifyParams.ExpiringDocTableName);
      
      var powerOfAttorneyIds = PowerOfAttorneyBases.GetAll()
        .Where(p => !alreadySentDocs.Contains(p.Id))
        .Where(p => p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Active ||
               p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Draft)
        .Where(p => p.IssuedTo != null || p.PreparedBy != null)
        .Where(p => notifyParams.LastNotificationReserve.AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0) < p.ValidTill  &&
               p.ValidTill <= notifyParams.TodayReserve.AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0))
        .Where(p => p.DaysToFinishWorks == null || p.DaysToFinishWorks <= Constants.Module.MaxDaysToFinish)
        .Select(p => p.Id)
        .ToList();

      Logger.DebugFormat("Powers of Attorney to send notification count = {0}.", powerOfAttorneyIds.Count());
      
      for (int i = 0; i < powerOfAttorneyIds.Count(); i = i + notifyParams.BatchCount)
      {
        var result = Transactions.Execute(
          () =>
          {
            var powerOfAttorneyPart = PowerOfAttorneyBases.GetAll(p => powerOfAttorneyIds.Contains(p.Id)).Skip(i).Take(notifyParams.BatchCount).ToList();
            powerOfAttorneyPart = powerOfAttorneyPart.Where(p => notifyParams.LastNotification.ToUserTime(p.PreparedBy ?? p.IssuedTo).AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0) < p.ValidTill &&
                                                            p.ValidTill <= Calendar.GetUserToday(p.PreparedBy ?? p.IssuedTo).AddDays(p.DaysToFinishWorks.HasValue ? p.DaysToFinishWorks.Value : 0))
              .ToList();
            
            if (!powerOfAttorneyPart.Any())
              return;
            
            PublicFunctions.Module.ClearIdsFromExpiringDocsTable(notifyParams.ExpiringDocTableName,
                                                                 powerOfAttorneyPart.Select(x => x.Id).ToList());
            PublicFunctions.Module.AddExpiringDocumentsToTable(notifyParams.ExpiringDocTableName,
                                                               powerOfAttorneyPart.Select(x => x.Id).ToList());
            
            foreach (var powerOfAttorney in powerOfAttorneyPart)
            {
              var subject = Docflow.PublicFunctions.Module.TrimQuotes(Resources.ExpiringPowerOfAttorneySubjectFormat(powerOfAttorney.DisplayValue));
              if (subject.Length > Workflow.SimpleTasks.Info.Properties.Subject.Length)
                subject = subject.Substring(0, Workflow.SimpleTasks.Info.Properties.Subject.Length);
              
              var activeText = Docflow.PublicFunctions.Module.TrimQuotes(Resources.ExpiringPowerOfAttorneyTextFormat(powerOfAttorney.ValidTill.Value.ToShortDateString(),
                                                                                                                     powerOfAttorney.DisplayValue));
              
              var performers = Functions.Module.GetNotificationPoAPerformers(powerOfAttorney);
              performers = performers.Where(p => p != null).Distinct().ToList();
              
              var attachments = new List<Sungero.Content.IElectronicDocument>();
              attachments.Add(powerOfAttorney);
              
              notifyParams.TaskParams.Document = powerOfAttorney;
              notifyParams.TaskParams.Subject = subject;
              notifyParams.TaskParams.ActiveText = activeText;
              notifyParams.TaskParams.Performers = performers;
              notifyParams.TaskParams.Attachments = attachments;
              PublicFunctions.Module.TrySendExpiringDocNotifications(notifyParams);
            }
          });
      }
      
      if (PublicFunctions.Module.IsAllNotificationsStarted(notifyParams.ExpiringDocTableName))
      {
        PublicFunctions.Module.UpdateLastNotificationDate(notifyParams);
        PublicFunctions.Module.ClearExpiringTable(notifyParams.ExpiringDocTableName, false);
      }
    }
    
    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия доверенностей.
    /// </summary>
    public virtual void SendNotificationForExpiringPowersOfAttorney()
    {
    }

    /// <summary>
    /// Автоматическая выдача прав на документы.
    /// </summary>
    public virtual void GrantAccessRightsToDocuments()
    {
      // Отключение ФП для проверки тестов.
      if (PublicFunctions.Module.GetGrantRightMode() != Constants.Module.GrantRightsModeByJob)
        return;
      
      var startDate = Calendar.Now;
      var lastStartDate = PublicFunctions.Module.GetLastAgentRunDate(Constants.Module.LastAccessRightsUpdateDate);
      
      var allRules = AccessRightsRules.GetAll(s => s.Status == Docflow.AccessRightsRule.Status.Active).ToList();
      if (!allRules.Any())
      {
        PublicFunctions.Module.UpdateLastAgentRunDate(Constants.Module.LastAccessRightsUpdateDate, startDate);
        return;
      }
      
      var hasRuleWithLeadingDocuments = allRules.Any(s => s.GrantRightsOnLeadingDocument == true);
      
      // Измененные документы.
      var queue = new List<Structures.DocumentGrantRightsQueueItem.ProxyQueueItem>();
      var changedDocumentIds = OfficialDocuments.GetAll(d => d.Modified >= lastStartDate && d.Modified <= startDate).Select(d => d.Id);
      foreach (var documentId in changedDocumentIds)
      {
        var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
        if (document == null)
          continue;
        
        var documentRules = GetAvailableRules(document, allRules);
        foreach (var documentRule in documentRules)
        {
          queue.Add(CreateAccessRightsQueueItem(documentId, documentRule, Docflow.DocumentGrantRightsQueueItem.ChangedEntityType.Document));
        }
        
        // Проверить наличие правил для ведущих документов, если есть хотя бы одно такое правило.
        if (hasRuleWithLeadingDocuments)
        {
          var leadingDocumentIds = GetLeadingDocuments(document);
          foreach (var leadingDocumentId in leadingDocumentIds)
          {
            var leadingDocument = OfficialDocuments.GetAll(d => d.Id == leadingDocumentId).FirstOrDefault();
            var leadDocumentRules = GetAvailableRules(leadingDocument, allRules);
            foreach (var leadDocumentRule in leadDocumentRules)
              queue.Add(CreateAccessRightsQueueItem(leadingDocument.Id, leadDocumentRule, Docflow.DocumentGrantRightsQueueItem.ChangedEntityType.Document));
            
            leadingDocument = leadingDocument.LeadingDocument;
          }
        }
      }
      
      // Измененные настройки.
      var changedRules = allRules.Where(s =>
                                        s.Modified >= lastStartDate &&
                                        s.Modified <= startDate &&
                                        s.GrantRightsOnExistingDocuments == true);
      foreach (var changedRule in changedRules)
      {
        foreach (var ruleDocument in GetDocumentsByRule(changedRule))
          queue.Add(CreateAccessRightsQueueItem(ruleDocument, changedRule, Docflow.DocumentGrantRightsQueueItem.ChangedEntityType.Rule));
      }
      
      var table = DocumentGrantRightsQueueItems.Info.DBTableName;
      var ids = Sungero.Domain.IdentifierGenerator.GenerateIdentifiers(table, queue.Count).ToList();
      for (int i = 0; i < queue.Count; i++)
        queue[i].Id = ids[i];
      Docflow.PublicFunctions.Module.WriteStructuresToTable(table, queue);
      Logger.DebugFormat("GrantAccessRightsToDocuments: Added to queue {0} documents.", queue.Count);
      
      // Обновить дату запуска агента в базе.
      PublicFunctions.Module.UpdateLastAgentRunDate(Constants.Module.LastAccessRightsUpdateDate, startDate);
      
      // Выдать права на документы.
      var step = 5;
      var error = 0;
      var isEmpty = false;
      for (int i = 0; i < 10000; i = i + step)
      {
        // Если элементов больше нет - заканчиваем.
        if (isEmpty)
          break;
        
        var result = Transactions.Execute(
          () =>
          {
            Logger.DebugFormat("GrantAccessRightsToDocuments: Start process queue from {0}.", i);

            // Т.к. в конце транзакции элементы удаляются, в Take берем просто N новых элементов.
            var queueItemPart = DocumentGrantRightsQueueItems.GetAll().Skip(error).Take(step).ToList();
            if (!queueItemPart.Any())
            {
              // Завершаем транзакцию, если больше нечего обрабатывать.
              isEmpty = true;
              return;
            }

            var accessRightsGranted = queueItemPart
              .Where(q => this.GrantRightsToDocumentByRules(q, allRules))
              .ToList();
            if (accessRightsGranted.Any())
              Functions.Module.FastDeleteQueueItems(accessRightsGranted.Select(a => a.Id).ToList());
            error += queueItemPart.Count - accessRightsGranted.Count;
          });
        if (!result)
          error += step;
      }
    }

    /// <summary>
    /// Выдать права на документ и приложения к нему.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="allRules">Действующие правила выдачи прав.</param>
    /// <returns>True, если элемент очереди был успешно обработан.</returns>
    protected virtual bool GrantRightsToDocumentByRules(IDocumentGrantRightsQueueItem queueItem, List<IAccessRightsRule> allRules)
    {
      var document = OfficialDocuments.GetAll(d => d.Id == queueItem.DocumentId.Value).FirstOrDefault();
      if (document == null)
        return true;

      var rule = queueItem.AccessRightsRule;

      // Права на документ.
      if (GetAvailableRules(document, allRules).Any(s => Equals(s, rule)))
      {
        if (!TryGrantAccessRightsToDocumentByRule(document, rule))
          return false;
        
        // Права на дочерние документы от ведущего.
        if (rule.GrantRightsOnLeadingDocument == true)
        {
          var childDocumentIds = GetChildDocuments(document);
          foreach (var childDocumentId in childDocumentIds)
          {
            var childDocument = OfficialDocuments.GetAll(d => d.Id == childDocumentId).FirstOrDefault();
            if (childDocument == null)
              continue;

            if (!TryGrantAccessRightsToDocumentByRule(childDocument, rule))
              return false;
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Создать элемент очереди выдачи прав.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="rule">Правило.</param>
    /// <param name="rightType">Тип элемента.</param>
    /// <returns>Структура для сохранения в таблицу очереди выдачи прав.</returns>
    private static Structures.DocumentGrantRightsQueueItem.ProxyQueueItem CreateAccessRightsQueueItem(int documentId, IAccessRightsRule rule, Enumeration rightType)
    {
      Logger.DebugFormat("CreateAccessRightsQueueItem: document {0}, rule {1}, rightType {2}", documentId, rule.Id, rightType);
      var queueItem = Structures.DocumentGrantRightsQueueItem.ProxyQueueItem.Create();
      queueItem.Discriminator = DocumentGrantRightsQueueItem.ClassTypeGuid;
      queueItem.DocumentId_Docflow_Sungero = documentId;
      queueItem.AccessRights_Docflow_Sungero = rule.Id;
      queueItem.ChangedType_Docflow_Sungero = rightType.Value;
      return queueItem;
    }
    
    /// <summary>
    /// Удалить элементы очереди.
    /// </summary>
    /// <param name="items">Элементы на удаление.</param>
    private static void FastDeleteQueueItems(List<IDocumentGrantRightsQueueItem> items)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format("delete from {0} where Id in ({1})",
                                            items[0].Info.DBTableName, string.Join(", ", items.Select(i => i.Id)));
        command.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Выдать права на документ по правилу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rule">Правило.</param>
    /// <returns>Возвращает true, если права удалось выдать, false - если надо повторить позже.</returns>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Obsolete("Используйте метод TryGrantAccessRightsToDocumentByRule.")]
    public static bool TryGrantRightsToDocument(IOfficialDocument document, IAccessRightsRule rule)
    {
      Logger.DebugFormat("TryGrantRightsToDocument: document {0}, rule {1}", document.Id, rule.Id);
      
      var isChanged = false;
      foreach (var member in rule.Members)
      {
        if (!document.AccessRights.IsGrantedDirectly(Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType), member.Recipient))
        {
          if (Locks.GetLockInfo(document).IsLockedByOther)
            return false;

          document.AccessRights.Grant(member.Recipient, Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType));
          isChanged = true;
        }
      }
      if (isChanged)
      {
        ((Domain.Shared.IExtendedEntity)document).Params[Constants.OfficialDocument.DontUpdateModified] = true;
        document.Save();
      }
      
      return true;
    }

    /// <summary>
    /// Выдать права на документ по правилу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rule">Правило.</param>
    /// <returns>Возвращает true, если права удалось выдать, false - если надо повторить позже.</returns>
    public static bool TryGrantAccessRightsToDocumentByRule(IOfficialDocument document, IAccessRightsRule rule)
    {
      Logger.DebugFormat("TryGrantAccessRightsToDocumentByRule: document {0}, rule {1}", document.Id, rule.Id);
      
      var isChanged = false;
      foreach (var member in rule.Members)
      {
        if (!document.AccessRights.IsGrantedDirectly(Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType), member.Recipient))
        {
          if (Locks.GetLockInfo(document).IsLockedByOther)
            return false;

          document.AccessRights.Grant(member.Recipient, Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType));
          isChanged = true;
        }
      }
      if (isChanged)
      {
        ((Domain.Shared.IExtendedEntity)document).Params[Constants.OfficialDocument.DontUpdateModified] = true;
        document.Save();
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить из списка правил подходящие для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rules">Правила.</param>
    /// <returns>Подходящие правила.</returns>
    public static List<IAccessRightsRule> GetAvailableRules(IOfficialDocument document, List<IAccessRightsRule> rules)
    {
      var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
      
      return rules
        .Where(s => s.Status == Docflow.AccessRightsRule.Status.Active)
        .Where(s => !s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind)))
        .Where(s => !s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, document.BusinessUnit)))
        .Where(s => !s.Departments.Any() || s.Departments.Any(k => Equals(k.Department, document.Department)))
        .Where(s => !s.DocumentGroups.Any() || s.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup))).ToList();
    }
    
    /// <summary>
    /// Получить документы по правилу.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <returns>Документы по правилу.</returns>
    private static IEnumerable<int> GetDocumentsByRule(IAccessRightsRule rule)
    {
      var documentKinds = rule.DocumentKinds.Select(t => t.DocumentKind).ToList();
      var businessUnits = rule.BusinessUnits.Select(t => t.BusinessUnit).ToList();
      var departments = rule.Departments.Select(t => t.Department).ToList();
      
      var documents = OfficialDocuments.GetAll()
        .Where(d => !documentKinds.Any() || documentKinds.Contains(d.DocumentKind))
        .Where(d => !businessUnits.Any() || businessUnits.Contains(d.BusinessUnit))
        .Where(d => !departments.Any() || departments.Contains(d.Department));
      
      if (rule.DocumentGroups.Any())
        return FilterDocumentsByGroups(rule, documents);
      else
        return documents.Select(d => d.Id);
    }
    
    /// <summary>
    /// Получить ведущие документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ведущие документы.</returns>
    private static List<int> GetLeadingDocuments(IOfficialDocument document)
    {
      var documents = new List<int>() { document.Id };
      var leadingDocuments = new List<int>();
      while (document.LeadingDocument != null && !documents.Contains(document.LeadingDocument.Id))
      {
        documents.Add(document.LeadingDocument.Id);
        leadingDocuments.Add(document.LeadingDocument.Id);
      }
      return leadingDocuments;
    }
    
    /// <summary>
    /// Получить ведомые документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ведомые документы.</returns>
    private static List<int> GetChildDocuments(IOfficialDocument document)
    {
      var documents = new List<int>() { document.Id };
      var allChildDocuments = new List<int>();
      var childDocuments = OfficialDocuments.GetAll(d => d.LeadingDocument != null && documents.Contains(d.LeadingDocument.Id) && !documents.Contains(d.Id))
        .Select(d => d.Id)
        .ToList();
      while (childDocuments.Any())
      {
        documents.AddRange(childDocuments);
        allChildDocuments.AddRange(childDocuments);
        childDocuments = OfficialDocuments.GetAll(d => d.LeadingDocument != null && documents.Contains(d.LeadingDocument.Id) && !documents.Contains(d.Id))
          .Select(d => d.Id)
          .ToList();
      }
      return allChildDocuments;
    }
    
    /// <summary>
    /// Фильтр для категорий договоров.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="query">Ленивый запрос документов.</param>
    /// <returns>Относительно ленивый запрос с категориями.</returns>
    private static IEnumerable<int> FilterDocumentsByGroups(IAccessRightsRule rule, IQueryable<IOfficialDocument> query)
    {
      foreach (var document in query)
      {
        var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
        if (rule.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup)))
          yield return document.Id;
      }
    }

    /// <summary>
    /// Рассылка электронных писем о заданиях.
    /// </summary>
    public virtual void SendMailNotification()
    {
      Logger.Debug("SendMailNotification. Start.");
      Functions.Module.SendMailNotification();
      Logger.Debug("SendMailNotification. Done.");
    }
    
    /// <summary>
    /// Рассылка электронных писем со сводкой о заданиях.
    /// </summary>
    public virtual void SendSummaryMailNotifications()
    {
      Functions.Module.SummaryMailLogDebug("Start job SendSummaryMailNotifications.");
      Functions.Module.SendSummaryMailNotification();
      Functions.Module.SummaryMailLogDebug("End job SendSummaryMailNotifications.");
    }
    
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.SessionExtensions;
using Sungero.Domain.Shared;
using Sungero.Parties;
using Sungero.Workflow;

namespace Sungero.Integration1C.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Отправить уведомление о результатах синхронизации в 1С простой задачей.
    /// </summary>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="text">Содержание уведомления с результатами синхронизации.</param>
    [Remote]
    public void SendNotificationBySimpleTask(string title, string text)
    {
      var role = Roles.GetAll(x => x.Sid == Integration1C.Constants.Module.SynchronizationResponsibleRoleGuid).FirstOrDefault();
      if (role == null || !role.RecipientLinks.Any())
        return;
      
      var task = Workflow.SimpleTasks.CreateWithNotices(title, role);
      task.ActiveText = text;
      task.Save();
      task.Start();
    }
    
    /// <summary>
    /// Получить сущности, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Remote(IsPure = true)]
    public List<Sungero.Domain.Shared.IEntity> GetChangedEntitiesFromSyncDateRemote(List<Guid> entityTypeGuids,
                                                                                    int processedEntitiesCount,
                                                                                    int entitiesCountForProcessing,
                                                                                    string extEntityType,
                                                                                    string systemId)
    {
      using (var session = new Domain.Session())
      {
        var entities = new List<Sungero.Domain.Shared.IEntity>().AsQueryable();
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          entities = entities.Union(this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId));
        }
        var entitiesBatch = entities
          .Skip(processedEntitiesCount)
          .Take(entitiesCountForProcessing)
          .ToList();
        
        return entitiesBatch;
      }
    }
    
    /// <summary>
    /// Получить количество сущностей, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Remote(IsPure = true)]
    public int GetChangedEntitiesFromSyncDateRemoteCount(List<Guid> entityTypeGuids,
                                                         string extEntityType,
                                                         string systemId)
    {
      using (var session = new Domain.Session())
      {
        var totalCount = 0;
        
        foreach (var entityTypeGuid in entityTypeGuids)
          totalCount += this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId).Count();
        return totalCount;
      }
    }

    /// <summary>
    /// Получить банковские счета, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Remote(IsPure = true)]
    public List<Sungero.Domain.Shared.IEntity> GetChangedBankAccountsFromSyncDateRemote(List<Guid> entityTypeGuids,
                                                                                        int processedEntitiesCount,
                                                                                        int entitiesCountForProcessing,
                                                                                        string extEntityType,
                                                                                        string systemId)
    {
      using (var session = new Domain.Session())
      {
        var entities = new List<Sungero.Domain.Shared.IEntity>().AsQueryable();
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          entities = entities.Union(this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId));
        }
        entities = entities.Where(x => Counterparties.Is(x) &&
                                  Counterparties.As(x).Status == Sungero.Parties.Counterparty.Status.Active &&
                                  Counterparties.As(x).Bank != null &&
                                  !string.IsNullOrEmpty(Counterparties.As(x).Account));
        
        var entitiesBatch = entities
          .Skip(processedEntitiesCount)
          .Take(entitiesCountForProcessing)
          .ToList();
        
        return entitiesBatch;
      }
    }
    
    /// <summary>
    /// Получить количество банковских счетов, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Remote(IsPure = true)]
    public int GetChangedBankAccountsFromSyncDateRemoteCount(List<Guid> entityTypeGuids,
                                                             string extEntityType,
                                                             string systemId)
    {
      using (var session = new Domain.Session())
      {
        var totalCount = 0;
        
        foreach (var entityTypeGuid in entityTypeGuids)
        {
          var entities = this.GetChangedEntities(session, entityTypeGuid, extEntityType, systemId).ToList();
          totalCount += entities.Where(x => Counterparties.Is(x) &&
                                       Counterparties.As(x).Status == Sungero.Parties.Counterparty.Status.Active &&
                                       Counterparties.As(x).Bank != null &&
                                       !string.IsNullOrEmpty(Counterparties.As(x).Account))
                                .Count();
        }
        return totalCount;
      }
    }

    /// <summary>
    /// Получить сущности, измененные с момента последней синхронизации, в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="entityTypeGuid">Guid типа сущности.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Список сущностей, измененных с момента последней синхронизации.</returns>
    private IQueryable<Sungero.Domain.Shared.IEntity> GetChangedEntities(Sungero.Domain.Session session,
                                                                         Guid entityTypeGuid,
                                                                         string extEntityType,
                                                                         string systemId)
    {
      var entityTypeGuidString = entityTypeGuid.ToString().ToLower();
      var entityType = Sungero.Domain.Shared.TypeExtension.GetTypeByGuid(entityTypeGuid);
      var entityTypeMetadata = entityType.GetEntityMetadata();
      var originalTypeGuid = entityTypeGuid.GetOriginalTypeGuid();
      var historyType = entityTypeMetadata.HistoryTypeGuid.GetTypeByGuid();
      var user = Users.Current;
      
      var entities = session.GetAll(entityType)
        .Where(s => Domain.Session.GetAllFromCurrentSession(historyType).Cast<IHistory>()
               .Where(h => h.EntityType == originalTypeGuid && h.EntityId == s.Id)
               .Where(h => (!session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.EntityId == h.EntityId && el.EntityType.ToLower() == entityTypeGuidString &&
                                 el.ExtEntityType == extEntityType && el.ExtSystemId == systemId) &&
                            !session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.ExtEntityId == h.EntityId.ToString() &&
                                 el.ExtSystemId == Constants.Module.InternalLinkSystemId) ||
                            !Equals(h.User, user) &&
                            (h.Action == Sungero.CoreEntities.History.Action.Create || h.Action == Sungero.CoreEntities.History.Action.Update) &&
                            session.GetAll<Commons.IExternalEntityLink>()
                            .Any(el => el.EntityId == h.EntityId && el.EntityType.ToLower() == entityTypeGuidString &&
                                 el.ExtEntityType == extEntityType && Equals(el.ExtSystemId, systemId) &&
                                 (!el.SyncDate.HasValue || h.HistoryDate >= el.SyncDate))))
               .Any())
        .OrderBy(s => s.Id);

      return entities;
    }
    
    /// <summary>
    /// Получить документ с результатами синхронизации за сегодня.
    /// </summary>
    /// <param name="fileExists">Признак, что документ с результатами синхронизации существует в системе.</param>
    /// <returns>Документ с сегодняшними результатами синхронизации.</returns>
    [Remote(IsPure = true)]
    public Docflow.ISimpleDocument GetTodayDocumentRemote(bool fileExists)
    {
      if (!fileExists)
      {
        var document = Docflow.SimpleDocuments.Create();
        document.Name = "Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString();
        var version = document.Versions.AddNew();
        version.AssociatedApplication = Content.AssociatedApplications.GetAll(app => app.Extension == "txt").FirstOrDefault();
        document.Save();
        
        return document;
      }
      else
      {
        var document = Docflow.SimpleDocuments.GetAll().Where(doc => doc.Name == ("Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString())).FirstOrDefault();
        return document;
      }
    }
    
    /// <summary>
    /// Проверить, что протокол результатов синхронизации за сегодня существует в системе.
    /// </summary>
    /// <returns>True, если сегодняшний протокол существует, иначе False.</returns>
    [Remote(IsPure = true)]
    public bool IsSummaryProtocolExistRemote()
    {
      return Docflow.SimpleDocuments.GetAll().Any(doc => doc.Name == ("Результаты синхронизации за " + Calendar.UserToday.Date.ToShortDateString()));
    }
    
    /// <summary>
    /// Обновить дату последней синхронизации с 1С.
    /// </summary>
    /// <param name="date">Дата синхронизации, на которую обновить.</param>
    /// <param name="systemId">ИД системы 1С.</param>
    [Remote]
    public void UpdateLastNotificationDate(DateTime date, string systemId)
    {
      var key = Constants.Module.LastNotifyOfSyncDateParamName;
      var systemIdBytes = System.Text.Encoding.Default.GetBytes(systemId);
      var systemIdCode = string.Empty;
      foreach (var b in systemIdBytes)
        systemIdCode = string.Format("{0}{1}", systemIdCode, b.ToString("X2"));
      
      var command = string.Format(Queries.Module.SelectLastNotificationDate, key);
      var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
      var noticesValues = string.Empty;
      if (!(executionResult is DBNull) && executionResult != null)
        noticesValues = executionResult.ToString();
      
      if (string.IsNullOrWhiteSpace(noticesValues))
      {
        var value = string.Format("{0}{1}", date.ToString("yyyy-MM-dd"), systemIdCode);
        Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertLastNotificationDate, new[] { key, value });
      }
      else
      {
        var parts = noticesValues.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var dates = new Dictionary<string, string>();
        foreach (var part in parts)
        {
          var value = part.Substring(0, 10);
          var code = part.Remove(0, 10);
          dates.Add(code, value);
        }
        
        if (dates.ContainsKey(systemIdCode))
          dates[systemIdCode] = date.ToString("yyyy-MM-dd");
        else
          dates.Add(systemIdCode, date.ToString("yyyy-MM-dd"));
        
        var result = string.Empty;
        foreach (var d in dates)
          result = string.Format("{0}&{1}{2}", result, d.Value, d.Key);
        result = result.Trim('&');
        
        Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.UpdateLastNotificationDate, new[] { key, result });
      }
    }
    
    /// <summary>
    /// Получить дату последней синхронизации с 1С
    /// из уведомления о результатах синхронизации.
    /// </summary>
    /// <param name="systemId">ИД системы 1С.</param>
    /// <returns>Дата последней синхронизации, либо пустая строка в случае ее отсутствия.</returns>
    [Remote(IsPure = true)]
    public string GetLastNotificationDate(string systemId)
    {
      var key = Constants.Module.LastNotifyOfSyncDateParamName;
      var systemIdBytes = System.Text.Encoding.Default.GetBytes(systemId);
      var systemIdCode = string.Empty;
      foreach (var b in systemIdBytes)
        systemIdCode = string.Format("{0}{1}", systemIdCode, b.ToString("X2"));
      
      try
      {
        var command = string.Format(Queries.Module.SelectLastNotificationDate, key);
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var noticesValues = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          noticesValues = executionResult.ToString();
        
        if (string.IsNullOrWhiteSpace(noticesValues))
          return string.Empty;

        var parts = noticesValues.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        var dates = new Dictionary<string, string>();
        foreach (var part in parts)
        {
          var value = part.Substring(0, 10);
          var code = part.Remove(0, 10);
          dates.Add(code, value);
        }
        
        if (dates.ContainsKey(systemIdCode))
          return dates[systemIdCode];
        else
          return string.Empty;
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }
  }
}
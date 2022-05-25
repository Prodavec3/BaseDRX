using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Integration1C.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить сущности, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">Ид системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Public]
    public List<Sungero.Domain.Shared.IEntity> GetChangedEntitiesFromSyncDate(List<Guid> entityTypeGuids,
                                                                              int processedEntitiesCount,
                                                                              int entitiesCountForProcessing,
                                                                              string extEntityType,
                                                                              string systemId)
    {
      return Functions.Module.Remote.GetChangedEntitiesFromSyncDateRemote(entityTypeGuids, processedEntitiesCount, entitiesCountForProcessing, extEntityType, systemId);
    }
    
    /// <summary>
    /// Получить количество сущностей, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">Ид системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Public]
    public int GetChangedEntitiesFromSyncDateCount(List<Guid> entityTypeGuids,
                                                   string extEntityType,
                                                   string systemId)
    {
      return Functions.Module.Remote.GetChangedEntitiesFromSyncDateRemoteCount(entityTypeGuids, extEntityType, systemId);
    }

    /// <summary>
    /// Получить банковские счета, измененные с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="processedEntitiesCount">Количество обработанных записей.</param>
    /// <param name="entitiesCountForProcessing">Размер пакета.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">Ид системы 1С.</param>
    /// <returns>Список сущностей.</returns>
    [Public]
    public List<Sungero.Domain.Shared.IEntity> GetChangedBankAccountsFromSyncDate(List<Guid> entityTypeGuids,
                                                                                  int processedEntitiesCount,
                                                                                  int entitiesCountForProcessing,
                                                                                  string extEntityType,
                                                                                  string systemId)
    {
      return Functions.Module.Remote.GetChangedBankAccountsFromSyncDateRemote(entityTypeGuids, processedEntitiesCount, entitiesCountForProcessing, extEntityType, systemId);
    }
    
    /// <summary>
    /// Получить количество банковских счетов, измененных с момента последней синхронизации.
    /// </summary>
    /// <param name="entityTypeGuids">Список гуидов типов сущностей.</param>
    /// <param name="extEntityType">Тип записи внешней системы.</param>
    /// <param name="systemId">Ид системы 1С.</param>
    /// <returns>Количество сущностей.</returns>
    [Public]
    public int GetChangedBankAccountsFromSyncDateCount(List<Guid> entityTypeGuids,
                                                       string extEntityType,
                                                       string systemId)
    {
      return Functions.Module.Remote.GetChangedBankAccountsFromSyncDateRemoteCount(entityTypeGuids, extEntityType, systemId);
    }

    /// <summary>
    /// Получить документ с результатами синхронизации за сегодня.
    /// </summary>
    /// <param name="fileExists">Признак, что документ с результатами синхронизации существует в системе.</param>
    /// <returns>Документ с сегодняшними результатами синхронизации.</returns>
    [Public]
    public Docflow.ISimpleDocument GetTodayDocument(bool fileExists)
    {
      return Functions.Module.Remote.GetTodayDocumentRemote(fileExists);
    }
    
    /// <summary>
    /// Признак, что протокол результатов синхронизации за сегодня существует в системе.
    /// </summary>
    /// <returns>True, если сегодняшний протокол существует, иначе False.</returns>
    [Public]
    public bool IsSummaryProtocolExist()
    {
      return Functions.Module.Remote.IsSummaryProtocolExistRemote();
    }
    
    /// <summary>
    /// Отправить уведомление о результатах синхронизации в 1С простой задачей.
    /// </summary>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="text">Содержание уведомления с результатами синхронизации.</param>
    [Public]
    public void SendNotificationBySimpleTaskRemote(string title, string text)
    {
      Functions.Module.Remote.SendNotificationBySimpleTask(title, text);
    }
    
    /// <summary>
    /// Обновить дату последней синхронизации с 1С.
    /// </summary>
    /// <param name="date">Дата синхронизации, на которую обновить.</param>
    /// <param name="systemId">Ид системы 1С.</param>
    [Public]
    public void UpdateLastNotificationDateRemote(DateTime date, string systemId)
    {
      Functions.Module.Remote.UpdateLastNotificationDate(date, systemId);
    }
    
    /// <summary>
    /// Получить дату последней синхронизации с 1С
    /// из уведомления о результатах синхронизации.
    /// </summary>
    /// <param name="systemId">Ид системы 1С.</param>
    /// <returns>Дата последней синхронизации, либо пустая строка в случае ее отсутствия.</returns>
    [Public]
    public string GetLastNotificationDateRemote(string systemId)
    {
      return Functions.Module.Remote.GetLastNotificationDate(systemId);
    }
  }
}
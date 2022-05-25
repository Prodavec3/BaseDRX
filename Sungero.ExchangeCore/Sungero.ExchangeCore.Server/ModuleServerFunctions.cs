using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.ExchangeCore.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить роль "Пользователи с правами на работу через сервис обмена.
    /// </summary>
    /// <returns>Роль.</returns>
    [Public]
    public static IRole GetExchangeServiceUsersRole()
    {
      return Roles.GetAll(x => x.Sid == Constants.Module.RoleGuid.ExchangeServiceUsersRoleGuid).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить дату последней синхронизации ящиком в СО.
    /// </summary>
    /// <param name="box">Ящик.</param>
    /// <returns>Дата последней синхронизации.</returns>
    public static DateTime GetLastSyncDate(IBusinessUnitBox box)
    {
      var key = string.Format(Constants.Module.LastBoxSyncDate, box.Id);
      var command = string.Format(Queries.Module.GetLastSyncDate, key);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Last box sync date in DB is {0} (UTC)", date);
        
        DateTime result = DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        return result;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Error while getting box sync date", ex);
        return Calendar.SqlMinValue;
      }
    }

    /// <summary>
    /// Обновить дату последней синхронизации ящика.
    /// </summary>
    /// <param name="notificationDate">Дата синхронизации ящика.</param>
    /// <param name="box">Ящик.</param>
    public static void UpdateLastSyncDate(DateTime notificationDate, IBusinessUnitBox box)
    {
      var key = string.Format(Constants.Module.LastBoxSyncDate, box.Id);
      
      var newDate = notificationDate.ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.UpdateLastSyncDate, new[] { key, newDate });
      Logger.DebugFormat("Last box sync date is set to {0} (UTC)", newDate);
    }
    
    /// <summary>
    /// Запуск фонового процесса "Электронный обмен. Синхронизация контрагентов".
    /// </summary>
    [Public, Remote]
    public static void RequeueCounterpartySync()
    {
      Jobs.SyncCounterparties.Enqueue();
    }
    
    /// <summary>
    /// Запуск фонового процесса "Электронный обмен. Синхронизация абонентских ящиков".
    /// </summary>
    [Public, Remote]
    public static void RequeueBoxSync()
    {
      Sungero.ExchangeCore.Jobs.SyncBoxes.Enqueue();
    }
    
    /// <summary>
    /// Получить список организаций зарегистрированных в сервисах обмена по ИНН.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="boxes">Абонентские ящики.</param>
    /// <returns>Список организаций в формате {Name}|{TIN}|{TRRC}|{BoxId}|{OrganizationId}|{ExchangeStatus}.</returns>
    [Public, Remote(IsPure = true)]
    public static List<string> FindOrganizationsInExchangeServices(string tin, string trrc, List<IBusinessUnitBox> boxes)
    {
      var result = new List<string>();
      
      if (string.IsNullOrWhiteSpace(tin))
        return result;
      
      foreach (var box in boxes)
      {
        var client = Functions.BusinessUnitBox.GetClient(box);
        var contacts = client.GetContacts();
        var organizations = client.FindOrganizationsByInnKpp(tin, trrc);
        
        if (organizations == null || !organizations.Any())
          continue;
        
        var organizationInfos = new List<string>();
        foreach (var organization in organizations)
        {
          var contact = contacts.Where(c => c.Organization.OrganizationId == organization.OrganizationId).FirstOrDefault();
          var exchangeStatus = string.Empty;
          if (contact != null)
            exchangeStatus = Functions.BusinessUnitBox.GetCounterpartyExchangeStatus(box, contact.Status).ToString();
          var name = organization.Name;
          if (organization.IsRoaming)
            name += " " + BusinessUnitBoxes.Resources.ExchangeServiceNameFormat(organization.ExchangeServiceName);
          var organizationFullInfo = string.Format("{0}|{1}|{2}|{3}|{4}|{5}", name, organization.Inn, organization.Kpp, box.Id, organization.OrganizationId, exchangeStatus);
          
          result.Add(organizationFullInfo);
        }
      }
      
      return result.Distinct().ToList();
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Электронный обмен.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(Company.IEmployee employee)
    {
      var result = new List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      // HACK: Получаем отображаемое имя модуля.
      // Dmitriev_IA: Данные из модуля ExchangeCode должны попасть в таблицу модуля Компания.
      var companyModuleMetadata = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(Company.PublicConstants.Module.ModuleGuid);
      var moduleName = companyModuleMetadata.GetDisplayName();
      var modulePriority = Company.PublicConstants.ResponsibilitiesReport.ExchangePriority;
      
      // Цифровые сертификаты.
      if (Certificates.AccessRights.CanRead())
      {
        var certificateResponsibility = Company.Reports.Resources.ResponsibilitiesReport.CertificateResponsibility;
        var certificates = Certificates.GetAll()
          .Where(x => Equals(x.Owner, employee))
          .Where(d => d.Enabled.HasValue && d.Enabled.Value)
          .Where(d => !d.NotAfter.HasValue || d.NotAfter.Value > Calendar.Now);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, certificates, moduleName, modulePriority,
                                                                                   certificateResponsibility, null);
      }
      
      // Ответственный за абонентские ящики наших организаций.
      if (BoxBases.AccessRights.CanRead())
      {
        var boxResponsibility = Company.Reports.Resources.ResponsibilitiesReport.BoxResponsibility;
        var boxes = BoxBases.GetAll()
          .Where(x => Equals(x.Responsible, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, boxes, moduleName, modulePriority,
                                                                                   boxResponsibility, null);
      }
      
      return result;
    }
    
    [Public(WebApiRequestType = RequestType.Get)]
    public string GetEncryptedData(string data)
    {
      return Sungero.ExchangeCore.Functions.BusinessUnitBox.GetEncryptedDataRemote(data);
    }
    
    /// <summary>
    /// Получить список элементов очереди синхронизации сообщений.
    /// </summary>
    /// <param name="rootBox">Абонентский ящик.</param>
    /// <returns>Список элементов очереди сообщений.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<Sungero.ExchangeCore.IMessageQueueItem> GetMessageQueueItems(IBoxBase rootBox)
    {
      return MessageQueueItems.GetAll().Where(q => Equals(q.RootBox, rootBox));
    }
  }
}
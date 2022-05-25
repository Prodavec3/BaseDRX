using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Parties.Server
{
  public class ModuleFunctions
  {
    #region Обложка

    /// <summary>
    /// Создать новую организацию.
    /// </summary>
    /// <returns>Организация.</returns>
    [Remote]
    public static ICompany CreateCompany()
    {
      return Companies.Create();
    }

    /// <summary>
    /// Создать новое контактное лицо.
    /// </summary>
    /// <returns>Контакт.</returns>
    [Remote]
    public static IContact CreateContact()
    {
      return Contacts.Create();
    }
    
    /// <summary>
    /// Создать новую персону.
    /// </summary>
    /// <returns>Персона.</returns>
    [Remote]
    public static IPerson CreatePerson()
    {
      return People.Create();
    }
    
    /// <summary>
    /// Получить контрагентов, с которыми разрешен эл. обмен.
    /// </summary>
    /// <returns>Список контрагентов, с кот. разрешен эл. обмен.</returns>
    [Remote]
    public IQueryable<ICounterparty> CounterpartiesAvailableForExchange()
    {
      return Counterparties.GetAll(x => x.CanExchange == true);
    }
    
    /// <summary>
    /// Получить список контактных лиц.
    /// </summary>
    /// <param name="company">Организация.</param>
    /// <returns>Список контактов.</returns>
    [Remote(IsPure = true)]
    public static List<IContact> GetContactsFromCompany(ICompanyBase company)
    {
      return Contacts.GetAll(o => Equals(o.Company, company)).ToList();
    }
    
    /// <summary>
    /// Подобрать email по ИНН/КПП.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <returns>Почта.</returns>
    [Remote(IsPure = true)]
    public static string GetEmailByTinTrrc(string tin, string trrc)
    {
      var counterparties = Counterparties.GetAll().Where(c => c.TIN == tin).ToList();
      
      if (!string.IsNullOrWhiteSpace(trrc))
        counterparties = counterparties.Where(t => Companies.Is(t) && Companies.As(t).TRRC == trrc).ToList();
      
      var counterparty = counterparties.FirstOrDefault();
      
      return counterparty != null ? counterparty.Email : string.Empty;
    }
    
    /// <summary>
    /// Поиск контрагента по ИНН.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="boxes">Ящики, в которых осуществлять поиск.</param>
    /// <returns>Список контрагентов.</returns>
    [Remote]
    public static List<Structures.Module.CounterpartyFromExchangeService> FindCompanyInExchangeServices(string tin, string trrc, List<ExchangeCore.IBusinessUnitBox> boxes)
    {
      var counterparties = Sungero.ExchangeCore.PublicFunctions.Module.Remote.FindOrganizationsInExchangeServices(tin, trrc, boxes);
      var foundCompanies = new List<Structures.Module.CounterpartyFromExchangeService>();
      foreach (var counterparty in counterparties)
      {
        var items = counterparty.Split(new char[] { '|' }, StringSplitOptions.None);
        var foundCompany = Structures.Module.CounterpartyFromExchangeService.Create();
        
        foundCompany.Name = items[0];
        foundCompany.TIN = items[1];
        foundCompany.TRRC = items[2];
        foundCompany.Box = ExchangeCore.BusinessUnitBoxes.GetAll().FirstOrDefault(b => b.Id == Convert.ToInt32(items[3]));
        foundCompany.OrganizationId = items[4];
        foundCompany.Counterparty = FindCounterpartyByOrganizationId(foundCompany.Box, foundCompany.OrganizationId);
        
        if (string.IsNullOrEmpty(items[5]))
          foundCompany.ExchangeStatus = null;
        else
          foundCompany.ExchangeStatus = new Enumeration(items[5]);
        
        foundCompanies.Add(foundCompany);
      }
      
      return foundCompanies;
    }
    
    /// <summary>
    /// Найти контрагента в системе по ящику и id сервиса обмена.
    /// </summary>
    /// <param name="box">Ящик.</param>
    /// <param name="organizationId">Id сервиса обмена.</param>
    /// <returns>Найденный контрагент.</returns>
    [Remote]
    public static ICounterparty FindCounterpartyByOrganizationId(ExchangeCore.IBusinessUnitBox box, string organizationId)
    {
      return Counterparties.GetAll().Where(c => c.ExchangeBoxes.Any(b => Equals(b.Box, box) && b.OrganizationId == organizationId)).FirstOrDefault();
    }
    
    #endregion
    
    #region МКДО
    
    /// <summary>
    /// Проверить наличие абонентских ящиков.
    /// </summary>
    /// <returns>True, если есть хоть один.</returns>
    [Remote]
    public bool CheckAnyBusinessUnitBoxes()
    {
      return ExchangeCore.BusinessUnitBoxes.GetAll().Any(b => b.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
    }
    
    #endregion
    
    #region Поиск дублей контрагента
    
    /// <summary>
    /// Получить дубли контрагентов.
    /// </summary>
    /// <param name="counterparties">Контрагенты, среди которых будет осуществляться поиск дублей.</param>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование контрагента.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагентов.</returns>
    private static List<ICounterparty> GetDuplicateCounterparties(IQueryable<ICounterparty> counterparties, string tin, string trrc, string name, bool excludeClosed)
    {
      var searchByName = !string.IsNullOrWhiteSpace(name);
      var searchByTin = !string.IsNullOrWhiteSpace(tin);
      var searchByTrrc = !string.IsNullOrWhiteSpace(trrc);
      
      if (!searchByName && !searchByTin)
        return new List<ICounterparty>();
      
      var duplicates = new List<ICounterparty>();
      
      // Отфильтровать закрытые сущности.
      if (excludeClosed)
        counterparties = counterparties.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
      
      // Поиск по ИНН, если ИНН передан.
      if (searchByTin)
      {
        var counterpartiesByTin = counterparties.Where(x => x.TIN == tin);
        
        // Поиск по КПП, если КПП передан.
        if (searchByTrrc)
        {
          // Поиск по КПП или пустому КПП. Контрагент с пустым КПП также является потенциальным дублем.
          var companies = counterpartiesByTin.Where(c => CompanyBases.Is(c)).Select(c => CompanyBases.As(c));
          duplicates = companies.Where(x => x.TRRC == trrc || x.TRRC == null || x.TRRC.Trim() == string.Empty).ToList<ICounterparty>();
          
          // Поиск по Имени с пустыми ИНН и КПП, если ничего не найдено раньше.
          if (duplicates.Count == 0 && searchByName)
          {
            companies = counterparties.Where(x => CompanyBases.Is(x)).Select(x => CompanyBases.As(x));
            duplicates = companies.Where(x => (x.TIN == null || x.TIN.Trim() == string.Empty) &&
                                         (x.TRRC == null || x.TRRC.Trim() == string.Empty) &&
                                         string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).ToList<ICounterparty>();
          }
        }
        else
        {
          // Поиск по Имени с пустыми ИНН, если задано Имя, но по ИНН ничего не найдено.
          if (counterpartiesByTin.Count() == 0 && searchByName)
          {
            var counterpartiesByName = counterparties.Where(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
            duplicates = counterpartiesByName.Where(x => x.TIN == null || x.TIN.Trim() == string.Empty).ToList();
          }
          else
            duplicates = counterpartiesByTin.ToList();
        }
      }
      
      // Поиск по Имени, если задано только Имя.
      if (searchByName && !searchByTin)
        duplicates = counterparties.Where(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)).ToList();
      
      return duplicates;
    }
    
    /// <summary>
    /// Получить дубли контрагентов.
    /// </summary>
    /// <param name="counterparties">Контрагенты, среди которых будет осуществляться поиск дублей.</param>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование контрагента.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагентов.</returns>
    [Remote(IsPure = true)]
    public static List<ICounterparty> GetDuplicateCounterpartiesFromList(List<ICounterparty> counterparties, string tin, string trrc, string name, bool excludeClosed)
    {
      return GetDuplicateCounterparties(counterparties.AsQueryable(), tin, trrc, name, excludeClosed);
    }
    
    /// <summary>
    /// Получить дубли контрагентов.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование контрагента.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагентов.</returns>
    [Remote(IsPure = true)]
    public static List<ICounterparty> GetDuplicateCounterparties(string tin, string trrc, string name, bool excludeClosed)
    {
      return GetDuplicateCounterparties(tin, trrc, name, null, excludeClosed);
    }
    
    /// <summary>
    /// Получить дубли контрагентов.
    /// </summary>
    /// <param name="tin">ИНН.</param>
    /// <param name="trrc">КПП.</param>
    /// <param name="name">Наименование контрагента.</param>
    /// <param name="excludedCounterpartyId">ИД контрагента, который будет исключен из списка дублей.</param>
    /// <param name="excludeClosed">Признак необходимости исключить закрытые записи.</param>
    /// <returns>Список дублей контрагентов.</returns>
    [Remote(IsPure = true)]
    public static List<ICounterparty> GetDuplicateCounterparties(string tin, string trrc, string name, int? excludedCounterpartyId, bool excludeClosed)
    {
      var counterparties = Counterparties.GetAll();
      if (excludedCounterpartyId.HasValue)
        counterparties = counterparties.Where(x => x.Id != excludedCounterpartyId.Value);
      
      return GetDuplicateCounterparties(counterparties, tin, trrc, name, excludeClosed);
    }
    
    #endregion
    
    #region Поиск контрагента из коннектора к 1С
    
    /// <summary>
    /// Найти контрагента. Применяется при переходе по ссылке из 1С.
    /// </summary>
    /// <param name="uuid">Uuid контрагента в 1С.</param>
    /// <param name="tin">ИНН контрагента.</param>
    /// <param name="trrc">КПП контрагента.</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    /// <returns>Список найденных контрагентов.</returns>
    [Public, Remote(IsPure = true)]
    public static List<ICounterparty> FindCounterparty(string uuid, string tin, string trrc, string sysid)
    {
      // Найти контрагента среди синхронизированных ранее.
      if (!string.IsNullOrWhiteSpace(uuid))
      {
        var linkedIds = Commons.PublicFunctions.Module.GetExternalEntityLinks(uuid, sysid).Select(x => x.EntityId).ToList();
        var result = Counterparties.GetAll().Where(x => linkedIds.Contains(x.Id));
        
        if (result.Any())
          return result.ToList();
      }
      
      // Найти контрагентов, удовлетворяющих критериям ИНН/КПП, если не найдено синхронизированных ранее.
      return GetDuplicateCounterparties(tin, trrc, string.Empty, null, false);
    }
    
    #endregion
    
    #region Поиск контрагента по ИНН/ОГРН
    
    /// <summary>
    /// Получить адрес нашего сервиса заполнения контрагентов.
    /// </summary>
    /// <returns>Адрес сервера, или пустую строку, если его нет.</returns>
    [Remote]
    public string GetCompanyDataServiceURL()
    {
      var key = Sungero.Docflow.PublicConstants.Module.CompanyDataServiceKey;
      var command = string.Format(Queries.Module.SelectCompanyDataService, key);
      var commandExecutionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
      var serviceUrl = string.Empty;
      if (!(commandExecutionResult is DBNull) && commandExecutionResult != null)
        serviceUrl = commandExecutionResult.ToString();
      
      return serviceUrl;
    }
    
    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Контрагенты.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Sungero.Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(Sungero.Company.IEmployee employee)
    {
      var result = new List<Sungero.Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new PartiesModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var modulePriority = Sungero.Company.PublicConstants.ResponsibilitiesReport.CounterpartyPriority;
      
      if (!Companies.AccessRights.CanRead())
        return result;
      
      var activeCompanies = Companies.GetAll(c => c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Ответственный за контрагентов.
      var counterparties = activeCompanies.Where(c => Equals(c.Responsible, employee));
      result = Sungero.Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, counterparties, moduleName, modulePriority,
                                                                                         Resources.OrganizationResponsible, null);
      
      return result;
    }
    
    #endregion
  }
}
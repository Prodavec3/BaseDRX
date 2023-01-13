using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain.Shared;
using Init = Sungero.Contracts.Constants.Module.Initialize;

namespace Sungero.Contracts.Server
{
  public class ModuleFunctions
  {
    #region Remote CRUD
    
    /// <summary>
    /// Создать дополнительное соглашение.
    /// </summary>
    /// <returns>Созданное доп. соглашение.</returns>
    [Remote]
    public ISupAgreement CreateSupAgreemnt()
    {
      return SupAgreements.Create();
    }
    
    /// <summary>
    /// Создать акт к договорному документу.
    /// </summary>
    /// <returns>Созданный акт.</returns>
    [Remote]
    public Sungero.FinancialArchive.IContractStatement CreateContractStatement()
    {
      return Sungero.FinancialArchive.ContractStatements.Create();
    }
    
    /// <summary>
    /// Создать входящий счет.
    /// </summary>
    /// <returns>Созданный входящий счет.</returns>
    [Remote]
    public IIncomingInvoice CreateIncomingInvoice()
    {
      return IncomingInvoices.Create();
    }
    
    /// <summary>
    /// Создать исходящий счет.
    /// </summary>
    /// <returns>Созданный исходящий счет.</returns>
    [Remote]
    public IOutgoingInvoice CreateOutgoingInvoice()
    {
      return OutgoingInvoices.Create();
    }
    
    #endregion
    
    #region Спец. папки

    /// <summary>
    /// Получить виды документов с документопотоком "Договоры".
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Remote]
    public static global::System.Linq.IQueryable<Sungero.Docflow.IDocumentKind> GetDocumentKinds()
    {
      return DocumentKinds.GetAll().Where(k => k.DocumentFlow == DocumentFlow.Contracts);
    }
    
    #endregion
    
    #region Обложка
    
    /// <summary>
    /// Получение списка документов, удовлетворяющих условиям поиска.
    /// </summary>
    /// <returns>Массив строк для выбора.</returns>
    [Remote(IsPure = true), Public]
    public static List<IDocumentRegister> GetContractualDocumentRegisters()
    {
      return DocumentRegisters.GetAll()
        .Where(r => r.DocumentFlow == Sungero.Docflow.DocumentRegister.DocumentFlow.Contracts).ToList();
    }
    
    /// <summary>
    /// Получение списка договорных документов, удовлетворяющих условиям поиска по регистрационным данным.
    /// </summary>
    /// <param name="number">Номер регистрации документа.</param>
    /// <param name="dateFrom">Дата регистрации документа от.</param>
    /// <param name="dateTo">Дата регистрации документа по.</param>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <param name="caseFile">Дело.</param>
    /// <param name="responsibleEmployee">Сотрудник.</param>
    /// <returns>Выборка договоров, удовлетворяющих условиям.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IContractualDocument> GetFilteredRegisteredDocuments(
      string number, DateTime? dateFrom, DateTime? dateTo,
      IDocumentRegister documentRegister, ICaseFile caseFile, IEmployee responsibleEmployee)
    {
      if (dateTo != null)
        dateTo = dateTo.Value.AddDays(1);
      return ContractualDocuments.GetAll()
        .Where(l => number == null || l.RegistrationNumber.Contains(number))
        .Where(l => dateFrom == null || l.RegistrationDate >= dateFrom)
        .Where(l => dateTo == null || l.RegistrationDate < dateTo)
        .Where(l => documentRegister == null || l.DocumentRegister.Equals(documentRegister))
        .Where(l => caseFile == null || l.CaseFile.Equals(caseFile))
        .Where(l => responsibleEmployee == null || l.ResponsibleEmployee.Equals(responsibleEmployee));
    }
    
    /// <summary>
    /// Получение договорных документов для контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="dateFrom">Дата регистрации документа от.</param>
    /// <param name="dateTo">Дата регистрации документа по.</param>
    /// <returns>Выборка договорных документов, удовлетворяющих условиям.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IContractualDocument> GetContractualDocsWithCounterparty(Parties.ICounterparty counterparty,
                                                                                      DateTime? dateFrom, DateTime? dateTo)
    {
      if (dateTo != null)
        dateTo = dateTo.Value.AddDays(1);

      return ContractualDocuments.GetAll()
        .Where(r => dateFrom == null || r.RegistrationDate >= dateFrom)
        .Where(r => dateTo == null || r.RegistrationDate < dateTo)
        .Where(r => r.Counterparty.Equals(counterparty));
    }
    
    #endregion
    
    #region Рассылка по пролонгации
    
    /// <summary>
    /// Сотрудники, которых необходимо уведомить о сроке договора.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetNotificationPerformers(IContractBase contract)
    {
      var performer = contract.ResponsibleEmployee ?? Employees.As(contract.Author);
      var performers = new List<IUser>() { };
      
      if (performer == null)
        return performers;
      
      var manager = Docflow.PublicFunctions.Module.Remote.GetManager(performer);
      
      var performerPersonalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(performer).MyContractsNotification;
      
      if (performerPersonalSetting == true)
        performers.Add(performer);
      if (manager != null)
      {
        var managerPersonalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(manager).MySubordinatesContractsNotification;
        if (managerPersonalSetting == true)
          performers.Add(manager);
      }
      
      return performers;
    }
    
    #endregion
    
    /// <summary>
    /// Отфильтровать договора в зависимости от ЖЦ.
    /// </summary>
    /// <param name="query">Выборка договоров.</param>
    /// <returns>Отфильтрованные договора.</returns>
    public static IQueryable<IContractBase> FilterContractsByLifeCycleState(IQueryable<IContractBase> query)
    {
      return query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete) &&
                         !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Terminated) &&
                         !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Closed));
    }
    
    /// <summary>
    /// Найти договор. Применяется при переходе по ссылке из 1С.
    /// </summary>
    /// <param name="uuid">Uuid договора в 1С.</param>
    /// <param name="number">Номер договора.</param>
    /// <param name="date">Дата договора.</param>
    /// <param name="businessUnitTIN">ИНН НОР.</param>
    /// <param name="businessUnitTRRC">КПП НОР.</param>
    /// <param name="counterpartyUuid">Uuid контрагента в 1С.</param>
    /// <param name="counterpartyTIN">ИНН контрагента.</param>
    /// <param name="counterpartyTRRC">КПП контрагента.</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    /// <returns>Список найденных договоров.</returns>
    [Remote(IsPure = true)]
    public static List<IContractualDocument> FindContract(string uuid, string number, string date,
                                                          string businessUnitTIN, string businessUnitTRRC,
                                                          string counterpartyUuid, string counterpartyTIN, string counterpartyTRRC,
                                                          string sysid)
    {
      // Найти документ среди синхронизированных ранее.
      if (!string.IsNullOrWhiteSpace(uuid) && !string.IsNullOrWhiteSpace(sysid))
      {
        // Получить GUID типа Договор и доп.соглашение из разработки.
        var etalonContractTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(IContract).GetFinalType()).NameGuid.ToString();
        var etalonSubAgreementTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(ISupAgreement).GetFinalType()).NameGuid.ToString();
        
        var extLinks = Commons.PublicFunctions.Module.GetExternalEntityLinks(uuid, sysid)
          .Where(x => x.EntityType == etalonContractTypeGuid || x.EntityType == etalonSubAgreementTypeGuid)
          .ToList();
        var contractIds = extLinks.Where(x => x.EntityType.ToUpper() == etalonContractTypeGuid.ToUpper()).Select(x => x.EntityId).ToList();
        var supAgreeIds = extLinks.Where(x => x.EntityType.ToUpper() == etalonSubAgreementTypeGuid.ToUpper()).Select(x => x.EntityId).ToList();
        
        var existDocuments = new List<IContractualDocument>();
        existDocuments.AddRange(Contracts.GetAll().Where(x => contractIds.Contains(x.Id)));
        existDocuments.AddRange(SupAgreements.GetAll().Where(x => supAgreeIds.Contains(x.Id)));
        
        if (existDocuments.Any())
          return existDocuments;
      }
      
      var result = ContractualDocuments.GetAll();
      
      // Фильтр по НОР.
      if (string.IsNullOrWhiteSpace(businessUnitTIN) || string.IsNullOrWhiteSpace(businessUnitTRRC))
        return new List<IContractualDocument>();
      
      var businessUnit = Sungero.Company.BusinessUnits.GetAll().FirstOrDefault(x => x.TIN == businessUnitTIN && x.TRRC == businessUnitTRRC);
      if (businessUnit == null)
        return new List<IContractualDocument>();
      else
        result = result.Where(x => Equals(x.BusinessUnit, businessUnit));
      
      // Фильтр по номеру.
      if (string.IsNullOrEmpty(number))
      {
        var emptyNumberSynonyms = new List<string> { "б/н", "бн", "б-н", "б.н.", "б\\н" };
        result = result.Where(x => emptyNumberSynonyms.Contains(x.RegistrationNumber.ToLower()));
      }
      else
      {
        result = result.Where(x => x.RegistrationNumber == number);
      }
      
      // Фильтр по дате.
      DateTime parsedDate;
      if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParseExact(date,
                                                                     "dd'.'MM'.'yyyy",
                                                                     System.Globalization.CultureInfo.InvariantCulture,
                                                                     System.Globalization.DateTimeStyles.None,
                                                                     out parsedDate))
        result = result.Where(x => x.RegistrationDate == parsedDate);
      
      // Фильтр по контрагенту.
      var counterparties = Sungero.Parties.PublicFunctions.Module.Remote.FindCounterparty(counterpartyUuid, counterpartyTIN, counterpartyTRRC, sysid);
      if (counterparties.Any())
        result = result.Where(x => counterparties.Contains(x.Counterparty));
      
      return result.ToList();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Договоры. Рассылка задач об окончании срока действия договоров".
    /// </summary>
    [Public, Remote]
    public static void RequeueSendNotificationForExpiringContracts()
    {
      Jobs.SendNotificationForExpiringContracts.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Договоры. Рассылка задач о выполнении работ по договору".
    /// </summary>
    [Public, Remote]
    public static void RequeueSendTaskForContractMilestones()
    {
      Jobs.SendTaskForContractMilestones.Enqueue();
    }
    
    /// <summary>
    /// Получить ответственного за договор.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сотрудник.</returns>
    [Public]
    public virtual IEmployee GetPerformerContractResponsible(IOfficialDocument document)
    {
      if (ContractualDocuments.Is(document))
        return ContractualDocuments.As(document).ResponsibleEmployee;
      else if (Docflow.AccountingDocumentBases.Is(document))
        return Docflow.AccountingDocumentBases.As(document).ResponsibleEmployee;
      return null;
    }
  }
}
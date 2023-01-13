using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore.Server
{
  partial class BusinessUnitBoxFunctions
  {
    #region Авторизация и приглашения
    
    /// <summary>
    /// Получить ящики, у которых настроено подключение к сервису.
    /// </summary>
    /// <returns>Ящики.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IBusinessUnitBox> GetConnectedBoxes()
    {
      return BusinessUnitBoxes.GetAll(b => Equals(b.ConnectionStatus, ConnectionStatus.Connected));
    }
    
    /// <summary>
    /// Получить все действующие ящики.
    /// </summary>
    /// <returns>Ящики.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<IBusinessUnitBox> GetActiveBoxes()
    {
      return BusinessUnitBoxes.GetAll(b => b.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
    }

    /// <summary>
    /// Авторизация в сервисе обмена.
    /// </summary>
    /// <param name="password">Пароль.</param>
    /// <returns>Пустую строку, если авторизация успешна, иначе текст ошибки.</returns>
    [Remote]
    public string Login(string password)
    {
      var result = this.LoginWithoutSave(Encryption.Encrypt(password));
      if (string.IsNullOrWhiteSpace(result))
        _obj.Save();
      
      return result;
    }
    
    private string LoginWithoutSave(string encryptedPassword)
    {
      try
      {
        this.CheckExchangeConnectorLicense();
      }
      catch (AppliedCodeException ex)
      {
        return ex.Message;
      }
      
      if (_obj.ExchangeService == null)
        return BusinessUnitBoxes.Resources.ExchangeServiceNotFound;
      
      NpoComputer.DCX.ClientApi.Client.Initialize((l, msg, args) =>
                                                  {
                                                    if (l == NpoComputer.DCX.Common.LogLevel.Error || l == NpoComputer.DCX.Common.LogLevel.Fatal)
                                                      Logger.ErrorFormat(msg, args);
                                                    else
                                                      Logger.DebugFormat(msg, args);
                                                  }, Logger.ErrorFormat);
      NpoComputer.DCX.Common.ExchangeSystem exchangeServiceType;
      var serviceName = _obj.ExchangeService.ExchangeProvider.Value.Value;
      if (!NpoComputer.DCX.Common.ExchangeSystem.TryParse(serviceName, out exchangeServiceType))
      {
        return BusinessUnitBoxes.Resources.ExchangeServiceNotSupportedFormat(serviceName);
      }
      
      NpoComputer.DCX.ClientApi.Client client = null;

      try
      {
        var serviceSettings = new NpoComputer.DCX.Common.ServiceSettings()
        {
          OurOrganizationInn = _obj.BusinessUnit.TIN,
          OurOrganizationKpp = _obj.BusinessUnit.TRRC,
          ServiceUrl = _obj.ExchangeService.Uri
        };
        
        client = new NpoComputer.DCX.ClientApi.Client(exchangeServiceType, serviceSettings, null);
      }
      catch (Exception ex)
      {
        var innerExceptionText = ex.InnerException != null
          ? string.Format("{0}", ex.InnerException.Message)
          : string.Empty;
        Logger.ErrorFormat("Exchange. Create client error. {0}. {1}", ex.Message, innerExceptionText);
        
        return BusinessUnitBoxes.Resources.ExchangeServiceConnectionError;
      }
      
      try
      {
        client.Login(_obj.Login, Encryption.Decrypt(encryptedPassword));
      }
      catch (Exception ex)
      {
        var innerExceptionText = ex.InnerException != null
          ? string.Format("{0}", ex.InnerException.Message)
          : string.Empty;
        Logger.ErrorFormat("Exchange. Connection error. {0}. {1}", ex.Message, innerExceptionText);
        return ex.Message;
      }
      
      if (!string.IsNullOrWhiteSpace(_obj.OrganizationId) && !Equals(_obj.OrganizationId, client.OurSubscriber.Organization.OrganizationId))
        return BusinessUnitBoxes.Resources.LoginChangedToOtherOrganization;
      
      // TODO проверка регистрации сертификата ответственного на сервере обмена.
      if (encryptedPassword != _obj.Password)
        _obj.Password = encryptedPassword;
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Connected)
        _obj.ConnectionStatus = ConnectionStatus.Connected;
      if (string.IsNullOrWhiteSpace(_obj.OrganizationId))
        _obj.OrganizationId = client.OurSubscriber.Organization.OrganizationId;
      if (string.IsNullOrWhiteSpace(_obj.FtsId))
        _obj.FtsId = client.OurSubscriber.Organization.FnsParticipantId;
      return string.Empty;
    }
    
    /// <summary>
    /// Проверить, приобретена ли лицензия на коннектор к сервису обмена.
    /// </summary>
    private void CheckExchangeConnectorLicense()
    {
      var moduleGuid = Guid.Empty;
      if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        moduleGuid = Constants.BusinessUnitBox.ExchangeCoreDiadocGiud;
      else if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Sbis)
        moduleGuid = Constants.BusinessUnitBox.ExchangeCoreSBISGiud;
      
      if (moduleGuid != Guid.Empty && !Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(moduleGuid))
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ConnectorNoLicenseFormat(_obj.ExchangeService.Info.Properties.ExchangeProvider.GetLocalizedValue(_obj.ExchangeService.ExchangeProvider)));
    }
    
    /// <summary>
    /// Проверить возможность подключения.
    /// </summary>
    /// <returns>Текст проблемы, если она обнаружена.</returns>
    /// <remarks>Может менять статус подключения и сохранять сущность.</remarks>
    [Public, Remote]
    public string CheckConnection()
    {
      var needSave = !_obj.State.IsChanged;
      var result = this.LoginWithoutSave(_obj.Password);
      if (string.IsNullOrWhiteSpace(result))
      {
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Connected)
          _obj.ConnectionStatus = ConnectionStatus.Connected;
      }
      else
      {
        if (_obj.Status == CoreEntities.DatabookEntry.Status.Active && _obj.ConnectionStatus != ConnectionStatus.Error)
        {
          if (string.IsNullOrEmpty(_obj.Password))
            _obj.ConnectionStatus = ConnectionStatus.Waiting;
          else
            _obj.ConnectionStatus = ConnectionStatus.Error;
        }
      }

      if (needSave && _obj.State.IsChanged)
        _obj.Save();
      
      return result;
    }
    
    /// <summary>
    /// Отправить приглашение КА.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="invitationNote">Текст приглашения.</param>
    /// <returns>Текст ошибки или пустая строка.</returns>
    [Remote, Public]
    public string SendInvitation(Parties.ICounterparty counterparty, string invitationNote)
    {
      NpoComputer.DCX.ClientApi.Client client = null;
      List<NpoComputer.DCX.Common.Organization> organizations = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organizations = this.GetOrganizations(counterparty).ToList();
        if (!organizations.Any())
          return BusinessUnitBoxes.Resources.CounterpartyNotFoundFormat(counterparty.Name, _obj.ExchangeService.Info.Properties.ExchangeProvider.GetLocalizedValue(_obj.ExchangeService.ExchangeProvider));
        
        foreach (var organization in organizations)
          client.SendInvitationRequest(organization, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error on send invitation to counterparty {0}.", ex, counterparty.Id);
        result = ex.Message;
      }
      finally
      {
        if (organizations != null)
          foreach (var organization in organizations.OrderBy(o => o.IsRoaming))
            this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result));
      }
      
      return result;
    }
    
    /// <summary>
    /// Отправить приглашение КА.
    /// </summary>
    /// <param name="organizationId">ИД организации в сервисе обмена.</param>
    /// <param name="counterpartyName">Наименование контрагента для отображения в ошибках.</param>
    /// <param name="invitationNote">Текст приглашения.</param>
    /// <returns>Текст ошибки или пустая строка.</returns>
    [Remote, Public]
    public string SendInvitation(string organizationId, string counterpartyName, string invitationNote)
    {
      NpoComputer.DCX.ClientApi.Client client = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        client.SendInvitationRequest(new NpoComputer.DCX.Common.Organization { OrganizationId = organizationId }, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error on send invitation to counterparty {0}.", ex, counterpartyName);
        result = ex.Message;
      }
      
      return result;
    }
    
    /// <summary>
    /// Проверка возможности отправить приглашение контрагенту через ящик.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>ИД организации, если найдена в сервисе.</returns>
    /// <remarks>Только полное совпадение по ИНН и КПП.</remarks>
    [Remote(IsPure = true), Public]
    public List<string> CanSendInvitationFrom(Parties.ICounterparty counterparty)
    {
      return this.GetOrganizations(counterparty).Select(o => o.OrganizationId).ToList();
    }
    
    private IEnumerable<NpoComputer.DCX.Common.Organization> GetOrganizations(Parties.ICounterparty counterparty)
    {
      var company = Parties.CompanyBases.As(counterparty);
      if (company != null)
      {
        var organizations = this.GetDCXOrganizations(company.TIN, company.TRRC);
        foreach (var organization in organizations)
          if (organization.Inn == company.TIN && organization.Kpp == company.TRRC)
            yield return organization;
      }
      else
      {
        var organizations = this.GetDCXOrganizations(counterparty.TIN, string.Empty);
        foreach (var organization in organizations)
          if (organization.Inn == counterparty.TIN && string.IsNullOrWhiteSpace(organization.Kpp))
            yield return organization;
      }
    }
    
    private List<NpoComputer.DCX.Common.Organization> GetDCXOrganizations(string tin, string trrc)
    {
      var empty = new List<NpoComputer.DCX.Common.Organization>();
      var client = this.GetClient();
      var organizations = client.FindOrganizationsByInnKpp(tin, trrc);
      if (organizations == null || !organizations.Any())
        return empty;
      
      return organizations;
    }
    
    private void CounterpartyStatusRefresh(NpoComputer.DCX.Common.Organization organization,
                                           NpoComputer.DCX.ClientApi.Client client,
                                           Parties.ICounterparty counterparty,
                                           string invitationText, bool existNote)
    {
      if (client != null && client.IsLoggedIn() && organization != null)
      {
        var organizationId = organization.OrganizationId;
        var status = client.GetContactStatus(organization);
        var boxLine = counterparty.ExchangeBoxes.SingleOrDefault(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, organizationId)) ??
          counterparty.ExchangeBoxes.AddNew();
        boxLine.Box = _obj;
        boxLine.Status = this.GetCounterpartyExchangeStatus(status);
        boxLine.OrganizationId = organizationId;
        boxLine.IsRoaming = organization.IsRoaming;
        boxLine.FtsId = organization.FnsParticipantId;
        if (existNote)
          boxLine.InvitationText = invitationText;

        this.SetIsDefault(counterparty);
        
        boxLine.CounterpartyBox = organization.IsRoaming ?
          BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(organization.ExchangeServiceName) :
          BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
        
        if (counterparty.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && b.IsDefault == true))
          counterparty.Save();
      }
    }
    
    /// <summary>
    /// Принять приглашение от контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyId">Ид контрагента в сервисе обмена.</param>
    /// <param name="invitationNote">Комментарий к запросу.</param>
    /// <returns>Строка с ошибкой при принятии приглашения. Пусто - если приглашение принято.</returns>
    [Remote, Public]
    public string AcceptInvitation(Parties.ICounterparty counterparty, string counterpartyId, string invitationNote)
    {
      var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(_obj);
      if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        return boxNotActiveMessage;
      
      NpoComputer.DCX.ClientApi.Client client = null;
      NpoComputer.DCX.Common.Organization organization = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organization = client.GetContact(counterpartyId).Organization;
        client.AcceptInvitation(organization, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error in box {0} on invitation accept from counterparty {1}.", ex, _obj.Id, counterparty.Id);
        result = ex.Message;
      }
      finally
      {
        this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result));
      }
      
      return result;
    }
    
    /// <summary>
    /// Отклонить приглашение контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="counterpartyId">Ид контрагента в сервисе обмена.</param>
    /// <param name="invitationNote">Комментарий к запросу.</param>
    /// <returns>Строка с ошибкой при отклонении приглашения. Пусто - если приглашение отклонено.</returns>
    [Remote, Public]
    public string RejectInvitation(Parties.ICounterparty counterparty, string counterpartyId, string invitationNote)
    {
      var boxNotActiveMessage = Functions.BusinessUnitBox.CheckBusinessUnitBoxActive(_obj);
      if (!string.IsNullOrWhiteSpace(boxNotActiveMessage))
        return boxNotActiveMessage;
      
      NpoComputer.DCX.ClientApi.Client client = null;
      NpoComputer.DCX.Common.Organization organization = null;
      var result = string.Empty;
      try
      {
        client = this.GetClient();
        organization = client.GetContact(counterpartyId).Organization;
        client.RejectInvitation(organization, invitationNote);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error in box {0} on invitation reject from counterparty {1}.", ex, _obj.Id, counterparty.Id);
        result = ex.Message;
      }
      finally
      {
        this.CounterpartyStatusRefresh(organization, client, counterparty, invitationNote, string.IsNullOrWhiteSpace(result));
      }
      
      return result;
    }
    
    /// <summary>
    /// Сконвертировать статус КА из DCX в статус КА в RX.
    /// </summary>
    /// <param name="status">Статус в DCX.</param>
    /// <returns>Статус RX.</returns>
    public Enumeration GetCounterpartyExchangeStatus(NpoComputer.DCX.Common.ContactStatus status)
    {
      switch (status)
      {
        case NpoComputer.DCX.Common.ContactStatus.ApprovingByCounteragent:
          return Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA;
        case NpoComputer.DCX.Common.ContactStatus.ApprovingByUs:
          return Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs;
        case NpoComputer.DCX.Common.ContactStatus.Active:
          return Parties.CounterpartyExchangeBoxes.Status.Active;
        case NpoComputer.DCX.Common.ContactStatus.Closed:
          return Parties.CounterpartyExchangeBoxes.Status.Closed;
        default:
          throw new Exception("Invalid value for ContactStatus");
      }
    }
    
    /// <summary>
    /// Получить клиента для вызова из другого модуля.
    /// </summary>
    /// <returns>Подключенный к сервису клиент.</returns>
    [Public]
    public object GetPublicClient()
    {
      return this.GetClient();
    }
    
    /// <summary>
    /// Получить клиента для ящика.
    /// </summary>
    /// <returns>Подключенный к сервису клиент.</returns>
    /// <exception cref="AppliedCodeException">СО не поддерживается или логин\пароль не прошли авторизацию.</exception>
    public NpoComputer.DCX.ClientApi.Client GetClient()
    {
      if (string.IsNullOrEmpty(_obj.Password))
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.WrongPassword);
      
      this.CheckExchangeConnectorLicense();
      
      if (_obj.ExchangeService == null)
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ExchangeServiceNotFound);
      
      NpoComputer.DCX.ClientApi.Client.Initialize((l, msg, args) =>
                                                  {
                                                    if (l == NpoComputer.DCX.Common.LogLevel.Error || l == NpoComputer.DCX.Common.LogLevel.Fatal)
                                                      Logger.ErrorFormat(msg, args);
                                                    else
                                                      Logger.DebugFormat(msg, args);
                                                  }, Logger.ErrorFormat);
      NpoComputer.DCX.Common.ExchangeSystem exchangeServiceType;
      var serviceName = _obj.ExchangeService.ExchangeProvider.Value.Value;
      if (!NpoComputer.DCX.Common.ExchangeSystem.TryParse(serviceName, out exchangeServiceType))
      {
        throw AppliedCodeException.Create(BusinessUnitBoxes.Resources.ExchangeServiceNotSupportedFormat(serviceName));
      }
      
      NpoComputer.DCX.ClientApi.Client client = null;

      try
      {
        NpoComputer.DCX.Common.ConnectorSettings connectorSetting = new NpoComputer.DCX.Common.ConnectorSettings(true);
        client = NpoComputer.DCX.ClientApi.Client.Get(exchangeServiceType,
                                                      _obj.BusinessUnit.TIN, _obj.BusinessUnit.TRRC,
                                                      _obj.ExchangeService.Uri,
                                                      new Uri(_obj.ExchangeService.LogonUrl).GetLeftPart(UriPartial.Authority),
                                                      _obj.Login, Encryption.Decrypt(_obj.Password), connectorSetting);
        
      }
      catch (Exception ex)
      {
        throw AppliedCodeException.Create(ex.Message, ex);
      }
      
      return client;
    }

    #endregion
    
    #region Синхронизация ящиков
    
    /// <summary>
    /// Синхронизация ящиков.
    /// </summary>
    public void SyncBoxStatus()
    {
      ((Domain.Shared.IExtendedEntity)_obj).Params[Constants.BoxBase.JobRunned] = true;
      this.CheckConnection();
    }
    
    #endregion
    
    #region Синхронизация КА
    
    /// <summary>
    /// Синхронизация контрагентов с сервисом обмена.
    /// </summary>
    /// <returns>True - если синхронизация завершилась успешно.</returns>
    [Public]
    public virtual bool SyncBoxCounterparties()
    {
      try
      {
        var lastSync = Functions.Module.GetLastSyncDate(_obj);
        var client = Functions.BusinessUnitBox.GetClient(_obj);
        var lastSyncTicks = lastSync.Ticks / TimeSpan.TicksPerMillisecond;
        var allContacts = client.GetContacts();
        var counterparties = allContacts
          .Where(l => (l.StatusChangeDate.Value.Ticks / TimeSpan.TicksPerMillisecond) > lastSyncTicks)
          .ToList();
        
        foreach (var counterparty in counterparties)
        {
          if (CounterpartyQueueItems.GetAll(c => c.ExternalId == counterparty.Organization.OrganizationId && Equals(c.Box, _obj)).Any())
            continue;

          var queueItem = CounterpartyQueueItems.Create();
          queueItem.ExternalId = counterparty.Organization.OrganizationId;
          queueItem.Box = _obj;
          queueItem.RootBox = _obj;
          queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.NotProcessed;
          queueItem.Save();
          var logMessage = string.Format("Create queue item for counterparty OrganizationId {0}, TIN {1}, KPP {2}", counterparty.Organization.OrganizationId, counterparty.Organization.Inn, counterparty.Organization.Kpp);
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, logMessage);
        }
        
        if (counterparties.Any())
          Functions.Module.UpdateLastSyncDate(counterparties.OrderByDescending(c => c.StatusChangeDate).First().StatusChangeDate.Value, _obj);
        
        var queueItems = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj)).ToList();
        
        // Дозагрузка контрагентов из очереди.
        var addedItems = queueItems.Where(x => !counterparties.Any(c => c.Organization.OrganizationId == x.ExternalId)).ToList();
        foreach (var queueItem in addedItems)
        {
          Transactions.Execute(
            () =>
            {
              var contact = allContacts.FirstOrDefault(c => c.Organization.OrganizationId == queueItem.ExternalId);
              if (contact == null)
                contact = client.GetContact(queueItem.ExternalId);
              
              counterparties.Add(contact);
            });
        }
        
        // Обновить дату сихронизации для Сбис, если в очереди есть КА для синхронизации.
        if (_obj.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Sbis && counterparties.Any())
          Functions.Module.UpdateLastSyncDate(Calendar.Now, _obj);
        
        var counterpartiesConflict = new Dictionary<NpoComputer.DCX.Common.IContact, List<Parties.ICounterparty>>();
        
        foreach (var party in counterparties
                 .OrderBy(c => c.Status != NpoComputer.DCX.Common.ContactStatus.Active)
                 .ThenBy(c => c.Organization.IsRoaming))
        {
          var queueItem = queueItems.Single(q => q.ExternalId == party.Organization.OrganizationId);
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "Start processing sync counterparty.");
          Transactions.Execute(
            () =>
            {
              var allCounterparties = Parties.Counterparties.GetAll();
              var organizationId = party.Organization.OrganizationId;
              var specificCounterparty = allCounterparties.FirstOrDefault(c => c.ExchangeBoxes.Any(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, organizationId)));
              if (specificCounterparty != null)
              {
                Functions.BusinessUnitBox.UpdateExchangeStatus(_obj, specificCounterparty, party, queueItem);
                queueItem.Counterparty = specificCounterparty;
              }
              else
              {
                var doubles = Functions.BusinessUnitBox.TryCompareCounterparty(_obj, party, allCounterparties, queueItem);
                if (doubles.Any())
                {
                  counterpartiesConflict.Add(party, doubles);
                  return;
                }
              }
              
              queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.TaskSendWaiting;
              queueItem.Save();
            });
          Exchange.PublicFunctions.Module.LogDebugFormat(queueItem, "End processing sync counterparty.");
        }
        
        if (counterpartiesConflict.Any())
          Functions.BusinessUnitBox.CreateConflictTask(_obj, counterpartiesConflict);
        
        var allQueueItems = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj))
          .Where(q => Equals(q.ProcessingStatus, ExchangeCore.CounterpartyQueueItem.ProcessingStatus.TaskSendWaiting)).ToList();
        if (allQueueItems.Any(x => x.SyncResult != null))
        {
          var begin = 0;
          var total = Constants.BusinessUnitBox.CounterpartySyncBatchSize;
          var queueItemsForNotice = allQueueItems.Skip(begin).Take(total).ToList();
          
          while (queueItemsForNotice.Any())
          {
            Transactions.Execute(
              () =>
              {
                Functions.BusinessUnitBox.CreateNotice(_obj, queueItemsForNotice);
              });

            begin += total;
            queueItemsForNotice = allQueueItems.Skip(begin).Take(total).ToList();
          }
        }
        
        Functions.BusinessUnitBox.FillCounterpartiesFtsIds(_obj, client, allContacts);
        
        return true;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat(BusinessUnitBoxes.Resources.BoxError, ex, _obj.Id);
        return false;
      }
    }
    
    /// <summary>
    /// Дозаполнить ФНС ИД у существующих контрагентов.
    /// </summary>
    /// <param name="client">Клиент.</param>
    /// <param name="contacts">Контакты из сервиса.</param>
    public virtual void FillCounterpartiesFtsIds(NpoComputer.DCX.ClientApi.Client client, List<NpoComputer.DCX.Common.IContact> contacts)
    {
      var counterpartiesWithEmptyFtsId = Parties.Counterparties.GetAll(c => c.ExchangeBoxes.Any(x => Equals(x.Box, _obj) &&
                                                                                                (x.FtsId == null || x.FtsId == string.Empty)));
      foreach (var counterparty in counterpartiesWithEmptyFtsId)
      {
        foreach (var exchangeBox in counterparty.ExchangeBoxes.Where(x => Equals(x.Box, _obj) &&
                                                                     (x.FtsId == null || x.FtsId == string.Empty)))
        {
          var contact = contacts.FirstOrDefault(c => c.Organization.OrganizationId == exchangeBox.OrganizationId);
          if (contact == null)
            contact = client.GetContact(exchangeBox.OrganizationId);
          
          exchangeBox.FtsId = contact.Organization.FnsParticipantId;
        }
        
        counterparty.Save();
      }
    }
    
    /// <summary>
    /// Обновить статус обмена в контрагенте.
    /// </summary>
    /// <param name="counterparty">Контрагент из RX.</param>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    public virtual void UpdateExchangeStatus(Parties.ICounterparty counterparty, NpoComputer.DCX.Common.IContact contact, ICounterpartyQueueItem queueItem)
    {
      var counterpartyExchangeBox = counterparty.ExchangeBoxes.Single(b => Equals(b.Box, _obj) && Equals(b.OrganizationId, contact.Organization.OrganizationId));
      var exchangeStatus = Functions.BusinessUnitBox.GetCounterpartyExchangeStatus(_obj, contact.Status);
      var needSendInvitation = false;
      
      var incomingTask = IncomingInvitationTasks.GetAll().Where(x => Equals(x.Box, _obj) &&
                                                                Equals(x.Counterparty, counterparty) &&
                                                                Equals(x.OrganizationId, contact.Organization.OrganizationId) &&
                                                                Equals(x.Status, Workflow.Task.Status.InProcess)).FirstOrDefault();
      var incomingAssignment = IncomingInvitationAssignments.GetAll().Where(x => Equals(x.Task, incomingTask) &&
                                                                            Equals(x.Status, Workflow.AssignmentBase.Status.InProcess)).FirstOrDefault();
      var noticeLine = BusinessUnitBoxes.Resources.NoticeLineFormat(string.Empty, contact.Organization.Inn, contact.Organization.Kpp);
      
      #region Нам пришло приглашение
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
      {
        // Создаем отложенно, контрагент должен быть валиден и сохранен.
        if (incomingTask == null)
          needSendInvitation = true;
      }
      
      #endregion
      
      #region Мы отправили приглашение
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA)
      {
        if (counterpartyExchangeBox.Status == Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
        {
          if (incomingTask != null)
            incomingTask.Abort();
        }
      }
      
      #endregion
      
      #region Обмен установлен
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Active)
      {
        if (incomingAssignment != null)
        {
          incomingAssignment.ActiveText = contact.Comment;
          incomingAssignment.Save();
          incomingAssignment.Complete(ExchangeCore.IncomingInvitationAssignment.Result.Accept);
        }
      }
      
      #endregion
      
      #region Обмен запрещен
      
      if (exchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Closed)
      {
        if (incomingAssignment != null)
        {
          incomingAssignment.ActiveText = contact.Comment;
          incomingAssignment.Save();
          incomingAssignment.Complete(ExchangeCore.IncomingInvitationAssignment.Result.Reject);
        }
      }
      
      #endregion
      
      counterpartyExchangeBox.Status = exchangeStatus;
      counterpartyExchangeBox.FtsId = contact.Organization.FnsParticipantId;
      
      if (!string.IsNullOrEmpty(contact.Comment) && contact.Comment.Length > counterpartyExchangeBox.Info.Properties.InvitationText.Length)
        counterpartyExchangeBox.InvitationText = contact.Comment.Substring(0, counterpartyExchangeBox.Info.Properties.InvitationText.Length);
      else
        counterpartyExchangeBox.InvitationText = contact.Comment;
      
      counterpartyExchangeBox.IsRoaming = contact.Organization.IsRoaming;
      counterpartyExchangeBox.CounterpartyBox = contact.Organization.IsRoaming ?
        BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(contact.Organization.ExchangeServiceName) :
        BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
      
      Functions.BusinessUnitBox.SetIsDefault(_obj, counterparty);
      
      counterparty.Save();
      
      if (queueItem != null)
      {
        queueItem.SyncResult = exchangeStatus;
        queueItem.Note = noticeLine;
        queueItem.Save();
      }
      
      if (needSendInvitation)
        Functions.IncomingInvitationTask.Create(counterparty, _obj, contact.Organization.OrganizationId, contact.Comment);
      
      Logger.DebugFormat("Updated exchange status {0} for counterparty Id {1}", exchangeStatus, counterparty.Id);
    }
    
    /// <summary>
    /// Пересчитать признак IsDefault.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    public virtual void SetIsDefault(Parties.ICounterparty counterparty)
    {
      var boxLines = counterparty.ExchangeBoxes.Where(b => Equals(b.Box, _obj));
      var defaultLine = boxLines.SingleOrDefault(b => b.IsDefault == true);
      if (defaultLine == null || defaultLine.Status != Parties.CompanyBaseExchangeBoxes.Status.Active)
      {
        var activeLine = boxLines
          .OrderBy(l => l.Status != Parties.CompanyBaseExchangeBoxes.Status.Active)
          .ThenBy(l => l.IsRoaming == true)
          .First();
        if (activeLine != null)
          activeLine.IsDefault = true;
      }
    }
    
    /// <summary>
    /// Сопоставить контрагента из сервиса обмена с контрагентом из RX.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <param name="counterparties">Список всех контрагентов из RX.</param>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>Если сопоставить контрагентов не удалось, вернуть список контрагентов, помешавших найти однозначное соответствие.</returns>
    public virtual List<Parties.ICounterparty> TryCompareCounterparty(NpoComputer.DCX.Common.IContact contact, System.Collections.Generic.IEnumerable<Parties.ICounterparty> counterparties,
                                                                      ICounterpartyQueueItem queueItem)
    {
      var counterpartiesWithSameTin = counterparties.Where(c => Equals(c.TIN, contact.Organization.Inn) && Equals(c.Status, Sungero.CoreEntities.DatabookEntry.Status.Active)).ToList();
      var allCompanies = counterpartiesWithSameTin.Select(c => Parties.CompanyBases.As(c)).Where(c => c != null);
      var companiesWithSameTinTrrc = allCompanies.Where(c => Equals(c.TRRC, contact.Organization.Kpp)).ToList();
      var companiesWithSameTinWithoutTrrc = allCompanies.Where(c => string.IsNullOrWhiteSpace(c.TRRC)).ToList();
      Parties.ICounterparty comparedCounterparty = null;
      var doubles = new List<Parties.ICounterparty>();

      if (counterpartiesWithSameTin.Count == 0)
      {
        Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Counterparty for contact {0} with TIN not exists.", contact.Organization.OrganizationId));
        comparedCounterparty = Functions.BusinessUnitBox.CreateCounterparty(_obj, contact);
      }
      else
      {
        if (string.IsNullOrWhiteSpace(contact.Organization.Kpp))
        {
          // Нашли явно организацию без кпп или банк.
          if (companiesWithSameTinWithoutTrrc.Count == 1)
          {
            comparedCounterparty = companiesWithSameTinWithoutTrrc.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one counterparty with same TIN, id {0}.", comparedCounterparty.Id));
          }
          
          // Организаций с таким ИНН нет, нашли персону.
          if (!allCompanies.Any() && counterpartiesWithSameTin.Count == 1)
          {
            comparedCounterparty = counterpartiesWithSameTin.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one person with same TIN, id {0}.", comparedCounterparty.Id));
          }
          // Если не удалось найти подходящий справочник.
          if (comparedCounterparty == null)
          {
            if (companiesWithSameTinWithoutTrrc.Any())
            {
              doubles.AddRange(companiesWithSameTinWithoutTrrc);
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN, without TRRC. Organization without TRRC.");
            }
            else
            {
              doubles.AddRange(counterpartiesWithSameTin.Where(p => !Parties.CompanyBases.Is(p)));
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN. Organization without TRRC.");
            }
          }
        }
        else
        {
          // Нашли явно организацию с этим кпп.
          if (companiesWithSameTinTrrc.Count == 1)
          {
            comparedCounterparty = companiesWithSameTinTrrc.Single();
            Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Find one counterparty with same TIN, TRRC, id {0}.", comparedCounterparty.Id));
          }
          
          // Если не удалось найти подходящий справочник.
          if (comparedCounterparty == null)
          {
            if (companiesWithSameTinTrrc.Any())
            {
              doubles.AddRange(companiesWithSameTinTrrc);
              Exchange.PublicFunctions.Module.LogDebugFormat("Find many counterparties with same TIN, TRRC. Organization with TRRC.");
            }
            else
            {
              doubles.AddRange(allCompanies);
              Exchange.PublicFunctions.Module.LogDebugFormat("Find counterparties with same TIN with other TRRC.");
            }
          }
        }
      }

      if (comparedCounterparty != null)
      {
        var newCounterpartyExchangeBox = comparedCounterparty.ExchangeBoxes.AddNew();
        newCounterpartyExchangeBox.Box = _obj;
        newCounterpartyExchangeBox.OrganizationId = contact.Organization.OrganizationId;
        newCounterpartyExchangeBox.IsRoaming = contact.Organization.IsRoaming;
        newCounterpartyExchangeBox.CounterpartyBox = contact.Organization.IsRoaming ?
          BusinessUnitBoxes.Resources.IsRoamingCounterpartyBoxFormat(contact.Organization.ExchangeServiceName) :
          BusinessUnitBoxes.Resources.IsMainCounterpartyBoxFormat(_obj.ExchangeService.Name);
        Functions.BusinessUnitBox.UpdateExchangeStatus(_obj, comparedCounterparty, contact, queueItem);
        if (queueItem != null)
          queueItem.Counterparty = comparedCounterparty;
        Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Compared counterparty {0}, status updated.", comparedCounterparty.Id));
      }
      return doubles;
    }
    
    /// <summary>
    /// Создать контрагента.
    /// </summary>
    /// <param name="contact">Контрагент из сервиса обмена.</param>
    /// <returns>Контрагент.</returns>
    public virtual Parties.ICounterparty CreateCounterparty(NpoComputer.DCX.Common.IContact contact)
    {
      var isPerson = NpoComputer.DCX.Common.OrganizationType.Individual == contact.Organization.OrganizationType;
      
      Parties.ICounterparty counterparty = null;
      var organization = contact.Organization;
      if (isPerson)
      {
        var person = Parties.People.Create();
        // TODO: персоне нужны хотя бы имя и фамилия, поля обязательные.
        person.FirstName = string.IsNullOrEmpty(organization.FirstName) ? organization.Name : organization.FirstName;
        person.LastName = string.IsNullOrEmpty(organization.LastName) ? organization.Name : organization.LastName;
        person.MiddleName = string.IsNullOrEmpty(organization.MiddleName) ? string.Empty : organization.MiddleName;
        
        if (organization.RegistrationAddress != null)
          person.LegalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.RegistrationAddress);
        
        counterparty = person;
      }
      else
      {
        var company = Parties.Companies.Create();
        company.TRRC = organization.Kpp;
        var companyName = organization.Name;
        
        var namePropertyLength = company.Info.Properties.Name.Length;
        if (organization.Name.Length > namePropertyLength)
        {
          companyName = string.Format("{0}~", organization.Name.Substring(0, namePropertyLength - 1));
          company.Note += string.IsNullOrEmpty(company.Note) ? organization.Name : string.Format("{0}{1}", Environment.NewLine, organization.Name);
        }
        company.Name = companyName;
        
        company.LegalName = organization.LegalName;
        counterparty = company;
      }
      
      // TODO: сейчас не валидное ИНН не заполняется.
      if (string.IsNullOrWhiteSpace(Parties.PublicFunctions.Counterparty.CheckTin(organization.Inn, !isPerson)))
        counterparty.TIN = organization.Inn;
      else
        counterparty.Note = BusinessUnitBoxes.Resources.TINNoteFormat(organization.Inn);
      
      if (!string.IsNullOrEmpty(organization.Bik))
      {
        var bank = Parties.Banks.GetAll(x => x.BIC == organization.Bik).FirstOrDefault();
        if (bank != null)
        {
          counterparty.Bank = bank;
          counterparty.Account = organization.CurrentAccount;
        }
      }
      
      if (organization.LegalAddress != null)
      {
        counterparty.LegalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.LegalAddress);
        Functions.BusinessUnitBox.SetCityAndRegion(_obj, organization.LegalAddress, counterparty);
      }
      
      if (organization.MailAddress != null)
      {
        counterparty.PostalAddress = Functions.BusinessUnitBox.ConvertAddressToPostalFormat(organization.MailAddress);
        if (counterparty.Region == null && counterparty.City == null)
          Functions.BusinessUnitBox.SetCityAndRegion(_obj, organization.MailAddress, counterparty);
      }
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var phoneNumber = string.Empty;

        if (!string.IsNullOrEmpty(organization.Fax))
        {
          if (!string.IsNullOrEmpty(organization.PhoneNumber))
            phoneNumber = BusinessUnitBoxes.Resources.PhoneNumberFormat(organization.PhoneNumber);
          
          phoneNumber += BusinessUnitBoxes.Resources.FaxNumberFormat(organization.Fax);
        }
        else
          phoneNumber = organization.PhoneNumber;
        
        counterparty.Phones = phoneNumber;
      }

      counterparty.PSRN = organization.Ogrn;
      counterparty.Save();
      Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Created counterparty for contact {0}, id {1}.", contact.Organization.OrganizationId, counterparty.Id));
      return counterparty;
    }
    
    /// <summary>
    /// Очистить название населенного пункта от сокращений.
    /// </summary>
    /// <param name="localityName">Название населенного пункта.</param>
    /// <returns>Название, очищенное от сокращений.</returns>
    // TODO Dmitriev_IA: Подобную очистку нужно будет продумать еще и для Дома/Корпуса/Квартиры.
    private static string TrimAbbreviationsLocalityName(string localityName)
    {
      if (string.IsNullOrWhiteSpace(localityName))
        return string.Empty;
      
      var terms = localityName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
      var firstTerm = terms.FirstOrDefault();
      
      if (string.IsNullOrWhiteSpace(firstTerm))
        return string.Empty;
      
      var firstTermLowerCase = firstTerm.ToLower();
      
      // TODO Dmitriev_IA: Самая простая реализация, закрывающая очевидные кейсы. В дальнейшем наверняка придется переделать или дополнить.
      if (firstTermLowerCase == "г" ||
          firstTermLowerCase == "г." ||
          firstTermLowerCase == "гор" ||
          firstTermLowerCase == "гор." ||
          firstTermLowerCase == "город" ||
          firstTermLowerCase == "пгт" ||
          firstTermLowerCase == "пгт.")
        terms.Remove(firstTerm);
      
      return string.Join(" ", terms);
    }
    
    /// <summary>
    /// Преобразовать составляющие адреса DCX в адрес формата Почты России.
    /// </summary>
    /// <param name="addressTerms">Составляющие адреса DCX.</param>
    /// <returns>Адрес в формате Почты России.</returns>
    /// <remarks>Функция введена для тестирования.
    /// Необходимо, чтобы составляющие шли строго в следующем порядке:
    /// Почтовый индекс, Код региона, Город, Населенный пункт, Улица, Дом, Корпус, Квартира.</remarks>
    [Public, Remote(IsPure = true)]
    public static string ConvertAddressToPostalFormat(List<string> addressTerms)
    {
      if (addressTerms.Count < 8)
        return string.Empty;
      
      var address = new NpoComputer.DCX.Common.OrganizationAddress();
      address.PostalCode = addressTerms[0];
      address.RegionCode = addressTerms[1];
      address.City = addressTerms[2];
      address.Locality = addressTerms[3];
      address.Street = addressTerms[4];
      address.House = addressTerms[5];
      address.Building = addressTerms[6];
      address.Apartment = addressTerms[7];
      
      return Functions.BusinessUnitBox.ConvertAddressToPostalFormat(address);
    }
    
    /// <summary>
    /// Преобразовать адрес DCX в адрес формата Почты России.
    /// </summary>
    /// <param name="address">Адрес DCX.</param>
    /// <returns>Адрес в формате Почты России.</returns>
    public static string ConvertAddressToPostalFormat(NpoComputer.DCX.Common.OrganizationAddress address)
    {
      var addressTerms = new List<string>();
      
      var federalCities = new string[]
      {
        Constants.BusinessUnitBox.Moscow,
        Constants.BusinessUnitBox.SaintPetersburg,
        Constants.BusinessUnitBox.Sevastopol,
        Constants.BusinessUnitBox.Baikonur
      };
      
      var cityName = TrimAbbreviationsLocalityName(address.City);
      var localityName = TrimAbbreviationsLocalityName(address.Locality);
      
      // Сформировать наименование города или населенного пункта сервиса обмена.
      var city = string.Format("{0} {1}", Constants.BusinessUnitBox.City, cityName);
      var locality = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, localityName);
      
      // Обработать ситуацию, в которой город или населенный пункт перепутаны местами.
      var cityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.City, localityName);
      var localityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, cityName);
      
      // Найти соответствующий населенный пункт в RX.
      var cityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(city) || c.Name.Equals(cityRev));
      var localityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(locality) || c.Name.Equals(localityRev));
      
      // Подготовить составляющие части адреса в порядке почты России.
      // Улица.
      addressTerms.Add(!string.IsNullOrEmpty(address.Street) ?
                       address.Street :
                       string.Empty);
      // Дом.
      addressTerms.Add(!string.IsNullOrEmpty(address.House) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.House, address.House) :
                       string.Empty);
      // Строение.
      addressTerms.Add(!string.IsNullOrEmpty(address.Building) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.Building, address.Building) :
                       string.Empty);
      // Квартира.
      addressTerms.Add(!string.IsNullOrEmpty(address.Apartment) ?
                       string.Format("{0} {1}", Constants.BusinessUnitBox.Apartment, address.Apartment) :
                       string.Empty);
      
      // Населенный пункт, если смогли найти его в RX.
      if (localityRx != null)
        addressTerms.Add(localityRx.Name);
      else
        // Указать в качестве населенного пункта информацию, которая содержится в адресе из сервиса обмена,
        // если она не пуста и города с таким именем в RX не найдено.
        if (cityRx == null && !string.IsNullOrWhiteSpace(address.Locality))
          addressTerms.Add(address.Locality);
      
      // Город, если смогли найти его в RX.
      if (cityRx != null)
        addressTerms.Add(cityRx.Name);
      else
        // Указать в качестве города информацию, которая содержится в адресе из сервиса обмена,
        // если она не пуста и населенного пункта с таким именем в RX не найдено.
        if (localityRx == null && !string.IsNullOrWhiteSpace(address.City))
          addressTerms.Add(address.City);
      
      // Если город не является городом федерального значения, то заполняем регион. Иначе - регион пустой.
      if (!federalCities.Contains(city) && !federalCities.Contains(cityRev))
      {
        var region = Commons.Regions.GetAll().FirstOrDefault(r => r.Code.Equals(address.RegionCode));
        
        addressTerms.Add(region == null ? string.Empty : region.Name);
      }
      else
        addressTerms.Add(string.Empty);
      
      addressTerms.Add(!string.IsNullOrEmpty(address.PostalCode) ? address.PostalCode : string.Empty);
      
      // Убрать лишние запятые в кусках адреса пришедшего из СО, если таковые имеются.
      for (var i = 0; i < addressTerms.Count; i++)
        addressTerms[i] = addressTerms[i] == null ? addressTerms[i] : addressTerms[i].TrimEnd(new char[] { ',' });
      
      // Объединить куски адреса в одну строку, в порядке почты России.
      return string.Join(", ", addressTerms.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()).TrimEnd();
    }
    
    /// <summary>
    /// Заполнить населенный пункт и регион контрагента.
    /// </summary>
    /// <param name="address">Адрес из сервиса обмена.</param>
    /// <param name="counterparty">Контрагент.</param>
    public void SetCityAndRegion(NpoComputer.DCX.Common.OrganizationAddress address,
                                 Sungero.Parties.ICounterparty counterparty)
    {
      var federalCities = new string[]
      {
        Constants.BusinessUnitBox.Moscow,
        Constants.BusinessUnitBox.SaintPetersburg,
        Constants.BusinessUnitBox.Sevastopol,
        Constants.BusinessUnitBox.Baikonur
      };
      
      var cityName = TrimAbbreviationsLocalityName(address.City);
      var localityName = TrimAbbreviationsLocalityName(address.Locality);
      
      // Сформировать наименование города или населенного пункта сервиса обмена.
      var city = string.Format("{0} {1}", Constants.BusinessUnitBox.City, cityName);
      var locality = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, localityName);
      
      // Обработать ситуацию, в которой город или населенный пункт перепутаны местами.
      var cityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.City, localityName);
      var localityRev = string.Format("{0} {1}", Constants.BusinessUnitBox.Locality, cityName);
      
      // Если полученный город не совпадает с одним из городов федерального значения - найти регион по коду.
      Commons.IRegion regionRx = null;
      if (!federalCities.Contains(city) && !federalCities.Contains(cityRev))
        regionRx = Commons.Regions.GetAll().FirstOrDefault(r => r.Code.Equals(address.RegionCode));
      
      // Найти населенный пункт в RX.
      Commons.ICity cityRx = null;
      Commons.ICity localityRx = null;
      
      // Если регион известен - ищем населенный пункт в этом регионе.
      // Иначе ищем населенный пункт и пытаемся определить по нему регион.
      if (regionRx != null)
      {
        cityRx = Commons.Cities.GetAll().FirstOrDefault(c => (c.Name.Equals(city) || c.Name.Equals(cityRev)) && c.Region.Equals(regionRx));
        localityRx = Commons.Cities.GetAll().FirstOrDefault(c => (c.Name.Equals(locality) || c.Name.Equals(localityRev)) && c.Region.Equals(regionRx));
        
        counterparty.Region = regionRx;
        
        if (cityRx != null)
          counterparty.City = cityRx;
        if (localityRx != null)
          counterparty.City = localityRx;
      }
      else
      {
        cityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(city) || c.Name.Equals(cityRev));
        localityRx = Commons.Cities.GetAll().FirstOrDefault(c => c.Name.Equals(locality) || c.Name.Equals(localityRev));
        
        if (cityRx != null)
        {
          counterparty.City = cityRx;
          counterparty.Region = cityRx.Region;
        }
        if (localityRx != null)
        {
          counterparty.City = localityRx;
          counterparty.Region = localityRx.Region;
        }
      }
      
      counterparty.Save();
    }
    
    // TODO Dmitriev_IA: Возможно стоит удалить метод. Выше есть AddressInPostalFormat().
    private string GetAddress(NpoComputer.DCX.Common.OrganizationAddress address)
    {
      return string.Join(", ", new string[] { address.Street, address.House, address.Building, address.Apartment, address.City, address.PostalCode });
    }
    
    /// <summary>
    /// Создать уведомления о конфликтах синхронизации.
    /// </summary>
    /// <param name="conflicts">Список КА, которых не удалось однозначно определить в RX.</param>
    public virtual void CreateConflictTask(System.Collections.Generic.Dictionary<NpoComputer.DCX.Common.IContact, List<Parties.ICounterparty>> conflicts)
    {
      if (!conflicts.Any())
        return;
      
      var queueItems = CounterpartyQueueItems.GetAll(q => Equals(q.Box, _obj)).ToList();
      foreach (var conflict in conflicts)
      {
        var party = conflict.Key.Organization;
        var queueItem = queueItems.First(q => q.ExternalId == party.OrganizationId);
        
        // Если по данному контрагенту уже есть в работе задача на обработку конфликтов синхронизации - пропускаем.
        if (queueItem.MatchingTask != null && Equals(queueItem.MatchingTask.Status, Sungero.Workflow.Task.Status.InProcess))
          continue;
        
        var parties = conflict.Value;
        var task = Functions.CounterpartyConflictProcessingTask.Create(_obj, party, parties);
        queueItem.MatchingTask = task;
        queueItem.ProcessingStatus = ExchangeCore.CounterpartyQueueItem.ProcessingStatus.Error;
        queueItem.Note = ExchangeCore.BusinessUnitBoxes.Resources.NeedProcessSyncronizationConflict;
        queueItem.Save();
        
        task.Save();
        task.Start();
      }
    }
    
    /// <summary>
    /// Создать уведомление об изменении статуса обмена.
    /// </summary>
    /// <param name="queueItems">Список КА, для которых необходимо создать уведомления.</param>
    public virtual void CreateNotice(System.Collections.Generic.IList<ICounterpartyQueueItem> queueItems)
    {
      var task = Workflow.SimpleTasks.Create();
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(Calendar.Now);
      var subject = BusinessUnitBoxes.Resources.NoticeSubjectFormat(_obj.BusinessUnit.Name, _obj.ExchangeService.Name, dateWithUTC);
      task.Subject = Exchange.PublicFunctions.Module.CutText(subject, task.Info.Properties.Subject.Length);
      task.ThreadSubject = BusinessUnitBoxes.Resources.NoticeThreadSubject;
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Workflow.SimpleTask.AssignmentType.Notice;
      step.Performer = _obj.Responsible;

      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.Active)), BusinessUnitBoxes.Resources.NoticeActiveSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.ApprovingByUs)), BusinessUnitBoxes.Resources.NoticeApprovingByUsSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.Closed)), BusinessUnitBoxes.Resources.NoticeClosedSection);
      Functions.BusinessUnitBox.AddLines(_obj, task, queueItems.Where(x => Equals(x.SyncResult, ExchangeCore.CounterpartyQueueItem.SyncResult.ApprovingByCA)), BusinessUnitBoxes.Resources.NoticeApprovingByCASection);
      
      task.Save();
      task.Start();
      
      foreach (var queueItem in queueItems)
      {
        if (queueItem.MatchingTask != null && Equals(queueItem.MatchingTask.Status, Workflow.Task.Status.InProcess))
        {
          var incomingAssignment = CounterpartyConflictProcessingAssignments.GetAll().Where(x => Equals(x.Task, queueItem.MatchingTask) &&
                                                                                            Equals(x.Status, Workflow.AssignmentBase.Status.InProcess)).FirstOrDefault();
          
          if (incomingAssignment != null)
          {
            incomingAssignment.ActiveText = BusinessUnitBoxes.Resources.SyncronizationConflictProcessed;
            incomingAssignment.Save();
            incomingAssignment.Complete(ExchangeCore.CounterpartyConflictProcessingAssignment.Result.Complete);
          }
        }
        
        CounterpartyQueueItems.Delete(queueItem);
      }
    }
    
    /// <summary>
    /// Добавить текст уведомления.
    /// </summary>
    /// <param name="task">Уведомление.</param>
    /// <param name="queueItems">Элементы очереди для отправки уведомления.</param>
    /// <param name="header">Заголовок раздела.</param>
    public virtual void AddLines(Workflow.ISimpleTask task, System.Collections.Generic.IEnumerable<ICounterpartyQueueItem> queueItems, string header)
    {
      if (queueItems.Any())
      {
        task.ActiveText += header + Environment.NewLine;
        foreach (var queueItem in queueItems)
        {
          task.ActiveText += Constants.BusinessUnitBox.Delimiter;
          if (queueItem.Counterparty != null)
          {
            task.ActiveText += Hyperlinks.Get(queueItem.Counterparty);
            if (!task.Attachments.Contains(queueItem.Counterparty))
              task.Attachments.Add(queueItem.Counterparty);
          }
          
          var counterpartyBox = queueItem.Counterparty.ExchangeBoxes.Where(x => Equals(x.OrganizationId, queueItem.ExternalId)).Select(o => o.CounterpartyBox).FirstOrDefault();
          task.ActiveText += queueItem.Note + " " + counterpartyBox + Environment.NewLine;
        }
        task.ActiveText += Environment.NewLine;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить сервисы обмена, для которых у заданной НОР уже есть абонентские ящики.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true), Public]
    public List<IExchangeService> GetUsedServicesOfBox(IBusinessUnit businessUnit)
    {
      return BusinessUnitBoxes.GetAll()
        .Where(x => Equals(x.BusinessUnit, businessUnit) && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Select(b => b.ExchangeService).Distinct().ToList();
    }
    
    /// <summary>
    /// Получить все сервисы обмена, для которых есть абонентские ящики.
    /// </summary>
    /// <returns>Сервисы обмена.</returns>
    [Remote(IsPure = true), Public]
    public static List<IExchangeService> GetUsedServicesOfBox()
    {
      return BusinessUnitBoxes.GetAll()
        .Where(x => x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Select(b => b.ExchangeService).Distinct().ToList();
    }

    /// <summary>
    /// Получить сервис обмена по умолчанию.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    [Remote, Public]
    public IExchangeService GetDefaultExchangeService()
    {
      var alreadyUsedServices = this.GetUsedServicesOfBox(_obj.BusinessUnit);
      var exchangeServices = ExchangeServices.GetAll(s => s.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).Where(x => !alreadyUsedServices.Contains(x)).ToList();
      
      return exchangeServices.FirstOrDefault();
    }
    
    /// <summary>
    /// Получить сервис обмена ящика.
    /// </summary>
    /// <returns>Сервис обмена.</returns>
    public override IExchangeService GetExchangeService()
    {
      return _obj.ExchangeService;
    }
    
    /// <summary>
    /// Получить НОР ящика.
    /// </summary>
    /// <returns>Наша организация.</returns>
    [Public]
    public override Sungero.Company.IBusinessUnit GetBusinessUnit()
    {
      return _obj.BusinessUnit;
    }

    /// <summary>
    /// Найти сертификаты, зарегистрированные на пользователя в системе.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сертификаты пользователя.</returns>
    [Remote(IsPure = true), Public]
    public IQueryable<ICertificate> GetCertificatesOfEmployee(IEmployee employee)
    {
      return Certificates.GetAll().Where(x => Equals(x.Owner, employee) && x.Enabled == true);
    }
    
    /// <summary>
    /// Проверить, есть ли сертификаты ответственного в сервисе обмена.
    /// </summary>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true), Public]
    public bool CheckResponsibleServiceCertificates(IEmployee responsible)
    {
      return (_obj.HasExchangeServiceCertificates == false) || _obj.ExchangeServiceCertificates.Select(x => x.Certificate).Any(x => x.Enabled == true && Equals(x.Owner, responsible));
    }
    
    /// <summary>
    /// Проверить, есть ли сертификаты ответственного в сервисе обмена и RX.
    /// </summary>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true), Public]
    public bool CheckAllResponsibleCertificates(IEmployee responsible)
    {
      if (_obj.ConnectionStatus == Sungero.ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        return this.CheckResponsibleServiceCertificates(responsible);
      else
        return this.GetCertificatesOfEmployee(responsible).Any();
    }
    
    /// <summary>
    /// Проверить, совпадают ли ИНН/КПП нашей организации и ИНН/КПП в сервисе.
    /// </summary>
    /// <returns>Результат проверки.</returns>
    [Remote(IsPure = true)]
    public bool CheckBusinessUnitTinTRRC()
    {
      if (_obj.ConnectionStatus != Sungero.ExchangeCore.BusinessUnitBox.ConnectionStatus.Connected)
        return true;
      
      var client = this.GetClient();
      return Equals(client.OurSubscriber.Organization.Inn, _obj.BusinessUnit.TIN) && Equals(client.OurSubscriber.Organization.Kpp ?? string.Empty, _obj.BusinessUnit.TRRC ?? string.Empty);
    }
    
    /// <summary>
    /// Обновить список сертификатов сервиса обмена, имеющихся в системе RX, в свойство-коллекцию ящика.
    /// </summary>
    [Remote]
    public void UpdateExchangeServiceCertificates()
    {
      var needSave = !_obj.State.IsChanged;
      var client = this.GetClient();
      if (!Equals(_obj.HasExchangeServiceCertificates, client.CanGetOurSubscriberCertificates))
        _obj.HasExchangeServiceCertificates = client.CanGetOurSubscriberCertificates;
      
      if (_obj.HasExchangeServiceCertificates == true)
      {
        var allCertificates = Certificates.GetAll().ToList();
        
        var serviceCertificates = client.GetOrganizationCertificates(allCertificates.Select(x => x.X509Certificate).ToList()).ToList();
        var boxCertificates = _obj.ExchangeServiceCertificates.Select(x => x.Certificate).ToList();
        
        var exchangeCertificatesToDelete = boxCertificates
          .Where(x => !serviceCertificates.Any(c => c.Thumbprint.Equals(x.Thumbprint, StringComparison.InvariantCultureIgnoreCase)))
          .ToList();
        foreach (var certificateToDelete in exchangeCertificatesToDelete)
        {
          var certificateStringToDelete = _obj.ExchangeServiceCertificates
            .Where(x => Equals(x.Certificate, certificateToDelete)).FirstOrDefault();
          _obj.ExchangeServiceCertificates.Remove(certificateStringToDelete);
          
          boxCertificates.Remove(certificateToDelete);
        }
        
        var notInBoxCertificates = allCertificates
          .Where(x => !boxCertificates.Contains(x)).ToList();
        var exchangeCertificatesToAdd = notInBoxCertificates
          .Where(x => serviceCertificates.Any(c => c.Thumbprint.Equals(x.Thumbprint, StringComparison.InvariantCultureIgnoreCase)));
        foreach (var certificate in exchangeCertificatesToAdd)
        {
          _obj.ExchangeServiceCertificates.AddNew().Certificate = certificate;
        }
      }
      
      if (needSave && _obj.State.IsChanged)
        _obj.Save();
    }
    
    /// <summary>
    /// Проверить на уникальность логин и сервис обмена.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    [Remote(IsPure = true)]
    public List<string> CheckProperties()
    {
      return this.BeforeSaveCheckProperties().Select(p => p.Value).ToList();
    }
    
    /// <summary>
    /// Проверить на уникальность логин и сервис обмена.
    /// </summary>
    /// <returns>Список ошибок.</returns>
    public System.Collections.Generic.Dictionary<Sungero.Domain.Shared.IPropertyInfo, string> BeforeSaveCheckProperties()
    {
      var result = new Dictionary<Sungero.Domain.Shared.IPropertyInfo, string>();

      var loginIsAlreadyInUse = BusinessUnitBoxes.GetAll()
        .Any(x => !Equals(x, _obj) && x.Login == _obj.Login &&
             Equals(x.ExchangeService, _obj.ExchangeService));
      if (loginIsAlreadyInUse)
        result.Add(_obj.Info.Properties.Login, BusinessUnitBoxes.Resources.LoginIsAlreadyInUse);

      var duplicateBoxExists = BusinessUnitBoxes.GetAll()
        .Any(x => !Equals(x, _obj) && Equals(x.BusinessUnit, _obj.BusinessUnit) &&
             Equals(x.ExchangeService, _obj.ExchangeService));
      if (duplicateBoxExists)
        result.Add(_obj.Info.Properties.ExchangeService, BusinessUnitBoxes.Resources.DuplicateServiceExists);
      
      return result;
    }
    
    /// <summary>
    /// Зашифровать данные.
    /// </summary>
    /// <param name="data">Данные для шифрования.</param>
    /// <returns>Зашифрованные данные.</returns>
    [Remote(IsPure = true)]
    public static string GetEncryptedDataRemote(string data)
    {
      return Encryption.Encrypt(data);
    }
    
    /// <summary>
    /// Проверить сертификаты ящика.
    /// </summary>
    public virtual void ValidateCertificates()
    {
      var certificates = _obj.ExchangeServiceCertificates.Where(x => x.Certificate.NotAfter >= Calendar.Today).ToList();
      var hasErrors = false;
      foreach (var certificate in certificates)
      {
        var errors = certificate.Certificate.GetValidationErrors();
        foreach (var error in errors)
          Exchange.PublicFunctions.Module.LogErrorFormat(string.Format("Certificate with id {0} validation error {1}.", certificate.Certificate.Id, error));
        if (errors.Any())
          hasErrors = true;
        
        var expiryDate = Calendar.Today.AddDays(Constants.Module.ExpirePeriod);
        if (expiryDate >= certificate.Certificate.NotAfter)
          Exchange.PublicFunctions.Module.LogDebugFormat(string.Format("Certificate with id {0} is expiring {1}.", certificate.Certificate.Id, certificate.Certificate.NotAfter));
      }
      
      if (hasErrors)
        throw AppliedCodeException.Create(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.CertificateValidationError);
    }
    
    /// <summary>
    /// Получение пакета документов пришедшего из системы обмена.
    /// </summary>
    /// <param name="messageId">ИД сообщения.</param>
    /// <param name="documentId">ИД документа в СО.</param>
    /// <returns>Пакет совместно обрабатываемых документов и сообщений из системы обмена.</returns>
    [Remote]
    public static Structures.BusinessUnitBox.ExchangeDocumentsPackage GetExchangeDocumentsPackage(string messageId, string documentId)
    {
      var packageInfo = Structures.BusinessUnitBox.ExchangeDocumentsPackage.Create();
      packageInfo.Messages = new List<IMessageQueueItem>() { };
      packageInfo.Documents = new List<Sungero.ExchangeCore.Structures.BusinessUnitBox.ExchangeProcessedDocument>() { };
      var infos = Exchange.ExchangeDocumentInfos.GetAll(i => i.ServiceMessageId.Contains(messageId) && i.ServiceDocumentId.Contains(documentId));
      foreach (var info in infos)
      {
        var processedDocument = Structures.BusinessUnitBox.ExchangeProcessedDocument.Create();
        processedDocument.DocumentInfo = info;
        
        if (info != null && info.Document != null)
        {
          AccessRights.AllowRead(
            () =>
            {
              processedDocument.Document = info.Document;
              processedDocument.DocumentId = info.Document.Id;
            });
          processedDocument.HasDocumentReadPermissions = info.Document.AccessRights.CanRead();
        }
        packageInfo.Documents.Add(processedDocument);
      }
      
      var messageQueueItems = ExchangeCore.MessageQueueItems.GetAll(
        m => m.ExternalId.Contains(messageId) &&
        m.Documents.Any(d => d.ExternalId.Contains(documentId) && d.Type == Sungero.ExchangeCore.MessageQueueItemDocuments.Type.Primary));
      packageInfo.Messages.AddRange(messageQueueItems);

      return packageInfo;
    }
  }
}
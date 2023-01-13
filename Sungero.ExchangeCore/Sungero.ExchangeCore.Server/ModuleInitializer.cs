using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.ExchangeCore.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Справочники.
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      GrantRightsOnDatabooks();
      
      // Создание типов прав модуля.
      InitializationLogger.Debug("Init: Create access rights.");
      CreateCounterpartyAccessRights();
      
      CreateExchangeServices();
      InitializeExchangeServiceUsersRole();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    public static void GrantRightsOnDatabooks()
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      var allUsers = Roles.AllUsers;
      
      BoxBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ExchangeServices.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);

      BoxBases.AccessRights.Save();
      ExchangeServices.AccessRights.Save();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");

      Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.ExchangeServiceUsersRoleName,
                                                Resources.ExchangeServiceUsersRoleDescription,
                                                Constants.Module.RoleGuid.ExchangeServiceUsersRoleGuid);
    }
    
    /// <summary>
    /// Создать типы прав для контрагентов.
    /// </summary>
    public static void CreateCounterpartyAccessRights()
    {
      InitializationLogger.Debug("Init: Create access rights for counterparty type Counterparty");
      CreateAccessRightsForCounterpartyType(Guid.Parse("294767f1-009f-4fbd-80fc-f98c49ddc560"));
    }
    
    /// <summary>
    /// Создать права для типа контрагента.
    /// </summary>
    /// <param name="entityTypeGuid">Guid типа контрагента.</param>
    public static void CreateAccessRightsForCounterpartyType(Guid entityTypeGuid)
    {
      // Создать тип прав "Отправка через сервис обмена".
      var mask = Parties.CounterpartyOperations.Read ^ Parties.CounterpartyOperations.Update ^ Parties.CounterpartyOperations.SetExchange;
      Docflow.PublicInitializationFunctions.Module.CreateAccessRightsType(entityTypeGuid.ToString(), Docflow.Resources.SetExchangeRightTypeName.ToString(), mask,
                                                            mask, CoreEntities.AccessRightsType.AccessRightsTypeArea.Type,
                                                            Constants.Module.DefaultAccessRightsTypeSid.SetExchange, false, string.Empty);
      
      // Переопределяем тип прав "Изменение".
      mask = Parties.CounterpartyOperations.Read ^ Parties.CounterpartyOperations.Create ^ Parties.CounterpartyOperations.Update ^
        Parties.CounterpartyOperations.DelegateAccess ^ Parties.CounterpartyOperations.ManageRelations ^ Parties.CounterpartyOperations.ChangeCard;
      Docflow.PublicInitializationFunctions.Module.CreateAccessRightsType(entityTypeGuid.ToString(), Docflow.Resources.UpdateRightTypeName.ToString(), mask,
                                                            mask, CoreEntities.AccessRightsType.AccessRightsTypeArea.Both,
                                                            Constants.Module.DefaultAccessRightsTypeSid.Update, true, string.Empty);
    }
    
    /// <summary>
    /// Инициализация роли "Пользователи с правами на работу через сервис обмена".
    /// </summary>
    public static void InitializeExchangeServiceUsersRole()
    {
      InitializationLogger.Debug("Init: Initialize Exchange Service Users role");
      
      var exchangeServiceUsersRole = ExchangeCore.PublicFunctions.Module.GetExchangeServiceUsersRole();
      if (exchangeServiceUsersRole == null)
      {
        InitializationLogger.Debug("Init: No service users role found");
        return;
      }
      
      Parties.Counterparties.AccessRights.Grant(exchangeServiceUsersRole, Constants.Module.DefaultAccessRightsTypeSid.SetExchange);
      Parties.Counterparties.AccessRights.Save();
    }
    
    public static void CreateExchangeServices()
    { 
      CreateExchangeService("https://diadoc-api.kontur.ru/", ExchangeCore.ExchangeService.ExchangeProvider.Diadoc,
                            "https://diadoc.kontur.ru/");
      
      CreateExchangeService("https://online.sbis.ru/", ExchangeCore.ExchangeService.ExchangeProvider.Sbis,
                            "https://online.sbis.ru/");
    }
    
    /// <summary>
    /// Создать сервис обмена.
    /// </summary>
    /// <param name="uri">Ссылка на сервис.</param>
    /// <param name="exchangeProvider">Оператор обмена. ExchangeCore.ExchangeService.ExchangeProvider.</param>
    /// <param name="logonUrl">URL личного кабинета.</param>
    public static void CreateExchangeService(string uri, Enumeration exchangeProvider, string logonUrl)
    {
      if (ExchangeServices.GetAll(s => Equals(s.ExchangeProvider, exchangeProvider)).Any())
        return;
      
      var system = ExchangeServices.Create();
      system.Name = ExchangeServices.Info.Properties.ExchangeProvider.GetLocalizedValue(exchangeProvider);
      system.Uri = uri;
      system.ExchangeProvider = exchangeProvider;
      system.LogonUrl = logonUrl;
      system.Save();
    }
  }
}

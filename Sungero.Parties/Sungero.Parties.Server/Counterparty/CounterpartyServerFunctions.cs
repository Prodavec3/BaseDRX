using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties.Server
{
  partial class CounterpartyFunctions
  {
    /// <summary>
    /// Сформировать информацию о возможности отправки приглашений контрагенту в сервис обмена.
    /// </summary>
    /// <returns>Информация о возможности отправки приглашений контрагенту в сервис обмена.</returns>
    [Remote(IsPure = true)]
    public Structures.Counterparty.SendInvitation InvitationBoxes()
    {
      var result = Structures.Counterparty.SendInvitation.Create();
      var usedBoxes = _obj.ExchangeBoxes.Select(b => b.Box.Id).ToList();
      var allBoxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      var doubleBoxes = new List<ExchangeCore.IBusinessUnitBox>();
      
      // Ящики, через которые доступна отправка приглашений.
      var allowedBoxes = Parties.Functions.Counterparty.CanSendInvitation(_obj);
      foreach (var box in allowedBoxes)
      {
        var counterparty = Parties.Counterparties.GetAll()
          .Where(x => !Equals(x, _obj))
          .Where(x => x.ExchangeBoxes.Any(e => Equals(e.Box, box.Box) && box.OrganizationIds.Contains(e.OrganizationId)))
          .FirstOrDefault();
        if (counterparty != null)
        {
          doubleBoxes.Add(box.Box);
          result.HaveDoubleCounterparty = true;
        }
      }
      
      // Если найдены дублирующие ящики - убрать их из доступных.
      if (doubleBoxes.Any())
        allowedBoxes = allowedBoxes.Where(x => !doubleBoxes.Contains(x.Box)).ToList();
      
      // Ящики для отправки приглашения.
      result.Boxes = allBoxes.Where(b => !usedBoxes.Contains(b.Id)).Where(b => allowedBoxes.Any(x => Equals(x.Box, b))).ToList();
      result.DefaultBox = result.Boxes.OrderBy(x => x.Name).FirstOrDefault();
      result.HaveAllowedBoxes = result.Boxes.Any();
      result.HaveAnyBoxes = allBoxes.Any();
      
      // Возможен ли обмен с контрагентом через доступные сервисы.
      result.CanSendInivtationFromAnyService = allowedBoxes.Any();
      
      // Сервисы, через которые возможна отправка приглашений, с учетом доступных ящиков.
      var exchangeServices = result.Boxes.Select(x => x.ExchangeService).Distinct().ToList();
      result.Services = exchangeServices.OrderBy(x => x.Name).ToList();

      // Действия зависят друг от друга - общий признак, можно ли в итоге выполнять действие.
      result.CanDoAction = result.HaveAnyBoxes && result.HaveAllowedBoxes && result.CanSendInivtationFromAnyService;
      return result;
    }
    
    /// <summary>
    /// Проверка возможности отправить приглашение контрагенту хоть через один ящик.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Список сервисов, способных отправить приглашение.</returns>
    /// <remarks>Только полное совпадение по ИНН и КПП.</remarks>
    public static List<Structures.Counterparty.AllowedBoxes> CanSendInvitation(Parties.ICounterparty counterparty)
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      
      // Если контрагент является копией какой-либо НОР, то ящики этой НОР убираем из списка.
      var parentBusinessUnit = Sungero.Company.BusinessUnits.GetAll(b => Equals(b.Company, counterparty)).FirstOrDefault();
      if (parentBusinessUnit != null)
        boxes = boxes.Where(b => !Equals(b.BusinessUnit, parentBusinessUnit)).ToList();
      
      var allowedServices = new List<Structures.Counterparty.AllowedBoxes>();
      foreach (var box in boxes)
      {
        var organizations = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.CanSendInvitationFrom(box, counterparty);
        
        if (organizations.Any())
          allowedServices.Add(Structures.Counterparty.AllowedBoxes.Create(box, organizations));
      }
      return allowedServices;
    }
    
    /// <summary>
    /// Получить контрагентов, с которыми установлен обмен для данной НОР.
    /// </summary>
    /// <param name="businessUnit">НОР.</param>
    /// <returns>Список контрагентов.</returns>
    [Remote(IsPure = true), Public]
    public static List<ICounterparty> GetExchangeCounterparty(Sungero.Company.IBusinessUnit businessUnit)
    {
      // TODO Пока отправляем только тем, с кем установлен обмен.
      var parties = Counterparties.GetAll()
        .Where(x => x.ExchangeBoxes.Any(b => Equals(b.Status, Sungero.Parties.CounterpartyExchangeBoxes.Status.Active) && b.IsDefault == true))
        .ToList();
      
      if (businessUnit != null)
        parties = parties.Where(x => x.ExchangeBoxes.Any(b => Equals(b.Box.BusinessUnit, businessUnit))).ToList();
      
      return parties;
    }
    
    /// <summary>
    /// Получить системного контрагента для листов рассылки в обход фильтрации.
    /// </summary>
    /// <returns>Системный контрагент - "По списку рассылки".</returns>
    [Remote(IsPure = true), Public]
    public static ICounterparty GetDistributionListCounterparty()
    {
      var guid = Sungero.Parties.Constants.Counterparty.DistributionListCounterpartyGuid;
      var link = Docflow.PublicFunctions.Module.GetExternalLink(Company.ClassTypeGuid, guid);
      
      if (link != null && link.EntityId.HasValue)
      {
        var companyId = link.EntityId.Value;
        // HACK mukhachev: использование внутренней сессии для обхода фильтрации.
        Logger.DebugFormat("CreateDefaultDistributionListCounterpartyIgnoreFiltering: companyId {0}", companyId);
        using (var session = new Sungero.Domain.Session())
        {
          var innerSession = (Sungero.Domain.ISession)session.GetType()
            .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
          
          return Companies.As((Sungero.Domain.Shared.IEntity)innerSession.Get(typeof(ICompany), companyId));
        }
      }
      
      return null;
    }

  }
}
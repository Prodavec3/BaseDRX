using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Server
{
  partial class InvitedCounterpartiesFolderHandlers
  {

    public virtual IQueryable<Sungero.Parties.ICounterparty> InvitedCounterpartiesDataQuery(IQueryable<Sungero.Parties.ICounterparty> query)
    {
      // Показать контрагентов, которым отправили приглашения, или которые отправили приглашения нам.
      query = query.Where(x => x.ExchangeBoxes.Any(b => b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA ||
                                                   b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs));
      
      if (_filter == null)
        return query;
      
      // НОР.
      if (_filter.BusinessUnit != null)
        query = query.Where(x => x.ExchangeBoxes.Any(b => Equals(b.Box.BusinessUnit, _filter.BusinessUnit) &&
                                                     (b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA ||
                                                      b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)));
      
      // Сервис обмена.
      if (_filter.ExchangeService != null)
        query = query.Where(x => x.ExchangeBoxes.Any(b => Equals(b.Box.ExchangeService, _filter.ExchangeService) &&
                                                     (b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA ||
                                                      b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)));
      
      return query;
    }
  }

  partial class BlockedCounterpartiesFolderHandlers
  {
    public virtual IQueryable<Sungero.Parties.ICounterparty> BlockedCounterpartiesDataQuery(IQueryable<Sungero.Parties.ICounterparty> query)
    {
      // Показать контрагентов с хотя бы одним заблокированным ящиком.
      query = query.Where(x => x.ExchangeBoxes.Any(b => b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Closed));
      
      if (_filter == null)
        return query;
      
      // НОР.
      if (_filter.BusinessUnit != null)
        query = query.Where(x => x.ExchangeBoxes.Any(b => Equals(b.Box.BusinessUnit, _filter.BusinessUnit) &&
                                                     b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Closed));
      
      // Сервис обмена.
      if (_filter.ExchangeService != null)
        query = query.Where(x => x.ExchangeBoxes.Any(b => Equals(b.Box.ExchangeService, _filter.ExchangeService) &&
                                                     b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Closed));
      
      return query;
    }
  }
}
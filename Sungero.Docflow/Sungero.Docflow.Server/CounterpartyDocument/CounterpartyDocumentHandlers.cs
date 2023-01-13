using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CounterpartyDocument;

namespace Sungero.Docflow
{
  partial class CounterpartyDocumentConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var counterpartyDocument = CounterpartyDocuments.As(e.Entity);
        counterpartyDocument.Counterparty = counterparty;
      }      
    }
  }

  partial class CounterpartyDocumentFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      // Фильтр по виду документа.
      if (_filter.Kind != null)
        query = query.Where(d => Equals(d.DocumentKind, _filter.Kind));
      
      // Фильтр по статусу.
      if (_filter.Active || _filter.Obsolete || _filter.InWork)
        query = query.Where(l => _filter.Active && l.LifeCycleState == OfficialDocument.LifeCycleState.Active ||
                            _filter.InWork && l.LifeCycleState == OfficialDocument.LifeCycleState.Draft ||
                            _filter.Obsolete && l.LifeCycleState == OfficialDocument.LifeCycleState.Obsolete);
      
      // Фильтр по интервалу времени.
      var periodBegin = Calendar.UserToday.AddDays(-7);
      var periodEnd = Calendar.UserToday.EndOfDay();

      if (_filter.CurrentYear)
        periodBegin = Calendar.UserToday.BeginningOfYear();
      
      if (_filter.LastYear)
      {
        var lastYear = Calendar.UserToday.AddYears(-1);
        periodBegin = lastYear.BeginningOfYear();
        periodEnd = lastYear.EndOfYear();
      }
      
      if (_filter.Manual)
      {
        periodBegin = _filter.RangeOfDateFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.RangeOfDateTo ?? Calendar.SqlMaxValue;
      }
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, periodBegin) ? periodBegin : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd : periodEnd.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == periodBegin) && j.DocumentDate != clientPeriodEnd);
      
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> KindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query,
                                                                           Sungero.Domain.FilteringEventArgs e)
    {
      var kinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(T));
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Inner && kinds.Contains(d));
    }
  }

  partial class CounterpartyDocumentServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Заполнить статус жизненного цикла.
      if (_obj.LifeCycleState != OfficialDocument.LifeCycleState.Obsolete)
        _obj.LifeCycleState = OfficialDocument.LifeCycleState.Active;
      
      if (CallContext.CalledFrom(Parties.Counterparties.Info))
      {
        _obj.Counterparty = Parties.Counterparties.GetAll()
          .Where(c => c.Id == CallContext.GetCallerEntityId(Parties.Counterparties.Info))
          .FirstOrDefault();
      }
    }
  }

}
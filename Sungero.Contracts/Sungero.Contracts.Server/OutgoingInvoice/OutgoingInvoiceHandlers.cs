using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.OutgoingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{

  partial class OutgoingInvoiceFilteringServerHandler<T>
  {
    /// <summary>
    /// Фильтрация списка исходящих счетов.
    /// </summary>
    /// <param name="query">Фильтруемый список счетов.</param>
    /// <param name="e">Аргументы события фильтрации.</param>
    /// <returns>Список счетов с примененными фильтрами.</returns>
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      // Состояние.
      if ((_filter.DraftState || _filter.ActiveState || _filter.PaidState || _filter.ObsoleteState) &&
          !(_filter.DraftState && _filter.ActiveState && _filter.PaidState && _filter.ObsoleteState))
      {
        query = query.Where(x => _filter.DraftState && x.LifeCycleState == OutgoingInvoice.LifeCycleState.Draft ||
                            _filter.ActiveState && x.LifeCycleState == OutgoingInvoice.LifeCycleState.Active ||
                            _filter.PaidState && x.LifeCycleState == OutgoingInvoice.LifeCycleState.Paid ||
                            _filter.ObsoleteState && x.LifeCycleState == OutgoingInvoice.LifeCycleState.Obsolete);
      }
      
      // Контрагент.
      if (_filter.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _filter.Counterparty));
      
      // НОР.
      if (_filter.BusinessUnit != null)
        query = query.Where(x => Equals(x.BusinessUnit, _filter.BusinessUnit));
      
      // Подразделение.
      if (_filter.Department != null)
        query = query.Where(x => Equals(x.Department, _filter.Department));
      
      // Дата.
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (_filter.Last7daysInvoice)
        beginDate = Calendar.UserToday.AddDays(-7);
      
      if (_filter.ManualPeriodInvoice)
      {
        beginDate = _filter.DateRangeInvoiceFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeInvoiceTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, beginDate) ? beginDate : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, endDate) ? endDate : endDate.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, endDate) ? endDate.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == beginDate) && j.DocumentDate != clientPeriodEnd);
      
      return query;
    }
  }

}
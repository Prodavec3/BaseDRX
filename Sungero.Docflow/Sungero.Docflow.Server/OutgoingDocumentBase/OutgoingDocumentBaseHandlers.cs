using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow
{
  partial class OutgoingDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
      
      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var outgoingDocument = OutgoingDocumentBases.As(e.Entity);
        outgoingDocument.IsManyAddressees = false;
        outgoingDocument.Correspondent = counterparty;
      }
    }
  }

  partial class OutgoingDocumentBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.InResponseTo == null || !_source.InResponseTo.AccessRights.CanRead())
        e.Without(_info.Properties.InResponseTo);
    }
  }

  partial class OutgoingDocumentBaseAddresseesAddresseePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseesAddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
      {
        if (Sungero.Parties.People.Is(_obj.Correspondent))
          return query.Where(c => c.Company == null);

        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      }
      return query;
    }
  }

  partial class OutgoingDocumentBaseFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(k => k.Status == CoreEntities.DatabookEntry.Status.Active &&
                         k.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Outgoing &&
                         k.DocumentType.IsRegistrationAllowed == true);
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> DocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query, Sungero.Domain.FilteringEventArgs e)
    {
      return Functions.DocumentRegister.GetAvailableDocumentRegisters(DocumentRegister.DocumentFlow.Outgoing);
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      if (_filter == null)
        return query;
      
      // Фильтр по журналу регистрации.
      if (_filter.DocumentRegister != null)
        query = query.Where(d => d.DocumentRegister == _filter.DocumentRegister);
      
      // Фильтр по виду документа.
      if (_filter.DocumentKind != null)
        query = query.Where(d => d.DocumentKind == _filter.DocumentKind);
      
      // Фильтр по статусу. Если все галочки включены, то нет смысла добавлять фильтр.
      if ((_filter.Registered || _filter.Reserved || _filter.NotRegistered) &&
          !(_filter.Registered && _filter.Reserved && _filter.NotRegistered))
        query = query.Where(l => _filter.Registered && l.RegistrationState == OfficialDocument.RegistrationState.Registered ||
                            _filter.Reserved && l.RegistrationState == OfficialDocument.RegistrationState.Reserved ||
                            _filter.NotRegistered && l.RegistrationState == OfficialDocument.RegistrationState.NotRegistered);
      
      // Фильтр по контрагенту.
      if (_filter.Counterparty != null)
        query = query.Where(d => d.Addressees.Select(x => x.Correspondent).Any(y => Equals(y, _filter.Counterparty)));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      // Фильтр по интервалу времени
      var periodBegin = Calendar.UserToday.AddDays(-7);
      var periodEnd = Calendar.UserToday.EndOfDay();
      
      if (_filter.LastWeek)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (_filter.LastMonth)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last90Days)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (_filter.ManualPeriod)
      {
        periodBegin = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, periodBegin) ? periodBegin : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd : periodEnd.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == periodBegin) && j.DocumentDate != clientPeriodEnd);

      return query;
    }
  }

  partial class OutgoingDocumentBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.IsManyAddressees == true && !_obj.Addressees.Any())
        e.AddError(_obj.Info.Properties.Addressees, OutgoingDocumentBases.Resources.NeedFillAddressee);
      
      if (_obj.InResponseTo != null && _obj.InResponseTo.AccessRights.CanRead() && !_obj.Relations.GetRelatedFrom(Constants.Module.ResponseRelationName).Contains(_obj.InResponseTo))
        _obj.Relations.AddFromOrUpdate(Constants.Module.ResponseRelationName, _obj.State.Properties.InResponseTo.OriginalValue, _obj.InResponseTo);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Заполнить исполнителя.
      if (_obj.Assignee == null)
        _obj.Assignee = Company.Employees.As(_obj.Author);
      
      if (_obj.IsManyAddressees == null)
        _obj.IsManyAddressees = false;
    }
  }

  partial class OutgoingDocumentBaseInResponseToPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> InResponseToFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Addressees.Any(a => a.Correspondent != null))
      {
        var correspondents = _obj.Addressees.Where(a => a.Correspondent != null).Select(a => a.Correspondent).ToList();
        query = query.Where(l => correspondents.Contains(l.Correspondent));
      }
      
      if (_obj.BusinessUnit != null)
        query = query.Where(l => Equals(_obj.BusinessUnit, l.BusinessUnit));
      
      return query;
    }
  }

  partial class OutgoingDocumentBaseAddresseePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Correspondent != null)
        query = query.Where(c => Equals(c.Company, _obj.Correspondent));
      return query;
    }
  }

}
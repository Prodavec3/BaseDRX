using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.InternalDocumentBase;

namespace Sungero.Docflow
{
  partial class InternalDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      e.Without(Sungero.Docflow.Addendums.Info.Properties.LeadingDocument);
      
      // При смене типа с вх. документа эл. обмена, а также с финансовых и договорных документов
      // дополнить примечание информацией об основании подписания со стороны контрагента.
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      if (sourceOfficialDocument != null)
      {
        var note = sourceOfficialDocument.Note;
        
        // Получить основание подписания со стороны контрагента.
        var counterpartySigningReason = Docflow.PublicFunctions.OfficialDocument.GetCounterpartySigningReason(sourceOfficialDocument);
        if (!string.IsNullOrWhiteSpace(counterpartySigningReason))
        {
          if (!string.IsNullOrWhiteSpace(note))
            note += Environment.NewLine;
          
          note += SimpleDocuments.Resources.CounterpartySigningReasonTitleFormat(counterpartySigningReason);
        }
        
        e.Map(_info.Properties.Note, note);
      }
    }
  }

  partial class InternalDocumentBaseFilteringServerHandler<T>
  {
    
    /// <summary>
    /// Признак папки "Внутренние документы".
    /// </summary>
    /// <returns>True, для внутренних документов, False для наследников.</returns>
    private bool IsInternalDocumentBase()
    {
      // HACK Zamerov: 35180, чтобы в наследниках не фильтровало.
      return Equals(typeof(T), typeof(IInternalDocumentBase));
    }

    public virtual IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      // При вызове во внутренних документах, не выводим Не нумеруемые виды документов.
      if (this.IsInternalDocumentBase())
      {
        query = query.Where(d => d.NumberingType != DocumentKind.NumberingType.NotNumerable && d.DocumentType.IsRegistrationAllowed == true);
        
        // TODO Zamerov: явно убираем приложения.
        var guid = Server.Addendum.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != guid);
        
        // Убираем документы контрагента.
        var counterpartyDocumentTypeGuid = Sungero.Docflow.Server.CounterpartyDocument.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentType.DocumentTypeGuid != counterpartyDocumentTypeGuid);
      }
      
      var kinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(T));
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentType.DocumentFlow == DocumentType.DocumentFlow.Inner && kinds.Contains(d));
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentRegister> DocumentRegisterFiltering(IQueryable<Sungero.Docflow.IDocumentRegister> query, Sungero.Domain.FilteringEventArgs e)
    {
      return Functions.DocumentRegister.GetAvailableDocumentRegisters(DocumentRegister.DocumentFlow.Inner);
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      if (_filter == null)
        return query;
      
      // При вызове во внутренних документах с панелью фильтрации, не выводим Не нумеруемые документы.
      if (this.IsInternalDocumentBase())
      {
        query = query.Where(d => d.DocumentKind.NumberingType != DocumentKind.NumberingType.NotNumerable);
        
        // TODO Zamerov: явно убираем приложения.
        var guid = Server.Addendum.ClassTypeGuid.ToString();
        query = query.Where(d => d.DocumentKind.DocumentType.DocumentTypeGuid != guid);
      }
      
      // При вызове во внутренних документах не выводим документы по контрагенту.
      if (this.IsInternalDocumentBase())
      {
        query = query.Where(d => !CounterpartyDocuments.Is(d));
      }
      
      // Фильтр по журналу регистрации.
      if (_filter.DocumentRegister != null)
        query = query.Where(d => Equals(d.DocumentRegister, _filter.DocumentRegister));
      
      // Фильтр по виду документа.
      if (_filter.DocumentKind != null)
        query = query.Where(d => Equals(d.DocumentKind, _filter.DocumentKind));
      
      // Фильтр по статусу. Если все галочки включены, то нет смысла добавлять фильтр.
      if ((_filter.Registered || _filter.Reserved || _filter.NotRegistered) &&
          !(_filter.Registered && _filter.Reserved && _filter.NotRegistered))
        query = query.Where(l => _filter.Registered && l.RegistrationState == OfficialDocument.RegistrationState.Registered ||
                            _filter.Reserved && l.RegistrationState == OfficialDocument.RegistrationState.Reserved ||
                            _filter.NotRegistered && l.RegistrationState == OfficialDocument.RegistrationState.NotRegistered);
      
      // Фильтр по нашей организации.
      if (_filter.BusinessUnit != null)
        query = query.Where(d => Equals(d.BusinessUnit, _filter.BusinessUnit));
      
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

  partial class InternalDocumentBaseDocumentKindPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DocumentKindFiltering(query, e);
      
      // Отфильтровать внутренние виды документов.
      return query.Where(k => k.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Inner);
    }
  }

}
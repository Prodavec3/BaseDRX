using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchiveUI.Server
{
  partial class PowerOfAttorneyListFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IPowerOfAttorneyBase> PowerOfAttorneyListDataQuery(IQueryable<Sungero.Docflow.IPowerOfAttorneyBase> query)
    {
      #region Фильтры
      
      if (_filter == null)
        return query;
      
      // Состояние.
      if ((_filter.DraftState || _filter.ActiveState || _filter.ObsoleteState) &&
          !(_filter.DraftState && _filter.ActiveState && _filter.ObsoleteState))
      {
        query = query.Where(p => _filter.DraftState && p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Draft ||
                            _filter.ActiveState && p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Active ||
                            _filter.ObsoleteState && p.LifeCycleState == Sungero.Docflow.PowerOfAttorneyBase.LifeCycleState.Obsolete);
      }
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(p => Equals(p.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(p => Equals(p.Department, _filter.Department));
      
      // Фильтр "Кому выдана".
      if (_filter.Performer != null)
        query = query.Where(p => Equals(p.IssuedTo, _filter.Performer));
      
      // Период.
      if (_filter.Today)
        query = query.Where(p => (!p.RegistrationDate.HasValue || p.RegistrationDate <= Calendar.UserToday) &&
                            (!p.ValidTill.HasValue || p.ValidTill >= Calendar.UserToday));
      
      if (_filter.ManualPeriod)
      {
        if (_filter.DateRangeFrom.HasValue)
          query = query.Where(p => !p.ValidTill.HasValue || p.ValidTill >= _filter.DateRangeFrom);
        if (_filter.DateRangeTo.HasValue)
          query = query.Where(p => !p.RegistrationDate.HasValue || p.RegistrationDate <= _filter.DateRangeTo);
      }
      
      #endregion
      
      return query;
    }
  }

  partial class SignAwaitedDocumentsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IAccountingDocumentBase> SignAwaitedDocumentsDataQuery(IQueryable<Sungero.Docflow.IAccountingDocumentBase> query)
    {
      query = query.Where(x => x.IsFormalized == true && (x.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignAwaited ||
                                                          x.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignRequired));
      #region Фильтры

      // Фильтр "Контрагент".
      if (_filter.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _filter.Counterparty));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      // Фильтр "Ожидают подписания".
      if (_filter.ByBusinessUnit && !_filter.ByCounterparty)
        query = query.Where(c => Equals(c.ExchangeState, Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignRequired));
      
      if (_filter.ByCounterparty && !_filter.ByBusinessUnit)
        query = query.Where(c => Equals(c.ExchangeState, Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignAwaited));
      
      #region Фильтрация по дате

      var beginDate = Calendar.UserToday.BeginningOfMonth();
      var endDate = Calendar.UserToday.EndOfMonth();

      if (_filter.PreviousMonth)
      {
        beginDate = Calendar.UserToday.AddMonths(-1).BeginningOfMonth();
        endDate = Calendar.UserToday.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, beginDate) ? beginDate : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, endDate) ? endDate : endDate.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, endDate) ? endDate.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == beginDate) && j.DocumentDate != clientPeriodEnd);
      
      #endregion
      
      #endregion
      
      return query;
    }
  }

  partial class DocumentsWithoutScanFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IAccountingDocumentBase> DocumentsWithoutScanDataQuery(IQueryable<Sungero.Docflow.IAccountingDocumentBase> query)
    {
      // Получить все финансовые документы без скан-копий.
      var scanExtensions = new List<string>() { "pdf", "jpg", "tiff", "png", "tif", "bmp", "jpeg" };

      query = query.Where(x => !Contracts.IncomingInvoices.Is(x) && !Contracts.OutgoingInvoices.Is(x))
        .Where(x => x.IsFormalized != true)
        .Where(x => x.LifeCycleState != Sungero.Docflow.AccountingDocumentBase.LifeCycleState.Obsolete);
      
      var infos = Exchange.ExchangeDocumentInfos.GetAll().Where(x => query.Contains(x.Document)).Select(d => d.Document).ToList();
      query = query.Where(x => !infos.Contains(x));
      
      var associatedApps = Sungero.Content.AssociatedApplications.GetAll().Where(x => scanExtensions.Contains(x.Extension.ToLower())).ToList();
      query = query.Where(x => !x.Versions.Any() || !associatedApps.Contains(x.AssociatedApplication));
      
      #region Фильтры

      // Фильтр "Контрагент".
      if (_filter.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _filter.Counterparty));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      #region Фильтрация по дате

      var beginDate = Calendar.UserToday.BeginningOfMonth();
      var endDate = Calendar.UserToday.EndOfMonth();
      if (_filter.PreviousMonth)
      {
        beginDate = Calendar.UserToday.AddMonths(-1).BeginningOfMonth();
        endDate = Calendar.UserToday.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, beginDate) ? beginDate : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, endDate) ? endDate : endDate.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, endDate) ? endDate.AddDays(1) : Calendar.SqlMaxValue;
      query = query.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                j.DocumentDate == beginDate) && j.DocumentDate != clientPeriodEnd);
      
      #endregion
      
      #endregion

      return query;
    }
  }

  partial class FinContractListFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> FinContractListDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return ContractsUI.PublicFunctions.Module.ContractsFilterContractsKind(query, true);
    }

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> FinContractListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var documents = query.Where(x => x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Terminated ||
                                  x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Active ||
                                  x.LifeCycleState == Contracts.ContractBase.LifeCycleState.Closed);
      
      if (_filter == null)
        return documents;
      
      #region Фильтры
      
      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
        documents = documents.Where(c => Equals(c.DocumentKind, _filter.DocumentKind));
      
      // Фильтр "Категория".
      if (_filter.Category != null)
        documents = documents.Where(c => Equals(c.DocumentGroup, _filter.Category));
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
        documents = documents.Where(c => Equals(c.Counterparty, _filter.Contractor));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        documents = documents.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        documents = documents.Where(c => Equals(c.Department, _filter.Department));
      
      #region Фильтрация по дате договора

      DateTime? beginDate = null;
      DateTime? endDate = null;
      var today = Calendar.UserToday;
      
      if (_filter.CurrentMonth)
      {
        beginDate = today.BeginningOfMonth();
        endDate = today.EndOfMonth();
      }
      if (_filter.PreviousMonth)
      {
        beginDate = today.AddMonths(-1).BeginningOfMonth();
        endDate = today.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today);
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(today.AddMonths(-3));
        endDate = Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(today.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        if (_filter.DateRangeFrom.HasValue)
          beginDate = _filter.DateRangeFrom.Value;
        if (_filter.DateRangeTo.HasValue)
          endDate = _filter.DateRangeTo.Value;
      }

      if (beginDate != null)
        documents = documents.Where(q => q.ValidTill != null && q.ValidTill >= beginDate ||
                                    q.ValidTill == null);
      
      if (endDate != null)
        documents = documents.Where(q => q.ValidFrom != null && q.ValidFrom <= endDate ||
                                    q.ValidFrom == null);
      #endregion
      
      // Фильтр по типу документа
      if (_filter.Contracts && !_filter.SupAgreements)
        return documents.Where(d => ContractBases.Is(d));
      
      if (_filter.SupAgreements && !_filter.Contracts)
        return documents.Where(d => SupAgreements.Is(d));
      
      #endregion
      
      return documents;
    }
  }

  partial class FinancialArchiveUIHandlers
  {
  }
}
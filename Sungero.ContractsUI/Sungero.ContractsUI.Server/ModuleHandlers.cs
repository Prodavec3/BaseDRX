using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Server
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

  partial class ContractsListFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      if (_filter == null)
        return query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      #region Фильтры
      
      // Фильтр по состоянию.
      var statuses = new List<Enumeration>();
      if (_filter.Draft)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Draft);
      if (_filter.Active)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
      if (_filter.Executed)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      if (_filter.Terminated)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      if (_filter.Cancelled)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);

      // Фильтр по состоянию.
      if (statuses.Any())
        query = query.Where(q => q.LifeCycleState != null && statuses.Contains(q.LifeCycleState.Value));
      
      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
        query = query.Where(c => Equals(c.DocumentKind, _filter.DocumentKind));
      
      // Фильтр "Категория".
      if (_filter.Category != null)
        query = query.Where(c => ContractBases.Is(c) && Equals(c.DocumentGroup, _filter.Category));
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
        query = query.Where(c => Equals(c.Counterparty, _filter.Contractor));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        query = query.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        query = query.Where(c => Equals(c.Department, _filter.Department));
      
      #region Фильтрация по дате договора
      
      var beginDate = Calendar.UserToday.AddDays(-30);
      var endDate = Calendar.UserToday;
      
      if (_filter.Last365days)
        beginDate = Calendar.UserToday.AddDays(-365);
      
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
      
      // Фильтр по типу документа
      if (_filter.Contracts != _filter.SupAgreements)
      {
        if (_filter.Contracts)
          query = query.Where(d => ContractBases.Is(d));

        if (_filter.SupAgreements)
          query = query.Where(d => SupAgreements.Is(d));
      }
      else
        query = query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      #endregion
      
      return query;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsListDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ContractsHistoryFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var documents = query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d));
      
      if (_filter == null)
        return documents;
      
      #region Фильтр по состоянию и датам
      
      DateTime beginPeriod = Calendar.SqlMinValue;
      DateTime endPeriod = Calendar.UserToday.EndOfDay().FromUserTime();
      
      if (_filter.Last30days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      
      if (_filter.Last365days)
        beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-365));
      
      if (_filter.ManualPeriod)
      {
        if (_filter.DateRangeFrom.HasValue)
          beginPeriod = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(_filter.DateRangeFrom.Value);
        
        endPeriod = _filter.DateRangeTo.HasValue ? _filter.DateRangeTo.Value.EndOfDay().FromUserTime() : Calendar.SqlMaxValue;
      }
      
      Enumeration? operation = null;
      var lifeCycleStates = new List<Enumeration>();
      
      if (_filter.Concluded)
      {
        operation = new Enumeration("SetToActive");
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (_filter.Executed)
      {
        operation = new Enumeration("SetToClosed");
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      }
      
      if (_filter.Terminated)
      {
        operation = new Enumeration("SetToTerminated");
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Terminated);
      }
      
      if (_filter.Cancelled)
      {
        operation = new Enumeration("SetToObsolete");
        lifeCycleStates.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);
      }
      
      // Использовать можно только один WhereDocumentHistory, т.к. это отдельный подзапрос (+join).
      documents = documents.Where(d => d.LifeCycleState.HasValue && lifeCycleStates.Contains(d.LifeCycleState.Value))
        .WhereDocumentHistory(h => h.Operation == operation && h.HistoryDate.Between(beginPeriod, endPeriod));
      
      #endregion
      
      #region Фильтры по полям навигации
      
      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
        documents = documents.Where(c => Equals(c.DocumentKind, _filter.DocumentKind));
      
      // Фильтр "Категория".
      if (_filter.Category != null)
        documents = documents.Where(c => Equals(c.DocumentGroup, _filter.Category));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        documents = documents.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Ответственный".
      if (_filter.Responsible != null)
        documents = documents.Where(c => Equals(c.ResponsibleEmployee, _filter.Responsible));
      
      #endregion
      
      #region Фильтр по типу документа
      
      if (_filter.SupAgreements || _filter.Contracts)
        documents = documents.Where(d => _filter.Contracts && ContractBases.Is(d) || _filter.SupAgreements && SupAgreements.Is(d));
      
      #endregion
      
      return documents;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsHistoryDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ExpiringSoonContractsFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> ExpiringSoonContractsDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      // Документ действующий и осталось 14 (либо указанное в договоре число) дней до окончания документа.
      var today = Calendar.UserToday;
      var documents = query.Where(d => d.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active)
        .Where(d => SupAgreements.Is(d) || ContractBases.Is(d) && (ContractBases.As(d).DaysToFinishWorks == null ||
                                                                   ContractBases.As(d).DaysToFinishWorks <= Docflow.PublicConstants.Module.MaxDaysToFinish))
        .Where(d => (ContractBases.Is(d) && today.AddDays(ContractBases.As(d).DaysToFinishWorks ?? 14) >= d.ValidTill) ||
               (SupAgreements.Is(d) && today.AddDays(14) >= d.ValidTill));
      
      if (_filter == null)
        return documents;
      
      #region Фильтры
      
      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
      {
        documents = documents.Where(d => Equals(d.DocumentKind, _filter.DocumentKind));
      }
      
      // Фильтр "Категория".
      if (_filter.Category != null)
      {
        documents = documents.Where(d => ContractBases.Is(d) && Equals(d.DocumentGroup, _filter.Category));
      }
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
      {
        documents = documents.Where(d => Equals(d.Counterparty, _filter.Contractor));
      }
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
      {
        documents = documents.Where(d => Equals(d.BusinessUnit, _filter.BusinessUnit));
      }
      
      // Фильтр "Ответственный".
      if (_filter.Responsible != null)
      {
        documents = documents.Where(d => Equals(d.ResponsibleEmployee, _filter.Responsible));
      }
      
      // Фильтр по типу документа
      if (_filter.Contracts && !_filter.SupAgreements)
        return documents.Where(d => ContractBases.Is(d));
      if (_filter.SupAgreements && !_filter.Contracts)
        return documents.Where(d => SupAgreements.Is(d));
      
      #endregion
      
      return documents;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ExpiringSoonContractsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, true);
    }
  }

  partial class ContractsAtContractorsFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> ContractsAtContractorsDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      var documents = Docflow.OfficialDocuments.GetAll().Where(c => c.IsHeldByCounterParty.Value &&
                                                               (ContractBases.Is(c) || SupAgreements.Is(c) ||
                                                                FinancialArchive.ContractStatements.Is(c) || FinancialArchive.Waybills.Is(c) ||
                                                                FinancialArchive.UniversalTransferDocuments.Is(c)));
      
      if (_filter == null)
        return documents;

      #region Фильтры

      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
        documents = documents.Where(c => Equals(c.DocumentKind, _filter.DocumentKind));
      
      // Фильтр "Категория".
      if (_filter.Category != null)
        documents = documents.Where(c => !ContractBases.Is(c) || (ContractBases.Is(c) && Equals(c.DocumentGroup, _filter.Category)));
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
        documents = documents.Where(c => (Docflow.ContractualDocumentBases.Is(c) && Equals(Docflow.ContractualDocumentBases.As(c).Counterparty, _filter.Contractor)) ||
                                    (Docflow.AccountingDocumentBases.Is(c) && Equals(Docflow.AccountingDocumentBases.As(c).Counterparty, _filter.Contractor)));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        documents = documents.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Ответственный за возврат".
      if (_filter.Responsible != null)
        documents = documents.Where(c => Equals(c.ResponsibleForReturnEmployee, _filter.Responsible));
      
      // Фильтр "Только просроченные".
      if (_filter.OnlyOverdue)
      {
        var today = Calendar.UserToday;
        documents = documents.Where(c => c.ScheduledReturnDateFromCounterparty < today);
      }
      
      #endregion
      
      return documents;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> ContractsAtContractorsDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return Functions.Module.ContractsFilterContractsKind(query, false);
    }
  }

  partial class IssuanceJournalFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> IssuanceJournalDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      var today = Calendar.UserToday;
      var documents = query.Where(l => l.IsReturnRequired == true &&
                                  (Sungero.Docflow.ContractualDocumentBases.Is(l) || Sungero.Docflow.AccountingDocumentBases.Is(l)));

      if (_filter == null)
        return documents;

      // Фильтр по статусу.
      if (_filter.Overdue)
        documents = documents
          .Where(l => l.Tracking.Any(d => (d.ReturnDate > d.ReturnDeadline) || (!d.ReturnDate.HasValue && d.ReturnDeadline < today)));
      
      // Фильтр по виду документа.
      if (_filter.DocumentKind != null)
        documents = documents.Where(l => Equals(l.DocumentKind, _filter.DocumentKind));
      
      // Фильтр по сотруднику.
      if (_filter.Employee != null)
        documents = documents.Where(l => l.Tracking.Any(p => !p.ReturnDate.HasValue && Equals(p.DeliveredTo, _filter.Employee)));
      
      // Фильтр по подразделению.
      if (_filter.Department != null)
        documents = documents.Where(l => l.Tracking.Any(p => !p.ReturnDate.HasValue &&
                                                        p.DeliveredTo != null &&
                                                        Equals(p.DeliveredTo.Department, _filter.Department)));
      
      // Фильтр по группе регистрации.
      if (_filter.RegistrationGroup != null)
        documents = documents.Where(l => l.DocumentRegister != null && Equals(l.DocumentRegister.RegistrationGroup, _filter.RegistrationGroup));
      
      // Фильтр по делу.
      if (_filter.File != null)
        documents = documents.Where(l => Equals(l.CaseFile, _filter.File));
      
      // Исключить строки из таблиц Выдачи с Результатом возврата: "Возвращен".
      var returned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;
      
      // Фильтр по сроку возврата.
      if (_filter.EndDay)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                        p.ReturnDeadline < today.AddDays(1)));

      if (_filter.EndWeek)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                        p.ReturnDeadline <= today.EndOfWeek()));
      
      if (_filter.EndMonth)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                        p.ReturnDeadline <= today.EndOfMonth()));
      
      if (_filter.Manual)
      {
        var dateFrom = _filter.ReturnPeriodDataRangeFrom;
        var dateTo = _filter.ReturnPeriodDataRangeTo;
        
        if (dateFrom.HasValue && !dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline >= dateFrom.Value));
        
        if (dateTo.HasValue && !dateFrom.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= dateTo.Value));
        
        if (dateFrom.HasValue && dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                          p.ReturnDeadline.Between(dateFrom.Value, dateTo.Value)));
      }
      
      return documents;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> IssuanceJournalDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      query = query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(d => d.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.IncomingTaxInvoiceGuid ||
               d.DocumentType.DocumentTypeGuid == Docflow.PublicConstants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
      
      return query;
    }
  }

  partial class ContractsUIHandlers
  {

  }
}
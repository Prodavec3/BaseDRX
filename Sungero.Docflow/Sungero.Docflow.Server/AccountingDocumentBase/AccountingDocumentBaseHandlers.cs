using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow
{
  partial class AccountingDocumentBaseConvertingFromServerHandler
  {
    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      if (Sungero.Docflow.Addendums.Is(_source))
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.LeadingDocument);
      
      e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Corrected);

      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var accountingDocument = AccountingDocumentBases.As(e.Entity);
        accountingDocument.Counterparty = counterparty;
      }
    }
  }

  partial class AccountingDocumentBaseCorrectedPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CorrectedFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(x => x.Id != _obj.Id && x.IsAdjustment != true);
      
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      
      return query;
    }
  }

  partial class AccountingDocumentBaseLeadingDocumentPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> LeadingDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.LeadingDocumentFiltering(query, e);
      var documents = query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete));
      
      if (_obj.Counterparty != null)
        documents = documents.Where(d => d.Counterparty == _obj.Counterparty);
      
      return documents;
    }
  }

  partial class AccountingDocumentBaseCounterpartySignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartySignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

  partial class AccountingDocumentBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.IsFormalized == true)
        e.Without(_info.Properties.Versions);
      
      if (_source.LeadingDocument == null || !_source.LeadingDocument.AccessRights.CanRead())
        e.Without(_info.Properties.LeadingDocument);
      
      if (_source.Corrected == null || !_source.LeadingDocument.AccessRights.CanRead())
        e.Without(_info.Properties.Corrected);
      
      e.Without(_info.Properties.IsFormalized);
      e.Without(_info.Properties.SellerTitleId);
      e.Without(_info.Properties.BuyerTitleId);
      e.Without(_info.Properties.SellerSignatureId);
      e.Without(_info.Properties.BuyerSignatureId);
      e.Without(_info.Properties.BusinessUnitBox);
      e.Without(_info.Properties.FormalizedServiceType);
      e.Without(_info.Properties.IsRevision);
      e.Without(_info.Properties.FormalizedFunction);
    }
  }

  partial class AccountingDocumentBaseFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      #region Фильтры
      
      if (_filter == null)
        return query;
      
      // Исключаем входящие и исходящие счета из финархива, но только если это визуальный список с панелью фильтрации.
      var documents = query.Where(d => !Contracts.IncomingInvoices.Is(d) && !Contracts.OutgoingInvoices.Is(d));

      // Состояние.
      if ((_filter.DraftState || _filter.ActiveState || _filter.ObsoleteState) &&
          !(_filter.DraftState && _filter.ActiveState && _filter.ObsoleteState))
      {
        documents = documents.Where(x => (_filter.DraftState && x.LifeCycleState == AccountingDocumentBase.LifeCycleState.Draft) ||
                                    (_filter.ActiveState && x.LifeCycleState == AccountingDocumentBase.LifeCycleState.Active) ||
                                    (_filter.ObsoleteState && x.LifeCycleState == AccountingDocumentBase.LifeCycleState.Obsolete));
      }
      
      // Фильтр "Период".
      var beginDate = Calendar.UserToday.BeginningOfMonth();
      var endDate = Calendar.UserToday.EndOfMonth();

      if (_filter.PreviousMonth)
      {
        beginDate = Calendar.UserToday.AddMonths(-1).BeginningOfMonth();
        endDate = Calendar.UserToday.AddMonths(-1).EndOfMonth();
      }
      if (_filter.CurrentQuarter)
      {
        beginDate = Functions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday);
        endDate = Functions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday);
      }
      if (_filter.PreviousQuarter)
      {
        beginDate = Functions.AccountingDocumentBase.BeginningOfQuarter(Calendar.UserToday.AddMonths(-3));
        endDate = Functions.AccountingDocumentBase.EndOfQuarter(Calendar.UserToday.AddMonths(-3));
      }

      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        endDate = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, beginDate) ? beginDate : Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, endDate) ? endDate : endDate.EndOfDay().FromUserTime();
      var clientPeriodEnd = !Equals(Calendar.SqlMaxValue, endDate) ? endDate.AddDays(1) : Calendar.SqlMaxValue;
      documents = documents.Where(j => (j.DocumentDate.Between(serverPeriodBegin, serverPeriodEnd) ||
                                        j.DocumentDate == beginDate) && j.DocumentDate != clientPeriodEnd);
      
      // Фильтр "Типы документов".
      var documentTypes = new List<string>();
      if (_filter.TaxInvoice)
      {
        documentTypes.Add(Constants.AccountingDocumentBase.IncomingTaxInvoiceGuid);
        documentTypes.Add(Constants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
      }
      if (_filter.Waybill)
        documentTypes.Add(Constants.AccountingDocumentBase.WaybillInvoiceGuid);
      if (_filter.ContractStatement)
        documentTypes.Add(Constants.AccountingDocumentBase.ContractStatementInvoiceGuid);
      if (_filter.UniversalTransfer)
        documentTypes.Add(Constants.AccountingDocumentBase.UniversalTransferDocumentGuid);
      
      if (documentTypes.Any())
        documents = documents.Where(c => documentTypes.Contains(c.DocumentKind.DocumentType.DocumentTypeGuid));
      
      // Фильтр "Наша организация".
      if (_filter.BusinessUnit != null)
        documents = documents.Where(c => Equals(c.BusinessUnit, _filter.BusinessUnit));
      
      // Фильтр "Подразделение".
      if (_filter.Department != null)
        documents = documents.Where(c => Equals(c.Department, _filter.Department));
      
      // Фильтр "Контрагенты".
      if (_filter.Counterparty != null)
        documents = documents.Where(c => Equals(c.Counterparty, _filter.Counterparty));
      
      #endregion
      
      return documents;
    }
  }

  partial class AccountingDocumentBaseContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => c.Company == _obj.Counterparty);
      
      return query;
    }
  }

  partial class AccountingDocumentBaseServerHandlers
  {

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      base.BeforeSigning(e);
      
      // Если подписание выполняется в рамках агента - генерировать заглушку не надо.
      bool jobRan;
      if (e.Params.TryGetValue(ExchangeCore.PublicConstants.BoxBase.JobRunned, out jobRan) && jobRan)
        return;
      
      if (_obj.BuyerTitleId.HasValue &&
          e.Signature.SignatureType == SignatureType.Approval &&
          !Signatures.Get(_obj.Versions.Single(v => v.Id == _obj.BuyerTitleId.Value)).Any(s => s.SignatureType == SignatureType.Approval))
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Выдать ответственному права на изменение документа.
      var responsible = _obj.ResponsibleEmployee;
      if (responsible != null && !Equals(_obj.State.Properties.ResponsibleEmployee.OriginalValue, responsible) &&
          !Equals(responsible, Sungero.Company.Employees.Current) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, responsible) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, responsible) &&
          _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
        _obj.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Change);
      
      if (_obj.LeadingDocument != null && _obj.LeadingDocument.AccessRights.CanRead() &&
          !_obj.Relations.GetRelatedFrom(Contracts.PublicConstants.Module.AccountingDocumentsRelationName).Contains(_obj.LeadingDocument))
        _obj.Relations.AddFromOrUpdate(Contracts.PublicConstants.Module.AccountingDocumentsRelationName, _obj.State.Properties.LeadingDocument.OriginalValue, _obj.LeadingDocument);
      
      if (_obj.Corrected != null && _obj.Corrected.AccessRights.CanRead() && !_obj.Relations.GetRelatedFrom(Constants.Module.CorrectionRelationName).Contains(_obj.Corrected))
        _obj.Relations.AddFromOrUpdate(Constants.Module.CorrectionRelationName, _obj.State.Properties.Corrected.OriginalValue, _obj.Corrected);
      
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.ResponsibleEmployee == null)
        _obj.ResponsibleEmployee = Company.Employees.As(_obj.Author);
      
      if (_obj.IsAdjustment == null)
        _obj.IsAdjustment = false;
      
      if (_obj.IsRevision == null)
        _obj.IsRevision = false;
      
      if (_obj.State.IsInserted && _obj.Corrected != null)
        _obj.Relations.AddFrom(Constants.Module.CorrectionRelationName, _obj.Corrected);
      
      if (_obj.State.IsInserted && _obj.LeadingDocument != null)
        _obj.Relations.AddFrom(Contracts.PublicConstants.Module.AccountingDocumentsRelationName, _obj.LeadingDocument);
    }

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);

      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isVersionCreateAction = e.Action == Sungero.CoreEntities.History.Action.Update &&
        e.Operation == new Enumeration(Constants.OfficialDocument.Operation.CreateVersion);
      var properties = _obj.State.Properties;

      // Изменять историю только для изменения и создания документа.
      if (!isUpdateAction && !isCreateAction)
        return;
      
      // Добавить комментарий к записи создания в истории о том, что титул продавца получен через СО.
      if (_obj.IsFormalized == true && isCreateAction && _obj.SellerTitleId.HasValue && _obj.ExchangeState != null)
      {
        var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, _obj.SellerTitleId.Value);
        if (info != null)
        {
          e.OperationDetailed = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.SellerTitleFromExchangeService);
          e.Comment = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(info.Box).Name;
        }
      }
      
      // Добавить комментарий к записи создания в истории о том, что заполнен титул покупателя.
      if (_obj.IsFormalized == true && isVersionCreateAction && _obj.ExchangeState != null)
        e.OperationDetailed = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.BuyerTitle);
      
      // Изменение суммы или валюты.
      var sumWasChanged = _obj.State.Properties.TotalAmount.IsChanged || (_obj.State.Properties.Currency.IsChanged && _obj.TotalAmount.HasValue);
      if (sumWasChanged)
      {
        // Локализация для операции в ресурсах OfficialDocument.
        var operation = new Enumeration(Constants.OfficialDocument.Operation.TotalAmountChange);
        var operationDetailed = _obj.TotalAmount.HasValue ? operation : new Enumeration(Constants.OfficialDocument.Operation.TotalAmountClear);
        var currency = (_obj.Currency == null) ? string.Empty : _obj.Currency.AlphaCode;
        var comment = _obj.TotalAmount.HasValue ? string.Join("|", _obj.TotalAmount.Value, currency) : string.Empty;

        e.Write(operation, operationDetailed, comment);
      }
    }
  }

}
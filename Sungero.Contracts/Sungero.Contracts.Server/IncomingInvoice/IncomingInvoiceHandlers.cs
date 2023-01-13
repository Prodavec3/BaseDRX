using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class IncomingInvoiceConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);  

      if (Sungero.Docflow.AccountingDocumentBases.Is(_source))
      {
        e.Map(_info.Properties.Number, Sungero.Docflow.AccountingDocumentBases.Info.Properties.RegistrationNumber);
        e.Map(_info.Properties.Date, Sungero.Docflow.AccountingDocumentBases.Info.Properties.RegistrationDate);
        e.Map(_info.Properties.Contract, Sungero.Docflow.AccountingDocumentBases.Info.Properties.LeadingDocument);
        
        // Котегов: Отключен проброс Number и Date, иначе они перетирали одноименные свойства (баг 115832).
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Number);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Date);
        
        // Отключить проброс полей, которых нет во входящих счетах.
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.IsAdjustment);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySignatory);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.CounterpartySigningReason);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.Contact);
        e.Without(Sungero.Docflow.AccountingDocumentBases.Info.Properties.ResponsibleEmployee);
      }
      
      // Исключаем проброс LeadingDocument, так как в счете должно заполняться поле Contract.
      e.Without(Sungero.Docflow.OfficialDocuments.Info.Properties.LeadingDocument);
    }
  }

  partial class IncomingInvoiceCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      if (_source.Contract == null || !_source.Contract.AccessRights.CanRead())
        e.Without(_info.Properties.Contract);
    }
  }

  partial class IncomingInvoiceServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isChangeTypeAction = e.Action == Sungero.CoreEntities.History.Action.ChangeType;
      
      if (!isCreateAction && !isChangeTypeAction)
        base.BeforeSaveHistory(e);
      else
      {
        // Изменение суммы или валюты.
        var sumWasChanged = _obj.State.Properties.TotalAmount.IsChanged || (_obj.State.Properties.Currency.IsChanged && _obj.TotalAmount.HasValue);
        if (sumWasChanged)
        {
          // Локализация для операции в ресурсах OfficialDocument.
          var operation = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.TotalAmountChange);
          var operationDetailed = _obj.TotalAmount.HasValue ? operation : new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.TotalAmountClear);
          var currency = (_obj.Currency == null) ? string.Empty : _obj.Currency.AlphaCode;
          var comment = _obj.TotalAmount.HasValue ? string.Join("|", _obj.TotalAmount.Value, currency) : string.Empty;
          
          e.Write(operation, operationDetailed, comment);
        }
      }
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.Date.HasValue && _obj.PaymentDueDate.HasValue &&
          _obj.Date.Value > _obj.PaymentDueDate)
        e.AddError(_obj.Info.Properties.PaymentDueDate, IncomingInvoices.Resources.DatePaymentDeadlineValidationMessage, _obj.Info.Properties.Date);
      
      if (Functions.IncomingInvoice.HaveDuplicates(_obj,
                                                   _obj.DocumentKind,
                                                   _obj.Number,
                                                   _obj.Date,
                                                   _obj.TotalAmount,
                                                   _obj.Currency,
                                                   _obj.Counterparty))
        e.AddWarning(IncomingInvoices.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
      
      if (_obj.Contract != null && _obj.Contract.AccessRights.CanRead() && !_obj.Relations.GetRelatedFrom(Constants.Module.AccountingDocumentsRelationName).Contains(_obj.Contract))
        _obj.Relations.AddFromOrUpdate(Constants.Module.AccountingDocumentsRelationName, _obj.State.Properties.Contract.OriginalValue, _obj.Contract);
      
      _obj.DocumentDate = _obj.Date.HasValue ? _obj.Date : _obj.Created;
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsInserted && _obj.Contract != null)
        _obj.Relations.AddFrom(Constants.Module.AccountingDocumentsRelationName, _obj.Contract);
      
      _obj.ResponsibleEmployee = null;
    }
  }

  partial class IncomingInvoiceFilteringServerHandler<T>
  {
    /// <summary>
    /// Фильтрация списка входящих счетов.
    /// </summary>
    /// <param name="query">Фильтруемый список счетов.</param>
    /// <param name="e">Аргументы события фильтрации.</param>
    /// <returns>Список счетов с примененными фильтрами.</returns>
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return base.Filtering(query, e);
      
      // Состояние.
      if ((_filter.Draft || _filter.OnApproval || _filter.PayAccepted || _filter.PayRejected || _filter.PayComplete) &&
          !(_filter.Draft && _filter.OnApproval && _filter.PayAccepted && _filter.PayRejected && _filter.PayComplete))
      {
        query = query.Where(x => (_filter.Draft && x.LifeCycleState == IncomingInvoice.LifeCycleState.Draft && x.InternalApprovalState == null) ||
                            (_filter.OnApproval && x.LifeCycleState != IncomingInvoice.LifeCycleState.Obsolete &&
                             (x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                              x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnRework ||
                              x.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval)) ||
                            (_filter.PayAccepted && x.LifeCycleState == IncomingInvoice.LifeCycleState.Active) ||
                            (_filter.PayRejected && x.LifeCycleState == IncomingInvoice.LifeCycleState.Obsolete) ||
                            (_filter.PayComplete && x.LifeCycleState == IncomingInvoice.LifeCycleState.Paid));
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

  partial class IncomingInvoiceContractPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> ContractFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Counterparty, _obj.Counterparty));
      
      query = query.Where(c => !Equals(c.LifeCycleState, Sungero.Contracts.ContractBase.LifeCycleState.Obsolete));
      
      return query;
    }
  }
}
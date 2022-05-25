using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties
{
  partial class CounterpartyCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ExchangeBoxes);
    }
  }

  partial class CounterpartyFilteringServerHandler<T>
  {
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter != null)
      {
        if (_filter.Company || _filter.Person || _filter.Bank)
          query = query.Where(x => (_filter.Company && Companies.Is(x)) ||
                              (_filter.Person && People.Is(x)) ||
                              (_filter.Bank && Banks.Is(x)));
        
        if (_filter.Responsible != null)
          query = query.Where(x => Counterparties.Is(x) && Equals(_filter.Responsible, Counterparties.As(x).Responsible));
      }
      
      // Исключение системного контрагента - "По списку рассылки".
      var distributionListCounterparty = Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty();
      return query.Where(x => !Equals(x, distributionListCounterparty));
    }
  }

  partial class CounterpartyCityPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> CityFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтровать населенные пункты по региону.
      if (_obj.Region != null)
        query = query.Where(settlement => Equals(settlement.Region, _obj.Region));
      
      return query;
    }
  }

  partial class CounterpartyServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!Commons.PublicFunctions.Module.IsAllExternalEntityLinksDeleted(_obj))
        throw AppliedCodeException.Create(Commons.Resources.HasLinkedExternalEntities);
    }

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      var isExchangeBoxesAction = _obj.State.Properties.ExchangeBoxes.IsChanged;
      
      // Изменять историю только для изменения эл. обмена.
      if (!isExchangeBoxesAction)
        return;
      
      var exchangeBoxes = _obj.ExchangeBoxes.ToList();
      foreach (var exchangeBox in exchangeBoxes)
      {
        if (exchangeBox.State.Properties.Status.IsChanged)
        {
          var operation = e.Operation;
          if (exchangeBox.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Active)
            operation = new Enumeration(Constants.Counterparty.ExchangeWithCAActivated);
          else if (exchangeBox.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Closed)
            operation = new Enumeration(Constants.Counterparty.ExchangeWithCAClosed);
          else if (exchangeBox.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA)
            operation = new Enumeration(Constants.Counterparty.InvitationSentToCA);
          else if (exchangeBox.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
            operation = new Enumeration(Constants.Counterparty.InvitationSentToUs);
          else
            return;
          
          var operationDetailed =
            new Enumeration(Constants.Counterparty.StatusChanged);
          var comment = string.Format("{0}|{1}", exchangeBox.Box.BusinessUnit, exchangeBox.Box.ExchangeService);
          e.Write(operation, operationDetailed, comment);
        }
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Nonresident = false;
      _obj.CanExchange = false;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_obj.Code))
      {
        _obj.Code = _obj.Code.Trim();
        if (Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Sungero.Company.Resources.NoSpacesInCode);
      }
      
      // Проверить код на пробелы, если свойство изменено.
      if (!string.IsNullOrEmpty(_obj.Code))
      {
        // При изменении кода e.AddError сбрасывается.
        var codeIsChanged = _obj.State.Properties.Code.IsChanged;
        _obj.Code = _obj.Code.Trim();
        
        if (codeIsChanged && Regex.IsMatch(_obj.Code, @"\s"))
          e.AddError(_obj.Info.Properties.Code, Sungero.Company.Resources.NoSpacesInCode);
      }
      
      if (!_obj.AccessRights.CanChangeCard())
      {
        var exchangeBoxesProp = _obj.State.Properties.ExchangeBoxes;
        var canExchangeProp = _obj.State.Properties.CanExchange;
        
        if (_obj.State.Properties
            .Where(x => !Equals(x, exchangeBoxesProp) && !Equals(x, canExchangeProp))
            .Select(x => x as Sungero.Domain.Shared.IPropertyState)
            .Where(x => x != null)
            .Any(x => x.IsChanged))
        {
          e.AddError(Counterparties.Resources.NoRightsToChangeCard);
        }
      }
      
      // Трим пробелов в ИНН, ОГРН, ОКПО.
      if (!string.IsNullOrEmpty(_obj.TIN))
        _obj.TIN = _obj.TIN.Trim();

      if (!string.IsNullOrEmpty(_obj.PSRN))
        _obj.PSRN = _obj.PSRN.Trim();
      
      if (!string.IsNullOrEmpty(_obj.NCEO))
        _obj.NCEO = _obj.NCEO.Trim();

      // Проверка корректности ИНН.
      if (_obj.Nonresident != true)
      {
        var errorMessage = Functions.Counterparty.CheckTin(_obj, _obj.TIN);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.TIN, errorMessage);

        // Проверка корректности ОГРН.
        errorMessage = Functions.Counterparty.CheckPsrnLength(_obj, _obj.PSRN);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.PSRN, errorMessage);
        
        // Проверка корректности ОКПО.
        errorMessage = Functions.Counterparty.CheckNceoLength(_obj, _obj.NCEO);
        if (!string.IsNullOrEmpty(errorMessage))
          e.AddError(_obj.Info.Properties.NCEO, errorMessage);
      }
      
      // Проверка дублей контрагента.
      var saveFromUI = e.Params.Contains(Counterparties.Resources.ParameterSaveFromUIFormat(_obj.Id));
      var isForceDuplicateSave = e.Params.Contains(Counterparties.Resources.ParameterIsForceDuplicateSaveFormat(_obj.Id));
      if (saveFromUI && !isForceDuplicateSave)
      {
        var checkDuplicatesErrorText = Functions.Counterparty.GetCounterpartyDuplicatesErrorText(_obj);
        if (!string.IsNullOrWhiteSpace(checkDuplicatesErrorText))
          e.AddError(checkDuplicatesErrorText, _obj.Info.Actions.ShowDuplicates, _obj.Info.Actions.ForceDuplicateSave);
      }
      
      // Проверка ящиков эл. обмена.
      foreach (var box in _obj.ExchangeBoxes.Select(x => x.Box).Distinct())
      {
        var boxLines = _obj.ExchangeBoxes.Where(x => Equals(x.Box, box)).ToList();
        if (boxLines.All(x => x.IsDefault == false))
        {
          foreach (var boxLine in boxLines)
            e.AddError(boxLine, _obj.Info.Properties.ExchangeBoxes.Properties.IsDefault,
                       Counterparties.Resources.NoDefaultBoxServiceFormat(boxLine.Box),
                       _obj.Info.Properties.ExchangeBoxes.Properties.IsDefault);
        }
      }
    }
  }
}
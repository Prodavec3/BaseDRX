using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow
{

  partial class ContractualDocumentBaseConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);

      if (Sungero.Docflow.Addendums.Is(_source))
        e.Without(Sungero.Docflow.ContractualDocumentBases.Info.Properties.LeadingDocument);
      
      var counterparty = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentCounterparty(_source, _source.LastVersion);
      if (counterparty != null)
      {
        var contractualDocument = ContractualDocumentBases.As(e.Entity);
        contractualDocument.Counterparty = counterparty;
      }
    }
  }

  partial class ContractualDocumentBaseServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var properties = _obj.State.Properties;

      // Изменять историю только для изменения и создания документа.
      if (!isUpdateAction && !isCreateAction)
        return;
      
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
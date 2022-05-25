using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ExchangeDocument;

namespace Sungero.Docflow
{
  partial class ExchangeDocumentServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      // Добавить комментарий к записи создания в истории.
      if (e.Action == Sungero.CoreEntities.History.Action.Create)
      {
        e.OperationDetailed = new Enumeration(Sungero.Docflow.Constants.OfficialDocument.Operation.FromExchangeService);
        e.Comment = _obj.BusinessUnitBox.ExchangeService.Name;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.LifeCycleState = LifeCycleState.Draft;
      
      if (!_obj.State.IsCopied)
        _obj.DeliveryMethod = MailDeliveryMethods.GetAll(m => m.Sid == Constants.MailDeliveryMethod.Exchange).FirstOrDefault();
    }
  }
}
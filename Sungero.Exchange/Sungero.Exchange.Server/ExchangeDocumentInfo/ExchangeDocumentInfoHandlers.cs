using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.Exchange.ExchangeDocumentInfo;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentInfoUiFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.UiFilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      // Вернуть пустой список, если пользователь не администратор или аудитор.
      if (!Sungero.Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
        return query.Where(i => 1 == 0);
      
      return query;
    }
  }
  
  partial class ExchangeDocumentInfoServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.RevocationStatus = RevocationStatus.None;
    }
  }

}
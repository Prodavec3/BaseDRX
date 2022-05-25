using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.QueueItemBase;

namespace Sungero.ExchangeCore
{
  partial class QueueItemBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.LastUpdate = Calendar.Now;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Retries = 0;
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingTask;

namespace Sungero.ExchangeCore
{
  partial class CounterpartyConflictProcessingTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedsReview = false;
    }
  }

}
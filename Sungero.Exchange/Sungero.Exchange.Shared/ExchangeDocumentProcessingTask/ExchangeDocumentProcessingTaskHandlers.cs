using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingTask;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentProcessingTaskSharedHandlers
  {

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = _obj.Deadline;
    }

  }
}
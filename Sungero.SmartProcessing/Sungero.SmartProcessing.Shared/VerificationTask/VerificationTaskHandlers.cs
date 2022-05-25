using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationTask;

namespace Sungero.SmartProcessing
{
  partial class VerificationTaskSharedHandlers
  {

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = _obj.Deadline;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StoragePolicyBase;

namespace Sungero.Docflow
{
  partial class StoragePolicyBaseClientHandlers
  {

    public virtual void PriorityValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(Sungero.Docflow.StoragePolicyBases.Resources.IncorrectPriority);
    }

  }
}
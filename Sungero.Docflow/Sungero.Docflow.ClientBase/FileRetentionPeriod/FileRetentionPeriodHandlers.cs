using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FileRetentionPeriod;

namespace Sungero.Docflow
{
  partial class FileRetentionPeriodClientHandlers
  {

    public virtual void RetentionPeriodValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Срок хранения не может быть отрицательным.
      if (e.NewValue != null && e.NewValue != e.OldValue && e.NewValue < 0)
        e.AddError(FileRetentionPeriods.Resources.WrongRetentionPeriod);
    }
  }
}
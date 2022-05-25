using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DistributionList;

namespace Sungero.Docflow
{
  partial class DistributionListAddresseesClientHandlers
  {

    public virtual void AddresseesNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(Resources.NumberDistributionListIsNotPositive);
    }
  }

  partial class DistributionListClientHandlers
  {

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore.Shared
{
  partial class BoxBaseFunctions
  {
    /// <summary>
    /// Получить основной ящик.
    /// </summary>
    /// <returns>Основной ящик.</returns>
    [Public]
    public virtual IBusinessUnitBox GetRootBox()
    {
      return null;
    }
    
  }
}
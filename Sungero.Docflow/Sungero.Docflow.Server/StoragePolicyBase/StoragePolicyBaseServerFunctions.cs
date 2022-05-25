using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StoragePolicyBase;

namespace Sungero.Docflow.Server
{
  partial class StoragePolicyBaseFunctions
  {

    /// <summary>
    /// Проверить наличие политик с таким же приоритетом.
    /// </summary>
    /// <returns>Признак наличия политик с таким же приоритетом.</returns>
    public virtual bool HasSamePriorityPolicies()
    {
      return false;
    }

  }
}
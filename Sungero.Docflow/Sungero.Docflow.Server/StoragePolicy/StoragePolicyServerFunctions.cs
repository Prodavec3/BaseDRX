using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StoragePolicy;

namespace Sungero.Docflow.Server
{
  partial class StoragePolicyFunctions
  {

    /// <summary>
    /// Проверить наличие политик с таким же приоритетом.
    /// </summary>
    /// <returns>Признак наличия политик с таким же приоритетом.</returns>
    public override bool HasSamePriorityPolicies()
    {
      return StoragePolicies.GetAll().Any(x => x.Status == Docflow.StoragePolicyBase.Status.Active && x.Priority == _obj.Priority && x.Id != _obj.Id);
    }
  }
}
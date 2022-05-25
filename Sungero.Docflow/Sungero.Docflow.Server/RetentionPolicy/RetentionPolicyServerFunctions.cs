using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RetentionPolicy;

namespace Sungero.Docflow.Server
{
  partial class RetentionPolicyFunctions
  {

    /// <summary>
    /// Проверить наличие политик с таким же приоритетом.
    /// </summary>
    /// <returns>Признак наличия политик с таким же приоритетом.</returns>
    public override bool HasSamePriorityPolicies()
    {
      return RetentionPolicies.GetAll().Any(x => x.Status == Docflow.StoragePolicyBase.Status.Active && x.Priority == _obj.Priority && x.Id != _obj.Id);
    }

  }
}
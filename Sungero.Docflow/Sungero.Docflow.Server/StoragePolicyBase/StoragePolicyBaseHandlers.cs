using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.StoragePolicyBase;

namespace Sungero.Docflow
{
  partial class StoragePolicyBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var isNotUniquePriority = Functions.StoragePolicyBase.HasSamePriorityPolicies(_obj);
      if (isNotUniquePriority)
        e.AddError(_obj.Info.Properties.Priority, Sungero.Docflow.StoragePolicyBases.Resources.NonUniquePriority, _obj.Info.Properties.Priority);
    }
  }

}
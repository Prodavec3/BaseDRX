using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractsApprovalRuleSharedHandlers
  {
    
    public override void DocumentKindsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      base.DocumentKindsChanged(e);
      
      e.Params.AddOrUpdate(Constants.ContractsApprovalRule.IsSupportConditions, false);
      
      if (!_obj.DocumentKinds.Any())
        e.Params.AddOrUpdate(Constants.ContractsApprovalRule.IsSupportConditions, true);
    }
  }

}
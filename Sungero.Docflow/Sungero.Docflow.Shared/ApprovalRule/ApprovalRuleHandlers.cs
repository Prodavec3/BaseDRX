using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRule;

namespace Sungero.Docflow
{
  partial class ApprovalRuleSharedHandlers
  {
    
    public override void DocumentKindsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      base.DocumentKindsChanged(e);
    }
  }

}
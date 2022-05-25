using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRule;

namespace Sungero.Docflow
{
  partial class ApprovalRuleClientHandlers
  {
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e) 
    {
      base.Refresh(e);
    }

    public override IEnumerable<Enumeration> DocumentFlowFiltering(IEnumerable<Enumeration> query) 
    {
      query = base.DocumentFlowFiltering(query);
      return query.Where(f => f != ApprovalRuleBase.DocumentFlow.Contracts);
    }

  }
}
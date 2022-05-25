using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRule;

namespace Sungero.Docflow
{
  partial class ApprovalRuleServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e) 
    {
      base.Created(e);
      
      if (_obj.State.IsCopied)
      {
        foreach (var conditionsList in _obj.Conditions)
        {         
          var newCondition = Conditions.Copy(Conditions.As(conditionsList.Condition));
          newCondition.Save();
          conditionsList.Condition = newCondition;
        }
      }
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractsApprovalRuleobsoleteContractCategoriesCategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ObsoleteContractCategoriesCategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)   
    {
      return query; 
    }
  }

  partial class ContractsApprovalRuleServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e) 
    {
      base.Created(e);
      _obj.DocumentFlow = Docflow.ApprovalRuleBase.DocumentFlow.Contracts;
      
      if (_obj.State.IsCopied)
      {
        foreach (var conditionsList in _obj.Conditions)
        {          
          var newCondition = ContractConditions.Copy(ContractConditions.As(conditionsList.Condition));
          newCondition.Save();
          conditionsList.Condition = newCondition;
        }
      }
    }

  }

}
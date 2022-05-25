using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractCondition;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Contracts.Server
{
  partial class ContractConditionFunctions
  {
    /// <summary>
    /// Создать договорное условие.
    /// </summary>
    /// <returns>Договорное условие.</returns>
    [Remote]
    public static IContractCondition CreateContractCondition()
    {
      return ContractConditions.Create();
    }
    
    public override string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == Sungero.Contracts.ContractCondition.ConditionType.Standard)
        {
          return ContractConditions.Resources.StandardFormContract;
        }
      }
      return base.GetConditionName();
    }
  }
}
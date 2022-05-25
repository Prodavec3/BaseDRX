using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Shared
{
  partial class ContractsApprovalRuleFunctions
  {

    public override string GetIncompatibleDocumentGroupsExcludedHint()
    {
      return ContractsApprovalRules.Resources.IncompatibleCategoriesExcluded;
    }
    
    public override List<Sungero.Docflow.IDocumentGroupBase> GetAvailableDocumentGroups()
    {
      var ruleKinds = _obj.DocumentKinds.Select(k => k.DocumentKind).ToList();
      return Functions.ContractCategory.GetFilteredContractCategoris(ruleKinds);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts
{
  partial class ContractsApprovalRuleClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (_obj.DocumentFlow == null)
        _obj.DocumentFlow = Docflow.ApprovalRuleBase.DocumentFlow.Contracts;
      
      _obj.State.Properties.DocumentFlow.IsEnabled = false;
      
      var availableDocumentGroups = Functions.ContractsApprovalRule.GetAvailableDocumentGroups(_obj);
      _obj.State.Properties.DocumentGroups.IsEnabled = availableDocumentGroups.Any();
      
      // Проверка возможности добавления блока условия на схему, при указанных видах документов.
      if (!e.Params.Contains(Constants.ContractsApprovalRule.IsSupportConditions))
        e.Params.Add(Constants.ContractsApprovalRule.IsSupportConditions, true);
      
      if (!_obj.DocumentKinds.Any())
        e.Params.AddOrUpdate(Constants.ContractsApprovalRule.IsSupportConditions, true);
      
      if (_obj.AccessRights.CanUpdate() && ContractConditions.AccessRights.CanCreate())
      {
        if (_obj.DocumentKinds.Any() && _obj.State.Properties.DocumentKinds.IsChanged)
        {
          var condition = Functions.ContractCondition.Remote.CreateContractCondition();
          var possibleConditions = Functions.ContractCondition.GetSupportedConditions(condition);
          
          e.Params.AddOrUpdate(Constants.ContractsApprovalRule.IsSupportConditions, _obj.DocumentKinds.Any(x => possibleConditions.ContainsKey(x.DocumentKind.DocumentType.DocumentTypeGuid)));
        }
      }
      else
      {
        e.Params.AddOrUpdate(Constants.ContractsApprovalRule.IsSupportConditions, false);
      }
    }
    
    public override IEnumerable<Enumeration> DocumentFlowFiltering(IEnumerable<Enumeration> query)
    {
      query = base.DocumentFlowFiltering(query);
      return query.Where(f => f == Docflow.ApprovalRuleBase.DocumentFlow.Contracts);
    }

  }
}
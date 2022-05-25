using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractsApprovalRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Client
{
  partial class ContractsApprovalRuleConditionsActions
  {
    public override void ChartConfigCondition(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {              
      IContractCondition condition;
      
      if (_obj.Condition == null)
        condition = Functions.ContractCondition.Remote.CreateContractCondition(); 
      else
        condition = _obj.Condition;        

      Docflow.PublicFunctions.ApprovalRuleBase.AddDocumentKindToCondition(_obj.ApprovalRuleBase, condition);
      
      condition.ShowModal();
      
      // TODO Belyak: баг платформы 32055. Убрать переполучение условия после исправления.
      if (!condition.State.IsInserted)
        condition = ContractConditions.As(Docflow.PublicFunctions.ConditionBase.Remote.GetCondition(condition.Id));
      
      if (!condition.State.IsInserted && !Equals(_obj.Condition, condition))
        _obj.Condition = condition;
    }

    public override bool CanChartConfigCondition(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {    
      return base.CanChartConfigCondition(e);
    }

  }

  partial class ContractsApprovalRuleActions
  {
    public override void CreateVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateVersion(e) && ContractsApprovalRules.AccessRights.CanCreate();
    }
    
    public override void ChartAddCondition(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChartAddCondition(e);
    }

    public override bool CanChartAddCondition(Sungero.Domain.Client.CanExecuteActionArgs e)
    {                 
      return true;
    }

    public override void ShowDuplicate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetDoubleRules(_obj);
      
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Docflow.ApprovalRuleBases.Resources.DuplicateNotFound);
    }

    public override bool CanShowDuplicate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowDuplicate(e);
    }

  }

}
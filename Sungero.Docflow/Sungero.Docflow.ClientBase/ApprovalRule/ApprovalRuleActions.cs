using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRule;

namespace Sungero.Docflow.Client
{
  partial class ApprovalRuleActions
  {
    public override void CreateVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateVersion(e) && ApprovalRules.AccessRights.CanCreate();
    }

    public override void ChartAddCondition(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChartAddCondition(e);
    }

    public override bool CanChartAddCondition(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

  partial class ApprovalRuleConditionsActions
  {
    public override void ChartConfigCondition(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {     
      ICondition condition;
      
      if (_obj.Condition == null)
        condition = Functions.Condition.Remote.CreateCondition();  
      else
        condition = _obj.Condition;        

      Functions.ApprovalRuleBase.AddDocumentKindToCondition(_obj.ApprovalRuleBase, condition);
      
      condition.ShowModal();
      
      // TODO Belyak: баг платформы 32055. Убрать переполучение условия после исправления.
      if (!condition.State.IsInserted)
        condition = Conditions.As(Functions.ConditionBase.Remote.GetCondition(condition.Id));
      
      if (!condition.State.IsInserted && !Equals(_obj.Condition, condition))
        _obj.Condition = condition;
    }

    public override bool CanChartConfigCondition(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanChartConfigCondition(e);
    }

  }

}
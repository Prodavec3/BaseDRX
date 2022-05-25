using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;

namespace Sungero.Docflow
{
  partial class ApprovalRuleBaseConditionsSharedCollectionHandlers
  {

    public virtual void ConditionsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = Functions.ApprovalRuleBase.GetNextNumber(_obj);
    }
  }

  partial class ApprovalRuleBaseTransitionsSharedCollectionHandlers
  {

    public virtual void TransitionsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.ConditionValue = null;
    }
  }

  partial class ApprovalRuleBaseSharedHandlers
  {

    public virtual void ReworkPerformerTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.ApprovalRuleBase.SetStateProperties(_obj);
      if (!Equals(e.NewValue, ApprovalRuleBase.ReworkPerformerType.EmployeeRole) && !Equals(e.NewValue, e.OldValue))
        _obj.ReworkPerformer = null;
      
      if (!Equals(e.NewValue, ApprovalRuleBase.ReworkPerformerType.ApprovalRole) && !Equals(e.NewValue, e.OldValue))
        _obj.ReworkApprovalRole = null;
    }
    
    public virtual void DocumentKindsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      var availableDocumentGroups = Functions.ApprovalRuleBase.GetAvailableDocumentGroups(_obj);
      var suitableDocumentGroups = _obj.DocumentGroups.Select(d => d.DocumentGroup).Where(dg => availableDocumentGroups.Contains(dg)).ToList();
      
      if (suitableDocumentGroups.Count < _obj.DocumentGroups.Count())
      {
        Functions.Module.TryToShowNotifyMessage(Functions.ApprovalRuleBase.GetIncompatibleDocumentGroupsExcludedHint(_obj));
        _obj.DocumentGroups.Clear();
        foreach (var documentGroup in suitableDocumentGroups)
          _obj.DocumentGroups.AddNew().DocumentGroup = documentGroup;
      }
      
      _obj.State.Properties.DocumentGroups.IsEnabled = availableDocumentGroups.Any();
      
    }

    public virtual void DocumentFlowChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue != null)
      {
        var suitableDocumentKinds = _obj.DocumentKinds.Where(dk => dk.DocumentKind.DocumentFlow == e.NewValue).Select(k => k.DocumentKind).ToList();
        
        if (suitableDocumentKinds.Count < _obj.DocumentKinds.Count())
        {
          Functions.Module.TryToShowNotifyMessage(ApprovalRuleBases.Resources.IncompatibleDocumentKindsExcluded);
          _obj.DocumentKinds.Clear();
          foreach (var documentKind in suitableDocumentKinds)
            _obj.DocumentKinds.AddNew().DocumentKind = documentKind;
        }
      }
    }
  }

  partial class ApprovalRuleBaseStagesSharedHandlers
  {

    public virtual void StagesStageBaseChanged(Sungero.Docflow.Shared.ApprovalRuleBaseStagesStageBaseChangedEventArgs e)
    {
      if (e.NewValue == null || !ApprovalStages.Is(e.NewValue))
        _obj.Stage = null;
      else if (!Equals(e.NewValue, _obj.Stage))
        _obj.Stage = ApprovalStages.As(e.NewValue);
      
      _obj.StageType = e.NewValue == null ? null : Functions.ApprovalStageBase.GetStageType(e.NewValue);
    }

    public virtual void StagesStageChanged(Sungero.Docflow.Shared.ApprovalRuleBaseStagesStageChangedEventArgs e)
    {
      if (!Equals(e.NewValue, _obj.StageBase) && e.NewValue != null)
        _obj.StageBase = e.NewValue;
      
      _obj.StageType = e.NewValue == null ? null : Functions.ApprovalStageBase.GetStageType(e.NewValue);
    }
  }

  partial class ApprovalRuleBaseStagesSharedCollectionHandlers
  {

    public virtual void StagesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = Functions.ApprovalRuleBase.GetNextNumber(_obj);
    }
  }
}
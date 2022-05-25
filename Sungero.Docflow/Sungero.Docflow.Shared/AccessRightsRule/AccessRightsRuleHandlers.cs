using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow
{
  partial class AccessRightsRuleSharedHandlers
  {

    public virtual void DocumentKindsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      var availableDocumentGroups = Functions.AccessRightsRule.GetDocumentGroups(_obj);
      var suitableDocumentGroups = _obj.DocumentGroups.Select(d => d.DocumentGroup).Where(dg => availableDocumentGroups.Contains(dg)).ToList();
      
      if (suitableDocumentGroups.Count < _obj.DocumentGroups.Count())
      {
        Functions.Module.TryToShowNotifyMessage(Contracts.ContractsApprovalRules.Resources.IncompatibleCategoriesExcluded);
        _obj.DocumentGroups.Clear();
        foreach (var documentGroup in suitableDocumentGroups)
          _obj.DocumentGroups.AddNew().DocumentGroup = documentGroup;
      }
      
      _obj.State.Properties.DocumentGroups.IsEnabled = availableDocumentGroups.Any();
    }
  }

}
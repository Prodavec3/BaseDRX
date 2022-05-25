using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccessRightsRule;

namespace Sungero.Docflow
{
  partial class AccessRightsRuleClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var availableDocumentGroups = Functions.AccessRightsRule.GetDocumentGroups(_obj);
      _obj.State.Properties.DocumentGroups.IsEnabled = availableDocumentGroups.Any();
    }
  }

}
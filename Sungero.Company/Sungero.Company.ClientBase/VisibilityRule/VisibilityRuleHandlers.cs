using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.VisibilityRule;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class VisibilityRuleClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (!PublicFunctions.Module.Remote.IsRecipientRestrictModeOn())
        e.AddWarning(Sungero.Company.VisibilityRules.Resources.RecipientRestrictModeOff);
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.DueDiligenceWebsite;

namespace Sungero.Parties.Client
{
  partial class DueDiligenceWebsiteActions
  {
    public virtual void OpenWebsite(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.GoToWebsite(_obj.HomeUrl.Trim(), e);
    }

    public virtual bool CanOpenWebsite(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !string.IsNullOrWhiteSpace(_obj.HomeUrl);
    }

    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {        
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.IsSystem.Value && base.CanDeleteEntity(e);
    }

  }

}
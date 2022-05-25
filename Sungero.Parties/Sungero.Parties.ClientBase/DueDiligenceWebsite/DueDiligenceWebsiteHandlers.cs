using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.DueDiligenceWebsite;

namespace Sungero.Parties
{
  partial class DueDiligenceWebsiteClientHandlers
  {    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Обработка системных сайтов.
      if (_obj.IsSystem  == true)
      {        
        // Блокировки от изменений.
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
        
        _obj.State.Properties.IsDefault.IsEnabled = true;
      }
      
      if (!Functions.DueDiligenceWebsite.Remote.IsDefaultDueDiligenceWebsiteSet())
        e.AddInformation(DueDiligenceWebsites.Resources.InfoSetDefaultDueDiligenceWebsite);
    }
  }

}
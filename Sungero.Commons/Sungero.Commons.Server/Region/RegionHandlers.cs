using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.Region;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class RegionServerHandlers
  {
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Проверить код региона на уникальность.
      if (!string.IsNullOrWhiteSpace(_obj.Code))
      {
        var region = Regions.GetAll().FirstOrDefault(r => r.Code == _obj.Code &&
                                                     r.Country.Equals(_obj.Country) &&
                                                     r.Status != CoreEntities.DatabookEntry.Status.Closed);
        if (region != null && !region.Equals(_obj))
          e.AddWarning(_obj.Info.Properties.Code, Regions.Resources.CodeDuplicateFormat(region.Name));
      }
    }

  }
}
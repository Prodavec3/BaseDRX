using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.ExternalEntityLink;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Commons
{
  partial class ExternalEntityLinkServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.IsDeleted = false;
    }
  }

}
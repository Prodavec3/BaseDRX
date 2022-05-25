using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MailDeliveryMethod;

namespace Sungero.Docflow
{
  partial class MailDeliveryMethodCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Sid);
    }
  }

  partial class MailDeliveryMethodServerHandlers
  {

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MailDeliveryMethod;

namespace Sungero.Docflow
{
  partial class MailDeliveryMethodClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      foreach (var property in _obj.State.Properties)
        property.IsEnabled = string.IsNullOrWhiteSpace(_obj.Sid);
    }

  }
}
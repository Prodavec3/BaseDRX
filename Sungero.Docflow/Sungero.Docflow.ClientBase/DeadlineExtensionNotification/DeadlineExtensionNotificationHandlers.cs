using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DeadlineExtensionNotification;

namespace Sungero.Docflow
{
  partial class DeadlineExtensionNotificationClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.PreviousDeadline.IsVisible = _obj.PreviousDeadline != null;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Agenda;

namespace Sungero.Meetings
{
  partial class AgendaClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var isCalledFromMeeting = CallContext.CalledFrom(Meetings.Info);
      if (isCalledFromMeeting)
        _obj.State.Properties.Meeting.IsEnabled = false;

      base.Refresh(e);
    }

  }
}
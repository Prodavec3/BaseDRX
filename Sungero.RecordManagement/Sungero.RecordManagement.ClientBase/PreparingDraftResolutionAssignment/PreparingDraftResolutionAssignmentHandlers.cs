using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.PreparingDraftResolutionAssignment;

namespace Sungero.RecordManagement
{
  partial class PreparingDraftResolutionAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Addressee.IsVisible = _obj.IsRework != true;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.IsRework == true)
        e.HideAction(_obj.Info.Actions.Forward);
    }

  }
}
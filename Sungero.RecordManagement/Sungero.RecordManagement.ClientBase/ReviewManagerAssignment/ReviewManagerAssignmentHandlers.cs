using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewManagerAssignment;

namespace Sungero.RecordManagement
{
  partial class ReviewManagerAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (_obj.Task.GetStartedSchemeVersion() == LayerSchemeVersions.V1)
        _obj.State.Properties.Addressee.IsVisible = false;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.Task.GetStartedSchemeVersion() == LayerSchemeVersions.V1)
        e.HideAction(_obj.Info.Actions.Forward);
    }
  }

}
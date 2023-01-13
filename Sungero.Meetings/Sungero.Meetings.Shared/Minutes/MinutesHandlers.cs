using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Minutes;

namespace Sungero.Meetings
{
  partial class MinutesSharedHandlers
  {

    public virtual void MeetingChanged(Sungero.Meetings.Shared.MinutesMeetingChangedEventArgs e)
    {
      FillName();
      Functions.Minutes.SetRequiredProperties(_obj);
      
      if (e.NewValue != null && e.NewValue.President != null && _obj.OurSignatory == null &&
          Docflow.PublicFunctions.OfficialDocument.Remote.CanSignByEmployee(_obj, e.NewValue.President))
        _obj.OurSignatory = e.NewValue.President;
    }
  }

}
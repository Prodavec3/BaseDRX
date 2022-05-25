using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Minutes;

namespace Sungero.Meetings
{

  partial class MinutesServerHandlers
  {

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      // Выдать права на документ участникам совещания.
      PublicFunctions.Meeting.SetAccessRightsOnDocument(_obj.Meeting, _obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (!_obj.State.IsCopied)
        _obj.Meeting = Sungero.Meetings.PublicFunctions.Meeting.GetContextMeeting();
    }
  }

}
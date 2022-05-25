using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceList;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceListClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var distinctParticipants = _obj.Participants.Select(r => r.Participant).Distinct();
      var distinctExcludedParticipants = _obj.ExcludedParticipants.Select(r => r.ExcludedParticipant).Distinct();
      if (distinctParticipants.Count() != _obj.Participants.Count() ||
          distinctExcludedParticipants.Count() != _obj.ExcludedParticipants.Count())
        e.AddWarning(AcquaintanceLists.Resources.DuplicatesWarning);
    }

  }
}
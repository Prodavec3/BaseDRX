using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceList;

namespace Sungero.RecordManagement.Shared
{
  partial class AcquaintanceListFunctions
  {
    /// <summary>
    /// Получить участников.
    /// </summary>
    /// <returns>Участники ознакомления.</returns>
    [Public]
    public virtual List<Company.IEmployee> GetParticipants()
    {
      var recipients = _obj.Participants.Select(x => x.Participant).ToList();
      var excludedRecipients = _obj.ExcludedParticipants.Select(x => x.ExcludedParticipant).ToList();
      
      var performers = Company.PublicFunctions.Module.GetNotSystemEmployees(recipients);
      var excludedPerformers = Company.PublicFunctions.Module.GetNotSystemEmployees(excludedRecipients);
      
      return performers.Except(excludedPerformers).ToList();
    }

  }
}
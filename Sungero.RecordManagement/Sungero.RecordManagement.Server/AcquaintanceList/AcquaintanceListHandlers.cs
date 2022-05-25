using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceList;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceListParticipantsParticipantPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ParticipantsParticipantFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)Functions.Module.ObserversFiltering(query);
    }
  }

  partial class AcquaintanceListServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Удалить дубли в списке участников, если они есть.
      var distinctAcquaintanceList = _obj.Participants.Select(r => r.Participant).ToList().Distinct();
      if (distinctAcquaintanceList.Count() != _obj.Participants.Count())
      {
        _obj.Participants.Clear();
        foreach (var participant in distinctAcquaintanceList)
        {
          var newRow = _obj.Participants.AddNew();
          newRow.Participant = participant;
        }
      }
      
      // Удалить дубли в поле "Кроме", если они есть.
      var distinctExcludedAcquaintanceList = _obj.ExcludedParticipants.Select(r => r.ExcludedParticipant).ToList().Distinct();
      if (distinctExcludedAcquaintanceList.Count() != _obj.ExcludedParticipants.Count())
      {
        _obj.ExcludedParticipants.Clear();
        foreach (var excludedParticipant in distinctExcludedAcquaintanceList)
        {
          var newRow = _obj.ExcludedParticipants.AddNew();
          newRow.ExcludedParticipant = excludedParticipant;
        }
      }
      
      // Предупредить о техническом ограничении платформы на запуск задачи для большого числа участников.
      var participants = _obj.Participants.Select(p => p.Participant).ToList();
      var excludedParticipants = _obj.ExcludedParticipants.Select(x => x.ExcludedParticipant).ToList();
      var performers = Functions.AcquaintanceTask.GetParticipants(participants, excludedParticipants);
      if (performers.Count() > Constants.AcquaintanceTask.PerformersLimit)
      {
        if (_obj.State.IsInserted)
        {
          e.AddError(AcquaintanceLists.Resources.TooManyParticipantsFormat(Constants.AcquaintanceTask.PerformersLimit));
          return;
        }
        
        e.AddWarning(AcquaintanceLists.Resources.TooManyParticipantsFormat(Constants.AcquaintanceTask.PerformersLimit));
      }
      
      // Проверить наличие участников ознакомления.
      if (performers.Count == 0)
        e.AddError(AcquaintanceTasks.Resources.PerformersCantBeEmpty);
    }
  }

}
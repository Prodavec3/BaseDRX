using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.CounterpartyConflictProcessingTask;

namespace Sungero.ExchangeCore.Server
{
  partial class CounterpartyConflictProcessingTaskFunctions
  {
    /// <summary>
    /// Создать задачу на обработку конфликтов синхронизации контрагентов.
    /// </summary>
    /// <param name="box">Абонентский ящик.</param>
    /// <param name="party">Организация из сервиса обмена.</param>
    /// <param name="parties">Список конфликтных контрагентов.</param>
    /// <returns>Задача на обработку конфликтов синхронизации контрагентов.</returns>
    public static ICounterpartyConflictProcessingTask Create(IBusinessUnitBox box,
                                                             NpoComputer.DCX.Common.Organization party, List<Parties.ICounterparty> parties)
    {
      var task = CounterpartyConflictProcessingTasks.Create();
      var dateWithUTC = Sungero.Docflow.PublicFunctions.Module.GetDateWithUTCLabel(Calendar.Now);
      var subject =
        CounterpartyConflictProcessingTasks.Resources.ConflictTaskSubjectFormat(box.BusinessUnit.Name, box.ExchangeService.Name, dateWithUTC);
      task.Subject = Exchange.PublicFunctions.Module.CutText(subject, task.Info.Properties.Subject.Length);
      task.ThreadSubject = CounterpartyConflictProcessingTasks.Resources.ConflictTaskThreadSubject;
      task.Assignee = box.Responsible;
      task.MaxDeadline = Calendar.Today.AddWorkingDays(task.Assignee, 2);
      
      if (parties.All(p => Parties.CompanyBases.Is(p) && Equals(Parties.CompanyBases.As(p).TRRC, party.Kpp)) ||
          parties.All(p => !Parties.CompanyBases.Is(p) && string.IsNullOrWhiteSpace(party.Kpp)))
      {
        task.ActiveText = CounterpartyConflictProcessingTasks.Resources.ConflictTaskMany;
        foreach (var attach in parties)
          task.ActiveText += string.Format("{0}{1}{2}", Environment.NewLine, Constants.BusinessUnitBox.Delimiter, Hyperlinks.Get(attach));
      }
      else
      {
        task.ActiveText =
          CounterpartyConflictProcessingTasks.Resources.ConflictTaskSingleFormat(party.Name, party.Inn, party.Kpp);
        if (!string.IsNullOrWhiteSpace(party.Ogrn))
          task.ActiveText += CounterpartyConflictProcessingTasks.Resources.ConflictTaskSingleOgrnFormat(Environment.NewLine, party.Ogrn);
      }
      
      foreach (var attach in parties)
        task.Attachments.Add(attach);
      
      task.Save();
      return task;
    }
  }
}
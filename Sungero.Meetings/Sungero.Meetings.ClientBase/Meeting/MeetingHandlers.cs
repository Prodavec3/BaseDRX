using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Meetings.Meeting;

namespace Sungero.Meetings
{
  partial class MeetingClientHandlers
  {

    public virtual void DurationValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      // Проверить корректность длительности.
      if (!e.NewValue.HasValue)
        return;
      if (e.NewValue.Value < 0)
        e.AddError(Meetings.Resources.DurationMustBePositive);
    }

    public virtual void DateTimeValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (!e.NewValue.HasValue)
        return;
      
      // Если время не задано, ставить начало рабочего дня.
      if (!e.NewValue.Value.HasTime())
        e.NewValue = e.NewValue.Value.BeginningOfWorkingDay();
      // Проверить корректность даты.
      else if (!e.NewValue.Value.IsWorkingDay(Users.Current))
        e.AddWarning(Meetings.Resources.MeetingDateIsWeekend);
      else if (!e.NewValue.Value.IsWorkingTime(Users.Current))
        e.AddWarning(Meetings.Resources.MeetingTimeIsWeekend);
    }

  }
}
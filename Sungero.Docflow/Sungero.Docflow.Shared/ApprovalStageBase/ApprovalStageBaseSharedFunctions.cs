using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStageBase;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalStageBaseFunctions
  {

    /// <summary>
    /// Получить тип этапа.
    /// </summary>
    /// <returns>Тип этапа.</returns>
    public virtual Enumeration? GetStageType()
    {
      return null;
    }
    
    /// <summary>
    /// Получить представление срока в этапе.
    /// </summary>
    /// <param name="performersCount">Количество исполнителей.</param>
    /// <param name="daysHoursSeparator">Разделитель дней и часов.</param>
    /// <param name="needHoursConvert">Конвертировать часы в дни.</param>
    /// <param name="isParallel">Признак параллельности исполнителей этапа.</param>
    /// <returns>Представление срока.</returns>
    public virtual string GetDeadlineDescription(int performersCount, string daysHoursSeparator, bool needHoursConvert, bool isParallel)
    {
      var deadline = string.Empty;
      int? days = null;
      int? hours = null;
      var hasDays = _obj.DeadlineInDays.HasValue && _obj.DeadlineInDays != 0;
      var hasHours = _obj.DeadlineInHours.HasValue && _obj.DeadlineInHours != 0;
      if (!hasDays && !hasHours)
        return deadline;

      if (hasDays)
        days = isParallel ? _obj.DeadlineInDays : (_obj.DeadlineInDays * performersCount);
      if (hasHours)
        hours = isParallel ? _obj.DeadlineInHours : (_obj.DeadlineInHours * performersCount);
      
      // Превращаем часы в дни.
      var hoursInDay = 8;
      if (needHoursConvert && hours >= hoursInDay)
      {
        if (!hasDays)
          days = 0;
        
        days += hours / hoursInDay;
        hours = hours % hoursInDay;
        
        if (hours == 0)
          hours = null;
      }
      
      if (days.HasValue)
        deadline = string.Format(" {0} {1}",
                                 days,
                                 Functions.Module.GetNumberDeclination(days.Value, Resources.StateViewDay, Resources.StateViewDayGenetive, Resources.StateViewDayPlural));
      if (hours.HasValue)
        deadline += string.Format("{0}{1} {2}",
                                  daysHoursSeparator,
                                  hours,
                                  Functions.Module.GetNumberDeclination(hours.Value, Resources.StateViewHour, Resources.StateViewHourGenetive, Resources.StateViewHourPlural));
      
      return deadline;
    }

  }
}
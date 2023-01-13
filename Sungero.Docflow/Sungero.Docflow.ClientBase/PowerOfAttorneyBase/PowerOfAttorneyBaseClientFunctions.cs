using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow.Client
{
  partial class PowerOfAttorneyBaseFunctions
  {
    /// <summary>
    /// Проверяет корректность дней для завершения относительно даты Действует по.
    /// </summary>
    /// <param name="validTill">Действует по.</param>
    /// <param name="daysToFinishWorks">Дней для завершения.</param>
    /// <returns>Текст ошибки.</returns>
    public static string CheckCorrectnessDaysToFinishWorks(DateTime? validTill, int? daysToFinishWorks)
    {
      if (daysToFinishWorks == null || validTill == null)
        return string.Empty;
      
      TimeSpan daysRange = validTill.Value - Calendar.UserToday;
      var maxDaysToFinish = daysRange.TotalDays;
      if (daysToFinishWorks <= maxDaysToFinish)
        return string.Empty;
      
      if (maxDaysToFinish > 0)
        return PowerOfAttorneyBases.Resources.DaysToFinishTooMatchFormat(maxDaysToFinish + 1);
      else
        return PowerOfAttorneyBases.Resources.DocumentAlreadyFinish;
    }
  }
}
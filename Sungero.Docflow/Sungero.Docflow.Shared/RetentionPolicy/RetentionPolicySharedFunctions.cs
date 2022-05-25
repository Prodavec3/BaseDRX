using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RetentionPolicy;

namespace Sungero.Docflow.Shared
{
  partial class RetentionPolicyFunctions
  {
    /// <summary>
    /// Установить обязательность, доступность свойств.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      var isCustomInterval = _obj.RepeatType != null && _obj.RepeatType.Value == Sungero.Docflow.RetentionPolicy.RepeatType.CustomInterval;
      var isJobStart = _obj.RepeatType != null && _obj.RepeatType.Value == Sungero.Docflow.RetentionPolicy.RepeatType.WhenJobStart;
      
      _obj.State.Properties.IntervalType.IsRequired = isCustomInterval;
      _obj.State.Properties.IntervalType.IsEnabled = isCustomInterval;
      
      _obj.State.Properties.Interval.IsRequired = isCustomInterval;
      _obj.State.Properties.Interval.IsEnabled = isCustomInterval;
      
      _obj.State.Properties.NextRetention.IsRequired = !isJobStart;
      _obj.State.Properties.NextRetention.IsEnabled = !isJobStart;
    }
    
    /// <summary>
    /// Получить дату следующего перемещения.
    /// </summary>
    /// <param name="repeatType">Тип повтора.</param>
    /// <param name="intervalType">Тип интервала.</param>
    /// <param name="interval">Интервал.</param>
    /// <param name="now">Текущая дата.</param>
    /// <returns>Дата следующего перемещения.</returns>
    [Public]
    public static DateTime? GetNextRetentionDate(Enumeration? repeatType, Enumeration? intervalType, int? interval, DateTime now)
    {
      if (repeatType == null || repeatType == Docflow.RetentionPolicy.RepeatType.WhenJobStart)
        return null;
      else if (repeatType == Docflow.RetentionPolicy.RepeatType.EveryDay)
        return now.AddDays(1);
      else if (repeatType == Docflow.RetentionPolicy.RepeatType.EveryWeek)
        return now.AddDays(7);
      else if (repeatType == Docflow.RetentionPolicy.RepeatType.EveryMonth)
        return now.AddMonths(1);
      else if (repeatType == Docflow.RetentionPolicy.RepeatType.CustomInterval)
      {
        if (intervalType == null || interval == null)
          return null;
        
        if (intervalType == Docflow.RetentionPolicy.IntervalType.Day)
          return now.AddDays(interval.Value);
        else if (intervalType == Docflow.RetentionPolicy.IntervalType.Month)
          return now.AddMonths(interval.Value);
      }
      
      return null;
    }
  }
}
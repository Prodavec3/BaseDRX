using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RetentionPolicy;

namespace Sungero.Docflow
{
  partial class RetentionPolicySharedHandlers
  {

    public virtual void IntervalChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
        _obj.NextRetention = Functions.RetentionPolicy.GetNextRetentionDate(_obj.RepeatType, _obj.IntervalType, e.NewValue, Calendar.Now);
    }

    public virtual void IntervalTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
        _obj.NextRetention = Functions.RetentionPolicy.GetNextRetentionDate(_obj.RepeatType, e.NewValue, _obj.Interval, Calendar.Now);
    }

    public virtual void RepeatTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        if (e.NewValue == null || e.NewValue.Value != RetentionPolicy.RepeatType.CustomInterval)
        {
          _obj.Interval = null;
          _obj.IntervalType = null;
        }
        
        if (e.NewValue != null && e.NewValue.Value == RetentionPolicy.RepeatType.WhenJobStart)
          _obj.NextRetention = null;
        
        _obj.NextRetention = Functions.RetentionPolicy.GetNextRetentionDate(e.NewValue, _obj.IntervalType, _obj.Interval, Calendar.Now);
      }
      
      Functions.RetentionPolicy.SetRequiredProperties(_obj);
    }

  }
}
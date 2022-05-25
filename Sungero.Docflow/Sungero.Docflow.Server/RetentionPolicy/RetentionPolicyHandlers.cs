using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RetentionPolicy;

namespace Sungero.Docflow
{
  partial class RetentionPolicyCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.LastRetention);
    }
  }

  partial class RetentionPolicyServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (!_obj.State.IsCopied)
        _obj.RepeatType = Sungero.Docflow.RetentionPolicy.RepeatType.WhenJobStart;
      else
        _obj.NextRetention = Functions.RetentionPolicy.GetNextRetentionDate(_obj.RepeatType, _obj.IntervalType, _obj.Interval, Calendar.Now);
    }
  }

}
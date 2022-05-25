using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewTaskStage;

namespace Sungero.Docflow
{
  partial class ApprovalReviewTaskStageSharedHandlers
  {
    public override void DeadlineInDaysChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      base.DeadlineInDaysChanged(e);
      _obj.State.Properties.DeadlineInHours.IsRequired = !e.NewValue.HasValue;
    }

    public override void DeadlineInHoursChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      base.DeadlineInHoursChanged(e);
      _obj.State.Properties.DeadlineInDays.IsRequired = !e.NewValue.HasValue;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewTaskStage;

namespace Sungero.Docflow
{
  partial class ApprovalReviewTaskStageClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      _obj.State.Properties.DeadlineInDays.IsRequired = !_obj.DeadlineInHours.HasValue;
      _obj.State.Properties.DeadlineInHours.IsRequired = !_obj.DeadlineInDays.HasValue;
    }

  }
}
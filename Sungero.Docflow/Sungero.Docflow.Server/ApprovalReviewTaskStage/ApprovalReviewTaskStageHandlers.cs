using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReviewTaskStage;

namespace Sungero.Docflow
{
  partial class ApprovalReviewTaskStageServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsCopied)
        return;
      
      _obj.WaitReviewTaskCompletion = false;
      _obj.TimeoutAction = ApprovalReviewTaskStage.TimeoutAction.Repeat;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      Functions.ApprovalStageBase.ValidateStageDeadline(_obj, e);
    }
  }

}
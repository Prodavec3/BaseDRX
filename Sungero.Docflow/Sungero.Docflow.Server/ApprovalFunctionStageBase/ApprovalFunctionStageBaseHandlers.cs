using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalFunctionStageBase;

namespace Sungero.Docflow
{
  partial class ApprovalFunctionStageBaseServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (!_obj.TimeoutInDays.HasValue && !_obj.TimeoutInHours.HasValue)
      {
        e.AddError(_obj.Info.Properties.TimeoutInDays, Sungero.Docflow.ApprovalFunctionStageBases.Resources.NeedSetStageTimeout, new[] { _obj.Info.Properties.TimeoutInDays, _obj.Info.Properties.TimeoutInHours });
        e.AddError(_obj.Info.Properties.TimeoutInHours, Sungero.Docflow.ApprovalFunctionStageBases.Resources.NeedSetStageTimeout, new[] { _obj.Info.Properties.TimeoutInDays, _obj.Info.Properties.TimeoutInHours });
      }
      
      // Общий срок ожидания выполнения этапа должен быть больше нуля.
      if ((_obj.TimeoutInDays ?? 0) + (_obj.TimeoutInHours ?? 0) == 0 && e.IsValid)
      {
        e.AddError(_obj.Info.Properties.TimeoutInDays, Sungero.Docflow.ApprovalFunctionStageBases.Resources.IncorrectHourTimeout, new[] { _obj.Info.Properties.TimeoutInDays, _obj.Info.Properties.TimeoutInHours });
        e.AddError(_obj.Info.Properties.TimeoutInHours, Sungero.Docflow.ApprovalFunctionStageBases.Resources.IncorrectHourTimeout, new[] { _obj.Info.Properties.TimeoutInDays, _obj.Info.Properties.TimeoutInHours });
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (_obj.State.IsCopied)
        return;
      
      _obj.TimeoutAction = Docflow.ApprovalFunctionStageBase.TimeoutAction.Skip;
    }
  }

}
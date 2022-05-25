using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceTask;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceTaskClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (Functions.AcquaintanceTask.NeedShowSignRecommendation(_obj, _obj.IsElectronicAcquaintance.Value, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault()))
        e.AddWarning(_obj.Info.Properties.IsElectronicAcquaintance, AcquaintanceTasks.Resources.RecommendApprovalSignature);
    }
    
    public virtual void IsElectronicAcquaintanceValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (Functions.AcquaintanceTask.NeedShowSignRecommendation(_obj, e.NewValue.Value, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault()))
        e.AddWarning(AcquaintanceTasks.Resources.RecommendApprovalSignature);
    }

    public virtual void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
    }

  }
}
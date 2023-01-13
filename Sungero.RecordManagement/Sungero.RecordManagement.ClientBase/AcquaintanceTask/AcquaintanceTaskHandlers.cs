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

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      // Скрывать действиe "Исключить из ознакомления" для задач, стартованных на ранних версиях схемы.
      var schemeSupportsExcludeFromAcquaintance = Functions.AcquaintanceTask.SchemeVersionSupportsExcludeFromAcquaintance(_obj);
      if (!schemeSupportsExcludeFromAcquaintance)
        e.HideAction(_obj.Info.Actions.ExcludeFromAcquaintance);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (Functions.AcquaintanceTask.NeedShowSignRecommendation(_obj, _obj.IsElectronicAcquaintance.Value, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault()))
        e.AddWarning(_obj.Info.Properties.IsElectronicAcquaintance, AcquaintanceTasks.Resources.RecommendApprovalSignature);
      
      if (_obj.Status != Workflow.Task.Status.Draft &&
          _obj.Status != Workflow.Task.Status.Aborted &&
          !Functions.AcquaintanceTask.HasDocumentAndCanRead(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
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
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DeadlineExtensionTask;

namespace Sungero.RecordManagement
{
  partial class DeadlineExtensionTaskClientHandlers
  {
    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      if (_obj.CurrentDeadline < Calendar.Now)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
          e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
      }
      else
      {
        // Новый срок поручения должен быть больше старого.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.CurrentDeadline))
          e.AddError(DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.Texts.IsVisible = false;
      
      // Изменить видимость причины, если задача стартована.
      _obj.State.Properties.PrimaryReason.IsVisible = _obj.Status.Value == Workflow.Task.Status.InProcess
        || _obj.Status.Value == Workflow.Task.Status.Completed
        || _obj.Status.Value == Workflow.Task.Status.Aborted;
      
      // Сделать недоступной для изменения первичную причину.
      _obj.State.Properties.PrimaryReason.IsEnabled = false;
    }
  }

}
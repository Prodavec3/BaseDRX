using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing
{
  partial class VerificationAssignmentClientHandlers
  {

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      e.Params.Remove(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      _obj.State.Properties.NewDeadline.IsEnabled = _obj.Addressee != null;
      _obj.State.Properties.NewDeadline.IsRequired = _obj.Addressee != null;
      var canRead = Functions.VerificationTask.HasDocumentAndCanRead(VerificationTasks.As(_obj.Task));
      _obj.State.Properties.NewDeadline.IsVisible = canRead;
      _obj.State.Properties.Addressee.IsVisible = canRead;

      e.Params.AddOrUpdate(Sungero.SmartProcessing.PublicConstants.VerificationAssignment.CanDeleteParamName,
                           Sungero.Docflow.OfficialDocuments.AccessRights.CanDelete());
    }

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      // Не давать указывать срок меньше, чем сейчас.
      if (e.NewValue.HasValue)
      {
        // Проводить валидацию на конец дня, если указана дата без времени.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee ?? Users.Current, e.NewValue.Value, Calendar.Now))
          e.AddError(VerificationAssignments.Resources.ImpossibleSpecifyDeadlineLessThenToday,
                     _obj.Info.Properties.NewDeadline);
        
        if (_obj.Addressee != null)
        {
          var checkMessageText = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(Users.Current, e.NewValue);
          if (!string.IsNullOrEmpty(checkMessageText))
            e.AddWarning(checkMessageText, _obj.Info.Properties.NewDeadline);
        }
      }
    }

    public virtual void AddresseeValueInput(Sungero.SmartProcessing.Client.VerificationAssignmentAddresseeValueInputEventArgs e)
    {
      if (e.NewValue != null && _obj.NewDeadline != null)
      {
        var checkMessageText = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(Users.Current, _obj.NewDeadline);
        if (!string.IsNullOrEmpty(checkMessageText))
          e.AddWarning(checkMessageText, _obj.Info.Properties.NewDeadline);
      }
      
      // При указании адресата заполнить срок: + 4 рабочих часа
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.Deadline != null)
        _obj.NewDeadline = _obj.Deadline.Value < Calendar.Now ?
          Calendar.Now.AddWorkingHours(e.NewValue, 4) :
          _obj.Deadline.Value.AddWorkingHours(e.NewValue, 4);
      if (e.NewValue == null)
        _obj.NewDeadline = null;
      
      _obj.State.Properties.NewDeadline.IsEnabled = e.NewValue != null;
      _obj.State.Properties.NewDeadline.IsRequired = e.NewValue != null;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingAssignment;

namespace Sungero.Exchange
{
  partial class ReceiptNotificationSendingAssignmentClientHandlers
  {

    public virtual void NewDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      // Не давать указывать срок меньше, чем сейчас.
      if (e.NewValue.HasValue)
      {
        // Проводить валидацию на конец дня, если указана дата без времени.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee ?? Users.Current, e.NewValue.Value, Calendar.Now))
          e.AddError(ExchangeDocumentProcessingAssignments.Resources.ImpossibleSpecifyDeadlineLessThenToday,
                     _obj.Info.Properties.NewDeadline);
      }
    }

    public virtual void AddresseeValueInput(Sungero.Exchange.Client.ReceiptNotificationSendingAssignmentAddresseeValueInputEventArgs e)
    {
      // При указании адресата заполнить срок: + 2 рабочих дня.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.NewDeadline == null)
        _obj.NewDeadline = _obj.Deadline.Value < Calendar.Now ?
          Calendar.Now.AddWorkingDays(e.NewValue, 2) : _obj.Deadline.Value.AddWorkingDays(e.NewValue, 2);
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingAssignment;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentProcessingAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var hasNoCurrentUserExchangeServiceCertificate = !Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.BusinessUnitBox);
      e.Params.AddOrUpdate(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate, hasNoCurrentUserExchangeServiceCertificate);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var hasNoCurrentUserExchangeServiceCertificate = false;
      if (e.Params.Contains(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate))
        e.Params.TryGetValue(Constants.ExchangeDocumentProcessingAssignment.HasNoCurrentUserExchangeServiceCertificate, out hasNoCurrentUserExchangeServiceCertificate);
      
      // Проверить, что у пользователя есть сертификат сервиса обмена, если задачу еще не переадресовывали.
      if (e.IsValid && ExchangeDocumentProcessingTasks.As(_obj.Task).Addressee == null && hasNoCurrentUserExchangeServiceCertificate)
        e.AddWarning(ExchangeDocumentProcessingAssignments.Resources.CertificateNotFound);
      _obj.State.Properties.NewDeadline.IsEnabled = _obj.Addressee != null;
    }

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

    public virtual void AddresseeValueInput(Sungero.Exchange.Client.ExchangeDocumentProcessingAssignmentAddresseeValueInputEventArgs e)
    {
      // При указании адресата заполнить срок: + 2 рабочих дня.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.NewDeadline == null)
        _obj.NewDeadline = _obj.Deadline.Value < Calendar.Now ?
          Calendar.Now.AddWorkingDays(e.NewValue, 2) :
          _obj.Deadline.Value.AddWorkingDays(e.NewValue, 2);
      if (e.NewValue == null)
        _obj.NewDeadline = null;
      _obj.State.Properties.NewDeadline.IsEnabled = e.NewValue != null;
    }

  }
}
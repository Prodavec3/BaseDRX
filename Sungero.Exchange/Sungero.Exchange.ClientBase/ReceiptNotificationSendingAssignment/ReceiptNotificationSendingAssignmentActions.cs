using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ReceiptNotificationSendingAssignment;

namespace Sungero.Exchange.Client
{
  partial class ReceiptNotificationSendingAssignmentActions
  {
    public virtual void Forwarded(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      // Не давать переадресовать, если адресат или срок не заполнены.
      if (_obj.Addressee == null || _obj.NewDeadline == null)
      {
        e.AddError(ExchangeDocumentProcessingAssignments.Resources.CantReAddressWithoutAddresseeAndDeadline);
        return;
      }
      
      // Не давать переадресовывать на срок меньше, чем сейчас.
      if (_obj.NewDeadline.HasValue)
      {
        // Проводить валидацию на конец дня, если указана дата без времени.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee, _obj.NewDeadline, Calendar.Now))
        {
          e.AddError(ExchangeDocumentProcessingAssignments.Resources.ImpossibleSpecifyDeadlineLessThenToday);
          return;
        }
      }
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.ReceiptNotificationSendingTask.ReceiptNotificationSendingAssignmentConfirmDialogID.Forwarded))
        e.Cancel();
      
      // Прокинуть новый срок и исполнителя в задачу.
      var task = ReceiptNotificationSendingTasks.As(_obj.Task);
      task.Addressee = _obj.Addressee;
      task.MaxDeadline = _obj.NewDeadline;
      task.Save();
    }

    public virtual bool CanForwarded(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void ShowDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Functions.Module.Remote.GetDocumentsWithoutReceiptNotification(_obj.Box);
      documents.Show();
    }

    public virtual bool CanShowDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.Module.HasCurrentUserExchangeServiceCertificate(_obj.Box))
      {
        e.AddError(Resources.CertificateNotFound);
        return;
      }
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                 Constants.ReceiptNotificationSendingTask.ReceiptNotificationSendingAssignmentConfirmDialogID.Complete))
        e.Cancel();
      
      Functions.ReceiptNotificationSendingAssignment.SendReceiptNotification(_obj, e);
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return _obj.Addressee == null;
    }

  }

}
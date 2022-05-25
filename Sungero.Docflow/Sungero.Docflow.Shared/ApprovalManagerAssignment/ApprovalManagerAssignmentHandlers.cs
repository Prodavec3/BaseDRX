using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalManagerAssignmentAddApproversSharedHandlers
  {

    public virtual void AddApproversApproverChanged(Sungero.Docflow.Shared.ApprovalManagerAssignmentAddApproversApproverChangedEventArgs e)
    {
      _obj.ApprovalManagerAssignment.State.Controls.Control.Refresh();
    }
  }

  partial class ApprovalManagerAssignmentSharedHandlers
  {

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.ApprovalManagerAssignmentAddresseeChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var firstAddressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (firstAddressee == null ||
          firstAddressee != null && !Equals(e.NewValue, firstAddressee.Addressee))
        Functions.ApprovalManagerAssignment.ClearAddresseesAndFillFirstAddressee(_obj);
    }

    public virtual void ExchangeServiceChanged(Sungero.Docflow.Shared.ApprovalManagerAssignmentExchangeServiceChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void DeliveryMethodChanged(Sungero.Docflow.Shared.ApprovalManagerAssignmentDeliveryMethodChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue == null || e.NewValue.Sid != Constants.MailDeliveryMethod.Exchange)
      {
        _obj.ExchangeService = null;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsRequired = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = true;
        _obj.State.Properties.ExchangeService.IsRequired = true;
        _obj.ExchangeService = Functions.ApprovalTask.Remote.GetExchangeServices(ApprovalTasks.As(_obj.Task)).DefaultService;
      }
      Functions.ApprovalManagerAssignment.UpdateDeliveryMethod(_obj);
      
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void AddApproversChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SignatoryChanged(Sungero.Docflow.Shared.ApprovalManagerAssignmentSignatoryChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }
  }
}
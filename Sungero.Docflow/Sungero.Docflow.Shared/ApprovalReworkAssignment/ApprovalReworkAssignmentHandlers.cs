using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalReworkAssignmentApproversSharedCollectionHandlers
  {

    public virtual void ApproversAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
      _added.Approved = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Approved.NotApproved;
      _added.IsRequiredApprover = false;
      if (_added.State.IsCopied == true)
        _added.Approver = null;
    }
  }

  partial class ApprovalReworkAssignmentSharedHandlers
  {

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentAddresseeChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var firstAddressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (firstAddressee == null ||
          firstAddressee != null && !Equals(e.NewValue, firstAddressee.Addressee))
        Functions.ApprovalReworkAssignment.ClearAddresseesAndFillFirstAddressee(_obj);
    }

    public virtual void ForwardPerformerChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentForwardPerformerChangedEventArgs e)
    {
      Functions.ApprovalReworkAssignment.UpdatePropertiesEnableState(_obj);
    }

    public virtual void ApproversChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      if (!_obj.Approvers.Any(a => a.Approver == null))
        _obj.State.Controls.Control.Refresh();
    }

    public virtual void ExchangeServiceChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentExchangeServiceChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void DeliveryMethodChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentDeliveryMethodChangedEventArgs e)
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
      Functions.ApprovalReworkAssignment.UpdateDeliveryMethod(_obj);
      
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SignatoryChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentSignatoryChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void AddApproversChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }
  }

  partial class ApprovalReworkAssignmentAddApproversSharedHandlers
  {

    public virtual void AddApproversApproverChanged(Sungero.Docflow.Shared.ApprovalReworkAssignmentAddApproversApproverChangedEventArgs e)
    {
      _obj.ApprovalReworkAssignment.State.Controls.Control.Refresh();
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalReworkAssignmentApproversClientHandlers
  {

    public virtual void ApproversApproverValueInput(Sungero.Docflow.Client.ApprovalReworkAssignmentApproversApproverValueInputEventArgs e)
    {
      if (Equals(e.OldValue, e.NewValue))
        return;

      // Запрещено изменять обязательных согласующих.
      if (_obj.IsRequiredApprover == true)
        e.AddError(ApprovalReworkAssignments.Resources.CannotChangeRquiredApprovers);
      else
      {
        _obj.Approved = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Approved.NotApproved;
        _obj.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
      }
      
      // Запрещено добавлять повторяющихся согласующих.
      if (_obj.ApprovalReworkAssignment.Approvers.Any(app => Equals(app.Approver, e.NewValue) && app.Id != _obj.Id))
        e.AddError(_obj.Info.Properties.Approver, ApprovalReworkAssignments.Resources.CantAddApproverTwice);
    }

    public virtual IEnumerable<Enumeration> ApproversActionFiltering(IEnumerable<Enumeration> query)
    {
      // Если согласующий еще не согласовал документ, то для выбора доступно только действие отправки задания.
      if (_obj.Approved == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Approved.NotApproved)
        return query.Where(q => q == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval);
      return query;
    }
  }

  partial class ApprovalReworkAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (!Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task)))
        e.HideAction(_obj.Info.Actions.Forward);
    }

    public virtual void DeliveryMethodValueInput(Sungero.Docflow.Client.ApprovalReworkAssignmentDeliveryMethodValueInputEventArgs e)
    {
      var task = ApprovalTasks.As(_obj.Task);
      if (e.NewValue != null && e.NewValue.Sid == Constants.MailDeliveryMethod.Exchange)
      {
        var services = Functions.ApprovalTask.Remote.GetExchangeServices(task).Services;
        if (!services.Any())
          e.AddError(ApprovalTasks.Resources.DeliveryByExchangeNotAllowed, e.Property);
        
        return;
      }
      
      Functions.ApprovalTask.ShowExchangeHint(task, _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, e.NewValue, e);
    }

    public virtual void AddresseeValueInput(Sungero.Docflow.Client.ApprovalReworkAssignmentAddresseeValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SignatoryValueInput(Sungero.Docflow.Client.ApprovalReworkAssignmentSignatoryValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Если в регламенте запрещен уменьшающийся круг рецензентов, то не даем изменять действие в гриде.
      if (ApprovalTasks.As(_obj.Task).ApprovalRule.IsSmallApprovalAllowed != true)
        _obj.State.Properties.Approvers.Properties.Action.IsEnabled = false;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var refreshParameters = Functions.ApprovalTask.Remote.GetFullStagesInfoForRefresh(ApprovalTasks.As(_obj.Task));

      _obj.State.Properties.Addressee.IsVisible = refreshParameters.AddresseeIsVisible;
      _obj.State.Properties.Addressee.IsRequired = refreshParameters.AddresseeIsRequired;
      _obj.State.Properties.Addressees.IsVisible = refreshParameters.AddresseesIsVisible;
      _obj.State.Properties.Addressees.IsRequired = refreshParameters.AddresseesIsRequired;

      _obj.State.Properties.Signatory.IsVisible = refreshParameters.SignatoryIsVisible;
      _obj.State.Properties.Signatory.IsRequired = refreshParameters.SignatoryIsRequired;
      _obj.State.Properties.AddApprovers.IsVisible = refreshParameters.AddApproversIsVisible;
      _obj.State.Properties.DeliveryMethod.IsVisible = refreshParameters.DeliveryMethodIsVisible;
      _obj.State.Properties.ExchangeService.IsVisible = refreshParameters.ExchangeServiceIsVisible;
      _obj.State.Properties.ForwardPerformer.IsVisible = Functions.ApprovalTask.SchemeVersionSupportsRework(ApprovalTasks.As(_obj.Task));
      
      Functions.ApprovalReworkAssignment.UpdatePropertiesEnableState(_obj);
      
      if (_obj.Status == Status.InProcess && !Functions.Module.IsLockedByOther(_obj) && _obj.AccessRights.CanUpdate())
        Functions.ApprovalTask.ShowExchangeHint(ApprovalTasks.As(_obj.Task), _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, _obj.DeliveryMethod, e);
      
      if (!_obj.DocumentGroup.OfficialDocuments.Any())
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
    }
  }
}
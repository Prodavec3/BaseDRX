using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalManagerAssignmentClientHandlers
  {

    public virtual void DeliveryMethodValueInput(Sungero.Docflow.Client.ApprovalManagerAssignmentDeliveryMethodValueInputEventArgs e)
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

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.ApprovalManagerAssignment.Remote.CreateParamsCache(_obj);
      
      bool hideActionWithSuggestions = false;
      if (e.Params.TryGetValue(Constants.ApprovalManagerAssignment.HideActionWithSuggestionsParamName, out hideActionWithSuggestions) && hideActionWithSuggestions)
        e.HideAction(_obj.Info.Actions.WithSuggestions);
    }

    public virtual void AddresseeValueInput(Sungero.Docflow.Client.ApprovalManagerAssignmentAddresseeValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void ApprovalRuleValueInput(Sungero.Docflow.Client.ApprovalManagerAssignmentApprovalRuleValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SignatoryValueInput(Sungero.Docflow.Client.ApprovalManagerAssignmentSignatoryValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      var refreshParameters = Functions.ApprovalTask.GetOrUpdateAssignmentRefreshParams(ApprovalTasks.As(_obj.Task), _obj, false);
      _obj.State.Properties.Addressee.IsVisible = refreshParameters.AddresseeIsVisible;
      _obj.State.Properties.Addressee.IsRequired = refreshParameters.AddresseeIsRequired;
      
      _obj.State.Properties.Addressees.IsVisible = refreshParameters.AddresseesIsVisible;
      _obj.State.Properties.Addressees.IsRequired = refreshParameters.AddresseesIsRequired;
      
      // Не давать менять адресата в согласовании служебных записок.
      if (Memos.Is(document))
      {
        _obj.State.Properties.Addressee.IsEnabled = false;
        _obj.State.Properties.Addressees.IsEnabled = false;
      }
      
      _obj.State.Properties.Signatory.IsVisible = refreshParameters.SignatoryIsVisible;
      _obj.State.Properties.Signatory.IsRequired = refreshParameters.SignatoryIsRequired;
      _obj.State.Properties.AddApprovers.IsVisible = refreshParameters.AddApproversIsVisible;
      _obj.State.Properties.DeliveryMethod.IsVisible = refreshParameters.DeliveryMethodIsVisible;
      _obj.State.Properties.ExchangeService.IsVisible = refreshParameters.ExchangeServiceIsVisible;
      
      Functions.ApprovalManagerAssignment.UpdateDeliveryMethod(_obj);
      
      _obj.State.Properties.Signatory.IsEnabled = _obj.ApprovalRule == null ? false : refreshParameters.SignatoryIsVisible && _obj.Status.Value == Workflow.AssignmentBase.Status.InProcess;

      if (_obj.Status == Status.InProcess && !Functions.Module.IsLockedByOther(_obj) && _obj.AccessRights.CanUpdate())
        Functions.ApprovalTask.ShowExchangeHint(ApprovalTasks.As(_obj.Task), _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, _obj.DeliveryMethod, e);
      
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(_obj.Task)))
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
      
      // Скрывать контрол состояния со сводкой, если сводка пустая.
      var needViewDocumentSummary = Functions.ApprovalManagerAssignment.NeedViewDocumentSummary(_obj);
      _obj.State.Controls.DocumentSummary.IsVisible = needViewDocumentSummary;
      
      bool canAddApprovers;
      if (e.Params.TryGetValue(Constants.ApprovalManagerAssignment.CanAddApprovers, out canAddApprovers))
        _obj.State.Properties.AddApprovers.IsEnabled = canAddApprovers;
      
      var reworkParameters = Functions.ApprovalTask.GetAssignmentReworkParameters(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);     
      _obj.State.Properties.ReworkPerformer.IsEnabled = reworkParameters.AllowChangeReworkPerformer;
      _obj.State.Properties.ReworkPerformer.IsVisible = reworkParameters.AllowViewReworkPerformer;
    }
  }
}
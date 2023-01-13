using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;

namespace Sungero.Docflow
{

  partial class ApprovalTaskClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      Functions.ApprovalTask.Remote.CreateParamsCache(_obj);
    }

    public virtual void DeliveryMethodValueInput(Sungero.Docflow.Client.ApprovalTaskDeliveryMethodValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Sid == Constants.MailDeliveryMethod.Exchange)
      {
        var services = Functions.ApprovalTask.Remote.GetExchangeServices(_obj).Services;
        if (!services.Any())
          e.AddError(ApprovalTasks.Resources.DeliveryByExchangeNotAllowed, e.Property);
        
        return;
      }
      
      Functions.ApprovalTask.ShowExchangeHint(_obj, _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, e.NewValue, e);
    }

    public virtual void AddresseeValueInput(Sungero.Docflow.Client.ApprovalTaskAddresseeValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void SignatoryValueInput(Sungero.Docflow.Client.ApprovalTaskSignatoryValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void ApprovalRuleValueInput(Sungero.Docflow.Client.ApprovalTaskApprovalRuleValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
      if (Functions.ApprovalTask.NeedShowExchangeHint(_obj, _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, _obj.DeliveryMethod, e.Params))
        e.AddInformation(_obj.Info.Properties.DeliveryMethod, Sungero.Docflow.ApprovalTasks.Resources.ExchangeDeliveryExist, _obj.Info.Properties.ApprovalRule);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ApprovalTask.RefreshApprovalTaskForm(_obj, false);
      // Обновить предметное отображение регламента.
      _obj.State.Controls.Control.Refresh();

      if (_obj.Status == Status.Draft && !Functions.Module.IsLockedByOther(_obj) && _obj.AccessRights.CanUpdate())
        Functions.ApprovalTask.ShowExchangeHint(_obj, _obj.State.Properties.DeliveryMethod, _obj.Info.Properties.DeliveryMethod, _obj.DeliveryMethod, e);
      
      if (_obj.Status != Workflow.Task.Status.Draft && 
          _obj.Status != Workflow.Task.Status.Aborted &&
          !Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
        e.AddError(Docflow.Resources.NoRightsToDocument);
    }
  }
}
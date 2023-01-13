using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalReworkAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalReworkAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      var task = ApprovalTasks.As(_obj.Task);
      var approvers = _obj.Approvers.Select(a => Recipients.As(a.Approver)).ToList();
      var reqApprovers = _obj.RegApprovers.Select(a => Recipients.As(a.Approver)).ToList();
      var addApprovers = approvers.Where(a => !reqApprovers.Contains(a)).ToList();
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(task, addApprovers, _obj.Signatory, _obj.Addressee, _obj.DeliveryMethod, _obj.ExchangeService);
    }
    
    #endregion
    
    /// <summary>
    /// Создать кеш параметров.
    /// </summary>
    [Remote(IsPure = true)]
    public virtual void CreateParamsCache()
    {
      var refreshParameters = Functions.ApprovalTask.GetOrUpdateAssignmentRefreshParams(ApprovalTasks.As(_obj.Task), _obj, true);
      
      Functions.ApprovalReworkAssignment.UpdateDeliveryMethod(_obj);
      
      // Необходимость показывать хинт о возможности отправки документа через СО.
      var lockInfo = Locks.GetLockInfo(_obj);
      if (_obj.Status == Status.InProcess && !(lockInfo != null && lockInfo.IsLockedByOther) && _obj.AccessRights.CanUpdate())
      {
        var isVisibleAndEnabled = (_obj.State.Properties.DeliveryMethod.IsVisible || refreshParameters.DeliveryMethodIsVisible) 
          && (_obj.State.Properties.DeliveryMethod.IsEnabled || refreshParameters.DeliveryMethodIsEnabled);
        if (isVisibleAndEnabled && (_obj.DeliveryMethod == null || _obj.DeliveryMethod.Sid != Constants.MailDeliveryMethod.Exchange))
        {
          var param = ((Domain.Shared.IExtendedEntity)_obj).Params;
          if (!param.ContainsKey(Constants.ApprovalTask.NeedShowExchangeServiceHint))
          {
            var show = Functions.ApprovalTask.GetExchangeServices(ApprovalTasks.As(_obj.Task)).DefaultService != null;
            param[Constants.ApprovalTask.NeedShowExchangeServiceHint] = show;
          }
        }
      }
    }
  }
}
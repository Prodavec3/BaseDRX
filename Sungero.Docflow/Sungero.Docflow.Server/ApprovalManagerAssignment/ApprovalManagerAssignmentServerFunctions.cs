using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalManagerAssignmentFunctions
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
      var approvers = _obj.AddApprovers.Select(a => a.Approver).ToList();
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(task, approvers, _obj.Signatory, _obj.Addressee, _obj.DeliveryMethod, _obj.ExchangeService);
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public StateView GetDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Docflow.PublicFunctions.Module.GetDocumentSummary(document);
    }
    
    #endregion
    
    /// <summary>
    /// Создать кеш параметров.
    /// </summary>
    [Remote(IsPure = true)]
    public virtual void CreateParamsCache()
    {
      var assignmentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      
      // Нельзя изменить состав доп. согласующих, если их этап идет до этапа руководителя.
      var canAddApprovers = Functions.ApprovalTask.CheckSequenceOfCoupleStages(ApprovalTasks.As(_obj.Task),
                                                                               Sungero.Docflow.ApprovalStage.StageType.Manager,
                                                                               Sungero.Docflow.ApprovalStage.StageType.Approvers, true);
      
      assignmentParams[Constants.ApprovalManagerAssignment.CanAddApprovers] = canAddApprovers;
      
      // Скрывать результат выполнения "Согласовать с замечаниями" для стартованных на ранних версиях схемы задач и в случаях, когда он отключен в настройках этапа.
      var schemeSupportsApproveWithSuggestions = Functions.ApprovalTask.SchemeVersionSupportsApproveWithSuggestions(ApprovalTasks.As(_obj.Task));
      var stageAllowsApproveWithSuggestions = Functions.ApprovalTask.GetApprovalWithSuggestionsParameter(ApprovalTasks.As(_obj.Task), _obj.StageNumber.Value);
      if (!schemeSupportsApproveWithSuggestions || !stageAllowsApproveWithSuggestions)
        assignmentParams[Constants.ApprovalManagerAssignment.HideActionWithSuggestionsParamName] = true;
      
      Functions.ApprovalTask.GetOrUpdateAssignmentRefreshParams(ApprovalTasks.As(_obj.Task), _obj, true);
      
      Functions.ApprovalManagerAssignment.UpdateDeliveryMethod(_obj);
      
      // Необходимость показывать хинт о возможности отправки документа через СО.
      var lockInfo = Locks.GetLockInfo(_obj);
      if (_obj.Status == Status.InProcess && !(lockInfo != null && lockInfo.IsLockedByOther) && _obj.AccessRights.CanUpdate())
      {
        var isVisibleAndEnabled = _obj.State.Properties.DeliveryMethod.IsVisible && _obj.State.Properties.DeliveryMethod.IsEnabled;
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
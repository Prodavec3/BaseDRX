using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow
{
  partial class ApprovalStageReworkApprovalRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkApprovalRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var withoutContractRoles = CallContext.CalledFrom(ApprovalRules.Info);
      var roles = Functions.ApprovalStage.GetSupportedApprovalRolesForRework(_obj, withoutContractRoles);
      query = query.Where(r => roles.Contains(r));
      return query;
    }
  }

  partial class ApprovalStageReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Для выбора доступны только сотрудники и одиночные роли.
      return query.Where(q => Company.Employees.Is(q) || Roles.Is(q) && Roles.As(q).IsSingleUser == true);
    }
  }

  partial class ApprovalStageAssigneePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AssigneeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(q => q.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Для выбора доступны только сотрудники и одиночные роли.
      return query.Where(q => Company.Employees.Is(q) || Roles.Is(q) && Roles.As(q).IsSingleUser == true);
    }
  }

  partial class ApprovalStageRecipientsRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RecipientsRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(c => c.Status == CoreEntities.DatabookEntry.Status.Active);
      
      // Если выбран тип этапа Согласование или Контроль возврата - не давать выбирать роль "все пользователи".
      if (_root != null && _root.StageType != null &&
          (_root.StageType == StageType.CheckReturn || _root.StageType == StageType.Approvers))
        query = query.Where(x => x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers);
      
      // Отфильтровать служебные роли.
      return (IQueryable<T>)RecordManagement.PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ApprovalStageApprovalRolesApprovalRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApprovalRolesApprovalRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var possibleRoles = Functions.ApprovalStage.GetPossibleRoles(_root);
      if (CallContext.CalledFrom(ApprovalRules.Info))
      {
        possibleRoles.Remove(Docflow.ApprovalRoleBase.Type.ContractResp);
        possibleRoles.Remove(Docflow.ApprovalRoleBase.Type.ContRespManager);
      }
      return query.Where(r => possibleRoles.Contains(r.Type));
    }
  }

  partial class ApprovalStageApprovalRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ApprovalRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var possibleRoles = Functions.ApprovalStage.GetPossibleRoles(_obj);
      if (CallContext.CalledFrom(ApprovalRules.Info))
      {
        possibleRoles.Remove(Docflow.ApprovalRoleBase.Type.ContractResp);
        possibleRoles.Remove(Docflow.ApprovalRoleBase.Type.ContRespManager);
      }
      return query.Where(r => possibleRoles.Contains(r.Type));
    }
  }

  partial class ApprovalStageServerHandlers
  {
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var isControlReturn = _obj.StageType == StageType.CheckReturn;
      var isSign = _obj.StageType == StageType.Sign;
      var isNotice = _obj.StageType == StageType.Notice;
      var isApprovers = _obj.StageType == StageType.Approvers;
      var isSimpleAgreement = _obj.StageType == StageType.SimpleAgr;

      #region Проверка корректности срока, общего срока и отсрочки старта задания
      
      // Проверить срок этапа для этапов кроме уведомления.
      if (!isNotice)
        Functions.ApprovalStageBase.ValidateStageDeadline(_obj, e);
      
      // Отсрочка старта не может превышать срок задания.
      if (isControlReturn && _obj.DeadlineInDays <= _obj.StartDelayDays)
      {
        e.AddError(_obj.Info.Properties.StartDelayDays, ApprovalStages.Resources.StartDelayMustBeLessDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.StartDelayDays });
        e.AddError(_obj.Info.Properties.DeadlineInDays, ApprovalStages.Resources.StartDelayMustBeLessDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.StartDelayDays });
      }
      
      #endregion
      
      var isMultiPerformers = isApprovers || isControlReturn || isSimpleAgreement || isNotice;
      if (isMultiPerformers && !_obj.ApprovalRoles.Any() && !_obj.Recipients.Any() && _obj.AllowAdditionalApprovers != true)
      {
        e.AddError(_obj.Info.Properties.ApprovalRoles, ApprovalStages.Resources.NeedSetStageApprovers, _obj.Info.Properties.ApprovalRoles);
        e.AddError(_obj.Info.Properties.Recipients, ApprovalStages.Resources.NeedSetStageApprovers, _obj.Info.Properties.Recipients);
      }
      
      // Проверка возможности использования роли во всех правилах, где встречается этот этап.
      if (_obj.State.Properties.ApprovalRole.IsChanged || _obj.State.Properties.ApprovalRoles.IsChanged || _obj.State.Properties.ReworkApprovalRole.IsChanged)
      {
        var rules = Functions.ApprovalStage.GetRulesWithImpossibleRoles(_obj);
        
        if (rules.Any())
          e.AddError(ApprovalStages.Resources.ImpossibleRole, _obj.Info.Actions.GetApprovalRulesWithImpossibleRoles);
      }

      // HACK, BUG 208989
      var approvalStageParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      approvalStageParams.Remove(Sungero.Docflow.Constants.ApprovalStage.HasRules);
      if (e.IsValid)
        approvalStageParams.Remove(Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites);
      
      // Добавить в параметры информацию о возможности регистрации документа в этапе регистрации.
      // Оптимизация для того, чтобы на refresh карточки не было лишнего запроса на то же самое.
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.ApprovalStage.CanRegister, Functions.ApprovalStage.ClerkCanRegister(_obj));
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (_obj.State.IsCopied)
        return;
      
      _obj.ReworkType = Sungero.Docflow.ApprovalStage.ReworkType.AfterAll;
      _obj.Sequence = Sungero.Docflow.ApprovalStage.Sequence.Parallel;
      _obj.AllowSendToRework = false;
      _obj.IsConfirmSigning = false;
      _obj.IsResultSubmission = false;
      _obj.AllowAdditionalApprovers = false;
      _obj.AllowChangeReworkPerformer = false;
      _obj.RightType = Sungero.Docflow.ApprovalStage.RightType.Edit;
      _obj.NeedRestrictPerformerRights = false;
      _obj.AllowApproveWithSuggestions = false;
      
      _obj.StageType = ApprovalStages.Info.Properties.StageType.GetFilter();
    }
  }
}
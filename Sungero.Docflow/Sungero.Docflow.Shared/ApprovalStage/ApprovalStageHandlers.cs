using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow
{

  partial class ApprovalStageSharedHandlers {

    public override void DeadlineInHoursChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      base.DeadlineInHoursChanged(e);
      _obj.State.Properties.DeadlineInDays.IsRequired = !e.NewValue.HasValue;
    }

    public override void DeadlineInDaysChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      base.DeadlineInDaysChanged(e);
      _obj.State.Properties.DeadlineInHours.IsRequired = !e.NewValue.HasValue && _obj.State.Properties.DeadlineInHours.IsEnabled;
    }
    
    public virtual void RightTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == RightType.Read)
        _obj.NeedRestrictPerformerRights = false;
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
    }
    
    public virtual void ReworkTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      if (e.NewValue != null && e.NewValue == ReworkType.AfterAll)
      {
        _obj.AllowChangeReworkPerformer = false;
      }
    }

    public virtual void ReworkPerformerTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      
      if (!Equals(e.NewValue, ApprovalStage.ReworkPerformerType.EmployeeRole) && !Equals(e.NewValue, e.OldValue))
        _obj.ReworkPerformer = null;
      
      if (!Equals(e.NewValue, ApprovalStage.ReworkPerformerType.ApprovalRole) && !Equals(e.NewValue, e.OldValue))
        _obj.ReworkApprovalRole = null;
    }
    
    public virtual void AssigneeTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue)
        return;
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      if (e.NewValue == null)
      {
        _obj.ApprovalRole = null;
        _obj.Assignee = null;
      }
      if (e.NewValue == AssigneeType.Role)
      {
        _obj.Assignee = null;
        Functions.ApprovalStage.SetDefaultRole(_obj);
      }
      if (e.NewValue == AssigneeType.Employee)
        _obj.ApprovalRole = null;
      
    }
    
    public virtual void IsResultSubmissionChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue.Value == e.OldValue)
        return;
      
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      
      _obj.AssigneeType = AssigneeType.Role;
      
      if (e.NewValue == true)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.AddrAssistant);
      else
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Addressee);
    }

    public virtual void IsConfirmSigningChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue.Value == e.OldValue)
        return;
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      
      _obj.AssigneeType = AssigneeType.Role;
      
      if (e.NewValue == true)
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.SignAssistant);
      else
        _obj.ApprovalRole = Functions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.Signatory);
    }
    
    public virtual void AssigneeChanged(Sungero.Docflow.Shared.ApprovalStageAssigneeChangedEventArgs e)
    {

    }

    public virtual void AllowSendToReworkChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue.Value == e.OldValue)
        return;
      
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      
      if (e.NewValue == true)
      {
        _obj.ReworkType = _obj.StageType == StageType.SimpleAgr ? _obj.ReworkType = ApprovalStage.ReworkType.AfterAll : _obj.ReworkType = ApprovalStage.ReworkType.AfterComplete;
        _obj.ReworkPerformerType = Docflow.ApprovalStage.ReworkPerformerType.FromRule;
      }
      else
      {
        _obj.ReworkType = null;
        _obj.ReworkPerformerType = null;
        _obj.ReworkPerformer = null;
        _obj.ReworkApprovalRole = null;
        _obj.AllowChangeReworkPerformer = false;
      }
    }

    public virtual void StageTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue)
        return;
      
      // Тип исполнителя по умолчанию Роль.
      _obj.AssigneeType = AssigneeType.Role;
      
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      
      #region Очистка скрытых свойств
      
      var properties = _obj.State.Properties;
      
      if (!properties.Assignee.IsVisible)
        _obj.Assignee = null;
      
      if (!properties.ApprovalRole.IsVisible)
        _obj.ApprovalRole = null;

      if (!properties.ApprovalRoles.IsVisible)
        _obj.ApprovalRoles.Clear();
      
      if (!properties.Sequence.IsVisible)
        _obj.Sequence = null;

      if (!properties.NeedStrongSign.IsVisible)
        _obj.NeedStrongSign = false;
      
      if (!properties.AllowSendToRework.IsVisible)
        _obj.AllowSendToRework = false;
      
      if (!properties.IsConfirmSigning.IsVisible)
        _obj.IsConfirmSigning = false;
      
      if (!properties.IsResultSubmission.IsVisible)
        _obj.IsResultSubmission = false;
      
      if (!properties.DeadlineInDays.IsVisible)
        _obj.DeadlineInDays = null;
      
      if (!properties.DeadlineInHours.IsVisible || !properties.DeadlineInHours.IsEnabled)
        _obj.DeadlineInHours = null;

      if (!properties.StartDelayDays.IsEnabled)
        _obj.StartDelayDays = null;
      
      if (!properties.ReworkType.IsVisible || !properties.ReworkType.IsEnabled)
        _obj.ReworkType = null;
      
      if (!properties.Subject.IsVisible)
        _obj.Subject = null;
      
      if (!properties.ReworkPerformerType.IsVisible || !properties.ReworkPerformerType.IsEnabled)
        _obj.ReworkPerformerType = null;
      
      if (!properties.ReworkPerformer.IsVisible || !properties.ReworkPerformer.IsEnabled)
        _obj.ReworkPerformer = null;
      
      if (!properties.ReworkApprovalRole.IsVisible)
        _obj.ReworkApprovalRole = null;
      
      if (!properties.AllowChangeReworkPerformer.IsVisible || !properties.AllowChangeReworkPerformer.IsEnabled)
        _obj.AllowChangeReworkPerformer = false;
      
      #endregion
      
      Functions.ApprovalStage.SetDefaultRole(_obj);
      
      // Установить тип подписи по умолчанию.
      if (e.NewValue == StageType.Approvers || e.NewValue == StageType.Manager || e.NewValue == StageType.Review)
        _obj.NeedStrongSign = false;
      else if (e.NewValue == StageType.Sign)
        _obj.NeedStrongSign = true;
      else
        _obj.NeedStrongSign = null;
      
      if (e.NewValue == StageType.Approvers)
        _obj.ReworkType = Docflow.ApprovalStage.ReworkType.AfterAll;
      
      if (e.NewValue == StageType.Approvers || e.NewValue == StageType.Manager || e.NewValue == StageType.Sign || e.NewValue == StageType.Review)
        _obj.ReworkPerformerType = Docflow.ApprovalStage.ReworkPerformerType.FromRule;
      
      // Для контроля возврата старт всегда параллельно, доработка после каждого.
      if (e.NewValue == StageType.CheckReturn)
      {
        _obj.Sequence = Sequence.Parallel;
        _obj.ReworkType = ReworkType.AfterEach;
      }
      
      // Для задания, печати, регистрации, отправки, создании поручений по умолчанию нет доработки.
      if (e.NewValue == StageType.SimpleAgr || e.NewValue == StageType.Print || e.NewValue == StageType.Register ||
          e.NewValue == StageType.Sending || e.NewValue == StageType.Execution)
        _obj.AllowSendToRework = false;
      
      // Для печати и уведомления права на просмотр.
      if (e.NewValue == StageType.Print || e.NewValue == StageType.Notice)
        _obj.RightType = Sungero.Docflow.ApprovalStage.RightType.Read;
      else
        _obj.RightType = Sungero.Docflow.ApprovalStage.RightType.Edit;
      
      // HACK, BUG 28505
      ((Domain.Shared.Validation.IValidationObject)_obj).ValidationResult.Clear();
      
      Functions.ApprovalStage.SetRequiredProperties(_obj);
    }
  }
}
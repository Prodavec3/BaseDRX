using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow
{
  partial class ApprovalRuleBaseReworkApprovalRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkApprovalRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var supportedRoles = Functions.ApprovalRuleBase.GetSupportedApprovalRolesForRework(_obj);
      return query.Where(r => supportedRoles.Contains(r));
    }
  }

  partial class ApprovalRuleBaseReworkPerformerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ReworkPerformerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Для выбора доступны только сотрудники и одиночные роли.
      return query.Where(q => Company.Employees.Is(q) || Roles.Is(q) && Roles.As(q).IsSingleUser == true);
    }
  }

  partial class ApprovalRuleBaseCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ParentRule);
      e.Without(_info.Properties.VersionNumber);
    }
  }

  partial class ApprovalRuleBaseDocumentGroupsDocumentGroupPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentGroupsDocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(g => Functions.ApprovalRuleBase.GetAvailableDocumentGroups(_root).Contains(g));
    }
  }

  partial class ApprovalRuleBaseDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var sendAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      if (_root.DocumentFlow != null)
        query = query.Where(d => d.DocumentFlow == _root.DocumentFlow);
      return query.Where(d => d.AvailableActions.Any(a => Equals(a.Action, sendAction)));
    }
  }

  partial class ApprovalRuleBaseServerHandlers
  {

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      if (_obj.Status == Docflow.ApprovalRuleBase.Status.Active)
      {
        var prevRule = Functions.ApprovalRuleBase.GetPreviousActiveRule(_obj);
        
        if (prevRule != null)
          prevRule.Status = Docflow.ApprovalRuleBase.Status.Closed;
      }
      
      if (_obj.State.Properties.Name.IsChanged && _obj.Status != Docflow.ApprovalRuleBase.Status.Closed)
      {
        var rules = Functions.ApprovalRuleBase.GetAllRuleVersions(_obj).Where(x => !Equals(x.Name, _obj.Name));
        
        foreach (var rule in rules)
          rule.Name = _obj.Name;
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      _obj.Priority = 0;
      if (_obj.BusinessUnits.Any())
        _obj.Priority += 8;
      if (_obj.DocumentKinds.Any())
        _obj.Priority += 4;
      if (_obj.Departments.Any())
        _obj.Priority += 2;
      if (_obj.DocumentGroups.Any())
        _obj.Priority += 1;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Status = Docflow.ApprovalRuleBase.Status.Draft;
      _obj.VersionNumber = 1;
      
      if (!_obj.IsSmallApprovalAllowed.HasValue)
        _obj.IsSmallApprovalAllowed = true;
      
      if (!_obj.NeedRestrictInitiatorRights.HasValue)
        _obj.NeedRestrictInitiatorRights = false;
      
      // Правило по умолчанию создаются только в инициализации.
      _obj.IsDefaultRule = false;
      
      if (!_obj.ReworkPerformerType.HasValue) 
        _obj.ReworkPerformerType = ApprovalRuleBase.ReworkPerformerType.Author;
      if (!_obj.ReworkDeadline.HasValue)
        _obj.ReworkDeadline = Sungero.Docflow.Constants.ApprovalRuleBase.ReworkDeadline;
      
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Equals(_obj.Status, Docflow.ApprovalRuleBase.Status.Closed))
        return;
      
      // Проверка на уже сохраненную версию правила.
      if (!Functions.ApprovalRuleBase.IsVersionUnique(_obj))
        e.AddError(ApprovalRuleBases.Resources.NewVersionNotAllowed, _obj.Info.Actions.ShowAllVersions);
      
      if (!_obj.Stages.Any())
      {
        e.AddError(ApprovalRuleBases.Resources.RuleMustHaveAtLeastOneStage);
        return;
      }
      
      var blocksError = false;
      
      // Валидация блоков этапов.
      foreach (var approvalStage in _obj.Stages)
      {
        if (approvalStage.Stage == null && approvalStage.StageBase == null)
        {
          e.AddError(approvalStage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, ApprovalRuleBases.Resources.ConfigStagesOnSchema);

          blocksError = true;
        }
      }
      
      // Валидация блоков условий.
      foreach (var approvalCondition in _obj.Conditions)
      {
        if (approvalCondition.Condition == null)
        {
          e.AddError(approvalCondition, ApprovalRuleBases.Info.Properties.Conditions.Properties.Condition, ApprovalRuleBases.Resources.ConfigConditionsOnSchema);

          blocksError = true;
        }
      }
      
      if (_obj.Stages.All(x => x.StageType == ApprovalRuleBaseStages.StageType.Function))
      {
        e.AddError(Sungero.Docflow.ApprovalRuleBases.Resources.RuleMustHaveOtherStages);
        blocksError = true;
      }
      
      if (blocksError)
        return;
      
      // Проверка применимости условий в правиле к выбранному списку документов.
      var sendAction = Functions.Module.GetSendAction(OfficialDocuments.Info.Actions.SendForApproval);
      var documentKinds = _obj.DocumentKinds.Any() ? _obj.DocumentKinds.Select(d => d.DocumentKind).ToList() :
        Docflow.DocumentKinds.GetAll().Where(d => Equals(d.DocumentFlow, _obj.DocumentFlow) && d.AvailableActions.Any(a => Equals(a.Action, sendAction))).ToList();
      if (_obj.Conditions.Any())
      {
        foreach (var condition in _obj.Conditions)
        {
          var possibleConditions = Functions.ConditionBase.GetSupportedConditions(condition.Condition);
          var notUseCondition = documentKinds.Any(k => !Functions.ConditionBase.CheckConditionAbility(condition.Condition, k, possibleConditions));
          
          if (notUseCondition)
          {
            var errorText = ApprovalRuleBases.Resources.ConditionNotSupportedByThisDocumentFormat(
              condition.Condition.Info.Properties.ConditionType.GetLocalizedValue(condition.Condition.ConditionType));
            
            e.AddError(condition, ApprovalRuleBases.Info.Properties.Conditions.Properties.Condition, errorText);
          }
          if (condition.Condition.ConditionType == ConditionBase.ConditionType.DocumentKind)
          {
            var availableDocumentKinds = Functions.ApprovalRuleBase.GetAvailableDocumentKinds(_obj);
            var isError = condition.Condition.ConditionDocumentKinds.Any(k => !availableDocumentKinds.Contains(k.DocumentKind));
            if (condition.Condition.ConditionDocumentKinds.Any(k => !availableDocumentKinds.Contains(k.DocumentKind)))
            {
              var conditionName = condition.Condition.Info.Properties.ConditionType.GetLocalizedValue(condition.Condition.ConditionType);
              var errorText = _obj.DocumentKinds.Any() ?
                ApprovalRuleBases.Resources.ConditionNotAvaliableDocumentKindFormat(conditionName) :
                ApprovalRuleBases.Resources.ConditionNotAvaliableDocumentFlowFormat(conditionName);
              e.AddError(condition, ApprovalRuleBases.Info.Properties.Conditions.Properties.Condition, errorText);
            }
          }
        }
      }
      
      // Проверка применимости ролей к выбранным видам.
      if (_obj.Stages.Any())
      {
        var roles = _obj.Stages.Where(s => s.Stage != null)
          .Select(s => s.Stage.ApprovalRole)
          .Where(r => r != null)
          .ToList();
        roles.AddRange(_obj.Stages.Where(s => s.Stage != null).SelectMany(s => s.Stage.ApprovalRoles.Select(r => r.ApprovalRole)));
        if (_obj.ReworkApprovalRole != null)
          roles.Add(_obj.ReworkApprovalRole);
        roles = roles.Distinct().ToList();
        
        foreach (var role in roles)
        {
          if (!Functions.ApprovalRoleBase.SupportDocumentKinds(role, documentKinds))
          {
            var error = ApprovalRuleBases.Resources.StageNotSupportedByThisDocumentFormat(role.Info.Properties.Type.GetLocalizedValue(role.Type));
            var stagesWithProjectRole = _obj.Stages.Where(s => s.Stage != null).Where(s => Functions.ApprovalStage.HasRole(s.Stage, role.Type)).ToList();
            foreach (var stage in stagesWithProjectRole)
              e.AddError(stage, ApprovalRuleBases.Info.Properties.Stages.Properties.Stage, error);
            
            if (!stagesWithProjectRole.Any() && Equals(role, _obj.ReworkApprovalRole))
              e.AddError(ApprovalRuleBases.Info.Properties.ReworkApprovalRole, error);
          }
        }
      }
      
      // Если коллекции изменились, а по данному правилу есть задачи в работе
      if ((_obj.State.Properties.Stages.IsChanged ||
           _obj.State.Properties.Conditions.IsChanged ||
           _obj.State.Properties.Transitions.IsChanged) &&
          Functions.ApprovalRuleBase.HasTasksInProcess(_obj))
        e.AddError(ApprovalRuleBases.Resources.RuleHasTasksInProcess, _obj.Info.Actions.ShowActiveTasks);
      
      // Добавить в параметры информацию о возможности регистрации документа в этапе регистрации.
      // Оптимизация для того, чтобы на refresh карточки не было лишнего запроса на то же самое.
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.ApprovalRuleBase.HintsInfoParam, Functions.ApprovalRuleBase.CanRegisterAndHasTaskInProcess(_obj));

      // Отфильтровать строки с пустыми этапами и типами.
      var stages = _obj.Stages.Where(st => st.Stage != null && st.StageType != null).ToList();
      
      // Проверить уникальность номеров в правиле.
      var stageNumbers = new List<int>();
      foreach (var stage in stages.Where(st => st.Number.HasValue))
      {
        if (stageNumbers.Contains(stage.Number.Value))
          e.AddError(ApprovalRuleBases.Resources.StageNumbersMustBeUnique);
        else
          stageNumbers.Add(stage.Number.Value);
      }
      
      var stagesVariants = Functions.ApprovalRuleBase.GetAllStagesVariants(_obj);
      Functions.ApprovalRuleBase.CheckRuleStages(_obj, stagesVariants, e);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRuleBase;

namespace Sungero.Docflow.Shared
{
  public partial class ApprovalRuleBaseFunctions
  {
    /// <summary>
    /// Получение номера для нового этапа или условия.
    /// </summary>
    /// <returns>Номер.</returns>
    public int GetNextNumber()
    {
      var stageNumber = _obj.Stages.Select(x => x.Number).Union(_obj.Conditions.Select(x => x.Number)).Max() ?? 0;
      
      return stageNumber + 1;
    }
    
    /// <summary>
    /// Определить условия.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Список условий по задаче.</returns>
    public virtual List<Sungero.Docflow.IApprovalRuleBaseConditions> GetConditions(IOfficialDocument document, IApprovalTask task)
    {
      var conditions = new List<Sungero.Docflow.IApprovalRuleBaseConditions>() { };
      if (_obj == null || !_obj.Conditions.Any())
        return conditions;
      // Вычисление первого этапа.
      var nextStageNumber = _obj.Transitions.Select(x => x.SourceStage).FirstOrDefault(s => !_obj.Transitions.Any(t => t.TargetStage.Equals(s)));
      
      if (nextStageNumber == null)
        return conditions;
      
      while (nextStageNumber != null && nextStageNumber >= 0)
      {
        var currentStageNumber = nextStageNumber;
        bool? conditionBranch = null;
        if (_obj.Conditions.Any(x => x.Number == currentStageNumber))
        {
          var condition = _obj.Conditions.Single(s => s.Number == currentStageNumber);
          conditions.Add(condition);
          var conditionResult = Functions.ConditionBase.CheckCondition(condition.Condition, document, task);
          conditionBranch = conditionResult != null ? conditionResult.Branch : null;
        }
        
        var nextTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == currentStageNumber && t.ConditionValue == conditionBranch);
        
        nextStageNumber = nextTransition != null ? nextTransition.TargetStage : null;
      }
      
      return conditions;
    }
    
    /// <summary>
    /// Определить номер следующего этапа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="currentStageNumber">Текущий номер этапа.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>
    /// Номер следующего этапа.
    /// -1 - если невозможно определить условие.
    /// -2 - если переход по условию в конец схемы.
    /// </returns>
    public virtual Structures.ApprovalRuleBase.NextStageNumber GetNextStageNumber(IOfficialDocument document, int? currentStageNumber, IApprovalTask task)
    {
      if (_obj.Transitions.Any())
      {
        if (currentStageNumber == null)
        {
          // Вычисление первого этапа.
          currentStageNumber = _obj.Transitions.Select(x => x.SourceStage).FirstOrDefault(s => !_obj.Transitions.Any(t => t.TargetStage == s));
          
          // Если этот этап условный, находим следующий обычный этап.
          if (!_obj.Stages.Any(s => s.Number == currentStageNumber))
            return this.GetNextStageNumber(document, currentStageNumber, task);
          
          return Structures.ApprovalRuleBase.NextStageNumber.Create(currentStageNumber, string.Empty);
        }
        
        var nextTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == currentStageNumber);
        
        if (nextTransition != null)
        {
          var condition = _obj.Conditions.FirstOrDefault(s => s.Number == nextTransition.SourceStage);
          
          if (condition != null)
          {
            var conditionResult = Functions.ConditionBase.CheckCondition(condition.Condition, document, task);
            
            // Если невозможно определить условие, то прерываем процесс и возвращаем -1 и текст ошибки.
            if (conditionResult.Branch == null)
              return Structures.ApprovalRuleBase.NextStageNumber.Create(-1, conditionResult.Message);

            nextTransition = _obj.Transitions.FirstOrDefault(t => t.SourceStage == currentStageNumber && t.ConditionValue == conditionResult.Branch);
            
            // Если по условию переход в конец.
            if (nextTransition == null)
              return Structures.ApprovalRuleBase.NextStageNumber.Create(-2, string.Empty);
          }

          var isStage = _obj.Stages.Any(s => s.Number == nextTransition.TargetStage);
          if (!isStage)
            return this.GetNextStageNumber(document, nextTransition.TargetStage.Value, task);
          
          return nextTransition != null ?
            Structures.ApprovalRuleBase.NextStageNumber.Create(nextTransition.TargetStage, string.Empty) :
            Structures.ApprovalRuleBase.NextStageNumber.Create(null, string.Empty);
        }
      }
      else if (_obj.Stages.Count == 1)
      {
        var singleNumber = _obj.Stages.Single().Number;
        return Structures.ApprovalRuleBase.NextStageNumber.Create(singleNumber == currentStageNumber ? null : singleNumber, string.Empty);
      }
      
      return Structures.ApprovalRuleBase.NextStageNumber.Create(null, string.Empty);
    }
    
    /// <summary>
    /// Получить текст уведомления о несовместимых группах.
    /// </summary>
    /// <returns>Текст.</returns>
    public virtual string GetIncompatibleDocumentGroupsExcludedHint()
    {
      return ApprovalRuleBases.Resources.IncompatibleDocumentGroupsExcluded;
    }
    
    /// <summary>
    /// Получить список групп документов, доступных для выбора в правиле.
    /// </summary>
    /// <returns>Список групп документов.</returns>
    public virtual List<IDocumentGroupBase> GetAvailableDocumentGroups()
    {
      return DocumentGroupBases.GetAllCached().ToList();
    }

    /// <summary>
    /// Определять, содержит ли правило этап согласования.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <param name="document">Документ.</param>
    /// <param name="stages">Этапы согласования в правильном порядке.</param>
    /// <returns>True, если содержит, иначе false.</returns>
    public bool HasApprovalStage(Enumeration stage, IOfficialDocument document, List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var fullStages = new List<Structures.Module.DefinedApprovalBaseStageLite>() { };
      foreach (var approvalStage in stages)
        fullStages.Add(Structures.Module.DefinedApprovalBaseStageLite.Create(approvalStage.Stage, approvalStage.Number, approvalStage.StageType));
      
      return this.HasApprovalStage(stage, document, fullStages);
    }
    
    /// <summary>
    /// Определять, содержит ли правило базовый этап согласования.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <param name="document">Документ.</param>
    /// <param name="stages">Базовые этапы согласования в правильном порядке.</param>
    /// <returns>True, если содержит, иначе false.</returns>
    public bool HasApprovalStage(Enumeration stage, IOfficialDocument document, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      if (_obj == null)
        return false;
      
      var hasStage = stages.Where(s => s.StageType == stage).Any();
      
      return hasStage;
    }
    
    /// <summary>
    /// Определять, содержит ли правило этап отправки на рассмотрение.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="stages">Базовые этапы согласования в правильном порядке.</param>
    /// <returns>True, если содержит, иначе false.</returns>
    public bool HasApprovalReviewTaskStage(IOfficialDocument document, List<Structures.Module.DefinedApprovalBaseStageLite> stages)
    {
      if (_obj == null)
        return false;
      
      var hasStage = stages
        .Where(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Function &&
               ApprovalReviewTaskStages.Is(s.StageBase))
        .Any();
      
      return hasStage;
    }
    
    /// <summary>
    /// Определять, содержит ли правило условие.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <param name="condition">Тип условия.</param>
    /// <returns>True, если содержит, иначе false.</returns>
    public bool HasApprovalCondition(IOfficialDocument document, IApprovalTask task, Enumeration condition)
    {
      if (_obj == null)
        return false;
      var conditions = this.GetConditions(document, task);
      
      return conditions.Any(c => c.Condition.ConditionType == condition);
    }
    
    /// <summary>
    /// Определить наличие указанной роли согласования в списке условий правила согласования.
    /// </summary>
    /// <param name="conditions">Список условий правила согласования.</param>
    /// <param name="approvalRoleType">Тип роли согласования.</param>
    /// <returns>True, если содержит, иначе false.</returns>
    public bool HasApprovalConditionWithRole(List<IApprovalRuleBaseConditions> conditions, Enumeration approvalRoleType)
    {
      return conditions.Any(c => c.Condition.ApprovalRole != null && Equals(c.Condition.ApprovalRole.Type, approvalRoleType) ||
                            c.Condition.ApprovalRoleForComparison != null && Equals(c.Condition.ApprovalRoleForComparison.Type, approvalRoleType));
    }
    
    /// <summary>
    /// Создание переходов по умолчанию.
    /// </summary>
    /// <param name="rule">Правило согласования.</param>
    [Public]
    public static void CreateAutoTransitions(IApprovalRuleBase rule)
    {
      rule.Transitions.Clear();

      foreach (var stage in rule.Stages.OrderBy(x => x.Number))
      {
        var nextStage = rule.Stages.Where(s => s.Number > stage.Number).Min(s => s.Number);
        if (nextStage != null)
        {
          var transition = rule.Transitions.AddNew();
          transition.SourceStage = stage.Number;
          transition.TargetStage = nextStage;
        }
      }
    }
    
    /// <summary>
    /// Заполнить виды документов условия выбранными видами в правиле.
    /// </summary>
    /// <param name="condition">Условие.</param>
    [Public]
    public virtual void AddDocumentKindToCondition(IConditionBase condition)
    {
      if (condition != null && condition.AccessRights.CanUpdate())
      {
        condition.DocumentKinds.Clear();
        
        foreach (var documentKind in this.GetAvailableDocumentKinds())
        {
          var newDocumentKind = condition.DocumentKinds.AddNew();
          newDocumentKind.DocumentKind = documentKind;
        }
        
        if (!condition.State.IsInserted)
          condition.Save();
      }
    }
    
    /// <summary>
    /// Получить доступные виды документов.
    /// </summary>
    /// <returns>Виды документов.</returns>
    [Obsolete("Используйте метод GetAvailableDocumentKinds().")]
    public virtual List<IDocumentKind> GetAvaliableDocumentKinds()
    {
      return this.GetAvailableDocumentKinds();
    }
    
    /// <summary>
    /// Получить доступные виды документов.
    /// </summary>
    /// <returns>Виды документов.</returns>
    public virtual List<IDocumentKind> GetAvailableDocumentKinds()
    {
      var availableDocumentKinds = new List<IDocumentKind>();
      
      if (_obj.DocumentKinds.Any())
        availableDocumentKinds.AddRange(_obj.DocumentKinds.Select(d => d.DocumentKind));
      else if (_obj.DocumentFlow != null)
      {
        availableDocumentKinds.AddRange(Functions.DocumentKind.Remote.GetAllDocumentKinds().Where(k => Equals(k.DocumentFlow, _obj.DocumentFlow)).ToList());
      }
      
      return availableDocumentKinds;
    }
    
    /// <summary>
    /// Установить обязательность, доступность, видимость свойств.
    /// </summary>
    public virtual void SetStateProperties()
    {
      var isReworkPerformerEnabled = _obj.ReworkPerformerType == Sungero.Docflow.ApprovalRuleBase.ReworkPerformerType.EmployeeRole;
      var isApprovalRoleEnabled = _obj.ReworkPerformerType == Sungero.Docflow.ApprovalRuleBase.ReworkPerformerType.ApprovalRole;
      
      _obj.State.Properties.ReworkPerformer.IsRequired = isReworkPerformerEnabled;
      _obj.State.Properties.ReworkPerformer.IsEnabled = isReworkPerformerEnabled;
      _obj.State.Properties.ReworkPerformer.IsVisible = !isApprovalRoleEnabled;
      
      _obj.State.Properties.ReworkApprovalRole.IsRequired = isApprovalRoleEnabled;
      _obj.State.Properties.ReworkApprovalRole.IsEnabled = isApprovalRoleEnabled;
      _obj.State.Properties.ReworkApprovalRole.IsVisible = isApprovalRoleEnabled;
    }
    
    /// <summary>
    /// Преобразовать базовый этап в этап согласования.
    /// </summary>
    /// <param name="stage">Базовый этап согласования.</param>
    /// <returns>Этап согласования. Null, если базовый этап не является этапом согласования.</returns>
    public static Structures.Module.DefinedApprovalStageLite CastToDefinedApprovalStageLite(Structures.Module.DefinedApprovalBaseStageLite stage)
    {
      if (stage == null)
        return null;
      
      var approvalStage = ApprovalStages.As(stage.StageBase);
      if (approvalStage == null)
        return null;
      
      return Structures.Module.DefinedApprovalStageLite.Create(approvalStage, stage.Number, stage.StageType);
    }
    
    /// <summary>
    /// Преобразовать этап согласования в базовый этап.
    /// </summary>
    /// <param name="stages">Список этапов согласования.</param>
    /// <returns>Список базовых этапов согласования.</returns>
    public static List<Structures.Module.DefinedApprovalBaseStageLite> CastToBaseApprovalStageLite(List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var baseStages = new List<Structures.Module.DefinedApprovalBaseStageLite>();
      foreach (var stage in stages)
        baseStages.Add(Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stage));
      return baseStages;
    }
    
    /// <summary>
    /// Преобразовать этап согласования в базовый этап.
    /// </summary>
    /// <param name="stage">Этап согласования.</param>
    /// <returns>Базовый этап согласования.</returns>
    public static Structures.Module.DefinedApprovalBaseStageLite CastToBaseApprovalStageLite(Structures.Module.DefinedApprovalStageLite stage)
    {
      if (stage == null)
        return null;
      
      var baseStage = ApprovalStageBases.As(stage.Stage);
      if (baseStage == null)
        return null;
      
      return Structures.Module.DefinedApprovalBaseStageLite.Create(baseStage, stage.Number, stage.StageType);
    }
    
    /// <summary>
    /// Преобразовать список базовых этапов в список этапов согласования.
    /// </summary>
    /// <param name="baseStages">Список базовых этапов.</param>
    /// <returns>Список этапов согласования.</returns>
    public static Structures.Module.DefinedApprovalStages CastToDefinedApprovalStages(Structures.Module.DefinedApprovalBaseStages baseStages)
    {
      var stages = new List<Structures.Module.DefinedApprovalStageLite>();
      foreach (var baseStage in baseStages.BaseStages)
      {
        var stage = Functions.ApprovalRuleBase.CastToDefinedApprovalStageLite(baseStage);
        if (stage != null)
          stages.Add(stage);
      }
      return Structures.Module.DefinedApprovalStages.Create(stages, baseStages.IsConditionsDefined, baseStages.ErrorMessage);
    }
  }
}
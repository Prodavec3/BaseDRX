using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStageBase;

namespace Sungero.Docflow.Server
{
  partial class ApprovalStageBaseFunctions
  {
    /// <summary>
    /// Получить список правил согласования с текущим этапом согласования.
    /// </summary>
    /// <returns>Список правил согласования.</returns>
    [Remote]
    public IQueryable<IApprovalRuleBase> GetApprovalRules()
    {
      return ApprovalRuleBases.GetAll(r => r.Stages.Any(s => Equals(s.StageBase, _obj)));
    }
    
    /// <summary>
    /// Проверить, не используется ли этап в правилах.
    /// </summary>
    /// <returns>True, если используется, false, если нет.</returns>
    [Remote(IsPure = true), Public]
    public bool HasRules()
    {
      return ApprovalRuleBases.GetAll(r => r.Stages.Any(s => Equals(s.StageBase, _obj))).Any();
    }
    
    /// <summary>
    /// Добвить этап в схему правила согласования.
    /// </summary>
    /// <param name="linedRoute">Схема правила.</param>
    /// <param name="prefix">Префикс перед заголовком.</param>
    /// <param name="level">Отступ от левого края: 0, 1, 2.</param>
    /// <remarks>Используется в отчете Печать правила согласования. Вынесено в этапы для перекрываемости.</remarks>
    public virtual void AddStageToRoute(List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute, string prefix, int level)
    {
      return;
    }
    
    /// <summary>
    /// Получить ожидаемый срок выполнения этапа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="maxDeadline">Точка отсчета срока.</param>
    /// <param name="stageInProcess">Признак, что этап в работе.</param>
    /// <returns>Ожидаемый срок выполнения этапа.</returns>
    public virtual DateTime GetStageMaxDeadline(IApprovalTask task, DateTime maxDeadline, bool stageInProcess)
    {
      var deadlineInDays = _obj.DeadlineInDays.HasValue ? _obj.DeadlineInDays.Value : 0;
      var deadlineInHours = _obj.DeadlineInHours.HasValue ? _obj.DeadlineInHours.Value : 0;
      
      maxDeadline = maxDeadline.AddWorkingDays(deadlineInDays);
      maxDeadline = maxDeadline.AddWorkingHours(deadlineInHours);
      
      return maxDeadline;
    }
    
    /// <summary>
    /// Проверить корректность срока этапа.
    /// </summary>
    /// <param name="e">Параметр сохранения.</param>
    public virtual void ValidateStageDeadline(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Срок должен быть заполнен у всех этапов.
      if (!_obj.DeadlineInDays.HasValue && !_obj.DeadlineInHours.HasValue)
      {
        e.AddError(_obj.Info.Properties.DeadlineInDays, ApprovalStages.Resources.NeedSetStageDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
        e.AddError(_obj.Info.Properties.DeadlineInHours, ApprovalStages.Resources.NeedSetStageDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
      }
      
      // Общий срок этапа должен быть больше нуля.
      if ((_obj.DeadlineInDays ?? 0) + (_obj.DeadlineInHours ?? 0) == 0 && e.IsValid)
      {
        e.AddError(_obj.Info.Properties.DeadlineInDays, ApprovalStages.Resources.IncorrectHourDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
        e.AddError(_obj.Info.Properties.DeadlineInHours, ApprovalStages.Resources.IncorrectHourDeadline, new[] { _obj.Info.Properties.DeadlineInDays, _obj.Info.Properties.DeadlineInHours });
      }
    }
    
    /// <summary>
    /// Валидация этапа при сохранении правила.
    /// </summary>
    /// <param name="rule">Правило.</param>
    /// <param name="stagesSequence">Последовательность этапов одной ветви.</param>
    /// <param name="stage">Этап.</param>
    /// <param name="e">Аргументы события До сохранения.</param>
    public virtual void Validate(IApprovalRuleBase rule, List<IApprovalRuleBaseStages> stagesSequence, IApprovalRuleBaseStages stage, Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Получить поддерживаемые роли согласования для этапа.
    /// </summary>
    /// <returns>Список поддерживаемых ролей.</returns>
    public virtual List<Enumeration?> GetSupportableRoles()
    {
      return new List<Enumeration?>();
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalFunctionStageBase;

namespace Sungero.Docflow.Server
{
  partial class ApprovalFunctionStageBaseFunctions
  {

    /// <summary>
    /// Выполнить сценарий.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Результат выполнения сценария.</returns>
    public virtual Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult Execute(IApprovalTask approvalTask)
    {
      return this.GetSuccessResult();
    }
    
    /// <summary>
    /// Проверить состояние этапа выполнения сценария.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Состояние этапа.</returns>
    public virtual Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult CheckCompletionState(IApprovalTask approvalTask)
    {
      return this.GetSuccessResult();
    }
    
    /// <summary>
    /// Действия на прекращение задачи на согласование по регламенту.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stageNumber">Номер этапа.</param>
    public virtual void Abort(IApprovalTask task, int stageNumber)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Действие при отправке на доработку.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stageNumber">Номер этапа.</param>
    public virtual void Rework(IApprovalTask task, int stageNumber)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Результат выполнения сценария, если он успешно выполнен.
    /// </summary>
    /// <returns>Результат выполнения сценария.</returns>
    public virtual Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult GetSuccessResult()
    {
      return Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult.Create(true, false, string.Empty);
    }
    
    /// <summary>
    /// Результат выполнения сценария, если он выполнен с ошибкой.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <returns>Результат выполнения сценария.</returns>
    public virtual Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult GetErrorResult(string errorMessage)
    {
      return Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult.Create(false, false, errorMessage);
    }
    
    /// <summary>
    /// Результат выполнения сценария, если требуется переповтор.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <returns>Результат выполнения сценария.</returns>
    public virtual Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult GetRetryResult(string errorMessage)
    {
      return Docflow.Structures.ApprovalFunctionStageBase.ExecutionResult.Create(false, true, errorMessage);
    }
    
    /// <summary>
    /// Добавить этап в схему правила согласования.
    /// </summary>
    /// <param name="linedRoute">Схема правила.</param>
    /// <param name="prefix">Префикс перед заголовком.</param>
    /// <param name="level">Отступ от левого края: 0, 1, 2.</param>
    /// <remarks>Используется в отчете Печать правила согласования. Вынесено в этапы для перекрываемости.</remarks>
    public override void AddStageToRoute(List<Structures.ApprovalRuleCardReport.ConditionTableLine> linedRoute, string prefix, int level)
    {
      var tableLine = new Structures.ApprovalRuleCardReport.ConditionTableLine();

      // Тип этапа.
      tableLine.StageType = ApprovalRuleBases.Info.Properties.Stages.Properties.StageType.GetLocalizedValue(Functions.ApprovalFunctionStageBase.GetStageType(_obj));
      
      var ruleId = _obj.Id.ToString();
      var hyperlink = Hyperlinks.Get(_obj);
      tableLine.RuleId = ruleId;
      tableLine.Hyperlink = hyperlink;
      tableLine.Header = ApprovalRuleCardReportServerHandlers.BreakLineAndAddPadding(_obj.Name, Constants.ApprovalRuleCardReport.StageCellWidth, level);
      tableLine.Level = level;
      tableLine.IsCondition = false;
      
      // Ожидание выполнения.
      var parameters = new List<string>();
      var deadline = Sungero.Docflow.ApprovalFunctionStageBases.Resources.Timeout.ToString();
      int? days = _obj.TimeoutInDays;
      int? hours = _obj.TimeoutInHours;
      
      if (days.HasValue && hours.HasValue)
        deadline += string.Format(" {0} {1}, {2} {3}",
                                  days, Functions.Module.GetNumberDeclination(days.Value, Resources.StateViewDay, Resources.StateViewDayGenetive, Resources.StateViewDayPlural),
                                  hours, Functions.Module.GetNumberDeclination(hours.Value, Resources.StateViewHour, Resources.StateViewHourGenetive, Resources.StateViewHourPlural));
      else if (days.HasValue)
        deadline += string.Format(" {0} {1}",
                                  days,
                                  Functions.Module.GetNumberDeclination(days.Value, Resources.StateViewDay, Resources.StateViewDayGenetive, Resources.StateViewDayPlural));
      else if (hours.HasValue)
        deadline += string.Format(" {0} {1}",
                                  hours,
                                  Functions.Module.GetNumberDeclination(hours.Value, Resources.StateViewHour, Resources.StateViewHourGenetive, Resources.StateViewHourPlural));
      
      parameters.Add(deadline);
      
      if (_obj.TimeoutAction != null)
        parameters.Add(string.Format("{0}: {1}", Docflow.ApprovalFunctionStageBases.Info.Properties.TimeoutAction.LocalizedName,
                                     Docflow.ApprovalFunctionStageBases.Info.Properties.TimeoutAction.GetLocalizedValue(_obj.TimeoutAction)));
      
      tableLine.Parameters = string.Join(System.Environment.NewLine, parameters);
      linedRoute.Add(tableLine);
    }
    
    /// <summary>
    /// Получить имя блока для построения предметного отображения в задаче.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <param name="statusInfo">Состояние текущего блока.</param>
    /// <returns>Имя блока.</returns>
    public virtual string GetStateViewBlockName(IApprovalTask task, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      return _obj.Name;
    }
    
    /// <summary>
    /// Добавить исполнителей блока для построения предметного отображения в задаче.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <param name="block">Блок предметного отображения.</param>
    /// <param name="statusInfo">Состояние текущего блока.</param>
    public virtual void AddStateViewBlockPerformers(IApprovalTask task, Sungero.Core.StateBlock block, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      var noteStyle = Functions.Module.CreateNoteStyle();
      var performer = OfficialDocuments.Resources.StateViewSystem.ToString();
      block.AddLabel(performer, noteStyle);
    }
    
    /// <summary>
    /// Получить срок выполнения блока для построения предметного отображения в задаче.
    /// </summary>
    /// <param name="task">Задача на согласование по регламенту.</param>
    /// <param name="statusInfo">Состояние текущего блока.</param>
    /// <returns>Срок выполнения.</returns>
    public virtual string GetStateViewBlockDeadline(IApprovalTask task, Docflow.Structures.ApprovalRuleBase.StageStatusInfo statusInfo)
    {
      return string.Empty;
    }
    
    /// <summary>
    /// Отправить подзадачу об истечении срока выполнения сценария.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    public virtual void SendTimeoutSubtask(IApprovalTask approvalTask, string errorMessage)
    {
      // Задать исполнителей.
      var performers = this.GetTimeoutSubtaskPerformers(approvalTask);

      // Задать текст и тему.
      var threadSubject = this.GetTimeoutSubtaskThreadSubject();
      var subject = this.GetTimeoutSubtaskSubject(threadSubject, approvalTask.Subject);
      var text = this.GetTimeoutSubtaskActiveText(errorMessage);
      Functions.Module.SendNoticesAsSubtask(subject, performers, approvalTask, text, null, threadSubject);
    }
    
    /// <summary>
    /// Получить список исполнителей, кому будет отправлена подзадача об истечении срока.
    /// </summary>
    /// <param name="approvalTask">Задача на согласование по регламенту.</param>
    /// <returns>Список исполнителей.</returns>
    public virtual List<IUser> GetTimeoutSubtaskPerformers(IApprovalTask approvalTask)
    {
      var performers = new List<IUser>();
      performers.Add(approvalTask.Author);
      
      var reworkPerformer = Functions.ApprovalTask.GetReworkPerformer(approvalTask, null);
      if (!Equals(reworkPerformer, approvalTask.Author))
      {
        performers.Add(reworkPerformer);
        Functions.ApprovalTask.SetAccessRightsForAttachments(approvalTask, reworkPerformer, DefaultAccessRightsTypes.Read, false);
      }
      return performers;
    }
    
    /// <summary>
    /// Получить тему для переписки.
    /// </summary>
    /// <returns>Тема в переписке.</returns>
    public virtual string GetTimeoutSubtaskThreadSubject()
    {
      return Sungero.Docflow.ApprovalFunctionStageBases.Resources.TimeoutSubtaskThreadSubject;
    }
    
    /// <summary>
    /// Получить тему задачи об истечении срока.
    /// </summary>
    /// <param name="prefix">Префикс.</param>
    /// <param name="additionalInfo">Дополнительная информация.</param>
    /// <returns>Тема задачи.</returns>
    public virtual string GetTimeoutSubtaskSubject(string prefix, string additionalInfo)
    {
      return string.Format(Sungero.Docflow.ApprovalFunctionStageBases.Resources.SubtaskSubjectTemplate, prefix, Docflow.PublicFunctions.Module.TrimSpecialSymbols(additionalInfo));
    }
    
    /// <summary>
    /// Получить текст подзадачи об истечении срока.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <returns>Текст подзадачи об истечении срока.</returns>
    public virtual string GetTimeoutSubtaskActiveText(string errorMessage)
    {
      var skipCurrentStage = Equals(_obj.TimeoutAction, Docflow.ApprovalFunctionStageBase.TimeoutAction.Skip);
      var activeText = string.Empty;
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var link = Hyperlinks.Get(_obj);
        activeText = Sungero.Docflow.ApprovalFunctionStageBases.Resources.HasFunctionStageErrorFormat(link);
        if (!string.IsNullOrEmpty(errorMessage))
          activeText += string.Format(":{0}{1}{2}", Environment.NewLine, errorMessage, Environment.NewLine);
        else
          activeText += ".";
        var additionalText = skipCurrentStage ? Sungero.Docflow.ApprovalFunctionStageBases.Resources.SkipFunctionStageError : Sungero.Docflow.ApprovalFunctionStageBases.Resources.RepeatFunctionStageError;
        activeText += string.Format("{0}{1}", Environment.NewLine, additionalText);
      }
      return activeText;
    }
    
  }
}
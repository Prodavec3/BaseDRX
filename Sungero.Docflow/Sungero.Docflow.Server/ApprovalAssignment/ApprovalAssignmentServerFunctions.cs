using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalAssignmentFunctions
  {
    #region Контроль состояния
    
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
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
    /// Проверить, можно ли добавить сотрудника в процесс согласования.
    /// </summary>
    /// <param name="employee">Сотрудник, которого добавляем.</param>
    /// <returns>True, если сотрудника можно добавлять.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanForwardTo(Company.IEmployee employee)
    {
      var assignments = ApprovalAssignments.GetAll(a => Equals(a.Task, _obj.Task) &&
                                                   Equals(a.TaskStartId, _obj.TaskStartId) &&
                                                   Equals(a.IterationId, _obj.IterationId));

      // Если у сотрудника есть хоть одно задание в работе - считаем, что нет смысла дублировать ему задания.
      // BUG: если assignments материализовать (завернуть ToList), то в задании можно будет переадресовать самому себе, т.к. в BeforeComplete задание считается уже выполненным.
      var hasInProcess = assignments.Where(a => Equals(a.Status, Status.InProcess) && Equals(a.Performer, employee)).Any();
      if (hasInProcess)
        return false;
      
      // При последовательном выполнении сотрудники ещё не получили задания, вычисляем их.
      var currentStageApprovers = Functions.ApprovalAssignment.GetCurrentIterationEmployeesWithoutAssignment(_obj);
      if (currentStageApprovers.Contains(employee))
        return false;
      
      var materialized = assignments.ToList();
      // Если у сотрудника нет заданий в работе, проверяем, все ли его задания созданы.
      foreach (var assignment in materialized)
      {
        var added = assignment.ForwardedTo.Count(u => Equals(u, employee));
        var created = materialized.Count(a => Equals(a.Performer, employee) && Equals(a.ForwardedFrom, assignment));
        if (added != created)
          return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить согласующих, которые будут реально согласовывать на текущей итерации.
    /// </summary>
    /// <returns>Согласующие.</returns>
    public List<Company.IEmployee> GetCurrentIterationEmployeesWithoutAssignment()
    {
      var approvalTask = ApprovalTasks.As(_obj.Task);
      var performers = ApprovalAssignments.GetAll(x => Equals(x.Task, approvalTask) && x.IterationId == _obj.IterationId &&
                                                  Equals(x.BlockUid, _obj.BlockUid) && x.TaskStartId == _obj.TaskStartId)
        .Select(x => x.Performer).Distinct().ToList();
      return GetCurrentIterationEmployees(approvalTask, _obj.Stage)
        .Where(x => !performers.Contains(x))
        .ToList();
    }
    
    /// <summary>
    /// Получить согласующих, которые будут реально согласовывать на текущей итерации.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="stage">Этап согласования (обязательных или доп. согласующих).</param>
    /// <returns>Согласующие.</returns>
    public static List<Company.IEmployee> GetCurrentIterationEmployees(IApprovalTask task, IApprovalStage stage)
    {
      var result = new List<Company.IEmployee>();
      var lastReworkAssignment = Functions.ApprovalTask.GetLastReworkAssignment(task);
      if (lastReworkAssignment != null)
        Logger.DebugFormat("Find last rework assignment id {0}.", lastReworkAssignment.Id);
      
      var approvers = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(task, stage);
      
      // Исключить согласующих, если они уже подписали документ, либо в последнем задании на доработку было указано, что повторно не отправлять.
      foreach (var approver in approvers)
      {
        Logger.DebugFormat("Find approver id {0}.", approver.Id);
        if (lastReworkAssignment == null ||
            lastReworkAssignment.Approvers.Any(a => Equals(a.Approver, approver) && a.Action == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval) ||
            !lastReworkAssignment.Approvers.Any(a => Equals(a.Approver, approver)))
        {
          if (!Functions.ApprovalTask.HasValidSignature(task, approver))
          {
            Logger.DebugFormat("Add approver id {0} to current iteration employees.", approver.Id);
            result.Add(approver);
          }
        }
      }
      
      return result;
    }
  }
}
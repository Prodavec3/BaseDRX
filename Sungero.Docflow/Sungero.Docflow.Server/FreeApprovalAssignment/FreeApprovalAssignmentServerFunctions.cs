using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow.Server
{
  partial class FreeApprovalAssignmentFunctions
  {
    /// <summary>
    /// Проверить, можно ли добавить сотрудника в процесс согласования.
    /// </summary>
    /// <param name="employee">Сотрудник, которого добавляем.</param>
    /// <returns>True, если сотрудника можно добавлять.</returns>
    [Remote(IsPure = true)]
    public virtual bool CanForwardTo(Company.IEmployee employee)
    {
      var assignments = FreeApprovalAssignments.GetAll(a => Equals(a.Task, _obj.Task) &&
                                                       Equals(a.TaskStartId, _obj.TaskStartId) &&
                                                       Equals(a.IterationId, _obj.IterationId));

      // Если у сотрудника есть хоть одно задание в работе - считаем, что нет смысла дублировать ему задания.
      // BUG: если assignments материализовать (завернуть ToList), то в задании можно будет переадресовать самому себе, т.к. в BeforeComplete задание считается уже выполненным.
      var hasInProcess = assignments.Where(a => Equals(a.Status, Status.InProcess) && Equals(a.Performer, employee)).Any();
      if (hasInProcess)
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
  }
}
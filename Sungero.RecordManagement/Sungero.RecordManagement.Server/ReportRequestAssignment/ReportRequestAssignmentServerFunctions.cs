using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class ReportRequestAssignmentFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var task = StatusReportRequestTasks.As(_obj.Task);
      return Functions.StatusReportRequestTask.GetStateView(task);
    }
  }
}
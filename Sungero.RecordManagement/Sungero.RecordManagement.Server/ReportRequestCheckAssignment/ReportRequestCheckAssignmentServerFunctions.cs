using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReportRequestCheckAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class ReportRequestCheckAssignmentFunctions
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
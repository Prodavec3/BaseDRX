using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement;
using Sungero.RecordManagement.DeadlineExtensionAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class DeadlineExtensionAssignmentFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var task = DeadlineExtensionTasks.As(_obj.Task);
      return Functions.DeadlineExtensionTask.GetStateView(task);
    }
  }
}
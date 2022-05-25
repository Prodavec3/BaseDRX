using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionNotification;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionNotificationFunctions
  {
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      return Functions.ActionItemExecutionTask.GetStateView(task);
    }
  }
}
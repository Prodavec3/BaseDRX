using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Client
{
  partial class FreeApprovalTaskFunctions
  {
    
    /// <summary>
    /// Проверить возможность отправки задания подписания на доработку.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    /// <returns>True - разрешить отправку, иначе false.</returns>
    public static bool ValidateBeforeRework(IAssignment assignment, string errorMessage, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (string.IsNullOrEmpty(assignment.ActiveText))
      {
        eventArgs.AddError(errorMessage);
        return false;
      }
      
      return true;
    }

  }
}
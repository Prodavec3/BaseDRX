using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.AcquaintanceTask
{
  /// <summary>
  /// Сообщение валидации при старте задачи.
  /// </summary>
  partial class StartValidationMessage
  {
    /// <summary>
    /// Сообщение валидации.
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Признак, что сообщение о неавтоматизированных сотрудниках в участниках ознакомления.
    /// </summary>
    public bool IsShowNotAutomatedEmployeesMessage { get; set; }
    
    /// <summary>
    /// Признак, что сообщение об ошибке валидации сотрудника.
    /// </summary>
    public bool IsCantSendTaskByNonEmployeeMessage { get; set; }
  }

}
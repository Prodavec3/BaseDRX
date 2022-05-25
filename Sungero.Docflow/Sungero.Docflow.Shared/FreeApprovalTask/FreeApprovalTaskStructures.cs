using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.FreeApprovalTask
{
  /// <summary>
  /// Информация о задании.
  /// </summary>
  partial class AssignmentInfo
  {
    /// <summary>
    /// Исполнитель.
    /// </summary>
    public string Performer { get; set; }
    
    /// <summary>
    /// Срок задания.
    /// </summary>
    public string Deadline { get; set; }
    
    /// <summary>
    /// Результат выполнения.
    /// </summary>
    public string Status { get; set; }
  }
  
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
    /// Признак, что сообщение об ошибке валидации срока.
    /// </summary>
    public bool IsImpossibleSpecifyDeadlineLessThanTodayMessage { get; set; }
    
    /// <summary>
    /// Признак, что сообщение об ошибке валидации сотрудника.
    /// </summary>
    public bool IsCantSendTaskByNonEmployeeMessage { get; set; }
  }
}
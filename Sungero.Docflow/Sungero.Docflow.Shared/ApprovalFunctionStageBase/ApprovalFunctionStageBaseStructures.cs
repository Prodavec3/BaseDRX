using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalFunctionStageBase
{
  /// <summary>
  /// Результат выполнения сценария.
  /// </summary>
  partial class ExecutionResult
  {
    public bool Success { get; set; }
    
    public bool Retry { get; set; }
    
    public string ErrorMessage { get; set; }
  }
}
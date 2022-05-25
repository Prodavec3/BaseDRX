using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ConditionBase
{

  /// <summary>
  /// Результат проверки условия.
  /// </summary>
  partial class ConditionResult
  {
    public bool? Branch { get; set; }
    
    public string Message { get; set; }
  }

}
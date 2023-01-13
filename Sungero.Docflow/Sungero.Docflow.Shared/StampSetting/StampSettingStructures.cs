using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.StampSetting
{
  /// <summary>
  /// Параметры простановки отметки.
  /// </summary>
  [Public]
  partial class SignatureStampParams
  {
    public string Logo { get; set; }
    
    public string Title { get; set; }
    
    public string SigningDate { get; set; }
  }
}
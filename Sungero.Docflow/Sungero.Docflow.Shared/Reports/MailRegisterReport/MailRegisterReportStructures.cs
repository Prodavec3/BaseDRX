using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.MailRegisterReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public string ToName { get; set; }
    
    public string PostAddress { get; set; }
  }
}
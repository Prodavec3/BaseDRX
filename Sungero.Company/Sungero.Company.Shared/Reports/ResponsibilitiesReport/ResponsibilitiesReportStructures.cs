using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Structures.ResponsibilitiesReport
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class ResponsibilitiesReportTableLine
  {    
    public string ModuleName { get; set; }
    
    public string Responsibility { get; set; }
    
    public string Record { get; set; }
    
    public int? RecordId { get; set; }
    
    public string RecordHyperlink { get; set; }
    
    public int Priority { get; set; }
    
    public string ReportSessionId { get; set; }
  }  
}
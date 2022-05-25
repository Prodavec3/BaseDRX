using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.ActionItemPrintReport
{
  /// <summary>
  /// Параметры для отчета ActionItemPrintReport.
  /// </summary>
  [Public]
  partial class ActionItemPrintReportParameters
  {
    public string ReportSessionId { get; set; }
    
    public string Performer { get; set; }
    
    public string Deadline { get; set; }

    public string CoAssigneesDeadline { get; set; }
    
    public string FromAuthor { get; set; }
    
    public string ActionItemText { get; set; }  

    public string Supervisor { get; set; }
    
  }
}
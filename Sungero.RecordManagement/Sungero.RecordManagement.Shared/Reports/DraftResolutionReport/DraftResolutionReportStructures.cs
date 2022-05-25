using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.DraftResolutionReport
{
  /// <summary>
  /// Параметры для отчета DraftResolutionReport.
  /// </summary>
  [Public]
  partial class DraftResolutionReportParameters
  {
    public string ReportSessionId { get; set; }
    
    public string PerformersLabel { get; set; }
    
    public string Deadline { get; set; }
    
    public string ResolutionLabel { get; set; }
    
    public string SupervisorLabel { get; set; }
  }
}
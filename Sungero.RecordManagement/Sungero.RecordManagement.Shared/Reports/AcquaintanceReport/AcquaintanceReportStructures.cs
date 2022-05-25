using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.AcquaintanceReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string TaskDisplayName { get; set; }
    
    public int TaskId { get; set; }

    public string TaskHyperlink { get; set; }

    public int RowNumber { get; set; }
    
    public string ShortName { get; set; }
    
    public string LastName { get; set; }

    public string JobTitle { get; set; }

    public string Department { get; set; }

    public string AcquaintanceDate { get; set; }

    public string State { get; set; }
    
    public string Note { get; set; }
    
    public string AssignmentId { get; set; }
    
    public string AssignmentHyperlink { get; set; }
    
    public string ReportSessionId { get; set; }
  }
}
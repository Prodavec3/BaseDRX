using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.EmployeesAssignmentCompletionReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  [Public]
  partial class TableLine
  {
    public int RowIndex { get; set; }
    
    public string ReportSessionId { get; set; }

    public int Employee { get; set; }
    
    public string EmployeeName { get; set; }
    
    public bool IsActiveEmployee { get; set; }
    
    public string JobTitle { get; set; }
    
    public string Department { get; set; }
    
    public int? AssignmentCompletion { get; set; }
    
    public int AssignmentsCount { get; set; }
    
    public int AffectDisciplineAssignmentsCount { get; set; }
    
    public int CompletedInTimeAssignmentsCount { get; set; }
    
    public int OverdueAssignmentsCount { get; set; }
    
    public int OverdueCompletedAssignmentsCount { get; set; }
    
    public int OverdueInWorkAssignmentsCount { get; set; }

  }
  
}
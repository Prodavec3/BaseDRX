using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DepartmentsAssignmentCompletionReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  [Public]
  partial class TableLine
  {
    public int RowIndex { get; set; }
    
    public string ReportSessionId { get; set; }

    public int Department { get; set; }
    
    public bool Unwrap { get; set; }
    
    public string DepartmentName { get; set; }
    
    public bool IsActiveDepartment { get; set; }
    
    public string SubDepartmentName { get; set; }
    
    public bool IsActiveSubDepartment { get; set; }
    
    public int HyperlinkBusinessUnitId { get; set; }
    
    public string BusinessUnitName { get; set; }
    
    public int? AssignmentCompletion { get; set; }
    
    public int AssignmentsCount { get; set; }
    
    public int AffectDisciplineAssignmentsCount { get; set; }
    
    public int CompletedInTimeAssignmentsCount { get; set; }
    
    public int OverdueAssignmentsCount { get; set; }
    
  }
}
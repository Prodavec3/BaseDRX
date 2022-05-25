using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.EmployeeAssignmentsReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  [Public]
  partial class TableLine
  {
    public string ReportSessionId { get; set; }

    public int AssignmentId { get; set; }
    
    public string Subject { get; set; }
    
    public string AuthorName { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime? Deadline { get; set; }
    
    public DateTime? Completed { get; set; }
    
    public int? Delay { get; set; }
    
    public string RealPerformerName { get; set; }
    
    public bool AffectDiscipline { get; set; }
  }
  
  /// <summary>
  /// Информация по выполненному заданию.
  /// </summary>
  [Public]
  partial class AssignmentLightView
  {
    public int AssignmentId { get; set; }
    
    public string Subject { get; set; }
    
    public string AuthorName { get; set; }
    
    public string RealPerformerName { get; set; }
  }
}
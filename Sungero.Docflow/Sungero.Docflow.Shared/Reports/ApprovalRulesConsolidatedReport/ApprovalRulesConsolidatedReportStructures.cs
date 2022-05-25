using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalRulesConsolidatedReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public string ApprovalRule { get; set; }
    
    public int? ApprovalRuleId { get; set; }
    
    public string ApprovalRuleUrl { get; set; }
    
    public int? ApprovalRulePriority { get; set; }
    
    public string Status { get; set; }
    
    public string DocumentKind { get; set; }
    
    public string DocumentFlow { get; set; }
    
    public string BusinessUnit { get; set; }
    
    public string Department { get; set; }
    
    public string Category { get; set; }
    
    public string DocumentParentType { get; set; }
    
    public string Relation { get; set; }
  }

}
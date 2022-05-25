using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalRuleCardReport
{
  /// <summary>
  /// Строка критерия правила.
  /// </summary>
  partial class CriteriaTableLine
  {
    public string ReportSessionId { get; set; }
    
    public string Criterion { get; set; }
    
    public string Value { get; set; }
    
    public int? ValueId { get; set; }
    
    public string ValueHyperlink { get; set; }
  }
  
  partial class ConditionTableLine
  {
    public string ReportSessionId { get; set; }
    
    public string Prefix { get; set; }
    
    public string Header { get; set; }
    
    public string StageType { get; set; }
    
    public string Performers { get; set; }
    
    public string Deadline { get; set; }
    
    public string Parameters { get; set; }
    
    public string Item { get; set; }
    
    public string Text { get; set; }

    public string RuleId { get; set; }
    
    public string Hyperlink { get; set; }
    
    public int Level { get; set; }
    
    public int Id { get; set; }
    
    public bool IsCondition { get; set; }
    
    public int TableLineId { get; set; }
  }
  
  /// <summary>
  /// Структура данных прав подписи.
  /// </summary>
  partial class SignatureSettingsTableLine
  {
    public string ReportSessionId { get; set; }
    
    public int OrderNumber { get; set; }
    
    public string Name { get; set; }
    
    public int Id { get; set; }
    
    public string Hyperlink { get; set; }
    
    public string UnitsAndDeps { get; set; }
    
    public string KindsAndCategories { get; set; }
    
    public int Priority { get; set; }
    
    public string Limits { get; set; }
    
    public string ValidTill { get; set; }
    
    public string Note { get; set; }
  }
  
  /// <summary>
  /// Ветка перехода по схеме.
  /// </summary>
  partial class Transition
  {
    public int? SourceStage { get; set; }
    
    public bool? ConditionValue { get; set; }
  }
}
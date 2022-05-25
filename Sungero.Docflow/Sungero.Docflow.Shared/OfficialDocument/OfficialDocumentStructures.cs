using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.OfficialDocument
{
  partial class DialogResult
  {
    public IDocumentRegister Register { get; set; }
    
    public DateTime Date { get; set; }
    
    public string Number { get; set; }
  }
  
  partial class DialogParams
  {
    public List<IDocumentRegister> Registers { get; set; }
    
    public Sungero.Core.Enumeration Operation { get; set; }
    
    public IDocumentRegister DefaultRegister { get; set; }
    
    public string CurrentRegistrationNumber { get; set; }
    
    public DateTime? CurrentRegistrationDate { get; set; }
    
    public string NextNumber { get; set; }
    
    public int LeadId { get; set; }
    
    public string LeadNumber { get; set; }
    
    public bool IsNumberValidationDisabled { get; set; }
    
    public int DepartmentId { get; set; }
    
    public string DepartmentCode { get; set; }
    
    public string BusinessUnitCode { get; set; }
    
    public int BusinessUnitId { get; set; }
    
    public string CaseFileIndex { get; set; }
    
    public string DocKindCode { get; set; }
    
    public string CounterpartyCode { get; set; }
    
    public bool IsClerk { get; set; }
  }
  
  /// <summary>
  /// Результат преобразования документа в PDF.
  /// </summary>
  [SuppressMessage("AppliedStylecopNamingRules.ApiNamingAnalyzer", "CR0001:ApiNamesMustNotContainCyrillic", Justification = "Deferred tech debt, #122325")]
  partial class СonversionToPdfResult
  {
    public bool IsFastConvertion { get; set; }
    
    public bool IsOnConvertion { get; set; }
    
    public bool HasErrors { get; set; }
    
    public bool HasConvertionError { get; set; }
    
    public bool HasLockError { get; set; }
    
    public string ErrorTitle { get; set; }
    
    public string ErrorMessage { get; set; }
  }
  
  /// <summary>
  /// Распознанное свойство.
  /// </summary>
  partial class RecognizedProperty
  {
    // Наименование.
    public string Name { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    // Позиция.
    public string Position { get; set; }
  }  
}
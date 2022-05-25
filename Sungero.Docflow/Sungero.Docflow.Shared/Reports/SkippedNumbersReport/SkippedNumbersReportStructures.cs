using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.SkippedNumbersReport
{
  /// <summary>
  /// Аргументы с которыми строится отчет.
  /// </summary>
  [Public]
  partial class BeforeExecuteArguments
  {
    public string ReportSessionId { get; set; }
    
    public DateTime CurrentDate { get; set; }
    
    public DateTime BaseDate { get; set; }
    
    public IDocumentRegister DocumentRegister { get; set; }
    
    public DateTime? RegistrationDate { get; set; }
    
    public bool LaunchedFromDialog { get; set; }
    
    public int PeriodOffset { get; set; }
    
    public string Period { get; set; }
    
    public DateTime PeriodBegin { get; set; }
    
    public DateTime PeriodEnd { get; set; }
    
    public DateTime? DocumentRegisterPeriodBegin { get; set; }
    
    public DateTime? DocumentRegisterPeriodEnd { get; set; }
    
    public bool HasLeadingDocument { get; set; }
    
    public IOfficialDocument LeadingDocument { get; set; }
    
    public bool HasDepartment { get; set; }
    
    public IDepartment Department { get; set; }
    
    public bool HasBusinessUnit { get; set; }
    
    public IBusinessUnit BusinessUnit { get; set; }
    
    public string NumberFormat { get; set; }
    
    public string HyperlinkMask { get; set; }
    
    public string SkipedNumberList { get; set; }
  }
  
  /// <summary>
  /// Пропущенный номер.
  /// </summary>
  partial class SkippedNumber
  {
    public string RegistrationNumber { get; set; }
    
    public string OrdinalNumber { get; set; }
    
    public int Index { get; set; }
    
    public string ReportSessionId { get; set; }
  }
  
  /// <summary>
  /// Доступный по правам документ.
  /// </summary>
  partial class AvailableDocument
  {
    public int Id { get; set; }
    
    public bool NumberOnFormat { get; set; }
    
    public bool CanRead { get; set; }
    
    public bool InCorrectOrder { get; set; }
    
    public string ReportSessionId { get; set; }
  }
}
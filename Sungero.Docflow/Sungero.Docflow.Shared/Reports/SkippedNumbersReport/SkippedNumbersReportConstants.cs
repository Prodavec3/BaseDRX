using System;

namespace Sungero.Docflow.Constants
{
  public static class SkippedNumbersReport
  {
    public const string SkipsTableName = "Sungero_Reports_SkipNum_Skips";
    
    public const string AvailableDocumentsTableName = "Sungero_Reports_SkipNum_Rights";
    
    // Код диалога.
    public const string HelpCode = "Sungero_Docflow_SkippedNumbersReportDialog";
    
    [Sungero.Core.Public]
    public const string Year = "Year";
    public const string Quarter = "Quarter";
    [Sungero.Core.Public]
    public const string Month = "Month";
    public const string Week = "Week";
    public const string Day = "Day";
    
  }
}
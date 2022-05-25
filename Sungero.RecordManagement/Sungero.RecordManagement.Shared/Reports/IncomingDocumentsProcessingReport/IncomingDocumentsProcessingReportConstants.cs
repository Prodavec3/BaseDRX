using System;

namespace Sungero.RecordManagement.Constants
{
  public static class IncomingDocumentsProcessingReport
  {
    /// <summary>
    /// Количество элементов в update запросе.
    /// </summary>
    public const int UpdateRows = 100;
    
    public const string IncomingDocumentsProcessingReportTableName = "Sungero_Reports_IncDocProcessing";
    
    // Код диалога.
    public const string HelpCode = "Sungero_RecMan_IncomingDocumentsProcessingReportDialog";
  }
}
using System;

namespace Sungero.RecordManagement.Constants
{
  public static class StatusReportRequestTask
  {
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "3cbaa2b2-0202-4137-bb56-e99882190220";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на подготовку отчета по поручению.
    /// </summary>
    public const string ReportRequestAssignmentConfirmDialogID = "15c28cde-67d4-4a57-b187-f1765c0fa7d3";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на проверку отчета по поручению.
    /// </summary>
    public static class ReportRequestCheckAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отчет принят".
      /// </summary>
      public const string Accept = "f40e7720-9cd3-41db-b0c1-af2e97a7417c";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "d3872afd-91ff-4c10-9fd7-6b64c5488ad1";
    }
  }
}
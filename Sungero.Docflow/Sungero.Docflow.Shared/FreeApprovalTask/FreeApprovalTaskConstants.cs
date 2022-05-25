using System;

namespace Sungero.Docflow.Constants
{
  public static class FreeApprovalTask
  {
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "7f6fc7cb-e261-4e4f-9a71-95f3871a6c9e";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на свободное согласование.
    /// </summary>
    public static class FreeApprovalAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Согласовать".
      /// </summary>
      public const string Approved = "936a56c4-60ca-4ed2-a2d5-bf0f311fdbfe";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "7a4b0121-bd31-4f8a-ade3-24046969f915";
      
      /// <summary>
      /// С результатом "Переадресовать".
      /// </summary>
      public const string Forward = "71e65e6c-33f1-4f9f-a38a-9ecba4af7e9e";
    }
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на доработку.
    /// </summary>
    public static class ReworkAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Исправлено".
      /// </summary>
      public const string Reworked = "94769837-f76c-4393-a034-3c0ac35344d4";
      
      /// <summary>
      /// С прекращением согласования.
      /// </summary>
      public const string AbortAction = "2540f66a-da05-45e8-aa87-3437722bedef";
    }
    
    /// <summary>
    /// ИД диалога подтверждения при завершении согласования.
    /// </summary>
    public const string FinishConfirmDialogID = "1c6cddd1-0b69-4cc8-a80a-c76a95cec36a";
    
    /// <summary>
    /// ИД группы приложений.
    /// </summary>
    public static readonly Guid AddendaGroupGuid = Guid.Parse("fe0d933f-02f8-4733-b110-1e49467a9cf8");
  }
}
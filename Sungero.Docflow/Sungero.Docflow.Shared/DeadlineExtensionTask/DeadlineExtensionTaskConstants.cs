using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class DeadlineExtensionTask
  {
    public const string CanSelectAssignee = "CanSelectAssignee";
    
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "d983330b-7bc2-4e6c-9be4-a3960bb3c227";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на отказ в продлении срока.
    /// </summary>
    public const string DeadlineRejectionAssignmentConfirmDialogID = "0b4c65a7-a835-463b-8012-18b4864e26e8";    
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на продление срока.
    /// </summary>
    public static class DeadlineExtensionAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отказать".
      /// </summary>
      public const string ForRework = "6ea60302-8527-4745-ab19-4799e40fcb51";
    }
  }
}
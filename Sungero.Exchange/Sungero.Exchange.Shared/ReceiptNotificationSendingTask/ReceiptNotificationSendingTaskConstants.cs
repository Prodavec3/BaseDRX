using System;
using Sungero.Core;

namespace Sungero.Exchange.Constants
{
  public static class ReceiptNotificationSendingTask
  {
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на отправку извещений о получении документов.
    /// </summary>
    public static class ReceiptNotificationSendingAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отправить извещения".
      /// </summary>
      public const string Complete = "23601D98-E6FA-4D9B-B1B8-3D9232250AA9";
      
      /// <summary>
      /// С результатом "Переадресовать".
      /// </summary>
      public const string Forwarded = "BD9932A0-910F-4FF3-BF06-B4BAFCDC1E4A";
    }
    
  }
}
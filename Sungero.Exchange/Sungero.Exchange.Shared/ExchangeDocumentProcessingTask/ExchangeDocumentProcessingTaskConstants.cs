using System;

namespace Sungero.Exchange.Constants
{
  public static class ExchangeDocumentProcessingTask
  {
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на обработку входящих документов эл. обмена.
    /// </summary>
    public static class ExchangeDocumentProcessingAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Обработано".
      /// </summary>
      public const string Complete = "144a5cdc-3e7a-4bb3-adee-51ec47501458";
      
      /// <summary>
      /// С результатом "Обработано" без отправки документов в работу.
      /// </summary>
      public const string CompleteWithoutAllDocumentsSendedForProcessing = "62205041-e6a7-4e47-8f98-f2f8e9e41590";
      
      /// <summary>
      /// С результатом "Переадресовано".
      /// </summary>
      public const string ReAddress = "4cc11032-57a7-44e4-a762-0b7c06052371";
      
      /// <summary>
      /// С результатом "Отказано".
      /// </summary>
      public const string Abort = "5dcd4db8-e458-46d6-a507-116b360975b5";
    }
  }
}
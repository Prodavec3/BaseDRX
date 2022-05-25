using System;

namespace Sungero.ExchangeCore.Constants
{
  public static class IncomingInvitationTask
  {
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на обработку приглашения к эл. обмену.
    /// </summary>
    public static class IncomingInvitationAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Принять".
      /// </summary>
      public const string Accept = "304d4140-5cf2-4317-928b-acff2185346f";
      
      /// <summary>
      /// С результатом "Отклонить".
      /// </summary>
      public const string Reject = "8579d954-3ab9-479f-96b2-8b3a65db06d6";
    }
  }
}
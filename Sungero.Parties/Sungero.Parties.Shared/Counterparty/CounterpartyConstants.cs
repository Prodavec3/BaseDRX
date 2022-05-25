using System;

namespace Sungero.Parties.Constants
{
  public static class Counterparty
  {
    // Отправлено приглашение к обмену.
    public const string InvitationSentToCA = "SentToCA";
    
    // Получено приглашение к обмену.
    public const string InvitationSentToUs = "SentToUs";
    
    // Разрешен обмен с контрагентом.
    public const string ExchangeWithCAActivated = "Activated";
    
    // Заблокирован обмен с контрагентом.
    public const string ExchangeWithCAClosed = "Closed";
    
    // Статус изменился.
    public const string StatusChanged = "Changed";
    
    /// <summary>
    /// GUID для системного контрагента - "По списку рассылки".
    /// </summary>
    public static readonly Guid DistributionListCounterpartyGuid = Guid.Parse("29CBFDCC-7DD2-4013-A85D-978452CF2F45");
    
    /// <summary>
    /// Коды справки для действий по обмену.
    /// </summary>
    public static class HelpCodes
    {
      // Диалог отправки приглашения.
      public const string SendInvitation = "Sungero_Exchange_InvitationDialog";
      
      // Возобновление обмена из состояния "Обмен заблокирован".
      public const string RestoreExchange = "Sungero_Exchange_RestoreExchangeDialog";
      
      // Разрешение \ Запрет из состояния "Получено приглашение".
      public const string WhenApprovingByUs = "Sungero_Exchange_ApprovingByUsDialog";
      
      // Прекращение из состояния "Отправлено приглашение" или "Обмен разрешен".
      public const string RejectExchange = "Sungero_Exchange_RejectExchangeDialog";
    }
  }
}
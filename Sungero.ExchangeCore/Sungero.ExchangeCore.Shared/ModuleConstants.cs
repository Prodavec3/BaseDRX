using System;

namespace Sungero.ExchangeCore.Constants
{
  public static class Module
  {
    public const string LastBoxSyncDate = "LastBoxSyncDate_{0}";
    
    /// <summary>
    /// Количество дней для уведомления о том, что истекает сертификат абонентского ящика.
    /// </summary>
    public const int ExpirePeriod = 30;
    
    public static class RoleGuid
    {
      // GUID роли "Пользователи с правами на работу через сервис обмена".
      [Sungero.Core.Public]
      public static readonly Guid ExchangeServiceUsersRoleGuid = Guid.Parse("5AFA06FB-3B66-4216-8681-56ACDEAC7FC1");
      
      // GUID роли "Ответственные за контрагентов".
      [Sungero.Core.Public]
      public static readonly Guid CounterpartiesResponsibleRole = Guid.Parse("C719C823-C4BD-4434-A34B-D7E83E524414");
    }
    
    #region Права
    
    /// <summary>
    /// GUID прав.
    /// </summary>
    public static class DefaultAccessRightsTypeSid
    {
      /// <summary>
      /// Отправка через сервис обмена.
      /// </summary>
      public static readonly Guid SendByExchange = Guid.Parse("56D0F76C-5442-4B0C-B363-7E353D348994");
      
      /// <summary>
      /// Установить обмен с контрагентом.
      /// </summary>
      public static readonly Guid SetExchange = Guid.Parse("6CCAD865-AA9C-4AFD-A08B-836363545AAF");

      /// <summary>
      /// Изменение.
      /// </summary>
      public static readonly Guid Update = Guid.Parse("179af257-a60f-44b8-97b5-1d5bbd06716b");
    }
    
    #endregion
    
    /// <summary>
    /// Шаблон для поиска Guid с дополнительным текстом.
    /// </summary>
    public const string GuidPatternWithAdditionalInfo = @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[.]*";
    
    /// <summary>
    /// Шаблон для поиска Guid. 
    /// </summary>
    public const string GuidPattern = @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}";
    
    /// <summary>
    /// Символ-разделитель для частей гиперссылки.
    /// </summary>
    public const char HyperlinkDelimiter = '/';
  }
}
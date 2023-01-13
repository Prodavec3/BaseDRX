using System;

namespace Sungero.Parties.Constants
{
  public static class Module
  {

    public static class HelpCodes
    {
      public const string CounterpartyInvitation = "Sungero_CounterpartyInvitation";
    }
    
    public static class RolesGuid
    {
      public static readonly Guid ExchangeServiceUsers = new Guid("5AFA06FB-3B66-4216-8681-56ACDEAC7FC1");
    }
    
    // Код системы у external link для данных инициализации.
    [Sungero.Core.Public]
    public const string InitializeExternalLinkSystem = "Initialize";

    public const string SugeroCounterpartyTableName = "Sungero_Parties_Counterparty";
    
    /// <summary>
    /// Шаблон поиска инициалов в строке, в различных вариантах написания.
    /// </summary>
    [Sungero.Core.Public]
    public const string InitialsRegex = @"(?:^|\s)+([А-Я])(?:\.|\s){0,}([А-Я])?(?:\.|\s|$)+";
  }
}
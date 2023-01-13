using System;

namespace Sungero.RecordManagement.Constants
{
  public static class Module
  {
    // Срок рассмотрения документа по умолчанию в днях.
    public const int DocumentReviewDefaultDays = 3;
    
    #region Связи
    
    // Имя типа связи "Переписка".
    [Sungero.Core.Public]
    public const string CorrespondenceRelationName = "Correspondence";
    
    // Имя типа связи "Ответное письмо".
    [Sungero.Core.Public]
    public const string ResponseRelationName = "Response";
    
    // Описание типа связи "Ответное письмо".
    [Sungero.Core.Public]
    public const string ResponseRelationDescription = "Для указания ответного письма";
    
    // Имя типа связи "Доп. соглашение".
    [Sungero.Core.Public]
    public const string SupAgreementRelationName = "SupAgreement";
    
    // Описание типа связи "Доп. соглашение".
    [Sungero.Core.Public]
    public const string SupAgreementRelationDescription = "Для указания дополнительного соглашения к договору";
    
    // Имя типа связи "Прочие".
    [Sungero.Core.Public]
    public const string SimpleRelationRelationName = "Simple relation";
    
    #endregion
    
    public static class Initialize
    {
      [Sungero.Core.Public]
      public static readonly Guid IncomingLetterKind = Guid.Parse("0002C3CB-43E1-4A01-A4FE-35ABC8994D66");
      [Sungero.Core.Public]
      public static readonly Guid OutgoingLetterKind = Guid.Parse("352EC449-E344-48EE-AD32-D0B2BABDC56E");
      [Sungero.Core.Public]
      public static readonly Guid OrderKind = Guid.Parse("8F529647-3F37-484A-B83A-A793B69D013E");
      [Sungero.Core.Public]
      public static readonly Guid CompanyDirective = Guid.Parse("8EABA48D-F32C-45F0-9367-4A2B58ACBD20");
    }

    #region Диалог заполнения пунктов составного поручения
    
    // Высота контрола текста поручения.
    [Sungero.Core.Public]
    public const int ActionItemPartTextRowsCount = 6;
    
    // Высота контрола заполнения соисполнителей.
    [Sungero.Core.Public]
    public const int CoAssigneesTextRowsCount = 3;
    
    #endregion
  }
}
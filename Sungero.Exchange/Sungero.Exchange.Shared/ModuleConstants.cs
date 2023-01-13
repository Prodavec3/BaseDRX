using System;

namespace Sungero.Exchange.Constants
{
  public static class Module
  {
    [Sungero.Core.Public]
    public const string SendSignedReceiptNotificationsId = "a050e9dc-ac0a-40c2-a322-7f1832e53f36";
    
    [Sungero.Core.Public]
    public const string CreateReceiptNotifications = "b54f0e86-0cac-49bf-b99b-30ffd8030d9b";
    
    public const string LastBoxIncomingMessageId = "LastBoxIncomingMessageId_{0}";
    public const string LastBoxOutgoingMessageId = "LastBoxOutgoingMessageId_{0}";
    
    public const string ExchangeDocument = "ExchangeDocument";
    
    public const string RoubleCurrencyCode = "643";
    
    // Имя типа связи "Приложение".
    [Sungero.Core.Public]
    public const string AddendumRelationName = "Addendum";
    
    // Имя типа связи "Прочие".
    [Sungero.Core.Public]
    public const string SimpleRelationRelationName = "Simple relation";
    
    #region Системные имена действий
    
    public const string ReviewAction = "SendForReview";
    
    public const string ApprovalAction = "SendForApproval";
    
    public const string FreeApprovalAction = "SendForFreeApproval";
    
    public const string ExecutionAction = "SendForExecution";
    
    #endregion
    
    // Операции над документом в сервисах обмена.
    public static class Exchange
    {
      /// <summary>
      /// Отправка документов.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendDocument = "ExchSendDoc";
      
      /// <summary>
      /// Отправка ответа контрагенту.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendAnswer = "ExchSendAnswer";
      
      /// <summary>
      /// Получение ответа от контрагента.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetAnswer = "ExchGetAnswer";
      
      /// <summary>
      /// Документ подписан (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedSign = "ExchSign";
      
      /// <summary>
      /// В подписании отказано (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedReject = "ExchReject";
      
      /// <summary>
      /// Отправлено уведомление об уточнении (нами или КА).
      /// </summary>
      [Sungero.Core.Public]
      public const string DetailedInvoiceReject = "ExchInvReject";
      
      /// <summary>
      /// Отправка извещения о получении.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendReadMark = "ExchReadTo";
      
      /// <summary>
      /// Получение извещения о получении.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetReadMark = "ExchReadFrom";
      
      /// <summary>
      /// Отправка уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendNoteReceiptReadMark = "ExchReadNRecTo";
      
      /// <summary>
      /// Получение уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetNoteReceiptReadMark = "ExchReadNRFrom";
      
      /// <summary>
      /// Отправка извещение о получении уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string SendRNoteReceiptReadMark = "ExchReadRNRecTo";
      
      /// <summary>
      /// Получение извещение о получении уведомления о приеме.
      /// </summary>
      [Sungero.Core.Public]
      public const string GetRNoteReceiptReadMark = "ExchReadRNRFrom";
      
      /// <summary>
      /// Документ аннулирован нашей организацией.
      /// </summary>
      [Sungero.Core.Public]
      public const string ObsoleteOur = "ExchObsoletOur";

      /// <summary>
      /// Документ аннулирован контрагентом.
      /// </summary>
      [Sungero.Core.Public]
      public const string ObsoletedByCounterparty = "ExchObsoleteCP";
      
      /// <summary>
      /// Документ отозван нашей организацией.
      /// </summary>
      [Sungero.Core.Public]
      public const string TerminateOur = "ExchTerminOur";
      
      /// <summary>
      /// Документ отозван контрагентом.
      /// </summary>
      [Sungero.Core.Public]
      public const string TerminatedByCounterparty = "ExchTerminCP";
    }
    
    /// <summary>
    /// Коды справки для действий по обмену.
    /// </summary>
    public static class HelpCodes
    {
      // Диалог отправки документа.
      public const string SendDocument = "Sungero_Exchange_SendDocumentDialog";
      
      // Диалог отправки ответа на документ.
      public const string SendAnswerOnDocument = "Sungero_Exchange_SendReplyToDocumentDialog";
    }
    
    /// <summary>
    /// Максимальный размер документа, который может быть отправлен через сервис обмена.
    /// </summary>
    public const int ExchangeDocumentMaxSize = 31457280;
    
    public const string FunctionUTDDop = "ДОП";
    
    public const string FunctionUTDDopCorrection = "ДИС";
    
    /// <summary>
    /// Коды документов по КНД.
    /// </summary>
    public static class TaxDocumentClassifier
    {
      /// <summary>
      /// Торг-12.
      /// </summary>
      [Sungero.Core.Public]
      public const string Waybill = "1175004";
      
      /// <summary>
      /// Акт.
      /// </summary>
      [Sungero.Core.Public]
      public const string Act = "1175006";
      
      /// <summary>
      /// ДПТ.
      /// </summary>
      [Sungero.Core.Public]
      public const string GoodsTransferSeller = "1175010";
      
      /// <summary>
      /// ДПРР.
      /// </summary>
      [Sungero.Core.Public]
      public const string WorksTransferSeller = "1175012";
      
      /// <summary>
      /// УПД по приказу ММВ-7-15/820.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalTransferDocumentSeller = "1115131";
      
      /// <summary>
      /// УПД по приказу ММВ-7-15/155.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalTransferDocumentSeller155 = "1115125";
      
      /// <summary>
      /// УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocumentSeller = "1115127";
    }
    
    /// <summary>
    /// Уникальные идентификаторы типа документа.
    /// </summary>
    /// <remarks>Используется в Диадок.</remarks>
    public static class DocumentTypeNamedId
    {
      /// <summary>
      /// УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocument = "UniversalCorrectionDocument";
      
      /// <summary>
      /// Исправление УКД.
      /// </summary>
      [Sungero.Core.Public]
      public const string UniversalCorrectionDocumentRevision = "UniversalCorrectionDocumentRevision";
    }
    
    /// <summary>
    /// Идентификатор версии, уникальный в рамках функции типа документа.
    /// </summary>
    /// <remarks>Используется в Диадок.</remarks>
    [Sungero.Core.Public]
    public const string UCDVersion = "ucd736_05_01_02";
    
    /// <summary>
    /// Максимальная длина пути документа, который может быть отправлен через сервис обмена.
    /// </summary>
    public const int ExchangeDocumentMaxLength = 250;
        
    /// <summary>
    /// Результат по умолчанию при вызове из схлопнутого задания на подписание и отправку.
    /// </summary>
    [Sungero.Core.Public]
    public const string DefaultSignResult = "SignAndSend";
    
    /// <summary>
    /// Количество дней для уведомления о том, что сообщение висит в очереди.
    /// </summary>
    public const int PoisonedMessagePeriod = 7;
        
    /// <summary>
    /// Ссылка на эл. доверенность в сервисе.
    /// </summary>
    [Sungero.Core.Public]
    public const string DefaultFormalizedPoALink = "https://m4d.nalog.gov.ru/";
  }
}
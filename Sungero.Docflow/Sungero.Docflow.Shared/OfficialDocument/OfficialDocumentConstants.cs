using System;

namespace Sungero.Docflow.Constants
{
  public static class OfficialDocument
  {
    #region Интеллектуальная обработка
    
    // Имя параметра: подсвечены ли свойства.
    [Sungero.Core.Public]
    public const string PropertiesAlreadyColoredParamName = "PropertiesAlreadyColored";
    
    #endregion
    
    // Параметр "Визуальный режим".
    [Sungero.Core.Public]
    public const string IsVisualModeParamName = "IsVisualMode";
    
    // Имя параметра: найден ли документ по штрихкоду.
    [Sungero.Core.Public]
    public const string FindByBarcodeParamName = "FindByBarcode";
    
    // Pdf расширение.
    [Sungero.Core.Public]
    public const string PdfExtension = "pdf";

    public static class Operation
    {
      // Изменение суммы.
      public const string TotalAmountChange = "TAChange";
      
      // Очистка суммы.
      public const string TotalAmountClear = "TAClear";
      
      // Регистрация.
      public const string Registration = "Registration";
      
      // Резервирование.
      public const string Reservation = "Reservation";
      
      // Нумерация.
      public const string Numeration = "Numeration";
      
      // Изменение рег. данных.
      public const string ChangeRegistration = "RegChange";
      
      // Изменение рег. данных.
      public const string ChangeNumeration = "NumChange";
      
      // Отмена регистрации.
      public const string Unregistration = "Unregistration";
      
      // Отмена резервирования.
      public const string Unreservation = "Unreservation";

      // Отмена нумерации.
      public const string Unnumeration = "Unnumeration";
      
      // Из сервиса обмена.
      public const string FromExchangeService = "FrmExS";
      
      // Титул покупателя.
      public const string BuyerTitle = "BuyerTitle";

      // Титул продавца. Из сервиса обмена.
      public const string SellerTitleFromExchangeService = "SlrTtlFrmExS";
      
      // Создание версии.
      public const string CreateVersion = "CreateVersion";
      
      // Преобразование в pdf.
      public const string ConvertToPdf = "ConvertToPdf";
      
      // Префиксы для статусов жизненного цикла.
      public static class Prefix
      {
        public const string InternalApproval = "En";
        
        public const string ExternalApproval = "CE";
        
        public const string Execution = "Ex";
        
        public const string ControlExecution = "C";
        
        public const string LifeCycle = "SetTo";
      }
      
      // Максимальная длина строки для перечисления.
      public const int OperationPropertyLength = 15;
      
      // Простановка отметки об ЭП (изменение содержимого).
      public const string ContentChange = "ContentChange";
    }

    public static class HelpCode
    {
      public const string Reservation = "Sungero_ReservationDialog";
      public const string Numeration = "Sungero_NumerationDialog";
      public const string Registration = "Sungero_RegistrationDialog";
      public const string Return = "Sungero_ReturnDialog";
      public const string Deliver = "Sungero_DeliverDialog";
      public const string ReturnFromCounterparty = "Sungero_ReturnFromCounterpartyDialog";
      // Код диалога создания поручений по документу.
      public const string CreateActionItems = "Sungero_Meetings_CreateActionItemsDialog";
      public const string SendByEmail = "Sungero_SendByEmailDialog";
    }

    public const string ShowParam = "showParam";
    public const string ShowOurSigningReasonParam = "showOurSigningReasonParam";
    public const string RepeatRegister = "repeatRegister";
    public const string HasReservationSetting = "hasReservationSetting";
    public const string HasNumerationSetting = "hasNumerationSetting";
    public const string NeedValidateRegisterFormat = "NeedValidateRegisterFormat";
    public const string NumberRequired = "numberRequired";
    public const string RegistrationNumberPrefix = "registrationNumberPrefix";
    public const string RegistrationNumberPostfix = "registrationNumberPostfix";
    public const string NeedRegistration = "needRegistration";
    public const string CanChangeAssignee = "canChangeAssignee";
    public const string ConvertingVersionId = "convertingVersionId";
    
    [Sungero.Core.PublicAttribute]
    public const string DontUpdateModified = "DontUpdateModified";
    
    // Выдать права на документ асинхронно.
    [Sungero.Core.PublicAttribute]
    public const string GrantAccessRightsToDocumentAsync = "GrantAccessRightsToDocumentAsync";
    
    public const string TemplateIndexLeadingSymbol = "*";
    public const string DefaultIndexLeadingSymbol = "0";
    
    public const int MaxBodySizeForInteractiveConvertation = 1048576;
    
    public static readonly Guid AllUsersSid = new Guid("440103ea-a766-47a8-98ad-5260ca32de46");
    
    [Sungero.Core.Public]
    public const string GrantAccessRightsToProjectDocument = "GrantAccessRightsToProjectDocument";
    
    // Необходимость сохранения подтвержденных значений.
    [Sungero.Core.Public]
    public const string NeedStoreVerifiedPropertiesValuesParamName = "NeedStoreVerifiedPropertiesValues";
    
    // Признак возможности подписания заблокированного документа.
    [Sungero.Core.Public]
    public const string CanSignLockedDocument = "CanSignLockedDocument";
    
    // Добавить комментарий в историю документа о конвертации.
    [Sungero.Core.Public]
    public const string AddHistoryCommentAboutPDFConvert = "AddHistoryCommentAboutPDFConvert";
    
    // Добавить комментарий в историю документа про добавление отметки о регистрации.
    [Sungero.Core.Public]
    public const string AddHistoryCommentAboutRegistrationStamp = "AddHistoryCommentAboutRegistrationStamp";
    
    // Длина, до которой сокращается имя документа в нотифайках.
    public const int NameLengthForNotification = 145;
  }
}
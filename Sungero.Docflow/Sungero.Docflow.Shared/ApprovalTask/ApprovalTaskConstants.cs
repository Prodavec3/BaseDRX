using System;

namespace Sungero.Docflow.Constants
{
  public static class ApprovalTask
  {
    public const string NeedShowExchangeServiceHint = "NeedShowExchangeServiceHint";
    
    public const string EmployeeTypeGuid = "b7905516-2be5-4931-961c-cb38d5677565";
    
    public const string AllowChangeReworkPerformer = "AllowChangeReworkPerformer";
    
    public const string AllowViewReworkPerformer = "AllowViewReworkPerformer";
    
    public const string AllowSendToRework = "AllowSendToRework";
    
    public const string CreateFromSchema = "CreateFromSchema";
    
    public const string NeedSetDocumentObsolete = "NeedSetDocumentObsolete";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на согласование.
    /// </summary>
    public static class ApprovalAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Согласовать".
      /// </summary>
      public const string Approved = "C1D5A607-0E6D-4AB0-9FF3-9B59BCD94073";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "120318FC-EF0C-422C-877A-4129C8A3D585";
      
      /// <summary>
      /// С результатом "Переадресовать".
      /// </summary>
      public const string Forward = "E2DC81D0-74CE-45E6-89A4-C3C09EB46A19";
      
      /// <summary>
      /// С результатом "Согласовать с замечаниями".
      /// </summary>
      public const string WithSuggestions = "7D3587F6-B8F5-4A8D-A987-85FD08EA2D23";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания с доработкой.
    /// </summary>
    public static class ApprovalCheckingAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Согласовать".
      /// </summary>
      public const string Accept = "C5010A4F-08BC-49A2-BB39-B675405970EC";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "361B7BEC-A331-460A-9EC9-74693E2205F0";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания контроль возврата.
    /// </summary>
    public static class ApprovalCheckReturnAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Подписан".
      /// </summary>
      public const string Signed = "9D7CC7B1-8DE8-47BD-BCEF-48369C39EDF4";
      
      /// <summary>
      /// С результатом "Не подписан".
      /// </summary>
      public const string NotSigned = "78AE5584-F2E8-402B-A244-9403C2439AC7";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания исполнение поручений.
    /// </summary>
    public static class ApprovalExecutionAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Выполнено".
      /// </summary>
      public const string Complete = "39CFF281-22EB-42D5-BE31-BE2DB6AE76BC";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "F1B70507-23AB-49DE-85C9-C9C9A293FD6C";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на согласование руководителем.
    /// </summary>
    public static class ApprovalManagerAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Согласовать".
      /// </summary>
      public const string Approved = "39CFF281-22EB-42D5-BE31-BE2DB6AE76BC";
      
      /// <summary>
      /// С результатом "Согласовать с замечаниями".
      /// </summary>
      public const string WithSuggestions = "D97B8BB3-BA47-4EC0-B45F-BCCA7CE6A42C";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "F1B70507-23AB-49DE-85C9-C9C9A293FD6C";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на печать.
    /// </summary>
    public static class ApprovalPrintingAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Выполнено".
      /// </summary>
      public const string Execute = "417CCCF0-975E-43EF-8E1F-2CD8892A78F0";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "EA1F6C0D-33B4-4A7A-9600-0204B010E447";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на регистрацию.
    /// </summary>
    public static class ApprovalRegistrationAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Выполнено".
      /// </summary>
      public const string Execute = "BC394D87-7B68-4B4C-B9B3-247761AC0A9E";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "4D161F7B-2EB3-42FE-8393-3A3BB2D8E1A6";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на рассмотрение.
    /// </summary>
    public static class ApprovalReviewAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Отправлено на исполнение".
      /// </summary>
      public const string AddActionItem = "83E50058-B750-4F76-A6A0-0ABF2B20A648";
      
      /// <summary>
      /// С результатом "Вынесена резолюция".
      /// </summary>
      public const string AddResolution = "1B75C3AD-62BE-40B7-BB11-4236FD538A99";
      
      /// <summary>
      /// С результатом "Принято к сведению".
      /// </summary>
      public const string Informed = "82A918AA-074C-45E7-ADF2-D3C699E28A56";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "009B7B7C-7EC1-484B-A3CF-EF7E0C4AE1F1";
      
      /// <summary>
      /// С результатом "Отказано".
      /// </summary>
      public const string Abort = "4A8D6084-5084-4095-AD95-FBD906C9E0AE";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на доработку.
    /// </summary>
    public static class ApprovalReworkAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Исправлено".
      /// </summary>
      public const string ForReapproving = "F734ECF4-1CDF-4F10-AEA0-1D19D1B7AD7B";
      
      /// <summary>
      /// С результатом "Переадресовать".
      /// </summary>
      public const string Forward = "3C8FADE8-AD56-40AC-AA7F-FFBC528CD726";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на отправку.
    /// </summary>
    public static class ApprovalSendingAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Выполнено".
      /// </summary>
      public const string Complete = "6ED3D4DB-B172-4DC7-B91F-F7F5A5B5CDB6";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "7613B0A0-3502-403B-B27D-7861F9B7DF81";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на подписание.
    /// </summary>
    public static class ApprovalSigningAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Подписать".
      /// </summary>
      public const string Sign = "7B3CF249-33BE-4AB6-838E-77A372D48C19";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRevision = "D1DC6384-AFC4-4A9A-B70D-CC7FF2E52DA8";
      
      /// <summary>
      /// С результатом "Отказать".
      /// </summary>
      public const string Abort = "AD4622E9-682B-45FC-A1E1-21A34D428351";
      
      /// <summary>
      /// С результатом "Подтвердить подписание".
      /// </summary>
      public const string ConfirmSign = "67F44C70-1F5E-4704-A549-D2EB8EB9FA52";
    }
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении простого задания.
    /// </summary>
    public static class ApprovalSimpleAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Выполнено".
      /// </summary>
      public const string Complete = "F6190E2A-E801-40D1-8CC1-60E279D5864F";
    }
    
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "D91AFA16-0D37-400A-AE51-2069A6F54AB1";
    
    /// <summary>
    /// ИД диалога подтверждения при рестарте задачи.
    /// </summary>
    public const string RestartConfirmDialogID = "E760BB8F-18D1-4665-A71F-FB92F682B11F";
    
    /// <summary>
    /// ИД группы приложений.
    /// </summary>
    public static readonly Guid AddendaGroupGuid = Guid.Parse("852b3e7d-f178-47d3-8fad-a64021065cfd");
        
    /// <summary>
    /// Параметры обновления формы.
    /// </summary>
    public static class RefreshApprovalTaskForm
    {
      // Имя параметра: виден ли согласуемый документ.
      public const string HasDocumentAndCanReadParamName = "ATFormHasDocumentAndCanRead";
      
      // Имя параметра: виден ли сотрудник, которому нужно переадресовать.
      public const string ForwardPerformerIsVisibleParamName = "ATFormForwardPerformerIsVisible";
      
      // Имя параметра: виден ли контрол На подпись.
      public const string SignatoryIsVisibleParamName = "ATFormSignatoryIsVisible";
      
      // Имя параметра: обязателен ли контрол На подпись.
      public const string SignatoryIsRequiredParamName = "ATFormSignatoryIsRequired";
      
      // Имя параметра: доступен ли контрол Адресат.
      public const string AddresseeIsEnabledParamName = "ATFormAddresseeIsEnabled";
      
      // Имя параметра: виден ли контрол Адресат.
      public const string AddresseeIsVisibleParamName = "ATFormAddresseeIsVisible";
      
      // Имя параметра: обязателен ли контрол Адресат.
      public const string AddresseeIsRequiredParamName = "ATFormAddresseeIsRequired";
      
      // Имя параметра: доступен ли контрол Адресаты.
      public const string AddresseesIsEnabledParamName = "ATFormAddresseesIsEnabled";
      
      // Имя параметра: виден ли контрол Адресаты.
      public const string AddresseesIsVisibleParamName = "ATFormAddresseesIsVisible";
      
      // Имя параметра: обязателен ли контрол Адресаты.
      public const string AddresseesIsRequiredParamName = "ATFormAddresseesIsRequired";
      
      // Имя параметра: доступнен ли контрол Способ доставки.
      public const string DeliveryMethodIsEnabledParamName = "ATFormDeliveryMethodIsEnabled";
      
      // Имя параметра: виден ли контрол Способ доставки.
      public const string DeliveryMethodIsVisibleParamName = "ATFormDeliveryMethodIsVisible";
      
      // Имя параметра: доступнен ли контрол Сервис обмена.
      public const string ExchangeServiceIsEnabledParamName = "ATFormExchangeServiceIsEnabled";
      
      // Имя параметра: виден ли контрол Сервис обмена.
      public const string ExchangeServiceIsVisibleParamName = "ATFormExchangeServiceIsVisible";
      
      // Имя параметра: обязателен ли контрол Способ доставки.
      public const string ExchangeServiceIsRequiredParamName = "ATFormExchangeServiceIsRequired";
      
      // Имя параметра: доступна ли колонка "Действие" в списке согласующих.
      public const string ApproversActionIsEnabledParamName = "ATFormApproversActionIsEnabled";
      
      // Имя параметра: виден ли контрол Согласующие.
      public const string ApproversIsVisibleParamName = "ATFormApproversIsVisible";
      
      // Имя параметра: виден ли контрол Дополнительные.
      public const string AddApproversIsVisibleParamName = "ATFormAddApproversIsVisible";
      
      // Имя параметра: пропустить ли обновление на событиях.
      public const string SkipRefreshEventsParamName = "ATFormSkipRefreshEvents";
    }
    
  }
}
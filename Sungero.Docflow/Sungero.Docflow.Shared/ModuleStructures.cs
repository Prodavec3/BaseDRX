using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.Module
{
  /// <summary>
  /// Ид сотрудников, получающих сводку по заданиям (используется для SQL запроса).
  /// </summary>
  partial class PerformerIds
  {
    /// <summary>
    /// Ид сотрудника.
    /// </summary>
    public int Id { get; set; }
  }
  
  /// <summary>
  /// Информация о задачах, заданиях и уведомлениях для построения сводки по сотруднику.
  /// </summary>
  [Public]
  partial class WorkflowEntityMailInfo
  {
    /// <summary>
    /// Ид сущности.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Дата создания.
    /// </summary>
    public DateTime? Created { get; set; }
    
    /// <summary>
    /// Срок.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Отображаемое значение срока.
    /// </summary>
    public string DeadlineDisplayValue { get; set; }
    
    /// <summary>
    /// Признак просроченности.
    /// </summary>
    public bool IsOverdue { get; set; }
    
    /// <summary>
    /// Признак новой сущности.
    /// </summary>
    public bool IsNew { get; set; }
    
    /// <summary>
    /// Тема.
    /// </summary>
    public string Subject { get; set; }
    
    /// <summary>
    /// Ид исполнителя.
    /// </summary>
    public int PerformerId { get; set; }
    
    /// <summary>
    /// Имя автора.
    /// </summary>
    public string AuthorName { get; set; }
    
    /// <summary>
    /// Гиперссылка.
    /// </summary>
    public string Hyperlink { get; set; }
    
    /// <summary>
    /// Признак исходящих задач в работе.
    /// </summary>
    public bool IsTasks { get; set; }
    
    /// <summary>
    /// Признак того, что сущность является уведомлением.
    /// </summary>
    public bool IsNotice { get; set; }
    
    /// <summary>
    /// Признак поручений.
    /// </summary>
    public bool IsActionItems { get; set; }
    
    /// <summary>
    /// Признак контейнера составного поручения.
    /// </summary>
    public bool IsCompoundActionItem { get; set; }
  }
  
  /// <summary>
  /// Информация по сотруднику для отправки ему писем сводки по заданиям.
  /// </summary>
  [Public]
  partial class EmployeeMailInfo
  {
    /// <summary>
    /// Ид сотрудника.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// E-mail сотрудника.
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Короткое имя сотрудника.
    /// </summary>
    public string EmployeeShortName { get; set; }
    
    /// <summary>
    /// Дата последнего рабочего дня сотрудника.
    /// </summary>
    public DateTime? LastWorkingDay { get; set; }
    
    /// <summary>
    /// Дата срока задания, после которой задание не должно попадать в сводку.
    /// </summary>
    public DateTime? PeriodLastDay { get; set; }
    
    /// <summary>
    /// Дата создания задания/уведомления, начиная с которой оно должно попасть в сводку сотрудника.
    /// </summary>
    public DateTime? PeriodFirstDay { get; set; }
    
    /// <summary>
    /// Список email-ов заместителей.
    /// </summary>
    public List<string> SubstitutorEmails { get; set; }
    
    /// <summary>
    /// Признак того, нужно ли отправлять сводку по заданиям сотруднику.
    /// </summary>
    public bool NeedNotifyAssignmentsSummary { get; set; }
    
    /// <summary>
    /// Текущая дата в часовом поясе сотрудника.
    /// </summary>
    public DateTime? EmployeeCurrentDate { get; set; }
    
    /// <summary>
    /// Информация по заданиям и уведомлениям.
    /// </summary>
    public List<Sungero.Docflow.Structures.Module.IWorkflowEntityMailInfo> AssignmentsAndNotices { get; set; }
    
    /// <summary>
    /// Информация по поручениям.
    /// </summary>
    public List<Sungero.Docflow.Structures.Module.IWorkflowEntityMailInfo> ActionItems { get; set; }
    
    /// <summary>
    /// Информация по задачам.
    /// </summary>
    public List<Sungero.Docflow.Structures.Module.IWorkflowEntityMailInfo> Tasks { get; set; }
  }
  
  /// <summary>
  /// Замещения по сотрудникам.
  /// </summary>
  [Public]
  partial class EmployeeSubstitutions
  {
    /// <summary>
    /// Замещаемый.
    /// </summary>
    public int SubstitutedId { get; set; }
    
    /// <summary>
    /// Замещающий.
    /// </summary>
    public int SubstitutorId { get; set; }
    
    /// <summary>
    /// E-mail замещающего.
    /// </summary>
    public string SubstitutorEmail { get; set; }
  }

  /// <summary>
  /// Результаты почтовой рассылки.
  /// </summary>
  partial class MailSendingResult
  {
    public bool IsSuccess { get; set; }
    
    public bool AnyMailSended { get; set; }
  }
  
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class EnvelopeReportTableLine
  {
    public string ReportSessionId { get; set; }
    
    public int Id { get; set; }
    
    public string ToName { get; set; }
    
    public string FromName { get; set; }
    
    public string ToZipCode { get; set; }

    public string FromZipCode { get; set; }
    
    public string ToPlace { get; set; }
    
    public string FromPlace { get; set; }
  }

  /// <summary>
  /// Отсортированный список этапов согласования, подходящих по условиям.
  /// </summary>
  partial class DefinedApprovalStages
  {
    public List<Sungero.Docflow.Structures.Module.DefinedApprovalStageLite> Stages { get; set; }
    
    public bool IsConditionsDefined { get; set; }
    
    public string ErrorMessage { get; set; }
  }
  
  partial class DefinedApprovalStageLite
  {
    public Sungero.Docflow.IApprovalStage Stage { get; set; }
    
    public int? Number { get; set; }
    
    public Sungero.Core.Enumeration? StageType { get; set; }
  }
  
  /// <summary>
  /// Отсортированный список базовых этапов согласования, подходящих по условиям.
  /// </summary>
  partial class DefinedApprovalBaseStages
  {
    public List<Sungero.Docflow.Structures.Module.DefinedApprovalBaseStageLite> BaseStages { get; set; }
    
    public bool IsConditionsDefined { get; set; }
    
    public string ErrorMessage { get; set; }
  }
  
  partial class DefinedApprovalBaseStageLite
  {
    public Sungero.Docflow.IApprovalStageBase StageBase { get; set; }
    
    public int? Number { get; set; }
    
    public Sungero.Core.Enumeration? StageType { get; set; }
  }
  
  /// <summary>
  /// Получатель и отправитель для конвертов.
  /// </summary>
  partial class AddresseeAndSender
  {
    public Sungero.Parties.ICounterparty Addresse { get; set; }
    
    public Sungero.Company.IBusinessUnit Sender { get; set; }
  }

  /// <summary>
  /// Индекс и адрес без индекса.
  /// </summary>
  partial class ZipCodeAndAddress
  {
    public string ZipCode { get; set; }
    
    public string Address { get; set; }
  }

  /// <summary>
  /// Даты итераций задачи.
  /// </summary>
  partial class TaskIterations
  {
    public DateTime Date { get; set; }
    
    public bool IsRework { get; set; }
    
    public bool IsRestart { get; set; }
  }
  
  /// <summary>
  /// Атрибуты содержания сертификата.
  /// </summary>
  [Public]
  partial class CertificateSubject
  {
    public string CounterpartyName { get; set; }
    
    public string Country { get; set; }
    
    public string State { get; set; }
    
    public string Locality { get; set; }
    
    public string Street { get; set; }
    
    public string Department { get; set; }
    
    public string Surname { get; set; }
    
    public string GivenName { get; set; }
    
    public string JobTitle { get; set; }
    
    public string OrganizationName { get; set; }
    
    public string Email { get; set; }
    
    public string TIN { get; set; }
  }
  
  [Public]
  partial class ByteArray
  {
    public byte[] Bytes { get; set; }
  }
  
  #region Интеллектуальная обработка
  
  /// <summary>
  /// Параметры отображения фокусировки подсветки в предпросмотре.
  /// </summary>
  [Public]
  partial class HighlightActivationStyle
  {
    public string UseBorder { get; set; }
    
    public string BorderColor { get; set; }
    
    public double BorderWidth { get; set; }
    
    public string UseFilling { get; set; }
    
    public string FillingColor { get; set; }
  }
  
  #endregion
  
  /// <summary>
  /// Параметры отправки уведомлений об истечении срока документов.
  /// </summary>
  [Public]
  partial class ExpiringDocsNotificationParams
  {
    // Дата последнего уведомления.
    public DateTime LastNotification { get; set; }
    
    // Дата последнего уведомления с резервом.
    public DateTime LastNotificationReserve { get; set; }
    
    // Дата сегодня.
    public DateTime Today { get; set; }
    
    // Дата сегодня с резервом.
    public DateTime TodayReserve { get; set; }
    
    // Количество обрабатываемых за один раз документов.
    public int BatchCount { get; set; }
    
    // Имя таблицы БД с информацией об уведомлениях.
    public string ExpiringDocTableName { get; set; }
    
    // Имя параметра в Sungero_Docflow_Params с датой последнего уведомления.
    public string LastNotificationParamName { get; set; }
    
    // Параметры задачи об истечении срока документа.
    public Sungero.Docflow.Structures.Module.IExpiringNotificationTaskParams TaskParams { get; set; }
  }
  
  /// <summary>
  /// Параметры отправки задачи об истечении срока документа.
  /// </summary>
  [Public]
  partial class ExpiringNotificationTaskParams
  {
    // Тема.
    public string Subject { get; set; }
    
    // Текст.
    public string ActiveText { get; set; }
    
    // Документ, по которому создается уведомление.
    public IOfficialDocument Document { get; set; }
    
    // Исполнители.
    public List<IUser> Performers { get; set; }
    
    // Вложения.
    public List<Sungero.Content.IElectronicDocument> Attachments { get; set; }
  }
  
  /// <summary>
  /// Соответствие документа и хранилища для хранения его содержимого.
  /// </summary>
  partial class DocumentToSetStorage
  {
    public int DocumentId { get; set; }

    public int StorageId { get; set; }
  }
  
  partial class ExportReport
  {
    public string ReportSessionId { get; set; }
    
    public string Document { get; set; }
    
    public string Hyperlink { get; set; }
    
    public int Id { get; set; }
    
    public string Exported { get; set; }
    
    public string Note { get; set; }
    
    public string IOHyperlink { get; set; }
    
    public int OrderId { get; set; }
  }
  
  [Public]
  partial class ExportDialogSearch
  {
    public Company.IBusinessUnit BusinessUnit { get; set; }
    
    public Parties.ICounterparty Counterparty { get; set; }
    
    public Contracts.IContractualDocument Contract { get; set; }
    
    public DateTime? From { get; set; }
    
    public DateTime? To { get; set; }
    
    public List<Docflow.IDocumentKind> DocumentKinds { get; set; }
  }
  
  partial class ExportDialogParams
  {
    public bool GroupCounterparty { get; set; }
    
    public bool GroupDocumentType { get; set; }
    
    public bool ForPrint { get; set; }
    
    public bool IsSingleExport { get; set; }
    
    public bool AddAddendum { get; set; }
  }
  
  partial class AfterExportDialog
  {
    public string RootFolder { get; set; }
    
    public string PathToRoot { get; set; }
    
    public DateTime? DateTime { get; set; }
    
    public List<Sungero.Docflow.Structures.Module.ExportedDocument> Documents { get; set; }
  }
  
  partial class ExportedDocument
  {
    public int Id { get; set; }
    
    public bool IsFormalized { get; set; }
    
    public bool IsAddendum { get; set; }
    
    public string ParentShortName { get; set; }
    
    public bool IsFaulted { get; set; }
    
    public bool IsPrint { get; set; }
    
    public string Error { get; set; }
    
    // Папка самого документа всегда с пустым именем, это фактически корень общий для всех.
    public Sungero.Docflow.Structures.Module.ExportedFolder Folder { get; set; }
    
    public string Name { get; set; }
    
    public int? LeadDocumentId { get; set; }
    
    public bool IsSingleExport { get; set; }
  }
  
  partial class ExportResult
  {
    public List<Sungero.Docflow.Structures.Module.ExportedDocument> ExportedDocuments { get; set; }
    
    public List<Sungero.Docflow.Structures.Module.ZipModel> ZipModels { get; set; }
  }
  
  partial class ExportedFolder
  {
    public string FolderName { get; set; }
    
    public List<Sungero.Docflow.Structures.Module.ExportedFile> Files { get; set; }
    
    public List<Sungero.Docflow.Structures.Module.ExportedFolder> Folders { get; set; }
    
    public string ParentRelativePath { get; set; }
  }
  
  partial class ExportedFile
  {
    public int Id { get; set; }
    
    public string FileName { get; set; }
    
    public byte[] Body { get; set; }
    
    public string ServicePath { get; set; }
    
    public string Token { get; set; }
  }
  
  partial class ZipModel
  {
    public int DocumentId { get; set; }
    
    public int VersionId { get; set; }
    
    public bool IsPublicBody { get; set; }
    
    public string FileName { get; set; }
    
    public List<string> FolderRelativePath { get; set; }
    
    public int? SignatureId { get; set; }
    
    public long Size { get; set; }
  }
  
  /// <summary>
  /// Период времени с/по.
  /// </summary>
  partial class DateTimePeriod
  {
    public DateTime DateFrom { get; set; }
    
    public DateTime DateTo { get; set; }
  }

  /// <summary>
  /// Информация, что в справочниках не заполнены коды.
  /// </summary>
  partial class DatabooksWithNullCode
  {
    public bool HasDepartmentWithNullCode { get; set; }
    
    public bool HasBusinessUnitWithNullCode { get; set; }
    
    public bool HasDocumentKindWithNullCode { get; set; }
  }
  
  /// <summary>
  /// Элемент истории по вложению.
  /// </summary>
  partial class AttachmentHistoryEntry
  {
    public DateTime Date { get; set; }
    
    public int DocumentId { get; set; }
    
    public Guid GroupId { get; set; }
    
    public Sungero.Core.Enumeration? OperationType { get; set; }
  }
  
  /// <summary>
  /// История по вложениям задания.
  /// </summary>
  partial class AttachmentHistoryEntries
  {
    public List<Sungero.Docflow.Structures.Module.AttachmentHistoryEntry> Added { get; set; }
    
    public List<Sungero.Docflow.Structures.Module.AttachmentHistoryEntry> Removed { get; set; }
  }
  
  /// <summary>
  /// Упрощенная модель задания для расчета исполнительской дисциплины.
  /// </summary>
  partial class LightAssignment
  {
    public int AssignmentId { get; set; }
    
    public int Performer { get; set; }
    
    public int Department { get; set; }
    
    public DateTime? Created { get; set; }
    
    public DateTime? Deadline { get; set; }
    
    public DateTime? Completed { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public bool IsCompletedInPeriod { get; set; }

    public int DelayInPeriod { get; set; }
    
    public bool AffectDiscipline { get; set; }
  }
  
  [Public]
  partial class ActiveAssignmentsDynamicPoint
  {
    public DateTime Date { get; set; }
    
    public int ActiveAssignmentsCount { get; set; }
    
    public int ActiveOverdueAssignmentsCount { get; set; }
  }
  
  /// <summary>
  /// Статистика по заданиям.
  /// </summary>
  partial class AssignmentStatistic
  {
    // Общее количество заданий.
    public int TotalAssignmentCount { get; set; }
    
    // Количество просроченных заданий в периоде.
    public int OverdueCount { get; set; }
    
    // Количество выполненных в периоде заданий.
    public int CompletedCount { get; set; }
    
    // Количество выполненных вовремя заданий.
    public int CompletedInTimeCount { get; set; }
    
    // Количество выполненных с просрочкой заданий.
    public int OverdueCompletedCount { get; set; }
    
    // Количество заданий в работе в периоде.
    public int InWorkCount { get; set; }
    
    // Количество заданий в работе и просроченных в периоде.
    public int OverdueInWorkCount { get; set; }
    
    // Количество заданий, влияющих на исполнительскую дисциплину.
    public int AffectAssignmentCount { get; set; }
  }
  
  /// <summary>
  /// Фильтраторы для отбора заданий по исполнительской дисциплине.
  /// </summary>
  partial class LightAssignmentFilter
  {
    public List<int> PerformerIds { get; set; }
    
    public bool NeedFilter { get; set; }
  }
  
  /// <summary>
  /// Подпись и версия документа.
  /// </summary>
  partial class DocumentSignature
  {
    public int SignatureId { get; set; }
    
    public DateTime SigningDate { get; set; }
    
    public int? VersionNumber { get; set; }
  }

  /// <summary>
  /// Список подписей.
  /// </summary>
  partial class SignaturesInfo
  {
    public IUser Signatory { get; set; }
    
    public IUser SubstitutedUser { get; set; }
    
    public string SignatoryType { get; set; }
  }
  
  /// <summary>
  /// Результаты поиска строки в документе.
  /// </summary>
  [Public]
  partial class PdfStringSearchResult
  {
    /// <summary>
    /// Отступ слева до найденной строки в сантиметрах.
    /// </summary>
    public double XIndent { get; set; }
    
    /// <summary>
    /// Отступ снизу до найденной строки в сантиметрах.
    /// </summary>
    public double YIndent { get; set; }
    
    /// <summary>
    /// Номер страницы, на которой была найдена строка.
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Ширина страницы, на которой была найдена строка, в сантиметрах.
    /// </summary>
    public double PageWidth { get; set; }
    
    /// <summary>
    /// Высота страницы, на которой была найдена строка, в сантиметрах.
    /// </summary>
    public double PageHeight { get; set; }
    
    /// <summary>
    /// Количество страниц в документе.
    /// </summary>
    public int PageCount { get; set; }
  }
  
}
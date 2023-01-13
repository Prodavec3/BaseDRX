using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class Module
  {

    // Наименования для тела письма с электронной почты.
    public static class DcsMailBodyName
    {
      [Sungero.Core.Public]
      public const string Html = "body.html";
      
      [Sungero.Core.Public]
      public const string Txt = "body.txt";
    }
    
    /// <summary>
    /// Наименования фактов и полей фактов в правилах извлечения фактов Ario.
    /// </summary>
    /// <remarks>Составлен для версии Ario 1.7.</remarks>
    public static class ArioGrammars
    {
      /// <summary>
      /// Факт "Письмо".
      /// </summary>
      public static class LetterFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Letter";
        
        /// <summary>
        /// Адресат письма.
        /// </summary>
        /// <remarks>Содержит информацию в формате "Фамилия И.О." или "Фамилия Имя Отчество".</remarks>
        [Sungero.Core.Public]
        public const string AddresseeField = "Addressee";
        
        /// <summary>
        /// Гриф доступа.
        /// </summary>
        /// <remarks>Гриф "Конфиденциально", "Для служебного пользования", "Коммерческая тайна".</remarks>
        [Sungero.Core.Public]
        public const string ConfidentialField = "Confidential";
        
        /// <summary>
        /// Организационно-правовая форма корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentLegalFormField = "CorrespondentLegalForm";
        
        /// <summary>
        /// Наименование корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentNameField = "CorrespondentName";

        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// В ответ на дату документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToDateField = "ResponseToDate";
        
        /// <summary>
        /// В ответ на номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToNumberField = "ResponseToNumber";
        
        /// <summary>
        /// Тема документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SubjectField = "Subject";
        
        /// <summary>
        /// ИНН.
        /// </summary>
        [Sungero.Core.Public]
        public const string TinField = "TIN";
        
        /// <summary>
        /// Признак "ИНН корректен".
        /// </summary>
        [Sungero.Core.Public]
        public const string TinIsValidField = "TinIsValid";
        
        /// <summary>
        /// КПП.
        /// </summary>
        [Sungero.Core.Public]
        public const string TrrcField = "TRRC";

        /// <summary>
        /// ОГРН.
        /// </summary>
        [Sungero.Core.Public]
        public const string PsrnField = "PSRN";

        /// <summary>
        /// Головная организация.
        /// </summary>
        [Sungero.Core.Public]
        public const string HeadCompanyNameField = "CorrHeadCompanyName";
        
        /// <summary>
        /// Адрес эл. почты.
        /// </summary>
        [Sungero.Core.Public]
        public const string EmailField = "Email";

        /// <summary>
        /// Номер телефона.
        /// </summary>
        [Sungero.Core.Public]
        public const string PhoneField = "Phone";

        /// <summary>
        /// Веб-сайт.
        /// </summary>
        [Sungero.Core.Public]
        public const string WebsiteField = "Website";

        /// <summary>
        /// Тип корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string TypeField = "Type";

        /// <summary>
        /// Типы корреспондентов: "Корреспондент", "Адресат".
        /// </summary>
        public static class CorrespondentTypes
        {
          [Sungero.Core.Public]
          public const string Correspondent = "CORRESPONDENT";
          
          [Sungero.Core.Public]
          public const string Recipient = "RECIPIENT";
        }

      }
      
      /// <summary>
      /// Факт "Персона письма".
      /// </summary>
      public static class LetterPersonFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "LetterPerson";
        
        /// <summary>
        /// Имя.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Отчество.
        /// </summary>
        [Sungero.Core.Public]
        public const string PatrnField = "Patrn";
        
        /// <summary>
        /// Фамилия.
        /// </summary>
        [Sungero.Core.Public]
        public const string SurnameField = "Surname";
        
        /// <summary>
        /// Тип персоны.
        /// </summary>
        [Sungero.Core.Public]
        public const string TypeField = "Type";
        
        /// <summary>
        /// Типы персоны: "Подписант", "Исполнитель".
        /// </summary>
        public static class PersonTypes
        {
          [Sungero.Core.Public]
          public const string Signatory = "SIGNATORY";
          
          [Sungero.Core.Public]
          public const string Responsible = "RESPONSIBLE";
        }
      }
      
      /// <summary>
      /// Факт "Контрагент".
      /// </summary>
      public static class CounterpartyFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Counterparty";
        
        /// <summary>
        /// Расчетный счет.
        /// </summary>
        [Sungero.Core.Public]
        public const string BankAccountField = "BankAccount";
        
        /// <summary>
        /// БИК.
        /// </summary>
        [Sungero.Core.Public]
        public const string BinField = "BIN";
        
        /// <summary>
        /// Тип контрагента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CounterpartyTypeField = "CounterpartyType";
        
        /// <summary>
        /// Типы контрагента.
        /// </summary>
        public static class CounterpartyTypes
        {
          [Sungero.Core.Public]
          public const string Consignee = "CONSIGNEE";
          
          [Sungero.Core.Public]
          public const string Payer = "PAYER";
          
          [Sungero.Core.Public]
          public const string Shipper = "SHIPPER";
          
          [Sungero.Core.Public]
          public const string Supplier = "SUPPLIER";
          
          [Sungero.Core.Public]
          public const string Buyer = "BUYER";
          
          [Sungero.Core.Public]
          public const string Seller = "SELLER";
        }
        
        /// <summary>
        /// Организационно-правовая форма.
        /// </summary>
        [Sungero.Core.Public]
        public const string LegalFormField = "LegalForm";
        
        /// <summary>
        /// Наименование.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Имя подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryNameField = "SignatoryName";
        
        /// <summary>
        /// Отчество подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryPatrnField = "SignatoryPatrn";
        
        /// <summary>
        /// Фамилия подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatorySurnameField = "SignatorySurname";
        
        /// <summary>
        /// ИНН.
        /// </summary>
        [Sungero.Core.Public]
        public const string TinField = "TIN";
        
        /// <summary>
        /// Признак "ИНН корректен".
        /// </summary>
        [Sungero.Core.Public]
        public const string TinIsValidField = "TinIsValid";
        
        /// <summary>
        /// КПП.
        /// </summary>
        [Sungero.Core.Public]
        public const string TrrcField = "TRRC";
        
        /// <summary>
        /// ОГРН.
        /// </summary>
        [Sungero.Core.Public]
        public const string PsrnField = "PSRN";
      }
      
      /// <summary>
      /// Факт "Документ".
      /// </summary>
      /// <remarks>Используется в договорах, актах.</remarks>
      public static class DocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Document";
        
        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Дополнительное соглашение".
      /// </summary>
      public static class SupAgreementFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "SupAgreement";
        
        /// <summary>
        /// Номер ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Сумма документа".
      /// </summary>
      public static class DocumentAmountFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "DocumentAmount";
        
        /// <summary>
        /// Целая часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
        
        /// <summary>
        /// Дробная часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountCentsField = "AmountCents";
        
        /// <summary>
        /// Валюта.
        /// </summary>
        [Sungero.Core.Public]
        public const string CurrencyField = "Currency";
        
        /// <summary>
        /// Целая часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Дробная часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountCentsField = "VatAmountCents";
      }
      
      /// <summary>
      /// Факт "Финансовый документ".
      /// </summary>
      public static class FinancialDocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "FinancialDocument";
        
        /// <summary>
        /// Наименование ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNameField = "DocumentBaseName";
        
        /// <summary>
        /// Номер ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// Дата корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionDateField = "CorrectionDate";
        
        /// <summary>
        /// Номер корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionNumberField = "CorrectionNumber";
        
        /// <summary>
        /// Номер исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionNumberField = "RevisionNumber";
        
        /// <summary>
        /// Дата исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionDateField = "RevisionDate";
        
        /// <summary>
        /// Номер исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionNumberField = "CorrectionRevisionNumber";
        
        /// <summary>
        /// Дата исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionDateField = "CorrectionRevisionDate";
      }
      
      /// <summary>
      /// Факт "Номенклатура".
      /// </summary>
      public static class GoodsFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Goods";
        
        /// <summary>
        /// Наименование товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Количество (объем) товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string CountField = "Count";
        
        /// <summary>
        /// Наименование, условное обозначение единицы измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string UnitNameField = "UnitName";
        
        /// <summary>
        /// Цена за единицу измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string PriceField = "Price";
        
        /// <summary>
        /// Сумма НДС, по товару.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Стоимость с НДС, товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
      }
    }
    
    // Уровни вероятности.
    public static class PropertyProbabilityLevels
    {
      [Sungero.Core.Public]
      public const double Max = 90;

      [Sungero.Core.Public]
      public const double UpperMiddle = 75;
      
      [Sungero.Core.Public]
      public const double Middle = 50;
      
      [Sungero.Core.Public]
      public const double LowerMiddle = 25;
      
      [Sungero.Core.Public]
      public const double Min = 5;
    }
    
    // Html расширение.
    public static class HtmlExtension
    {
      [Sungero.Core.Public]
      public const string WithDot = ".html";
      
      [Sungero.Core.Public]
      public const string WithoutDot = "html";
    }
    
    // Html теги.
    public static class HtmlTags
    {
      [Sungero.Core.Public]
      public const string MaskForSearch = "<html";
      
      [Sungero.Core.Public]
      public const string StartTag = "<html>";
      
      [Sungero.Core.Public]
      public const string EndTag = "</html>";
    }
    
    /// <summary>
    /// Статусы задачи на обработку файла.
    /// </summary>
    /// <remarks>Значения статусов Task.State: 0 - Новая, 1 - В работе, 2 - Завершена,
    /// 3 - Произошла ошибка, 4 - Обучение завершено, 5 - Прекращена.</remarks>
    public static class ProcessingTaskStates
    {
      // Новая.
      [Sungero.Core.Public]
      public const int New = 0;

      // В работе.
      [Sungero.Core.Public]
      public const int InWork = 1;
      
      // Завершена.
      [Sungero.Core.Public]
      public const int Completed = 2;
      
      // Произошла ошибка.
      [Sungero.Core.Public]
      public const int ErrorOccurred = 3;
      
      // Обучение завершено.
      [Sungero.Core.Public]
      public const int TrainingCompleted = 4;
      
      // Прекращена.
      [Sungero.Core.Public]
      public const int Terminated = 5;
    }
    
    /// <summary>
    /// Минимальное время обработки одного документа в Ario.
    /// </summary>
    public const int ArioDocumentProcessingMinDuration = 20;
    
    /// <summary>
    /// Минимальный промежуток обращения к Ario.
    /// </summary>
    public const int ArioAccessMinPeriod = 5;
    
  }
}
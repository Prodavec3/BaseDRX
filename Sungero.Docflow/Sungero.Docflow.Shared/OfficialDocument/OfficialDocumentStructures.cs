using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.OfficialDocument
{
  /// <summary>
  /// Адресат для отправки по электронной почте.
  /// </summary>
  [Public]
  partial class EmailAddressee
  {
    // Наименование адресата.
    public string Label { get; set; }
    
    // Электронная почта.
    public string Email { get; set; }
  }
  
  partial class DialogResult
  {
    public IDocumentRegister Register { get; set; }
    
    public DateTime Date { get; set; }
    
    public string Number { get; set; }
  }
  
  [Obsolete("Используйте структуру DialogParamsLite.")]
  partial class DialogParams
  {
    public List<IDocumentRegister> Registers { get; set; }
    
    public Sungero.Core.Enumeration Operation { get; set; }
    
    public IDocumentRegister DefaultRegister { get; set; }
    
    public string CurrentRegistrationNumber { get; set; }
    
    public DateTime? CurrentRegistrationDate { get; set; }
    
    public string NextNumber { get; set; }
    
    public int LeadId { get; set; }
    
    public string LeadNumber { get; set; }
    
    public bool IsNumberValidationDisabled { get; set; }
    
    public int DepartmentId { get; set; }
    
    public string DepartmentCode { get; set; }
    
    public string BusinessUnitCode { get; set; }
    
    public int BusinessUnitId { get; set; }
    
    public string CaseFileIndex { get; set; }
    
    public string DocKindCode { get; set; }
    
    public string CounterpartyCode { get; set; }
    
    public bool IsClerk { get; set; }
  }
  
  /// <summary>
  /// Параметры диалога регистрации.
  /// </summary>
  [Public]
  partial class DialogParamsLite
  {
    // ИД доступных журналов регистрации.
    public List<int> RegistersIds { get; set; }
    
    // Тип регистрации (нумерация, резервирование, регистрация).
    public Sungero.Core.Enumeration Operation { get; set; }
    
    // Журнал по умолчанию.
    public IDocumentRegister DefaultRegister { get; set; }
    
    // Текущий регистрационный номер.
    public string CurrentRegistrationNumber { get; set; }
    
    // Текущая дата регистрации.
    public DateTime? CurrentRegistrationDate { get; set; }
    
    // Следующий регистрационный номер.
    public string NextNumber { get; set; }
    
    // ИД ведущего документа.
    public int LeadId { get; set; }
    
    // Номер ведущего документа.
    public string LeadNumber { get; set; }
    
    // Прзнак, что валидация номера отключена.
    public bool IsNumberValidationDisabled { get; set; }
    
    // ИД подразделения.
    public int DepartmentId { get; set; }
    
    // Код подразделения.
    public string DepartmentCode { get; set; }
    
    // Код нашей организации.
    public string BusinessUnitCode { get; set; }
    
    // ИД нашей организации.
    public int BusinessUnitId { get; set; }
    
    // Индекс дела, в которое будет помещён документ.
    public string CaseFileIndex { get; set; }
    
    // Код вида документа.
    public string DocKindCode { get; set; }
    
    // Код контрагента.
    public string CounterpartyCode { get; set; }
    
    // Признак, что текущий пользователь может зарегистрировать документ.
    public bool IsClerk { get; set; }
  }
  
  /// <summary>
  /// Результат преобразования документа в PDF.
  /// </summary>
  [SuppressMessage("AppliedStylecopNamingRules.ApiNamingAnalyzer", "CR0001:ApiNamesMustNotContainCyrillic", Justification = "Deferred tech debt, #122325")]
  partial class СonversionToPdfResult
  {
    public bool IsFastConvertion { get; set; }
    
    public bool IsOnConvertion { get; set; }
    
    public bool HasErrors { get; set; }
    
    public bool HasConvertionError { get; set; }
    
    public bool HasLockError { get; set; }
    
    public string ErrorTitle { get; set; }
    
    public string ErrorMessage { get; set; }
  }
  
  /// <summary>
  /// Распознанное свойство.
  /// </summary>
  partial class RecognizedProperty
  {
    // Наименование.
    public string Name { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    // Позиция.
    public string Position { get; set; }
  }
  
  /// <summary>
  /// Параметры, получаемые с сервера, для клиентских событий OfficialDocument.
  /// </summary>
  [Public]
  partial class OfficialDocumentParams
  {
    public bool? HasReservationSetting { get; set; }
    
    public bool? HasNumerationSetting { get; set; }
    
    public bool? NeedShowRegistrationPane { get; set; }
    
    public bool? CanChangeAssignee { get; set; }
  }
}
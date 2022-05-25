using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchive.Structures.Module
{
  /// <summary>
  /// Результат импорта XML.
  /// </summary>
  [Public]
  partial class ImportResult
  {
    /// <summary>
    /// Импорт прошел успешно.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Текст ошибки при неудачном импорте.
    /// </summary>
    public string Error { get; set; }
    
    /// <summary>
    /// Документ.
    /// </summary>
    public Docflow.IAccountingDocumentBase Document { get; set; }
    
    /// <summary>
    /// Тело для версии документа.
    /// </summary>
    public byte[] Body { get; set; }
    
    /// <summary>
    /// Публичное тело (PDF) для версии документа.
    /// </summary>
    public byte[] PublicBody { get; set; }
    
    /// <summary>
    /// Название организации.
    /// </summary>
    public string BusinessUnitName { get; set; }
    
    /// <summary>
    /// ИНН.
    /// </summary>
    public string BusinessUnitTin { get; set; }

    /// <summary>
    /// КПП, если есть.
    /// </summary>
    public string BusinessUnitTrrc { get; set; }

    /// <summary>
    /// Тип организации.
    /// </summary>
    public string BusinessUnitType { get; set; }
    
    /// <summary>
    /// Организация найдена в RX.
    /// </summary>
    public bool IsBusinessUnitFound { get; set; }

    /// <summary>
    /// Название организации.
    /// </summary>
    public string CounterpartyName { get; set; }
    
    /// <summary>
    /// ИНН.
    /// </summary>
    public string CounterpartyTin { get; set; }

    /// <summary>
    /// КПП, если есть.
    /// </summary>
    public string CounterpartyTrrc { get; set; }

    /// <summary>
    /// Тип организации.
    /// </summary>
    public string CounterpartyType { get; set; }
    
    /// <summary>
    /// Организация найдена в RX.
    /// </summary>
    public bool IsCounterpartyFound { get; set; }
    
    /// <summary>
    /// Организация поддерживает эл. обмен.
    /// </summary>
    public bool IsCounterpartyCanExchange { get; set; }

    /// <summary>
    /// ФНС ИД продавца.
    /// </summary>
    public string SenderFtsId { get; set; }
    
    /// <summary>
    /// ФНС ИД покупателя.
    /// </summary>
    public string ReceiverFtsId { get; set; }
  }
}
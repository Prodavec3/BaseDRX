using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ExchangeServiceDocumentReport
{
  /// <summary>
  /// Строка таблицы.
  /// </summary>
  partial class ExchangeServiceDocumentTableLine
  {
    /// <summary>
    /// ИД текущего запуска отчета.
    /// </summary>
    public string ReportSessionId { get; set; }
    
    /// <summary>
    /// НОР.
    /// </summary>
    public string BusinessUnitName { get; set; }
    
    /// <summary>
    /// Ид НОР.
    /// </summary>
    public int BusinessUnitId { get; set; }
    
    /// <summary>
    /// Наименование документа.
    /// </summary>
    public string DocName { get; set; }
    
    /// <summary>
    /// Ид документа.
    /// </summary>
    public int DocId { get; set; }
    
    /// <summary>
    /// Контрагент.
    /// </summary>
    public string Counterparty { get; set; }
    
    /// <summary>
    /// Сервис обмена.
    /// </summary>
    public string ExchangeService { get; set; }
    
    /// <summary>
    /// Ответственный.
    /// </summary>
    public string Responsible { get; set; }
    
    /// <summary>
    /// Подразделение ответственного.
    /// </summary>
    public string Department { get; set; }
    
    /// <summary>
    /// Дата отправки.
    /// </summary>
    public string SendDate { get; set; }
    
    /// <summary>
    /// Ссылка на документ.
    /// </summary>
    public string Hyperlink { get; set; }
    
    /// <summary>
    /// Статус.
    /// </summary>
    public string Delay { get; set; }
  }
}
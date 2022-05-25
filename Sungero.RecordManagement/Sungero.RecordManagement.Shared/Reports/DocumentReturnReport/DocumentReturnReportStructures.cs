using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.DocumentReturnReport
{

  /// <summary>
  /// Строка таблицы.
  /// </summary>
  partial class DocumentReturnTableLine
  {
    /// <summary>
    /// ИД сессии.
    /// </summary>
    public string ReportSessionId { get; set; }
    
    /// <summary>
    /// Имя сотрудника.
    /// </summary>
    public string FullName { get; set; }
    
    /// <summary>
    /// Наименование документа.
    /// </summary>
    public string DocName { get; set; }
    
    /// <summary>
    /// Оригинал документа или копия.
    /// </summary>
    public string OriginalOrCopy { get; set; }
    
    /// <summary>
    /// Дата передачи.
    /// </summary>
    public string DeliveryDate { get; set; }
    
    /// <summary>
    /// Планируемая дата возврата.
    /// </summary>
    public string ScheduledReturnDate { get; set; }
    
    /// <summary>
    /// Просрочка в днях.
    /// </summary>
    public int OverdueDelay { get; set; }
    
    /// <summary>
    /// Ид документа.
    /// </summary>
    public int DocId { get; set; }
    
    /// <summary>
    /// Ссылка на документ.
    /// </summary>
    public string Hyperlink { get; set; }
    
    /// <summary>
    /// Наименование подразделения.
    /// </summary>
    public string DepName { get; set; }
    
    /// <summary>
    /// Ид подразделения.
    /// </summary>
    public int DepId { get; set; }
  }

}
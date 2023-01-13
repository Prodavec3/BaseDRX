using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.InternalDocumentsReport
{
  /// <summary>
  /// Данные по документу для отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public int LineNumber { get; set; }
    
    public DateTime? RegistrationDate { get; set; }
    
    public string RegistrationNumber { get; set; }
    
    public string PreparedByName { get; set; }
    
    public string PreparedByDepartmentShortName { get; set; }
    
    public string PreparedByDepartmentName { get; set; }
    
    public string Subject { get; set; }
    
    public bool CanRead { get; set; }
  }
}
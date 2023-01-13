using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.OutgoingDocumentsReport
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
    
    public string Addressee { get; set; }
    
    public string DepartmentShortName { get; set; }
    
    public string AssigneeName { get; set; }
    
    public string AssigneeDepartmentShortName { get; set; }
    
    public string AssigneeDepartmentName { get; set; }
    
    public string Subject { get; set; }
    
    public string Note { get; set; }
    
    public bool CanRead { get; set; }
  }
}
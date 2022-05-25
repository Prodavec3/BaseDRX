using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DistributionSheetReport
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public string CompanyName { get; set; }
    
    public string NameWithJobTitle { get; set; }
    
    public string DeliveryMethod { get; set; }
    
    public string ContactsInformation { get; set; }
  }
}
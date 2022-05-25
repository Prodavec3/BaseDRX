using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.AcquaintanceFormReport
{
  partial class TableLine
  {
    public int RowNumber { get; set; }
    
    public string ShortName { get; set; }
    
    public string LastName { get; set; }

    public string JobTitle { get; set; }

    public string Department { get; set; }
    
    public string ReportSessionId { get; set; }
  }   
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.IncomingDocumentsProcessingReport
{

  /// <summary>
  /// Ссылка на документ.
  /// </summary>
  partial class Hyperlinks
  {
    public int DocId { get; set; }
    
    public string Hyperlink { get; set; }
  }

  /// <summary>
  /// Ссылка на документ.
  /// </summary>
  partial class ViewDates
  {
    public int AssignmentId { get; set; }
    
    public DateTime ViewDate { get; set; }
  }
}
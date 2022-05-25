using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow.Client
{
  partial class ApprovalManagerAssignmentFunctions
  {
    /// <summary>
    /// Показывать сводку по документу.
    /// </summary>
    /// <returns>True, если в задании нужно показывать сводку по документу.</returns>
    [Public]
    public virtual bool NeedViewDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
        return false;
      
      return Docflow.PublicFunctions.OfficialDocument.NeedViewDocumentSummary(document);
    }

  }
}
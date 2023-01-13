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
    
    /// <summary>
    /// Получить список дополнительных согласующих.
    /// </summary>
    /// <returns>Список дополнительных согласующих.</returns>
    public virtual List<IRecipient> GetAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();
      if (_obj.Signatory != null)
        assignees.Add(_obj.Signatory);
      assignees.AddRange(_obj.AddApprovers.Where(a => a.Approver != null).Select(a => a.Approver));
      return assignees;
    }
  }
}
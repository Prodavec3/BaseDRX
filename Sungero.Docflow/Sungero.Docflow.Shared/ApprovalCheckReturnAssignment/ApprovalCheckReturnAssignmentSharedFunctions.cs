using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalCheckReturnAssignment;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalCheckReturnAssignmentFunctions
  {
    /// <summary>
    /// Проверить возможность выполнения задания.
    /// </summary>
    /// <returns>True, если можно выполнить задание, иначе - false.</returns>
    public bool CanCompleteAssignment()
    {
      var doc = _obj.DocumentGroup.OfficialDocuments.First();
      var tracking = doc.Tracking.Any(x => x.Action == Docflow.OfficialDocumentTracking.Action.Endorsement &&
                                                 Equals(x.ReturnTask, _obj.Task) &&
                                                 x.ExternalLinkId != null &&
                                                 x.ReturnResult == null);
      
      return !tracking;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalReworkAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalReworkAssignmentApproversSharedCollectionHandlers
  {

    public virtual void ApproversAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Action = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Action.SendForApproval;
      _added.Approved = Sungero.Docflow.FreeApprovalReworkAssignmentApprovers.Approved.NotApproved;
      if (_added.State.IsCopied == true)
        _added.Approver = null;
    }
  }
}
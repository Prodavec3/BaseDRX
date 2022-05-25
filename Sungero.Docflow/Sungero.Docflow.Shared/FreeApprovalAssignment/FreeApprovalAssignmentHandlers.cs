using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalAssignment;

namespace Sungero.Docflow
{
  partial class FreeApprovalAssignmentSharedHandlers
  {

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.FreeApprovalAssignmentAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && e.OldValue == null &&
          _obj.AddresseeDeadline == null &&
          Docflow.PublicFunctions.Module.CheckDeadline(_obj.Deadline, Calendar.Now))
        _obj.AddresseeDeadline = _obj.Deadline;
      if (e.NewValue == null && _obj.AddresseeDeadline != null)
        _obj.AddresseeDeadline = null;
      
      // Срок для переадресации нужен, если у самого задания есть срок, плюс указан сотрудник, которому переадресуют.
      _obj.State.Properties.AddresseeDeadline.IsRequired = _obj.Deadline.HasValue && _obj.Addressee != null;
      _obj.State.Properties.AddresseeDeadline.IsEnabled = e.NewValue != null;
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow
{
  partial class ConditionBaseSharedHandlers
  {
    
    public virtual void ConditionTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.ConditionBase.ChangePropertiesAccess(_obj);
      
      Functions.ConditionBase.ClearHiddenProperties(_obj);
      
      if (e.NewValue != e.OldValue && (e.NewValue == Docflow.ConditionBase.ConditionType.EmployeeInRole || e.OldValue == Docflow.ConditionBase.ConditionType.EmployeeInRole))
        _obj.ApprovalRole = null;
    }
  }
}
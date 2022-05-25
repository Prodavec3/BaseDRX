using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CaseFile;

namespace Sungero.Docflow
{
  partial class CaseFileSharedHandlers
  {
    
    public virtual void RegistrationGroupChanged(Sungero.Docflow.Shared.CaseFileRegistrationGroupChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var isSubstituteParamName = Constants.Module.IsSubstituteResponsibleEmployeeParamName;
      if (e.Params.Contains(isSubstituteParamName))
        e.Params.Remove(isSubstituteParamName);
      
      var isAdministratorParamName = Constants.Module.IsAdministratorParamName;
      if (e.Params.Contains(isAdministratorParamName))
        e.Params.Remove(isAdministratorParamName);
    }
  }
}
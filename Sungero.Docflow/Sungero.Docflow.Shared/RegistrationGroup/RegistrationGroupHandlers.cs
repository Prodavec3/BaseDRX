using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationGroup;

namespace Sungero.Docflow
{

  partial class RegistrationGroupRecipientLinksSharedCollectionHandlers
  {

    public override void RecipientLinksDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e) 
    {
      if (Equals(_deleted.Member, _obj.ResponsibleEmployee))
        _obj.ResponsibleEmployee = null;      
    }
  }

  partial class RegistrationGroupSharedHandlers
  {

    public virtual void ResponsibleEmployeeChanged(Sungero.Docflow.Shared.RegistrationGroupResponsibleEmployeeChangedEventArgs e)
    {
      // Добавить ответственного в состав группы.
      var responsible = e.NewValue;
      if (responsible != null && !_obj.RecipientLinks.Any(r => Equals(r.Member, responsible)))
        _obj.RecipientLinks.AddNew().Member = responsible;
      
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
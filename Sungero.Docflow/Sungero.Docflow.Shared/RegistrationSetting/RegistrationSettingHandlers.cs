using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow
{
  partial class RegistrationSettingSharedHandlers
  {
    
    public virtual void DocumentRegisterChanged(Sungero.Docflow.Shared.RegistrationSettingDocumentRegisterChangedEventArgs e)
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
    
    public virtual void SettingTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      _obj.State.Properties.DocumentRegister.IsEnabled = e.NewValue != null;
      _obj.State.Properties.DocumentKinds.IsEnabled = e.NewValue != null;
      
      var docRegisterTypeChanged = ((e.OldValue == SettingType.Numeration) || (e.NewValue == SettingType.Numeration)) && (e.NewValue != e.OldValue);
      if (e.NewValue == null || docRegisterTypeChanged)
      {
        _obj.DocumentRegister = null;
        _obj.DocumentKinds.Clear();
      }
      
      // Для входящих документов резервирование не имеет смысла.
      if (e.NewValue == SettingType.Reservation && _obj.DocumentFlow == DocumentFlow.Incoming)
        _obj.DocumentFlow = null;
    }

    public virtual void DocumentFlowChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      _obj.State.Properties.DocumentRegister.IsEnabled = e.NewValue != null;
      _obj.State.Properties.DocumentKinds.IsEnabled = e.NewValue != null;
      
      if (e.NewValue == null || !Equals(e.NewValue, e.OldValue))
      {
        _obj.DocumentRegister = null;
        _obj.DocumentKinds.Clear();
      }
    }
  }
}
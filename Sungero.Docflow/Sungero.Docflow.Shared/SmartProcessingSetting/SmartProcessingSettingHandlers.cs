using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;

namespace Sungero.Docflow
{
  partial class SmartProcessingSettingSharedHandlers
  {

    public virtual void LanguagesChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;

      var trimmedLanguages = e.NewValue.Trim();
      if (e.NewValue == trimmedLanguages)
        return;
      
      _obj.Languages = trimmedLanguages;
    }

    public virtual void LoginChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      _obj.Password = string.Empty;
      
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var trimmedLogin = e.NewValue.Trim();
      if (e.NewValue == trimmedLogin)
        return;
      
      _obj.Login = trimmedLogin;
    }
    
    public virtual void ArioUrlChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var trimmedArioUrl = e.NewValue.Trim();
      if (e.NewValue == trimmedArioUrl)
        return;
      
      _obj.ArioUrl = trimmedArioUrl;
    }

  }
}
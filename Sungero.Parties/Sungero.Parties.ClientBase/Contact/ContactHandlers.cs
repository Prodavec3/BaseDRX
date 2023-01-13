using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties
{
  partial class ContactClientHandlers
  {

    public virtual void SigningReasonValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
       if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var trimmedSigningReason = e.NewValue.Trim();
      if (e.NewValue == trimmedSigningReason)
        return;
      
      _obj.SigningReason = trimmedSigningReason;
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Заблокировать ФИО, если есть персона.
      if (_obj.Person != null)
        _obj.State.Properties.Name.IsEnabled = false;
    }

    public virtual void EmailValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(e.NewValue) && !Functions.Module.EmailIsValid(e.NewValue))
        e.AddWarning(Resources.WrongEmailFormat);
    }
  }
}
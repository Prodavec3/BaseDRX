using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties
{
  partial class ContactSharedHandlers
  {

    public virtual void PersonChanged(Sungero.Parties.Shared.ContactPersonChangedEventArgs e)
    {
      // Заполнение данных в соответствии с данными Персоны.
      if (_obj.Person != null)
      {
        _obj.State.Properties.Name.IsEnabled = false;
        _obj.Name = e.NewValue.Name;
        _obj.Phone = e.NewValue.Phones;
        _obj.Email = e.NewValue.Email;
      }
      else
      {
        _obj.State.Properties.Name.IsEnabled = true;
        _obj.Name = string.Empty;
        _obj.Phone = string.Empty;
        _obj.Email = string.Empty;
      }
    }
  }
}
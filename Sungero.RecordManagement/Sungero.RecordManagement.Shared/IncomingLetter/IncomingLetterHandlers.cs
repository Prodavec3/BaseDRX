using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.IncomingLetter;

namespace Sungero.RecordManagement
{
  partial class IncomingLetterSharedHandlers
  {
    public virtual void ContactChanged(Sungero.RecordManagement.Shared.IncomingLetterContactChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        _obj.Correspondent = e.NewValue.Company;
    }

    public virtual void SignedByChanged(Sungero.RecordManagement.Shared.IncomingLetterSignedByChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        _obj.Correspondent = e.NewValue.Company;
    }

    public override void CorrespondentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseCorrespondentChangedEventArgs e)
    {
      base.CorrespondentChanged(e);
      FillName();
      if (Sungero.Parties.People.Is(e.NewValue))
      {
        _obj.State.Properties.SignedBy.IsEnabled = false;
        _obj.SignedBy = null;
        _obj.State.Properties.Contact.IsEnabled = false;
        _obj.Contact = null;
      }
      else
      {
        _obj.State.Properties.SignedBy.IsEnabled = true;
        _obj.State.Properties.Contact.IsEnabled = true;
        if (_obj.SignedBy != null && !Equals(e.NewValue, _obj.SignedBy.Company))
          _obj.SignedBy = null;
        if (_obj.Contact != null && !Equals(e.NewValue, _obj.Contact.Company))
          _obj.Contact = null;
      }
    }
  }
}
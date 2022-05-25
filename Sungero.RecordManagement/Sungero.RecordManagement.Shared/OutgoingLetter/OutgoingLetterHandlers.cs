using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.OutgoingLetter;

namespace Sungero.RecordManagement
{
  partial class OutgoingLetterSharedHandlers
  {

    public override void CorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseCorrespondentChangedEventArgs e) 
    {
      base.CorrespondentChanged(e);
      FillName();
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e) 
    {
      base.SubjectChanged(e);
    }

  }
}
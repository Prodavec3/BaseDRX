using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow
{
  partial class SimpleDocumentSharedHandlers
  {

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      // Заполнить имя.
      Functions.OfficialDocument.FillName(_obj);
    }

  }
}
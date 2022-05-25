using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ExchangeDocument;

namespace Sungero.Docflow
{
  partial class ExchangeDocumentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Дизейбл полей.
      foreach (var property in _obj.State.Properties)
        property.IsEnabled = false;
      
      // Хинт о смене типа.
      e.AddInformation(ExchangeDocuments.Resources.ChangeDocumentTypeHint, _obj.Info.Actions.ChangeDocumentType);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;

namespace Sungero.Docflow.Client
{
  partial class DocumentKindActions
  {
    public virtual void ShowSettings(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var settings = Functions.Module.Remote.GetDocumentKindSettings(_obj);
      settings.Show();
    }

    public virtual bool CanShowSettings(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;

namespace Sungero.Docflow.Client
{
  partial class SimpleDocumentActions
  {
    public override void ChangeDocumentType(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Для смены типа необходимо отменить регистрацию.
      if (_obj.RegistrationState == OfficialDocument.RegistrationState.Registered &&
          _obj.DocumentKind.NumberingType != DocumentKind.NumberingType.Numerable ||
          _obj.RegistrationState == OfficialDocument.RegistrationState.Reserved)
      {
        // Используем диалоги, чтобы хинт не пробрасывался в задачу, в которую он вложен.
        Dialogs.ShowMessage(SimpleDocuments.Resources.NeedCancelRegistration, MessageType.Error);
        return;
      }
      
      base.ChangeDocumentType(e);
    }

    public override bool CanChangeDocumentType(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanChangeDocumentType(e);
    }

  }

}
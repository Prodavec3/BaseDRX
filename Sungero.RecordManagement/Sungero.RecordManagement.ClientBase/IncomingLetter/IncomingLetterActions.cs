using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.IncomingLetter;

namespace Sungero.RecordManagement.Client
{

  partial class IncomingLetterActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.IncomingLetter.Remote.GetDuplicates(_obj, _obj.DocumentKind, _obj.BusinessUnit, _obj.InNumber, _obj.Dated, _obj.Correspondent);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(IncomingLetters.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}
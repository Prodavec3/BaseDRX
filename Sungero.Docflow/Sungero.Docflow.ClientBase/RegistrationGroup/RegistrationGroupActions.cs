using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationGroup;

namespace Sungero.Docflow.Client
{
  partial class RegistrationGroupActions
  {

    public virtual void ShowGroupDocumentRegisters(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.RegistrationGroup.Remote.GetGroupDocumentRegisters(_obj).Show();
    }

    public virtual bool CanShowGroupDocumentRegisters(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
}
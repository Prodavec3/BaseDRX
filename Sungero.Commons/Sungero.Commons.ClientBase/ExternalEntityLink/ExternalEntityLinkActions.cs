using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons.ExternalEntityLink;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Client;
using Sungero.Domain.Shared;

namespace Sungero.Commons.Client
{
  partial class ExternalEntityLinkActions
  {
    public virtual void OpenEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ExternalEntityLink.Remote.GetEntity(_obj).Show();
    }

    public virtual bool CanOpenEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}
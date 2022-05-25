using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MailDeliveryMethod;

namespace Sungero.Docflow.Client
{
  partial class MailDeliveryMethodActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e) && string.IsNullOrWhiteSpace(_obj.Sid);
    }

  }

}
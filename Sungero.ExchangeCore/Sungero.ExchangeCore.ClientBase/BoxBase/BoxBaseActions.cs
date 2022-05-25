using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BoxBase;

namespace Sungero.ExchangeCore.Client
{
  partial class BoxBaseActions
  {
    public virtual void FindClosedChildBoxes(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var boxes = ExchangeCore.Functions.BoxBase.Remote.GetClosedChildBoxes(_obj);
      boxes.Show();
    }

    public virtual bool CanFindClosedChildBoxes(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void FindActiveChildBoxes(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var boxes = ExchangeCore.Functions.BoxBase.Remote.GetActiveChildBoxes(_obj);
      boxes.Show();
    }

    public virtual bool CanFindActiveChildBoxes(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}
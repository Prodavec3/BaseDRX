using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemCompletionGraphWidgetHandlers
  {

    public virtual void ExecuteActionItemCompletionGraphActionItemCompletionGraphAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      DateTime month;
      if (Calendar.TryParseDate(e.ValueId, out month))
        Functions.Module.ShowActionItemsExecutionReport(month, _parameters.Performer);
    }
  }

}
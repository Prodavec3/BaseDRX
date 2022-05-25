using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement
{
  partial class AcquaintanceAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      // Заменить результат выполнения, если задание выполнено не лично.
      var isElectronicAcquaintance = AcquaintanceTasks.As(_obj.Task).IsElectronicAcquaintance == true;
      if (!Equals(_obj.CompletedBy, _obj.Performer) && isElectronicAcquaintance)
        e.Result = AcquaintanceTasks.Resources.CompletedByAnother;
      else
        e.Result = AcquaintanceTasks.Resources.Acquainted;
    }
  }

}
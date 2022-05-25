using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.DeadlineExtensionTask
{
  partial class ActionItemAssignees
  {
    public List<IUser> Assignees { get; set; }
    
    public bool CanSelect { get; set; }
  }
}
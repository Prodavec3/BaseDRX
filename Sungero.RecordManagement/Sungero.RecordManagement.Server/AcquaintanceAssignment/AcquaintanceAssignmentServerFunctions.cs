using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.AcquaintanceAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class AcquaintanceAssignmentFunctions
  {
    
    /// <summary>
    /// Проверить, замещает ли сотрудник другого сотрудника.
    /// </summary>
    /// <param name="who">Кто замещает.</param>
    /// <param name="whom">Кого замещают.</param>
    /// <returns>True, если замещает, иначе False.</returns>
    [Remote(IsPure = true)]
    public bool IsSubstituteOf(IUser who, IUser whom)
    {
      return Substitutions.GetAll().Where(x => Equals(x.User, whom) &&
                                   Equals(x.Substitute, who) &&
                                   x.Status == CoreEntities.DatabookEntry.Status.Active &&
                                   (!x.StartDate.HasValue || Calendar.Today >= x.StartDate) &&
                                   (!x.EndDate.HasValue || Calendar.Today <= x.EndDate)).Any();
    }
    
  }
}
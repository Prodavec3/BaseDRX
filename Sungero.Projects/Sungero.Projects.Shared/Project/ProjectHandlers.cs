using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.Project;

namespace Sungero.Projects
{
  partial class ProjectSharedHandlers
  {

    public virtual void StageChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != Stage.Completed)
        _obj.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;
      else
        _obj.Status = Sungero.CoreEntities.DatabookEntry.Status.Closed;
    }
  }

  partial class ProjectTeamMembersSharedCollectionHandlers
  {

    public virtual void TeamMembersAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      if (_added.Group == null)
        _added.Group = ProjectTeamMembers.Group.Change;
    }
  }

}
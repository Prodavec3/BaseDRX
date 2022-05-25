using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectTeam;

namespace Sungero.Projects
{
  partial class ProjectTeamClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!Users.Current.IncludedIn(Roles.Administrators))
      {
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
      }
    }

  }
}
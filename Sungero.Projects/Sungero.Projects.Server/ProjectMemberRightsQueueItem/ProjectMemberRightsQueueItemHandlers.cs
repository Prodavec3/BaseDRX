using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectMemberRightsQueueItem;

namespace Sungero.Projects
{
  partial class ProjectMemberRightsQueueItemServerHandlers
  {

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      base.Deleting(e);
    }
  }

}
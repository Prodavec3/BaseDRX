using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Projects.Server
{
  partial class ProjectStagesWidgetHandlers
  {
    public virtual IQueryable<Sungero.Projects.IProject> ProjectStagesOverdueFiltering(System.Linq.IQueryable<Sungero.Projects.IProject> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, true, null);
    }

    public virtual IQueryable<Sungero.Projects.IProject> ProjectStagesClosingFiltering(System.Linq.IQueryable<Sungero.Projects.IProject> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.Project.Stage.Completion);
    }

    public virtual IQueryable<Sungero.Projects.IProject> ProjectStagesInWorkFiltering(System.Linq.IQueryable<Sungero.Projects.IProject> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.Project.Stage.Execution);
    }

    public virtual IQueryable<Sungero.Projects.IProject> ProjectStagesInitiationFiltering(System.Linq.IQueryable<Sungero.Projects.IProject> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, Sungero.Projects.Project.Stage.Initiation);
    }

    public virtual IQueryable<Sungero.Projects.IProject> ProjectStagesAllProjectsFiltering(System.Linq.IQueryable<Sungero.Projects.IProject> query)
    {
      return Functions.Module.GetProjectsToWidgets(_parameters.Performer, false, null);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.Project;

namespace Sungero.Projects
{
  partial class ProjectTeamMembersClientHandlers
  {

    public virtual void TeamMembersGroupValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void TeamMembersMemberValueInput(Sungero.Projects.Client.ProjectTeamMembersMemberValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }
  }

  partial class ProjectClientHandlers
  {

    public virtual void LeadingProjectValueInput(Sungero.Projects.Client.ProjectLeadingProjectValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void InternalCustomerValueInput(Sungero.Projects.Client.ProjectInternalCustomerValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void ManagerValueInput(Sungero.Projects.Client.ProjectManagerValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void AdministratorValueInput(Sungero.Projects.Client.ProjectAdministratorValueInputEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue))
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual IEnumerable<Enumeration> StageFiltering(IEnumerable<Enumeration> query)
    {
      return query.Where(s => !Equals(s, Stage.Completed));
    }

    public virtual void ActualFinishDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.ActualStartDate != null) && (_obj.ActualStartDate > e.NewValue))
        e.AddError(Projects.Resources.IncorrectEndDate, _obj.Info.Properties.ActualFinishDate);
    }

    public virtual void ActualStartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.ActualFinishDate != null) && (e.NewValue > _obj.ActualFinishDate))
        e.AddError(Projects.Resources.IncorrectStartDate, _obj.Info.Properties.ActualStartDate);
    }

    public virtual void ExecutionPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if ((e.NewValue != null) && (e.NewValue.Value < 0 || e.NewValue.Value > 100))
        e.AddError(Projects.Resources.IncorrectPercent);
    }

    public virtual void ShortNameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != null)
        e.NewValue = e.NewValue.Trim();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);

      var available = _obj.Stage != Stage.Completed;
      foreach (var property in _obj.State.Properties)
        property.IsEnabled = available;
      
      if (_obj.State.IsCopied)
        Sungero.Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Resources.ProjectAndProjectFoldersRightsNotifyMessage);
    }

    public virtual void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.EndDate != null) && (e.NewValue > _obj.EndDate))
        e.AddError(Projects.Resources.IncorrectStartDate, _obj.Info.Properties.StartDate);
    }

    public virtual void EndDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if ((_obj.StartDate != null) && (_obj.StartDate > e.NewValue))
        e.AddError(Projects.Resources.IncorrectEndDate, _obj.Info.Properties.EndDate);
    }
  }
}
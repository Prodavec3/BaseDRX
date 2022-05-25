using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.Project;

namespace Sungero.Projects
{
  partial class ProjectTeamMembersMemberPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> TeamMembersMemberFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var systemRecipientsSid = Company.PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);      
      systemRecipientsSid.Remove(Sungero.Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam);      
      return query.Where(x => !systemRecipientsSid.Contains(x.Sid.Value));
    }
  }

  partial class ProjectProjectKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ProjectKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
    }
  }

  partial class ProjectLeadingProjectPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> LeadingProjectFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Из-за того, что команда подпроекта включается в команду ведущего проекта, изменять ведущий проект могут РП или администратор.
      var admin = Users.Current.IncludedIn(Roles.Administrators);
      if (!admin)
      {
        var projectsIds = Functions.Project.GetProjectsManagerOrAdministrator(Users.Current).Select(p => p.Id).ToList();
        query = query.Where(x => projectsIds.Contains(x.Id));
      }

      return query.Where(x => x.Stage != Stage.Completed && !Equals(x, _obj));
    }
  }

  partial class ProjectClassifierDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ClassifierDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(dk => dk.ProjectsAccounting == true);
      return query;
    }
  }

  partial class ProjectCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      e.Without(_info.Properties.ActualStartDate);
      e.Without(_info.Properties.ActualFinishDate);
      e.Without(_info.Properties.ExecutionPercent);
      e.Without(_info.Properties.Note);
      e.Without(_info.Properties.Folder);
    }
  }

  partial class ProjectManagerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ManagerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var role = Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
      
      if (role != null)
      {
        var allRecipientIds = Groups.GetAllUsersInGroup(role).Select(x => x.Id).ToList();
        return query.Where(m => allRecipientIds.Contains(m.Id));
      }
      
      return query;
    }
  }

  partial class ProjectFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      if (_filter == null)
        return query;
      
      // Фильтр по состоянию.
      if (_filter.Active || _filter.Closed || _filter.Closing || _filter.Initiation)
        query = query.Where(x => (_filter.Active && x.Stage == Stage.Execution) ||
                            (_filter.Closed && x.Stage == Stage.Completed) ||
                            (_filter.Closing && x.Stage == Stage.Completion) ||
                            (_filter.Initiation && x.Stage == Stage.Initiation));
      
      // Фильтр по виду проекта.
      if (_filter.ProjectKind != null)
        query = query.Where(x => Equals(x.ProjectKind, _filter.ProjectKind));

      // Фильтр по руководителю проекта.
      if (_filter.ProjectManager != null)
        query = query.Where(x => Equals(x.Manager, _filter.ProjectManager));
      
      // Фильтр по ведущему проекту.
      if (_filter.LeadingProject != null)
        query = query.Where(x => Equals(x.LeadingProject, _filter.LeadingProject));

      // Фильтр по внутреннему заказчику.
      if (_filter.InternalCustomer != null)
        query = query.Where(x => Equals(x.InternalCustomer, _filter.InternalCustomer));

      // Фильтр по внешнему заказчику.
      if (_filter.ExternalCustomer != null)
        query = query.Where(x => Equals(x.ExternalCustomer, _filter.ExternalCustomer));

      var today = Calendar.UserToday;
      
      // Фильтр по дате начала проекта.
      var startDateBeginPeriod = _filter.StartDateRangeFrom ?? Calendar.SqlMinValue;
      var startDateEndPeriod = _filter.StartDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.StartPeriodThisMonth)
      {
        startDateBeginPeriod = today.BeginningOfMonth();
        startDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.StartPeriodThisMonth || (_filter.StartDateRangeFrom != null || _filter.StartDateRangeTo != null))
        query = query.Where(x => (x.StartDate.Between(startDateBeginPeriod, startDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
                            (x.ActualStartDate.Between(startDateBeginPeriod, startDateEndPeriod) && Equals(x.Stage, Stage.Completed)));

      // Фильтр по дате окончания проекта.
      var finishDateBeginPeriod = _filter.FinishDateRangeFrom ?? Calendar.SqlMinValue;
      var finishDateEndPeriod = _filter.FinishDateRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.FinishPeriodThisMonth)
      {
        finishDateBeginPeriod = today.BeginningOfMonth();
        finishDateEndPeriod = today.EndOfMonth();
      }
      
      if (_filter.FinishPeriodThisMonth || (_filter.FinishDateRangeFrom != null || _filter.FinishDateRangeTo != null))
        query = query.Where(x => (x.EndDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && !Equals(x.Stage, Stage.Completed)) ||
                            (x.ActualFinishDate.Between(finishDateBeginPeriod, finishDateEndPeriod) && Equals(x.Stage, Stage.Completed)));
      
      return query;
    }
  }

  partial class ProjectServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);
      
      if (!e.Params.Contains(Sungero.Projects.Constants.Module.DontUpdateModified) && e.Params.Contains(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument))
      {
        Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
        e.Params.Remove(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument);
      }
      
      if (!e.Params.Contains(Sungero.Projects.Constants.Module.DontUpdateModified))
        Sungero.Projects.Jobs.GrantAccessRightsToProjectFolders.Enqueue();
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      base.Deleting(e);
      
      if (!Docflow.OfficialDocuments.GetAll().Any(d => Equals(d.Project, _obj)))
      {
        // Папки.
        var folder = _obj.Folder;
        Folders.Delete(folder);
        
        foreach (var line in _obj.Classifier)
        {
          folder = line.Folder;
          Folders.Delete(folder);
        }
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      #region Работа с папками
      
      // Создание папки проекта.
      if (_obj.Folder == null)
      {
        var folder = Folders.Create();
        folder.Name = _obj.ShortName;
        
        folder.Items.Add(_obj);
        _obj.Folder = folder;
      }
      
      // Вложение папки проекта в корневую папку или папку ведущего проекта.
      var projectsFolder = Folders.GetAll().Single(f => f.Uid == Constants.Module.ProjectFolders.ProjectFolderUid);

      if (_obj.State.Properties.LeadingProject.IsChanged)
      {
        // Удаление из папки предыдущего проекта.
        var leading = _obj.State.Properties.LeadingProject.OriginalValue;
        if (leading != null && !Equals(leading, _obj.LeadingProject) && leading.Folder != null && leading.Folder.Items.Contains(_obj.Folder))
          leading.Folder.Items.Remove(_obj.Folder);
      }
      
      if (_obj.LeadingProject == null)
      {
        if (projectsFolder != null && !projectsFolder.Items.Contains(_obj.Folder) && _obj.Stage != Stage.Completed)
          projectsFolder.Items.Add(_obj.Folder);
      }
      else
      {
        // Удаление из корневой папки.
        if (projectsFolder != null)
          projectsFolder.Items.Remove(_obj.Folder);
        
        // Добавление в папку ведущего проекта.
        if (_obj.LeadingProject.Folder != null && !_obj.LeadingProject.Folder.Items.Contains(_obj.Folder))
          _obj.LeadingProject.Folder.Items.Add(_obj.Folder);
      }
      
      #endregion
      
      #region Переименование групп, папок
      
      if (_obj.State.Properties.ShortName.IsChanged)
        _obj.Folder.Name = _obj.ShortName;
      
      #endregion
      
      #region Перенос в папку Архив и обратно
      
      if (_obj.State.Properties.Stage.OriginalValue != _obj.Stage && _obj.LeadingProject == null)
      {
        var rootProjectFolder = Folders.GetAll().Single(f => f.Uid == Constants.Module.ProjectFolders.ProjectFolderUid);
        var archiveFolder = Folders.GetAll().Single(f => f.Uid == Constants.Module.ProjectFolders.ProjectArhiveFolderUid);
        var projectFolder = _obj.Folder;
        
        if (_obj.Stage == Stage.Completed)
        {
          archiveFolder.Items.Add(projectFolder);
          rootProjectFolder.Items.Remove(projectFolder);
        }
        else
        {
          rootProjectFolder.Items.Add(projectFolder);
          archiveFolder.Items.Remove(projectFolder);
        }
      }
      
      #endregion
      
      #region Создание и обновление папок проекта
      
      Functions.Project.UpdateClassifier(_obj);
      
      #endregion
      
      #region Очередь выдачи прав участнику
      
      var properties = _obj.State.Properties;
      var needAddToQueue = false;
      if (properties.Manager.IsChanged && properties.Manager.OriginalValue != _obj.Manager)
        needAddToQueue = true;
      
      if (properties.Administrator.IsChanged && _obj.Administrator != null && properties.Administrator.OriginalValue != _obj.Administrator)
        needAddToQueue = true;
      
      if (properties.InternalCustomer.IsChanged && _obj.InternalCustomer != null && properties.InternalCustomer.OriginalValue != _obj.InternalCustomer)
        needAddToQueue = true;
      
      if (properties.TeamMembers.IsChanged && _obj.TeamMembers.Any())
        needAddToQueue = true;
      
      if (_obj.State.Properties.LeadingProject.IsChanged)
      {
        var originalLeadProject = properties.LeadingProject.OriginalValue;
        var leadProject = _obj.LeadingProject;
        if (leadProject != null && leadProject != originalLeadProject)
          needAddToQueue = true;
      }
      
      if (needAddToQueue)
      {
        Logger.DebugFormat("CreateProjectMemberRightsQueueItem: project {0}", _obj.Id);
        var queueItem = ProjectMemberRightsQueueItems.Create();
        queueItem.ProjectId = _obj.Id;
        queueItem.Save();
        
        // Добавить признак того, что нужно запустить ФП Автоматическое назначение прав на документы.
        e.Params.Add(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToProjectDocument, true);
      }

      #endregion
      
      #region Выдача прав РП и администратору
      
      var recipients = new List<IRecipient>() { _obj.Manager };
      if (_obj.Administrator != null)
        recipients.Add(_obj.Administrator);
      
      var folders = new List<IFolder>();
      if (_obj.Folder != null)
        folders.Add(_obj.Folder);
      folders.AddRange(_obj.Classifier.Where(c => c.Folder != null).Select(f => f.Folder).ToList());
      
      foreach (var recipient in recipients)
      {
        foreach (var folder in folders)
        {
          if (!folder.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, recipient) && (Locks.GetLockInfo(folder) == null || !Locks.GetLockInfo(folder).IsLockedByOther))
            folder.AccessRights.Grant(recipient, DefaultAccessRightsTypes.FullAccess);
        }
        
        if (!_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, recipient) && Equals(recipient, _obj.Manager))
          _obj.AccessRights.Grant(recipient, DefaultAccessRightsTypes.FullAccess);
        
        if (!Functions.Module.CheckGrantedRights(_obj, recipient, DefaultAccessRightsTypes.Change) && Equals(recipient, _obj.Administrator))
          _obj.AccessRights.Grant(recipient, DefaultAccessRightsTypes.Change);
      }
      
      #endregion
      
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      base.BeforeDelete(e);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.Stage = Stage.Initiation;
      
      // Проверка, является ли служебным пользователем.
      var role = Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
      if (Sungero.Company.Employees.Is(Users.Current) && Users.Current.IncludedIn(role))
        _obj.Manager = Sungero.Company.Employees.Current;
      
      _obj.Modified = Calendar.Now;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (!_obj.AccessRights.CanUpdate())
      {
        e.AddError(Projects.Resources.NoRightToUpdateProject);
        return;
      }
      
      // TODO Zamerov: сравнивать надо с ресурсом в локали тенанта. BUG: 35010
      if (Equals(_obj.ShortName, Sungero.Projects.Resources.ProjectArhiveFolderName))
        e.AddError(Projects.Resources.PropertyReservedFormat(_obj.Info.Properties.ShortName.LocalizedName, Sungero.Projects.Resources.ProjectArhiveFolderName));
      
      if (Projects.GetAll().Any(p => !Equals(p, _obj) && Equals(p.ShortName, _obj.ShortName)))
        e.AddError(Projects.Resources.PropertyAlreadyUsedFormat(_obj.Info.Properties.ShortName.LocalizedName, _obj.ShortName));
      
      // Проверка циклических ссылок в подпроектах.
      if (_obj.State.Properties.LeadingProject.IsChanged && _obj.LeadingProject != null)
      {
        var leadingProject = _obj.LeadingProject;
        
        while (leadingProject != null)
        {
          if (Equals(leadingProject, _obj))
          {
            e.AddError(_obj.Info.Properties.LeadingProject, Projects.Resources.LeadingProjectCyclicReference, _obj.Info.Properties.LeadingProject);
            break;
          }
          
          leadingProject = leadingProject.LeadingProject;
        }
      }

      if (!e.Params.Contains(Sungero.Projects.Constants.Module.DontUpdateModified))
        _obj.Modified = Calendar.Now;
    }
  }
}
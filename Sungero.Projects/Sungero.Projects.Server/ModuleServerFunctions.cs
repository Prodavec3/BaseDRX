using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Projects.Structures.Module;
using Init = Sungero.Projects.Constants.Module.Initialize;

namespace Sungero.Projects.Server
{
  public class ModuleFunctions
  {
    #region Виджеты
    
    #region Проекты по стадиям
    
    /// <summary>
    /// Получить проекты по стадиям для виджетов.
    /// </summary>
    /// <param name="performer">Ограничение по сотрудникам.</param>
    /// <param name="onlyOverdue">По просроченным проектам.</param>
    /// <param name="stage">Стадия проекта.</param>
    /// <returns>Запрос проектов.</returns>
    public IQueryable<Sungero.Projects.IProject> GetProjectsToWidgets(Enumeration performer, bool onlyOverdue, Enumeration? stage)
    {
      var projects = Projects.GetAll(p => !Equals(p.Stage, Sungero.Projects.Project.Stage.Completed));

      if (onlyOverdue)
        projects = projects.Where(x => x.EndDate < Calendar.UserToday);
      else if (stage.HasValue)
        projects = projects.Where(x => Equals(x.Stage, stage));

      if (Equals(performer, Sungero.Projects.Widgets.ProjectStages.Performer.MyDepartment))
        projects = projects.Where(x => Equals(x.Manager.Department, Sungero.Company.Employees.Current.Department));
      else if (Equals(performer, Sungero.Projects.Widgets.ProjectStages.Performer.MyProjects))
        projects = projects.Where(x => Equals(x.Manager, Sungero.Company.Employees.Current));

      return projects;
    }
    
    #endregion
    
    #endregion

    #region Выдача прав на проектные документы

    /// <summary>
    /// Создать элементы очереди выдачи прав на документы по проектам, включая приложения к документу.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <returns>Список структур для сохранения в таблицу очереди выдачи прав.</returns>
    public static List<Structures.ProjectDocumentRightsQueueItem.ProxyQueueItem> CreateAccessRightsProjectDocumentQueueItemWithAddendum(int documentId, int projectId)
    {
      var queue = new List<Sungero.Projects.Structures.ProjectDocumentRightsQueueItem.ProxyQueueItem>();
      var document = Docflow.OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
        return queue;

      queue.Add(CreateAccessRightsProjectDocumentQueueItem(documentId, projectId));
      
      // Приложения по документу тоже собрать.
      var addenda = GetAddendums(document);
      foreach (var addendum in addenda)
        queue.Add(CreateAccessRightsProjectDocumentQueueItem(addendum.Id, Docflow.PublicFunctions.OfficialDocument.GetProject(addendum).Id));
      return queue;
    }
    
    /// <summary>
    /// Получить все приложения к заданному документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список приложений.</returns>
    private static List<IOfficialDocument> GetAddendums(IOfficialDocument document)
    {
      var addendaList = new List<IOfficialDocument>() { };
      var leadingDocuments = new List<IOfficialDocument>() { document };
      while (Addendums.GetAll(a => leadingDocuments.Contains(a.LeadingDocument)).Any())
      {
        leadingDocuments = OfficialDocuments.GetAll(a => Addendums.Is(a) && leadingDocuments.Contains(a.LeadingDocument)).ToList();
        addendaList.AddRange(leadingDocuments);
      }
      
      return addendaList;
    }
    
    /// <summary>
    /// Создать элемент очереди выдачи прав на документы по проектам.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <returns>Структура для сохранения в таблицу очереди выдачи прав.</returns>
    public static Structures.ProjectDocumentRightsQueueItem.ProxyQueueItem CreateAccessRightsProjectDocumentQueueItem(int documentId, int projectId)
    {
      Logger.DebugFormat("CreateProjectDocumentRightsQueueItem: document {0}, project {1}", documentId, projectId);
      var queueItem = Structures.ProjectDocumentRightsQueueItem.ProxyQueueItem.Create();
      queueItem.Discriminator = ProjectDocumentRightsQueueItem.ClassTypeGuid;
      queueItem.DocumentId_Project_Sungero = documentId;
      queueItem.ProjectId_Project_Sungero = projectId;
      return queueItem;
    }
    
    /// <summary>
    /// Получить права участников проекта.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <param name="document">Документ.</param>
    /// <param name="project">Проект.</param>
    /// <returns>Список прав в виде реципиент-тип прав.</returns>
    internal List<Structures.Module.RecipientRights> GetProjectRecipientRights(IProjectDocumentRightsQueueItem queueItem,
                                                                               IOfficialDocument document, IProjectBase project)
    {
      var result = new List<Structures.Module.RecipientRights>();
      // Выдать права на сам документ.
      this.AddRecipientRightsForProject(document, project, document.DocumentKind.GrantRightsToProject.Value, result);
      this.GrantRightsOnDocumentToLeadingProgect(document, result);

      return result;
    }
    
    /// <summary>
    /// Выдать права на документ вышестоящим проектам.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="result">Результат.</param>
    private void GrantRightsOnDocumentToLeadingProgect(IOfficialDocument document, List<RecipientRights> result)
    {
      var grantRightsDocument = document;
      while (document.LeadingDocument != null)
      {
        var leadingProjectBase = document.LeadingDocument.Project;
        var leadingProject = leadingProjectBase != null ? Projects.GetAll(p => p.Id == leadingProjectBase.Id).FirstOrDefault() : Projects.Null;
        this.AddRecipientRightsForProject(grantRightsDocument, leadingProject, document.LeadingDocument.DocumentKind.GrantRightsToProject.Value, result);
        document = document.LeadingDocument;
      }
    }
    
    /// <summary>
    /// Выдать права на документы.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>Признак успешности выдачи прав.</returns>
    public bool GrantRightsToProjectDocuments(IProjectDocumentRightsQueueItem queueItem)
    {
      var document = Docflow.OfficialDocuments.GetAll(d => d.Id == queueItem.DocumentId.Value).FirstOrDefault();
      if (document == null)
        return true;

      var isChanged = false;
      var accessRights = this.GetProjectRecipientRights(queueItem, document, document.Project);      

      var result = true;
      try
      {
        foreach (var accessRight in accessRights)
        {
          var accessRightsType = Docflow.PublicFunctions.Module.GetRightTypeGuid(new Enumeration(accessRight.AccessRights));
          if (!document.AccessRights.IsGrantedDirectly(accessRightsType, accessRight.Recipient))
          {
            if (!isChanged && !Locks.TryLock(document))
            {
              Logger.DebugFormat("GrantRightsToProjectDocuments: cannot grant rights, document {0} is locked.", document.Id);
              return false;
            }
            document.AccessRights.Grant(accessRight.Recipient, accessRightsType);
            Logger.DebugFormat("GrantRightsToProjectDocuments: granted rights on document {0} to recipient {1}.", document.Id, accessRight.Recipient.Id);
            isChanged = true;
          }
        }

        if (isChanged)
        {
          ((Domain.Shared.IExtendedEntity)document).Params[Docflow.PublicConstants.OfficialDocument.DontUpdateModified] = true;
          document.Save();
        }
      }
      catch (Exception ex)
      {
        result = false;
        Logger.ErrorFormat("GrantRightsToProjectDocuments: grant rights on project document {0} failed", ex, document.Id);
      }
      finally
      {
        if (isChanged)
          Locks.Unlock(document);
      }
      
      return result;
    }

    /// <summary>
    /// Заполнить список прав на проект.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="projectBase">Проект.</param>
    /// <param name="grantRightsToProject">True, если выдавать права участникам проектов. Иначе - false.</param>
    /// <param name="result">Список прав в виде реципиент-тип прав.</param>
    public virtual void AddRecipientRightsForProject(IOfficialDocument document, IProjectBase projectBase, bool grantRightsToProject, List<RecipientRights> result)
    {
      if (projectBase == null)
        return;
      
      if (!Projects.Is(projectBase))
        return;
      
      var project = Projects.As(projectBase);
      var readType = Constants.Module.AccessRightsReadTypeName;
      var editType = Constants.Module.AccessRightsEditTypeName;

      if (Docflow.PublicFunctions.OfficialDocument.IsProjectDocument(document, new List<int>()))
        result.Add(Structures.Module.RecipientRights.Create(project.Manager, editType));

      if (grantRightsToProject == true)
      {
        if (project.Administrator != null)
          result.Add(Structures.Module.RecipientRights.Create(project.Administrator, editType));
        
        if (project.InternalCustomer != null)
          result.Add(Structures.Module.RecipientRights.Create(project.InternalCustomer, readType));

        foreach (var teamMember in project.TeamMembers)
        {
          result.Add(teamMember.Group == Sungero.Projects.ProjectTeamMembers.Group.Management
                     ? Structures.Module.RecipientRights.Create(teamMember.Member, editType)
                     : Structures.Module.RecipientRights.Create(teamMember.Member, readType));
        }
      }
      
      if (project.LeadingProject != null)
        this.AddRecipientRightsForProject(document, project.LeadingProject, grantRightsToProject, result);
    }

    /// <summary>
    /// Запустить фоновый процесс "Проекты. Автоматическое назначение прав на документы".
    /// </summary>
    [Public, Remote]
    public static void RequeueProjectDocumentRightsSync()
    {
      Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
    }

    /// <summary>
    /// Вложить документ в папку проекта.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>True, если элемент очереди обработан.</returns>
    public virtual bool AddDocumentToFolder(IProjectDocumentRightsQueueItem queueItem)
    {
      var document = Docflow.OfficialDocuments.GetAll(d => d.Id == queueItem.DocumentId.Value).FirstOrDefault();
      if (document == null)
        return true;

      var project = Projects.GetAll(d => d.Id == queueItem.ProjectId.Value).FirstOrDefault();
      if (project == null)
        return true;

      if (!IsDocumentBelongProject(document, project))
        return true;

      Functions.Project.AddDocumentToFolder(project, document);
      
      return true;
    }

    /// <summary>
    /// Проверка, относится ли документ к проекту.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="project">Проект.</param>
    /// <returns>Если документ относится к проекту, то true, иначе - false.</returns>
    public static bool IsDocumentBelongProject(IOfficialDocument document, IProject project)
    {
      var documentProject = Docflow.PublicFunctions.OfficialDocument.GetProject(document);
      var projects = GetProjectAndSubProjects(project);
      return documentProject != null ? projects.Contains(documentProject) : false;
    }

    #endregion
    
    #region Выдача прав на проект и проектные папки

    /// <summary>
    /// Создать очередь выдачи прав на проект и папки проекта.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Структура с очередью выдачи прав.</returns>
    public static List<Structures.ProjectRightsQueueItem.ProxyQueueItem> CreateProjectAccessRightsQueueItems(IProject project)
    {
      var queueItems = new List<Structures.ProjectRightsQueueItem.ProxyQueueItem>();
      if (project == null)
        return queueItems;
      
      var allProjects = GetProjectAndSubProjects(project);
      queueItems.AddRange(allProjects.SelectMany(p => GetProjectFolders(p, false, true).Select(f => Functions.Module.CreateAccessRightsProjectQueueItem(p.Id, f.Id))).ToList());
      queueItems.AddRange(allProjects.Select(s => Functions.Module.CreateAccessRightsProjectQueueItem(s.Id, null)).ToList());
      
      if (project.LeadingProject != null)
        queueItems.AddRange(GetProjectFolders(project.LeadingProject, true, false).Select(f => Functions.Module.CreateAccessRightsProjectQueueItem(project.Id, f.Id)));
      
      return queueItems;
    }
    
    /// <summary>
    /// Создать элемент очереди выдачи прав на проекты и папки проектов.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="folderId">ИД папки.</param>
    /// <returns>Структура для сохранения в таблицу очереди выдачи прав.</returns>
    public static Structures.ProjectRightsQueueItem.ProxyQueueItem CreateAccessRightsProjectQueueItem(int projectId, int? folderId)
    {
      Logger.DebugFormat("CreateProjectRightsQueueItem: project {0}, folder {1}", projectId, folderId);
      var queueItem = Structures.ProjectRightsQueueItem.ProxyQueueItem.Create();
      queueItem.Discriminator = ProjectRightsQueueItem.ClassTypeGuid;
      queueItem.ProjectId_Project_Sungero = projectId;
      queueItem.FolderId_Project_Sungero = folderId;
      return queueItem;
    }

    /// <summary>
    /// Выдать права на проект и папки проекта.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    /// <returns>Признак успешности выдачи прав.</returns>
    public static bool GrantRightsToProjectAndFolder(IProjectRightsQueueItem queueItem)
    {
      var project = Projects.GetAll(d => d.Id == queueItem.ProjectId.Value).FirstOrDefault();
      if (project == null)
        return true;
      
      var members = GetMembersRights(project);
      if (!members.Any())
        return true;

      if (queueItem.FolderId != null)
      {
        var folder = Folders.GetAll(d => d.Id == queueItem.FolderId).FirstOrDefault();
        if (folder == null)
          return true;
        
        // Не выдавать права, если папка не относится к проекту или к ведущим проектам.
        if (!GetProjectFolders(project, true, true).Contains(folder))
          return true;
        
        // Если папка не относится к проекту, то это папка ведущего проекта. На такую папку выдать права только на просмотр любому из участников.
        if (!GetProjectFolders(project, false, true).Contains(folder) ||
            folder.Uid == Constants.Module.ProjectFolders.ProjectFolderUid ||
            folder.Uid == Constants.Module.ProjectFolders.ProjectArhiveFolderUid)
          members = members.Select(m => Structures.Project.ProjectMemberRights.Create(m.Recipient, Constants.Module.AccessRightsReadTypeName, Constants.Module.AccessRightsReadTypeName)).ToList();
        
        return GrantRightsToProjectFolder(folder, members);
      }
      else
        return GrantRightsToProject(project, members);
    }
    
    /// <summary>
    /// Выдать права на проект.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <param name="members">Список реципиентов с правами доступа.</param>
    /// <returns>Признак успешности выдачи прав.</returns>
    public static bool GrantRightsToProject(IProject project, List<Sungero.Projects.Structures.Project.ProjectMemberRights> members)
    {
      if (Locks.GetLockInfo(project).IsLockedByOther)
        return false;
      
      var isChanged = false;
      foreach (var memberItem in members)
      {
        var member = memberItem.Recipient;
        var accessRightsType = Docflow.PublicFunctions.Module.GetRightTypeGuid(new Sungero.Core.Enumeration(memberItem.ProjectRightsType));
        if (!CheckGrantedRights(project, member, accessRightsType))
        {
          project.AccessRights.Grant(member, accessRightsType);
          isChanged = true;
        }
      }
      if (isChanged)
      {
        ((Domain.Shared.IExtendedEntity)project).Params[Sungero.Projects.Constants.Module.DontUpdateModified] = true;
        project.Save();
      }
      return true;
    }

    /// <summary>
    /// Выдать права на папку.
    /// </summary>
    /// <param name="folder">Папка.</param>
    /// <param name="members">Список реципиентов с правами доступа.</param>
    /// <returns>Признак успешности выдачи прав.</returns>
    public static bool GrantRightsToProjectFolder(IFolder folder, List<Sungero.Projects.Structures.Project.ProjectMemberRights> members)
    {
      if (Locks.GetLockInfo(folder).IsLockedByOther)
        return false;
      
      var isChanged = false;
      foreach (var memberItem in members)
      {
        var member = memberItem.Recipient;
        // Для папки изменение - это изменение содержимого.
        var accessRightsType = Docflow.PublicFunctions.Module.GetRightTypeGuid(new Sungero.Core.Enumeration(memberItem.FoldersRightsType));
        if (accessRightsType == DefaultAccessRightsTypes.Change)
          accessRightsType = Docflow.Constants.Module.DefaultAccessRightsTypeSid.ChangeContent;
        if (!CheckGrantedRights(folder, member, accessRightsType))
        {
          folder.AccessRights.Grant(member, accessRightsType);
          isChanged = true;
        }
      }
      if (isChanged)
        folder.Save();
      return true;
    }
    
    /// <summary>
    /// Проверить наличие у участника прав на сущность.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="member">Участник.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    /// <returns>True - если права есть, иначе - false.</returns>
    public static bool CheckGrantedRights(IEntity entity, IRecipient member, Guid accessRightsType)
    {
      if (accessRightsType == DefaultAccessRightsTypes.Change)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
      
      if (accessRightsType == Docflow.Constants.Module.DefaultAccessRightsTypeSid.ChangeContent)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
      
      if (accessRightsType == DefaultAccessRightsTypes.Read)
        return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member) ||
          entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
          entity.AccessRights.IsGrantedDirectly(Docflow.Constants.Module.DefaultAccessRightsTypeSid.ChangeContent, member);
      
      return entity.AccessRights.IsGrantedDirectly(accessRightsType, member);
    }

    /// <summary>
    /// Получить проект с его подпроектами.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Список проектов.</returns>
    public static List<IProject> GetProjectAndSubProjects(IProject project)
    {
      var allProjects = new List<IProject>() { project };
      var leadingProjects = new List<IProject>() { project };
      
      while (Projects.GetAll().Any(p => leadingProjects.Contains(p.LeadingProject)))
      {
        leadingProjects = Projects.GetAll(p => leadingProjects.Contains(p.LeadingProject)).ToList();
        allProjects.AddRange(leadingProjects);
      }
      
      return allProjects;
    }

    /// <summary>
    /// Получить папки проекта.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <param name="withLeadProject">Признак учета ведущих проектов.</param>
    /// <param name="withClassifier">Добавлять папки классификатора проекта.</param>
    /// <returns>Список папок.</returns>
    private static List<IFolder> GetProjectFolders(IProject project, bool withLeadProject, bool withClassifier)
    {
      var folders = new List<IFolder>() { project.Folder };
      if (withClassifier)
        folders.AddRange(project.Classifier.Select(c => c.Folder));
      
      folders.Add(Folders.GetAll().SingleOrDefault(f => f.Uid == Constants.Module.ProjectFolders.ProjectFolderUid));
      folders.Add(Folders.GetAll().SingleOrDefault(f => f.Uid == Constants.Module.ProjectFolders.ProjectArhiveFolderUid));
      
      if (withLeadProject && project.LeadingProject != null)
        folders.AddRange(GetProjectFolders(project.LeadingProject, withLeadProject, withClassifier));
      return folders;
    }

    /// <summary>
    /// Получить список реципиентов для выдачи прав.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Список реципиентов с типом прав.</returns>
    private static List<Sungero.Projects.Structures.Project.ProjectMemberRights> GetMembersRights(IProject project)
    {
      var fullAccess = Constants.Module.AccessRightsFullAccessTypeName;
      var change = Constants.Module.AccessRightsEditTypeName;
      var read = Constants.Module.AccessRightsReadTypeName;
      
      var members = new List<Sungero.Projects.Structures.Project.ProjectMemberRights>();
      members.Add(Sungero.Projects.Structures.Project.ProjectMemberRights.Create(project.Manager, fullAccess, fullAccess));
      if (project.Administrator != null)
        members.Add(Sungero.Projects.Structures.Project.ProjectMemberRights.Create(project.Administrator, change, fullAccess));
      
      members.AddRange(project.TeamMembers.Select(x => Sungero.Projects.Structures.Project.ProjectMemberRights.Create(x.Member, read, GetRightType(x.Group.Value))).ToList());
      if (project.InternalCustomer != null)
        members.Add(Sungero.Projects.Structures.Project.ProjectMemberRights.Create(project.InternalCustomer, read, read));
      
      if (project.LeadingProject != null)
        members.AddRange(GetMembersRights(project.LeadingProject));
      
      return members;
    }
    
    /// <summary>
    /// Получить строковое представление типа прав.
    /// </summary>
    /// <param name="rightType">Тип прав.</param>
    /// <returns>Строковое представление.</returns>
    private static string GetRightType(Enumeration rightType)
    {
      return rightType == Sungero.Projects.ProjectTeamMembers.Group.Read ? Constants.Module.AccessRightsReadTypeName : Constants.Module.AccessRightsEditTypeName;
    }

    /// <summary>
    /// Запустить фоновый процесс "Проекты. Автоматическое назначение прав на проекты и проектные папки".
    /// </summary>
    [Public, Remote]
    public static void RequeueProjectRightsSync()
    {
      Sungero.Projects.Jobs.GrantAccessRightsToProjectFolders.Enqueue();
    }
    
    #endregion

    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Проекты.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Sungero.Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(Sungero.Company.IEmployee employee)
    {
      var result = new List<Sungero.Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new ProjectsModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var modulePriority = Sungero.Company.PublicConstants.ResponsibilitiesReport.ProjectsPriority;
      
      if (!Projects.AccessRights.CanRead())
        return result;
      
      var activeProjects = Projects.GetAll().Where(p => p.Stage != Sungero.Projects.Project.Stage.Completed &&
                                                   p.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Руководитель проектов.
      var projects = activeProjects.Where(p => Equals(employee, p.Manager));
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, projects, moduleName, modulePriority,
                                                                                 Resources.ProjectManager, null);
      
      // Администратор проектов.
      projects = activeProjects.Where(p => Equals(employee, p.Administrator));
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, projects, moduleName, modulePriority,
                                                                                 Resources.ProjectAdmin, null);
      
      // Участник проектов.
      projects = activeProjects.Where(p => p.TeamMembers.Any(m => Equals(employee, m.Member)));
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, projects, moduleName, modulePriority,
                                                                                 Resources.ProjectMember, null);
      
      // Заказчик проектов.
      projects = activeProjects.Where(p => Equals(employee, p.InternalCustomer));
      result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, projects, moduleName, modulePriority,
                                                                                 Resources.ProjectCustomer, null);
      
      return result;
    }

    #region Сервис интеграции
    
    /// <summary>
    /// Заполнить коллекцию участников проекта.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="memberIds">Список ИД участников.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual void SetProjectMembers(int projectId, List<int> memberIds)
    {
      var project = Projects.GetAll(p => p.Id == projectId).FirstOrDefault();
      if (project == null)
        throw AppliedCodeException.Create(string.Format("Set project members. Project with ID ({0}) not found.", projectId));

      var members = new List<Sungero.Company.IEmployee>();
      if (memberIds.Any())
      {
        members = Sungero.Company.Employees.GetAll(e => memberIds.Contains(e.Id)).ToList();
        if (!members.Any())
          throw AppliedCodeException.Create(string.Format("Set project members. No employee found."));
        project.TeamMembers.Clear();
        foreach (var member in members)
        {
          var row = project.TeamMembers.AddNew();
          row.Member = member;
          row.Group = Sungero.Projects.ProjectTeamMembers.Group.Change;
        }
      }

      project.Save();
    }
    
    #endregion
  }
}
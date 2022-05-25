using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Initialization;
using Init = Sungero.Projects.Constants.Module.Initialize;

namespace Sungero.Projects.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Документы.
        InitializationLogger.Debug("Init: Grant rights on documents to all users.");
        GrantRightsOnDocuments(allUsers);
        
        // Справочники.
        InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
        GrantRightsOnDatabooks(allUsers);
      }
      
      // Назначить права роли "Руководители проектов".
      InitializationLogger.Debug("Init: Grant rights on projects");
      GrantRightsOnProjects();
      
      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateProjectKinds();
      CreateProjectFolder();
      GrantReadRightsOnProjectDocuments();
      GrantReadRightsOnProjectTeam();
      
      CreateDefaultApprovalRoles();
      CreateDefaultApprovalRules();
    }
    
    #region Создание ролей согласования
    
    /// <summary>
    /// Создать базовые роли согласования.
    /// </summary>
    public static void CreateDefaultApprovalRoles()
    {
      InitializationLogger.Debug("Init: Create default approval roles.");
      
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.ProjectManager, Sungero.Projects.Resources.RoleDescriptionProjectManager);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.ProjectAdmin, Sungero.Projects.Resources.RoleDescriptionProjectAdministrator);
    }
    
    /// <summary>
    /// Создать базовые правила согласования.
    /// </summary>
    public static void CreateDefaultApprovalRules()
    {
      InitializationLogger.Debug("Init: Create default approval rules.");
      
      var stages = new List<Enumeration> { Docflow.ApprovalStage.StageType.Approvers, Docflow.ApprovalStage.StageType.Approvers, Docflow.ApprovalStage.StageType.Sign, Docflow.ApprovalStage.StageType.Notice };
      CreateApprovalRule(Resources.DefaultApprovalRuleNameProjectDocument, stages);
    }
    
    /// <summary>
    /// Создать правило согласования.
    /// </summary>
    /// <param name="name">Имя правила.</param>
    /// <param name="stages">Этапы.</param>
    [Public]
    public static void CreateApprovalRule(string name, List<Enumeration> stages)
    {
      // Проверить наличие правил НЕ по умолчанию.
      var hasNotDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      
      // Проверить, есть ли похожее правило: по умолчанию, внутренний документопоток,
      // в правиле указаны виды документов, все виды документов проектного типа.
      var hasDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule == true &&
                                                          r.DocumentFlow == Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Inner &&
                                                          r.DocumentKinds.Any() &&
                                                          !r.DocumentKinds.Any(dk => !Equals(dk.DocumentKind.DocumentType.DocumentTypeGuid, ProjectDocument.ClassTypeGuid.ToString())));
      
      if (hasNotDefaultRule || hasDefaultRule)
        return;
      
      var projectDocumentKinds = Docflow.DocumentKinds.GetAll().Where(pdk => pdk.DocumentType.DocumentTypeGuid == ProjectDocument.ClassTypeGuid.ToString()).ToList();
      
      var rule = ApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = name;
      rule.DocumentFlow = Sungero.Docflow.ApprovalRuleBase.DocumentFlow.Inner;
      rule.IsDefaultRule = true;
      
      // Задать виды документов в правиле.
      if (projectDocumentKinds != null && !rule.DocumentKinds.Any())
        foreach (var documentKind in projectDocumentKinds)
          rule.DocumentKinds.AddNew().DocumentKind = documentKind;
      
      // Проверить, есть ли этап согласования с РП.
      var projectManagerRole = Docflow.PublicFunctions.ApprovalRoleBase.GetRole(Docflow.ApprovalRoleBase.Type.ProjectManager);
      var projectManagerApprovingStage = ApprovalStages.GetAll().FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Approvers &&
                                                                                s.ApprovalRoles.Any(ar => Equals(ar.ApprovalRole, projectManagerRole)) &&
                                                                                s.ApprovalRoles.Count() == 1);
      
      // Создать этап согласования с РП, если это необходимо.
      var stage = ApprovalStages.Create();
      
      if (projectManagerApprovingStage == null)
      {
        stage.StageType = Docflow.ApprovalStage.StageType.Approvers;
        stage.Name = Sungero.Projects.Resources.ApprovingByProjectManager;
        stage.DeadlineInDays = 1;
        stage.ApprovalRoles.AddNew().ApprovalRole = projectManagerRole;
        stage.Save();
      }
      else
        stage = projectManagerApprovingStage;
      
      // Добавить стандартные этапы.
      Docflow.PublicInitializationFunctions.Module.SetRuleStages(rule, stages);
      
      // Заменяем созданный по умолчанию этап согласования с обязательными согласующими созданным/найденным ранее этапом.
      // Функция SetRuleStages при генерации подставляет первый попавшийся этап с нужным типом.
      var replacedDefaultStage = rule.Stages.FirstOrDefault(s => Equals(s.StageType, Docflow.ApprovalStage.StageType.Approvers));
      if (replacedDefaultStage != null)
        rule.Stages.Single(s => Equals(s, replacedDefaultStage)).Stage = stage;
      
      // Создать связи по умолчанию.
      Docflow.PublicFunctions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
    }
    
    /// <summary>
    /// Создать роль согласования.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание роли.</param>
    [Public]
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      InitializationLogger.DebugFormat("Init: Create approval rule {0}", ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(roleType));
      
      var role = ProjectApprovalRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
      {
        role = ProjectApprovalRoles.Create();
        role.Type = roleType;
      }
      role.Description = description;
      role.Save();
    }
    
    #endregion
    
    #region Создание видов и типов документов
    
    /// <summary>
    /// Создать типы проектных документов.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Projects.Resources.ProjectTypeName, ProjectDocument.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
    }
    
    /// <summary>
    /// Создать виды проектных документов.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var notifiable = Docflow.DocumentKind.NumberingType.Registrable;
      var numerable = Docflow.DocumentKind.NumberingType.Numerable;
      var notNumerable = Docflow.DocumentKind.NumberingType.NotNumerable;
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.CustomerRequirementsKindName,
                                                                      Sungero.Projects.Resources.CustomerRequirementsKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.CustomerRequirementsKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.RegulationsKindName,
                                                                      Sungero.Projects.Resources.RegulationsKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.RegulationsKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.ReportKindName, Sungero.Projects.Resources.ReportKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.ReportKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.ScheduleKindName, Sungero.Projects.Resources.ScheduleKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.ScheduleKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.ProjectSolutionKindName, Sungero.Projects.Resources.ProjectSolutionKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.ProjectSolutionKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Projects.Resources.AnalyticNoteKindName, Sungero.Projects.Resources.AnalyticNoteKindShortName, notNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, ProjectDocument.ClassTypeGuid, null, true, true,
                                                                      Init.AnalyticNoteKind, false);
    }
    
    #endregion
    
    #region Создание видов проектов
    
    /// <summary>
    /// Создать виды проектов.
    /// </summary>
    public static void CreateProjectKinds()
    {
      CreateProjectKind(Resources.ProjectKindNameInvestment, Init.ProjectKindInvestment);
      CreateProjectKind(Resources.ProjectKindNameInformationTechnology, Init.ProjectKindInformationTechnology);
      CreateProjectKind(Resources.ProjectKindNameOrganizationDevelopment, Init.ProjectKindOrganizationDevelopment);
      CreateProjectKind(Resources.ProjectKindNameCreatingNewProduct, Init.ProjectKindCreatingNewProduct);
      CreateProjectKind(Resources.ProjectKindNameOrganizationSale, Init.ProjectKindOrganizationSale);
      CreateProjectKind(Resources.ProjectKindNameMarketing, Init.ProjectKindMarketing);
    }
    
    /// <summary>
    /// Создать вид проекта.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="entityId">ИД экземпляра, созданного при инициализации.</param>
    [Public]
    public static void CreateProjectKind(string name, Guid entityId)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(ProjectKind.ClassTypeGuid, entityId);
      if (externalLink != null)
        return;
      
      InitializationLogger.DebugFormat("Init: Create project kind '{0}'.", name);
      
      var projectKind = ProjectKinds.Create();
      projectKind.Name = name;
      projectKind.Save();
      
      Docflow.PublicFunctions.Module.CreateExternalLink(projectKind, entityId);
    }
    
    #endregion
    
    #region Создание иерархии папок по проектам
    
    [Public]
    public static void CreateProjectFolder()
    {
      var shared = Core.SpecialFolders.Shared;
      var projectFolder = Folders.GetAll().SingleOrDefault(f => f.Uid == Constants.Module.ProjectFolders.ProjectFolderUid);
      if (projectFolder == null)
      {
        InitializationLogger.Debug("Init: Create default projects folder.");
        projectFolder = Folders.Create();
        projectFolder.Name = Resources.ProjectFolderName;
        projectFolder.Uid = Constants.Module.ProjectFolders.ProjectFolderUid;
        projectFolder.Save();
      }
      shared.Items.Add(projectFolder);
      
      var archiveFolder = Folders.GetAll().SingleOrDefault(f => f.Uid == Constants.Module.ProjectFolders.ProjectArhiveFolderUid);
      if (archiveFolder == null)
      {
        InitializationLogger.Debug("Init: Create default projects archive folder.");
        archiveFolder = Folders.Create();
        archiveFolder.Name = Resources.ProjectArhiveFolderName;
        archiveFolder.Uid = Constants.Module.ProjectFolders.ProjectArhiveFolderUid;
        archiveFolder.Save();
      }
      projectFolder.Items.Add(archiveFolder);
      
      var role = Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
      if (role == null)
        return;

      projectFolder.AccessRights.Grant(role, Docflow.Constants.Module.DefaultAccessRightsTypeSid.ChangeContent);
      archiveFolder.AccessRights.Grant(role, Docflow.Constants.Module.DefaultAccessRightsTypeSid.ChangeContent);
      projectFolder.AccessRights.Save();
      archiveFolder.AccessRights.Save();
    }
    
    #endregion
    
    #region Выдача прав роли "Руководители проектов"
    
    /// <summary>
    /// Выдать права на виды проектов для роли "Руководители проектов".
    /// </summary>
    public static void GrantRightsOnProjects()
    {
      var role = Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
      if (role == null)
        return;

      Sungero.Projects.ProjectKinds.AccessRights.Grant(role, DefaultAccessRightsTypes.Change);
      Sungero.Projects.ProjectKinds.AccessRights.Save();
      
      var team = ProjectTeams.GetAll(t => t.Sid == Constants.Module.RoleGuid.ParentProjectTeam).FirstOrDefault();
      if (team != null)
      {
        team.AccessRights.Grant(role, DefaultAccessRightsTypes.Change);
        team.AccessRights.Save();
      }
    }
    
    #endregion
    
    /// <summary>
    /// Выдать права всем пользователям на справочники и документы.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents to all users.");
      
      Sungero.Projects.ProjectDocuments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      Sungero.Projects.ProjectDocuments.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    [Remote]
    public static void GrantRightsOnDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      
      ProjectKinds.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ProjectKinds.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на вычислимую папку Документы по проектам.
    /// </summary>
    public static void GrantReadRightsOnProjectDocuments()
    {
      InitializationLogger.Debug("Init: Grant rights on ProjectDocuments folder to all users.");
      
      var role = Roles.AllUsers;
      Sungero.Projects.SpecialFolders.ProjectDocuments.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      Sungero.Projects.SpecialFolders.ProjectDocuments.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на Проектные команды.
    /// </summary>
    public static void GrantReadRightsOnProjectTeam()
    {
      InitializationLogger.Debug("Init: Grant rights on ProjectTeam to all users.");
      
      var role = Roles.AllUsers;
      Sungero.Projects.ProjectTeams.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      Sungero.Projects.ProjectTeams.AccessRights.Save();
    }
  }
}

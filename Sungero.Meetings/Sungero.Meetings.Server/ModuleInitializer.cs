using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Meetings.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Выдача прав роли "Ответственные за совещания".
      InitializationLogger.Debug("Init: Grant right on financial documents for responsible.");
      GrantRightToMeetingResponsible();
      
      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateDocumentRegistersAndSettings();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.NameMeetingResponsibleRole, Resources.DescriptionMeetingResponsibleRole, Constants.Module.MeetingResponsibleRole);
    }
    
    /// <summary>
    /// Выдать права роли "Ответственные за совещания".
    /// </summary>
    public static void GrantRightToMeetingResponsible()
    {
      InitializationLogger.Debug("Init: Grant rights on meeting to responsible managers.");
      
      var meetingResponsible = Roles.GetAll().Where(n => n.Sid == Constants.Module.MeetingResponsibleRole).FirstOrDefault();
      if (meetingResponsible == null)
        return;

      // Права на документы.
      Agendas.AccessRights.Grant(meetingResponsible, DefaultAccessRightsTypes.Create);
      Agendas.AccessRights.Save();
      
      // Права на справочники.
      Meetings.AccessRights.Grant(meetingResponsible, DefaultAccessRightsTypes.Create);
      Meetings.AccessRights.Save();
      
      // Права на спец. папки.
      var allUsers = Roles.AllUsers;
      GrantRightOnFolders(allUsers);
    }
    
    /// <summary>
    /// Выдать права на спец.папки.
    /// </summary>
    /// <param name="role">Роль.</param>
    public static void GrantRightOnFolders(IRole role)
    {
      var hasLicense = Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Guid.Parse("6ea9a047-b597-42eb-8f90-da8c559dd057"));
      Dictionary<int, byte[]> licenses = null;
      
      try
      {
        if (!hasLicense)
        {
          licenses = Docflow.PublicFunctions.Module.ReadLicense();
          Docflow.PublicFunctions.Module.DeleteLicense();
        }
        
        // Права на папку "Протоколы совещаний".
        MeetingsUI.SpecialFolders.Minutes.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        MeetingsUI.SpecialFolders.Minutes.AccessRights.Save();
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
    }
    
    /// <summary>
    /// Создать типы документов для модуля совещания.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.AgendaTypeName, Agenda.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Inner, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.MinutesTypeName, Minutes.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Inner, true);
    }
    
    /// <summary>
    /// Создать виды документов для модуля совещания.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.AgendaKindName,
                                                                      Resources.AgendaKindShortName,
                                                                      Docflow.DocumentKind.NumberingType.NotNumerable,
                                                                      Docflow.DocumentKind.DocumentFlow.Inner, true, false, Agenda.ClassTypeGuid,
                                                                      new Domain.Shared.IActionInfo[] { Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval },
                                                                      Constants.Module.AgendaKind);
      
      var minutesActions = new Domain.Shared.IActionInfo[] {
        Docflow.OfficialDocuments.Info.Actions.SendActionItem,
        Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval,
        Docflow.OfficialDocuments.Info.Actions.SendForApproval,
        Docflow.OfficialDocuments.Info.Actions.SendForAcquaintance };
      
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.MinutesKindName,
                                                                      Resources.MinutesKindShortName,
                                                                      Docflow.DocumentKind.NumberingType.Numerable, Docflow.DocumentKind.DocumentFlow.Inner,
                                                                      true, false, Minutes.ClassTypeGuid, minutesActions,
                                                                      Constants.Module.MinutesKind);
    }
    
    /// <summary>
    /// Создать журналы и настройки регистрации.
    /// </summary>
    public static void CreateDocumentRegistersAndSettings()
    {
      InitializationLogger.Debug("Init: Create default document registers and settings for docflow.");
      
      var minutesRegister = Docflow.PublicInitializationFunctions.Module.CreateYearSectionDocumentRegister(Resources.RegistersAndSettingsMinutesName,
                                                                                                           Resources.RegistersAndSettingsMinutesIndex,
                                                                                                           Constants.Module.MinutesRegister);
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(Minutes.ClassTypeGuid, Docflow.RegistrationSetting.DocumentFlow.Inner, minutesRegister);
    }
  }
}

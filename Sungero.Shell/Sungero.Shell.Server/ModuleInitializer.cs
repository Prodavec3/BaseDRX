using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Shell.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        Logger.Debug("Init: Grant right on shell special folders to all users.");
        GrantRightOnFolders(allUsers);
      }
    }
    
    /// <summary>
    /// Выдать права на спец. папки модуля.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightOnFolders(IRole allUsers)
    {
      Logger.Debug("Init: Grant right on shell special folders to all users.");
      
      Sungero.Shell.SpecialFolders.Approval.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.Notices.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnApproval.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnCheking.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnDocumentProcessing.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnPrint.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnRegister.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnReview.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnRework.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnSigning.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.ExchangeDocumentProcessing.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnVerification.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Sungero.Shell.SpecialFolders.OnAcquaintance.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      
      Sungero.Shell.SpecialFolders.Approval.AccessRights.Save();
      Sungero.Shell.SpecialFolders.Notices.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnApproval.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnCheking.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnDocumentProcessing.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnPrint.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnRegister.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnReview.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnRework.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnSigning.AccessRights.Save();
      Sungero.Shell.SpecialFolders.ExchangeDocumentProcessing.AccessRights.Save();
      Sungero.Shell.SpecialFolders.OnVerification.Save();
      Sungero.Shell.SpecialFolders.OnAcquaintance.Save();
    }
  }
}

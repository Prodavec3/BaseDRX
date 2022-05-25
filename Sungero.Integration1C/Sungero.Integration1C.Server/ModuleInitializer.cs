using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Integration1C.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");      
      Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.Synchronization1CResponsibleRoleName, Resources.Synchronization1CResponsibleRoleDescription, Integration1C.Constants.Module.SynchronizationResponsibleRoleGuid);
    }
  }
}

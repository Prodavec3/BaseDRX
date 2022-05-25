using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Commons.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      GrantRigthsToExternalEntityLinks();
      CreateExternalEntityLinksIndexes();
      CreateCountryRegionsCitiesFromFIAS();
    }
    
    /// <summary>
    /// Выдать права на чтение справочника ExternalEntityLink всем пользователям.
    /// </summary>
    private static void GrantRigthsToExternalEntityLinks()
    {
      // Получить роль "Все пользователи", не создавая зависимость от модуля Docflow.
      var allUsers = Roles.AllUsers;
      
      ExternalEntityLinks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ExternalEntityLinks.AccessRights.Save();
    }
    
    /// <summary>
    /// Создать прикладные индексы для справочника ExternalEntityLink.
    /// </summary>
    private static void CreateExternalEntityLinksIndexes()
    {
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntLink_EId_EType_EEType_ESId_SD);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntLink_EEId_ESId);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EELinks_Discr_EId_EEType_ESysId_SyncDate);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.idx_EEntityLinks_Disc_ExtEntityId_ExtSystemId);
    }
    
    /// <summary>
    /// Создать страну, регионы и города согласно ФИАС.
    /// </summary>
    public static void CreateCountryRegionsCitiesFromFIAS()
    {
      if (Functions.Module.IsServerCultureRussian() && !Countries.GetAll().Any())
      {
        InitializationLogger.DebugFormat("Init: Create country, regions and cities.");
        Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.CreateCountryRegionsCitiesFromFIAS);
      }
    }
    
  }
}

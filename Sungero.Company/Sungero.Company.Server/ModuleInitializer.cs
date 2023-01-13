using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace Sungero.Company.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateReportsTables();
      GrantRightsToResponsibilitiesReport();
      CreateSorageProcedures();
      
      InitializationLogger.Debug("Init: Create visibility settings.");
      CreateVisibilitySettings();
      CreateIndecies();
    }
    
    /// <summary>
    /// Выдать права на отчет о полномочиях.
    /// </summary>
    public static void GrantRightsToResponsibilitiesReport()
    {
      var role = Roles.AllUsers;
      if (role != null)
        Reports.AccessRights.Grant(Reports.GetResponsibilitiesReport().Info, role, DefaultReportAccessRightsTypes.Execute);
    }
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var responsibilitiesReportTableName = Constants.ResponsibilitiesReport.ResponsibilitiesReportTableName;
      Docflow.PublicFunctions.Module.DropReportTempTable(responsibilitiesReportTableName);
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ResponsibilitiesReport.CreateResponsibilitiesReportTable, new[] { responsibilitiesReportTableName });
    }
    
    /// <summary>
    /// Создать хранимые процедуры.
    /// </summary>
    public static void CreateSorageProcedures()
    {
      var getHeadRecipientsByEmployeeProcedureName = Constants.Module.GetHeadRecipientsByEmployeeProcedureName;
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.CreateProcedureGetHeadRecipientsByEmployee, new[] { getHeadRecipientsByEmployeeProcedureName });
      
      var getAllVisibleRecipientsProcedureName = Constants.Module.GetAllVisibleRecipientsProcedureName;
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.CreateProcedureGetAllVisibleRecipients, new[] { getAllVisibleRecipientsProcedureName, getHeadRecipientsByEmployeeProcedureName });
    }
    
    /// <summary>
    /// Создать настройки видимости организационной структуры.
    /// </summary>
    public static void CreateVisibilitySettings()
    {
      var visibilitySettings = Functions.Module.GetVisibilitySettings();
      if (visibilitySettings == null)
        Functions.Module.CreateVisibilitySettings();
    }
    
    /// <summary>
    /// Создать индексы для таблиц модуля.
    /// </summary>
    public static void CreateIndecies()
    {
      // Индексы для режима ограничения оргструктуры.
      Logger.Debug("Create index for recipient visibility restriction.");
      var command = string.Format(Queries.Module.CreateIndexForRecipientVisibility);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      
      // Индексы для Sungero_Core_Recipient.
      var tableName = Constants.Module.RecipientTableName;
      Logger.DebugFormat("Create index for {0}.", tableName);

      var indexName = "idx_Recipient_Discriminator_Parent_Id";
      var indexQuery = string.Format(Queries.Module.CreateIndexDiscriminatorParentId, tableName, indexName);
      Sungero.Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);

      indexName = "idx_Recipient_Discriminator_BusinessUnit_Id";
      indexQuery = string.Format(Queries.Module.CreateIndexDiscriminatorBusinessUnitId, tableName, indexName);
      Sungero.Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
      
      indexName = "idx_Recipient_Discriminator_HeadOffice_Id";
      indexQuery = string.Format(Queries.Module.CreateIndexDiscriminatorHeadOfficeId, tableName, indexName);
      Sungero.Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
  }
}

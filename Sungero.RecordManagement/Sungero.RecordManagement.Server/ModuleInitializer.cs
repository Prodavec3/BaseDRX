using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain;
using Sungero.Domain.Initialization;
using Sungero.Domain.Shared;
using Sungero.Workflow;
using Init = Sungero.RecordManagement.Constants.Module.Initialize;

namespace Sungero.RecordManagement.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Справочники.
        GrantRightsOnDatabooks(allUsers);
        
        // Документы.
        GrantRightsOnDocuments(allUsers);
        
        // Задачи.
        GrantRightsOnTasks(allUsers);
        
        // Спец.папки.
        GrantRightOnFolders(allUsers);
        
        // Отчеты.
        InitializationLogger.Debug("Init: Grant right on reports to all users.");
        Reports.AccessRights.Grant(Reports.GetActionItemsExecutionReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetDocumentReturnReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetAcquaintanceReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetAcquaintanceFormReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        Reports.AccessRights.Grant(Reports.GetDraftResolutionReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      }
      
      // Выдача дополнительных прав роли "Делопроизводители".
      GrantRightsToClerk();
      
      // Выдача прав ролям безопасности.
      InitializationLogger.Debug("Init: Grant right for security roles.");
      GrantRightsToRegistrationIncomingRole();
      GrantRightsToRegistrationOutgoingRole();
      GrantRightsToRegistrationInternalRole();
      
      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateAssignmentIndex();
      CreateTaskIndex();
      CreateReportsTables();
      CreateRecordManagementSettings();
      
      // Добавление в таблицу параметров ограничения исполнителей для задачи на ознакомление.
      AddAcquaintanceTaskPerformersLimit();
    }

    #region Выдача прав Всем пользователям
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
      
      RecordManagement.RecordManagementSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RecordManagement.AcquaintanceLists.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.AcquaintanceTaskParticipants.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.FullAccess);
      RecordManagement.RecordManagementSettings.AccessRights.Save();
      RecordManagement.AcquaintanceLists.AccessRights.Save();
      RecordManagement.AcquaintanceTaskParticipants.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права всем пользователям на документы.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents to all users.");

      RecordManagement.OrderBases.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.OutgoingLetters.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.IncomingLetters.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.OrderBases.AccessRights.Save();
      RecordManagement.OutgoingLetters.AccessRights.Save();
      RecordManagement.IncomingLetters.AccessRights.Save();
    }

    /// <summary>
    /// Выдать права всем пользователям на задачи.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnTasks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on tasks to all users.");
      
      RecordManagement.DocumentReviewTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.ActionItemExecutionTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.StatusReportRequestTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.DeadlineExtensionTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RecordManagement.AcquaintanceTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      
      RecordManagement.DocumentReviewTasks.AccessRights.Save();
      RecordManagement.ActionItemExecutionTasks.AccessRights.Save();
      RecordManagement.StatusReportRequestTasks.AccessRights.Save();
      RecordManagement.DeadlineExtensionTasks.AccessRights.Save();
      RecordManagement.AcquaintanceTasks.AccessRights.Save();
    }

    /// <summary>
    /// Выдать права всем пользователям на спец.папки.
    /// </summary>
    /// <param name="allUsers">Роль "Все пользователи".</param>
    public static void GrantRightOnFolders(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant right on special folders to all users.");
      
      RecordManagement.SpecialFolders.ForExecution.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RecordManagement.SpecialFolders.ActionItems.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RecordManagement.SpecialFolders.ForExecution.AccessRights.Save();
      RecordManagement.SpecialFolders.ActionItems.AccessRights.Save();
      
      var hasLicense = Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Guid.Parse("51247c94-981f-4bc8-819a-128704b5aa31"));
      Dictionary<int, byte[]> licenses = null;
      
      try
      {
        if (!hasLicense)
        {
          licenses = Docflow.PublicFunctions.Module.ReadLicense();
          Docflow.PublicFunctions.Module.DeleteLicense();
        }
        
        RecordManagementUI.SpecialFolders.DocumentsToReturn.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        RecordManagementUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        RecordManagementUI.SpecialFolders.DocumentsToReturn.AccessRights.Save();
        RecordManagementUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Save();
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
    }

    #endregion

    #region Выдача прав ролям

    /// <summary>
    /// Назначить права роли "Делопроизводители".
    /// </summary>
    public static void GrantRightsToClerk()
    {
      InitializationLogger.Debug("Init: Grant rights on reports to clerks");

      var clerks = Docflow.PublicFunctions.DocumentRegister.Remote.GetClerks();
      if (clerks == null)
        return;

      // Права на отчеты модуля.
      Reports.AccessRights.Grant(Reports.GetIncomingDocumentsReport().Info, clerks, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetOutgoingDocumentsReport().Info, clerks, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetInternalDocumentsReport().Info, clerks, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetIncomingDocumentsProcessingReport().Info, clerks, DefaultReportAccessRightsTypes.Execute);
    }

    /// <summary>
    /// Выдать права роли "Регистраторы входящих документов".
    /// </summary>
    public static void GrantRightsToRegistrationIncomingRole()
    {
      InitializationLogger.Debug("Init: Grant rights on documents to registration incoming document role.");

      var registrationRole = Roles.GetAll().FirstOrDefault(r => r.Sid == Docflow.Constants.Module.RoleGuid.RegistrationIncomingDocument);
      if (registrationRole == null)
        return;

      // Права на документы.
      RecordManagement.IncomingLetters.AccessRights.Grant(registrationRole, Docflow.Constants.Module.DefaultAccessRightsTypeSid.Register);
      RecordManagement.IncomingLetters.AccessRights.Save();
    }

    /// <summary>
    /// Выдать права роли "Регистраторы исходящих документов".
    /// </summary>
    public static void GrantRightsToRegistrationOutgoingRole()
    {
      InitializationLogger.Debug("Init: Grant rights on documents to registration outgoing document role.");

      var registrationRole = Roles.GetAll().FirstOrDefault(r => r.Sid == Docflow.Constants.Module.RoleGuid.RegistrationOutgoingDocument);
      if (registrationRole == null)
        return;

      // Права на документы.
      RecordManagement.OutgoingLetters.AccessRights.Grant(registrationRole, Docflow.Constants.Module.DefaultAccessRightsTypeSid.Register);
      RecordManagement.OutgoingLetters.AccessRights.Save();
    }

    /// <summary>
    /// Выдать права роли "Регистраторы внутренних документов".
    /// </summary>
    public static void GrantRightsToRegistrationInternalRole()
    {
      InitializationLogger.Debug("Init: Grant rights on documents to registration internal document role.");

      var registrationRole = Roles.GetAll().FirstOrDefault(r => r.Sid == Docflow.Constants.Module.RoleGuid.RegistrationInternalDocument);
      if (registrationRole == null)
        return;

      // Права на документы.
      RecordManagement.OrderBases.AccessRights.Grant(registrationRole, Docflow.Constants.Module.DefaultAccessRightsTypeSid.Register);
      RecordManagement.OrderBases.AccessRights.Save();
    }

    #endregion

    #region Создание видов и типов документов

    /// <summary>
    /// Создать типы документов для делопроизводства.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");

      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(RecordManagement.Resources.IncomingLetterTypeName, IncomingLetter.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Incoming, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(RecordManagement.Resources.OutgoingLetterTypeName, OutgoingLetter.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Outgoing, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(RecordManagement.Resources.OrderTypeName, Order.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(RecordManagement.Resources.CompanyDirectiveTypeName, CompanyDirective.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Inner, true);
    }

    /// <summary>
    /// Создать виды документов для делопроизводства.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");

      var notifiable = Docflow.DocumentKind.NumberingType.Registrable;
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(RecordManagement.Resources.IncomingLetterKindName, RecordManagement.Resources.IncomingLetterKindShortName, notifiable, Docflow.DocumentRegister.DocumentFlow.Incoming,
                                                                      true, false, IncomingLetter.ClassTypeGuid, null,
                                                                      Init.IncomingLetterKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(RecordManagement.Resources.OutgoingLetterKindName, RecordManagement.Resources.OutgoingLetterKindShortName, notifiable, Docflow.DocumentRegister.DocumentFlow.Outgoing,
                                                                      true, false, OutgoingLetter.ClassTypeGuid, null, Init.OutgoingLetterKind);
      
      var actions = new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendActionItem,
        OfficialDocuments.Info.Actions.SendForApproval,
        OfficialDocuments.Info.Actions.SendForFreeApproval,
        OfficialDocuments.Info.Actions.SendForAcquaintance };
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(RecordManagement.Resources.OrderKindName, RecordManagement.Resources.OrderKindShortName, notifiable, DocumentFlow.Inner,
                                                                      true, false, Order.ClassTypeGuid,
                                                                      actions,
                                                                      Init.OrderKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(RecordManagement.Resources.CompanyDirectiveKindName, RecordManagement.Resources.CompanyDirectiveKindShortName, notifiable, DocumentFlow.Inner,
                                                                      true, false, CompanyDirective.ClassTypeGuid,
                                                                      actions,
                                                                      Init.CompanyDirective);
    }

    #endregion

    #region Создание прикладных индексов в бд

    public static void CreateAssignmentIndex()
    {
      var tableName = "Sungero_WF_Assignment";
      var indexName = "idx_Asg_Status_Discriminator_Performer";
      var indexQuery = string.Format(Queries.Module.SungeroWFAssignmentIndex1Query, tableName, indexName);
      
      Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }

    [Public]
    public static void CreateTaskIndex()
    {
      // Проверить наличие индекса.
      var tableName = "Sungero_WF_Task";
      var indexName = "idx_Task_Discriminator_Status_Supervisor";
      var indexQuery = string.Format(Queries.Module.SungeroWFTaskIndex0Query, tableName, indexName);
      
      Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }

    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var incomingDocumentsProcessingReportTableName = Constants.IncomingDocumentsProcessingReport.IncomingDocumentsProcessingReportTableName;
      var actionItemExecutionReportTableName = Sungero.RecordManagement.Constants.ActionItemsExecutionReport.SourceTableName;
      var incomingDocumentsReportTableName = Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName;
      var documentsReturnReportTableName = Sungero.RecordManagement.Constants.DocumentReturnReport.SourceTableName;
      var acquaintanceReportTableName = Sungero.RecordManagement.Constants.AcquaintanceReport.SourceTableName;
      var acquaintanceFormReportTableName = Sungero.RecordManagement.Constants.AcquaintanceFormReport.SourceTableName;
      var draftResolutionReportTableName = Sungero.RecordManagement.Constants.DraftResolutionReport.SourceTableName;
      var actionItemPrintReportTableName = Sungero.RecordManagement.Constants.ActionItemPrintReport.SourceTableName;
      var internalDocumentsReportTableName = Sungero.RecordManagement.Constants.InternalDocumentsReport.SourceTableName;
      var outgoingDocumentsReportTableName = Sungero.RecordManagement.Constants.OutgoingDocumentsReport.SourceTableName;
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { incomingDocumentsProcessingReportTableName,
                                                            actionItemExecutionReportTableName,
                                                            incomingDocumentsReportTableName,
                                                            documentsReturnReportTableName,
                                                            acquaintanceReportTableName,
                                                            acquaintanceFormReportTableName,
                                                            draftResolutionReportTableName,
                                                            actionItemPrintReportTableName,
                                                            internalDocumentsReportTableName,
                                                            outgoingDocumentsReportTableName });

      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.IncomingDocumentsProcessingReport.CreateIncomingDocumentsProcessingReportSourceTable, new[] { incomingDocumentsProcessingReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ActionItemsExecutionReport.CreateActionItemExecutionReportSourceTable, new[] { actionItemExecutionReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.IncomingDocumentsReport.CreateIncomingDocumentsSourceTable, new[] { incomingDocumentsReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.DocumentReturnReport.CreateDocumentsReturnReportTable, new[] { documentsReturnReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.AcquaintanceReport.CreateAcquaintanceReportTable, new[] { acquaintanceReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.AcquaintanceFormReport.CreateAcquaintanceFormReportTable, new[] { acquaintanceFormReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.DraftResolutionReport.CreateDraftResolutionReportTable, new[] { draftResolutionReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ActionItemPrintReport.CreateActionItemPrintReportTable, new[] { actionItemPrintReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.InternalDocumentsReport.CreateSourceTable, new[] { internalDocumentsReportTableName });
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.OutgoingDocumentsReport.CreateSourceTable, new[] { outgoingDocumentsReportTableName });
    }
    
    #endregion
    
    #region Добавление в таблицу параметров ограничения исполнителей для задачи на ознакомление
    
    public static void AddAcquaintanceTaskPerformersLimit()
    {
      InitializationLogger.Debug("Init: Adding performers limit of acquaintance task .");
      
      if (Docflow.PublicFunctions.Module.GetDocflowParamsValue(Constants.AcquaintanceTask.PerformersLimitParamName) == null)
        Docflow.PublicFunctions.Module.InsertOrUpdateDocflowParam(Constants.AcquaintanceTask.PerformersLimitParamName, Constants.AcquaintanceTask.DefaultPerformersLimit);
    }
    
    #endregion
    
    #region Добавление параметров модуля
    
    /// <summary>
    /// Создать параметры модуля.
    /// </summary>
    public static void CreateRecordManagementSettings()
    {
      var recordManagementSettings = Functions.Module.GetSettings();
      if (recordManagementSettings == null)
        Functions.Module.CreateSettings();
    }
    
    #endregion
  }
}

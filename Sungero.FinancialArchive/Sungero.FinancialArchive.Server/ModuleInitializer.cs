using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Domain.Initialization;
using Init = Sungero.FinancialArchive.Constants.Module.Initialize;

namespace Sungero.FinancialArchive.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Выдача прав роли "Ответственные за финансовый архив".
      InitializationLogger.Debug("Init: Grant right on financial documents for responsible.");
      GrantRightToFinancialResponsible();

      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateFinancialDocumentRegistersAndSettings();
      CreateDefaultFinancialApprovalRules();
      CreateReportsTables();
    }

    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      
      Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.RoleNameFinancialArchiveResponsible, Resources.DescriptionFinancialArchiveResponsible, FinancialArchive.Constants.Module.FinancialArchiveResponsibleRole);
    }
    
    /// <summary>
    /// Выдать права роли "Ответственные за финансовый архив".
    /// </summary>
    public static void GrantRightToFinancialResponsible()
    {
      InitializationLogger.Debug("Init: Grant rights on financial document to responsible managers.");
      
      var financialResponsible = Roles.GetAll().Where(n => n.Sid == FinancialArchive.Constants.Module.FinancialArchiveResponsibleRole).FirstOrDefault();
      if (financialResponsible == null)
        return;
      
      var allUsers = Roles.AllUsers;

      // Если нет лицензии на финансовые документы (есть такие неудачные лицензии, где оно требуется), то используем РОФ.
      var hasLicense = Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Guid.Parse("59797aba-7718-45df-8ac1-5bb7a36c7a66"));
      Dictionary<int, byte[]> licenses = null;
      try
      {
        if (!hasLicense)
        {
          licenses = Docflow.PublicFunctions.Module.ReadLicense();
          Docflow.PublicFunctions.Module.DeleteLicense();
        }
        
        // Права на документы.
        IncomingTaxInvoices.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        OutgoingTaxInvoices.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        Waybills.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        ContractStatements.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        UniversalTransferDocuments.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        IncomingTaxInvoices.AccessRights.Save();
        OutgoingTaxInvoices.AccessRights.Save();
        Waybills.AccessRights.Save();
        ContractStatements.AccessRights.Save();
        UniversalTransferDocuments.AccessRights.Save();
        
        Contracts.OutgoingInvoices.AccessRights.Grant(financialResponsible, DefaultAccessRightsTypes.Create);
        Contracts.OutgoingInvoices.AccessRights.Save();
        
        // Права на отчет.
        Reports.AccessRights.Grant(Reports.GetFinArchiveExportReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
      
      GrantRightOnFolders(allUsers);
    }
    
    /// <summary>
    /// Выдать права на спец.папки.
    /// </summary>
    /// <param name="role">Роль.</param>
    public static void GrantRightOnFolders(IRole role)
    {
      var hasLicense = Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Guid.Parse("e99ae7e2-edb7-4904-a19a-4577f07609a4"));
      Dictionary<int, byte[]> licenses = null;
      
      try
      {
        if (!hasLicense)
        {
          licenses = Docflow.PublicFunctions.Module.ReadLicense();
          Docflow.PublicFunctions.Module.DeleteLicense();
        }
        
        // Права на папку "Договоры и доп.согл.".
        FinancialArchiveUI.SpecialFolders.FinContractList.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        FinancialArchiveUI.SpecialFolders.FinContractList.AccessRights.Save();
        
        // Права на папку "Отсутствуют скан-копии".
        FinancialArchiveUI.SpecialFolders.DocumentsWithoutScan.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        FinancialArchiveUI.SpecialFolders.DocumentsWithoutScan.AccessRights.Save();

        // Права на папку "Ожидают подписания".
        FinancialArchiveUI.SpecialFolders.SignAwaitedDocuments.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        FinancialArchiveUI.SpecialFolders.SignAwaitedDocuments.AccessRights.Save();
        
        // Права на папку "Реестр доверенностей".
        FinancialArchiveUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        FinancialArchiveUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Save();
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
    }
    
    /// <summary>
    /// Получить вид документа, созданный при инициализации.
    /// </summary>
    /// <param name="documentKindEntityGuid">ИД экземпляра, созданного при инициализации.</param>
    /// <returns>Вид документа.</returns>
    public static Docflow.IDocumentKind GetDefaultDocumentKind(Guid documentKindEntityGuid)
    {
      var externalLink = Docflow.PublicFunctions.Module.GetExternalLink(Init.DocumentKindTypeGuid, documentKindEntityGuid);
      
      return Docflow.DocumentKinds.GetAll().Where(x => x.Id == externalLink.EntityId).FirstOrDefault();
    }
    
    /// <summary>
    /// Создать правило по умолчанию.
    /// </summary>
    /// <param name="ruleName">Имя правила.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Созданное правило. Если правило создано не было, то null.</returns>
    [Public]
    public static Contracts.IContractsApprovalRule CreateDefaultRule(string ruleName, Enumeration documentFlow, List<Enumeration> stages)
    {
      var hasNotDefaultRule = Docflow.ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      var hasDefaultRule = Docflow.ApprovalRuleBases.GetAll().Any(r => r.DocumentFlow == documentFlow
                                                                  && r.DocumentKinds.Any(d => d.DocumentKind.DocumentType.DocumentTypeGuid == Waybill.ClassTypeGuid.ToString() ||
                                                                                         d.DocumentKind.DocumentType.DocumentTypeGuid == UniversalTransferDocument.ClassTypeGuid.ToString() ||
                                                                                         d.DocumentKind.DocumentType.DocumentTypeGuid == ContractStatement.ClassTypeGuid.ToString()));
      
      if (hasNotDefaultRule || hasDefaultRule)
        return null;
      
      var rule = Contracts.ContractsApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = ruleName;
      rule.DocumentFlow = documentFlow;
      rule.IsDefaultRule = true;
      
      // Виды финансовых документов.
      var documentKindsGuids = new List<Guid> { Init.ContractStatementKind, Init.WaybillDocumentKind, Init.UniversalTaxInvoiceAndBasicKind, Init.UniversalBasicKind };
      
      foreach (var docKindGuid in documentKindsGuids)
      {
        var docKind = GetDefaultDocumentKind(docKindGuid);
        
        if (docKind != null)
          rule.DocumentKinds.AddNew().DocumentKind = docKind;
      }
      
      Docflow.PublicInitializationFunctions.Module.SetRuleStages(rule, stages);
      Docflow.PublicFunctions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
      return rule;
    }
    
    /// <summary>
    /// Создать правила согласования по умолчанию для финансовых документов.
    /// </summary>
    public static void CreateDefaultFinancialApprovalRules()
    {
      InitializationLogger.Debug("Init: Create default financial approval rules.");
      
      var stages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Print, StageType.Sign, StageType.Sending, StageType.CheckReturn, StageType.Notice };
      
      var rule = CreateDefaultRule(Resources.DefaultApprovalRuleNameFinancial,
                                   Docflow.ApprovalRuleBase.DocumentFlow.Contracts,
                                   stages);
      
      // Добавить условие по способу отправки и непосредственный руководитель - подписывающий, для созданного правила.
      if (rule != null)
      {
        var condition = Contracts.ContractConditions.Create();
        condition.ConditionType = Docflow.ConditionBase.ConditionType.DeliveryMethod;
        var newDeliveryMethod = condition.DeliveryMethods.AddNew();
        newDeliveryMethod.DeliveryMethod = Docflow.MailDeliveryMethods.GetAll(m => m.Sid == Docflow.Constants.MailDeliveryMethod.Exchange).FirstOrDefault();
        condition.Save();
        var printStageNumber = stages.IndexOf(StageType.Print) + 1;
        Docflow.PublicInitializationFunctions.Module.AddConditionToRule(rule, condition, printStageNumber);
        
        var rolesCompareCondition = Docflow.PublicInitializationFunctions.Module.CreateRoleCompareSignatoryAndInitManagerCondition(Contracts.ContractConditions.Create());
        var managerStageNumber = stages.IndexOf(StageType.Manager) + 1;
        Docflow.PublicInitializationFunctions.Module.AddConditionToRule(rule, rolesCompareCondition, managerStageNumber);
      }
    }

    /// <summary>
    /// Создать типы документов для финансового архива.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.WaybillDocumentTypeName, Waybill.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Contracts, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.IncomingTaxInvoiceTypeName, IncomingTaxInvoice.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Incoming, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.OutgoingTaxInvoiceTypeName, OutgoingTaxInvoice.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Outgoing, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.ContractStatementTypeName, ContractStatement.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Contracts, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.UniversalTransferDocumentTypeName, UniversalTransferDocument.ClassTypeGuid,
                                                                      Docflow.DocumentType.DocumentFlow.Contracts, true);
    }
    
    /// <summary>
    /// Создать виды документов для финансового архива.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      var notifiable = Docflow.DocumentKind.NumberingType.Registrable;
      var numerable = Docflow.DocumentKind.NumberingType.Numerable;
      var notNumerable = Docflow.DocumentKind.NumberingType.NotNumerable;

      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.WaybillDocumentKindName,
                                                                      Resources.WaybillDocumentKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Contracts,
                                                                      true, false, Waybill.ClassTypeGuid,
                                                                      new[] { Waybills.Info.Actions.SendForFreeApproval, Waybills.Info.Actions.SendForApproval },
                                                                      Init.WaybillDocumentKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.IncomingTaxInvoiceKindName,
                                                                      Resources.IncomingTaxInvoiceKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Incoming,
                                                                      true, false, IncomingTaxInvoice.ClassTypeGuid,
                                                                      new[] { IncomingTaxInvoices.Info.Actions.SendForFreeApproval },
                                                                      Init.IncomingTaxInvoiceKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.OutgoingTaxInvoiceKindName,
                                                                      Resources.OutgoingTaxInvoiceKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Outgoing,
                                                                      true, false, OutgoingTaxInvoice.ClassTypeGuid,
                                                                      new[] { OutgoingTaxInvoices.Info.Actions.SendForFreeApproval },
                                                                      Init.OutgoingTaxInvoiceKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.ContractStatementKindName,
                                                                      Resources.ContractStatementKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Contracts,
                                                                      true, false, ContractStatement.ClassTypeGuid, null, Init.ContractStatementKind, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.UniversalTaxInvoiceAndBasicKindName,
                                                                      Resources.UniversalTaxInvoiceAndBasicKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Contracts,
                                                                      true, false, UniversalTransferDocument.ClassTypeGuid, null, Init.UniversalTaxInvoiceAndBasicKind, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Resources.UniversalBasicKindName,
                                                                      Resources.UniversalBasicKindShortName,
                                                                      numerable, Docflow.DocumentKind.DocumentFlow.Contracts,
                                                                      true, false, UniversalTransferDocument.ClassTypeGuid, null, Init.UniversalBasicKind);
    }
    
    /// <summary>
    /// Создать журнал и настройки регистрации для счетов и накладных.
    /// </summary>
    public static void CreateFinancialDocumentRegistersAndSettings()
    {
      InitializationLogger.Debug("Init: Create default logs and settings for financial archive.");
      
      var taxInvoiceOutgoingDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsOutgoingTaxInvoiceName,
                                                                                  Resources.RegistersAndSettingsOutgoingTaxInvoiceIndex,
                                                                                  Docflow.RegistrationSetting.DocumentFlow.Outgoing,
                                                                                  Init.OutgoingTaxInvoiceRegister);
      var taxInvoiceIncomingDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsIncomingTaxInvoiceName,
                                                                                  Resources.RegistersAndSettingsIncomingTaxInvoiceIndex,
                                                                                  Docflow.RegistrationSetting.DocumentFlow.Incoming,
                                                                                  Init.IncomingTaxInvoiceRegister);
      var waybillDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsWaybillName,
                                                                       Resources.RegistersAndSettingsWaybillIndex,
                                                                       Docflow.RegistrationSetting.DocumentFlow.Contracts,
                                                                       Init.WaybillRegister);
      var actDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsActName,
                                                                   Resources.RegistersAndSettingsActIndex,
                                                                   Docflow.DocumentRegister.DocumentFlow.Contracts,
                                                                   Init.ContractStatementRegister);
      var universalDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsUniversalName,
                                                                         Resources.RegistersAndSettingsUniversalIndex,
                                                                         Docflow.DocumentRegister.DocumentFlow.Contracts,
                                                                         Init.UniversalRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(OutgoingTaxInvoice.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Outgoing,
                                                                           taxInvoiceOutgoingDocumentRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(IncomingTaxInvoice.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Incoming,
                                                                           taxInvoiceIncomingDocumentRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(Waybill.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Contracts,
                                                                           waybillDocumentRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(ContractStatement.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Contracts,
                                                                           actDocumentRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(UniversalTransferDocument.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Contracts,
                                                                           universalDocumentRegister);
    }
    
    /// <summary>
    /// Создать журнал.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="index">Индекс.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <returns>Журнал.</returns>
    public static Docflow.IDocumentRegister CreateLeadNumberedDocumentRegister(string name, string index, Enumeration documentFlow, Guid entityId)
    {
      var documentRegister = Docflow.PublicInitializationFunctions.Module.CreateNumerationDocumentRegister(name,
                                                                                                           index,
                                                                                                           documentFlow,
                                                                                                           entityId);
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod != Docflow.DocumentRegister.NumberingPeriod.Year)
        documentRegister.NumberingPeriod = Docflow.DocumentRegister.NumberingPeriod.Year;

      return documentRegister;
    }
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var finArchiveExportReportTableName = Constants.FinArchiveExportReport.SourceTableName;
      Docflow.PublicFunctions.Module.DropReportTempTables(new[] { finArchiveExportReportTableName });
      
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.FinArchiveExportReport.CreateFinArchiveExportReportTable, new[] { finArchiveExportReportTableName });
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Domain.Initialization;
using Sungero.Domain.Shared;
using Init = Sungero.Contracts.Constants.Module.Initialize;

namespace Sungero.Contracts.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Справочники.
        InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
        GrantRightsOnDatabooks(allUsers);
        
        // Документы.
        InitializationLogger.Debug("Init: Grant rights on documents to all users.");
        GrantRightsOnDocuments(allUsers);

        GrantRightOnFolders(allUsers);
      }
      
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Довыдача прав роли "Ответственные за настройку регистрации".
      InitializationLogger.Debug("Init: Grant right on registration for registration managers.");
      GrantRightToRegistrationManagersRole();
      
      // Выдача прав роли "Ответственные за договоры".
      InitializationLogger.Debug("Init: Grant right on contract documents for contracts responsible.");
      GrantRightToContractsResponsible();
      
      // Выдача прав роли "Регистраторы договоров".
      InitializationLogger.Debug("Init: Grant right on contract documents for registration contractual documents.");
      GrantRightToRegistrationContractsRole();
      
      // Однократная выдача прав на регистрацию исходящих счетов группам, отвечающим за регистрацию исходящих документов.
      this.GrantOutgoingInvoiceRegistrationRights();
      
      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateDefaultApprovalRoles();
      CreateDefaultRelationTypes();
      CreateApprovalIncInvoicePaidStage();
      CreateDefaultContractualRules();
      CreateDocumentRegisterAndSettingsForContracts();
      CreateDocumentRegisterAndSettingsForOutgoingInvoice();
      CreateEDocIndex();
    }

    #region Выдача прав Всем пользователям
    
    /// <summary>
    /// Выдать права всем пользователям на справочники.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDatabooks(IRole allUsers)
    {
      // Сейчас все справочники получают права по иерархии наследования.
      // InitializationLogger.Debug("Init: Grant rights on databooks to all users.");
    }
    
    /// <summary>
    /// Выдать права всем пользователям на документы.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents to all users.");
      
      IncomingInvoices.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      IncomingInvoices.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права на спец.папки модуля.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightOnFolders(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant right on contracts special folders to all users.");
      var hasLicense = Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Sungero.Contracts.Constants.Module.ContractsUIGuid);
      Dictionary<int, byte[]> licenses = null;
      
      try
      {
        if (!hasLicense)
        {
          licenses = Docflow.PublicFunctions.Module.ReadLicense();
          Docflow.PublicFunctions.Module.DeleteLicense();
        }
        
        Sungero.ContractsUI.SpecialFolders.ContractsList.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        Sungero.ContractsUI.SpecialFolders.ExpiringSoonContracts.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        Sungero.ContractsUI.SpecialFolders.ContractsAtContractors.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        Sungero.ContractsUI.SpecialFolders.IssuanceJournal.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        Sungero.ContractsUI.SpecialFolders.ContractsHistory.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        Sungero.ContractsUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        
        Sungero.ContractsUI.SpecialFolders.ContractsList.AccessRights.Save();
        Sungero.ContractsUI.SpecialFolders.ExpiringSoonContracts.AccessRights.Save();
        Sungero.ContractsUI.SpecialFolders.ContractsAtContractors.AccessRights.Save();
        Sungero.ContractsUI.SpecialFolders.IssuanceJournal.AccessRights.Save();
        Sungero.ContractsUI.SpecialFolders.ContractsHistory.AccessRights.Save();
        Sungero.ContractsUI.SpecialFolders.PowerOfAttorneyList.AccessRights.Save();
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
      
    }
    
    #endregion
    
    #region Создание ролей и выдачи им прав
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      
      Docflow.PublicInitializationFunctions.Module.CreateRole(Docflow.Resources.RoleNameContractsResponsible, Sungero.Contracts.Resources.DescriptionResponsibleForContractsRole, Docflow.Constants.Module.RoleGuid.ContractsResponsible);
      Docflow.PublicInitializationFunctions.Module.CreateRole(Docflow.Resources.RoleNameRegistrationContracts, Sungero.Contracts.Resources.DescriptionContractsRegistrarRole, Docflow.Constants.Module.RoleGuid.RegistrationContractualDocument);
    }
    
    /// <summary>
    /// Выдать права роли "Ответственные за настройку регистрации".
    /// </summary>
    public static void GrantRightToRegistrationManagersRole()
    {
      InitializationLogger.Debug("Init: Grant rights on logs and registration settings to registration managers.");
      
      var registrationManagers = Roles.GetAll().SingleOrDefault(n => n.Sid == Docflow.Constants.Module.RoleGuid.RegistrationManagersRole);
      var registrationContracts = Roles.GetAll().SingleOrDefault(n => n.Sid == Docflow.Constants.Module.RoleGuid.RegistrationContractualDocument);
      
      if (registrationManagers == null || registrationContracts == null)
        return;
      
      registrationContracts.AccessRights.Grant(registrationManagers, DefaultAccessRightsTypes.Change);
      registrationContracts.Save();
    }
    
    /// <summary>
    /// Выдать права роли "Ответственные за договоры".
    /// </summary>
    public static void GrantRightToContractsResponsible()
    {
      InitializationLogger.Debug("Init: Grant rights on contractual document to responsible managers.");
      
      var contractsResponsible = Roles.GetAll().Where(n => n.Sid == Docflow.Constants.Module.RoleGuid.ContractsResponsible).FirstOrDefault();
      if (contractsResponsible == null)
        return;
      
      // Права на документы.
      ContractBases.AccessRights.Grant(contractsResponsible, DefaultAccessRightsTypes.Create);
      SupAgreements.AccessRights.Grant(contractsResponsible, DefaultAccessRightsTypes.Create);
      OutgoingInvoices.AccessRights.Grant(contractsResponsible, DefaultAccessRightsTypes.Create);
      ContractBases.AccessRights.Save();
      SupAgreements.AccessRights.Save();
      OutgoingInvoices.AccessRights.Save();
      
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
        
        FinancialArchive.ContractStatements.AccessRights.Grant(contractsResponsible, DefaultAccessRightsTypes.Create);
        FinancialArchive.ContractStatements.AccessRights.Save();
      }
      finally
      {
        Docflow.PublicFunctions.Module.RestoreLicense(licenses);
      }
    }
    
    /// <summary>
    /// Выдать права роли "Регистраторы договоров".
    /// </summary>
    public static void GrantRightToRegistrationContractsRole()
    {
      InitializationLogger.Debug("Init: Grant rights on documents to registration contractual documents role.");
      
      var registrationContracts = Roles.GetAll().FirstOrDefault(n => n.Sid == Docflow.Constants.Module.RoleGuid.RegistrationContractualDocument);
      if (registrationContracts == null)
        return;
      
      // Модуль "Документооборот".
      Docflow.CaseFiles.AccessRights.Grant(registrationContracts, DefaultAccessRightsTypes.Read);
      Docflow.MailDeliveryMethods.AccessRights.Grant(registrationContracts, DefaultAccessRightsTypes.Read);
      Docflow.FileRetentionPeriods.AccessRights.Grant(registrationContracts, DefaultAccessRightsTypes.Read);
      Docflow.CaseFiles.AccessRights.Save();
      Docflow.MailDeliveryMethods.AccessRights.Save();
      Docflow.FileRetentionPeriods.AccessRights.Save();
    }
    
    #endregion
    
    #region Создание видов и типов документов
    
    /// <summary>
    /// Создать типы документов для договоров.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Contracts.Resources.ContractTypeName, Contract.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Contracts, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Contracts.Resources.SupAgreementTypeName, SupAgreement.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Contracts, true);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Contracts.Resources.IncomingInvoiceTypeName, IncomingInvoice.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Incoming, false);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Sungero.Contracts.Resources.OutgoingInvoiceTypeName, OutgoingInvoice.ClassTypeGuid, Docflow.DocumentType.DocumentFlow.Outgoing, true);
    }
    
    /// <summary>
    /// Создать виды документов для договоров.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var notifiable = Docflow.DocumentKind.NumberingType.Registrable;
      var numerable = Docflow.DocumentKind.NumberingType.Numerable;
      var notNumerable = Docflow.DocumentKind.NumberingType.NotNumerable;
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Contracts.Resources.ContractKindName,
                                                                      Sungero.Contracts.Resources.ContractKindShortName,
                                                                      notifiable, DocumentFlow.Contracts, true, false,
                                                                      Contract.ClassTypeGuid, null, Init.ContractKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Contracts.Resources.SupAgreementKindName,
                                                                      Sungero.Contracts.Resources.SupAgreementKindShortName,
                                                                      numerable, DocumentFlow.Contracts, true, false,
                                                                      SupAgreement.ClassTypeGuid, null, Init.SupAgreementKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Contracts.Resources.IncomingInvoiceKindName,
                                                                      Sungero.Contracts.Resources.IncomingInvoiceKindShortName,
                                                                      notNumerable, DocumentFlow.Incoming, true, false,
                                                                      IncomingInvoice.ClassTypeGuid, new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval, OfficialDocuments.Info.Actions.SendForApproval },
                                                                      Init.IncomingInvoiceKind);
      Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(Sungero.Contracts.Resources.OutgoingInvoiceKindName,
                                                                      Sungero.Contracts.Resources.OutgoingInvoiceKindShortName,
                                                                      numerable, DocumentFlow.Outgoing, true, false,
                                                                      OutgoingInvoice.ClassTypeGuid, new Domain.Shared.IActionInfo[] { OfficialDocuments.Info.Actions.SendForFreeApproval },
                                                                      Init.OutgoingInvoiceKind);
    }
    
    #endregion
    
    #region Создание типов связей
    
    /// <summary>
    /// Создать базовые типы связей.
    /// </summary>
    public static void CreateDefaultRelationTypes()
    {
      InitializationLogger.Debug("Init: Create default relation types.");
      
      // Дополнительное соглашение.
      var supAgreement = Docflow.PublicInitializationFunctions.Module.CreateRelationType(Constants.Module.SupAgreementRelationName, Resources.RelationSupAgreementSourceTitle,
                                                                                         Resources.RelationSupAgreementTargetTitle, Resources.RelationSupAgreementSourceTitle,
                                                                                         Resources.RelationSupAgreementDescription, true, false, false, true);
      supAgreement.Mapping.Clear();
      var supAgreementRow = supAgreement.Mapping.AddNew();
      supAgreementRow.Source = Sungero.Contracts.ContractBases.Info;
      supAgreementRow.Target = Sungero.Contracts.SupAgreements.Info;
      supAgreementRow.RelatedProperty = Sungero.Contracts.SupAgreements.Info.Properties.LeadingDocument;
      supAgreement.Save();
      
      // Бухгалтерский документ.
      var accounting = Docflow.PublicInitializationFunctions.Module.CreateRelationType(Constants.Module.AccountingDocumentsRelationName, Resources.RelationAccountingSourceTitle,
                                                                                       Resources.RelationAccountingTargetTitle, Resources.RelationAccountingSourceTitle,
                                                                                       Resources.RelationAccountingDescription, true, false, false, true);
      accounting.UseTarget = true;
      accounting.HasDirection = true;
      accounting.Mapping.Clear();
      var accountingRow = accounting.Mapping.AddNew();
      accountingRow.Source = Sungero.Contracts.ContractualDocuments.Info;
      accountingRow.Target = Sungero.Docflow.AccountingDocumentBases.Info;
      accountingRow.RelatedProperty = Sungero.Docflow.AccountingDocumentBases.Info.Properties.LeadingDocument;
      accountingRow = accounting.Mapping.AddNew();
      accountingRow.Source = Sungero.Contracts.ContractualDocuments.Info;
      accountingRow.Target = Sungero.Contracts.IncomingInvoices.Info;
      accountingRow.RelatedProperty = Sungero.Contracts.IncomingInvoices.Info.Properties.Contract;
      accounting.Save();
    }
    
    #endregion
    
    #region Создание настроек регистрации и согласования по умолчанию
    
    /// <summary>
    /// Создать правила по умолчанию.
    /// </summary>
    public static void CreateDefaultContractualRules()
    {
      InitializationLogger.Debug("Init: Create default contractual approval rules.");
      
      var stages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Print, StageType.Sign, StageType.Register, StageType.Sending, StageType.CheckReturn, StageType.Notice };
      
      var rule = CreateDefaultRule(Resources.DefaultApprovalRuleNameContracts,
                                   Docflow.ApprovalRuleBase.DocumentFlow.Contracts,
                                   stages);
      
      // Добавить условие по способу отправки и непосредственный руководитель - подписывающий, для созданного правила.
      if (rule != null)
      {
        var deliveryCondition = ContractConditions.Create();
        deliveryCondition.ConditionType = Docflow.ConditionBase.ConditionType.DeliveryMethod;
        var newDeliveryMethod = deliveryCondition.DeliveryMethods.AddNew();
        newDeliveryMethod.DeliveryMethod = Docflow.MailDeliveryMethods.GetAll(m => m.Sid == Docflow.Constants.MailDeliveryMethod.Exchange).FirstOrDefault();
        deliveryCondition.Save();
        var printStageNumber = stages.IndexOf(StageType.Print) + 1;
        Docflow.PublicInitializationFunctions.Module.AddConditionToRule(rule, deliveryCondition, printStageNumber);
        
        var rolesCompareCondition = Docflow.PublicInitializationFunctions.Module.CreateRoleCompareSignatoryAndInitManagerCondition(ContractConditions.Create());
        var managerStageNumber = stages.IndexOf(StageType.Manager) + 1;
        Docflow.PublicInitializationFunctions.Module.AddConditionToRule(rule, rolesCompareCondition, managerStageNumber);
      }
      
      InitializationLogger.Debug("Init: Create default invoice approval rules.");
      var invoiceStages = new List<Enumeration>
      { StageType.Manager, StageType.Approvers, StageType.Sign };
      var invoiceRule = CreateDefaultInvoiceRule(Resources.DefaultApprovalRuleNameInvoice, invoiceStages);
      if (invoiceRule != null)
      {
        var rolesCompareCondition = Docflow.PublicInitializationFunctions.Module.CreateRoleCompareSignatoryAndInitManagerCondition(Conditions.Create());
        var managerStageNumber = invoiceStages.IndexOf(StageType.Manager) + 1;
        Docflow.PublicInitializationFunctions.Module.AddConditionToRule(invoiceRule, rolesCompareCondition, managerStageNumber);
      }
    }
    
    /// <summary>
    /// Создать правило по умолчанию.
    /// </summary>
    /// <param name="ruleName">Имя правила.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Созданное правило. Если правило создано не было, то null.</returns>
    [Public]
    public static IContractsApprovalRule CreateDefaultRule(string ruleName, Enumeration documentFlow, List<Enumeration> stages)
    {
      var hasNotDefaultRule = Docflow.ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      var hasDefaultRule = Docflow.ApprovalRuleBases.GetAll().Any(r => r.DocumentFlow == documentFlow &&
                                                                  (!r.DocumentKinds.Any() || r.DocumentKinds.Any(d => d.DocumentKind.DocumentType.DocumentTypeGuid == Contract.ClassTypeGuid.ToString() ||
                                                                                                                 d.DocumentKind.DocumentType.DocumentTypeGuid == SupAgreement.ClassTypeGuid.ToString())));
      
      if (hasNotDefaultRule || hasDefaultRule)
        return null;
      
      var rule = ContractsApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = ruleName;
      rule.DocumentFlow = documentFlow;
      rule.IsDefaultRule = true;
      
      Docflow.PublicInitializationFunctions.Module.SetRuleStages(rule, stages);
      Docflow.PublicFunctions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
      return rule;
    }
    
    /// <summary>
    /// Создать правило по умолчанию для входящего счета.
    /// </summary>
    /// <param name="ruleName">Имя правила.</param>
    /// <param name="stages">Этапы.</param>
    /// <returns>Созданное правило.</returns>
    [Public]
    public static IApprovalRule CreateDefaultInvoiceRule(string ruleName, List<Enumeration> stages)
    {
      var documentFlow = Docflow.ApprovalRuleBase.DocumentFlow.Incoming;
      
      var hasNotDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.IsDefaultRule != true);
      var hasDefaultRule = ApprovalRuleBases.GetAll().Any(r => r.DocumentFlow == documentFlow);
      if (hasNotDefaultRule || hasDefaultRule)
        return null;
      
      var rule = Docflow.ApprovalRules.Create();
      rule.Status = Sungero.Docflow.ApprovalRuleBase.Status.Active;
      rule.Name = ruleName;
      rule.DocumentFlow = documentFlow;
      rule.IsDefaultRule = true;
      rule.DocumentKinds.AddNew().DocumentKind = DocumentKinds.GetAll().Where(k => k.DocumentType.DocumentTypeGuid == IncomingInvoice.ClassTypeGuid.ToString()).FirstOrDefault();
      Docflow.PublicInitializationFunctions.Module.SetRuleStages(rule, stages);
      
      // Добавить этап конвертации в Pdf.
      Docflow.PublicInitializationFunctions.Module.AddConvertPdfStage(rule);
      
      // Создать этап передачи счета в бухгалтерию.
      Docflow.PublicInitializationFunctions.Module.AddGiveInvoiceApprovalStage(rule);
      
      Docflow.PublicFunctions.ApprovalRuleBase.CreateAutoTransitions(rule);
      rule.Save();
      
      return rule;
    }
    
    /// <summary>
    /// Создать журнал и настройки регистрации для актов и доп. соглашений.
    /// </summary>
    public static void CreateDocumentRegisterAndSettingsForContracts()
    {
      InitializationLogger.Debug("Init: Create default logs and settings for contracts.");
      
      var supAgreementDocumentRegister = CreateLeadNumberedDocumentRegister(Resources.RegistersAndSettingsSupAgreementName,
                                                                            Resources.RegistersAndSettingsSupAgreementIndex,
                                                                            Init.SupAgreementRegister);
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(SupAgreement.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Contracts,
                                                                           supAgreementDocumentRegister);
    }
    
    /// <summary>
    /// Создать журнал для договорного документа с разрезом нумерации по ведущему документу.
    /// </summary>
    /// <param name="name">Название.</param>
    /// <param name="index">Индекс.</param>
    /// <param name="entityId">ИД инициализации.</param>
    /// <returns>Журнал.</returns>
    public static IDocumentRegister CreateLeadNumberedDocumentRegister(string name, string index, Guid entityId)
    {
      var documentRegister = Docflow.PublicInitializationFunctions.Module.CreateNumerationDocumentRegister(name,
                                                                                                           index,
                                                                                                           Docflow.DocumentRegister.DocumentFlow.Contracts,
                                                                                                           entityId);
      
      if (documentRegister != null &&
          documentRegister.NumberingSection != Docflow.DocumentRegister.NumberingSection.LeadingDocument)
        documentRegister.NumberingSection = Docflow.DocumentRegister.NumberingSection.LeadingDocument;
      
      return documentRegister;
    }
    
    /// <summary>
    /// Создать журнал и настройки регистрации для исх. счетов.
    /// </summary>
    public static void CreateDocumentRegisterAndSettingsForOutgoingInvoice()
    {
      InitializationLogger.Debug("Init: Create default logs and settings for outgoing invoice.");
      
      var documentRegister = Docflow.PublicInitializationFunctions.Module.CreateNumerationDocumentRegister(
        Resources.RegistersAndSettingsOutgoingInvoiceName,
        Resources.RegistersAndSettingsOutgoingInvoiceIndex,
        Docflow.DocumentRegister.DocumentFlow.Outgoing,
        Init.OutgoingInvoiceRegister);
      
      if (documentRegister != null &&
          documentRegister.NumberingPeriod != Docflow.DocumentRegister.NumberingPeriod.Year)
        documentRegister.NumberingPeriod = Docflow.DocumentRegister.NumberingPeriod.Year;
      
      if (documentRegister != null &&
          documentRegister.NumberingSection != Docflow.DocumentRegister.NumberingSection.NoSection)
        documentRegister.NumberingSection = Docflow.DocumentRegister.NumberingSection.NoSection;
      
      Docflow.PublicInitializationFunctions.Module.CreateNumerationSetting(OutgoingInvoice.ClassTypeGuid,
                                                                           Docflow.RegistrationSetting.DocumentFlow.Outgoing,
                                                                           documentRegister);
    }
    
    /// <summary>
    /// Однократно выдать права на регистрацию исходящих счетов группам, отвечающим за регистрацию исходящих документов.
    /// </summary>
    /// <remarks>Сделано в инициализации. Конвертер писать сложнее.</remarks>
    public virtual void GrantOutgoingInvoiceRegistrationRights()
    {
      var initializationEventName = Constants.Module.Initialize.Events.GrantOutgoingInvoiceRegistrationRights.Name;
      var initializationEventId = Constants.Module.Initialize.Events.GrantOutgoingInvoiceRegistrationRights.Id;
      
      InitializationLogger.DebugFormat("Init: {0}", initializationEventName);
      try
      {
        if (HasInitializationEventMark(initializationEventId, initializationEventName))
          return;
        
        // Если уже были выданы какие-то права, то дополнительно прав не выдавать.
        var registrationGroups = Docflow.RegistrationGroups
          .GetAll(x => x.CanRegisterOutgoing == true)
          .ToList();
        var outgoingInvoicesCurrentAccessRights = OutgoingInvoices.AccessRights.Current.ToList();
        registrationGroups = registrationGroups
          .Where(x => !outgoingInvoicesCurrentAccessRights.Any(y => Equals(y.Recipient, x)))
          .ToList();
        foreach (var registrationGroup in registrationGroups)
        {
          InitializationLogger.DebugFormat("Init: Grant registration group {0}(ID={1}) rights on outgoing invoice registration.",
                                           registrationGroup.Name, registrationGroup.Id);
          OutgoingInvoices.AccessRights.Grant(registrationGroup, Docflow.Constants.Module.DefaultAccessRightsTypeSid.Register);
        }
        
        OutgoingInvoices.AccessRights.Save();
        
        SetInitializationEventMark(initializationEventId, initializationEventName);
      }
      catch (Exception ex)
      {
        Sungero.Core.Logger.ErrorFormat("Init Error: {0}", ex, initializationEventName);
      }
    }
    
    #endregion
    
    #region Создание ролей согласования
    
    /// <summary>
    /// Создать базовые роли согласования.
    /// </summary>
    public static void CreateDefaultApprovalRoles()
    {
      InitializationLogger.Debug("Init: Create default approval roles.");
      
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.ContractResp, Sungero.Contracts.Resources.RoleDescriptionContractResponsible);
      CreateApprovalRole(Docflow.ApprovalRoleBase.Type.ContRespManager, Sungero.Contracts.Resources.RoleDescriptionContractResponsibleManager);
    }
    
    /// <summary>
    /// Создать роль согласования.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание роли.</param>
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      InitializationLogger.DebugFormat("Init: Create contract approval rule {0}", ApprovalRoleBases.Info.Properties.Type.GetLocalizedValue(roleType));
      
      var role = ContractApprovalRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
        role = ContractApprovalRoles.Create();
      
      role.Type = roleType;
      role.Description = description;
      role.Save();
    }
    
    #endregion
    
    #region Создание этапов согласования
    
    /// <summary>
    /// Создание этапа установки состояния "Оплачен" для счета.
    /// </summary>
    public static void CreateApprovalIncInvoicePaidStage()
    {
      InitializationLogger.DebugFormat("Init: Create approval stage for incoming invoice set paid state.");
      if (ApprovalIncInvoicePaidStages.GetAll().Any())
        return;
      
      var stage = ApprovalIncInvoicePaidStages.Create();
      stage.Name = Sungero.Contracts.ApprovalIncInvoicePaidStages.Resources.SetPaidStateStageDefaultName;
      stage.TimeoutInHours = Sungero.Contracts.Constants.ApprovalIncInvoicePaidStage.DefaultTimeoutInHours;
      stage.Save();
    }
    
    #endregion
    
    public static void CreateEDocIndex()
    {
      // Проверить наличие индекса.
      var tableName = "Sungero_Content_EDoc";
      var indexName = "idx_EDoc_LifeCycleState_Discriminator_RespEmpl";
      var indexQuery = string.Format(Queries.Module.CreateEDocIndex0Query, tableName, indexName);
      
      Docflow.PublicFunctions.Module.CreateIndexOnTable(tableName, indexName, indexQuery);
    }
    
    /// <summary>
    /// Создать отметку о событии инициализации.
    /// </summary>
    /// <param name="eventId">Id события инициализации.</param>
    /// <param name="name">Имя события.</param>
    private static void SetInitializationEventMark(Guid eventId, string name)
    {
      var link = Sungero.Commons.ExternalEntityLinks.Create();
      link.ExtEntityId = eventId.ToString();
      link.ExtSystemId = Sungero.Docflow.Constants.Module.InitializeExternalLinkSystem;
      link.Name = name;
      link.IsDeleted = false;
      link.Save();
    }
    
    /// <summary>
    /// Проверить наличие отметки о событии инициализации.
    /// </summary>
    /// <param name="eventId">Id события инициализации.</param>
    /// <param name="name">Имя события.</param>
    /// <returns>True, если отметка есть, иначе False.</returns>
    private static bool HasInitializationEventMark(Guid eventId, string name)
    {
      return Sungero.Commons.ExternalEntityLinks.GetAll()
        .Where(x => string.Equals(x.ExtEntityId, eventId.ToString(), StringComparison.InvariantCultureIgnoreCase) &&
                    x.ExtSystemId == Sungero.Docflow.Constants.Module.InitializeExternalLinkSystem &&
                    x.Name == name)
        .Any();
    }
  }
}

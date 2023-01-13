using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SignatureSetting;

namespace Sungero.Docflow.Shared
{
  partial class SignatureSettingFunctions
  {
    /// <summary>
    /// Сменить доступность реквизитов.
    /// </summary>
    public virtual void ChangePropertiesAccess()
    {
      var amount = _obj.Limit == Limit.Amount;
      var isSystem = _obj.IsSystem == true;
      
      _obj.State.Properties.Amount.IsRequired = _obj.Info.Properties.Amount.IsRequired || amount;
      _obj.State.Properties.Amount.IsEnabled = amount;
      _obj.State.Properties.Currency.IsRequired = _obj.Info.Properties.Currency.IsRequired || amount;
      _obj.State.Properties.Currency.IsEnabled = amount;
      
      _obj.State.Properties.Recipient.IsEnabled = !isSystem;      
      _obj.State.Properties.BusinessUnits.IsEnabled = !isSystem;
      _obj.State.Properties.BusinessUnits.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.JobTitle.IsEnabled = _obj.Recipient != null && Company.Employees.Is(_obj.Recipient);
      
      _obj.State.Properties.Document.IsEnabled = _obj.Reason != Docflow.SignatureSetting.Reason.Duties;
      _obj.State.Properties.Document.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney ||
        _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.ValidTill.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney ||
        _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.Certificate.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA;
      _obj.State.Properties.Certificate.IsEnabled = _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA ||
        _obj.BusinessUnits.Count() == 1 && Company.Employees.Is(_obj.Recipient);
    }
    
    /// <summary>
    /// Получить роли, которым могут быть назначены права подписи.
    /// </summary>
    /// <returns>Список Sid ролей.</returns>
    public virtual List<Guid> GetPossibleSignatureRoles()
    {
      return new List<Guid>()
      {
        Domain.Shared.SystemRoleSid.AllUsers,
        Constants.Module.RoleGuid.ContractsResponsible,
        Constants.Module.RoleGuid.BusinessUnitHeadsRole,
        Constants.Module.RoleGuid.DepartmentManagersRole
      };
    }
    
    /// <summary>
    /// Отфильтровать категории договоров, с учетом выбранного документопотока и видов документов.
    /// </summary>
    /// <param name="query">Фильтруемые категории.</param>
    /// <returns>Доступные для выбора категории.</returns>
    [Public]
    public IQueryable<IDocumentGroupBase> FilterCategories(IQueryable<IDocumentGroupBase> query)
    {
      var filtrableDocumentKinds = Contracts.PublicFunctions.ContractCategory.GetAllowedDocumentKinds();
      var ruleDocumentKinds = _obj.DocumentKinds.Select(dk => dk.DocumentKind).ToList();
      var filtrableDocumentKindsInRule = ruleDocumentKinds.Where(dk => filtrableDocumentKinds.Contains(dk)).ToList();
      
      // Нельзя выбирать категории, если:
      // - Документопоток не любой \ договорной;
      // - При "любом" документопотоке выбраны какие-то виды документов, но не выбран хотя бы один договорной вид.
      var notContractKindsOrEmpty = ruleDocumentKinds.All(r => r != null && r.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Contracts);
      if (notContractKindsOrEmpty && (_obj.DocumentFlow == DocumentFlow.All ? ruleDocumentKinds.Any() : _obj.DocumentFlow != DocumentFlow.Contracts))
        return Enumerable.Empty<IDocumentGroupBase>().AsQueryable();
      
      var documentGroups = query.ToList();
      if (filtrableDocumentKindsInRule.Any())
        for (int i = 0; i < documentGroups.Count; i++)
      {
        var groupDocumentKinds = documentGroups[i].DocumentKinds.Select(d => d.DocumentKind).ToList();
        
        if (groupDocumentKinds.Any() && groupDocumentKinds.Where(dk => filtrableDocumentKindsInRule.Contains(dk)).Count() != filtrableDocumentKindsInRule.Count())
        {
          documentGroups.RemoveAt(i);
          i--;
        }
      }
      
      return documentGroups.AsQueryable<IDocumentGroupBase>();
    }
    
    /// <summary>
    /// Получить возможные категории из кэшированных для права подписи.
    /// </summary>
    /// <returns>Возможные категории.</returns>
    [Public]
    public IQueryable<IDocumentGroupBase> GetPossibleCashedCategories()
    {
      return this.FilterCategories(DocumentGroupBases.GetAllCached()
                                   .Where(c => c.Status == CoreEntities.DatabookEntry.Status.Active));
    }
    
    /// <summary>
    /// Получить основание подписания для документа-основания доверенность.
    /// </summary>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <returns>Основание подписания.</returns>
    public virtual string GetPowerOfAttorneySigningReason(IPowerOfAttorney powerOfAttorney)
    {
      // Основание подписания в формате: <Сокращенное имя вида> №<номер> от <дата>.
      var signingReason = powerOfAttorney.DocumentKind.ShortName;
      using (TenantInfo.Culture.SwitchTo())
      { 
        if (!string.IsNullOrWhiteSpace(powerOfAttorney.RegistrationNumber))
          signingReason += OfficialDocuments.Resources.Number + powerOfAttorney.RegistrationNumber;
        
        if (powerOfAttorney.RegistrationDate != null)
          signingReason += OfficialDocuments.Resources.DateFrom + powerOfAttorney.RegistrationDate.Value.ToString("d");
      }
      
      signingReason = Functions.Module.TrimSpecialSymbols(signingReason);
      return signingReason;
    }
    
    /// <summary>
    /// Получить основание подписания для документа-основания электронная доверенность.
    /// </summary>
    /// <param name="formalizedPowerOfAttorney">Электронная доверенность.</param>
    /// <returns>Основание подписания.</returns>
    public virtual string GetFormalizedPowerOfAttorneySigningReason(IFormalizedPowerOfAttorney formalizedPowerOfAttorney)
    {
      // Основание подписания в формате: Доверенность № <гуид> от <дата>.
      var signingReason = string.Empty;
      using (TenantInfo.Culture.SwitchTo())
      {
        signingReason = SignatureSettings.Resources.FormalizedPoASigningReasonDocumentName;
        
        if (!string.IsNullOrWhiteSpace(formalizedPowerOfAttorney.UnifiedRegistrationNumber))
          signingReason += OfficialDocuments.Resources.Number + " " + formalizedPowerOfAttorney.UnifiedRegistrationNumber;
        
        if (formalizedPowerOfAttorney.RegistrationDate != null)
          signingReason += OfficialDocuments.Resources.DateFrom + formalizedPowerOfAttorney.RegistrationDate.Value.ToString("d");
      }
      
      signingReason = Functions.Module.TrimSpecialSymbols(signingReason);
      return signingReason;
    }
    
    /// <summary>
    /// Заполнить имя Права подписи.
    /// </summary>
    [Public]
    public virtual void FillName()
    {
      if (_obj.Reason == Docflow.SignatureSetting.Reason.Other ||
          _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney ||
          _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA)
      {
        _obj.Name = _obj.SigningReason;
      }
      else if (_obj.Reason == Docflow.SignatureSetting.Reason.Duties)
        _obj.Name = SignatureSettings.Resources.DutiesDisplayNameFormat(_obj.SigningReason);
    }
  }
}
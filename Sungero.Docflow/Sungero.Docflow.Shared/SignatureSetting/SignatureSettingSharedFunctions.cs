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
      
      _obj.State.Properties.Document.IsEnabled = _obj.Reason != Docflow.SignatureSetting.Reason.Duties;
      _obj.State.Properties.Document.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney;
      _obj.State.Properties.DocumentInfo.IsEnabled = _obj.Reason == Docflow.SignatureSetting.Reason.Other;
      _obj.State.Properties.DocumentInfo.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.Other;
      _obj.State.Properties.ValidTill.IsRequired = _obj.Reason == Docflow.SignatureSetting.Reason.PowerOfAttorney;
      _obj.State.Properties.Certificate.IsEnabled = _obj.BusinessUnits.Count() == 1 && Company.Employees.Is(_obj.Recipient);
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
  }
}
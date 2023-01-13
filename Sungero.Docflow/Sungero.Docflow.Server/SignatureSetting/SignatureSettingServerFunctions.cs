using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SignatureSetting;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.Docflow.Server
{
  partial class SignatureSettingFunctions
  {
    /// <summary>
    /// Обновление списка подписывающих согласно правам подписи.
    /// </summary>
    /// <param name="beforeDelete">True, если это процесс удаления.</param>
    [Public]
    public virtual void UpdateSigningRole(bool beforeDelete)
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.SigningRole).Single();
      var closed = beforeDelete || _obj.Status == CoreEntities.DatabookEntry.Status.Closed;

      IRecipient oldMember = null;
      if (closed)
        oldMember = _obj.Recipient;
      if (!closed && !Equals(_obj.Recipient, _obj.State.Properties.Recipient.OriginalValue))
        oldMember = _obj.State.Properties.Recipient.OriginalValue;
      if (!closed && _obj.ValidTill != null && _obj.ValidTill < Calendar.GetUserToday(_obj.Recipient))
      {
        oldMember = _obj.Recipient;
        closed = true;
      }
      
      // Удалить подписывающего из роли, если у него больше нет активного права подписи.
      if (oldMember != null)
      {
        var recipientToday = Calendar.GetUserToday(oldMember);
        var activeSetting = SignatureSettings
          .GetAll(ss => !Equals(ss, _obj) && ss.Status == CoreEntities.DatabookEntry.Status.Active)
          .Where(ss => Equals(ss.Recipient, oldMember) && !Equals(ss, _obj))
          .Where(ss => ss.ValidTill == null || ss.ValidTill >= recipientToday);
        if (!activeSetting.Any())
          foreach (var link in role.RecipientLinks.Where(l => Equals(l.Member, oldMember)).ToList())
            role.RecipientLinks.Remove(link);
      }
      
      // Добавить подписывающего в роль.
      if (!closed)
      {
        if (!role.RecipientLinks.Any(r => Equals(r.Member, _obj.Recipient)))
        {
          role.RecipientLinks.AddNew().Member = _obj.Recipient;
        }
      }
    }
    
    /// <summary>
    /// Получить условия по праву подписи.
    /// </summary>
    /// <returns>Строка с условиями.</returns>
    public virtual string GetSignSettingCondition()
    {
      var result = string.Empty;
      if (_obj.Limit != Limit.NoLimit)
      {
        var currencyName = StringUtils.NumberDeclension((long)Math.Truncate(_obj.Amount.Value),
                                                        _obj.Currency.ShortName,
                                                        Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(_obj.Currency.ShortName, DeclensionCase.Genitive),
                                                        Sungero.Core.CaseConverter.ConvertCurrencyNameToTargetDeclension(_obj.Currency.ShortName.Pluralize(), DeclensionCase.Genitive));
        
        result = string.Format(SignatureSettings.Resources.DocumentAmountLessThen, _obj.Amount, currencyName.ToLower());
      }
      
      if (_obj.Categories.Any())
      {
        var separator = ", ";
        result += string.IsNullOrEmpty(result) ? SignatureSettings.Resources.DocumentCategories : SignatureSettings.Resources.DocumentAdditionalCategories;
        result += string.Join(separator, _obj.Categories.Select(c => string.Format("\"{0}\"", c.Category.Name)));
      }
      return result;
    }
    
    /// <summary>
    /// Создать права подписи для НОР.
    /// </summary>
    /// <param name="unit">НОР.</param>
    [Remote, Public]
    public static void UpdateBusinessUnitSetting(Company.IBusinessUnit unit)
    {
      var settings = SignatureSettings.GetAll()
        .Where(s => s.IsSystem == true && s.Status == CoreEntities.DatabookEntry.Status.Active &&
               s.BusinessUnits.Any(u => Equals(u.BusinessUnit, unit)))
        .ToList();
      
      if (settings.Count > 2)
        Logger.DebugFormat("UpdateBusinessUnitSetting: Has {0} SignatureSettings for BusinessUnit {1}. Must have max 2.", settings.Count, unit.Id);
      
      var isCeoChanged = !Equals(unit.State.Properties.CEO.OriginalValue, unit.CEO);
      
      if (unit.CEO != null && unit.Status == CoreEntities.DatabookEntry.Status.Active &&
          isCeoChanged && (!settings.Any() || !settings.Any(s => Equals(s.Recipient, unit.CEO))))
      {
        var setting = SignatureSettings.Create();
        setting.BusinessUnits.AddNew().BusinessUnit = unit;
        setting.Recipient = unit.CEO;
        setting.Reason = Docflow.SignatureSetting.Reason.Duties;
        setting.IsSystem = true;

        setting.Note = SignatureSettings.Resources.DefaultSignatureSettingNote;
      }
      
      if (settings.Any(s => Equals(s.Recipient, unit.CEO)) && unit.Status == CoreEntities.DatabookEntry.Status.Active)
      {
        foreach (var setting in settings.Where(s => Equals(s.Recipient, unit.CEO)))
        {
          Sungero.Docflow.PublicFunctions.SignatureSetting.UpdateSigningRole(setting, false);
        }
      }
      
      var oldSettings = settings;
      if (unit.Status == CoreEntities.DatabookEntry.Status.Active)
        oldSettings = oldSettings.Where(s => !Equals(s.Recipient, unit.CEO)).ToList();
      foreach (var setting in oldSettings)
      {
        setting.Status = CoreEntities.DatabookEntry.Status.Closed;
      }
    }
    
    /// <summary>
    /// Получить действующие права подписи по виду документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Права подписи.</returns>
    [Public]
    public static IQueryable<ISignatureSetting> GetSignatureSettingsByDocumentKind(IDocumentKind documentKind)
    {
      return SignatureSettings.GetAll(s => s.Status == Docflow.SignatureSetting.Status.Active)
        .Where(s => s.DocumentKinds.Any(k => k.DocumentKind.Id == documentKind.Id));
    }
    
    /// <summary>
    /// Получить действующие права подписи по виду документа и нашим организациям.
    /// </summary>
    /// <param name="businessUnits">Наши организации.</param>
    /// <param name="kinds">Виды документа.</param>
    /// <returns>Действующие права подписи.</returns>
    [Public]
    public static IQueryable<ISignatureSetting> GetSignatureSettings(List<IBusinessUnit> businessUnits, List<IDocumentKind> kinds)
    {
      var today = Calendar.UserToday;
      return SignatureSettings.GetAll()
        .Where(s => s.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(s => !s.BusinessUnits.Any() || !businessUnits.Any() || s.BusinessUnits.Any(k => businessUnits.Contains(k.BusinessUnit)))
        .Where(s => !s.DocumentKinds.Any() || !kinds.Any() || s.DocumentKinds.Any(k => kinds.Contains(k.DocumentKind)))
        .Where(s => (!s.ValidFrom.HasValue || s.ValidFrom.Value <= today) &&
               (!s.ValidTill.HasValue || s.ValidTill.Value >= today));
    }
    
    /// <summary>
    /// Проверить, является ли документ-основание в праве подписи эл. доверенностью.
    /// </summary>
    /// <returns>True - если документ-основание является эл. доверенностью.</returns>
    [Public]
    public virtual bool ReasonIsFormalizedPoA()
    {
      return _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA &&
        _obj.Document != null && Docflow.FormalizedPowerOfAttorneys.Is(_obj.Document);
    }
    
    /// <summary>
    /// Проверить срок действия электронной доверенности в праве подписи.
    /// </summary>
    /// <returns>True - если срок действия доверенности истёк, иначе - false.</returns>
    [Public, Remote]
    public virtual bool FormalizedPowerOfAttorneyIsExpired()
    {
      return _obj.Reason == Docflow.SignatureSetting.Reason.FormalizedPoA &&
        _obj.Document != null && FormalizedPowerOfAttorneys.As(_obj.Document).ValidTill < Calendar.Today;
    }
  }
}
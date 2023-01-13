using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Commons;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class AccountingDocumentBaseFunctions
  {
    /// <summary>
    /// Получение категории договора/доп. соглашения, указанного в акте.
    /// </summary>
    /// <returns>Категория договора.</returns>
    public override IDocumentGroupBase GetDocumentGroup()
    {
      if (_obj.LeadingDocument != null)
        return Functions.OfficialDocument.GetDocumentGroup(_obj.LeadingDocument);
      return null;
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Counterparty == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>() { _obj.Counterparty };
    }
    
    /// <summary>
    /// Получить основание подписания со стороны контрагента.
    /// </summary>
    /// <returns>Основание подписания со стороны контрагента.</returns>
    [Public]
    public override string GetCounterpartySigningReason()
    {
      return _obj.CounterpartySigningReason;
    }
    
    /// <summary>
    /// Поведение панели по умолчанию.
    /// </summary>
    /// <returns>True, если панель должна быть отображена при создании документа.</returns>
    public override bool DefaultRegistrationPaneVisibility()
    {
      return true;
    }
    
    /// <summary>
    /// Показ панели регистрации.
    /// </summary>
    /// <param name="conditions">Условие.</param>
    /// <returns>Результат проверки необходимости отображения.</returns>
    public override bool NeedShowRegistrationPane(bool conditions)
    {
      return base.NeedShowRegistrationPane(true);
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      if (_obj.VerificationState == VerificationState.InProcess && this.IsNumerationSucceed())
      {
        this.EnableRequisitesForVerification();
      }
      else
      {
        base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
        var properties = _obj.State.Properties;
        
        var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
        
        properties.TotalAmount.IsEnabled = enabledState || _obj.TotalAmount == null;
        properties.Currency.IsEnabled = enabledState || _obj.Currency == null;

        // Документ-основание.
        isEnabled = isEnabled && enabledState;

        var leadingNumberIncludedInNumber = _obj.DocumentRegister != null &&
          (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber) ||
           _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
        
        properties.LeadingDocument.IsEnabled = !leadingNumberIncludedInNumber;
        
        if (_obj.IsFormalized == true)
        {
          properties.BusinessUnit.IsEnabled = false;
          properties.IsAdjustment.IsEnabled = false;
          properties.DocumentKind.IsEnabled = isEnabled || isRepeatRegister;
        }
      }
      
      // Корректируемый документ доступен, если проставлен признак Корректировка.
      _obj.State.Properties.Corrected.IsEnabled = _obj.IsAdjustment == true;
      
      this.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Заполнить подписывающего.
    /// </summary>
    /// <param name="signatory">Подписывающий со стороны контрагента.</param>
    public override void FillCounterpartySignatory(Parties.IContact signatory)
    {
      _obj.CounterpartySignatory = signatory;
    }
    
    /// <summary>
    /// Заполнить основание со стороны контрагента.
    /// </summary>
    /// <param name="signingReason">Основание контрагента.</param>
    public override void FillCounterpartySigningReason(string signingReason)
    {
      if (!string.IsNullOrEmpty(signingReason) && signingReason.Length > _obj.Info.Properties.CounterpartySigningReason.Length)
        signingReason = signingReason.Substring(0, _obj.Info.Properties.CounterpartySigningReason.Length);
      _obj.CounterpartySigningReason = signingReason;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName;
      }
      
      if (_obj.DocumentKind == null || (!_obj.DocumentKind.GenerateDocumentName.Value && _obj.IsFormalized != true))
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> <контрагент> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += " " + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }

      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment != true)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment == true)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          name = AccountingDocumentBases.Resources.Adjustment + _obj.DocumentKind.ShortName.ToLower() + name;
        }
      }

      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    #region Доступность свойств
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Подразделение обязательно, только если это указано в метаданных.
      _obj.State.Properties.Department.IsRequired = _obj.Info.Properties.Department.IsRequired;
      
      // Содержание обязательно, только если это указано в метаданных.
      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired;
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      _obj.State.Properties.Counterparty.IsRequired = isVisualMode;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return _obj.Counterparty == null;
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled;
    }
    
    #endregion
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
    }
    
    /// <summary>
    /// Добавить связанные с финансовыми документами документы в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Получить ведущий документ.
      var financialDocuments = _obj.Relations.GetRelatedFrom(Contracts.PublicConstants.Module.AccountingDocumentsRelationName);
      var documentsToAdd = financialDocuments.Where(d => !group.All.Contains(d)).ToList();
      
      // Добавить корректируемые документы.
      var correctDocuments = _obj.Relations.GetRelatedFrom(Constants.Module.CorrectionRelationName);
      documentsToAdd.AddRange(correctDocuments.Where(d => !group.All.Contains(d)).ToList());
      
      foreach (var document in documentsToAdd)
        group.All.Add(document);
      
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      var properties = _obj.State.Properties;
      
      var leadingNumberIncludedInNumber = _obj.DocumentRegister != null &&
        (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber) ||
         _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
      
      properties.Counterparty.IsEnabled = isEnabled && !counterpartyCodeInNumber && (!leadingNumberIncludedInNumber && _obj.IsFormalized != true && enabledState);
      
      if (_obj.IsFormalized == true)
        properties.Counterparty.IsEnabled = false;
    }

    /// <summary>
    /// Проверять рег. номер на уникальность.
    /// </summary>
    /// <returns>True - проверять, False - не проверять.</returns>
    public override bool CheckRegistrationNumberUnique()
    {
      return false;
    }
    
    /// <summary>
    /// Сделать доступными рег. номер и рег. дату незарегистрированного документа регистрируемого вида в процессе верификации.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      var verificationInProcess = _obj.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess;
      if (!verificationInProcess)
        return;
      
      base.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Рассчитать сумму НДС.
    /// </summary>
    /// <param name="totalAmount">Сумма с НДС.</param>
    /// <param name="vatRate">Ставка НДС.</param>
    /// <returns>Сумма НДС.</returns>
    [Public]
    public virtual double GetVATAmountFromTotal(double totalAmount, Sungero.Commons.IVATRate vatRate)
    {
      if (vatRate == null)
        throw new ArgumentNullException(VATRates.Info.LocalizedName);
      
      if (!vatRate.Rate.HasValue)
        throw new ArgumentNullException(VATRates.Info.Properties.Rate.LocalizedName);
      
      var rateValue = Math.Round((double)vatRate.Rate.Value / 100, 2);
      return Math.Round(totalAmount * rateValue / (1 + rateValue), 2);
    }
    
    /// <summary>
    /// Заполнить свойство "Ведущий документ" в зависимости от типа документа.
    /// </summary>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <remarks>Используется при смене типа.</remarks>
    [Public]
    public override void FillLeadingDocument(IOfficialDocument leadingDocument)
    {
      var contractualDocument = ContractualDocumentBases.As(leadingDocument);
      if (contractualDocument != null && (_obj.Counterparty == null || Equals(_obj.Counterparty, contractualDocument.Counterparty)))
        _obj.LeadingDocument = contractualDocument;
      else
        _obj.LeadingDocument = null;
    }
  }
}
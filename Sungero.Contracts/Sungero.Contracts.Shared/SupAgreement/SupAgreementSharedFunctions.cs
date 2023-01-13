using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.SupAgreement;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Shared
{
  partial class SupAgreementFunctions
  {
    #region Интеллектуальная обработка
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return _obj.LeadingDocument == null || base.HasEmptyRequiredProperties();
    }
    
    #endregion
    
    /// <summary>
    /// Получение категории договора, указанного в доп. соглашении.
    /// </summary>
    /// <returns>Категория договора.</returns>
    public override IDocumentGroupBase GetDocumentGroup()
    {
      return _obj.LeadingDocument != null ? _obj.LeadingDocument.DocumentGroup : null;
    }

    /// <summary>
    /// Проверить доп. соглашение на дубли.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="contract">Договор.</param>
    /// <returns>Признак дублей.</returns>
    public static bool HaveDuplicates(ISupAgreement supAgreement,
                                      Sungero.Company.IBusinessUnit businessUnit,
                                      string registrationNumber,
                                      DateTime? registrationDate,
                                      Sungero.Parties.ICounterparty counterparty,
                                      IOfficialDocument contract)
    {
      if (supAgreement == null ||
          businessUnit == null ||
          string.IsNullOrWhiteSpace(registrationNumber) ||
          registrationDate == null ||
          counterparty == null ||
          contract == null)
        return false;
      
      return Functions.SupAgreement.Remote.GetDuplicates(supAgreement,
                                                         businessUnit,
                                                         registrationNumber,
                                                         registrationDate,
                                                         counterparty,
                                                         contract)
        .Any();
    }
    
    #region Регистрация
    
    /// <summary>
    /// Получить номер ведущего документа.
    /// </summary>
    /// <returns>Номер документа либо пустая строка.</returns>
    public override string GetLeadDocumentNumber()
    {
      if (_obj.LeadingDocument != null)
      {
        return _obj.LeadingDocument.AccessRights.CanRead() ?
          _obj.LeadingDocument.RegistrationNumber :
          Functions.ContractualDocument.Remote.GetRegistrationNumberIgnoreAccessRights(_obj.LeadingDocument.Id);
      }
      return string.Empty;
    }
    
    #endregion
    
    /// <summary>
    /// Получить данные доп.согл. для формирования имени акта.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <returns>Часть имени.</returns>
    /// <remarks>Если нет прав на доп. соглашение, то возникнет исключение.</remarks>
    public static string GetNamePartBySupAgreement(ISupAgreement supAgreement)
    {
      var namePart = ContractBases.Resources.NamePartForLeadDocument + supAgreement.DocumentKind.ShortName.ToLower();
      if (!string.IsNullOrWhiteSpace(supAgreement.RegistrationNumber))
        namePart += OfficialDocuments.Resources.Number + supAgreement.RegistrationNumber;
      
      namePart += Functions.ContractBase.GetContractNamePart(supAgreement.LeadingDocument);
      
      return namePart;
    }

    /// <summary>
    /// Получить данные доп.согл. для формирования имени акта.
    /// </summary>
    /// <param name="supAgreement">Доп. соглашение.</param>
    /// <returns>Часть имени.</returns>
    /// <remarks>Игнорирует права доступа на доп. соглашение.</remarks>
    [Public]
    public static string GetSupAgreementNamePart(ISupAgreement supAgreement)
    {
      if (supAgreement == null)
        return string.Empty;
      
      return supAgreement.AccessRights.CanRead() ?
        GetNamePartBySupAgreement(supAgreement) :
        Functions.SupAgreement.Remote.GetNamePartBySupAgreementIgnoreAccessRights(supAgreement.Id);
    }
    
    public override void SetObsolete(bool isActive)
    {
      if (isActive)
        _obj.LifeCycleState = LifeCycleState.Terminated;
      else
        base.SetObsolete(isActive);
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
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> к договору № <номер_договора> с <наименование контрагента> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.LeadingDocument != null)
          name += Functions.ContractBase.GetContractNamePart(_obj.LeadingDocument);
        
        if (!string.IsNullOrEmpty(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
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

        var supProperties = _obj.State.Properties;
        
        var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
        
        isEnabled = isEnabled && enabledState;
        
        // Признак "Типовой"
        supProperties.IsStandard.IsEnabled = isEnabled;
        
        // Ведущий документ.
        var leadingNumberIncludedInNumber = isRepeatRegister && _obj.DocumentRegister != null &&
          (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber) ||
           _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
        
        supProperties.LeadingDocument.IsEnabled = isEnabled && !leadingNumberIncludedInNumber || _obj.Contact == null;
      }
      
      this.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Добавить связанные с допсоглашением документы в группу вложений.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Получить договор.
      var contractDocuments = _obj.Relations.GetRelatedFrom(Constants.Module.SupAgreementRelationName);
      
      var documentsToAdd = contractDocuments.Where(d => !group.All.Contains(d)).ToList();
      foreach (var document in documentsToAdd)
        group.All.Add(document);
      
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      
      // При визуальной работе обязательность ведущего документа как в SupAgreement.
      // При программной работе поле делаем необязательными, чтобы сбросить обязательность,
      // если она изменилась в вызове текущего метода в базовой сущности.
      _obj.State.Properties.LeadingDocument.IsRequired = isVisualMode;
    }
    
    /// <summary>
    /// Заполнить свойство "Ведущий документ" в зависимости от типа документа.
    /// </summary>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <remarks>Используется при смене типа.</remarks>
    public override void FillLeadingDocument(Docflow.IOfficialDocument leadingDocument)
    {
      var contract = ContractBases.As(leadingDocument);
      if (contract != null && (_obj.Counterparty == null || Equals(_obj.Counterparty, contract.Counterparty)))
        _obj.LeadingDocument = contract;
      else
        _obj.LeadingDocument = null;
    }
  }
}
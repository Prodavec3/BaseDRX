using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.ContractBase;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Contracts.Shared
{
  partial class ContractBaseFunctions
  {
    
    /// <summary>
    /// Проверить договор на дубли.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="registrationNumber">Номер договора.</param>
    /// <param name="registrationDate">Дата договора.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Признак наличия дублей.</returns>
    public static bool HaveDuplicates(IContractBase contract,
                                      Sungero.Company.IBusinessUnit businessUnit,
                                      string registrationNumber,
                                      DateTime? registrationDate,
                                      Sungero.Parties.ICounterparty counterparty)
    {
      if (contract == null ||
          businessUnit == null ||
          string.IsNullOrWhiteSpace(registrationNumber) ||
          registrationDate == null ||
          counterparty == null)
        return false;
      
      return Functions.ContractBase.Remote.GetDuplicates(contract,
                                                         businessUnit,
                                                         registrationNumber,
                                                         registrationDate,
                                                         counterparty)
        .Any();
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

        var contractProperties = _obj.State.Properties;
        var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                             _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
        // Категория договора.
        contractProperties.DocumentGroup.IsEnabled = isEnabled;
        // Признак "Типовой"
        contractProperties.IsStandard.IsEnabled = isEnabled && enabledState;
      }
      
      this.EnableRegistrationNumberAndDate();
    }

    /// <summary>
    /// Получить данные договора для формирования имени доп.согл и акта.
    /// </summary>
    /// <param name="contract">Документ.</param>
    /// <returns>Данные документа.</returns>
    /// <remarks>Если нет прав на договор, то возникнет исключение.</remarks>
    public static string GetNamePartByContract(IContractualDocument contract)
    {
      var namePart = ContractBases.Resources.NamePartForLeadDocument + contract.DocumentKind.ShortName.ToLower();
      if (!string.IsNullOrWhiteSpace(contract.RegistrationNumber))
        namePart += OfficialDocuments.Resources.Number + contract.RegistrationNumber;
      if (contract.Counterparty != null)
        namePart += ContractBases.Resources.NamePartForContractor + contract.Counterparty.DisplayValue;
      return namePart;
    }
    
    /// <summary>
    /// Получить данные договора для формирования имени доп.согл и акта.
    /// </summary>
    /// <param name="contract">Документ.</param>
    /// <returns>Данные документа.</returns>
    /// <remarks>Игнорирует права доступа на договор.</remarks>
    [Public]
    public static string GetContractNamePart(IContractBase contract)
    {
      if (contract == null)
        return string.Empty;
      
      return contract.AccessRights.CanRead() ?
        GetNamePartByContract(contract) :
        Functions.ContractBase.Remote.GetNamePartByContractIgnoreAccessRights(contract.Id);
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
        <Вид документа> №<номер> от <дата> с <контрагент> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += ContractBases.Resources.NamePartForContractor + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
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
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      var hasAvailableCategories = Docflow.DocumentGroupBases.GetAllCached(g => g.Status == CoreEntities.DatabookEntry.Status.Active &&
                                                                           g.DocumentKinds.Any(d => Equals(d.DocumentKind, _obj.DocumentKind))).Any();
      
      _obj.State.Properties.DaysToFinishWorks.IsRequired = _obj.IsAutomaticRenewal == true;
      _obj.State.Properties.ValidTill.IsRequired = _obj.IsAutomaticRenewal == true || _obj.DaysToFinishWorks != null;
      _obj.State.Properties.DocumentGroup.IsRequired = _obj.DocumentKind != null && hasAvailableCategories;
      base.SetRequiredProperties();
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При визуальной работе обязательность категории вычисляется по стандартной логике.
      // При программной работе категорию делаем необязательными.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      if (!isVisualMode)
        _obj.State.Properties.DocumentGroup.IsRequired = false;
    }
    
    /// <summary>
    /// Проверять рег. номер на уникальность.
    /// </summary>
    /// <returns>True - проверять, False - не проверять.</returns>
    public override bool CheckRegistrationNumberUnique()
    {
      return false;
    }
    
    #region Интеллектуальная обработка
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      var hasAvailableCategories = Docflow.DocumentGroupBases.GetAllCached(g => g.Status == CoreEntities.DatabookEntry.Status.Active &&
                                                                           g.DocumentKinds.Any(d => Equals(d.DocumentKind, _obj.DocumentKind))).Any();
      
      return (_obj.DocumentKind != null && hasAvailableCategories && _obj.DocumentGroup == null) ||
        base.HasEmptyRequiredProperties();
    }
    
    #endregion
    
  }
}
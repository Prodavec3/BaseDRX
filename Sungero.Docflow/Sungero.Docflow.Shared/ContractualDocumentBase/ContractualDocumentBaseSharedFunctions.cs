using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ContractualDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class ContractualDocumentBaseFunctions
  {
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При визуальной работе обязательность содержания и контрагента как в Contract.
      // Обязательность категории вычисляется по стандартной логике.
      // При программной работе содержание, контрагента и категорию делаем необязательными.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      _obj.State.Properties.Subject.IsRequired = isVisualMode;
      _obj.State.Properties.Counterparty.IsRequired = isVisualMode;
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
      
      _obj.State.Properties.TotalAmount.IsEnabled = enabledState || _obj.TotalAmount == null;
      _obj.State.Properties.Currency.IsEnabled = enabledState || _obj.Currency == null;
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
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled && !counterpartyCodeInNumber && (enabledState || _obj.Counterparty == null);
    }
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Subject) || _obj.Counterparty == null;
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
    
    #endregion
  }
}
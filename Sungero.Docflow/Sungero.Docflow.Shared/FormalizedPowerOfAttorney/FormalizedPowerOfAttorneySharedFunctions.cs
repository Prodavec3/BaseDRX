using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow.Shared
{
  partial class FormalizedPowerOfAttorneyFunctions
  {
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var properties = _obj.State.Properties;
      
      properties.UnifiedRegistrationNumber.IsVisible = needShow;
    }
    
    /// <summary>
    /// Установить состояние жизненного цикла эл. доверенности в Действующее.
    /// </summary>
    [Public]
    public virtual void SetActiveLifeCycleState()
    {
      if (_obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
          _obj.IssuedTo != null && _obj.BusinessUnit != null && _obj.Department != null && _obj.HasVersions)
      {
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
      }
    }
    
    /// <summary>
    /// Проверять рег. номер на уникальность.
    /// </summary>
    /// <returns>True - проверять, False - не проверять.</returns>
    public override bool CheckRegistrationNumberUnique()
    {
      return false;
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<Единый рег. номер> (рег. №<номер>) от <дата> для <Кому выдана> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.UnifiedRegistrationNumber))
          name += FormalizedPowerOfAttorneys.Resources.UnifiedRegistrationNumberFormat(_obj.UnifiedRegistrationNumber);
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += FormalizedPowerOfAttorneys.Resources.RegistrationNumberInBracketsFormat(_obj.RegistrationNumber);
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.IssuedTo != null)
          name += PowerOfAttorneyBases.Resources.DocumentnameFor + Company.PublicFunctions.Employee.GetShortName(_obj.IssuedTo, DeclensionCase.Genitive, true);
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Functions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Functions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    #region Доступность свойств
    
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      // Поля сроков действия недоступны для редактирования в эл. доверенности.
      _obj.State.Properties.ValidTill.IsEnabled = false;
      _obj.State.Properties.ValidFrom.IsEnabled = false;
    }
    
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired;
      
      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
      _obj.State.Properties.IssuedTo.IsRequired = isVisualMode;
      _obj.State.Properties.BusinessUnit.IsRequired = isVisualMode;
      _obj.State.Properties.Department.IsRequired = isVisualMode;
    }
    
    #endregion
    
    #region Поиск дублей
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей.
    /// </summary>
    /// <returns>Текст ошибки или пустая строка, если ошибок нет.</returns>
    public virtual string GetDuplicatesErrorText()
    {
      var duplicates = this.GetDuplicates();
      
      if (!duplicates.Any())
        return string.Empty;
      
      // Сформировать текст ошибки.
      return FormalizedPowerOfAttorneys.Resources.DuplicatesDetected;
    }
    
    /// <summary>
    /// Получить дубли эл. доверенности.
    /// </summary>
    /// <returns>Дубли эл. доверенности.</returns>
    public virtual List<IFormalizedPowerOfAttorney> GetDuplicates()
    {
      return Functions.FormalizedPowerOfAttorney.Remote.GetFormalizedPowerOfAttorneyDuplicates(_obj);
    }
    
    #endregion
  }
}
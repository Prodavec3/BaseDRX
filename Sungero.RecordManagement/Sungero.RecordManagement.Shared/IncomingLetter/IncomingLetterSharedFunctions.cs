using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.IncomingLetter;

namespace Sungero.RecordManagement.Shared
{
  partial class IncomingLetterFunctions
  {
    
    /// <summary>
    /// Проверить письмо на дубликаты.
    /// </summary>
    /// <param name="letter">Входящее письмо.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="correspondentNumber">Номер корреспондента.</param>
    /// <param name="dated">Дата письма.</param>
    /// <param name="correspondent">Корреспондент.</param>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public static bool HaveDuplicates(IIncomingLetter letter,
                                      Sungero.Docflow.IDocumentKind documentKind,
                                      Company.IBusinessUnit businessUnit,
                                      string correspondentNumber,
                                      DateTime? dated,
                                      Parties.ICounterparty correspondent)
    {
      if (documentKind == null ||
          businessUnit == null ||
          string.IsNullOrEmpty(correspondentNumber) ||
          !dated.HasValue ||
          correspondent == null)
        return false;
      
      return Functions.IncomingLetter.Remote.GetDuplicates(letter, documentKind, businessUnit, correspondentNumber, dated, correspondent).Any();
    }
    
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == Docflow.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName; 
      }
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> от <корреспондент> №<номер> от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        
        if (_obj.Correspondent != null)
          name += IncomingLetters.Resources.CorrespondentFrom + _obj.Correspondent.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
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
    /// Сменить доступность реквизитов документа, блокируемых после регистрации.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">True, если повторная регистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);

      var letterProperties = _obj.State.Properties;
      var canRegister = _obj.AccessRights.CanRegister();
      
      // Основная группа.
      letterProperties.Dated.IsEnabled = isEnabled;
      letterProperties.InNumber.IsEnabled = isEnabled;
      
      // Свойства блокируются для всех, кроме делопроизводителей.
      if (!canRegister)
      {
        letterProperties.SignedBy.IsEnabled = isEnabled;
        letterProperties.Contact.IsEnabled = isEnabled;
        letterProperties.Assignee.IsEnabled = isEnabled;
        letterProperties.Addressee.IsEnabled = isEnabled;
      }
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();

      // Изменить обязательность полей в зависимости от того, программная или визуальная работа.
      var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);

      // При визуальной работе обязательность содержания и корреспондента как в IncomingLetter.
      // При программной работе содержание и корреспондент - необязательные.
      // Чтобы сбросить обязательность, если она изменилась в вызове текущего метода в базовой сущности.
      _obj.State.Properties.Subject.IsRequired = isVisualMode;
      _obj.State.Properties.Correspondent.IsRequired = isVisualMode;
    }
    
    #region Интеллектуальная обработка
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    [Public]
    public override bool HasEmptyRequiredProperties()
    {
      return string.IsNullOrEmpty(_obj.Subject) || _obj.Correspondent == null;
    }
    
    #endregion
  }
}
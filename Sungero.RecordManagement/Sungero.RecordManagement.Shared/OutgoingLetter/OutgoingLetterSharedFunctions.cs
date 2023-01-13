using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.OutgoingLetter;

namespace Sungero.RecordManagement.Shared
{
  partial class OutgoingLetterFunctions
  {
    public override void FillName()
    {
      if (_obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /*Имя в форматах:
          <Вид документа> в <корреспондент> №<номер> от <дата> "<содержание>".        | Для организации
          <Вид документа> для <корреспондент> №<номер> от <дата> "<содержание>".      | Для персоны
          <Вид документа> по списку рассылки №<номер> от <дата> "<содержание>".       | Для нескольких адресатов
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.IsManyAddressees == true && _obj.Addressees.Any())
          name += OutgoingLetters.Resources.CorrespondentToManyAddressees;
        else if (_obj.Correspondent != null && !Equals(_obj.Correspondent, Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty()))
          name += string.Format("{0}{1}", Sungero.Parties.People.Is(_obj.Correspondent) ? OutgoingLetters.Resources.CorrespondentToPerson : OutgoingLetters.Resources.CorrespondentToCompany,
                                _obj.Correspondent.DisplayValue);
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += string.Format("{0}{1}", Sungero.Docflow.OfficialDocuments.Resources.Number, _obj.RegistrationNumber);
        
        if (_obj.RegistrationDate != null)
          name += string.Format("{0}{1}", Sungero.Docflow.OfficialDocuments.Resources.DateFrom, _obj.RegistrationDate.Value.ToString("d"));
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += string.Format(" \"{0}\"", _obj.Subject);
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = string.Format("{0}{1}", _obj.DocumentKind.ShortName, name);
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">True, если повторная регистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);

      var letterProperties = _obj.State.Properties;
      var canRegister = _obj.AccessRights.CanRegister();
      
      // Свойства блокируются для всех, кроме делопроизводителей.
      if (!canRegister)
      {
        letterProperties.Assignee.IsEnabled = isEnabled;
        letterProperties.Addressee.IsEnabled = isEnabled;
      }
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки письма.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      if (_obj.IsManyAddressees == true)
        return this.GetEmailAddresseesFromManyAddresseesLetter();
      else
        return this.GetEmailAddresseesFromSingleAddresseeLetter();
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки многоадресного письма.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    public virtual List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddresseesFromManyAddresseesLetter()
    {
      var result = new List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee>();
      
      var allEmails = new StringBuilder();
      var correspondentEmails = new StringBuilder();
      var canSendByMailList = false;
      foreach (var addresseeRow in _obj.Addressees)
      {
        var addresseeHasEmail = addresseeRow.Addressee != null && !string.IsNullOrWhiteSpace(addresseeRow.Addressee.Email);
        var correspondentHasEmail = addresseeRow.Correspondent != null && !string.IsNullOrWhiteSpace(addresseeRow.Correspondent.Email);
        if (addresseeHasEmail)
        {
          allEmails.Append(string.Format("{0}; ", addresseeRow.Addressee.Email));
          canSendByMailList = true;
        }
        
        if (correspondentHasEmail && !addresseeHasEmail)
          allEmails.Append(string.Format("{0}; ", addresseeRow.Correspondent.Email));
        
        if (correspondentHasEmail)
          correspondentEmails.Append(string.Format("{0}; ", addresseeRow.Correspondent.Email));
      }
      
      if (!string.IsNullOrWhiteSpace(correspondentEmails.ToString()))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.RecordManagement.OutgoingLetters.Resources.Correspondents,
                  correspondentEmails.ToString());
        result.Add(emailAddressee);
      }
      
      if (canSendByMailList)
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.RecordManagement.OutgoingLetters.Resources.DistributionListCounterpartyName,
                  allEmails.ToString());
        result.Add(emailAddressee);
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки одноадресного письма.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    public virtual List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddresseesFromSingleAddresseeLetter()
    {
      var result = new List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee>();
      
      // Получить корреспондента.
      if (_obj.Correspondent != null && !string.IsNullOrWhiteSpace(_obj.Correspondent.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Correspondent.Name, _obj.Correspondent.Email),
                  _obj.Correspondent.Email);
        result.Add(emailAddressee);
      }
      
      // Получить адресата.
      if (_obj.Addressee != null && !string.IsNullOrWhiteSpace(_obj.Addressee.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Addressee.Name, _obj.Addressee.Email),
                  _obj.Addressee.Email);
        result.Add(emailAddressee);
      }
      
      return result;
    }
    
  }
}
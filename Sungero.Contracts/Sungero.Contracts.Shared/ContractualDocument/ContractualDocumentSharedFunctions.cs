using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Shared
{
  partial class ContractualDocumentFunctions
  {
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Пользователь, ответственный за документ.</returns>
    public override Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.ResponsibleEmployee != null)
        return _obj.ResponsibleEmployee;
      
      return base.GetDocumentResponsibleEmployee();
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки вложением в письмо.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      var result = new List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee>();
      
      // Получить контрагента.
      if (_obj.Counterparty != null && !string.IsNullOrWhiteSpace(_obj.Counterparty.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Counterparty.Name, _obj.Counterparty.Email),
                  _obj.Counterparty.Email);
        result.Add(emailAddressee);
      }
      
      // Получить контакта.
      if (_obj.Contact != null && !string.IsNullOrWhiteSpace(_obj.Contact.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Contact.Name, _obj.Contact.Email),
                  _obj.Contact.Email);
        result.Add(emailAddressee);
      }
      
      // Получить подписывающего.
      if (_obj.CounterpartySignatory != null && !string.IsNullOrWhiteSpace(_obj.CounterpartySignatory.Email) && !Equals(_obj.CounterpartySignatory, _obj.Contact))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.CounterpartySignatory.Name, _obj.CounterpartySignatory.Email),
                  _obj.CounterpartySignatory.Email);
        result.Add(emailAddressee);
      }
      
      return result;
    }
  }
}
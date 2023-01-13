using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchive.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки вложением в письмо.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список адресатов.</returns>
    [Public]
    public virtual List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees(Docflow.IAccountingDocumentBase document)
    {
      var result = new List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee>();
      
      // Получить контрагента.
      if (document.Counterparty != null && !string.IsNullOrWhiteSpace(document.Counterparty.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(document.Counterparty.Name, document.Counterparty.Email),
                  document.Counterparty.Email);
        result.Add(emailAddressee);
      }
      
      // Получить контакт.
      if (document.Contact != null && !string.IsNullOrWhiteSpace(document.Contact.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(document.Contact.Name, document.Contact.Email),
                  document.Contact.Email);
        result.Add(emailAddressee);
      }
      
      // Получить подписывающего.
      if (document.CounterpartySignatory != null && !string.IsNullOrWhiteSpace(document.CounterpartySignatory.Email) && !Equals(document.CounterpartySignatory, document.Contact))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(document.CounterpartySignatory.Name, document.CounterpartySignatory.Email),
                  document.CounterpartySignatory.Email);
        result.Add(emailAddressee);
      }
      
      return result;
    }
  }
}
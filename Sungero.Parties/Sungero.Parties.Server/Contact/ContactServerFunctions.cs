using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties.Server
{
  partial class ContactFunctions
  {
    /// <summary>
    /// Получить дубли контактов.
    /// </summary>
    /// <returns>Контакты, дублирующие текущего по ФИО.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IContact> GetDuplicates()
    {
      return Contacts.GetAll()
        .Where(contact =>
               !Equals(_obj, contact) &&
               Equals(contact.Name, _obj.Name) &&
               Equals(contact.Company, _obj.Company) &&
               contact.Status != Sungero.Parties.Contact.Status.Closed);
    }
    
    /// <summary>
    /// Получить контактные лица по имени.
    /// </summary>
    /// <param name="name">Имя в формате "Фамилия И.О." или "Фамилия Имя Отчество".</param>
    /// <param name="personShortName">Имя в формате "Фамилия И.О.".</param>
    /// <param name="counterparty">Контрагент, владелец контакта.</param>
    /// <returns>Коллекция контактных лиц.</returns>
    [Public]
    public static IQueryable<IContact> GetContactsByName(string name, string personShortName, ICounterparty counterparty)
    {
      var nonBreakingSpace = new string('\u00A0', 1);
      var space = new string('\u0020', 1);
      
      name = name.ToLower().Replace(nonBreakingSpace, space).Replace(". ", ".");
      
      var contacts = Contacts.GetAll()
        .Where(x => (x.Name.ToLower().Replace(nonBreakingSpace, space).Replace(". ", ".") == name) ||
               (x.Person != null && string.Equals(x.Person.ShortName, personShortName, StringComparison.InvariantCultureIgnoreCase)));
      
      if (counterparty != null)
        return contacts.Where(c => c.Company.Equals(counterparty));
      
      return contacts;
    }
  }
}
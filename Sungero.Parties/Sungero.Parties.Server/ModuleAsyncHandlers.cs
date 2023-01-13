using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Обновить имя контакта из персоны.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void UpdateContactName(Sungero.Parties.Server.AsyncHandlerInvokeArgs.UpdateContactNameInvokeArgs args)
    {
      int personId = args.PersonId;
      Logger.DebugFormat("UpdateContactName: start update contact name. Person id: {0}.", personId);
      var contacts = Parties.Contacts.GetAll(x => x.Person != null && x.Person.Id == personId);
      
      if (!contacts.Any())
      {
        Logger.DebugFormat("UpdateContactName: contact not found. Person id: {0}.", personId);
        return;
      }
      
      foreach (var contact in contacts)
      {
        try
        {
          Parties.Functions.Contact.UpdateName(contact, contact.Person);
          contact.Save();
        }
        catch
        {
          Logger.DebugFormat("UpdateContactName: could not update name. Contact id: {0}.", contact.Id);
          args.Retry = true;
          continue;
        }
        Logger.DebugFormat("UpdateContactName: name updated successfully. Contact id: {0}. Person id: {1}.", contact.Id, personId);
      }
    }

  }
}
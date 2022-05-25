using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties.Client
{
  public partial class ContactActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.Contact.Remote.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Sungero.Commons.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void GoToWebsite(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.GoToWebsite(_obj.Homepage, e);
    }

    public virtual bool CanGoToWebsite(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.Module.CanGoToWebsite(_obj.Homepage);
    }

    public virtual void WriteLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.WriteLetter(_obj.Email);
    }

    public virtual bool CanWriteLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
}
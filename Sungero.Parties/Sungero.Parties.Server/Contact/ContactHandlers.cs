using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Contact;

namespace Sungero.Parties
{
  partial class ContactServerHandlers
  {
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Functions.Contact.HaveDuplicates(_obj))
        e.AddWarning(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      if (!Commons.PublicFunctions.Module.IsAllExternalEntityLinksDeleted(_obj))
        throw AppliedCodeException.Create(Commons.Resources.HasLinkedExternalEntities);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      ICompanyBase company;
      if (CallContext.CalledFrom(CompanyBases.Info))
      {
        company = CompanyBases.Get(CallContext.GetCallerEntityId(CompanyBases.Info));
        _obj.Company = company;
      }
      
      if (Contacts.Info.Properties.Company.TryGetFilter(out company))
      {
        _obj.Company = company;
      }
    }
  }
}
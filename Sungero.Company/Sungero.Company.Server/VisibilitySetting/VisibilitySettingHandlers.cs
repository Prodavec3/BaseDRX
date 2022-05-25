using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.VisibilitySetting;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class VisibilitySettingHiddenRecipientsRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> HiddenRecipientsRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return query;
    }
  }

  partial class VisibilitySettingUnrestrictedRecipientsRecipientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> UnrestrictedRecipientsRecipientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      return (IQueryable<T>)Functions.Module.ExcludeSystemRecipients(query, true);
    }
  }

  partial class VisibilitySettingServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      throw AppliedCodeException.Create(Docflow.Resources.DeleteSettingsException);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedRestrictVisibility = false;
      _obj.NeedRestrictVisibilityDescription = Sungero.Company.VisibilitySettings.Resources.NeedRestrictVisibilityDescription;
    }
  }

}
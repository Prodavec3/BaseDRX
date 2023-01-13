using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow
{
  partial class AddendumClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      // Отобразить однократно нотифайку о выдаче прав на проектные документы.
      if (_obj.State.IsInserted && _obj.LeadingDocument != null && _obj.LeadingDocument.Project != null && Projects.Projects.Is(_obj.LeadingDocument.Project))
        Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Projects.Resources.ProjectDocumentRightsNotifyMessage);
    }

    public override void LeadingDocumentValueInput(Sungero.Docflow.Client.OfficialDocumentLeadingDocumentValueInputEventArgs e)
    {
      base.LeadingDocumentValueInput(e);
      
      // Отобразить однократно нотифайку о выдаче прав на проектные документы.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && e.NewValue.Project != null && Projects.Projects.Is(e.NewValue.Project))
        Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Projects.Resources.ProjectDocumentRightsNotifyMessage);
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectDocument;

namespace Sungero.Projects
{
  partial class ProjectDocumentSharedHandlers
  {
    public override void ProjectChanged(Sungero.Docflow.Shared.OfficialDocumentProjectChangedEventArgs e)
    {
      base.ProjectChanged(e);
      
      if (e.NewValue != null || e.OriginalValue != null)
        FillName();
    }
  }
}
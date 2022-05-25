using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Addendum;

namespace Sungero.Docflow
{
  partial class AddendumSharedHandlers
  {

    public override void LeadingDocumentChanged(Sungero.Docflow.Shared.OfficialDocumentLeadingDocumentChangedEventArgs e)
    {
      base.LeadingDocumentChanged(e);
      
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (e.NewValue != null)
      {
        var document = e.NewValue;
        if (document.BusinessUnit != null)
          _obj.BusinessUnit = document.BusinessUnit;
        
        Docflow.PublicFunctions.OfficialDocument.CopyProjects(e.NewValue, _obj);
      }
      
      FillName();
      _obj.Relations.AddFromOrUpdate(Constants.Module.AddendumRelationName, e.OldValue, e.NewValue);
    }
  }
}
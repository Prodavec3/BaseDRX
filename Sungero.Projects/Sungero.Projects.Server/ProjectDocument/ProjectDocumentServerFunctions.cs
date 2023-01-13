using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectDocument;

namespace Sungero.Projects.Server
{
  partial class ProjectDocumentFunctions
  {

    public override Sungero.Company.IEmployee GetDefaultSignatory()
    {
      if (Company.Employees.Current != null)
      {
        var businessUnit = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Company.Employees.Current);
        var ceo = businessUnit != null ? businessUnit.CEO : null;
        
        if (ceo != null && Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.CanSignByEmployee(_obj, ceo))
          return ceo;
      }
      return base.GetDefaultSignatory();
    }
  }
}
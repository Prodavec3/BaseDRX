using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Projects.ProjectApprovalRole;

namespace Sungero.Projects.Shared
{
  partial class ProjectApprovalRoleFunctions
  {
    public override List<Sungero.Docflow.IDocumentKind> Filter(List<Sungero.Docflow.IDocumentKind> kinds)
    {
      var query = base.Filter(kinds);
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ProjectManager ||
          _obj.Type == Docflow.ApprovalRoleBase.Type.ProjectAdmin)
        query = query.Where(k => k.ProjectsAccounting == true).ToList();
      return query;
    }
  }
}
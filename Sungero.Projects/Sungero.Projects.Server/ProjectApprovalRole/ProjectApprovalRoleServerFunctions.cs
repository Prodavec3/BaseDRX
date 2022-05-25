using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Projects.ProjectApprovalRole;

namespace Sungero.Projects.Server
{
  partial class ProjectApprovalRoleFunctions
  {
    public override IEmployee GetRolePerformer(IApprovalTask task)
    {
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ProjectManager)
        return this.GetProjectManager(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ProjectAdmin)
        return this.GetProjectAdministrator(task);
      
      return base.GetRolePerformer(task);
    }
    
    private IEmployee GetProjectManager(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var project = Projects.As(document.Project);
      if (project != null)
        return project.Manager;
      
      return null;
    }
    
    private IEmployee GetProjectAdministrator(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var project = Projects.As(document.Project);
      if (project != null)
        return project.Administrator ?? project.Manager;
      
      return null;
    }
  }
}
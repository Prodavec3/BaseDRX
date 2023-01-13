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
    /// <summary>
    /// Получить сотрудника из роли согласования.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public override IEmployee GetRolePerformer(IApprovalTask task)
    {
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ProjectManager)
        return this.GetProjectManager(task);
      
      if (_obj.Type == Docflow.ApprovalRoleBase.Type.ProjectAdmin)
        return this.GetProjectAdministrator(task);
      
      return base.GetRolePerformer(task);
    }
    
    /// <summary>
    /// Получить сотрудника с ролью согласования Руководитель проекта.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    private IEmployee GetProjectManager(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var project = Projects.As(document.Project);
      if (project != null)
        return project.Manager;
      
      return null;
    }
    
    /// <summary>
    /// Получить сотрудника с ролью согласования Администратор проекта.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
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
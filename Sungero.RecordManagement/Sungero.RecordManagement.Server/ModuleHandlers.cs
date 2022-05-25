using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.DocumentKind;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemsFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.ITask> ActionItemsDataQuery(IQueryable<Sungero.Workflow.ITask> query)
    {
      query = query.Where(t => ActionItemExecutionTasks.Is(t));
      if (_filter == null)
        return Functions.Module.ApplyCommonSubfolderFilters(query);
      
      // Фильтры по статусу и периоду.
      query = Functions.Module.ApplyCommonSubfolderFilters(query, _filter.InProcess,
                                                            _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      return query;
    }

    public virtual bool IsActionItemsVisible()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
        Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole() ||
        Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
    }
  }

  partial class ForExecutionFolderHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> ForExecutionDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      var result = query.Where(a => ActionItemExecutionAssignments.Is(a));
      
      // Запрос количества непрочитанных без фильтра.
      if (_filter == null)
        return Functions.Module.ApplyCommonSubfolderFilters(result);
      
      // Фильтры по статусу, замещению и периоду.
      result = Functions.Module.ApplyCommonSubfolderFilters(result, _filter.InProcess,
                                                            _filter.Last30Days, _filter.Last90Days, _filter.Last180Days, false);
      
      return result;
    }

    public virtual bool IsForExecutionVisible()
    {
      return !Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }
}
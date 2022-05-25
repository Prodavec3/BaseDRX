using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Показать документы контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    [Public]
    public void SearchDocumentsWithCounterparties(Sungero.Parties.ICounterparty counterparty)
    {
      Functions.Module.Remote.GetDocumentsWithCounterparties(counterparty).Show(counterparty.Name);
    }
    
    /// <summary>
    /// Открыть отчет из виджета "Исполнительская дисциплина".
    /// </summary>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <param name="period">Параметр "Период".</param>
    [Public]
    public virtual void OpenAssignmentCompletionReport(Enumeration performer, Enumeration period)
    {
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(period); 
      
      var periodEnd = Calendar.UserToday;
      
      var widgetParameter = Functions.Module.GetWidgetParameterLocalizedName(performer);
      
      if (performer == Shell.Widgets.AssignmentCompletionGraph.Performer.My)
      {
        Docflow.PublicFunctions.Module.OpenEmployeeAssignmentsReport(Users.Current.Id, periodBegin, periodEnd);
      }
      else if (performer == Shell.Widgets.AssignmentCompletionGraph.Performer.MyDepartments)
      {
        var departmentIds = Docflow.PublicFunctions.Module.Remote.GetManagersDepartments();
        
        Docflow.PublicFunctions.Module.OpenEmployeesAssignmentCompletionReport(null, null, new List<int>(), departmentIds, periodBegin, periodEnd, widgetParameter, true, false, true);
      }
      else if (performer == Shell.Widgets.AssignmentCompletionGraph.Performer.MyDirectDepts)
      {
        var departmentIds = Docflow.PublicFunctions.Module.Remote.GetManagersDepartments();
        Docflow.PublicFunctions.Module.OpenEmployeesAssignmentCompletionReport(null, null, new List<int>(), departmentIds, periodBegin, periodEnd, widgetParameter, false, false, true);
      }
      else if (performer == Shell.Widgets.AssignmentCompletionGraph.Performer.MyBusinessUnits)
      {
        var departmentIds = Docflow.PublicFunctions.Module.Remote.GetCEODepartments();
        Docflow.PublicFunctions.Module.OpenDepartmentsAssignmentCompletionReport(departmentIds, periodBegin, periodEnd, widgetParameter, true, false);
      }
      else if (performer == Shell.Widgets.AssignmentCompletionGraph.Performer.All)
      {
        Docflow.PublicFunctions.Module.OpenDepartmentsAssignmentCompletionReport(new List<int>(), periodBegin, periodEnd, string.Empty, true, false);
      }
    }
    
    /// <summary>
    /// Открыть отчет из виджетов "Исполнительская дисциплина сотрудников", "Сотрудники с высокой загрузкой".
    /// </summary>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="period">Параметр "Период".</param>
    [Public]
    public virtual void OpenReportFromEmployeeWidgets(int employeeId, Enumeration period)
    {
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(period);      
      Docflow.PublicFunctions.Module.OpenEmployeeAssignmentsReport(employeeId, periodBegin, Calendar.UserToday);
    }

    /// <summary>
    /// Открыть отчет из виджетов "Исполнительская дисциплина подразделений", "Подразделения с высокой загрузкой".
    /// </summary>
    /// <param name="departmentId">Ид подразделения.</param>
    /// <param name="departmentIds">Ид подразделений.</param>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <param name="period">Параметр "Период".</param>
    /// <param name="sortByAssignmentCompletion">Сортировать по исполнительской дисциплине.</param>
    [Public]
    public virtual void OpenReportFromDepartmentWidgets(int departmentId, List<int> departmentIds, Enumeration performer, Enumeration period, bool sortByAssignmentCompletion)
    {
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(period);
      
      var department = Company.PublicFunctions.Department.Remote.GetDepartment(departmentId);
      
      Docflow.PublicFunctions.Module.OpenEmployeesAssignmentCompletionReport(null, department, new List<int>(), departmentIds, periodBegin, Calendar.UserToday, null, false, false, sortByAssignmentCompletion);
    }
  }
}
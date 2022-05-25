using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Shell.Server
{

  partial class MyTodayAssignmentsWidgetHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignment> MyTodayAssignmentsChartFiltering(IQueryable<Sungero.Workflow.IAssignment> query, Sungero.Domain.WidgetPieChartFilteringEventArgs e)
    {
      return Functions.Module.GetMyAssignments(query, _parameters.Substitution, e.ValueId);
    }
    
    public virtual void GetMyTodayAssignmentsChartValue(Sungero.Domain.GetWidgetPieChartValueEventArgs e)
    {
      AccessRights.AllowRead(
        () =>
        {
          var assignments = Workflow.Assignments.GetAll();

          var overdue = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.OverdueToday).Count();
          if (overdue != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.OverdueToday, Resources.WidgetMTAOverdue, overdue, Colors.Charts.Red);

          var deadline = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.DeadlineToday).Count();
          if (deadline != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.DeadlineToday, Resources.WidgetMTADeadline, deadline, Colors.Charts.Color1);

          var after = Functions.Module.GetMyFutureAssignments(assignments, _parameters.Substitution);
          if (after != null && after.Count != 0)
            e.Chart.AddValue(after.ConstantName, after.Resource, after.Count, Colors.Charts.Color6);

          var completed = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.CompletedToday).Count();
          if (completed != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.CompletedToday, Resources.WidgetMTACompleted, completed, Colors.Charts.Green);
        });
    }
  }

  partial class AssignmentCompletionEmployeeGraphWidgetHandlers
  {

    public virtual string GetAssignmentCompletionEmployeeGraphOpenReportValue()
    {
      return Sungero.Shell.Resources.OpenReportHyperlink;
    }

    public virtual void GetAssignmentCompletionEmployeeGraphChartValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      if (!Equals(_parameters.Performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All) && Users.Current.IsSystem == true)
        return;
      
      if (Equals(_parameters.Performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All) && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
        return;
      
      e.Chart.IsLegendVisible = false;
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      var departmentIds = Functions.Module.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.Performer);
      var needFilter = !Equals(_parameters.Performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All);
      var items = Docflow.PublicFunctions.Module.GetDepartmentAssignmentCompletionReportData(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, needFilter);

      var employeeDisciplines = items
        .Where(d => d.AssignmentCompletion != null && d.IsActiveEmployee)
        .OrderBy(d => d.AssignmentCompletion)
        .Take(5)
        .Select(i => Sungero.Shell.Structures.Module.EmployeeDiscipline.Create(i.AssignmentCompletion, Employees.Get(i.Employee), i.OverdueAssignmentsCount))
        .ToList();
      
      var uniqueNamesEmployeeDiscipline = this.CreateEmployeeDisciplineUniqueName(employeeDisciplines);
      
      foreach (var employeeDiscipline in uniqueNamesEmployeeDiscipline)
      {
        var series = e.Chart.AddNewSeries(employeeDiscipline.EmployeeDiscipline.Employee.Id.ToString(), employeeDiscipline.UniqueName);
        series.DisplayValueFormat = "{0}%";
        series.AddValue(employeeDiscipline.EmployeeDiscipline.Employee.Id.ToString(), string.Empty, employeeDiscipline.EmployeeDiscipline.Discipline ?? 0);
      }
    }
    
    private List<Structures.Module.EmployeeDisciplineUniqueName> CreateEmployeeDisciplineUniqueName(List<Sungero.Shell.Structures.Module.EmployeeDiscipline> employeeDisciplines)
    {
      var result = new List<Structures.Module.EmployeeDisciplineUniqueName>();
      var employeeDisciplinesGroupByName = employeeDisciplines.GroupBy(e => e.Employee.Person.ShortName);
      
      foreach (var employeeDisciplinesGroup in employeeDisciplinesGroupByName)
      {
        if (employeeDisciplinesGroup.Count() < 1)
          continue;
        
        if (employeeDisciplinesGroup.Count() == 1)
        {
          var uniqueNameEmployee = new Structures.Module.EmployeeDisciplineUniqueName();
          
          uniqueNameEmployee.UniqueName = employeeDisciplinesGroup.Key;
          uniqueNameEmployee.EmployeeDiscipline = employeeDisciplinesGroup.FirstOrDefault();
          result.Add(uniqueNameEmployee);
        }
        else
        {
          var counter = 0;
          
          foreach (var employeeDiscipline in employeeDisciplinesGroup)
          {
            var uniqueName = employeeDiscipline.Employee.Person.ShortName;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var uniqueNameEmployee = new Structures.Module.EmployeeDisciplineUniqueName();
            
            uniqueNameEmployee.UniqueName = uniqueName;
            uniqueNameEmployee.EmployeeDiscipline = employeeDiscipline;
            
            result.Add(uniqueNameEmployee);
            counter++;
          }
        }
      }
      
      return result.OrderBy(e => e.EmployeeDiscipline.Discipline).ToList();
    }
  }

  partial class AssignmentCompletionDepartmentGraphWidgetHandlers
  {

    public virtual string GetAssignmentCompletionDepartmentGraphOpenReportValue()
    {
      return Sungero.Shell.Resources.OpenReportHyperlink;
    }

    public virtual void GetAssignmentCompletionDepartmentGraphChartValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      if (!Equals(_parameters.Performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.All) && Users.Current.IsSystem == true)
        return;
      
      if (Equals(_parameters.Performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.All) && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
        return;
      
      e.Chart.IsLegendVisible = false;
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      var departmentIds = Functions.Module.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.Performer);
      var needFilter = !Equals(_parameters.Performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.All);
      var items = Docflow.PublicFunctions.Module.GetBusinessUnitAssignmentCompletionWidgetData(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, needFilter);
      
      var seriesList = items
        .Where(d => d.AssignmentCompletion != null && d.IsActiveDepartment)
        .OrderBy(d => d.AssignmentCompletion)
        .Take(5)
        .Select(i => Structures.Module.DepartmentDiscipline.Create(i.AssignmentCompletion, Departments.Get(i.Department)))
        .ToList();
      
      var uniqueSeriesList = Functions.Module.SetUniqueDepartmentDisciplineNames(seriesList);
      foreach (var series in uniqueSeriesList)
      {
        var departmentSeries = e.Chart.AddNewSeries(series.DepartmentDiscipline.Department.Id.ToString(), series.UniqueName);
        departmentSeries.DisplayValueFormat = "{0}%";
        departmentSeries.AddValue(series.DepartmentDiscipline.Department.Id.ToString(), RecordManagement.Resources.AssignmentCompletion, series.DepartmentDiscipline.Discipline ?? 0);
      }
    }
  }

  partial class AssignmentCompletionGraphWidgetHandlers
  {

    public virtual void GetAssignmentCompletionGraphAllAssignmentChartValue(Sungero.Domain.GetWidgetGaugeChartValueEventArgs e)
    {
      if (!Equals(_parameters.Performer, Widgets.AssignmentCompletionGraph.Performer.All) && Users.Current.IsSystem == true)
        return;
      
      var isAdministratorOrAdvisor = Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      
      if (Equals(_parameters.Performer, Widgets.AssignmentCompletionGraph.Performer.All) && !isAdministratorOrAdvisor)
        return;
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      var departmentsIds = Functions.Module.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.Performer);
      var employee = Functions.Module.GetAssignmentCompletionUser(_parameters.Performer);
      var needFilter = !Equals(_parameters.Performer, Widgets.AssignmentCompletionGraph.Performer.All);
      
      var value = Docflow.PublicFunctions.Module.GetAssignmentCompletionReportData(businessUnitIds, departmentsIds, employee, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, needFilter);
      
      if (value == null)
        return;
      
      var color = Functions.Module.GetAssignmentCompletionWidgetValueColor(value.Value);
      
      e.Chart.AddValue(string.Format("{0}", Sungero.RecordManagement.Resources.AssignmentCompletion), value.Value, color);
    }
  }

  partial class ActiveAssignmentsDynamicWidgetHandlers
  {

    public virtual void GetActiveAssignmentsDynamicAssignmentsDynamicChartValue(Sungero.Domain.GetWidgetPlotChartValueEventArgs e)
    {
      if (!Equals(_parameters.CarriedObjects,  Widgets.ActiveAssignmentsDynamic.CarriedObjects.All) && Users.Current.IsSystem == true)
        return;
      
      var isAdministratorOrAdvisor = Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      
      if (Equals(_parameters.CarriedObjects,  Widgets.ActiveAssignmentsDynamic.CarriedObjects.All) && !isAdministratorOrAdvisor)
        return;
      
      var period = _parameters.Period == Widgets.ActiveAssignmentsDynamic.Period.Last90Days ? -90 :
        (_parameters.Period == Widgets.ActiveAssignmentsDynamic.Period.Last180Days ? -180 : -30);
      var periodEnd = Sungero.Core.Calendar.Today.EndOfDay();
      var periodBegin = Sungero.Core.Calendar.Today.AddDays(period).BeginningOfDay();
      
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.CarriedObjects);
      var departmentsIds = Functions.Module.GetWidgetDepartmentIds(_parameters.CarriedObjects);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.CarriedObjects);
      var employee = Functions.Module.GetAssignmentCompletionUser(_parameters.CarriedObjects);
      
      var needFilter = !Equals(_parameters.CarriedObjects, Widgets.ActiveAssignmentsDynamic.CarriedObjects.All);
      var points = Docflow.PublicFunctions.Module.GetActiveAssignmentsDynamicPoints(businessUnitIds, departmentsIds, employee, periodBegin, periodEnd, unwrap, needFilter);
      
      if (!points.Any())
        return;
      
      e.Chart.Axis.X.AxisType = AxisType.DateTime;
      e.Chart.Axis.Y.Title = Resources.WidgetActiveAssignmentsDynamicYAxisTitle;
      
      var assignmentsInWork = e.Chart.AddNewSeries(Resources.WidgetActiveAssignmentsDynamicSeriesAllTitle, Colors.Charts.Color1);
      var overdueAssignmentsInWork = e.Chart.AddNewSeries(Resources.WidgetActiveAssignmentsDynamicSeriesOverduedTitle, Colors.Charts.Red);
      
      var maxValue = points.Select(p => p.ActiveAssignmentsCount).Max();
      var minValue = points.Select(p => p.ActiveOverdueAssignmentsCount).Min();
      
      foreach (var point in points)
      {
        assignmentsInWork.AddValue(point.Date, point.ActiveAssignmentsCount);
        overdueAssignmentsInWork.AddValue(point.Date, point.ActiveOverdueAssignmentsCount);
      }
      
      // Dmitriev_IA: Ограничение оси Oy графика
      if (minValue > Math.Round(maxValue * 0.1))
        e.Chart.Axis.Y.MinValue = Math.Round(minValue * 0.95);
      else
        e.Chart.Axis.Y.MinValue = 0;
      
      e.Chart.Axis.Y.MaxValue = Math.Round(maxValue * 1.05);
    }
  }

  partial class TopLoadedDepartmentsGraphWidgetHandlers
  {

    public virtual string GetTopLoadedDepartmentsGraphOpenReportValue()
    {
      return Sungero.Shell.Resources.OpenReportHyperlink;
    }

    public virtual void GetTopLoadedDepartmentsGraphTopLoadedDepartmentsValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      if (!Equals(_parameters.Performer, Widgets.TopLoadedDepartmentsGraph.Performer.All) && Users.Current.IsSystem == true)
        return;
      
      if (Equals(_parameters.Performer, Widgets.TopLoadedDepartmentsGraph.Performer.All) && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
        return;
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      var departmentIds = Functions.Module.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.Performer);
      var needFilter = !Equals(_parameters.Performer, Widgets.TopLoadedDepartmentsGraph.Performer.All);
      var items = Docflow.PublicFunctions.Module.GetBusinessUnitAssignmentCompletionWidgetData(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, needFilter);
      
      var departmentLoads = items
        .Where(i => i.AssignmentCompletion != null && i.IsActiveDepartment)
        .OrderByDescending(i => i.AssignmentsCount)
        .Take(5)
        .Select(i => Structures.Module.DepartmentLoad.Create(Departments.Get(i.Department), i.AssignmentsCount, i.OverdueAssignmentsCount));
      
      var uniqueNameDepartmentLoads = Functions.Module.SetUniqueDepartmentNames(departmentLoads.ToList());
      
      foreach (var departmentLoad in uniqueNameDepartmentLoads)
      {
        var title = departmentLoad.UniqueName;
        var departmentSeries = e.Chart.AddNewSeries(departmentLoad.DepartmentLoad.Department.Id.ToString(), title);
        departmentSeries.AddValue(Constants.Module.OverduedAssignments,
                                  Resources.WithOverdue,
                                  departmentLoad.DepartmentLoad.OverduedAssignment, Colors.Charts.Red);
        departmentSeries.AddValue(Constants.Module.NotOverduedAssignments,
                                  Resources.WithoutOverdue,
                                  departmentLoad.DepartmentLoad.AllAssignment - departmentLoad.DepartmentLoad.OverduedAssignment, Colors.Charts.Green);
      }
    }
  }

  partial class TopLoadedPerformersGraphWidgetHandlers
  {

    public virtual string GetTopLoadedPerformersGraphOpenReportValue()
    {
      return Sungero.Shell.Resources.OpenReportHyperlink;
    }

    public virtual void GetTopLoadedPerformersGraphTopLoadedPerformersValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      if (!Equals(_parameters.CarriedObjects, Widgets.TopLoadedPerformersGraph.CarriedObjects.All) && Users.Current.IsSystem == true)
        return;
      
      if (Equals(_parameters.CarriedObjects, Widgets.TopLoadedPerformersGraph.CarriedObjects.All) && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
        return;
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.CarriedObjects);
      var departmentIds = Functions.Module.GetWidgetDepartmentIds(_parameters.CarriedObjects);
      var businessUnitIds = Functions.Module.GetWidgetBusinessUnitIds(_parameters.CarriedObjects);
      var needFilter = !Equals(_parameters.CarriedObjects, Widgets.TopLoadedPerformersGraph.CarriedObjects.All);
      
      var items = Docflow.PublicFunctions.Module.GetDepartmentAssignmentCompletionReportData(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, needFilter);
      
      var performerLoads = items
        .Where(i => i.AssignmentCompletion != null && i.IsActiveEmployee)
        .OrderByDescending(i => i.AssignmentsCount)
        .Take(5)
        .Select(i => Structures.Module.PerformerLoad.Create(Employees.Get(i.Employee), i.AssignmentsCount, i.OverdueAssignmentsCount));
      
      var uniqueNamedPerformerLoads = Functions.Module.SetUniquePerformerNames(performerLoads.ToList());
      
      foreach (var performerLoad in uniqueNamedPerformerLoads)
      {
        var performerSeries = e.Chart.AddNewSeries(performerLoad.PerformerLoad.Employee.Id.ToString(), performerLoad.UniqueName);
        performerSeries.AddValue(Constants.Module.OverduedAssignments,
                                 Resources.WithOverdue,
                                 performerLoad.PerformerLoad.OverduedAssignment, Colors.Charts.Red);
        performerSeries.AddValue(Constants.Module.NotOverduedAssignments,
                                 Resources.WithoutOverdue,
                                 performerLoad.PerformerLoad.AllAssignment - performerLoad.PerformerLoad.OverduedAssignment, Colors.Charts.Green);
      }
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Признак, учитывать ли подчиненные подразделения.
    /// </summary>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <returns>True, если учитывать подчиненные подразделения, иначе - false.</returns>
    public virtual bool NeedUnwrapWidgetDepartments(Enumeration performer)
    {
      return Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDepartments) ||
        Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyBusinessUnits) ||
        
        Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartments) ||
        Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyBusinessUnits) ||
        
        Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyDepartments) ||
        Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyBusinessUnits) ||
        
        Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyDepartments) ||
        Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyBusinessUnits) ||
        
        Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyDepartments) ||
        Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyBusinessUnits) ||
        
        Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyDepartments) ||
        Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyBusinessUnits);
    }
    
    /// <summary>
    /// Получить локализованное имя параметра-перечисления виджетов.
    /// </summary>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <returns>Локализованное имя параметра-перечисления виджетов.</returns>
    public virtual string GetWidgetParameterLocalizedName(Enumeration performer)
    {
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyDepartments))
        return Resources.WidgetParameterMyDepartments;
      
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyDirectDepts))
        return Resources.WidgetParameterPerformerMyDirectDepts;
      
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyBusinessUnits))
        return Resources.WidgetParameterPerformerMyBusinessUnits;
      
      // Для параметра "Все" не заполняем заголовок отчета.
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.All) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.All) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.All) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.All))
        return string.Empty;

      return null;
    }
    
    /// <summary>
    /// Получить дату начала периода для виджетов.
    /// </summary>
    /// <param name="period">Параметр "Период".</param>
    /// <returns>Дата начала периода.</returns>
    public virtual DateTime GetWidgetBeginPeriod(Enumeration period)
    {
      if (period == Widgets.TopLoadedPerformersGraph.Period.Last30Days ||
          period == Widgets.TopLoadedDepartmentsGraph.Period.Last30Days ||
          period == Widgets.AssignmentCompletionGraph.Period.Last30Days ||
          period == Widgets.AssignmentCompletionEmployeeGraph.Period.Last30Days ||
          period == Widgets.AssignmentCompletionDepartmentGraph.Period.Last30Days)
        return Calendar.UserToday.AddDays(-30);
      
      return Calendar.UserToday.AddDays(-90);
    } 
  }
}
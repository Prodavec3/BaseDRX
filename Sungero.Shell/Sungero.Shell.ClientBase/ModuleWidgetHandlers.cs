using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Client
{
  
  partial class AssignmentCompletionEmployeeGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionEmployeeGraphOpenReportAction()
    {
      var performerParameterIsAll = Equals(_parameters.Performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All);
      
      // Проверить наличие прав на отчёт и наличие данных для его формирования.
      if (!Docflow.Reports.GetDepartmentsAssignmentCompletionReport().CanExecute() ||
          (!performerParameterIsAll && Users.Current.IsSystem == true) ||
          (performerParameterIsAll && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor()))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      var businessUnitIds = Functions.Module.Remote.GetWidgetBusinessUnitIds(_parameters.Performer);
      var departmentIds = Functions.Module.Remote.GetWidgetDepartmentIds(_parameters.Performer);
      
      if (!Docflow.PublicFunctions.Module.Remote.DepartmentAssignmentCompletionReportDataExist(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, !performerParameterIsAll))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var widgetParameter = Functions.Module.GetWidgetParameterLocalizedName(_parameters.Performer);
      Docflow.PublicFunctions.Module.OpenEmployeesAssignmentCompletionReport(null, null, new List<int>(), departmentIds, periodBegin, Calendar.UserToday, widgetParameter, unwrap, false, true);
    }

    public virtual void ExecuteAssignmentCompletionEmployeeGraphChartAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int employeeId;
      if (!int.TryParse(e.ValueId, out employeeId))
      {
        Logger.ErrorFormat("ExecuteAssignmentCompletionEmployeeGraphChartAction. Failed parse employee id {0}", e.ValueId);
        return;
      }
      
      PublicFunctions.Module.OpenReportFromEmployeeWidgets(employeeId, _parameters.Period);
    }
  }

  partial class AssignmentCompletionDepartmentGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionDepartmentGraphOpenReportAction()
    {
      var performerParameterIsAll = Equals(_parameters.Performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.All);
      
      // Проверить наличие прав на отчёт и наличие данных для его формирования.
      if (!Docflow.Reports.GetDepartmentsAssignmentCompletionReport().CanExecute() ||
          (!performerParameterIsAll && Users.Current.IsSystem == true) ||
          (performerParameterIsAll && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor()))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var departmentIds = Functions.Module.Remote.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.Remote.GetWidgetBusinessUnitIds(_parameters.Performer);

      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      
      if (!Docflow.PublicFunctions.Module.Remote.BusinessUnitAssignmentCompletionWidgetDataExist(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, !performerParameterIsAll))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var widgetParameter = Functions.Module.GetWidgetParameterLocalizedName(_parameters.Performer);
      Docflow.PublicFunctions.Module.OpenDepartmentsAssignmentCompletionReport(departmentIds, periodBegin, Calendar.UserToday, widgetParameter, unwrap, false);
    }

    public virtual void ExecuteAssignmentCompletionDepartmentGraphChartAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int departmentId;
      if (!int.TryParse(e.SeriesId, out departmentId))
      {
        Logger.ErrorFormat("ExecuteAssignmentCompletionDepartmentGraphChartAction. Failed parse department id {0}", e.SeriesId);
        return;
      }
      
      PublicFunctions.Module.OpenReportFromDepartmentWidgets(departmentId, new List<int>() { departmentId }, _parameters.Performer, _parameters.Period, true);
    }
  }

  partial class AssignmentCompletionGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionGraphAllAssignmentChartAction()
    {
      PublicFunctions.Module.OpenAssignmentCompletionReport(_parameters.Performer, _parameters.Period);
    }
  }

  partial class TopLoadedDepartmentsGraphWidgetHandlers
  {

    public virtual void ExecuteTopLoadedDepartmentsGraphOpenReportAction()
    {
      var performerParameterIsAll = Equals(_parameters.Performer, Widgets.TopLoadedDepartmentsGraph.Performer.All);
      
      // Проверить наличие прав на отчёт и наличие данных для его формирования.
      if (!Docflow.Reports.GetDepartmentsAssignmentCompletionReport().CanExecute() ||
          (!performerParameterIsAll && Users.Current.IsSystem == true) ||
          (performerParameterIsAll && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor()))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var departmentIds = Functions.Module.Remote.GetWidgetDepartmentIds(_parameters.Performer);
      var businessUnitIds = Functions.Module.Remote.GetWidgetBusinessUnitIds(_parameters.Performer);
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.Performer);
      
      if (!Docflow.PublicFunctions.Module.Remote.BusinessUnitAssignmentCompletionWidgetDataExist(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, !performerParameterIsAll))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var widgetParameter = Functions.Module.GetWidgetParameterLocalizedName(_parameters.Performer);
      Docflow.PublicFunctions.Module.OpenDepartmentsAssignmentCompletionReport(departmentIds, periodBegin, Calendar.UserToday, widgetParameter, unwrap, false);
    }

    public virtual void ExecuteTopLoadedDepartmentsGraphTopLoadedDepartmentsAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int departmentId;
      if (!int.TryParse(e.SeriesId, out departmentId))
      {
        Logger.ErrorFormat("ExecuteTopLoadedDepartmentsGraphTopLoadedDepartmentsAction. Failed parse department id {0}", e.SeriesId);
        return;
      }
      
      PublicFunctions.Module.OpenReportFromDepartmentWidgets(departmentId, new List<int>() { departmentId },  _parameters.Performer, _parameters.Period, false);
    }
  }

  partial class TopLoadedPerformersGraphWidgetHandlers
  {

    public virtual void ExecuteTopLoadedPerformersGraphOpenReportAction()
    {
      var performerParameterIsAll = Equals(_parameters.CarriedObjects, Widgets.TopLoadedPerformersGraph.CarriedObjects.All);
      
      // Проверить наличие прав на отчёт и наличие данных для его формирования.
      if (!Docflow.Reports.GetDepartmentsAssignmentCompletionReport().CanExecute() ||
          (!performerParameterIsAll && Users.Current.IsSystem == true) ||
          (performerParameterIsAll && !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor()))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var periodBegin = Functions.Module.GetWidgetBeginPeriod(_parameters.Period);
      var unwrap = Functions.Module.NeedUnwrapWidgetDepartments(_parameters.CarriedObjects);
      var departmentIds = Functions.Module.Remote.GetWidgetDepartmentIds(_parameters.CarriedObjects);
      var businessUnitIds = Functions.Module.Remote.GetWidgetBusinessUnitIds(_parameters.CarriedObjects);
      
      if (!Docflow.PublicFunctions.Module.Remote.DepartmentAssignmentCompletionReportDataExist(businessUnitIds, departmentIds, periodBegin, Calendar.UserToday.EndOfDay(), unwrap, false, !performerParameterIsAll))
      {
        Dialogs.ShowMessage(Sungero.Shell.Resources.NoDataOrNoRightsToReport, MessageType.Error);
        return;
      }
      
      var widgetParameter = Functions.Module.GetWidgetParameterLocalizedName(_parameters.CarriedObjects);
      Docflow.PublicFunctions.Module.OpenEmployeesAssignmentCompletionReport(null, null, new List<int>(), departmentIds, periodBegin, Calendar.UserToday, widgetParameter, unwrap, false, false);
    }

    public virtual void ExecuteTopLoadedPerformersGraphTopLoadedPerformersAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int employeeId;
      if (!int.TryParse(e.SeriesId, out employeeId))
      {
        Logger.ErrorFormat("ExecuteTopLoadedPerformersGraphTopLoadedPerformersAction. Failed parse employee id {0}", e.SeriesId);
        return;
      }
      
      PublicFunctions.Module.OpenReportFromEmployeeWidgets(employeeId, _parameters.Period);
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.RecordManagement;
using Sungero.Shell.Structures.Module;
using Sungero.Workflow;

namespace Sungero.Shell.Server
{
  public class ModuleFunctions
  {
    #region Виджеты
    
    #region Топ загруженных
    
    /// <summary>
    /// Установить уникальность имен сотрудников.
    /// </summary>
    /// <param name="performerLoads">Загруженные сотрудники.</param>
    /// <returns>Загруженные сотрудники с уникальными именами, отсортированные по убыванию количества заданий.</returns>
    public static List<Shell.Structures.Module.PerformerLoadUniqueNames> SetUniquePerformerNames(List<Shell.Structures.Module.PerformerLoad> performerLoads)
    {
      var result = new List<Shell.Structures.Module.PerformerLoadUniqueNames>();
      var performerLoadsGroupByPersonName = performerLoads.GroupBy(pl => pl.Employee.Person.ShortName);
      
      foreach (var performerLoadsGroup in performerLoadsGroupByPersonName)
      {
        if (performerLoadsGroup.Count() < 1)
          continue;
        
        if (performerLoadsGroup.Count() == 1)
        {
          var uniqueNamePerformer = new Structures.Module.PerformerLoadUniqueNames();
          
          uniqueNamePerformer.UniqueName = performerLoadsGroup.Key;
          uniqueNamePerformer.PerformerLoad = performerLoadsGroup.FirstOrDefault();
          result.Add(uniqueNamePerformer);
        }
        else
        {
          var counter = 0;
          
          foreach (var performer in performerLoadsGroup)
          {
            var uniqueName = performer.Employee.Person.ShortName;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var uniqueNamePerformer = new Structures.Module.PerformerLoadUniqueNames();
            
            uniqueNamePerformer.UniqueName = uniqueName;
            uniqueNamePerformer.PerformerLoad = performer;
            
            result.Add(uniqueNamePerformer);
            counter++;
          }
        }
      }
      
      result = result.OrderByDescending(r => r.PerformerLoad.AllAssignment).ToList();
      
      return result;
    }
    
    /// <summary>
    /// Установить уникальность наименований подразделений.
    /// </summary>
    /// <param name="departmentLoad">Загруженные подразделения.</param>
    /// <returns>Загруженные подразделения с уникальными именами, отсортированные по убыванию количества заданий.</returns>
    public static List<Shell.Structures.Module.DepartmentLoadUniqueNames> SetUniqueDepartmentNames(List<Shell.Structures.Module.DepartmentLoad> departmentLoad)
    {
      var result = new List<Shell.Structures.Module.DepartmentLoadUniqueNames>();
      var departmentLoadsGroupByName = departmentLoad.GroupBy(d => (d.Department.BusinessUnit != null) ?
                                                              string.Format("{0} ({1})", d.Department.Name, d.Department.BusinessUnit.Name) :
                                                              string.Format("{0}", d.Department.Name));
      
      foreach (var departmentLoadsGroup in departmentLoadsGroupByName)
      {
        if (departmentLoadsGroup.Count() < 1)
          continue;
        
        if (departmentLoadsGroup.Count() == 1)
        {
          var uniqueName = new Structures.Module.DepartmentLoadUniqueNames();
          
          uniqueName.UniqueName = departmentLoadsGroup.Key;
          uniqueName.DepartmentLoad = departmentLoadsGroup.FirstOrDefault();
          result.Add(uniqueName);
        }
        else
        {
          var counter = 0;
          foreach (var department in departmentLoadsGroup)
          {
            var uniqueName = departmentLoadsGroup.Key;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var departmentLoadUnique = new Structures.Module.DepartmentLoadUniqueNames();
            
            departmentLoadUnique.UniqueName = uniqueName;
            departmentLoadUnique.DepartmentLoad = department;
            
            result.Add(departmentLoadUnique);
            counter++;
          }
        }
      }
      
      result = result.OrderByDescending(r => r.DepartmentLoad.AllAssignment).ToList();
      
      return result;
    }
    
    #endregion
    
    #region Цвета
    
    /// <summary>
    /// Получить цвет виджета.
    /// </summary>
    /// <param name="value">Процент исполнения заданий.</param>
    /// <returns>Цвет виджета.</returns>
    public static Sungero.Core.Color GetAssignmentCompletionWidgetValueColor(int value)
    {
      if (value <= 50)
        return Colors.Charts.Red;
      else if (value <= 75)
        return Colors.Charts.Yellow;
      else
        return Colors.Charts.Green;
    }
    
    #endregion
    
    #region Виджет "Исполнительская дисциплина"
   
    /// <summary>
    /// Получить Id заданий "Рассмотрение руководителем".
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <returns>Список Id заданий.</returns>
    [Public]
    public List<int> GetReviewTaskManagerAssignments(IQueryable<Workflow.IAssignmentBase> query)
    {
      return query.Where(a => ReviewManagerAssignments.Is(a)).Select(a => a.Id).ToList();
    }

    /// <summary>
    /// Получить список ид подразделений.
    /// </summary>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <returns>Список ид подразделений.</returns>
    [Remote(IsPure = true)]
    public virtual List<int> GetWidgetDepartmentIds(Enumeration performer)
    {
      var departmentsIds = new List<int>();
      var curentRecipients = Docflow.PublicFunctions.Module.GetCurrentRecipients(false);
      
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDirectDepts) ||
          Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyDirectDepts) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyDepartments) ||
          Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyDirectDepts) ||
          Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyDepartments))
        departmentsIds = Docflow.PublicFunctions.Module.GetManagersDepartments(curentRecipients);
      else if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyBusinessUnits) ||
               Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyBusinessUnits) ||
               Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyBusinessUnits) ||
               Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyBusinessUnits) ||
               Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyBusinessUnits) ||
               Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyBusinessUnits))
        departmentsIds = Docflow.PublicFunctions.Module.GetCEODepartments(curentRecipients);
      return departmentsIds;
    }
    
    /// <summary>
    /// Получить список ид подразделений.
    /// </summary>
    /// <param name="performer">Параметр "Сотрудники".</param>
    /// <returns>Список ид НОР.</returns>
    [Remote(IsPure = true)]
    public virtual List<int> GetWidgetBusinessUnitIds(Enumeration performer)
    {
      var businessUnitIds = new List<int>();
      var curentRecipients = Docflow.PublicFunctions.Module.GetCurrentRecipients(false);
      
      if (Equals(performer, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.TopLoadedDepartmentsGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.AssignmentCompletionDepartmentGraph.Performer.MyBusinessUnits) ||
          Equals(performer, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyBusinessUnits))
        businessUnitIds = Docflow.PublicFunctions.Module.GetCEOBusinessUnits(curentRecipients);
      return businessUnitIds;
    }
    
    /// <summary>
    /// Получить пользователя, по которому фильтруется виджет.
    /// </summary>
    /// <param name="performer">Параметр "Отображать" виджета "Исполнительская дисциплина".</param>
    /// <returns>Пользователь, по которому фильтруется виджет.</returns>
    public virtual IEmployee GetAssignmentCompletionUser(Enumeration performer)
    {
      var employee = Employees.Null;
      if (Equals(performer, Widgets.AssignmentCompletionGraph.Performer.My))
        employee = Employees.Current;
      return employee;
    }
    
    #endregion

    #region Виджет "Исполнительская дисциплина подразделений"
    
    /// <summary>
    /// Установить уникальность наименований подразделений.
    /// </summary>
    /// <param name="departmentDiscipline">Исполнительская дисциплина подразделения.</param>
    /// <returns>Загруженные подразделения с уникальными именами, отсортированные по возрастанию значения исп. дисциплины.</returns>
    public static List<Shell.Structures.Module.DepartmentDisciplineUniqueName> SetUniqueDepartmentDisciplineNames(List<Shell.Structures.Module.DepartmentDiscipline> departmentDiscipline)
    {
      var result = new List<Shell.Structures.Module.DepartmentDisciplineUniqueName>();
      var departmentGroupByName = departmentDiscipline.GroupBy(d => (d.Department.BusinessUnit != null) ?
                                                               string.Format("{0} ({1})", d.Department.Name, d.Department.BusinessUnit.Name) :
                                                               string.Format("{0}", d.Department.Name));
      
      foreach (var departmentGroup in departmentGroupByName)
      {
        if (departmentGroup.Count() < 1)
          continue;
        
        if (departmentGroup.Count() == 1)
        {
          var uniqueName = new Structures.Module.DepartmentDisciplineUniqueName();
          
          uniqueName.UniqueName = departmentGroup.Key;
          uniqueName.DepartmentDiscipline = departmentGroup.FirstOrDefault();
          result.Add(uniqueName);
        }
        else
        {
          var counter = 0;
          foreach (var department in departmentGroup)
          {
            var uniqueName = departmentGroup.Key;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var departmentUnique = new Structures.Module.DepartmentDisciplineUniqueName();
            
            departmentUnique.UniqueName = uniqueName;
            departmentUnique.DepartmentDiscipline = department;
            
            result.Add(departmentUnique);
            counter++;
          }
        }
      }
      
      result = result.OrderBy(r => r.DepartmentDiscipline.Discipline).ToList();
      
      return result;
    }

    #endregion
    
    #region "Мои задания на сегодня"
    
    /// <summary>
    /// Получить мои задания по фильтру.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <param name="value">Строковое обозначение серии.</param>
    /// <returns>Задания.</returns>
    public static IQueryable<IAssignment> GetMyAssignments(IQueryable<IAssignment> query, bool withSubstitution, string value)
    {
      query = Functions.Module.FilterMyTodayAssignments(query, withSubstitution);
      
      var userNow = Calendar.UserNow;
      var userBeginOfToday = userNow.BeginningOfDay();
      var userTodayEndOfWeek = userBeginOfToday.EndOfWeek().BeginningOfDay();
      var userTodayEndOfMonth = userBeginOfToday.EndOfMonth().BeginningOfDay();
      var userBeginOfDay = userBeginOfToday.AddMilliseconds(1);
      var userEndOfDay = userNow.EndOfDay();
      var userEndOfWeek = userNow.EndOfWeek();
      var userEndOfMonth = userNow.EndOfMonth();

      var serverNow = userNow.FromUserTime();
      var serverBeginOfToday = userBeginOfToday.FromUserTime();
      var serverTodayEndOfWeek = userTodayEndOfWeek.FromUserTime();
      var serverTodayEndOfMonth = userTodayEndOfMonth.FromUserTime();
      var serverBeginOfDay = userBeginOfDay.FromUserTime();
      var serverEndOfDay = userEndOfDay.FromUserTime();
      var serverEndOfWeek = userEndOfWeek.FromUserTime();
      var serverEndOfMonth = userEndOfMonth.FromUserTime();
      
      if (value == Constants.Module.TodayAssignments.CompletedToday)
      {
        query = query.Where(a => a.Completed.HasValue && a.Completed.Between(serverBeginOfDay, serverNow));
      }
      else
      {
        // Задания, которые ещё не выполнены.
        query = query.Where(a => a.Deadline.HasValue && Equals(a.Status, Workflow.AssignmentBase.Status.InProcess));
        
        if (value == Constants.Module.TodayAssignments.DeadlineToday)
        {
          query = query.Where(a => Equals(a.Deadline, serverBeginOfToday) ||
                              (a.Deadline.Between(serverNow, serverEndOfDay) && a.Deadline != a.Deadline.Value.Date));
        }
        else if (value == Constants.Module.TodayAssignments.OverdueToday)
        {
          query = query.Where(a => !Equals(a.Deadline, serverBeginOfToday) &&
                              (a.Deadline < serverNow && a.Deadline != a.Deadline.Value.Date ||
                               a.Deadline < serverEndOfDay.Date && a.Deadline == a.Deadline.Value.Date));
        }
        else
        {
          // Задания со сроком больше, чем сегодня.
          query = query.Where(a => (a.Deadline > serverBeginOfToday && a.Deadline == a.Deadline.Value.Date) ||
                              (a.Deadline > serverEndOfDay && a.Deadline != a.Deadline.Value.Date));
          
          if (value == Constants.Module.TodayAssignments.DeadlineTomorrow)
          {
            query = query.Where(a => Equals(a.Deadline, serverBeginOfToday.AddDays(1)) ||
                                (a.Deadline <= serverEndOfDay.AddDays(1) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.AfterTomorrow)
          {
            query = query.Where(a => Equals(a.Deadline, serverBeginOfToday.AddDays(2)) ||
                                (a.Deadline <= serverEndOfDay.AddDays(2) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.EndOfWeek)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfWeek && a.Deadline == a.Deadline.Value.Date) ||
                                (a.Deadline <= serverEndOfWeek && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.NextEndOfWeek)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfWeek.AddDays(7) && a.Deadline == a.Deadline.Value.Date) ||
                                (a.Deadline <= serverEndOfWeek.AddDays(7) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.EndOfMonth)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfMonth && a.Deadline == a.Deadline.Value.Date)
                                || (a.Deadline <= serverEndOfMonth && a.Deadline != a.Deadline.Value.Date));
          }
        }
      }
      
      return query;
    }
    
    /// <summary>
    /// Получить информацию по ближайшим заданиям.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <returns>Информация для графика.</returns>
    public static Structures.Module.AssignmentChartGroup GetMyFutureAssignments(IQueryable<Workflow.IAssignment> query, bool withSubstitution)
    {
      query = Functions.Module.FilterMyTodayAssignments(query, withSubstitution);
      
      var userNow = Calendar.UserNow;
      var userBeginOfToday = userNow.BeginningOfDay();
      var userTodayEndOfWeek = userBeginOfToday.EndOfWeek().BeginningOfDay();
      var userTodayEndOfMonth = userBeginOfToday.EndOfMonth().BeginningOfDay();
      var userEndOfDay = userNow.EndOfDay();
      var userEndOfWeek = userNow.EndOfWeek();
      var userEndOfMonth = userNow.EndOfMonth();

      var serverBeginOfToday = userBeginOfToday.FromUserTime();
      var serverTodayEndOfWeek = userTodayEndOfWeek.FromUserTime();
      var serverTodayEndOfMonth = userTodayEndOfMonth.FromUserTime();
      var serverEndOfDay = userEndOfDay.FromUserTime();
      var serverEndOfWeek = userEndOfWeek.FromUserTime();
      var serverEndOfMonth = userEndOfMonth.FromUserTime();
      
      // Задания, которые ещё не выполнены, со сроком больше, чем сегодня.
      query = query.Where(a => a.Deadline.HasValue && Equals(a.Status, Workflow.AssignmentBase.Status.InProcess)
                          && ((a.Deadline > serverBeginOfToday && a.Deadline == a.Deadline.Value.Date) ||
                              (a.Deadline > serverEndOfDay && a.Deadline != a.Deadline.Value.Date)));
      
      DateTime? firstDate = query.OrderBy(d => d.Deadline).Select(d => d.Deadline).FirstOrDefault();
      
      if (firstDate.HasValue)
      {
        var value = string.Empty;
        var text = string.Empty;
        var endOfMonth = serverEndOfMonth;
        if ((firstDate <= serverBeginOfToday.AddDays(1) && !firstDate.Value.HasTime()) ||
            (firstDate <= serverEndOfDay.AddDays(1) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.DeadlineTomorrow;
          text = Resources.WidgetMTATomorrow;
        }
        else if ((firstDate <= serverBeginOfToday.AddDays(2) && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfDay.AddDays(2) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.AfterTomorrow;
          text = Resources.WidgetMTAAfterTomorrow;
        }
        else if ((firstDate <= serverTodayEndOfWeek && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfWeek && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.EndOfWeek;
          text = Resources.WidgetMTAEndOfWeek;
        }
        else if ((firstDate <= serverTodayEndOfWeek.AddDays(7) && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfWeek.AddDays(7) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.NextEndOfWeek;
          text = Resources.WidgetMTANextEndOfWeek;
        }
        else if ((firstDate <= userTodayEndOfMonth && !firstDate.Value.HasTime()) ||
                 (firstDate <= endOfMonth && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.EndOfMonth;
          text = Resources.WidgetMTAEndOfMonth;
        }
        else
          return null;
        
        query = GetMyAssignments(query, withSubstitution, value);
        
        return Structures.Module.AssignmentChartGroup.Create(value, text, query.Count());
      }
      
      return null;
    }
    
    /// <summary>
    /// Отфильтровать задания по замещению и статусу.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <returns>Задания.</returns>
    public static IQueryable<IAssignment> FilterMyTodayAssignments(IQueryable<Workflow.IAssignment> query, bool withSubstitution)
    {
      query = query.Where(a => !Equals(a.Status, Workflow.AssignmentBase.Status.Aborted) && !Equals(a.Status, Workflow.AssignmentBase.Status.Suspended));
      
      if (withSubstitution)
      {
        var ids = Recipients.AllRecipientIds.ToList();
        query = query.Where(a => ids.Contains(a.Performer.Id));
      }
      else
      {
        query = query.Where(a => Equals(Users.Current, a.Performer));
      }
      return query;
    }
    
    #endregion
    
    #endregion
    
    /// <summary>
    /// Получить задания по типу этапа согласования, в том числе схлопнутые.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="stageType">Тип этапа согласования.</param>
    /// <returns>Задания.</returns>
    public IQueryable<Sungero.Workflow.IAssignmentBase> GetSpecificAssignmentsWithCollapsed(IQueryable<Sungero.Workflow.IAssignmentBase> query,
                                                                                            Enumeration stageType)
    {
      var needCheckSending = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Sending;
      var needCheckPrint = needCheckSending || stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Print;
      var needCheckRegister = needCheckPrint || stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Register;
      var isCheckExecution = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Execution;
      var needCheckExecution = needCheckRegister || isCheckExecution;
      var needCheckConfirmSign = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.ConfirmSign;
      var needCheckSign = needCheckExecution && !needCheckConfirmSign;
      var needCheckReview = needCheckSign;
      
      query = query.Where(q => needCheckReview && ApprovalReviewAssignments.Is(q) && ApprovalReviewAssignments.As(q).CollapsedStagesTypesRe.Any(s => s.StageType == stageType) ||
                          needCheckSign && ApprovalSigningAssignments.Is(q) && ApprovalSigningAssignments.As(q).CollapsedStagesTypesSig.Any(s => s.StageType == stageType) ||
                          needCheckConfirmSign && ApprovalSigningAssignments.Is(q) && ApprovalSigningAssignments.As(q).CollapsedStagesTypesSig.Any(s => s.StageType == stageType) &&
                          ApprovalSigningAssignments.As(q).IsConfirmSigning == true ||
                          needCheckExecution && (ApprovalExecutionAssignments.Is(q) && ApprovalExecutionAssignments.As(q).CollapsedStagesTypesExe.Any(s => s.StageType == stageType)) ||
                          needCheckRegister && ApprovalRegistrationAssignments.Is(q) && ApprovalRegistrationAssignments.As(q).CollapsedStagesTypesReg.Any(s => s.StageType == stageType) ||
                          needCheckPrint && ApprovalPrintingAssignments.Is(q) && ApprovalPrintingAssignments.As(q).CollapsedStagesTypesPr.Any(s => s.StageType == stageType) ||
                          needCheckSending && ApprovalSendingAssignments.Is(q) && ApprovalSendingAssignments.As(q).CollapsedStagesTypesSen.Any(s => s.StageType == stageType));
      
      return query;
    }
    
    /// <summary>
    /// Получить документы контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Документы.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<Sungero.Content.IElectronicDocument> GetDocumentsWithCounterparties(Sungero.Parties.ICounterparty counterparty)
    {
      return Sungero.Content.ElectronicDocuments.GetAll(d => IncomingDocumentBases.Is(d) && Equals(IncomingDocumentBases.As(d).Correspondent, counterparty) ||
                                                        OutgoingDocumentBases.Is(d) && OutgoingDocumentBases.As(d).Addressees.Select(x => x.Correspondent).Any(y => Equals(y, counterparty)) ||
                                                        AccountingDocumentBases.Is(d) && Equals(AccountingDocumentBases.As(d).Counterparty, counterparty) ||
                                                        ContractualDocumentBases.Is(d) && Equals(ContractualDocumentBases.As(d).Counterparty, counterparty) ||
                                                        ExchangeDocuments.Is(d) && Equals(ExchangeDocuments.As(d).Counterparty, counterparty));
    }
    
    /// <summary>
    /// Получить документы сотрудника, в которых он является ответственным.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Документы.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<Sungero.Content.IElectronicDocument> GetRespondingEmployeeDocuments(IEmployee employee)
    {
      var query = OfficialDocuments.GetAll(d => AccountingDocumentBases.Is(d) || ContractualDocuments.Is(d))
        .Where(d => ((Equals(AccountingDocumentBases.As(d).ResponsibleEmployee, employee) ||
                      Equals(ContractualDocuments.As(d).ResponsibleEmployee, employee)) &&
                     (d.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft ||
                      d.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Active)) ||
               ContractualDocuments.As(d).Milestones.Any(m => Equals(m.Performer, employee) && m.IsCompleted != true) ||
               d.Tracking.Any(t => Equals(t.DeliveredTo, employee) &&
                              t.IsOriginal == true &&
                              t.Action != Docflow.OfficialDocumentTracking.Action.Sending &&
                              (t.ReturnResult == null || t.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.AtControl)));
      
      return query;
    }
    
  }
}
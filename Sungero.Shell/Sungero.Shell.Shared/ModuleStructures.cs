using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Structures.Module
{
  
  /// <summary>
  /// Загруженность подразделения.
  /// </summary>
  partial class DepartmentLoad
  {
    public Sungero.Company.IDepartment Department { get; set; }
    
    public int AllAssignment { get; set; }
    
    public int OverduedAssignment { get; set; }
  }

  /// <summary>
  /// Загруженность сотрудника.
  /// </summary>
  partial class PerformerLoad
  {
    public Sungero.Company.IEmployee Employee { get; set; }
    
    public int AllAssignment { get; set; }
    
    public int OverduedAssignment { get; set; }
  }

  /// <summary>
  /// Облегченное задание. Минимально необходимое количество полей для расчета просрочки.
  /// </summary>
  partial class LightAssignment
  {
    public int Id { get; set; }
    
    /// <summary>
    /// Статус задания.
    /// </summary>
    public Sungero.Core.Enumeration? Status { get; set; }
    
    /// <summary>
    /// Срок задания.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Дата изменения задания.
    /// </summary>
    public DateTime? Modified { get; set; }
    
    /// <summary>
    /// Дата создания задания.
    /// </summary>
    public DateTime? Created { get; set; }
    
    /// <summary>
    /// Ид исполнителя.
    /// </summary>
    public int PerformerId { get; set; }
    
    /// <summary>
    /// Фактическая просрочка с учетом 4-х часов.
    /// </summary>
    public DateTime? FactDeadline { get; set; }

    /// <summary>
    /// Дата выполнения задания.
    /// </summary>
    public DateTime? Completed { get; set; }
  }
  
  /// <summary>
  /// Уникальное имя для загрузки сотрудника.
  /// </summary>
  partial class PerformerLoadUniqueNames
  {
    public string UniqueName { get; set; }
    
    public Sungero.Shell.Structures.Module.PerformerLoad PerformerLoad { get; set; }
  }

  /// <summary>
  /// Уникальное имя для загрузки подразделения.
  /// </summary>
  partial class DepartmentLoadUniqueNames
  {
    public string UniqueName { get; set; }
    
    public Sungero.Shell.Structures.Module.DepartmentLoad DepartmentLoad { get; set; }
  }

  /// <summary>
  /// Исполнительская дисциплина подразделения.
  /// </summary>
  partial class DepartmentDiscipline
  {
    public int? Discipline { get; set; }
    
    public Sungero.Company.IDepartment Department { get; set; }
  }
  
  /// <summary>
  /// Исполнительская дисциплина сотрудников.
  /// </summary>
  partial class EmployeeDiscipline
  {
    public int? Discipline { get; set; }
    
    public Sungero.Company.IEmployee Employee { get; set; }
    
    public int OverdueAsg { get; set; }
  }
  
  /// <summary>
  /// Уникальное имя для загрузки сотрудника.
  /// </summary>
  partial class EmployeeDisciplineUniqueName
  {
    public string UniqueName { get; set; }
    
    public Sungero.Shell.Structures.Module.EmployeeDiscipline EmployeeDiscipline { get; set; }
  }
  
  /// <summary>
  /// Уникальное имя для загрузки подразделений.
  /// </summary>
  partial class DepartmentDisciplineUniqueName
  {
    public string UniqueName { get; set; }
    
    public Sungero.Shell.Structures.Module.DepartmentDiscipline DepartmentDiscipline { get; set; }
  }
  
  /// <summary>
  /// Группировка данных с названием группы (TodayAssignments) и количеством элементов.
  /// </summary>
  partial class AssignmentChartGroup
  {
    public string ConstantName { get; set; }
    
    public string Resource { get; set; }
    
    public int Count { get; set; }
  }
  
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalRuleBase
{
  
  partial class StageWithUnsupportedRole
  {
    public Sungero.Docflow.IApprovalRuleBaseStages Stage { get; set; }
    
    public Sungero.Core.Enumeration Role { get; set; }
  }
  
  /// <summary>
  /// Список всех возможных последовательностей блоков.
  /// </summary>
  partial class StagesVariants
  {
    public List<List<int>> AllSteps { get; set; }
    
    public List<int> UnreachebleSteps { get; set; }
  }
  
  /// <summary>
  /// Список этапов и недопустимых ролей в них.
  /// </summary>
  partial class StagesIncorrectRoles
  {
    public IApprovalRuleBaseStages Stage { get; set; }
    
    public string Message { get; set; }
  }
  
  /// <summary>
  /// Исполнители блока.
  /// </summary>
  partial class BlockPerformers
  {
    public List<Sungero.Company.IEmployee> Employees { get; set; }
    
    public List<IRecipient> Recipient { get; set; }
    
    public string Message { get; set; }
  }

  /// <summary>
  /// Номер следующего этапа.
  /// </summary>
  partial class NextStageNumber
  {
    public int? Number { get; set; }
    
    public string Message { get; set; }
  }

  /// <summary>
  /// Этап маршрута.
  /// </summary>
  partial class RouteStep
  {
    public int StepNumber { get; set; }
    
    public bool Branch { get; set; }
  }
  
    /// <summary>
  /// Этап маршрута.
  /// </summary>
  partial class ConditionRouteStep
  {
    public Sungero.Docflow.Structures.ApprovalRuleBase.RouteStep RouteStep { get; set; }
    
    public Sungero.Core.Enumeration? ConditionType { get; set; }
  }
  
  /// <summary>
  /// Информация по этапу регламента.
  /// </summary>
  partial class StageStatusInfo
  {
    public bool IsLast { get; set; }
    
    public bool InProcess { get; set; }
    
    public bool IsNext { get; set; }
  }
}
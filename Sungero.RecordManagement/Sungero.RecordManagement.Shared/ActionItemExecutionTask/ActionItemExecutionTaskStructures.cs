using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.ActionItemExecutionTask
{

  /// <summary>
  /// Информация в шапке диалога корректировки.
  /// </summary>
  [Public]
  partial class ChangeDialogInfo
  {
    // Заголовок диалога.
    public string DialogTitle { get; set; }
    
    // Текст диалога.
    public string DialogText { get; set; }
  }
  
  /// <summary>
  /// Модель контрола состояния задачи на исполнение поручения: кэш статусов, задачи, задания.
  /// </summary>
  [Public]
  partial class StateViewModel
  {
    
    public System.Collections.Generic.Dictionary<Sungero.Core.Enumeration?, string> StatusesCache { get; set; }
    
    public List<IActionItemExecutionTask> Tasks { get; set; }
    
    public List<Sungero.Workflow.IAssignment> Assignments { get; set; }
    
  }
  
  /// <summary>
  /// Изменения поручения.
  /// </summary>
  [Public]
  partial class ActionItemChanges
  {
    /// <summary>
    /// Старый контролер.
    /// </summary>
    public IEmployee OldSupervisor { get; set; }
    
    /// <summary>
    /// Новый контролер.
    /// </summary>
    public IEmployee NewSupervisor { get; set; }
    
    /// <summary>
    /// Старый исполнитель.
    /// </summary>
    public IEmployee OldAssignee { get; set; }
    
    /// <summary>
    /// Новый исполнитель.
    /// </summary>
    public IEmployee NewAssignee { get; set; }
    
    /// <summary>
    /// Старый срок.
    /// </summary>
    public DateTime? OldDeadline { get; set; }

    /// <summary>
    /// Новый срок.
    /// </summary>
    public DateTime? NewDeadline { get; set; }
    
    /// <summary>
    /// Старые соисполнители.
    /// </summary>
    public List<IEmployee> OldCoAssignees { get; set; }
    
    /// <summary>
    /// Новые соисполнители.
    /// </summary>
    public List<IEmployee> NewCoAssignees { get; set; }
    
    /// <summary>
    /// Старый срок соисполнителей.
    /// </summary>
    public DateTime? CoAssigneesOldDeadline { get; set; }

    /// <summary>
    /// Новый срок соисполнителей.
    /// </summary>
    public DateTime? CoAssigneesNewDeadline { get; set; }
    
    /// <summary>
    /// Причина корректировки.
    /// </summary>
    public string EditingReason { get; set; }
    
    /// <summary>
    /// Дополнительная информация для использования в перекрытиях.
    /// </summary>
    public string AdditionalInfo { get; set; }
    
    /// <summary>
    /// Список ИД задач (пунктов составного) для корректировки.
    /// </summary>
    public List<int> TaskIds { get; set; }
    
    /// <summary>
    /// Текстовое представление выбранных пунктов поручения.
    /// </summary>
    public string ActionItemPartsText { get; set; }
    
    /// <summary>
    /// Пользователь, корректирующий поручение.
    /// </summary>
    public IUser InitiatorOfChange { get; set; }
    
    /// <summary>
    /// Контекст вызова корректировки (simple - простое; compound - массовая в составном; part - пункта в составном).
    /// </summary>
    public string ChangeContext { get; set; }
  }
}
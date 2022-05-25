using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.RecordManagement.Shared
{
  public class ModuleFunctions
  {
    #region Параметры модуля
    
    /// <summary>
    /// Получить параметры модуля.
    /// </summary>
    /// <returns>Параметры модуля.</returns>
    [Public]
    public virtual IRecordManagementSetting GetSettings()
    {
      return RecordManagementSettings.GetAllCached().SingleOrDefault();
    }
    
    /// <summary>
    /// Разрешены ли бессрочные поручения.
    /// </summary>
    /// <returns>true, если разрешены ли бессрочные поручения.</returns>
    [Public]
    public virtual bool AllowActionItemsWithIndefiniteDeadline()
    {
      return this.GetSettings().AllowActionItemsWithIndefiniteDeadline == true;
    }
    
    #endregion
    
    #region статус "Исполнение"
    
    /// <summary>
    /// Получить приоритеты для статусов исполнения.
    /// </summary>
    /// <returns>Словарь с приоритетами статусов исполнения.</returns>
    [Public]
    public virtual System.Collections.Generic.IDictionary<Enumeration?, int> GetExecutionStatePriorities()
    {
      var priorities = new Dictionary<Enumeration?, int>();
      priorities.Add(ExecutionState.OnExecution, 110);
      priorities.Add(ExecutionState.Sending, 100);
      priorities.Add(ExecutionState.OnReview, 90);
      priorities.Add(ExecutionState.Executed, 80);
      priorities.Add(ExecutionState.WithoutExecut, 70);
      priorities.Add(ExecutionState.Aborted, 0);
      return priorities;
    }
    
    #endregion

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ReviewDraftResolutionAssignment;

namespace Sungero.RecordManagement.Server
{
  partial class ReviewDraftResolutionAssignmentFunctions
  {
    /// <summary>
    /// Построить модель представления.
    /// </summary>
    /// <returns>Xml представление контрола состояние.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      var skipResolutionBlock = false;
      
      var statusesCache = new Dictionary<Enumeration?, string>();
      foreach (var task in _obj.ResolutionGroup.ActionItemExecutionTasks)
      {
        var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
        stateViewModel.StatusesCache = statusesCache;
        var blocks = PublicFunctions.ActionItemExecutionTask.GetActionItemExecutionTaskStateView(task, task, stateViewModel, null, skipResolutionBlock, false).Blocks;
        statusesCache = stateViewModel.StatusesCache;
        
        // Убираем первый блок с текстовой информацией по поручению.
        foreach (var block in blocks.Skip(1))
          stateView.AddBlock(block);
        
        // Строим блок резолюции только для первого поручения.
        skipResolutionBlock = true;
      }
      return stateView;
    }
  }
}
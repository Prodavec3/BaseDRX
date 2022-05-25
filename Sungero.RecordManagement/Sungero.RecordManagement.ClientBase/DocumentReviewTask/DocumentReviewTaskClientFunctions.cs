using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Client
{
  partial class DocumentReviewTaskFunctions
  {
    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <param name="parentAssignment">Задание, от которого создается поручение.</param>
    /// <param name="mainTask">Задача "Рассмотрение входящего", из которой создается поручение.</param>
    /// <param name="resolutionText">Текст резолюции.</param>
    /// <returns>Поручение.</returns>
    public static IActionItemExecutionTask CreateActionItemExecution(IAssignment parentAssignment, IDocumentReviewTask mainTask, string resolutionText)
    {
      var document = mainTask.DocumentForReviewGroup.OfficialDocuments.First();
      // TODO вернуть вызов с использованием задания, когда починят 24898.
      // var task = Functions.Module.Remote.CreateActionItemExecution(document, parentAssignment);
      var task = Functions.Module.Remote.CreateActionItemExecution(document, (int)parentAssignment.Id);
      task.ActiveText = resolutionText;
      return task;
    }
    
    /// <summary>
    /// Проверить просроченные поручения, вывести ошибку в случае просрочки.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    public virtual void CheckOverdueActionItemExecutionTasks(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var overdueTasks = Functions.DocumentReviewTask.GetDraftOverdueActionItemExecutionTasks(_obj);
      if (overdueTasks.Any())
      {
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanTodayCorrectIt);
        e.Cancel();
      }
    }
    
    /// <summary>
    /// Проверить, что текущий сотрудник может готовить проект резолюции.
    /// </summary>
    /// <returns>True, если сотрудник может готовить проект резолюции, иначе - False.</returns>
    public virtual bool CanPrepareDraftResolution()
    {
      var canPrepareResolution = false;
      var formParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      if (formParams.ContainsKey(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName))
      {
        object paramValue;
        formParams.TryGetValue(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName, out paramValue);
        bool.TryParse(paramValue.ToString(), out canPrepareResolution);
        return canPrepareResolution;
      }
      
      if (Company.Employees.Current != null)
        canPrepareResolution = Company.PublicFunctions.Employee.Remote.CanPrepareDraftResolution(Company.Employees.Current);
      formParams.Add(PublicConstants.DocumentReviewTask.CanPrepareDraftResolutionParamName, canPrepareResolution);
      return canPrepareResolution;
    }
  }
}
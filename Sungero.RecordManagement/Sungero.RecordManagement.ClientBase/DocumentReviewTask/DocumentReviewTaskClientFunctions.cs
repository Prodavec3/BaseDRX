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
    [Obsolete("Используйте метод Functions.Module.Remote.CreateActionItemExecutionWithResolution.")]
    public static IActionItemExecutionTask CreateActionItemExecution(IAssignment parentAssignment, IDocumentReviewTask mainTask, string resolutionText)
    {
      var document = mainTask.DocumentForReviewGroup.OfficialDocuments.First();
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
    
    /// <summary>
    /// Добавить проект резолюции.
    /// </summary>
    [Public]
    public virtual void AddResolution()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var task = document == null ? Functions.Module.Remote.CreateActionItemExecution() : Functions.Module.Remote.CreateActionItemExecution(document);
      var assignee = task.Assignee ?? Users.Current;
      task.MaxDeadline = _obj.Deadline ?? Calendar.Today.AddWorkingDays(assignee, 2);
      task.IsDraftResolution = true;
      foreach (var otherGroupAttachment in _obj.OtherGroup.All)
        task.OtherGroup.All.Add(otherGroupAttachment);
      task.ShowModal();
      if (!task.State.IsInserted)
      {
        var draftActionItem = Functions.Module.Remote.GetActionitemById(task.Id);
        _obj.ResolutionGroup.ActionItemExecutionTasks.Add(draftActionItem);
        _obj.Save();
      }
    }
    
    /// <summary>
    /// Открыть отчёт "Проект резолюции" для последующей печати.
    /// </summary>
    /// <param name="resolutionText">Текст резолюции.</param>
    /// <param name="actionItems">Поручения.</param>
    public virtual void OpenDraftResolutionReport(string resolutionText, List<IActionItemExecutionTask> actionItems)
    {
      var report = RecordManagement.Reports.GetDraftResolutionReport();
      report.Resolution.AddRange(actionItems);
      report.TextResolution = resolutionText;
      report.Document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      report.Author = _obj.Addressee;
      report.Open();
    }
    
    /// <summary>
    /// Проверить, что текущая задача стартована в рамках согласования по регламенту.
    /// </summary>
    /// <param name="task">Задача на рассмотрение.</param>
    /// <returns>True, если задача на рассмотрение была запущена из согласования по регламенту, иначе - False.</returns>
    public static bool ReviewStartedFromApproval(ITask task)
    {
      return Docflow.ApprovalTasks.Is(task.MainTask);
    }
    
    /// <summary>
    /// Подтвердить удаление проектов резолюции из текущей задачи.
    /// </summary>
    /// <param name="message">Текст диалога подтверждения удаления.</param>
    /// <param name="description">Описание диалога подтверждения удаления.</param>
    /// <param name="dialogId">ИД диалога подтверждения удаления.</param>
    /// <returns>True, если удаление было подтверждено, иначе - False.</returns>
    public virtual bool ShowDeletingDraftResolutionsConfirmationDialog(string message, string description, string dialogId)
    {
      var dropIsConfirmed = Docflow.PublicFunctions.Module.ShowConfirmationDialog(message,
                                                                                  description,
                                                                                  null, dialogId);
      if (dropIsConfirmed)
        _obj.NeedDeleteActionItems = true;
      
      return dropIsConfirmed;
    }
    
  }
}
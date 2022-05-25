using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationTask;
using Sungero.Workflow;

namespace Sungero.SmartProcessing.Server
{
  partial class VerificationTaskFunctions
  {
    #region Предметное отображение "Задачи"
    
    /// <summary>
    /// Получить модель контрола состояния задачи на верификацию.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния задачи на верификацию.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.AllAttachments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Получить модель контрола состояния задачи на верификацию.
    /// </summary>
    /// <returns>Модель контрола состояния задачи на верификацию.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      
      // Добавить блок информации к блоку задачи.
      var taskHeader = VerificationTasks.Resources.StateViewTaskBlockHeader;
      this.AddInformationBlock(stateView, taskHeader, _obj.Started.Value);
      
      // Блок информации о задаче.
      var taskBlock = this.AddTaskBlock(stateView);
      
      // Получить все задания по задаче.
      var taskAssignments = VerificationAssignments.GetAll(a => Equals(a.Task, _obj)).OrderBy(a => a.Created).ToList();
      
      // Статус задачи.
      var status = _obj.Info.Properties.Status.GetLocalizedValue(_obj.Status);
      
      var lastAssignment = taskAssignments.OrderByDescending(a => a.Created).FirstOrDefault();
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status);
      
      // Блоки информации о заданиях.
      foreach (var assignment in taskAssignments)
      {
        var assignmentBlock = this.GetAssignmentBlock(assignment);
        
        taskBlock.AddChildBlock(assignmentBlock);
      }
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить блок информации о действии.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="text">Текст действия.</param>
    /// <param name="date">Дата действия.</param>
    [Public]
    public void AddInformationBlock(object stateView, string text, DateTime date)
    {
      // Создать блок с пояснением.
      var block = (stateView as StateView).AddBlock();
      block.Entity = _obj;
      block.DockType = DockType.Bottom;
      block.ShowBorder = false;
      
      // Иконка.
      block.AssignIcon(VerificationTasks.Resources.VerificationDocumentIcon, StateBlockIconSize.Small);
      
      // Текст блока.
      block.AddLabel(text);
      var style = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDate.ToString(),
                                   Docflow.PublicFunctions.Module.ToShortDateShortTime(date.ToUserTime())), style);
    }
    
    /// <summary>
    /// Добавить блок задачи на верификацию.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock AddTaskBlock(Sungero.Core.StateView stateView)
    {
      // Создать блок задачи.
      var taskBlock = stateView.AddBlock();
      
      // Добавить ссылку на задачу и иконку.
      taskBlock.Entity = _obj;
      taskBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      
      // Определить схлопнутость.
      taskBlock.IsExpanded = _obj.Status == Workflow.Task.Status.InProcess;
      taskBlock.AddLabel(Resources.VerificationTaskThreadSubject, Sungero.Docflow.PublicFunctions.Module.CreateHeaderStyle());
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить блок задания на верификацию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Новый блок.</returns>
    public Sungero.Core.StateBlock GetAssignmentBlock(IAssignment assignment)
    {
      // Стили.
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var boldStyle = Docflow.PublicFunctions.Module.CreateStyle(true, false);
      var grayStyle = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      var noteStyle = Docflow.PublicFunctions.Module.CreateNoteStyle();
      
      var block = StateView.Create().AddBlock();
      block.Entity = assignment;
      
      // Иконка.
      this.SetIcon(block, VerificationAssignments.As(assignment));
      
      // Заголовок.
      block.AddLabel(VerificationTasks.Resources.StateViewAssignmentBlockHeader, boldStyle);
      block.AddLineBreak();
      
      // Кому.
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(Company.Employees.As(assignment.Performer), false);
      var performerInfo = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewTo, assigneeShortName);
      block.AddLabel(performerInfo, performerDeadlineStyle);
      
      // Срок.
      if (assignment.Deadline.HasValue)
      {
        var deadlineLabel = Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime());
        block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineLabel), performerDeadlineStyle);
      }
      
      // Текст задания.
      var comment = Docflow.PublicFunctions.Module.GetAssignmentUserComment(Assignments.As(assignment));
      if (!string.IsNullOrWhiteSpace(comment))
      {
        // Разделитель.
        block.AddLineBreak();
        block.AddLabel(Docflow.PublicFunctions.Module.GetSeparatorText(), separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.PublicFunctions.Module.GetEmptyLineMargin());
        
        block.AddLabel(comment, noteStyle);
      }
      
      // Статус.
      var status = AssignmentBases.Info.Properties.Status.GetLocalizedValue(assignment.Status);
      
      // Для непрочитанных заданий указать это.
      if (assignment.IsRead == false)
        status = Docflow.ApprovalTasks.Resources.StateViewUnRead.ToString();
      
      // Для исполненных заданий указать результат, с которым они исполнены, кроме "Проверено".
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed
          && assignment.Result != SmartProcessing.VerificationAssignment.Result.Complete)
        status = SmartProcessing.VerificationAssignments.Info.Properties.Result.GetLocalizedValue(assignment.Result);
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(block, status);
      
      // Задержка исполнения.
      if (assignment.Deadline.HasValue &&
          assignment.Status == Workflow.AssignmentBase.Status.InProcess)
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      
      return block;
    }
    
    /// <summary>
    /// Установить иконку.
    /// </summary>
    /// <param name="block">Блок, для которого требуется установить иконку.</param>
    /// <param name="assignment">Задание, от которого построен блок.</param>
    private void SetIcon(StateBlock block, IVerificationAssignment assignment)
    {
      var iconSize = StateBlockIconSize.Large;
      
      // Иконка по умолчанию.
      block.AssignIcon(StateBlockIconType.OfEntity, iconSize);

      // Прекращено, остановлено по ошибке.
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return;
      }
      
      if (assignment.Result == null)
        return;
      
      // Проверено.
      if (assignment.Result == SmartProcessing.VerificationAssignment.Result.Complete)
      {
        block.AssignIcon(StateBlockIconType.Completed, iconSize);
        return;
      }
      
      // Переадресовано.
      if (assignment.Result == SmartProcessing.VerificationAssignment.Result.Forward)
      {
        block.AssignIcon(Sungero.Docflow.FreeApprovalTasks.Resources.Forward, iconSize);
      }
    }
    
    #endregion
    
    #region Запрос подготовки предпросмотра
    
    /// <summary>
    /// Отправить запрос на конвертацию документов в сервис предпросмотра.
    /// </summary>
    /// <param name="documents">Документы.</param>
    [Obsolete("Используйте метод Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(ITask)")]
    public virtual void PreparePreview(List<Docflow.IOfficialDocument> documents)
    {
      Docflow.PublicFunctions.Module.PrepareAllAttachmentsPreviews(_obj);
    }
    
    #endregion
  }
}
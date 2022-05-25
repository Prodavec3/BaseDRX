using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentProcessingTask;
using Sungero.Workflow;

namespace Sungero.Exchange.Server
{
  partial class ExchangeDocumentProcessingTaskFunctions
  {
    #region Предметное отображение "Задачи"
    
    /// <summary>
    /// Построить модель контрола состояния документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Модель контрола состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.AllAttachments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить модель контрола состояния.
    /// </summary>
    /// <returns>Модель контрола состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();
      
      // Добавить блок информации к блоку задачи.
      var taskHeader = ExchangeDocumentProcessingTasks.Resources.StateViewInformationBlockFormat(ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(_obj.Box));
      this.AddInformationBlock(stateView, taskHeader, _obj.Started.Value);
      
      // Блок информации о задаче.
      var taskBlock = this.AddTaskBlock(stateView);
      
      // Получить все задания по задаче.
      var taskAssignments = ExchangeDocumentProcessingAssignments.GetAll(a => Equals(a.Task, _obj)).OrderBy(a => a.Created).ToList();
      
      // Статус задачи.
      var status = _obj.Info.Properties.Status.GetLocalizedValue(_obj.Status);
      
      var lastAssignment = taskAssignments.OrderByDescending(a => a.Created).FirstOrDefault();
      // Установить статус отказать, если обработка завершилась с этим результатом.
      if (lastAssignment.Result == Exchange.ExchangeDocumentProcessingAssignment.Result.Abort)
        status = lastAssignment.Info.Properties.Result.GetLocalizedValue(lastAssignment.Result);
      
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
      block.AssignIcon(ExchangeDocumentProcessingTasks.Resources.ExchangeDocumentIcon, StateBlockIconSize.Small);
      
      // Текст блока.
      block.AddLabel(text);
      var style = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDate.ToString(),
                                   Docflow.PublicFunctions.Module.ToShortDateShortTime(date.ToUserTime())), style);
    }
    
    /// <summary>
    /// Добавить блок задачи на обработку входящих документов из сервиса обмена.
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
      taskBlock.AddLabel(ExchangeDocumentProcessingTasks.Resources.StateViewTaskBlockHeader, Sungero.Docflow.PublicFunctions.Module.CreateHeaderStyle());
      taskBlock.AddLineBreak();
      
      // Добавить отправителя.
      var sender = _obj.Counterparty;
      taskBlock.AddLabel(ExchangeDocumentProcessingTasks.Resources.StateViewSender, Sungero.Docflow.PublicFunctions.Module.CreateNoteStyle());
      taskBlock.AddHyperlink(sender.Name, Hyperlinks.Get(sender));
      taskBlock.AddLineBreak();
      
      // Добавить получателя.
      var receiver = _obj.Box;
      taskBlock.AddLabel(ExchangeDocumentProcessingTasks.Resources.StateViewRecipient, Sungero.Docflow.PublicFunctions.Module.CreateNoteStyle());
      taskBlock.AddHyperlink(receiver.Name, Hyperlinks.Get(receiver));
      
      return taskBlock;
    }
    
    /// <summary>
    /// Добавить блок задания на обработку входящих документов из сервиса обмена.
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
      this.SetIcon(block, ExchangeDocumentProcessingAssignments.As(assignment));
      
      // Заголовок.
      block.AddLabel(ExchangeDocumentProcessingTasks.Resources.StateViewAssignmentBlockHeader, boldStyle);
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
      
      // Для исполненных заданий указать результат, с которым они исполнены, кроме "Обработано".
      if (assignment.Status == Workflow.AssignmentBase.Status.Completed
          && assignment.Result != Exchange.ExchangeDocumentProcessingAssignment.Result.Complete)
        status = Exchange.ExchangeDocumentProcessingAssignments.Info.Properties.Result.GetLocalizedValue(assignment.Result);
      
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
    private void SetIcon(StateBlock block, IExchangeDocumentProcessingAssignment assignment)
    {
      var iconSize = StateBlockIconSize.Large;
      
      // Иконка по умолчанию.
      block.AssignIcon(StateBlockIconType.OfEntity, iconSize);

      // Прекращено, остановлено по ошибке.
      if (assignment.Status == Workflow.AssignmentBase.Status.Aborted ||
          assignment.Status == Workflow.AssignmentBase.Status.Suspended ||
          assignment.Result == Exchange.ExchangeDocumentProcessingAssignment.Result.Abort)
      {
        block.AssignIcon(StateBlockIconType.Abort, iconSize);
        return;
      }
      
      if (assignment.Result == null)
        return;
      
      if (assignment.Result == Exchange.ExchangeDocumentProcessingAssignment.Result.Complete ||
          assignment.Result == Exchange.ExchangeDocumentProcessingAssignment.Result.ReAddress)
      {
        block.AssignIcon(StateBlockIconType.Completed, iconSize);
        return;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Проверить, что все документы отправлены в работу.
    /// </summary>
    /// <returns>True, если все документы в работе, иначе - false.</returns>
    [Remote]
    public bool AreAllDocumentsSendToWork()
    {
      foreach (Sungero.Docflow.IOfficialDocument document in _obj.NeedSigning.All)
      {
        var docTypeGuid = Guid.Parse(document.DocumentKind.DocumentType.DocumentTypeGuid);
        if (!Sungero.Workflow.Tasks
            .GetAll(t => !ExchangeDocumentProcessingTasks.Is(t))
            .Any(t => t.AttachmentDetails.Any(att => att.AttachmentId == document.Id &&
                                              att.EntityTypeGuid == docTypeGuid)))
          return false;
      }
      return true;
    }
  }
}
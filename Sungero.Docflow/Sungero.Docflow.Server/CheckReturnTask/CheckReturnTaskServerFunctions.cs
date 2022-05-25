using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CheckReturnTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class CheckReturnTaskFunctions
  {
    #region Предметное отображение "Задачи". Общие функции
    
    /// <summary>
    /// Получить блок задания модели контрола состояния задачи на контроль возврата.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Модели контрола состояния задачи на контроль возврата.</returns>
    [Public]
    public static Sungero.Core.StateBlock GetAssignmentBlock(IAssignmentBase assignment)
    {
      // Стили.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle();
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      // var boldStyle = Docflow.PublicFunctions.Module.CreateStyle(true, false);
      var grayStyle = Docflow.PublicFunctions.Module.CreateStyle(false, true);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      var block = StateView.Create().AddBlock();
      block.Entity = assignment;
      
      // Заголовок.
      block.AddLabel("Обработка входящего документа", headerStyle);
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
      
      // Разделитель.
      block.AddLineBreak();
      block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
      block.AddLineBreak();
      block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
      
      // Текст задания.
      block.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(assignment.ActiveText), grayStyle);

      // Статус.
      var status = AssignmentBases.Info.Properties.Status.GetLocalizedValue(assignment.Status);
      
      // Для непрочитанных заданий указать это.
      if (assignment.IsRead == false)
        status = Docflow.ApprovalTasks.Resources.StateViewUnRead.ToString();
      
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(block, status);
      
      // Задержка исполнения.
      if (assignment.Deadline.HasValue &&
          (assignment.Status == Workflow.AssignmentBase.Status.InProcess ||
           assignment.Status == Workflow.AssignmentBase.Status.Completed))
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
      
      return block;
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить результат возврата.
    /// </summary>
    /// <param name="returnControl">Задача.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="documentIsReturned">Признак возврата документа.</param>
    public static void SetReturnResult(Sungero.Docflow.ICheckReturnTask returnControl, IUser performer, bool documentIsReturned)
    {
      var document = returnControl.DocumentGroup.OfficialDocuments.First();
      var tracking = GetTrackingByTask(returnControl);
      if (tracking == null)
        return;
      
      if (documentIsReturned)
      {
        var resultReturned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;
        if (tracking.ReturnResult != resultReturned)
        {
          var accessRights = document.AccessRights;
          tracking.ReturnResult = (accessRights.CanUpdate(performer) && accessRights.CanRegister(performer)) ?
            resultReturned : Docflow.OfficialDocumentTracking.ReturnResult.AtControl;
        }
      }
      else
      {
        tracking.ReturnResult = null;
        tracking.ReturnDate = null;
      }
    }
    
    /// <summary>
    /// Получить строку выдачи.
    /// </summary>
    /// <param name="returnControl">Задача.</param>
    /// <returns>Коллекция выдачи.</returns>
    public static IOfficialDocumentTracking GetTrackingByTask(Sungero.Docflow.ICheckReturnTask returnControl)
    {
      var document = returnControl.DocumentGroup.OfficialDocuments.First();
      return document.Tracking.Where(r => Equals(r.ReturnTask, returnControl)).FirstOrDefault();
    }

  }
}
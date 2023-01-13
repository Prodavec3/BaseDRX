using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata.Services;
using Sungero.Reporting.Client;
using Sungero.Reporting.Shared;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Показать настройки текущего пользователя.
    /// </summary>
    public static void ShowCurrentPersonalSettings()
    {
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSettings != null)
        personalSettings.Show();
      else
        Dialogs.ShowMessage(Resources.FailedGetSettings, MessageType.Error);
    }
    
    /// <summary>
    /// Перегрузка открытия отчет "Контроль исполнения поручений" из виджета за указанный месяц.
    /// </summary>
    /// <param name="currentPeriod">Дата.</param>
    /// <param name="performerParam">Исполнитель, указанный в параметрах виджета.</param>
    public static void ShowActionItemsExecutionReport(DateTime currentPeriod, Enumeration performerParam)
    {
      // Текущий период.
      var periodBegin = currentPeriod.BeginningOfMonth();
      var periodEnd = currentPeriod.EndOfMonth();
      
      var report = RecordManagement.Reports.GetActionItemsExecutionReport();
      report.BeginDate = periodBegin;
      report.EndDate = periodEnd.EndOfDay();
      report.ClientEndDate = periodEnd;

      if (performerParam == RecordManagement.Widgets.ActionItemCompletionGraph.Performer.Author)
        report.Author = Company.Employees.Current;
      
      report.Open();
    }
    
    /// <summary>
    /// Диалог с запросом параметров для отчетов по журналам регистрации.
    /// </summary>
    /// <param name="reportName">Наименование отчета.</param>
    /// <param name="direction">Направление документопотока журнала.</param>
    /// <param name="documentRegisterValue">Журнал.</param>
    /// <param name="helpCode">Код справки.</param>
    /// <returns>Возвращает структуру формата - запустить отчет, дата начала, дата окончания, журнал.</returns>
    public static Structures.Module.DocumentRegisterReportParametrs ShowDocumentRegisterReportDialog(string reportName, Enumeration direction,
                                                                                                     IDocumentRegister documentRegisterValue,
                                                                                                     string helpCode)
    {
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Employees.Current);
      var dialog = Dialogs.CreateInputDialog(reportName);
      dialog.HelpCode = helpCode;

      var settingsBeginDate = Docflow.PublicFunctions.PersonalSetting.GetStartDate(personalSettings);
      var beginDate = dialog.AddDate(Resources.StartDate, true, settingsBeginDate ?? Calendar.UserToday);
      var settingsEndDate = Docflow.PublicFunctions.PersonalSetting.GetEndDate(personalSettings);
      var endDate = dialog.AddDate(Resources.EndDate, true, settingsEndDate ?? Calendar.UserToday);
      
      INavigationDialogValue<IDocumentRegister> documentRegister = null;
      if (documentRegisterValue == null)
      {
        var documentRegisters = Functions.Module.Remote.GetFilteredDocumentRegistersForReport(direction);
        IDocumentRegister defaultDocumentRegister = null;
        if (personalSettings != null)
        {
          if (direction == Docflow.DocumentRegister.DocumentFlow.Incoming)
            defaultDocumentRegister = personalSettings.IncomingDocRegister;
          else if (direction == Docflow.DocumentRegister.DocumentFlow.Outgoing)
            defaultDocumentRegister = personalSettings.OutgoingDocRegister;
          else
            defaultDocumentRegister = personalSettings.InnerDocRegister;
        }
        
        if (documentRegisters.Count == 1)
          documentRegister = dialog.AddSelect(Docflow.Resources.DocumentRegister, true, documentRegisters.FirstOrDefault()).From(documentRegisters);
        else
          documentRegister = dialog.AddSelect(Docflow.Resources.DocumentRegister, true, defaultDocumentRegister).From(documentRegisters);
      }
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
      {
        if (documentRegisterValue == null)
          documentRegisterValue = documentRegister.Value;
        return Structures.Module.DocumentRegisterReportParametrs.Create(true, beginDate.Value, endDate.Value, documentRegisterValue);
      }
      else
      {
        documentRegisterValue = null;
        return Structures.Module.DocumentRegisterReportParametrs.Create(false, null, null, null);
      }
    }

    /// <summary>
    /// Показать диалог подтверждения выполнения без создания поручений.
    /// </summary>
    /// <param name="assignment">Задание, которое выполняется.</param>
    /// <param name="document">Документ.</param>
    /// <param name="e">Аргументы.</param>
    /// <returns>True, если диалог был, иначе false.</returns>
    public static bool ShowConfirmationDialogCreationActionItem(IAssignment assignment, IOfficialDocument document, Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var reviewTask = DocumentReviewTasks.As(assignment.Task);
      var hasSubActionItem = Functions.ActionItemExecutionTask.Remote.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess, reviewTask.Addressee);
      
      if (hasSubActionItem)
        return false;
      
      var dialogText = ReviewResolutionAssignments.Is(assignment) ?
        Docflow.Resources.ExecuteWithoutCreatingActionItemFromAddressee : Docflow.Resources.ExecuteWithoutCreatingActionItem;
      var dialog = Dialogs.CreateTaskDialog(dialogText, MessageType.Question);
      dialog.Buttons.AddYes();
      dialog.Buttons.Default = DialogButtons.Yes;
      var createActionItemButton = dialog.Buttons.AddCustom(Docflow.Resources.CreateActionItem);
      dialog.Buttons.AddNo();
      
      var result = dialog.Show();
      if (result == DialogButtons.Yes)
        return true;
      
      if (result == DialogButtons.Cancel || result == DialogButtons.No)
        e.Cancel();
      
      assignment.Save();
      var assignedBy = reviewTask.Addressee;
      var resolution = ReviewResolutionAssignments.Is(assignment) ? ReviewResolutionAssignments.As(assignment).ResolutionText : assignment.ActiveText;
      var actionItem = ActionItemExecutionTasks.As(Functions.Module.Remote.CreateActionItemExecutionWithResolution(document, assignment.Id, resolution, assignedBy));
      if (actionItem != null)
      {
        actionItem.IsDraftResolution = false;
        actionItem.WaitForParentAssignment = true;
        actionItem.ShowModal();
      }
      
      hasSubActionItem = Functions.ActionItemExecutionTask.Remote.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess, reviewTask.Addressee);
      if (hasSubActionItem)
        return true;
      
      var hasDraftSubActionItem = Functions.ActionItemExecutionTask.Remote.HasSubActionItems(assignment.Task, Workflow.Task.Status.Draft, reviewTask.Addressee);
      e.AddError(hasDraftSubActionItem ? Docflow.Resources.AllCreatedActionItemsShouldBeStarted : Resources.NeedCreatedActionItemExecutionFromAdressee);
      e.Cancel();
      return true;
    }
    
    /// <summary>
    /// Показать диалог подтверждения прекращения подчиненных поручений.
    /// </summary>
    /// <param name="assignment">Ведущее поручение.</param>
    /// <returns>True, если запрос был подтвержден.
    /// False, если была нажата отмена.</returns>
    [Public, Obsolete("Используйте метод ShowAbortSubActionItemsConfirmationDialog")]
    public virtual bool ShowAbortConfirmationDialog(IActionItemExecutionAssignment assignment)
    {
      return this.ShowAbortSubActionItemsConfirmationDialog(assignment, null);
    }
    
    /// <summary>
    /// Показать диалог подтверждения прекращения подчиненных поручений.
    /// </summary>
    /// <param name="assignment">Ведущее поручение.</param>
    /// <param name="notCompletedExecutionSubTasks">Список невыполненных подчиненных поручений.</param>
    /// <returns>True, если запрос был подтвержден.
    /// False, если была нажата отмена.</returns>
    [Public]
    public virtual bool ShowAbortSubActionItemsConfirmationDialog(IActionItemExecutionAssignment assignment, List<IActionItemExecutionTask> notCompletedExecutionSubTasks)
    {
      if (assignment == null)
        return false;
      
      var dialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.StopAdditionalActionItemExecutions,
                                            MessageType.Question);
      
      Action showNotCompletedExecutionSubTasksHandler = () =>
      {
        notCompletedExecutionSubTasks.ShowModal();
      };
      
      var showNotCompletedExecutionSubTasks = dialog.AddHyperlink(ActionItemExecutionTasks.Resources.NotCompletedExecutionSubTasksHyperlinkTitle);
      showNotCompletedExecutionSubTasks.SetOnExecute(showNotCompletedExecutionSubTasksHandler);
      showNotCompletedExecutionSubTasks.IsVisible = notCompletedExecutionSubTasks.Any();
      
      var notAbort = dialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.NotAbort);
      dialog.Buttons.Default = notAbort;
      var abort = dialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.Abort);
      dialog.Buttons.AddCancel();
      var dialogResult = dialog.Show();
      
      if (dialogResult == notAbort)
        return true;
      
      if (dialogResult == abort)
      {
        assignment.NeedAbortChildActionItems = true;
        return true;
      }
      
      if (dialogResult == DialogButtons.Cancel)
        return false;
      
      return false;
    }
    
    /// <summary>
    /// Показать диалог подтверждения выполнения ведущего поручения.
    /// </summary>
    /// <param name="supervisorAssignment">Задание на приемку в рамках подчиненного поручения.</param>
    /// <param name="parentAssignment">Ведущее поручение.</param>
    /// <returns>True, если запрос был подтвержден.
    /// False, если была нажата отмена.</returns>
    public virtual bool ShowCompleteParentActionItemConfirmationDialog(IActionItemSupervisorAssignment supervisorAssignment, IActionItemExecutionAssignment parentAssignment)
    {
      var resources = Sungero.RecordManagement.ActionItemSupervisorAssignments.Resources;
      var dialog = Dialogs.CreateTaskDialog(resources.CompleteParentAssignmentDialogTitle, MessageType.Question);
      
      Action showParentAssignmentHandler = () =>
      {
        parentAssignment.ShowModal();
      };
      
      var showParentAssignment = dialog.AddHyperlink(resources.ShowParentAssignmentHyperlinkTitle);
      showParentAssignment.SetOnExecute(showParentAssignmentHandler);
      showParentAssignment.IsVisible = parentAssignment != null;
      
      var complete = dialog.Buttons.AddCustom(resources.CompleteParentAssignmentDialogCompleteButtonTitle);
      var notComplete = dialog.Buttons.AddCustom(resources.CompleteParentAssignmentDialogNotCompleteButtonTitle);
      var open = dialog.Buttons.AddCustom(resources.CompleteParentAssignmentDialogOpenButtonTitle);
      dialog.Buttons.AddCancel();
      dialog.Buttons.Default = complete;
      
      var dialogResult = dialog.Show();
      
      var task = ActionItemExecutionTasks.As(supervisorAssignment.Task);
      if (dialogResult == complete)
      {
        // Проверить наличие подчиненных поручений.
        var otherNotCompletedActionItemExecutionSubTasks = Functions.ActionItemExecutionTask.Remote.GetOtherNotCompletedActionItemExecutionSubTasks(task);
        if (otherNotCompletedActionItemExecutionSubTasks.Any())
          if (!Functions.Module.ShowAbortSubActionItemsConfirmationDialog(parentAssignment, otherNotCompletedActionItemExecutionSubTasks))
            return false;
        
        // Сохранить ведущее задание, чтобы установился признак необходимости прекращения подчиненных поручений.
        parentAssignment.Save();
        // Выполнить ведущее задание на исполнение асинхронно.
        Functions.Module.Remote.CompleteParentActionItemExecutionAssignmentAsync(task.Id, parentAssignment.Id, parentAssignment.TaskStartId);
        return true;
      }
      
      if (dialogResult == notComplete)
        return true;
      
      if (dialogResult == open)
      {
        Functions.ActionItemExecutionTask.Remote.SynchronizeResultGroup(task);
        parentAssignment.Show();
        return true;
      }
      
      if (dialogResult == DialogButtons.Cancel)
        return false;
      
      return false;
    }
    
    /// <summary>
    /// Показать диалог подтверждения выполнения.
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <param name="description">Дополнительный текст.</param>
    /// <param name="title">Заголовок.</param>
    /// <param name="parentActionItemExecutionAssignment">Ведущее поручение.</param>
    /// <returns>True, если запрос был подтвержден.</returns>
    public virtual bool ShowConfirmationDialog(string text, string description, string title,
                                               RecordManagement.IActionItemExecutionAssignment parentActionItemExecutionAssignment)
    {
      var dialog = Dialogs.CreateTaskDialog(text, description, MessageType.Question, title);
      dialog.Buttons.AddYesNo();
      
      Action showParentAssignmentHandler = () =>
      {
        parentActionItemExecutionAssignment.ShowModal();
      };
      
      var showParentAssignment = dialog.AddHyperlink(RecordManagement.ActionItemExecutionTasks.Resources.ShowParentAssignment);
      showParentAssignment.SetOnExecute(showParentAssignmentHandler);
      showParentAssignment.IsVisible = parentActionItemExecutionAssignment != null;
      
      var dialogResult = dialog.Show();
      
      if (dialogResult == DialogButtons.Yes)
        return true;
      
      if (dialogResult == DialogButtons.No)
        return false;
      
      return false;
    }
    
    /// <summary>
    /// Паблик обертка для получения отчета.
    /// </summary>
    /// <returns>Отчет.</returns>
    [Public]
    public Sungero.Reporting.IReport GetOutgoingDocumentsReport()
    {
      return Reports.GetOutgoingDocumentsReport();
    }
    
    /// <summary>
    /// Паблик обертка для получения отчета.
    /// </summary>
    /// <returns>Отчет.</returns>
    [Public]
    public Sungero.Reporting.IReport GetInternalDocumentsReport()
    {
      return Reports.GetInternalDocumentsReport();
    }
    
    /// <summary>
    /// Паблик обертка для получения отчета.
    /// </summary>
    /// <returns>Отчет.</returns>
    [Public]
    public Sungero.Reporting.IReport GetIncomingDocumentsReport()
    {
      return Reports.GetIncomingDocumentsReport();
    }
    
    /// <summary>
    /// Паблик обертка для получения отчета.
    /// </summary>
    /// <returns>Отчет.</returns>
    [Public]
    public Sungero.Reporting.IReport GetIncomingDocumentsProcessingReport()
    {
      return Reports.GetIncomingDocumentsProcessingReport();
    }
    
    /// <summary>
    /// Паблик обертка для получения отчета.
    /// </summary>
    /// <returns>Отчет.</returns>
    [Public]
    public Sungero.Reporting.IReport GetDocumentReturnReport()
    {
      return Reports.GetDocumentReturnReport();
    }
    
    /// <summary>
    /// Получить отчет "Контроль ознакомления".
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Отчет.</returns>
    [Public]
    public virtual Sungero.Reporting.IReport GetAcquaintanceReport(IAcquaintanceTask task)
    {
      var report = Reports.GetAcquaintanceReport();
      report.Task = task;
      return report;
    }
    
    /// <summary>
    /// Получить отчет "Бланк ознакомления".
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Отчет.</returns>
    [Public]
    public virtual Sungero.Reporting.IReport GetAcquaintanceFormReport(IAcquaintanceTask task)
    {
      var report = Reports.GetAcquaintanceFormReport();
      report.Task = task;
      return report;
    }
    
    /// <summary>
    /// Получить отчет "Контроль ознакомления".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Отчет.</returns>
    [Public]
    public virtual Sungero.Reporting.IReport GetAcquaintanceReport(IOfficialDocument document)
    {
      var report = Reports.GetAcquaintanceReport();
      report.Document = document;
      return report;
    }
  }
}
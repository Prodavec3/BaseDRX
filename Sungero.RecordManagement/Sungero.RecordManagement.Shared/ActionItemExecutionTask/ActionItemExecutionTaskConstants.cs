using System;

namespace Sungero.RecordManagement.Constants
{
  public static class ActionItemExecutionTask
  {
    public const string WorkingWithGUI = "Working with GUI";
    public const string HasIndefiniteDeadline = "HasIndefiniteDeadline";
    public const string HasIndefiniteDeadlineIsEnabled = "HasIndefiniteDeadlineIsEnabled";
    
    [Sungero.Core.Public]
    public const int MaxCompoundGroup = 100;
    
    public const int MaxActionItemAssignee = 250;
    
    /// <summary>
    /// Период проверки завершения составного поручения в часах.
    /// </summary>
    public const int CheckCompletionMonitoringPeriodInHours = 8;
    
    [Sungero.Core.Public]
    public const string CheckDeadline = "CheckDeadline";
    
    // Параметр "Запись в историю информации об изменении срока исполнителя или срока соисполнителей".
    [Sungero.Core.Public]
    public const string ChangeDeadlinesWriteInHistoryParamName = "ChangeDeadlinesWriteInHistory";
    
    // Параметр "Запись в историю информации об изменении, если менялись только сроки".
    [Sungero.Core.Public]
    public const string ChangeOnlyDeadlinesWriteInHistoryParamName = "ChangeOnlyDeadlinesWriteInHistory";
    
    public static class Operation
    {
      /// <summary>
      /// Изменение срока.
      /// </summary>
      public const string ChangeDeadline = "ChangeDeadline";
    }
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задачи на исполнение поручения.
    /// </summary>
    public const string ActionItemExecutionTaskConfirmDialogID = "1116c237-c585-4186-a7e1-ba81db27e420";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на исполнение поручения.
    /// </summary>
    public const string ActionItemExecutionAssignmentConfirmDialogID = "bd3560c8-dee0-4fce-a17f-005b170e7835";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на приемку работ контролером.
    /// </summary>
    public static class ActionItemSupervisorAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Принято".
      /// </summary>
      public const string Agree = "a70db196-fe4d-4d49-a0aa-b75821b9a03f";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "f27aad24-144e-4326-ad3c-30034c0c6f56";
    }
    
    /// <summary>
    /// Код диалога добавления пункта поручения.
    /// </summary>
    public const string AddActionItemHelpCode = "Sungero_AddActionItemDialog";
    
    /// <summary>
    /// Код диалога редактирования пункта поручения.
    /// </summary>
    public const string EditActionItemHelpCode = "Sungero_EditActionItemDialog";
    
    /// <summary>
    /// Код диалога корректировки пункта поручения.
    /// </summary>
    public const string ActionItemHelpCode = "Sungero_ChangeActionItemDialog";
    
    public const int ControlRelativeDeadline = 1;
    
    /// <summary>
    /// Ограничение длины короткого представления пункта корректировки.
    /// </summary>
    public const int ActionItemPartTextMaxLength = 58;
    
    /// <summary>
    /// Варианты контекста вызова корректировки поручения.
    /// </summary>
    public static class ChangeContext
    {
      public const string Simple = "Simple";
      public const string Compound = "Compound";
      public const string Part = "Part";
    }
    
    /// <summary>
    /// ИД группы приложений.
    /// </summary>
    public static readonly Guid AddendaGroupGuid = Guid.Parse("d44a8df5-3fe9-4a1b-a5a0-e8aaa65820da");

  }
}
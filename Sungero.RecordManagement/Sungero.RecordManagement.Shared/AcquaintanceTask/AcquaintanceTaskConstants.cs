using System;
using Sungero.Core;

namespace Sungero.RecordManagement.Constants
{
  public static class AcquaintanceTask
  {

    /// <summary>
    /// Ограничение числа участников ознакомления для списка ознакомления.
    /// </summary>
    public const int PerformersLimit = 1000;
    
    /// <summary>
    /// Наименование параметра "Ограничение числа участников ознакомления".
    /// </summary>
    public const string PerformersLimitParamName = "AcquaintanceTaskPerformersLimit";
    
    /// <summary>
    /// Ограничение числа участников ознакомления для задачи.
    /// Используется для первоначального заполнения параметра AcquaintanceTaskPerformersLimit в sungero_docflow_params.
    /// </summary>
    public const string DefaultPerformersLimit = "2000";
    
    /// <summary>
    /// ИД диалога подтверждения при старте задачи.
    /// </summary>
    public const string StartConfirmDialogID = "68527B87-6A68-4795-8803-5D4210970090";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на ознакомление с результатом "Ознакомлен".
    /// </summary>
    public const string AcquaintedConfirmDialogID = "7967EE35-E6D2-49BE-A586-D41DBD5B3CEB";
    
    /// <summary>
    /// Код диалога исключения участников из ознакомления.
    /// </summary>
    public const string ExcludeFromAcquaintanceHelpCode = "Sungero_ExcludeFromAcquaintanceDialog";
    
    /// <summary>
    /// ИД группы приложений.
    /// </summary>
    public static readonly Guid AddendaGroupGuid = Guid.Parse("a9f0df39-6287-42dd-a325-849fe22412af");
  }
}
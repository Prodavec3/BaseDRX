using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Client;
using Sungero.Reporting;
using Sungero.Workflow;

namespace Sungero.Docflow.Client
{
  public class ModuleFunctions
  {
    #region Замещение
    
    /// <summary>
    /// Имеет ли доступ по замещению.
    /// </summary>
    /// <param name="e">Аргументы карточки.</param>
    /// <param name="registrationGroup">Группа регистрации.</param>
    /// <param name="calculateIsSubstitute">Признак замещения.</param>
    /// <param name="calculateIsAdministrator">Признак администратора.</param>
    /// <param name="calculateIsUsed">Признак использования.</param>
    /// <param name="calculateHasDocuments">Признак наличия зарегистрированных документов.</param>
    /// <param name="documentRegister">Журнал.</param>
    /// <returns>True, если доступ имеется.</returns>
    public static bool CalculateParams(Sungero.Presentation.FormRefreshEventArgs e, IRegistrationGroup registrationGroup,
                                       bool calculateIsSubstitute, bool calculateIsAdministrator, bool calculateIsUsed, bool calculateHasDocuments,
                                       IDocumentRegister documentRegister)
    {
      var isSubstituteParamName = Constants.Module.IsSubstituteResponsibleEmployeeParamName;
      var isAdministratorParamName = Constants.Module.IsAdministratorParamName;
      var isUsedParamName = Constants.Module.IsUsedParamName;
      var hasDocumentsParamName = Constants.Module.HasRegisteredDocumentsParamName;
      bool isSubstituteParamValue;
      bool isAdministratorParamValue;
      var isSubstituteParamHasValue = e.Params.TryGetValue(isSubstituteParamName, out isSubstituteParamValue);
      var isAdministratorParamHasValue = e.Params.TryGetValue(isAdministratorParamName, out isAdministratorParamValue);

      // Получить старое значение параметра.
      if (calculateIsSubstitute && isSubstituteParamHasValue &&
          calculateIsAdministrator && isAdministratorParamHasValue)
        return isSubstituteParamValue || isAdministratorParamValue;
      
      if (calculateIsSubstitute && isSubstituteParamHasValue)
        return isSubstituteParamValue;
      
      if (calculateIsAdministrator && isAdministratorParamHasValue)
        return isAdministratorParamValue;
      
      bool isUsedParamValue;
      if (calculateIsUsed && e.Params.TryGetValue(isUsedParamName, out isUsedParamValue))
        return isUsedParamValue;
      
      bool hasDocumentsParamValue;
      if (calculateHasDocuments && e.Params.TryGetValue(hasDocumentsParamName, out hasDocumentsParamValue))
        return hasDocumentsParamValue;
      
      // Вычислить доступность на сервере, чтобы был один запрос.
      var result = Functions.Module.Remote.CalculateParams(registrationGroup, documentRegister);
      var access = Functions.Module.UnboxDictionary(result);
      var isSubstitute = access[isSubstituteParamName];
      var isAdministrator = access[isAdministratorParamName];
      var isUsed = access[isUsedParamName];
      var hasDocuments = access[hasDocumentsParamName];
      e.Params.AddOrUpdate(isSubstituteParamName, isSubstitute);
      e.Params.AddOrUpdate(isAdministratorParamName, isAdministrator);
      e.Params.AddOrUpdate(isUsedParamName, isUsed);
      e.Params.AddOrUpdate(hasDocumentsParamName, hasDocuments);
      
      if (calculateIsSubstitute && calculateIsAdministrator)
        return isSubstitute || isAdministrator;
      
      if (calculateIsSubstitute)
        return isSubstitute;
      
      if (calculateIsAdministrator)
        return isAdministrator;
      
      if (calculateIsUsed)
        return isUsed;
      
      if (calculateHasDocuments)
        return hasDocuments;
      
      return false;
    }
    
    /// <summary>
    /// Имеет ли доступ по замещению.
    /// </summary>
    /// <param name="e">Аргумент доступности.</param>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="calculateIsAdministrator">Признак администратора.</param>
    /// <returns>True, если доступ имеется.</returns>
    public static bool CalculateParams(Sungero.Domain.Client.CanExecuteActionArgs e, IDocumentRegister documentRegister, bool calculateIsAdministrator)
    {
      var isSubstituteParamName = Constants.Module.IsSubstituteResponsibleEmployeeParamName;
      var isAdministratorParamName = Constants.Module.IsAdministratorParamName;
      var isUsedParamName = Constants.Module.IsUsedParamName;
      var hasDocumentsParamName = Constants.Module.HasRegisteredDocumentsParamName;
      
      // Получить старое значение параметра.
      bool isSubstituteParamValue;
      bool isAdministratorParamValue;
      var isSubstituteParamHasValue = e.Params.TryGetValue(isSubstituteParamName, out isSubstituteParamValue);
      var isAdministratorParamHasValue = e.Params.TryGetValue(isAdministratorParamName, out isAdministratorParamValue);
      if (isSubstituteParamHasValue &&
          calculateIsAdministrator && isAdministratorParamHasValue)
        return isSubstituteParamValue || isAdministratorParamValue;
      
      // Вычислить доступность на сервере, чтобы был один запрос.
      var result = Functions.Module.Remote.CalculateParams(documentRegister.RegistrationGroup, documentRegister);
      var access = Functions.Module.UnboxDictionary(result);
      var isSubstitute = access[isSubstituteParamName];
      var isAdministrator = access[isAdministratorParamName];
      var isUsed = access[isUsedParamName];
      var hasDocuments = access[hasDocumentsParamName];
      e.Params.AddOrUpdate(isSubstituteParamName, isSubstitute);
      e.Params.AddOrUpdate(isAdministratorParamName, isAdministrator);
      e.Params.AddOrUpdate(isUsedParamName, isUsed);
      e.Params.AddOrUpdate(hasDocumentsParamName, hasDocuments);
      
      if (calculateIsAdministrator)
        return isSubstitute || isAdministrator;
      
      return isSubstitute;
    }
    
    #endregion
    
    #region Диалог выдачи прав на вложения ShowDialogGrantAccessRightsFromTask и ShowDialogGrantAccessRightsFromAssignment
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool? ShowDialogGrantAccessRights(IAssignmentBase assignment,
                                                    List<Domain.Shared.IEntity> attachments)
    {
      if (!attachments.Any() || assignment == null)
        return null;
      
      return ShowDialogGrantAccessRights(assignment.Task, attachments, new List<IRecipient>());
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения для определенного действия.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(IAssignmentBase assignment,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         Domain.Shared.IActionInfo action)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(assignment.Task, attachments, new List<IRecipient>(), action);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения для определенного действия.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <param name="dialogID">ИД диалога подтверждения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(IAssignmentBase assignment,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         Domain.Shared.IActionInfo action,
                                                                         string dialogID)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(assignment.Task, attachments, new List<IRecipient>(), action, dialogID);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения для соисполнителей.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Список соисполнителей.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool? ShowDialogGrantAccessRights(IAssignmentBase assignment,
                                                    List<Domain.Shared.IEntity> attachments,
                                                    List<IRecipient> additionalAssignees)
    {
      if (!attachments.Any() || assignment == null)
        return null;
      
      return ShowDialogGrantAccessRights(assignment.Task, attachments, additionalAssignees);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения на определенное действие для соисполнителей.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Соисполнители.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(IAssignmentBase assignment,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         List<IRecipient> additionalAssignees,
                                                                         Domain.Shared.IActionInfo action)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(assignment.Task, attachments, additionalAssignees, action);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения на определенное действие для соисполнителей.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Соисполнители.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <param name="dialogID">ИД диалога подтверждения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(IAssignmentBase assignment,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         List<IRecipient> additionalAssignees,
                                                                         Domain.Shared.IActionInfo action,
                                                                         string dialogID)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(assignment.Task,
                                                               attachments,
                                                               additionalAssignees,
                                                               action,
                                                               dialogID);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool? ShowDialogGrantAccessRights(ITask task, List<Domain.Shared.IEntity> attachments)
    {
      return ShowDialogGrantAccessRights(task, attachments, new List<IRecipient>());
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения для определенного действия.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(ITask task,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         Domain.Shared.IActionInfo action)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(task, attachments, null, action);
    }
    
    /// <summary>
    /// Создать диалог выдачи прав на вложения для определенного действия.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <param name="dialogID">ИД диалога подтверждения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(ITask task,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         Domain.Shared.IActionInfo action,
                                                                         string dialogID)
    {
      return ShowDialogGrantAccessRightsWithConfirmationDialog(task, attachments, null, action, dialogID);
    }
    
    /// <summary>
    /// Показать диалог выдачи прав на вложения с запросом подтверждения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Дополнительные согласующие.</param>
    /// <param name="action">Действие, текст утверждения которого будет показан.</param>
    /// <param name="dialogID">ИД диалога подтверждения.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.</returns>
    [Public]
    public static bool ShowDialogGrantAccessRightsWithConfirmationDialog(ITask task,
                                                                         List<Domain.Shared.IEntity> attachments,
                                                                         List<IRecipient> additionalAssignees,
                                                                         Domain.Shared.IActionInfo action,
                                                                         string dialogID = "")
    {
      var giveRights = ShowDialogGrantAccessRights(task, attachments, additionalAssignees);
      
      // Если явно не нажата отмена, то либо доп. прав не нужно (диалога не было), либо права назначены через диалог.
      if (action == null)
        return giveRights != false;
      
      // Замена стандартного диалога подтверждения выполнения действия.
      if (giveRights == null)
        return ShowConfirmationDialog(action.ConfirmationMessage, null, null, dialogID);
      
      return giveRights.Value;
    }
    
    /// <summary>
    /// Показать диалог выдачи прав на вложения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Дополнительные согласующие.</param>
    /// <returns>True, если был показан диалог (и не была нажата отмена).
    /// False, если была нажата отмена.
    /// Null, если диалог показан не был.</returns>
    [Public]
    public static bool? ShowDialogGrantAccessRights(ITask task,
                                                    List<Domain.Shared.IEntity> attachments,
                                                    List<IRecipient> additionalAssignees)
    {
      if (!attachments.Any() || task == null)
        return null;
      
      var participants = Functions.Module.Remote.GetTaskAssignees(task).ToList();
      if (!participants.Any())
        return null;
      
      if (additionalAssignees != null && additionalAssignees.Any())
        participants.AddRange(additionalAssignees);

      // Получаем только вложения, принадлежащие текущему заданию.
      // На остальные вложения проверять права не надо, т.к. скорее всего их добавил не текущий пользователь.
      var attachmentsWithoutAccessRights = Functions.Module.Remote.GetAttachmentsWithoutAccessRights(participants, attachments);
      if (attachmentsWithoutAccessRights.Any())
      {
        return Workflow.Client.ModuleFunctions.ShowDialogGrantAccessRights(participants, attachmentsWithoutAccessRights);
      }
      
      return null;
    }

    #endregion
    
    #region Вызов функций делопроизводства без явных зависимостей
    
    // Вызов remote функций в таком виде позволяет отказаться от зависимостей, оставив при этом работоспособность.
    
    /// <summary>
    /// Создать сопроводительное письмо.
    /// </summary>
    /// <param name="document">Документ, к которому создается сопроводительное письмо.</param>
    /// <returns>Письмо.</returns>
    [Public]
    public static IOfficialDocument CreateCoverLetter(IOfficialDocument document)
    {
      var letter = RecordManagement.PublicFunctions.OutgoingLetter.Remote.CreateCoverLetter(document);
      
      // Указать связь документов.
      letter.Relations.AddFrom(Constants.Module.CorrespondenceRelationName, document);
      return letter;
    }
    
    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <param name="document">Документ, по которому создается поручение.</param>
    /// <returns>Поручение.</returns>
    [Public]
    public virtual ITask CreateActionItemExecution(IOfficialDocument document)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(document);
    }
    
    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <param name="document">Документ, по которому создается поручение.</param>
    /// <param name="parentAssignmentId">Id задания, от которого создается поручение.</param>
    /// <returns>Поручение.</returns>
    public virtual ITask CreateActionItemExecution(IOfficialDocument document, int parentAssignmentId)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(document, parentAssignmentId);
    }
    
    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <param name="document">Документ, по которому создается поручение.</param>
    /// <param name="parentAssignmentId">Id задания, от которого создается поручение.</param>
    /// <param name="resolution">Текст резолюции.</param>
    /// <param name="assignedBy">Пользователь - автор резолюции.</param>
    /// <returns>Поручение.</returns>
    public virtual ITask CreateActionItemExecutionWithResolution(IOfficialDocument document, int parentAssignmentId, string resolution, Sungero.Company.IEmployee assignedBy)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecutionWithResolution(document, parentAssignmentId, resolution, assignedBy);
    }
    
    /// <summary>
    /// Создать задачу на рассмотрение документа.
    /// </summary>
    /// <param name="document">Входящий документ.</param>
    /// <returns>Рассмотрение.</returns>
    public static ITask CreateDocumentReview(IOfficialDocument document)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateDocumentReview(document);
    }
    
    #endregion
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Показать настройки интеллектуальной обработки документов.
    /// </summary>
    public virtual void ShowSmartProcessingSettings()
    {
      var smartProcessingSettings = PublicFunctions.SmartProcessingSetting.GetSettings();
      smartProcessingSettings.Show();
    }
    
    /// <summary>
    /// Удалить параметр NeedValidateRegisterFormat.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="e">Аргумент действия.</param>
    public static void RemoveNeedValidateRegisterFormatParameter(IOfficialDocument document,
                                                                 Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Если документ в процессе верификации, то игнорировать изменение полей регистрационных данных.
      if (document.VerificationState == OfficialDocument.VerificationState.InProcess)
        e.Params.Remove(Constants.OfficialDocument.NeedValidateRegisterFormat);
    }
    
    #endregion
    
    #region Проверка параметров диалогов
    
    /// <summary>
    /// Проверить даты диалога отчета.
    /// </summary>
    /// <param name="args">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="dialogPeriodBegin">Параметр даты начала.</param>
    /// <param name="dialogPeriodEnd">Параметр даты конца.</param>
    [Public]
    public static void CheckReportDialogPeriod(CommonLibrary.InputDialogButtonClickEventArgs args,
                                               CommonLibrary.IDateDialogValue dialogPeriodBegin,
                                               CommonLibrary.IDateDialogValue dialogPeriodEnd)
    {
      var periodBegin = dialogPeriodBegin.Value;
      var periodEnd = dialogPeriodEnd.Value;
      
      CheckDialogPeriod(args, dialogPeriodBegin, dialogPeriodEnd, Sungero.Docflow.Resources.WrongPeriodReport);
      
      // Проверить даты на наличие календаря рабочего времени.
      var periodBeginNoCalendarError = Sungero.Docflow.PublicFunctions.Module.CheckDateByWorkCalendar(periodBegin);
      if (periodBegin.HasValue && !string.IsNullOrWhiteSpace(periodBeginNoCalendarError))
        args.AddError(periodBeginNoCalendarError, dialogPeriodBegin);
      
      var periodEndNoCalendarError = Sungero.Docflow.PublicFunctions.Module.CheckDateByWorkCalendar(periodEnd);
      if (periodEnd.HasValue && !string.IsNullOrWhiteSpace(periodEndNoCalendarError))
        args.AddError(periodEndNoCalendarError, dialogPeriodEnd);
    }
    
    /// <summary>
    /// Проверить даты диалога.
    /// </summary>
    /// <param name="args">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="dialogPeriodBegin">Параметр даты начала.</param>
    /// <param name="dialogPeriodEnd">Параметр даты конца.</param>
    [Public]
    public static void CheckDialogPeriod(CommonLibrary.InputDialogButtonClickEventArgs args,
                                         CommonLibrary.IDateDialogValue dialogPeriodBegin,
                                         CommonLibrary.IDateDialogValue dialogPeriodEnd)
    {
      CheckDialogPeriod(args, dialogPeriodBegin, dialogPeriodEnd, Sungero.Docflow.Resources.WrongPeriod);
    }
    
    /// <summary>
    /// Проверить даты диалога.
    /// </summary>
    /// <param name="args">Аргументы события нажатия на кнопку диалога.</param>
    /// <param name="dialogPeriodBegin">Параметр даты начала.</param>
    /// <param name="dialogPeriodEnd">Параметр даты конца.</param>
    /// <param name="wrongPeriodError">Текст ошибки о неверной дате.</param>
    private static void CheckDialogPeriod(CommonLibrary.InputDialogButtonClickEventArgs args,
                                          CommonLibrary.IDateDialogValue dialogPeriodBegin,
                                          CommonLibrary.IDateDialogValue dialogPeriodEnd,
                                          CommonLibrary.LocalizedString wrongPeriodError)
    {
      var periodBegin = dialogPeriodBegin.Value;
      var periodEnd = dialogPeriodEnd.Value;
      
      if (periodBegin.HasValue && periodEnd.HasValue &&
          periodEnd.Value < periodBegin.Value)
      {
        // Выделить оба поля в диалоге с одним текстом ошибки. Ошибки с одинаковыми текстами схлапываются в одну.
        args.AddError(wrongPeriodError, dialogPeriodBegin);
        args.AddError(wrongPeriodError, dialogPeriodEnd);
      }
    }
    
    /// <summary>
    /// Валидация даты по рабочему календарю.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Сообщения валидации, пустая строка при их отсутствии.</returns>
    [Public]
    public static string CheckDateByWorkCalendar(DateTime? date)
    {
      if (date == null)
        return string.Empty;
      
      if (!WorkingTime.GetAllCachedByYear(date.Value.Year).Any(c => c.Year == date.Value.Year))
        return Docflow.Resources.EmptyWorkingCalendarFormat(date.Value.Year);
      
      return string.Empty;
    }
    
    #endregion
    
    /// <summary>
    /// Валидация срока по рабочему календарю.
    /// </summary>
    /// <param name="deadline">Срок.</param>
    /// <returns>Сообщения валидации, пустая строка при их отсутствии.</returns>
    [Public]
    public static string CheckDeadlineByWorkCalendar(DateTime? deadline)
    {
      return CheckDeadlineByWorkCalendar(Users.Current, deadline);
    }
    
    /// <summary>
    /// Валидация срока по рабочему календарю конкретного пользователя.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="deadline">Срок.</param>
    /// <returns>Сообщения валидации, пустая строка при их отсутствии.</returns>
    [Public]
    public static string CheckDeadlineByWorkCalendar(IUser user, DateTime? deadline)
    {
      if (deadline == null)
        return string.Empty;
      
      var checkDateError = CheckDateByWorkCalendar(deadline);
      if (!string.IsNullOrWhiteSpace(checkDateError))
        return checkDateError;
      
      // Срок задания дб рабочим днем.
      if (!deadline.Value.IsWorkingDay(user))
        return Docflow.Resources.ImpossibleSpecifyDeadlineToNotWorkingDay;
      
      // Срок задания дб рабочим временем.
      if (deadline.Value.HasTime() && !deadline.Value.IsWorkingTime(user))
        return Docflow.Resources.ImpossibleSpecifyDeadlineToNotWorkingTime;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <returns>Задача на ознакомление.</returns>
    [Public]
    public static ITask CreateAcquaintanceTask(IOfficialDocument document)
    {
      return RecordManagement.PublicFunctions.Module.Remote.CreateAcquaintanceTask(document);
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
      var documentApprovalTask = ApprovalTasks.As(assignment.Task);
      var hasSubActionItem = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess);
      if (hasSubActionItem)
        return false;

      var dialogText = Resources.ExecuteWithoutCreatingActionItem;
      var dialog = Dialogs.CreateTaskDialog(dialogText, MessageType.Question);
      dialog.Buttons.AddYes();
      dialog.Buttons.Default = DialogButtons.Yes;
      var createActionItemButton = dialog.Buttons.AddCustom(Resources.CreateActionItem);
      dialog.Buttons.AddNo();
      
      var result = dialog.Show();
      if (result == DialogButtons.Yes)
        return true;
      
      if (result == DialogButtons.No || result == DialogButtons.Cancel)
        e.Cancel();
      
      var stages = Functions.ApprovalRuleBase.Remote.GetStages(documentApprovalTask.ApprovalRule, document, documentApprovalTask).Stages;
      var assignedBy = Sungero.Company.Employees.Null;
      
      // Автором резолюции вычислить адресата, либо подписывающего.
      if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Review))
        assignedBy = documentApprovalTask.Addressee;
      else if (stages.Any(s => s.StageType == Docflow.ApprovalRuleBaseStages.StageType.Sign))
        assignedBy = documentApprovalTask.Signatory;
      
      var isExecutionAssignment = ApprovalExecutionAssignments.Is(assignment);
      var resolution = assignment.ActiveText;
      if (isExecutionAssignment)
        resolution = ApprovalExecutionAssignments.As(assignment).ResolutionText;
      
      assignment.Save();
      
      var actionItem = RecordManagement.ActionItemExecutionTasks.As(Functions.Module.CreateActionItemExecutionWithResolution(document, assignment.Id, resolution, assignedBy));
      
      if (actionItem != null)
      {
        actionItem.WaitForParentAssignment = true;
        actionItem.ShowModal();
      }

      hasSubActionItem = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.InProcess);
      if (hasSubActionItem)
        return true;
      
      var hasDraftSubActionItem = Functions.Module.HasSubActionItems(assignment.Task, Workflow.Task.Status.Draft);
      e.AddError(hasDraftSubActionItem ? Resources.AllCreatedActionItemsShouldBeStarted : Resources.CreatedActionItemExecutionNeeded);
      e.Cancel();
      return true;
    }
    
    /// <summary>
    /// Показать диалог подтверждения выполнения без отправки документа.
    /// </summary>
    /// <param name="assignment">Задание, которое выполняется.</param>
    /// <param name="collapsed">Схлопнутые типы заданий.</param>
    /// <param name="e">Аргументы.</param>
    /// <returns>True, если диалог был, иначе false.</returns>
    public static bool ShowConfirmationDialogSendToCounterparty(IAssignment assignment, System.Collections.Generic.IEnumerable<Enumeration?> collapsed,
                                                                Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var task = ApprovalTasks.As(assignment.Task);
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      var canSend = Functions.ApprovalSendingAssignment.CanSendToCounterparty(document);
      if (!canSend ||
          task.DeliveryMethod == null ||
          task.DeliveryMethod.Sid != Constants.MailDeliveryMethod.Exchange ||
          (collapsed != null && !collapsed.Any(c => c == ApprovalPrintingAssignmentCollapsedStagesTypesPr.StageType.Sending)) ||
          Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.LastVersionSended(document))
        return false;
      
      var dialog = Dialogs.CreateTaskDialog(Resources.ExecuteWithoutSendToCounterparty, MessageType.Warning);
      dialog.Buttons.AddYes();
      dialog.Buttons.Default = DialogButtons.Yes;
      var send = dialog.Buttons.AddCustom(ApprovalSendingAssignments.Info.Actions.SendViaExchangeService.LocalizedName);
      dialog.Buttons.AddNo();
      
      var result = dialog.Show();
      if (result == DialogButtons.Yes)
        return true;
      
      if (result == DialogButtons.No || result == DialogButtons.Cancel)
        e.Cancel();
      
      // Открываем диалог отправки.
      Functions.ApprovalSendingAssignment.SendToCounterparty(document, task);
      
      // Если отправка так и не была выполнена - отменяем выполнение.
      if (!Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.LastVersionSended(document))
        e.Cancel();
      
      return true;
    }
    
    /// <summary>
    /// Показать диалог подтверждения выполнения.
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <param name="description">Дополнительный текст.</param>
    /// <param name="title">Заголовок.</param>
    /// <param name="dialogID">ИД диалога подтверждения.</param>
    /// <returns>True, если запрос был подтвержден.</returns>
    /// <remarks>При указании dialogID в диалоге появляется флажок "Больше не спрашивать".</remarks>
    [Public]
    public static bool ShowConfirmationDialog(string text, string description, string title, string dialogID)
    {
      var confirmationDialog = Dialogs.CreateConfirmDialog(text, description, title);
      
      if (!string.IsNullOrWhiteSpace(dialogID))
        confirmationDialog.WithDontAskAgain(dialogID);
      return confirmationDialog.Show();
    }
    
    /// <summary>
    /// Проверка заблокированности сущности другими пользователями.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>True, если сущность заблокирована.
    /// False, если сущность не заблокирована (или заблокирована пользователем, который выполняет действие).</returns>
    [Public]
    public static bool IsLockedByOther(Domain.Shared.IEntity entity)
    {
      var lockInfo = entity != null ? Locks.GetLockInfo(entity) : null;
      return lockInfo != null && lockInfo.IsLockedByOther;
    }
    
    /// <summary>
    /// Проверка заблокированности сущности текущим клиентом.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>True, если сущность заблокирована.</returns>
    [Public]
    public static bool IsLockedByMe(Domain.Shared.IEntity entity)
    {
      var lockInfo = entity != null ? Locks.GetLockInfo(entity) : null;
      return lockInfo != null && lockInfo.IsLockedByMe;
    }
    
    /// <summary>
    /// Проверка заблокированности сущности.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>True, если сущность заблокирована.</returns>
    [Public]
    public static bool IsLocked(Domain.Shared.IEntity entity)
    {
      var lockInfo = entity != null ? Locks.GetLockInfo(entity) : null;
      return lockInfo != null && lockInfo.IsLocked;
    }
    
    /// <summary>
    /// Проверка заблокированности сущности, с добавлением ошибки, если сущность заблокирована.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="e">Аргументы события, в котором проверяется доступность сущности.</param>
    /// <returns>True, если сущность заблокирована.
    /// False, если сущность не заблокирована (или заблокирована пользователем, который выполняет действие).</returns>
    public static bool IsLockedByOther(Domain.Shared.IEntity entity, Domain.Client.ExecuteActionArgs e)
    {
      var lockInfo = entity != null ? Locks.GetLockInfo(entity) : null;
      var isLockedByOther = lockInfo != null && lockInfo.IsLockedByOther;
      
      if (isLockedByOther)
        e.AddError(lockInfo.LockedMessage);
      
      e.ClearMessageAfterAction = true;
      return isLockedByOther;
    }
    
    /// <summary>
    /// Проверка заблокированности любой версии.
    /// </summary>
    /// <param name="versions">Список версий документа.</param>
    /// <returns>True, если заблокирована хотя бы одна версия.</returns>
    [Public]
    public static bool VersionIsLocked(List<Sungero.Content.IElectronicDocumentVersions> versions)
    {
      foreach (var version in versions)
      {
        var lockInfo = version.Body != null ? Locks.GetLockInfo(version.Body) : null;
        var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
        
        if (isLockedByOther)
          return true;
      }
      
      return false;
    }
    
    /// <summary>
    /// Показать список всех отчетов.
    /// </summary>
    public virtual void ShowAllReports()
    {
      Reports.ShowAll();
    }

    /// <summary>
    /// Показать настройки текущего пользователя.
    /// </summary>
    public virtual void ShowCurrentPersonalSettings()
    {
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSettings != null)
        personalSettings.Show();
      else
      {
        if (Sungero.Company.Employees.Current == null)
          Dialogs.ShowMessage(Resources.FailedGetSettingsForNonEmployee, MessageType.Error);
        else
          Dialogs.ShowMessage(Resources.FailedGetSettings, MessageType.Error);
      }
    }
    
    /// <summary>
    /// Запустить отчет "Лист согласования".
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void RunApprovalSheetReport(IOfficialDocument document)
    {
      var hasSignatures = Functions.OfficialDocument.Remote.HasSignatureForApprovalSheetReport(document);
      if (!hasSignatures)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.DocumentIsNotSigned);
        return;
      }
      
      var report = Reports.GetApprovalSheetReport();
      report.Document = document;
      report.Open();
    }
    
    /// <summary>
    /// Запустить отчёт "Протокол эл. обмена".
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void RunExchangeOrderReport(IOfficialDocument document)
    {
      var report = Reports.GetExchangeOrderReport();
      report.Entity = document;
      report.Open();
    }
    
    #region Подписание документа
    
    /// <summary>
    /// Утвердить документ.
    /// </summary>
    /// <param name="assignment">Задание с документом.</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    [Obsolete("Используйте метод Functions.ApprovalSigningAssignment.ApproveDocument")]
    public virtual void ApproveDocument(IAssignment assignment, bool needStrongSign, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var task = ApprovalTasks.As(assignment.Task);
      if (task == null)
        return;
      
      var document = task.DocumentGroup.OfficialDocuments.Single();
      var addenda = task.AddendaGroup.OfficialDocuments.ToList();
      var performer = Company.Employees.As(assignment.Performer);
      var comment = string.IsNullOrWhiteSpace(assignment.ActiveText) ? string.Empty : assignment.ActiveText;
      
      this.ApproveDocument(document, addenda, performer, needStrongSign, comment, eventArgs);
    }
    
    /// <summary>
    /// Утвердить документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="substituted">За кого выполняется утверждение.</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="comment">Комментарий.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    public virtual void ApproveDocument(IOfficialDocument document, List<IOfficialDocument> addenda,
                                        Company.IEmployee substituted, bool needStrongSign, string comment,
                                        Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var currentEmployee = Company.Employees.Current;
      var canSubstitutedApprove = Functions.OfficialDocument.Remote.CanSignByEmployee(document, substituted);
      var canCurrentEmployeeApprove = currentEmployee != null
        ? Functions.OfficialDocument.Remote.CanSignByEmployee(document, currentEmployee)
        : false;
      var signatory = canSubstitutedApprove && canCurrentEmployeeApprove
        ? substituted
        : currentEmployee;

      try
      {
        if (!Functions.Module.ApproveWithAddenda(document, addenda, null, signatory, false, needStrongSign, comment))
          eventArgs.AddError(ApprovalTasks.Resources.ToPerformNeedSignDocument);
      }
      catch (CommonLibrary.Exceptions.PlatformException ex)
      {
        Logger.ErrorFormat("Failed to approve document with addenda. Document id = '{0}' ", ex, document.Id);
        if (!ex.IsInternal)
        {
          var message = ex.Message.Trim().EndsWith(".") ? ex.Message : string.Format("{0}.", ex.Message);
          eventArgs.AddError(message);
        }
        else
          throw;
      }
    }
    
    /// <summary>
    /// Согласовать документ.
    /// </summary>
    /// <param name="assignment">Задание с документом.</param>
    /// <param name="endorse">Признак согласования документа, true - согласовать документ, false - не согласовывать.</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    public virtual void EndorseDocument(IAssignment assignment,
                                        bool endorse, bool needStrongSign,
                                        Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var approvalTask = ApprovalTasks.As(assignment.Task);
      var freeApprovalTask = FreeApprovalTasks.As(assignment.Task);
      
      if (approvalTask == null && freeApprovalTask == null)
        return;
      
      var performer = Company.Employees.As(assignment.Performer);
      // Добавить в комментарий ЭП результат выполнения задания, если пользователь ничего не указал.
      var comment = string.IsNullOrWhiteSpace(assignment.ActiveText) ? string.Empty : assignment.ActiveText;
      
      var document = approvalTask != null
        ? ElectronicDocuments.As(approvalTask.DocumentGroup.OfficialDocuments.Single())
        : freeApprovalTask.ForApprovalGroup.ElectronicDocuments.Single();
      
      // Получить документы из группы вложений "Приложения", исключая дубли и основной документ.
      var addenda = new List<IElectronicDocument>();
      if (approvalTask != null)
        addenda = Functions.Module.GetApprovalTaskAddendaForEndorse(assignment);
      else if (freeApprovalTask != null)
        addenda = Functions.Module.GetFreeApprovalTaskAddendaForEndorse(assignment);
      addenda = addenda.Where(x => x.Id != document.Id).Distinct().ToList();
      
      this.EndorseDocument(document, addenda, performer, endorse, needStrongSign, comment, eventArgs);
    }
    
    /// <summary>
    /// Получить документы для согласования из группы вложений "Приложения" задания задачи на согласование по регламенту.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Документы из группы вложений "Приложения" задания.</returns>
    public virtual List<IElectronicDocument> GetApprovalTaskAddendaForEndorse(IAssignment assignment)
    {
      if (ApprovalAssignments.Is(assignment))
        return ApprovalAssignments.As(assignment).AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>();
      
      if (ApprovalManagerAssignments.Is(assignment))
        return ApprovalManagerAssignments.As(assignment).AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>();
      
      if (ApprovalReviewAssignments.Is(assignment))
        return ApprovalReviewAssignments.As(assignment).AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>();
      
      if (ApprovalReworkAssignments.Is(assignment))
        return ApprovalReworkAssignments.As(assignment).AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>();
      
      if (ApprovalSigningAssignments.Is(assignment))
        return ApprovalSigningAssignments.As(assignment).AddendaGroup.OfficialDocuments.ToList<IElectronicDocument>();
      
      return new List<IElectronicDocument>();
    }
    
    /// <summary>
    /// Получить документы для согласования из группы вложений "Приложения" задания задачи на свободное согласование.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Документы из группы вложений "Приложения" задания.</returns>
    public virtual List<IElectronicDocument> GetFreeApprovalTaskAddendaForEndorse(IAssignment assignment)
    {
      if (FreeApprovalAssignments.Is(assignment))
        return FreeApprovalAssignments.As(assignment).AddendaGroup.ElectronicDocuments.ToList();
      
      return new List<IElectronicDocument>();
    }
    
    /// <summary>
    /// Согласовать документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="substituted">За кого выполняется утверждение.</param>
    /// <param name="endorse">Признак согласования документа, true - согласовать документ, false - не согласовывать.</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="comment">Комментарий.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    public virtual void EndorseDocument(IElectronicDocument document, List<IElectronicDocument> addenda, Company.IEmployee substituted, bool endorse, bool needStrongSign, string comment, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (!document.HasVersions && !endorse)
        return;
      
      try
      {
        var isSigned = endorse ?
          this.EndorseWithAddenda(document, addenda, null, substituted, needStrongSign, comment) :
          Signatures.NotEndorse(document.LastVersion, null, comment, substituted);
        
        if (!isSigned)
          eventArgs.AddError(ApprovalTasks.Resources.ToPerformNeedSignDocument);
      }
      catch (CommonLibrary.Exceptions.PlatformException ex)
      {
        Logger.ErrorFormat("Failed to endorse document with addenda. Document id = '{0}' ", ex, document.Id);
        if (!ex.IsInternal)
        {
          var message = ex.Message.EndsWith(".") ? ex.Message : string.Format("{0}.", ex.Message);
          eventArgs.AddError(message);
        }
        else
          throw;
      }
    }
    
    /// <summary>
    /// Утвердить документ с приложениями.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="certificate">Сертификат (не передавать, чтобы оставить выбор пользователю).</param>
    /// <param name="substituted">За кого выполняется утверждение (не передавать, чтобы утвердить под текущим пользователем).</param>
    /// <param name="endorseWhenApproveFailed">Согласовать документ, если не удается выполнить утверждение.</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>True, если сам документ был утверждён или не имеет версий. Факт подписания приложений неважен.</returns>
    [Public]
    public virtual bool ApproveWithAddenda(IOfficialDocument document, List<IOfficialDocument> addenda,
                                           ICertificate certificate, Company.IEmployee substituted,
                                           bool endorseWhenApproveFailed, bool needStrongSign, string comment)
    {
      var addendaHaveVersions = addenda != null && addenda.Any(a => a.HasVersions);
      if (!document.HasVersions && !addendaHaveVersions)
        return true;
      
      if (certificate == null && needStrongSign)
      {
        if (!this.TryGetUserCertificate(document, out certificate))
          return false;
      }
      
      try
      {
        var result = !document.HasVersions;
        if (document.HasVersions)
        {
          var canApprove = !Functions.OfficialDocument.Remote.GetApprovalValidationErrors(document, true).Any();
          if (canApprove)
          {
            var accountingDocument = AccountingDocumentBases.As(document);
            if (accountingDocument != null && accountingDocument.IsFormalized == true)
            {
              Functions.AccountingDocumentBase.GenerateDefaultSellerTitle(accountingDocument);
              Functions.AccountingDocumentBase.GenerateDefaultBuyerTitle(accountingDocument);
            }
            
            result = Signatures.Approve(document.LastVersion, certificate, comment, substituted);
          }
          else if (endorseWhenApproveFailed)
            result = Signatures.Endorse(document.LastVersion, certificate, comment, substituted);
        }
        
        // Если не удалось утвердить основной документ или приложений нет - приложения не трогаем.
        if (!result || addenda == null || !addenda.Any())
          return result;
        
        var addendaWithVersions = addenda.Where(a => a.HasVersions).ToList();
        if (!addendaWithVersions.Any())
          return result;
        
        var canBeApproved = new List<IOfficialDocument>();
        var canBeEndorsed = new List<IOfficialDocument>();
        foreach (var addendumDocument in addendaWithVersions)
        {
          var canApprove = !Functions.OfficialDocument.Remote.GetApprovalValidationErrors(addendumDocument, true).Any();
          if (canApprove)
            canBeApproved.Add(addendumDocument);
          else
            canBeEndorsed.Add(addendumDocument);
        }
        foreach (var addendumDocument in canBeApproved)
        {
          var addendumAccountingDocument = AccountingDocumentBases.As(addendumDocument);
          if (addendumAccountingDocument != null && addendumAccountingDocument.IsFormalized == true)
          {
            Functions.AccountingDocumentBase.GenerateDefaultSellerTitle(addendumAccountingDocument);
            Functions.AccountingDocumentBase.GenerateDefaultBuyerTitle(addendumAccountingDocument);
          }
        }
        if (canBeApproved.Any())
          Signatures.Approve(canBeApproved.Select(a => a.LastVersion), certificate, comment, substituted);
        if (canBeEndorsed.Any())
          Signatures.Endorse(canBeEndorsed.Select(a => a.LastVersion), certificate, comment, substituted);
        return result;
      }
      catch (Sungero.Domain.Shared.Exceptions.ChildEntityNotFoundException ex)
      {
        throw AppliedCodeException.Create(OfficialDocuments.Resources.SigningVersionWasDeleted, ex);
      }
    }
    
    /// <summary>
    /// Согласовать документ с приложениями.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="addenda">Приложения.</param>
    /// <param name="certificate">Сертификат (не передавать, чтобы оставить выбор пользователю).</param>
    /// <param name="substituted">За кого выполняется утверждение (не передавать, чтобы утвердить под текущим пользователем).</param>
    /// <param name="needStrongSign">Требуется квалифицированная электронная подпись.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>True, если сам документ был согласован или не имеет версий. Факт согласования приложений неважен.</returns>
    [Public]
    public virtual bool EndorseWithAddenda(IElectronicDocument document, List<IElectronicDocument> addenda,
                                           ICertificate certificate, IUser substituted,
                                           bool needStrongSign, string comment)
    {
      var addendaHasVersions = addenda != null && addenda.Any(a => a.HasVersions);
      if (!document.HasVersions && !addendaHasVersions)
        return true;
      
      if (certificate == null && needStrongSign)
      {
        var officialDocument = OfficialDocuments.As(document);
        
        if (!this.TryGetUserCertificate(officialDocument, out certificate))
          return false;
      }
      
      try
      {
        var result = !document.HasVersions;
        if (document.HasVersions)
        {
          result = Signatures.Endorse(document.LastVersion, certificate, comment, substituted);
        }
        
        // Если не удалось согласовать основной документ или приложений нет - приложения не трогаем.
        if (!result || addenda == null || !addenda.Any())
          return result;
        
        var addendaWithVersions = addenda.Where(a => a.HasVersions).ToList();
        if (!addendaWithVersions.Any())
          return result;
        
        Signatures.Endorse(addendaWithVersions.Select(a => a.LastVersion), certificate, comment, substituted);
        return result;
      }
      catch (Sungero.Domain.Shared.Exceptions.ChildEntityNotFoundException ex)
      {
        throw AppliedCodeException.Create(OfficialDocuments.Resources.SigningVersionWasDeleted, ex);
      }
    }
    
    /// <summary>
    /// Получить сертификат пользователя для подписания.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    /// <returns>True, если выбор произведен, false в случае отмены.</returns>
    private bool TryGetUserCertificate(IOfficialDocument document, out ICertificate certificate)
    {
      certificate = null;
      var certificates = PublicFunctions.Module.Remote.GetCertificates(document);
      var ourSigningReason = document.OurSigningReason;
      
      // Взять сертификат из основания подписания, если он подходит по критериям.
      // При рассмотрении адресатом поле "Основание" вернет сертификат подписавшего, а не рассматривающего, поэтому для рассмотрения автовыбор сертификата из "Основания" не делать.
      if (!CallContext.CalledFrom(ApprovalReviewAssignments.Info) &&
          ourSigningReason != null &&
          ourSigningReason.Certificate != null &&
          certificates.Contains(ourSigningReason.Certificate) &&
          Equals(ourSigningReason.Certificate.Owner, Employees.Current) &&
          !Functions.SignatureSetting.Remote.FormalizedPowerOfAttorneyIsExpired(ourSigningReason))
      {
        certificate = document.OurSigningReason.Certificate;
        return true;
      }
      
      if (certificates.Any())
      {
        var selectedCertificate = certificates.Count() > 1 ?
          certificates.ShowSelectCertificate() :
          certificates.First();
        if (selectedCertificate == null)
          return false;
        certificate = selectedCertificate;
      }
      return true;
    }
    
    #endregion
    
    #region Диалог добавления приложений из файлов

    /// <summary>
    /// Получить сообщение об успешном создании приложений из файлов.
    /// </summary>
    /// <param name="addendaCount">Количество приложений.</param>
    /// <returns>Сообщение об успешном создании приложений из файлов.</returns>
    public virtual string GetManyAddendumDialogSuccessfulNotify(int addendaCount)
    {
      var addendumName = Sungero.Docflow.Resources.AddendumNameForOneDocument;
      if (addendaCount > 1 && addendaCount < 5)
        addendumName = Sungero.Docflow.Resources.AddendumNameLessFiveDocument;
      else if (addendaCount >= 5)
        addendumName = Sungero.Docflow.Resources.AddendumNameForManyDocument;
      
      return Sungero.Docflow.OfficialDocuments.Resources.AddendaCreatedSuccesfullyFormat(addendaCount, addendumName);
    }
    
    /// <summary>
    /// Создать приложение к документу.
    /// </summary>
    /// <param name="addendumName">Имя документа.</param>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <param name="addendumContent">Тело документа.</param>
    public virtual void CreateAddendum(string addendumName, IOfficialDocument leadingDocument, byte[] addendumContent)
    {
      var addendum = Functions.Addendum.Remote.Create();
      addendum.LeadingDocument = leadingDocument;
      if (addendum.State.Properties.Name.IsEnabled)
        addendum.Name = System.IO.Path.GetFileNameWithoutExtension(addendumName);
      else
        addendum.Subject = System.IO.Path.GetFileNameWithoutExtension(addendumName);
      if (addendum.DocumentKind == null)
        addendum.DocumentKind = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(Docflow.IAddendum)).FirstOrDefault();
      
      using (var fileStream = new System.IO.MemoryStream(addendumContent))
      {
        addendum.CreateVersionFrom(fileStream, System.IO.Path.GetExtension(addendumName));
        addendum.Save();
      }
      return;
    }
    
    /// <summary>
    /// Диалог массового добавления приложений из файлов.
    /// </summary>
    /// <param name="document">Документ, для которого создаются приложения.</param>
    public virtual void AddManyAddendumDialog(IOfficialDocument document)
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.OfficialDocuments.Resources.ManyAddendumDialogTitle);
      dialog.Text = Sungero.Docflow.OfficialDocuments.Resources.ManyAddendumDialogText;
      if (ClientApplication.ApplicationType == ApplicationType.Web)
        dialog.Width = 400;
      
      var fileSelectors = new List<CommonLibrary.IFileSelectDialogValue>();
      for (int i = 0; i < Constants.Module.ManyAddendumDialogLimit; i++)
      {
        var fileSelector = dialog.AddFileSelect(string.Format("{0}.", i + 1), false);
        fileSelector.IsVisible = i == 0;
        fileSelector.MaxFileSize(Constants.Module.ManyAddendumDialogMaxFileSize);
        fileSelectors.Add(fileSelector);
      }
      var importButton = dialog.Buttons.AddCustom(Sungero.Docflow.OfficialDocuments.Resources.AddendumAddFiles);
      var cancelButton = dialog.Buttons.AddCustom(Sungero.Docflow.Resources.Dialog_Close);
      
      var isImportingAddenda = false;
      var addendaCreatedSuccessfully = true;
      var successfullyCreatedAddendaCount = 0;
      var errorList = new List<string>();
      var errorFileSelectors = new List<CommonLibrary.IFileSelectDialogValue>();
      
      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = (b) =>
      {
        if (isImportingAddenda)
        {
          importButton.IsVisible = false;
          cancelButton.Name = Sungero.Docflow.Resources.Dialog_Close;
          foreach (var fileSelector in fileSelectors)
          {
            fileSelector.IsEnabled = false;
            if (fileSelector.Value == null || fileSelector.Value.Name == string.Empty)
              fileSelector.IsVisible = false;
          }
        }
        else
        {
          cancelButton.Name = Resources.ExportDialog_Cancel;
          var lastVisibleFileSelector = fileSelectors.Last(f => f.IsVisible);
          if (lastVisibleFileSelector.Value != null &&
              lastVisibleFileSelector.Value.Name != string.Empty &&
              fileSelectors.Any(f => !f.IsVisible))
            fileSelectors.FirstOrDefault(f => !f.IsVisible).IsVisible = true;
          
          foreach (var fileSelector in fileSelectors.Where(f => f.Value != null && f.Value.Name != string.Empty))
          {
            var equalFileSelectors = fileSelectors.Where(f => f.Value != null && f.Value.Name != string.Empty)
              .Where(f => !Equals(f, fileSelector) && Equals(f.Value.Name, fileSelector.Value.Name)).ToList();
            if (equalFileSelectors.Any())
            {
              equalFileSelectors.Add(fileSelector);
              b.AddError(Sungero.Docflow.Resources.ErrorEqualsFileAddManyAddendums, equalFileSelectors.ToArray());
            }
          }
          
          if (!fileSelectors.Any(f => f.Value != null && f.Value.Name != string.Empty))
            importButton.IsEnabled = false;
          else
            importButton.IsEnabled = true;
        }
        
      };
      dialog.SetOnRefresh(refresh);
      dialog.SetOnButtonClick(b =>
                              {
                                if (b.Button == cancelButton)
                                  return;
                                if (b.Button == importButton && b.IsValid)
                                {
                                  try
                                  {
                                    if (isImportingAddenda)
                                      return;

                                    if (!b.IsValid)
                                      return;
                                    
                                    isImportingAddenda = true;
                                    
                                    foreach (var fileSelector in fileSelectors.Where(f => f.Value != null && f.Value.Name != string.Empty))
                                    {
                                      try
                                      {
                                        CreateAddendum(fileSelector.Value.Name, document, fileSelector.Value.Content);
                                        successfullyCreatedAddendaCount++;
                                      }
                                      catch (AppliedCodeException ae)
                                      {
                                        errorFileSelectors.Add(fileSelector);
                                        errorList.Add(ae.Message);
                                        addendaCreatedSuccessfully = false;
                                      }
                                      catch (Sungero.Domain.Shared.Validation.ValidationException ex)
                                      {
                                        Logger.Error(ex.Message, ex);
                                        errorFileSelectors.Add(fileSelector);
                                        errorList.Add(ex.Message);
                                        addendaCreatedSuccessfully = false;
                                      }
                                      catch (Exception ex)
                                      {
                                        Logger.Error(ex.Message, ex);
                                        errorFileSelectors.Add(fileSelector);
                                        errorList.Add(Sungero.Docflow.Resources.InternalServerError);
                                        addendaCreatedSuccessfully = false;
                                      }
                                    }
                                    
                                    if (addendaCreatedSuccessfully && successfullyCreatedAddendaCount > 0)
                                      Dialogs.NotifyMessage(Functions.Module.GetManyAddendumDialogSuccessfulNotify(successfullyCreatedAddendaCount));
                                    else
                                    {
                                      var errorMessage = string.Empty;
                                      if (successfullyCreatedAddendaCount > 0)
                                        errorMessage += Functions.Module.GetManyAddendumDialogSuccessfulNotify(successfullyCreatedAddendaCount);

                                      errorMessage += Sungero.Docflow.Resources.ErrorAddManyAddendumsFormat(successfullyCreatedAddendaCount > 0 ?
                                                                                                            Sungero.Docflow.Resources.ErrorAddManyAddendumsOther : string.Empty);
                                      foreach (var error in errorList.Distinct())
                                      {
                                        errorMessage += string.Format(Sungero.Docflow.Resources.ErrorList, error);
                                      }
                                      b.AddError(errorMessage, errorFileSelectors.ToArray());
                                      refresh.Invoke(null);
                                    }
                                  }
                                  catch (AppliedCodeException ae)
                                  {
                                    b.AddError(ae.Message);
                                  }
                                  catch (Exception ex)
                                  {
                                    Logger.Error(ex.Message, ex);
                                    b.AddError(Sungero.Docflow.Resources.InternalServerError);
                                  }
                                }
                              });
      dialog.Show();
    }
    
    #endregion
    
    #region Выгрузка
    
    /// <summary>
    /// Запустить выгрузку документов с поиском документов.
    /// </summary>
    [Public]
    public static void ExportFinancialDocumentDialogWithSearch()
    {
      if (ClientApplication.ApplicationType == ApplicationType.Web)
        ExportDocumentDialogWithSearchInWeb(null, false);
    }
    
    /// <summary>
    /// Запустить поиск документов в финархиве.
    /// </summary>
    /// <returns>Ленивый запрос на отображение документов.</returns>
    [Public]
    public static IQueryable<IOfficialDocument> FinancialDocumentDialogSearch()
    {
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
        return ExportDocumentDialogWithSearch();
      else
        return ExportDocumentDialogWithSearchInWeb(null, true);
    }
    
    /// <summary>
    /// Запустить выгрузку документов из явно указанных.
    /// </summary>
    /// <param name="documents">Документы, которые надо выгрузить.</param>
    [Public]
    public static void ExportDocumentDialog(List<IOfficialDocument> documents)
    {
      if (ClientApplication.ApplicationType == ApplicationType.Web)
        ExportDocumentDialogWithSearchInWeb(documents, false);
    }
    
    /// <summary>
    /// Выгрузка документов в веб-клиенте.
    /// </summary>
    /// <param name="documentList">Список документов.</param>
    /// <param name="onlySearch">Признак "Только поиск". Если установлен в True, выгрузка проводиться не будет.</param>
    /// <returns>Кверик документов для выгрузки.</returns>
    [Public]
    public static IQueryable<IOfficialDocument> ExportDocumentDialogWithSearchInWeb(List<IOfficialDocument> documentList, bool onlySearch)
    {
      int zipModelFilesCount = 0;
      long zipModelFilesSumSize = 0;
      bool zipModelFilesExportError = false;
      string addErrorMessage = string.Empty;
      var totalForDownloadDialogText = string.Empty;
      var typeValueChanged = false;
      Docflow.Structures.Module.IExportDialogSearch filter = null;
      Structures.Module.ExportResult exportResult = null;
      var documents = documentList != null ? documentList.AsQueryable() : null;
      var documentCount = documents != null ? documents.Count() : 0;
      var canSearch = documents == null;
      IQueryable<IOfficialDocument> returned = null;
      var reportData = Structures.Module.AfterExportDialog.Create();
      reportData.PathToRoot = ".";
      reportData.Documents = new List<Structures.Module.ExportedDocument>();
      var documentsToPrepare = new List<int>();
      
      var isSingleExport = documentList != null && documentList.Count() == 1 &&
        (Contracts.IncomingInvoices.Is(documentList.FirstOrDefault()) ||
         Contracts.OutgoingInvoices.Is(documentList.FirstOrDefault()) ||
         !AccountingDocumentBases.Is(documentList.FirstOrDefault())) &&
        !Sungero.Contracts.ContractualDocuments.Is(documentList.FirstOrDefault());

      var search = canSearch;
      var start = !canSearch;
      var end = false;
      var step = 1;

      var dialog = Dialogs.CreateInputDialog(onlySearch ?
                                             Resources.ExportDialog_Search_Title :
                                             Resources.ExportDialog_Title);
      // Размеры подобраны на глаз.
      dialog.Height = canSearch ? (onlySearch ? 0 : 220) : 160;
      
      // Принудительно увеличиваем ширину диалога для корректного отображения кнопок.
      var fakeControl = dialog.AddString("123456789012345", false);
      fakeControl.IsVisible = false;
      
      dialog.HelpCode = onlySearch ? Constants.AccountingDocumentBase.HelpCodes.Search : Constants.AccountingDocumentBase.HelpCodes.Export;
      
      var type = dialog.AddSelect(Resources.ExportDialog_Format, true, Resources.ExportDialog_Format_Formalized)
        .From(Resources.ExportDialog_Format_Formalized, Resources.ExportDialog_Format_Print);

      var group = dialog.AddSelect(Resources.ExportDialog_Group, true, Resources.ExportDialog_Group_None)
        .From(Resources.ExportDialog_Group_None,
              Resources.ExportDialog_Group_Counterparty,
              Resources.ExportDialog_Group_DocumentType);
      var addAddendum = dialog.AddBoolean(Sungero.Docflow.Resources.ExportDialog_AddAddendum, true);
      
      var properties = AccountingDocumentBases.Info.Properties;
      var unit = dialog.AddSelect(properties.BusinessUnit.LocalizedName, !onlySearch, Company.BusinessUnits.Null);
      var counterparty = dialog.AddSelect(properties.Counterparty.LocalizedName, false, Parties.Counterparties.Null);
      
      var contract = dialog.AddSelect(Resources.ExportDialog_Search_Contract, false, Contracts.ContractualDocuments.Null)
        .Where(c => (unit.Value == null || Equals(c.BusinessUnit, unit.Value)) && (counterparty.Value == null || Equals(c.Counterparty, counterparty.Value)));

      var allowedKinds = new List<IDocumentKind>();
      
      var allowedAccountingDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(IAccountingDocumentBase))
        .Where(k => !Equals(k.DocumentType.DocumentTypeGuid, Constants.AccountingDocumentBase.IncomingInvoiceGuid) &&
               !Equals(k.DocumentType.DocumentTypeGuid, Constants.AccountingDocumentBase.OutgoingInvoiceGuid));
      allowedKinds.AddRange(allowedAccountingDocumentKinds);
      
      var allowedContractualDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(IContractualDocumentBase));
      allowedKinds.AddRange(allowedContractualDocumentKinds);
      allowedKinds = allowedKinds.OrderBy(k => k.Name).ToList();
      
      var kinds = dialog.AddSelectMany(Resources.ExportDialog_Search_DocumentKinds, false, Docflow.DocumentKinds.Null)
        .From(allowedKinds);
      var dateFrom = dialog.AddDate(Resources.ExportDialog_Search_DateFrom, false);
      var dateTo = dialog.AddDate(Resources.ExportDialog_Search_DateTo, false);
      
      var showDocs = dialog.Buttons.AddCustom(Resources.ExportDialog_Search_Show);
      var back = dialog.Buttons.AddCustom(Resources.ExportDialog_Back);
      back.IsVisible = canSearch;
      var next = dialog.Buttons.AddCustom(Resources.ExportDialog_StartExport);
      var cancel = dialog.Buttons.AddCustom(Resources.ExportDialog_Close);
      
      Action showAllDocuments = () => documents.Show();
      
      // Фильтрация договоров по НОР и контрагентам.
      unit.SetOnValueChanged(u =>
                             {
                               if (u.NewValue != null && contract.Value != null && !Equals(contract.Value.BusinessUnit, u.NewValue))
                                 contract.Value = null;
                             });
      counterparty.SetOnValueChanged(cp =>
                                     {
                                       if (cp.NewValue != null && contract.Value != null && !Equals(contract.Value.Counterparty, cp.NewValue))
                                         contract.Value = null;
                                     });
      contract.SetOnValueChanged(c =>
                                 {
                                   if (c.NewValue != null)
                                   {
                                     unit.Value = c.NewValue.BusinessUnit;
                                     counterparty.Value = c.NewValue.Counterparty;
                                   }
                                 });
      
      type.SetOnValueChanged(t =>
                             {
                               totalForDownloadDialogText = string.Empty;
                               typeValueChanged = true;
                             });
      
      #region Refresh
      
      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = (r) =>
      {
        if (!onlySearch)
        {
          if (search)
          {
            dialog.Text = Resources.ExportDialog_Step_SearchFormat(step);
            dialog.Text += Environment.NewLine;
          }
          else if (start)
          {
            dialog.Text = Resources.ExportDialog_Step_Config_WebFormat(step);
            dialog.Text += Environment.NewLine + Environment.NewLine;
            
            dialog.Text += Resources.ExportDialog_OpenDocumentsFormat(documentCount);
            
            dialog.Text += totalForDownloadDialogText;
            
            if (documentCount > Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit && start && step == 1)
            {
              r.AddError(Resources.ExportDialog_Error_DocumentCountLimitFormat(Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit));
            }
          }
          else if (end)
          {
            if (reportData.Documents.Any(d => !d.IsFaulted))
            {
              dialog.Text = Resources.ExportDialog_Step_End_WebFormat(step);
              dialog.Text += Environment.NewLine + Environment.NewLine;
              dialog.Text += Resources.ExportDialog_DocumentsSuccessfullyPreparedForDownload;
            }
            else if (reportData.Documents.Any(d => d.IsFaulted))
            {
              dialog.Text = Resources.ExportDialog_Step_End_Report_WebFormat(step);
              dialog.Text += Environment.NewLine + Environment.NewLine;
              dialog.Text += Resources.ExportDialog_CompletedNotExported;
            }
            
            dialog.Text += Environment.NewLine + Environment.NewLine;
            dialog.Text += Resources.ExportDialog_End_AllDocs_WebFormat(reportData.Documents.Count(d => !d.IsAddendum));
            dialog.Text += Environment.NewLine;
            dialog.Text += Resources.ExportDialog_End_FormalizedDocsFormat(reportData.Documents.Count(d => !d.IsAddendum && !d.IsFaulted && d.IsFormalized));
            dialog.Text += Environment.NewLine;
            dialog.Text += Resources.ExportDialog_End_NonformalizedDocsFormat(reportData.Documents.Count(d => !d.IsAddendum && !d.IsFaulted && !d.IsFormalized));
            if (reportData.Documents.Any(d => !d.IsAddendum && d.IsFaulted))
            {
              dialog.Text += Environment.NewLine;
              dialog.Text += Resources.ExportDialog_End_NotExportedDocs_WebFormat(reportData.Documents.Count(d => !d.IsAddendum && d.IsFaulted));
            }
            
            if (reportData.Documents.Any(d => d.IsAddendum))
            {
              dialog.Text += Environment.NewLine + Environment.NewLine;
              dialog.Text += Resources.ExportDialog_End_AllAddendumsFormat(reportData.Documents.Count(d => d.IsAddendum));
              dialog.Text += Environment.NewLine;
              dialog.Text += Resources.ExportDialog_End_FormalizedDocsFormat(reportData.Documents.Count(d => d.IsAddendum && !d.IsFaulted && d.IsFormalized));
              dialog.Text += Environment.NewLine;
              dialog.Text += Resources.ExportDialog_End_NonformalizedDocsFormat(reportData.Documents.Count(d => d.IsAddendum && !d.IsFaulted && !d.IsFormalized));
              if (reportData.Documents.Any(d => d.IsAddendum && d.IsFaulted))
              {
                dialog.Text += Environment.NewLine;
                dialog.Text += Resources.ExportDialog_End_NotExportedDocs_WebFormat(reportData.Documents.Count(d => d.IsAddendum && d.IsFaulted));
              }
            }
          }
        }
        
        group.IsVisible = start && !isSingleExport;
        type.IsVisible = start;
        addAddendum.IsVisible = start;
        group.IsEnabled = documentCount != 0 && documentCount <= Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit;
        type.IsEnabled = documentCount != 0 && documentCount <= Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit;
        
        unit.IsVisible = search;
        counterparty.IsVisible = search;
        dateFrom.IsVisible = search;
        dateTo.IsVisible = search;
        kinds.IsVisible = search;
        contract.IsVisible = search;
        showDocs.IsVisible = (search && onlySearch || start) && !isSingleExport;
        showDocs.IsEnabled = start ? documentCount != 0 : !end;
        
        next.IsVisible = !onlySearch;
        back.IsVisible = start && canSearch || end && reportData.Documents.All(d => d.IsFaulted);
        next.IsEnabled = start
          ? (documentCount != 0 && documentCount <= Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit && (!zipModelFilesExportError || typeValueChanged))
          : (end ? reportData.Documents.Any(d => !d.IsFaulted) : true);
        
        back.Name = end ? Resources.ExportDialog_End_Report : Resources.ExportDialog_Back;
        next.Name = end ? Resources.ExportDialog_StartExport :
          Resources.ExportDialog_ConfigExport;
        cancel.Name = end ? Resources.ExportDialog_Close : Resources.ExportDialog_Cancel;
        showDocs.Name = search ? Resources.ExportDialog_Search_OnlySearch :
          (start ? Resources.ExportDialog_Search_Show : Resources.ExportDialog_End_Report);
      };
      
      dialog.SetOnRefresh(refresh);
      
      #endregion
      
      IZip zip = null;
      dialog.SetOnButtonClick(
        (h) =>
        {
          if (h.Button == next || h.Button == back || (h.Button == showDocs && !onlySearch))
            h.CloseAfterExecute = false;
          
          #region Экран с результатами выгрузки

          if (end && h.Button == next)
          {
            zip.Export();
          }
          else if (end && h.Button == back)
          {
            var now = Calendar.UserNow;
            var report = Functions.Module.GetFinArchiveExportReport(exportResult.ExportedDocuments, now);
            report.Open();
          }
          
          #endregion

          #region Экран параметров выгрузки
          
          if (start)
          {
            if (h.Button == next && h.IsValid)
            {
              typeValueChanged = false;
              var parameters = Structures.Module.ExportDialogParams
                .Create(group.Value == Resources.ExportDialog_Group_Counterparty,
                        group.Value == Resources.ExportDialog_Group_DocumentType,
                        type.Value == Resources.ExportDialog_Format_Print,
                        isSingleExport, addAddendum.Value.Value);
              var initialize = Structures.Module.AfterExportDialog
                .Create(string.Empty, string.Empty, Calendar.UserNow, new List<Structures.Module.ExportedDocument>());
              try
              {
                initialize = filter != null ?
                  Functions.Module.Remote.PrepareExportDocumentDialogDocuments(documentsToPrepare, parameters) :
                  Functions.Module.Remote.PrepareExportDocumentDialogDocuments(documentList.Select(x => x.Id).ToList(), parameters);
              }
              catch (Exception ex)
              {
                Logger.Error("Не удалось подготовить данные для выгрузки", ex);
                addErrorMessage = Resources.ExportDialog_Error_Client_NoReason_Web;
                h.AddError(addErrorMessage);
                return;
              }
              
              exportResult = Functions.Module.Remote.AfterExportDocumentDialogToWeb(initialize.Documents, parameters);
              
              zipModelFilesCount = exportResult.ZipModels.Count;
              zipModelFilesSumSize = exportResult.ZipModels.Sum(m => m.Size);
              
              if (zipModelFilesCount != 0)
              {
                var filesSumSize = (double)zipModelFilesSumSize / Constants.AccountingDocumentBase.ConvertMb;
                if (filesSumSize < 0.1)
                  filesSumSize = 0.1;

                totalForDownloadDialogText = Environment.NewLine + Environment.NewLine +
                  Resources.ExportDialog_TotalForDownloadFormat(zipModelFilesCount, filesSumSize.ToString("0.#"));
              }
              
              if (zipModelFilesCount > Constants.AccountingDocumentBase.ExportedFilesCountMaxLimit)
              {
                addErrorMessage = Resources.ExportDialog_Error_ExportedFilesLimitFormat(Constants.AccountingDocumentBase.ExportedFilesCountMaxLimit);
                h.AddError(addErrorMessage);
                zipModelFilesExportError = true;
                return;
              }
              else if (zipModelFilesSumSize > Constants.AccountingDocumentBase.ExportedFilesSizeMaxLimitMb * Constants.AccountingDocumentBase.ConvertMb)
              {
                addErrorMessage = Sungero.Docflow.Resources.ExportDialog_Error_ExportedSizeLimitFormat(Constants.AccountingDocumentBase.ExportedFilesSizeMaxLimitMb);
                h.AddError(addErrorMessage);
                zipModelFilesExportError = true;
                return;
              }
              
              if (exportResult.ZipModels != null && exportResult.ZipModels.Any() && exportResult.ExportedDocuments != null && exportResult.ExportedDocuments.Any())
              {
                try
                {
                  zip = Functions.Module.Remote.CreateZipFromZipModel(exportResult.ZipModels, exportResult.ExportedDocuments, initialize.RootFolder);
                }
                catch (Exception ex)
                {
                  Logger.Error("Не удалось подготовить zip-архив для выгрузки", ex);
                  addErrorMessage = Resources.ExportDialog_Error_Client_NoReason_Web;
                  h.AddError(addErrorMessage);
                  zipModelFilesExportError = true;
                  return;
                }
              }
              
              var result = exportResult.ExportedDocuments;
              if (result.Any())
              {
                var faultedDocuments = reportData.Documents.Where(d => d.IsFaulted).Select(d => d.Id);
                var addendaFaulted = result.Where(d => !d.IsFaulted && d.IsAddendum && d.LeadDocumentId != null && faultedDocuments.Contains(d.LeadDocumentId.Value));
                foreach (var addendum in addendaFaulted)
                {
                  addendum.IsFaulted = true;
                  addendum.Error = Resources.ExportDialog_Error_LeadDocumentNoVersion;
                }
                reportData.Documents.AddRange(result);
              }
              
              back.IsEnabled = true;
              end = true;
              step += 1;
              start = false;
              refresh.Invoke(null);
            }
            
            if (h.Button == back)
            {
              search = true;
              start = false;
              step -= 1;
              zipModelFilesCount = 0;
              zipModelFilesExportError = false;
              typeValueChanged = false;
              addErrorMessage = string.Empty;
              totalForDownloadDialogText = string.Empty;
              refresh.Invoke(null);
            }
            
            if (h.Button == showDocs)
            {
              if (!string.IsNullOrEmpty(addErrorMessage))
                h.AddError(addErrorMessage);
              
              showAllDocuments();
            }
          }
          
          #endregion
          
          #region Экран поиска
          
          if (search)
          {
            
            if ((h.Button == next || h.Button == showDocs) &&
                dateTo.Value != null && dateFrom.Value != null && dateTo.Value < dateFrom.Value)
            {
              h.AddError(Sungero.Docflow.Resources.ExportDialog_Error_WrongDatePeriod);
              return;
            }
            
            if (h.Button == cancel)
            {
              unit.IsRequired = false;
              return;
            }
            
            filter = Docflow.Structures.Module.ExportDialogSearch
              .Create(unit.Value, counterparty.Value, contract.Value, dateFrom.Value, dateTo.Value, kinds.Value.ToList());
            if (h.Button == showDocs)
              returned = Functions.Module.Remote.SearchByRequisites(filter);
            
            if (h.Button == next)
            {
              if (h.IsValid)
              {
                var newDocuments = Functions.Module.Remote.SearchByRequisites(filter);
                documents = newDocuments;
                documentCount = documents.Count();
                documentsToPrepare = newDocuments.Select(x => x.Id).ToList();
                search = false;
                start = true;
                step += 1;
                if (documentCount > Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit)
                {
                  addErrorMessage = Resources.ExportDialog_Error_DocumentCountLimitFormat(Constants.AccountingDocumentBase.ExportedDocumentsCountMaxLimit);
                  h.AddError(addErrorMessage);
                }
                refresh.Invoke(null);
              }
            }
          }
          
          #endregion
          
        });
      dialog.Show();
      
      return returned;
    }
    
    /// <summary>
    /// Поиск документов в архиве.
    /// </summary>
    /// <returns>Кверик документов для выгрузки.</returns>
    private static IQueryable<IOfficialDocument> ExportDocumentDialogWithSearch()
    {
      Docflow.Structures.Module.IExportDialogSearch filter = null;
      IQueryable<IOfficialDocument> returned = null;

      var dialog = Dialogs.CreateInputDialog(Resources.ExportDialog_Search_Title);
      // Размеры подобраны на глаз.
      dialog.Height = 0;
      dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.Search;
      
      var properties = AccountingDocumentBases.Info.Properties;
      var unit = dialog.AddSelect(properties.BusinessUnit.LocalizedName, false, Company.BusinessUnits.Null);
      var counterparty = dialog.AddSelect(properties.Counterparty.LocalizedName, false, Parties.Counterparties.Null);
      
      var contract = dialog.AddSelect(Resources.ExportDialog_Search_Contract, false, Contracts.ContractualDocuments.Null)
        .Where(c => (unit.Value == null || Equals(c.BusinessUnit, unit.Value)) && (counterparty.Value == null || Equals(c.Counterparty, counterparty.Value)));

      var allowedKinds = new List<IDocumentKind>();
      
      var allowedAccountingDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(IAccountingDocumentBase))
        .Where(k => !Equals(k.DocumentType.DocumentTypeGuid, Constants.AccountingDocumentBase.IncomingInvoiceGuid) &&
               !Equals(k.DocumentType.DocumentTypeGuid, Constants.AccountingDocumentBase.OutgoingInvoiceGuid));
      allowedKinds.AddRange(allowedAccountingDocumentKinds);
      
      var allowedContractualDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(typeof(IContractualDocumentBase));
      allowedKinds.AddRange(allowedContractualDocumentKinds);
      allowedKinds = allowedKinds.OrderBy(k => k.Name).ToList();
      
      var kinds = dialog.AddSelectMany(Resources.ExportDialog_Search_DocumentKinds, false, Docflow.DocumentKinds.Null)
        .From(allowedKinds);
      var dateFrom = dialog.AddDate(Resources.ExportDialog_Search_DateFrom, false);
      var dateTo = dialog.AddDate(Resources.ExportDialog_Search_DateTo, false);
      
      var showDocs = dialog.Buttons.AddCustom(Resources.ExportDialog_Search_OnlySearch);
      var cancel = dialog.Buttons.AddCustom(Resources.ExportDialog_Cancel);
      
      // Фильтрация договоров по НОР и контрагентам.
      unit.SetOnValueChanged(u =>
                             {
                               if (u.NewValue != null && contract.Value != null && !Equals(contract.Value.BusinessUnit, u.NewValue))
                                 contract.Value = null;
                             });
      counterparty.SetOnValueChanged(cp =>
                                     {
                                       if (cp.NewValue != null && contract.Value != null && !Equals(contract.Value.Counterparty, cp.NewValue))
                                         contract.Value = null;
                                     });
      contract.SetOnValueChanged(c =>
                                 {
                                   if (c.NewValue != null)
                                   {
                                     unit.Value = c.NewValue.BusinessUnit;
                                     counterparty.Value = c.NewValue.Counterparty;
                                   }
                                 });
      
      dialog.SetOnButtonClick(
        (h) =>
        {
          #region Экран поиска
          if (h.Button == showDocs && dateTo.Value != null && dateFrom.Value != null && dateTo.Value < dateFrom.Value)
          {
            h.AddError(Sungero.Docflow.Resources.ExportDialog_Error_WrongDatePeriod);
            return;
          }
          
          if (h.Button == cancel)
          {
            unit.IsRequired = false;
            return;
          }
          
          filter = Docflow.Structures.Module.ExportDialogSearch
            .Create(unit.Value, counterparty.Value, contract.Value, dateFrom.Value, dateTo.Value, kinds.Value.ToList());
          if (h.Button == showDocs)
          {
            returned = Functions.Module.Remote.SearchByRequisites(filter);
          }
          #endregion
        });
      
      dialog.Show();
      
      return returned;
    }
    
    /// <summary>
    /// Проверить, приобретена ли лицензия на модуль Финансовый архив.
    /// </summary>
    /// <returns>True - если лицензия есть, иначе - false.</returns>
    [Public]
    public bool CheckFinancialArchiveLicense()
    {
      var moduleGuid = Constants.AccountingDocumentBase.FinancialArchiveUIGuid;
      if (!Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(moduleGuid))
      {
        Dialogs.NotifyMessage(Resources.NoFinancialArchiveLicense);
        return false;
      }
      return true;
    }
    
    #endregion
    
    #region Номенклатура дел
    
    /// <summary>
    /// Копирование номенклатуры дел на основании предыдущего периода.
    /// </summary>
    public virtual void CopyCaseFiles()
    {
      if (Users.Current.IsSystem != true &&
          !PublicFunctions.Module.Remote.IncludedInClerksRole())
      {
        Dialogs.ShowMessage(CaseFiles.Resources.CopyCaseFilesAccessMessage);
        return;
      }
      
      var copyStarted = this.ShowCaseFilesCopyDialog();
      if (copyStarted)
        Dialogs.NotifyMessage(CaseFiles.Resources.CopyCaseFilesNotifyMessage);
    }
    
    /// <summary>
    /// Показать диалог копирования номенклатуры.
    /// </summary>
    /// <returns>Копирование запущено: Да/Нет.</returns>
    public virtual bool ShowCaseFilesCopyDialog()
    {
      var dialog = Dialogs.CreateInputDialog(CaseFiles.Resources.CopyCaseFilesDialogTitle);
      dialog.HelpCode = Constants.CaseFile.CopyFilesDialogHelpCode;
      
      var targetYear = dialog.AddDate(CaseFiles.Resources.CopyCaseFilesDialogTargetYear, true,
                                      Calendar.Now.AddYears(1)).AsYear();
      var sourcePeriodStartDate = dialog.AddDate(CaseFiles.Resources.CopyCaseFilesDialogSourcePeriodStartDate, true,
                                                 Calendar.BeginningOfYear(Calendar.Today));
      var sourcePeriodEndDate = dialog.AddDate(CaseFiles.Resources.CopyCaseFilesDialogSourcePeriodEndDate, true,
                                               Calendar.EndOfYear(Calendar.Today));
      var defaultBusinessUnit = PublicFunctions.Module.GetDefaultBusinessUnit(Company.Employees.Current);
      var businessUnit = dialog.AddSelect(CaseFiles.Resources.CopyCaseFilesDialogBusinessUnit, false,
                                          defaultBusinessUnit);
      var department = dialog.AddSelect(CaseFiles.Resources.CopyCaseFilesDialogDepartment, false,
                                        Company.Departments.Null);
      if (defaultBusinessUnit != null)
        department.From(Company.PublicFunctions.BusinessUnit.Remote.GetAllDepartments(defaultBusinessUnit));
      
      var copyButton = dialog.Buttons.AddCustom(CaseFiles.Resources.CopyCaseFilesCopyButtonName);
      dialog.Buttons.Default = copyButton;
      dialog.Buttons.AddCancel();
      
      businessUnit.SetOnValueChanged((e) =>
                                     {
                                       department.Value = Company.Departments.Null;
                                       if (e.NewValue != null)
                                         department.From(Company.PublicFunctions.BusinessUnit.Remote.GetAllDepartments(e.NewValue));
                                       else
                                         department.From(Company.PublicFunctions.Department.Remote.GetVisibleDepartments());
                                     });
      
      dialog.SetOnRefresh((e) =>
                          {
                            copyButton.IsEnabled = true;
                            
                            // Если для исходного периода дата конца меньше даты начала,
                            // то кнопка "Создать" становится неактивной,
                            // а пользователю выводится соответствующее уведомление.
                            if (sourcePeriodStartDate.Value != null &&
                                sourcePeriodEndDate.Value != null &&
                                sourcePeriodEndDate.Value <= sourcePeriodStartDate.Value)
                            {
                              copyButton.IsEnabled = false;
                              e.AddError(CaseFiles.Resources.IncorrectDatesInSourcePeriod,
                                         sourcePeriodStartDate,
                                         sourcePeriodEndDate);
                              return;
                            }
                            
                            // Если год целевого периода меньше года исходного периода,
                            // то кнопка "Создать" становится неактивной,
                            // а пользователю выводится соответствующее уведомление.
                            if (sourcePeriodEndDate.Value != null &&
                                targetYear.Value != null &&
                                targetYear.Value.Value.Year < sourcePeriodEndDate.Value.Value.Year)
                            {
                              copyButton.IsEnabled = false;
                              e.AddError(CaseFiles.Resources.IncorrectTargetYear, targetYear);
                              return;
                            }
                          });
      
      var copyStarted = false;
      if (dialog.Show() == copyButton)
      {
        var targetPeriod = this.GetCaseFilesCopyDialogTargetPeriod(targetYear.Value.Value, null, null);
        var businessUnitId = businessUnit.Value != null ? businessUnit.Value.Id : -1;
        var departmentId = department.Value != null ? department.Value.Id : -1;
        Functions.CaseFile.Remote.CopyCaseFilesAsync(sourcePeriodStartDate.Value.Value,
                                                     sourcePeriodEndDate.Value.Value,
                                                     targetPeriod.DateFrom,
                                                     targetPeriod.DateTo,
                                                     businessUnitId,
                                                     departmentId);
        copyStarted = true;
      }
      
      return copyStarted;
    }
    
    /// <summary>
    /// Получить целевой период копирования номенклатуры.
    /// </summary>
    /// <param name="year">Год.</param>
    /// <param name="quarter">Квартал.</param>
    /// <param name="month">Месяц.</param>
    /// <returns>Структура дат с/по.</returns>
    /// <remarks>Параметры для квартала и месяца добавлены для удобства перекрытия.</remarks>
    /// <remarks>Расчёт периода в коробочном решении не зависит от квартала и месяца.</remarks>
    public virtual Structures.Module.DateTimePeriod GetCaseFilesCopyDialogTargetPeriod(DateTime year,
                                                                                       int? quarter,
                                                                                       int? month)
    {
      var period = Structures.Module.DateTimePeriod.Create();
      period.DateFrom = Calendar.BeginningOfYear(year);
      period.DateTo = Calendar.EndOfYear(year);
      return period;
    }
    
    #endregion
    
    #region Вызов отчетов
    
    /// <summary>
    /// Открыть отчет "Исполнительская дисциплина сотрудника".
    /// </summary>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    [Public]
    public virtual void OpenEmployeeAssignmentsReport(int employeeId, DateTime periodBegin, DateTime periodEnd)
    {
      var report = Sungero.Docflow.Reports.GetEmployeeAssignmentsReport();

      report.Employee = Company.PublicFunctions.Module.Remote.GetEmployeeById(employeeId);
      report.PeriodBegin = periodBegin;
      report.PeriodEnd = periodEnd;
      report.Open();
    }
    
    /// <summary>
    /// Открыть отчет "Исполнительская дисциплина сотрудника".
    /// </summary>
    /// <param name="employeeid">Ид сотрудника.</param>
    /// <param name="periodbegin">Начало периода.</param>
    /// <param name="periodend">Конец периода.</param>
    [Hyperlink]
    public void OpenEmployeeAssignmentsReport(string employeeid, string periodbegin, string periodend)
    {
      int employeeId;
      if (!int.TryParse(employeeid, out employeeId))
      {
        Logger.ErrorFormat("OpenReport. Failed parse id {0}", employeeid);
        return;
      }
      
      DateTime periodBegin;
      if (!Calendar.TryParseDateTime(periodbegin, out periodBegin))
      {
        Logger.ErrorFormat("OpenReport. Failed parse period begin {0}", periodbegin);
        return;
      }
      
      DateTime periodEnd;
      if (!Calendar.TryParseDateTime(periodend, out periodEnd))
      {
        Logger.ErrorFormat("OpenReport. Failed parse period end {0}", periodend);
        return;
      }
      
      Functions.Module.OpenEmployeeAssignmentsReport(employeeId, periodBegin, periodEnd);
    }
    
    /// <summary>
    /// Открыть отчет "Исполнительская дисциплина по сотрудникам".
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="businessUnitIds">Список ид НОР.</param>
    /// <param name="departmentIds">Список ид подразделений.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="widgetParameter">Локализованное имя параметра-перечисления виджетов.</param>
    /// <param name="unwrap">Разворачивать подчиненные подразделения.</param>
    /// <param name="withSubstitution">Учитывать замещение.</param>
    /// <param name="sortByAssignmentCompletion">Сортировать по исполнительской дисциплине.</param>
    [Public]
    public virtual void OpenEmployeesAssignmentCompletionReport(IBusinessUnit businessUnit, IDepartment department, List<int> businessUnitIds,
                                                                List<int> departmentIds, DateTime periodBegin, DateTime periodEnd,
                                                                string widgetParameter, bool unwrap, bool withSubstitution, bool sortByAssignmentCompletion)
    {
      var report = Sungero.Docflow.Reports.GetEmployeesAssignmentCompletionReport();
      
      if (departmentIds.Any())
        report.DepartmentIds.AddRange(departmentIds.Select(d => (int?)d).ToList());

      if (businessUnitIds.Any())
        report.BusinessUnitIds.AddRange(businessUnitIds.Select(d => (int?)d).ToList());
      
      if (businessUnit != null)
        report.BusinessUnit = businessUnit;
      
      if (department != null)
        report.Department = department;
      
      report.PeriodBegin = periodBegin;
      report.PeriodEnd = periodEnd;
      report.Unwrap = unwrap;
      report.WidgetParameter = widgetParameter;
      report.WithSubstitution = withSubstitution;
      report.SortByAssignmentCompletion = sortByAssignmentCompletion;
      report.Open();
    }
    
    /// <summary>
    /// Открыть отчет "Исполнительская дисциплина по сотрудникам".
    /// </summary>
    /// <param name="businessunitid">Ид нашей организации.</param>
    /// <param name="departmentid">Ид подразделения.</param>
    /// <param name="periodbegin">Начало периода.</param>
    /// <param name="periodend">Конец периода.</param>
    /// <param name="unwrap">Разворачивать подчиненные подразделения.</param>
    [Hyperlink]
    public void OpenEmployeesAssignmentCompletionReport(string businessunitid, string departmentid, string periodbegin, string periodend, string unwrap)
    {
      int businessUnitId;
      if (!int.TryParse(businessunitid, out businessUnitId))
      {
        Logger.ErrorFormat("OpenEmployeesAssignmentCompletionReport. Failed parse business unit id {0}", businessunitid);
        return;
      }

      int departmentId;
      if (!int.TryParse(departmentid, out departmentId))
      {
        Logger.ErrorFormat("OpenEmployeesAssignmentCompletionReport. Failed parse department id {0}", departmentid);
        return;
      }
      
      DateTime periodBegin;
      if (!Calendar.TryParseDateTime(periodbegin, out periodBegin))
      {
        Logger.ErrorFormat("OpenEmployeesAssignmentCompletionReport. Failed parse period begin {0}", periodbegin);
        return;
      }
      
      DateTime periodEnd;
      if (!Calendar.TryParseDateTime(periodend, out periodEnd))
      {
        Logger.ErrorFormat("OpenEmployeesAssignmentCompletionReport. Failed parse period end {0}", periodend);
        return;
      }
      
      bool withUnwrap;
      if (!bool.TryParse(unwrap, out withUnwrap))
      {
        Logger.ErrorFormat("OpenEmployeesAssignmentCompletionReport. Failed parse unwrap {0}", unwrap);
        return;
      }
      
      var departmentIds = departmentId == 0 ? new List<int>() : new List<int>() { departmentId };
      
      var businessUnitIds = businessUnitId == 0 ? new List<int>() : new List<int>() { businessUnitId };
      
      var businessUnit = businessUnitId != 0 ?
        Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(businessUnitId) :
        Company.BusinessUnits.Null;
      
      var department = departmentId != 0 ?
        Company.PublicFunctions.Department.Remote.GetDepartment(departmentId) :
        Company.Departments.Null;
      
      Functions.Module.OpenEmployeesAssignmentCompletionReport(businessUnit, department, businessUnitIds, departmentIds, periodBegin, periodEnd, null, withUnwrap, true, true);
    }
    
    /// <summary>
    /// Открыть отчет "Исполнительская дисциплина по подразделениям".
    /// </summary>
    /// <param name="departmentIds">Ид подразделений.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="widgetParameter">Локализованное имя параметра-перечисления виджетов.</param>
    /// <param name="unwrap">Разворачивать подчиненные подразделения.</param>
    /// <param name="withSubstitution">Учитывать замещение.</param>
    [Public]
    public virtual void OpenDepartmentsAssignmentCompletionReport(List<int> departmentIds, DateTime periodBegin, DateTime periodEnd, string widgetParameter, bool unwrap, bool withSubstitution)
    {
      var report = Sungero.Docflow.Reports.GetDepartmentsAssignmentCompletionReport();
      
      if (departmentIds.Any())
        report.DepartmentIds.AddRange(departmentIds.Select(d => (int?)d).ToList());
      report.PeriodBegin = periodBegin;
      report.PeriodEnd = periodEnd;
      report.Unwrap = unwrap;
      report.WidgetParameter = widgetParameter;
      report.WithSubstitution = withSubstitution;
      report.Open();
    }
    
    #endregion

  }
}
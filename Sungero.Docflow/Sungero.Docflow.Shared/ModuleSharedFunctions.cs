using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Workflow;

namespace Sungero.Docflow.Shared
{
  public class ModuleFunctions
  {
    #region Упаковка/распаковка результатов ремот-функций
    
    // TODO: посмотреть в сторону структур.
    
    /// <summary>
    /// Упаковка словаря в строку для передачи через Remote функции.
    /// </summary>
    /// <param name="result">Словарь.</param>
    /// <returns>Строка с упакованным словарем.</returns>
    [Public]
    public static string BoxToString(System.Collections.Generic.Dictionary<string, bool> result)
    {
      var valueDelimiter = "-";
      var rowDelimiter = "|";
      return string.Join(rowDelimiter, result.Select(d => string.Format("{0}{2}{1}", d.Key, d.Value, valueDelimiter)));
    }
    
    /// <summary>
    /// Распаковка словаря из строки.
    /// </summary>
    /// <param name="result">Строка с упакованным словарем.</param>
    /// <returns>Словарь.</returns>
    [Public]
    public static System.Collections.Generic.Dictionary<string, bool> UnboxDictionary(string result)
    {
      var valueDelimiter = '-';
      var rowDelimiter = '|';
      
      var dictionary = new Dictionary<string, bool>();
      foreach (var row in result.Split(rowDelimiter))
      {
        var key = row.Split(valueDelimiter)[0];
        var value = bool.Parse(row.Split(valueDelimiter)[1]);
        dictionary.Add(key, value);
      }
      
      return dictionary;
    }
    
    #endregion
    
    #region Копирование номенклатуры дел
    
    /// <summary>
    /// Получить сообщение о результатах копирования номенклатуры дел.
    /// </summary>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <param name="success">Количество успешно скопированных дел.</param>
    /// <param name="failed">Количество дел с ошибками при копировании.</param>
    /// <returns>Сообщение о результатах копирования номенклатуры дел.</returns>
    public virtual string GetCopyingCaseFilesTotalsMessage(DateTime targetPeriodStartDate,
                                                           DateTime targetPeriodEndDate,
                                                           int success,
                                                           int failed)
    {
      var year = this.GetCopyingCaseFilesTargetPeriodAsString(targetPeriodStartDate, targetPeriodEndDate);
      return this.AppendCaseFilesHyperlinkTo(Resources.CaseFileCopyingResultFormat(year, success));
    }
    
    /// <summary>
    /// Получить сообщение о том, что номенклатура дел была скопирована ранее.
    /// </summary>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <returns>Сообщение о том, что номенклатура дел была скопирована ранее.</returns>
    public virtual string GetAlreadyCopiedCaseFilesMessage(DateTime targetPeriodStartDate,
                                                           DateTime targetPeriodEndDate)
    {
      var year = this.GetCopyingCaseFilesTargetPeriodAsString(targetPeriodStartDate, targetPeriodEndDate);
      return this.AppendCaseFilesHyperlinkTo(Resources.CaseFileCopyingAlreadyDoneFormat(year));
    }
    
    /// <summary>
    /// Получить сообщение о том, что нет дел, соответствующих параметрам, для копирования.
    /// </summary>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <returns>Сообщение о том, что нет дел, соответствующих параметрам, для копирования.</returns>
    public virtual string GetNoCaseFilesToCopyMessage(DateTime targetPeriodStartDate,
                                                      DateTime targetPeriodEndDate)
    {
      var year = this.GetCopyingCaseFilesTargetPeriodAsString(targetPeriodStartDate, targetPeriodEndDate);
      return this.AppendCaseFilesHyperlinkTo(Resources.HasNoCaseFilesToCopyFormat(year));
    }
    
    /// <summary>
    /// Получить представление целевого периода копирования номенклатуры в виде строки.
    /// </summary>
    /// <param name="targetPeriodStartDate">Начало целевого периода.</param>
    /// <param name="targetPeriodEndDate">Конец целевого периода.</param>
    /// <returns>Представление целевого периода копирования номенклатуры в виде строки.</returns>
    public virtual string GetCopyingCaseFilesTargetPeriodAsString(DateTime targetPeriodStartDate,
                                                                  DateTime targetPeriodEndDate)
    {
      // Dmitriev_IA: Даты начала и конца целевого периода копирования формируются программно
      //              в клиентской функции GetCaseFilesCopyDialogTargetPeriod() модуля Docflow
      //              и всегда принадлежат одному году.
      return targetPeriodStartDate.Year.ToString();
    }
    
    /// <summary>
    /// Добавить гиперссылку на номенклатуру дел к строке.
    /// </summary>
    /// <param name="source">Исходная строка.</param>
    /// <returns>Строка, дополненная ссылкой на номенклатуру дел.</returns>
    public virtual string AppendCaseFilesHyperlinkTo(string source)
    {
      return string.Format("{0}{1}{2}", source, Environment.NewLine, Hyperlinks.Get(CaseFiles.Info));
    }
    
    #endregion
    
    /// <summary>
    /// Проверить наличие подчиненных поручений.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True, если есть подпоручения, иначе false.</returns>
    public static bool HasSubActionItems(ITask task)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.HasSubActionItems(task);
    }
    
    /// <summary>
    /// Проверить наличие подчиненных поручений.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="status">Статус поручения.</param>
    /// <returns>True, если есть подпоручения, иначе false.</returns>
    public static bool HasSubActionItems(ITask task, Enumeration status)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.HasSubActionItems(task, status);
    }
    
    /// <summary>
    /// Проверить наличие подчиненных поручений.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="status">Статус поручения.</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>True, если есть подпоручения, иначе false.</returns>
    public static bool HasSubActionItems(ITask task, Enumeration status, IEmployee addressee)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.HasSubActionItems(task, status, addressee);
    }
    
    /// <summary>
    /// Проверить, завершена ли задача.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>True, если задача завершена, иначе - False.</returns>
    [Public]
    public virtual bool IsTaskCompleted(ITask task)
    {
      var result = task.Status == Workflow.Task.Status.Completed ||
        task.Status == Workflow.Task.Status.Aborted;
      return result;
    }
    
    /// <summary>
    /// Получить список поручений для формирования блока резолюции задачи на согласование.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="status">Статус поручений (исключаемый).</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>Список поручений.</returns>
    public static List<ITask> GetActionItemsForResolution(ITask task, Enumeration status, IEmployee addressee)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.GetActionItemsForResolution(task, status, addressee);
    }
    
    /// <summary>
    /// Получить информацию по поручению для вывода резолюции.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <returns>Список значений строк:
    /// текст поручения;
    /// исполнитель (-и);
    /// срок;
    /// контролер.</returns>
    public static List<string> ActionItemInfoProvider(ITask task)
    {
      return RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.ActionItemInfoProvider(task);
    }
    
    /// <summary>
    /// Показать сообщение Dialog.NotifyMessage через Reflection.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    [Public]
    public static void TryToShowNotifyMessage(string message)
    {
      var dialogs = Type.GetType("Sungero.Core.Dialogs, Sungero.Domain.ClientBase");
      if (dialogs != null)
        dialogs.InvokeMember("NotifyMessage", System.Reflection.BindingFlags.InvokeMethod, null, null, new string[1] { message });
    }
    
    /// <summary>
    /// Получить последнего утвердившего документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Подписавший.</returns>
    public static IEmployee GetDocumentLastApprover(IElectronicDocument document)
    {
      return Employees.As(Signatures.Get(document.LastVersion)
                          .Where(s => s.SignatureType == SignatureType.Approval)
                          .OrderByDescending(s => s.SigningDate)
                          .Select(s => s.Signatory)
                          .FirstOrDefault());
    }

    /// <summary>
    /// Подобрать для документа подходящее хранилище.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Наиболее подходящее хранилище для документа.</returns>
    public virtual IStorage GetStorageByPolicies(IOfficialDocument document)
    {
      var policies = StoragePolicies.GetAllCached().Where(r => r.Status == Docflow.StoragePolicyBase.Status.Active &&
                                                          (document.DocumentKind == null ||
                                                           !r.DocumentKinds.Any() ||
                                                           r.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind))));

      var policy = policies.OrderByDescending(p => p.Priority).FirstOrDefault();
      if (policy != null)
        return policy.Storage;
      
      return null;
    }
    
    /// <summary>
    /// Привести дату к тенантному времени.
    /// </summary>
    /// <param name="datetime">Дата.</param>
    /// <returns>Дата во времени тенанта.</returns>
    [Public]
    public static DateTime ToTenantTime(DateTime datetime)
    {
      return datetime.Kind == DateTimeKind.Utc ? datetime.FromUtcTime() : datetime;
    }
    
    /// <summary>
    /// Заменить первый символ строки на прописной.
    /// </summary>
    /// <param name="label">Исходная строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string ReplaceFirstSymbolToUpperCase(string label)
    {
      if (string.IsNullOrWhiteSpace(label))
        return string.Empty;
      
      return string.Format("{0}{1}",
                           char.ToUpper(label[0]),
                           label.Length > 1 ? label.Substring(1) : string.Empty);
    }
    
    /// <summary>
    /// Проверить, состоит ли строка из ASCII символов.
    /// </summary>
    /// <param name="value"> Строка.</param>
    /// <returns> Результат.</returns>
    [Public]
    public static bool IsASCII(string value)
    {
      // Если длина строки в байтах = длине строки в символах, то строка состоит из аски символов.
      if (string.IsNullOrEmpty(value))
        return true;
      return System.Text.Encoding.UTF8.GetByteCount(value) == value.Length;
    }
    
    /// <summary>
    /// Заменить первый символ строки на строчный.
    /// </summary>
    /// <param name="label">Исходная строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string ReplaceFirstSymbolToLowerCase(string label)
    {
      if (string.IsNullOrWhiteSpace(label))
        return string.Empty;
      
      return string.Format("{0}{1}",
                           char.ToLower(label[0]),
                           label.Length > 1 ? label.Substring(1) : string.Empty);
    }
    
    /// <summary>
    /// Получить склоненное имя для числа.
    /// </summary>
    /// <param name="number">Число.</param>
    /// <param name="singleName">Имя в единственном числе.</param>
    /// <param name="genitiveName">Имя в родительном падеже.</param>
    /// <param name="pluralName">Имя во множественном числе.</param>
    /// <returns>Склоненное имя числа.</returns>
    [Public]
    public static string GetNumberDeclination(int number,
                                              CommonLibrary.LocalizedString singleName,
                                              CommonLibrary.LocalizedString genitiveName,
                                              CommonLibrary.LocalizedString pluralName)
    {
      // TODO: 35010.
      if (singleName.Culture.TwoLetterISOLanguageName == "ru")
      {
        number = number % 100;

        // Числа, заканчивающиеся на число с 11 до 19, всегда именовать в родительном падеже.
        if (number >= 11 && number <= 19)
          return pluralName;

        var i = number % 10;
        switch (i)
        {
          case 1:
            return singleName;
          case 2:
          case 3:
          case 4:
            return genitiveName;
          default:
            return pluralName;
        }
      }
      
      return number == 1 ? singleName : pluralName;
    }
    
    #region Работа с сотрудниками
    
    /// <summary>
    /// Получить секретаря руководителя.
    /// </summary>
    /// <param name="manager">Руководитель.</param>
    /// <returns>Секретарь.</returns>
    [Public]
    public static IEmployee GetSecretary(IEmployee manager)
    {
      var secretary = Employees.Null;
      if (manager != null)
        secretary = Company.PublicFunctions.Employee.GetManagerAssistants(manager)
          .Select(m => m.Assistant)
          .FirstOrDefault();
      return secretary;
    }
    
    /// <summary>
    /// Получить помощников руководителя.
    /// </summary>
    /// <param name="manager">Руководитель.</param>
    /// <returns>Список помощников руководителя.</returns>
    /// <remarks>Не используется, оставлен для совместимости.</remarks>
    [Public, Obsolete("Используйте разделяемый метод GetManagersAssistants справочника Employee.")]
    public static List<IManagersAssistant> GetSecretaries(IEmployee manager)
    {
      return Company.PublicFunctions.Employee.GetManagerAssistants(manager);
    }
    
    /// <summary>
    /// Получить начальника секретаря.
    /// </summary>
    /// <param name="secretary">Секретарь.</param>
    /// <returns>Руководитель.</returns>
    [Public]
    public static IEmployee GetSecretaryManager(IEmployee secretary)
    {
      return Sungero.Company.PublicFunctions.Module.Remote.GetActiveManagerAssistants()
        .Where(m => secretary.Equals(m.Assistant) && m.IsAssistant == true)
        .Select(m => m.Manager)
        .FirstOrDefault();
    }
    
    #endregion
    
    /// <summary>
    /// Сформировать название таблицы для отчета.
    /// </summary>
    /// <param name="reportName">Название отчёта.</param>
    /// <param name="userId">Id пользователя, запустившего отчёт.</param>
    /// <returns>Название таблицы вида "Sungero_Reports_{reportName}_{userId}_{randomNumber}".</returns>
    [Public]
    public static string GetReportTableName(string reportName, int userId)
    {
      var randomNumber = Math.Abs(Environment.TickCount);
      
      return string.Format("Sungero_Reports_{0}_{1}_{2}", reportName, userId, randomNumber);
    }
    
    /// <summary>
    /// Сформировать название таблицы для отчета.
    /// </summary>
    /// <param name="report">Отчёт.</param>
    /// <param name="userId">Id пользователя, запустившего отчёт.</param>
    /// <returns>Название таблицы вида "Sungero_Reports_{reportName}_{userId}_{randomNumber}".</returns>
    [Public]
    public static string GetReportTableName(Reporting.IReport report, int userId)
    {
      var reportName = report.Info.Name;
      return GetReportTableName(reportName, userId);
    }
    
    /// <summary>
    /// Сформировать название таблицы для отчета.
    /// </summary>
    /// <param name="report">Отчёт.</param>
    /// <param name="userId">Id пользователя, запустившего отчёт.</param>
    /// <param name="postfix">Постфикс таблицы.</param>
    /// <returns>Название таблицы вида "Sungero_Reports_{reportName}_{userId}_{postfix}".</returns>
    [Public]
    public static string GetReportTableName(Reporting.IReport report, int userId, string postfix)
    {
      var prefix = GetReportTableName(report, userId);
      
      if (string.IsNullOrWhiteSpace(postfix))
        return prefix;
      
      return string.Format("{0}_{1}", prefix, postfix);
    }
    
    /// <summary>
    /// Сформировать название таблицы для отчета.
    /// </summary>
    /// <param name="reportName">Название отчета.</param>
    /// <param name="userId">Id пользователя, запустившего отчёт.</param>
    /// <param name="postfix">Постфикс таблицы.</param>
    /// <returns>Название таблицы вида "Sungero_Reports_{reportName}_{userId}_{postfix}.</returns>
    [Public]
    public static string GetReportTableName(string reportName, int userId, string postfix)
    {
      var prefix = GetReportTableName(reportName, userId);
      
      if (string.IsNullOrWhiteSpace(postfix))
        return prefix;
      
      return string.Format("{0}_{1}", prefix, postfix);
    }
    
    /// <summary>
    /// Получить НОР сотрудника.
    /// Берется из настроек, либо определяется по оргструктуре.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Наша организация.</returns>
    [Public]
    public static Sungero.Company.IBusinessUnit GetDefaultBusinessUnit(Sungero.Company.IEmployee employee)
    {
      if (employee == null)
        return null;
      
      var setting = Functions.PersonalSetting.GetPersonalSettings(employee);
      return (setting != null && setting.BusinessUnit != null) ?
        setting.BusinessUnit :
        Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(employee);
    }
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    /// <param name="group">Группа вложения задачи.</param>
    /// <param name="document">Документ.</param>
    [Public]
    public virtual void SynchronizeAddendaAndAttachmentsGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group, IElectronicDocument document)
    {
      if (document == null)
      {
        foreach (var addendum in group.All)
          group.All.Remove(addendum);
        return;
      }

      // Получить все неустаревшие приложения документа.
      var documentAddenda = document.Relations.GetRelated(Docflow.Constants.Module.AddendumRelationName)
        .Where(x => OfficialDocuments.Is(x) &&
               !Docflow.PublicFunctions.OfficialDocument.IsObsolete(OfficialDocuments.As(x)));
      
      // Удалить лишние приложения задачи.
      foreach (var addendum in group.All.Select(e => ElectronicDocuments.As(e)).Where(d => d != null && !documentAddenda.Contains(d)))
        group.All.Remove(addendum);
      
      // Добавить в задачу недостающие приложения из документа.
      var newAddenda = documentAddenda.Where(d => !group.All.Contains(d)).ToList();
      foreach (var addendum in newAddenda)
        group.All.Add(addendum);
    }
    
    /// <summary>
    /// Получить документы, связанные типом связи "Приложение".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Документы, связанные типом связи "Приложение".</returns>
    /// <remarks>Возвращает только не устаревшие документы.</remarks>
    [Public]
    public virtual List<IElectronicDocument> GetAddenda(IElectronicDocument document)
    {
      var result = document.Relations.GetRelated(Docflow.Constants.Module.AddendumRelationName)
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .Where(x => !Functions.OfficialDocument.IsObsolete(x))
        .Select(x => ElectronicDocuments.As(x));
      
      return result.ToList();
    }
    
    #region TrimSpecialSymbols, TrimQuotes
    
    /// <summary>
    /// Убрать лишние кавычки и переносы строк.
    /// </summary>
    /// <param name="subject">Исходная строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string TrimSpecialSymbols(string subject)
    {
      subject = subject.Replace(Environment.NewLine, " ").Replace("\n", " ");
      subject = subject.Replace("   ", " ").Replace("  ", " ");
      subject = TrimQuotes(subject);
      
      return subject;
    }
    
    /// <summary>
    /// Убрать лишние кавычки и переносы строк.
    /// </summary>
    /// <param name="subject">Исходная строка с форматированием.</param>
    /// <param name="arg0">Аргумент.</param>
    /// <returns>Результирующая строка.</returns>
    /// TODO: Все TrimSpecialSymbols c аргументами подвержены FormatException.
    [Public]
    public static string TrimSpecialSymbols(string subject, object arg0)
    {
      return TrimSpecialSymbols(string.Format(subject, arg0));
    }
    
    /// <summary>
    /// Убрать лишние кавычки и переносы строк.
    /// </summary>
    /// <param name="subject">Исходная строка с форматированием.</param>
    /// <param name="arg0">Аргумент.</param>
    /// <param name="arg1">Второй аргумент.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string TrimSpecialSymbols(string subject, object arg0, object arg1)
    {
      return TrimSpecialSymbols(string.Format(subject, arg0, arg1));
    }
    
    /// <summary>
    /// Убрать лишние кавычки и переносы строк.
    /// </summary>
    /// <param name="subject">Исходная строка с форматированием.</param>
    /// <param name="args">Аргументы.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string TrimSpecialSymbols(string subject, object[] args)
    {
      return TrimSpecialSymbols(string.Format(subject, args));
    }
    
    /// <summary>
    /// Убрать лишние кавычки.
    /// </summary>
    /// <param name="row">Исходная строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string TrimQuotes(string row)
    {
      return row.Replace("\"\"\"", "\"").Replace("\"\"", "\"");
    }
    
    /// <summary>
    /// Убрать переносы строк в конце строки.
    /// </summary>
    /// <param name="row">Исходная строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string TrimEndNewLines(string row)
    {
      if (!string.IsNullOrEmpty(row))
        return row.TrimEnd(Environment.NewLine.ToCharArray());
      else
        return row;
    }
    
    #endregion
    
    /// <summary>
    /// Получить GUID действия.
    /// </summary>
    /// <param name="action">Действие.</param>
    /// <returns>Строка, содержащая GUID.</returns>
    public static string GetActionGuid(Domain.Shared.IActionInfo action)
    {
      var internalAction = action as Domain.Shared.IInternalActionInfo;
      return internalAction == null ? string.Empty : internalAction.NameGuid.ToString();
    }
    
    /// <summary>
    /// Получить действие по отправке документа.
    /// </summary>
    /// <param name="action">Информация о действии.</param>
    /// <returns>Действие по отправке документа.</returns>
    public static IDocumentSendAction GetSendAction(Domain.Shared.IActionInfo action)
    {
      return DocumentSendActions.GetAllCached(a => a.ActionGuid == Functions.Module.GetActionGuid(action)).Single();
    }
    
    /// <summary>
    /// Расcчитать задержку.
    /// </summary>
    /// <param name="deadline">Планируемый срок.</param>
    /// <param name="completed">Реальный срок.</param>
    /// <param name="user">Сотрудник.</param>
    /// <returns>Задержка.</returns>
    [Public]
    public virtual int CalculateDelay(DateTime? deadline, DateTime completed, IUser user)
    {
      // Для заданий без срока - просрочка невозможна.
      if (!deadline.HasValue)
        return 0;

      // Не просрочено.
      if (completed <= deadline)
        return 0;
      
      var delayInDays = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingDays(deadline.Value.Date, completed.Date, user);
      
      if (delayInDays <= 2)
      {
        // Для заданий без времени взять конец дня.
        var deadlineWithTime = Functions.Module.GetDateWithTime(deadline.Value, user);
        var completedWithTime = Functions.Module.GetDateWithTime(completed, user);
        // Просрочка менее чем на 4 рабочих часа считается за выполненное в срок задание.
        var delayInHours = Sungero.CoreEntities.WorkingTime.GetDurationInWorkingHours(deadlineWithTime, completedWithTime, user);
        if (delayInHours < 4)
          return 0;
      }
      
      // Вычислить просрочку. Просрочка минимум 1 день.
      var delay = delayInDays - 1;
      if (delay == 0)
        delay = 1;
      
      return delay;
    }
    
    /// <summary>
    /// Проверить корректность срока.
    /// </summary>
    /// <param name="user">Пользователь, по чьему календарю проверять срок.</param>
    /// <param name="deadline">Дата, которую сравниваем.</param>
    /// <param name="minDeadline">Минимально допустимая дата.</param>
    /// <returns>True, если сравниваемая дата больше допустимой.</returns>
    /// <remarks>Проверка на строго больше. При равных датах вернёт false.</remarks>
    /// <remarks>Если хоть одна дата не передана(null) - возвращается true.</remarks>
    [Public]
    public static bool CheckDeadline(IUser user, DateTime? deadline, DateTime? minDeadline)
    {
      if (minDeadline == null || deadline == null)
        return true;

      var minDeadlineWithTime = Functions.Module.GetDateWithTime(minDeadline.Value, user);
      var deadlineWithTime = Functions.Module.GetDateWithTime(deadline.Value, user);

      return deadlineWithTime > minDeadlineWithTime;
    }
    
    /// <summary>
    ///  Проверить корректность срока соисполнителей.
    /// </summary>
    /// <param name="coAssignees">Соисполнители.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителей.</param>
    /// <returns>True, если срок соисполнителей больше текущей даты.</returns>
    [Public]
    public virtual bool CheckCoAssigneesDeadline(List<Sungero.Company.IEmployee> coAssignees, DateTime? coAssigneesDeadline)
    {
      return coAssignees.All(c => Docflow.PublicFunctions.Module.CheckDeadline(c, coAssigneesDeadline, Calendar.Now));
    }
    
    /// <summary>
    /// Проверить сроки исполнителей. Срок соисполнителя должен быть меньше, либо равен сроку исполнителя.
    /// </summary>
    /// <param name="deadline">Срок исполнителя.</param>
    /// <param name="coAssigneesDeadline">Срок соисполнителя.</param>
    /// <returns>True, если срок соисполнителя меньше, либо равен сроку исполнителя.</returns>
    [Public]
    public virtual bool CheckAssigneesDeadlines(DateTime? deadline, DateTime? coAssigneesDeadline)
    {
      if (deadline == null || coAssigneesDeadline == null)
        return true;
      
      if (!deadline.Value.HasTime() && !coAssigneesDeadline.Value.HasTime())
        return deadline >= coAssigneesDeadline;
      
      deadline = deadline.Value.HasTime() ? deadline.ToUserTime() : deadline.Value.EndOfDay();
      coAssigneesDeadline = coAssigneesDeadline.Value.HasTime() ? coAssigneesDeadline.ToUserTime() : coAssigneesDeadline.Value.EndOfDay();

      return deadline >= coAssigneesDeadline;
    }
    
    /// <summary>
    /// Получить срок соисполнителей по умолчанию относительно срока исполнителя.
    /// </summary>
    /// <param name="deadline">Срок исполнителя.</param>
    /// <param name="controlRelativeDeadlineInDays">Дней на контроль.</param>
    /// <param name="controlRelativeDeadlineInHours">Часов на контроль.</param>
    /// <returns>Cрок соисполнителя с учетом срока на приемку.</returns>
    /// <remarks>Если вычисленный срок меньше текущей даты, то возвращается срок исполнителя.</remarks>
    [Public]
    public virtual DateTime? GetDefaultCoAssigneesDeadline(DateTime? deadline, int controlRelativeDeadlineInDays, int controlRelativeDeadlineInHours)
    {
      if (deadline.HasValue)
      {
        var relativeDeadline = deadline.Value;
        if (controlRelativeDeadlineInHours != 0)
          relativeDeadline = Functions.Module.GetDateWithTime(relativeDeadline, Users.Current);
        relativeDeadline = relativeDeadline.AddWorkingDays(Users.Current, controlRelativeDeadlineInDays);
        if (!deadline.Value.HasTime())
          relativeDeadline = Functions.Module.GetDateWithTime(relativeDeadline, Users.Current);
        relativeDeadline = relativeDeadline.AddWorkingHours(Users.Current, controlRelativeDeadlineInHours);
        
        if (!deadline.Value.HasTime() && controlRelativeDeadlineInHours == 0)
          relativeDeadline = relativeDeadline.Date;

        // Если срок соисполнителя после вычисления дельты меньше текущего времени, то проверяем срок исполнителя.
        // Если срок исполнителя - текущий день(со временем или без), то берем этот срок, иначе всегда Today без времени.
        var calculateDeadline = Docflow.PublicFunctions.Module.CheckDeadline(Users.Current, relativeDeadline, Calendar.Now) ? relativeDeadline : Calendar.Today;
        return Docflow.PublicFunctions.Module.CheckDeadline(Users.Current, calculateDeadline, deadline) ? deadline : calculateDeadline;
      }
      
      return null;
    }
    
    /// <summary>
    /// Проверить корректность срока.
    /// </summary>
    /// <param name="deadline">Дата, которую сравниваем.</param>
    /// <param name="minDeadline">Минимально допустимая дата.</param>
    /// <returns>True, если сравниваемая дата больше допустимой.</returns>
    /// <remarks>Проверка на строго больше. При равных датах вернёт false.</remarks>
    /// <remarks>Если хоть одна дата не передана(null) - возвращается true.</remarks>
    [Public]
    public static bool CheckDeadline(DateTime? deadline, DateTime? minDeadline)
    {
      if (minDeadline == null || deadline == null)
        return true;

      var minDeadlineWithTime = Functions.Module.GetDateWithTime(minDeadline.Value);
      var deadlineWithTime = Functions.Module.GetDateWithTime(deadline.Value);

      return deadlineWithTime > minDeadlineWithTime;
    }
    
    /// <summary>
    /// Валидация автора задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сообщение валидации, если автор не является сотрудником, иначе пустая строка.</returns>
    [Public]
    public static string ValidateTaskAuthor(ITask task)
    {
      if (!Sungero.Company.Employees.Is(task.Author))
        return Docflow.Resources.CantSendTaskByNonEmployee;
      
      return string.Empty;
    }
    
    /// <summary>
    /// Валидация автора задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если автор является сотрудником, иначе False.</returns>
    [Public]
    public static bool ValidateTaskAuthor(ITask task, Sungero.Core.IValidationArgs e)
    {
      var authorIsNonEmployeeMessage = ValidateTaskAuthor(task);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
      {
        e.AddError(task.Info.Properties.Author, authorIsNonEmployeeMessage);
        return false;
      }
      return true;
    }
    
    /// <summary>
    /// Сформировать имя документа для отчета.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="withHyperlink">Строка с гиперссылкой.</param>
    /// <returns>Имя документа в формате: Имя (ИД: 1, Версия 1/Без версии).</returns>
    [Public]
    public static string FormatDocumentNameForReport(Content.IElectronicDocument document, bool withHyperlink)
    {
      return PublicFunctions.Module.FormatDocumentNameForReport(document, 0, withHyperlink);
    }
    
    /// <summary>
    /// Сформировать имя документа для отчета.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Номер версии.</param>
    /// <param name="withHyperlink">Строка с гиперссылкой.</param>
    /// <returns>Имя документа в формате: Имя (ИД: 1, Версия 1/Без версии).</returns>
    [Public]
    public static string FormatDocumentNameForReport(Content.IElectronicDocument document, int version, bool withHyperlink)
    {
      var nonBreakingSpace = Convert.ToChar(160);
      
      // При использовании тегов к строке добавляются пробелы с обеих сторон, поэтому не ставится пробел в конечной строке.
      var documentId = withHyperlink
        ? string.Format(@"<font color=""blue""><u>{0}</u></font>", document.Id)
        : string.Format(@"{0}{1}", nonBreakingSpace, document.Id.ToString());
      var documentName = document.DisplayValue.Trim();
      var versionNumber = document.HasVersions && version < 1 ? document.LastVersion.Number : version;
      var documentVersion = versionNumber > 0
        ? string.Format("{1}{0}{2}", nonBreakingSpace, Docflow.Resources.Version, versionNumber)
        : Docflow.Resources.WithoutVersion;
      return string.Format("{1}{0}({2}:{3},{0}{4})",
                           nonBreakingSpace,
                           documentName,
                           Docflow.Resources.Id,
                           documentId,
                           documentVersion);
    }

    /// <summary>
    /// Получить дату со временем, день без времени вернет конец дня.
    /// </summary>
    /// <param name="date">Исходное время.</param>
    /// <param name="user">Пользователь.</param>
    /// <returns>Дата со временем.</returns>
    [Public]
    public static DateTime GetDateWithTime(DateTime date, IUser user)
    {
      return !date.HasTime() ? date.EndOfDay().FromUserTime(user) : date;
    }
    
    /// <summary>
    /// Получить дату со временем, день без времени вернет конец дня.
    /// </summary>
    /// <param name="date">Исходное время.</param>
    /// <returns>Дата со временем.</returns>
    [Public]
    public static DateTime GetDateWithTime(DateTime date)
    {
      return !date.HasTime() ? date.ToUserTime().EndOfDay().FromUserTime() : date;
    }

    /// <summary>
    /// Получить дату с припиской UTC.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Строковое представление даты с UTC.</returns>
    [Public]
    public static string GetDateWithUTCLabel(DateTime date)
    {
      var utcOffset = (Calendar.UserNow.Hour - Calendar.Now.Hour) + Calendar.UtcOffset.TotalHours;
      var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
      return string.Format("{0:g} (UTC{1})", date, utcOffsetLabel);
    }
    
    /// <summary>
    /// Преобразовать имя тенанта в строку для подстановки в ШК документа.
    /// </summary>
    /// <param name="tenant">Имя тенанта.</param>
    /// <returns>Идентификатор для подстановки в ШК.</returns>
    [Public]
    public static string FormatTenantIdForBarcode(string tenant)
    {
      return string.Format("{0, 10}", tenant).Substring(0, 10);
    }
    
    /// <summary>
    /// Создать отчет Журнал выгрузки документов из архива.
    /// </summary>
    /// <param name="objs">Структура данных для формирования отчета.</param>
    /// <param name="dateTimeNow">Дата и время формирования отчета.</param>
    /// <returns>Отчет.</returns>
    public static Sungero.FinancialArchive.IFinArchiveExportReport GetFinArchiveExportReport(List<Structures.Module.ExportedDocument> objs, DateTime dateTimeNow)
    {
      if (objs.Any())
      {
        var faultedDocuments = objs.Where(d => d.IsFaulted).Select(d => d.Id);
        var addendaFaulted = objs.Where(d => !d.IsFaulted && d.IsAddendum && d.LeadDocumentId != null && faultedDocuments.Contains(d.LeadDocumentId.Value));
        foreach (var addendum in addendaFaulted)
        {
          addendum.IsFaulted = true;
          addendum.Error = Resources.ExportDialog_Error_LeadDocumentNoVersion;
        }
      }

      var report = FinancialArchive.Reports.GetFinArchiveExportReport();
      report.CurrentTime = dateTimeNow;
      report.Exported = objs.Count(d => !d.IsFaulted);
      report.NotExported = objs.Count(d => d.IsFaulted);
      report.ReportSessionId = Functions.Module.Remote.GenerateFinArchiveExportReport(objs, ".");
      
      return report;
    }
    
    /// <summary>
    /// Собрать текстовое отображение адресатов.
    /// </summary>
    /// <param name="addressees">Список адресатов.</param>
    /// <param name="labelMaxLength">Максимальная длинна текстового отображения.</param>
    /// <returns>Текстовое отображение адресатов.</returns>
    public virtual string BuildManyAddresseesLabel(List<IEmployee> addressees, int labelMaxLength)
    {
      var addresseesNames = addressees.Select(x => x.DisplayValue);
      var separator = "; ";
      var additionalsFormat = " + {0}";
      var additionalsCount = addressees.Count;
      var label = string.Empty;

      // Поместить только тех адресатов, чьи ФИО помещаются полностью.
      foreach (var addressee in addresseesNames)
      {
        var predictedLabel = string.Join(separator, label, addressee).Trim(' ', ';');
        var predictedAdditionals = additionalsCount > 1 ?
          string.Format(additionalsFormat, additionalsCount - 1) :
          string.Empty;
        var predictedLength = predictedLabel.Length + predictedAdditionals.Length;

        if (predictedLength <= labelMaxLength)
        {
          label = predictedLabel;
          additionalsCount--;
        }
        else
        {
          break;
        }
      }
      
      // Дополнить конец строки количество не вошедших " + N".
      if (additionalsCount > 0)
        label = string.Format("{0}{1}", label, string.Format(additionalsFormat, additionalsCount));
      
      return label;
    }
    
    /// <summary>
    /// Получить имена соисполнителей.
    /// </summary>
    /// <param name="coAssignees">Соисполнители.</param>
    /// <param name="isFullName">Возвращать полные имена соисполнителей.</param>
    /// <returns>Отображаемые имена соисполнителей.</returns>
    [Public]
    public virtual string GetCoAssigneesNames(List<IEmployee> coAssignees, bool isFullName)
    {
      if (isFullName)
        return string.Join("; ", coAssignees.Select(x => x.Person != null ? x.Person.Name : x.Name));

      return string.Join("; ", coAssignees.Select(x => x.Person != null ? x.Person.ShortName : x.Name));
    }
    
    #region Работа с историей
    
    /// <summary>
    /// Получить список документов, удаленных из группы "Приложения" в заданиях.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="groupId">ИД группы вложений.</param>
    /// <returns>Список документов.</returns>
    [Public]
    public virtual List<IElectronicDocument> GetRemovedAddendaFromAssignments(Sungero.Workflow.ITask task, Guid groupId)
    {
      var removedAddenda = new List<IElectronicDocument>();
      if (task == null)
        return removedAddenda;
      
      var addendaHistory = Functions.Module.Remote.GetAttachmentHistoryEntriesByGroupId(task, groupId);
      var removedFromHistoryIds = addendaHistory.Removed
        .Select(x => x.DocumentId)
        .Distinct()
        .ToList();
      foreach (var id in removedFromHistoryIds)
      {
        var lastAddedDate = Docflow.Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Added, id);
        var lastRemovedDate = Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Removed, id);
        
        if (lastRemovedDate.HasValue && (!lastAddedDate.HasValue || lastRemovedDate.Value > lastAddedDate.Value))
        {
          var attachment = Functions.Module.Remote.GetElectronicDocumentById(id);
          if (attachment == null)
            continue;
          removedAddenda.Add(attachment);
        }
      }
      
      return removedAddenda;
    }
    
    /// <summary>
    /// Получить список документов, добавленных в группу "Приложения" в заданиях.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="groupId">ИД группы вложений.</param>
    /// <returns>Список документов.</returns>
    [Public]
    public virtual List<IElectronicDocument> GetAddedAddendaFromAssignments(Sungero.Workflow.ITask task, Guid groupId)
    {
      var addedAddenda = new List<IElectronicDocument>();
      
      var addendaHistory = Functions.Module.Remote.GetAttachmentHistoryEntriesByGroupId(task, groupId);
      var addedAttachmentIds = addendaHistory.Added
        .Select(x => x.DocumentId)
        .Distinct()
        .ToList();
      
      foreach (var id in addedAttachmentIds)
      {
        var lastAddedDate = Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Added, id);
        var lastRemovedDate = Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Removed, id);

        if (lastAddedDate.HasValue && (!lastRemovedDate.HasValue || lastAddedDate.Value > lastRemovedDate.Value))
        {
          var attachment = Functions.Module.Remote.GetElectronicDocumentById(id);
          if (attachment == null)
            continue;
          addedAddenda.Add(attachment);
        }
      }
      
      return addedAddenda;
    }
    
    /// <summary>
    /// Получить структурированный список операций с вложениями по записям из истории.
    /// </summary>
    /// <param name="history">Записи истории.</param>
    /// <returns>Структурированный список операций с вложениями.</returns>
    public virtual Structures.Module.AttachmentHistoryEntries ParseAttachmentsHistory(System.Collections.Generic.IEnumerable<Sungero.Workflow.IWorkflowHistory> history)
    {
      var attachmentHistoryEntries = Structures.Module.AttachmentHistoryEntries.Create();
      attachmentHistoryEntries.Added = new List<Sungero.Docflow.Structures.Module.AttachmentHistoryEntry>();
      attachmentHistoryEntries.Removed = new List<Sungero.Docflow.Structures.Module.AttachmentHistoryEntry>();
      
      foreach (var historyEntry in history)
      {
        if (!historyEntry.HistoryDate.HasValue)
          continue;
        
        var groupId = Functions.Module.GetAttachmentGroupIdFromHistoryComment(historyEntry.Comment);
        if (!groupId.HasValue)
          continue;
        
        var documentId = Functions.Module.GetDocumentIdFromHistoryComment(historyEntry.Comment);
        if (!documentId.HasValue)
          continue;
        
        var attachmentHistoryEntry = Sungero.Docflow.Structures.Module.AttachmentHistoryEntry.Create();
        attachmentHistoryEntry.OperationType = historyEntry.Operation;
        attachmentHistoryEntry.Date = historyEntry.HistoryDate.Value;
        attachmentHistoryEntry.DocumentId = documentId.Value;
        attachmentHistoryEntry.GroupId = groupId.Value;
        
        if (historyEntry.Operation == Sungero.Workflow.WorkflowHistory.Operation.AddAttachment)
          attachmentHistoryEntries.Added.Add(attachmentHistoryEntry);
        if (historyEntry.Operation == Sungero.Workflow.WorkflowHistory.Operation.DelAttachment)
          attachmentHistoryEntries.Removed.Add(attachmentHistoryEntry);
      }
      
      return attachmentHistoryEntries;
    }
    
    /// <summary>
    /// Получить ИД документа из комментария в истории.
    /// </summary>
    /// <param name="comment">Строка, содержащая комментарий из истории.</param>
    /// <returns>ИД документа или null, если ИД не удалось получить.</returns>
    [Public]
    public virtual int? GetDocumentIdFromHistoryComment(string comment)
    {
      if (string.IsNullOrWhiteSpace(comment))
        return null;
      
      var commentParts = comment.Split(Constants.Module.HistoryCommentDelimiter);
      var number = commentParts.Length >= Constants.Module.DocumentIdCommentPosition
        ? commentParts[Constants.Module.DocumentIdCommentPosition]
        : string.Empty;
      int documentId;
      if (!int.TryParse(number, out documentId))
        return null;
      
      return documentId;
    }
    
    /// <summary>
    /// Получить ИД группы вложений документа из комментария в истории.
    /// </summary>
    /// <param name="comment">Строка, содержащая комментарий из истории.</param>
    /// <returns>ИД группы вложений документа или null, если ИД не удалось получить.</returns>
    [Public]
    public virtual Guid? GetAttachmentGroupIdFromHistoryComment(string comment)
    {
      if (string.IsNullOrWhiteSpace(comment))
        return null;
      
      var commentParts = comment.Split(Constants.Module.HistoryCommentDelimiter);
      var id = commentParts.Length >= Constants.Module.AttachmentGroupIdCommentPosition
        ? commentParts[Constants.Module.AttachmentGroupIdCommentPosition]
        : string.Empty;
      Guid attachmentGroupId;
      if (!Guid.TryParse(id, out attachmentGroupId))
        return null;
      
      return attachmentGroupId;
    }
    
    /// <summary>
    /// Получить максимальное значение даты операции из списка операций для заданного ИД документа.
    /// </summary>
    /// <param name="operations">Список структур с информацией об операциях.</param>
    /// <param name="id">ИД документа.</param>
    /// <returns>Максимальная дата операции или null, если не удалось ее определить.</returns>
    public virtual DateTime? GetMaxHistoryOperationDateById(List<Structures.Module.AttachmentHistoryEntry> operations, int id)
    {
      if (operations == null)
        return null;
      
      var lastOperation = operations
        .Where(x => x.DocumentId == id)
        .OrderByDescending(x => x.Date)
        .FirstOrDefault();
      if (lastOperation == null)
        return null;
      
      return lastOperation.Date;
    }
    
    #endregion
    
    /// <summary>
    /// Добавить в начало текста метку, указывающую на то, что задание было согласовано с замечаниями.
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <returns>Текст с меткой.</returns>
    public virtual string AddApproveWithSuggestionsMark(string text)
    {
      var mark = this.GetApproveWithSuggestionsMark();
      return string.Concat(mark, text);
    }

    /// <summary>
    /// Удалить из начала текста метку, указывающую на то, что задание было согласовано с замечаниями.
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <returns>Текст без метки.</returns>
    public virtual string RemoveApproveWithSuggestionsMark(string text)
    {
      if (string.IsNullOrEmpty(text) || !this.HasApproveWithSuggestionsMark(text))
        return text;
      
      var mark = this.GetApproveWithSuggestionsMark();
      // Удалить только первое вхождение метки. GetApproveWithSuggestionsMark проверяет наличие метки в начале текста.
      return text.Remove(0, mark.Length);
    }
    
    /// <summary>
    /// Проверить, что в начале текста присутствует метка, указывающая на то, что задание было согласовано с замечаниями.
    /// </summary>
    /// <param name="text">Текст.</param>
    /// <returns>True - если метка в начале текста есть, иначе - false.</returns>
    public virtual bool HasApproveWithSuggestionsMark(string text)
    {
      if (string.IsNullOrEmpty(text))
        return false;
      
      var mark = this.GetApproveWithSuggestionsMark();
      // 185042 Используется данная перегрузка для корректной работы под Linux.
      return text.StartsWith(mark, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Получить метку, указывающую на то, что задание было согласовано с замечаниями.
    /// </summary>
    /// <returns>Метка.</returns>
    public virtual string GetApproveWithSuggestionsMark()
    {
      var zeroWidthSpace = Constants.Module.ZeroWidthSpace;
      return new string(new char[] { zeroWidthSpace, zeroWidthSpace, zeroWidthSpace });
    }
    
    /// <summary>
    /// Получить основание подписания из права подписи.
    /// </summary>
    /// <param name="signatureSetting">Право подписи.</param>
    /// <returns>Основание подписания.</returns>
    [Public, Obsolete("Используйте метод GetSigningReason.")]
    public virtual string GetPowersBase(Docflow.ISignatureSetting signatureSetting)
    {
      if (signatureSetting == null)
        return string.Empty;
      var powersBase = this.GetSigningReason(signatureSetting);
      powersBase = PublicFunctions.Module.CutText(powersBase, Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength);
      return powersBase;
    }
    
    /// <summary>
    /// Получить основание подписания из права подписи.
    /// </summary>
    /// <param name="signatureSetting">Право подписи.</param>
    /// <returns>Основание подписания.</returns>
    [Public]
    public virtual string GetSigningReason(Docflow.ISignatureSetting signatureSetting)
    {
      if (signatureSetting == null)
        return string.Empty;
      var signingReason = signatureSetting.Reason == Docflow.SignatureSetting.Reason.Duties
        ? Docflow.SignatureSettings.Info.Properties.Reason.GetLocalizedValue(Docflow.SignatureSetting.Reason.Duties)
        : signatureSetting.SigningReason;
      return signingReason;
    }
  }
}
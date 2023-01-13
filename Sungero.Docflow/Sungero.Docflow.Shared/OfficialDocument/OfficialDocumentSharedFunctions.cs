using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Shared
{
  partial class OfficialDocumentFunctions
  {
    
    #region Регистрация
    
    /// <summary>
    /// Получить ИД ведущего документа.
    /// </summary>
    /// <returns>ИД документа либо 0.</returns>
    public virtual int GetLeadDocumentId()
    {
      return _obj.LeadingDocument != null ? _obj.LeadingDocument.Id : 0;
    }
    
    /// <summary>
    /// Получить номер ведущего документа.
    /// </summary>
    /// <returns>Номер документа либо пустая строка.</returns>
    public virtual string GetLeadDocumentNumber()
    {
      // Виртуальная функция. Переопределено в потомках.
      return string.Empty;
    }
    
    /// <summary>
    /// Зарегистрировать документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentRegister">Журнал.</param>
    /// <param name="registrationDate">Дата.</param>
    /// <param name="registrationNumber">Номер регистрации.</param>
    /// <param name="numberReservation">Признак резервирования.</param>
    /// <param name="needSaveDocument">Признак необходимости сохранения документа.</param>
    [Public]
    public static void RegisterDocument(IOfficialDocument document, IDocumentRegister documentRegister,
                                        DateTime? registrationDate, string registrationNumber, bool? numberReservation, bool needSaveDocument)
    {
      // Определить новый статус документа.
      var registrationState = RegistrationState.Registered;
      if (documentRegister == null && !registrationDate.HasValue)
        registrationState = RegistrationState.NotRegistered;
      else if (numberReservation ?? false)
        registrationState = RegistrationState.Reserved;
      
      // Установить новый статус документа.
      document.RegistrationState = registrationState;
      
      // Обновить регистрационные данные.
      document.DocumentRegister = documentRegister;
      document.RegistrationDate = registrationDate;
      document.RegistrationNumber = registrationNumber;
      FillCaseFileAndDeliveryMethod(document, documentRegister);
      
      // Для регистрируемых документов завершить верификацию.
      if (document.RegistrationState == RegistrationState.Registered && document.VerificationState == VerificationState.InProcess &&
          document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable)
        document.VerificationState = VerificationState.Completed;
      
      if (needSaveDocument)
        document.Save();
    }
    
    /// <summary>
    /// Заполнить дело и способ доставки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentRegister">Журнал регистрации.</param>
    [Public]
    public static void FillCaseFileAndDeliveryMethod(IOfficialDocument document, IDocumentRegister documentRegister)
    {
      // Определить дело и способ доставки.
      var caseFile = CaseFiles.Null;
      var deliveryMethod = MailDeliveryMethods.Null;
      var direction = documentRegister != null ? documentRegister.DocumentFlow : null;
      var personalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      if (personalSetting != null)
      {
        if (direction == Docflow.DocumentRegister.DocumentFlow.Incoming)
        {
          caseFile = personalSetting.IncomingCaseFile;
          deliveryMethod = personalSetting.IncomingDeliveryMethod;
        }
        if (direction == Docflow.DocumentRegister.DocumentFlow.Outgoing)
        {
          caseFile = personalSetting.OutgoingCaseFile;
          deliveryMethod = personalSetting.OutgoingDeliveryMethod;
        }
        if (direction == Docflow.DocumentRegister.DocumentFlow.Inner)
        {
          caseFile = personalSetting.InnerCaseFile;
          deliveryMethod = personalSetting.InnerDeliveryMethod;
        }
      }
      
      // Дело должно быть действующим, период - актуальным. Иначе дело не указываем.
      if (!(caseFile != null &&
            caseFile.Status == CoreEntities.DatabookEntry.Status.Active &&
            caseFile.StartDate <= Calendar.UserToday &&
            Calendar.UserToday <= (caseFile.EndDate ?? DateTime.MaxValue)))
        caseFile = null;
      
      // Установить значения реквизитов в документе.
      if (document.CaseFile == null && caseFile != null)
      {
        document.CaseFile = caseFile;
        document.PlacedToCaseFileDate = Calendar.UserToday;
      }
      
      var outgoingDocument = OutgoingDocumentBases.As(document);
      if (outgoingDocument != null && outgoingDocument.IsManyAddressees == true)
      {
        var addressees = outgoingDocument.Addressees.Where(a => a.DeliveryMethod == null);
        foreach (var addressee in addressees)
          addressee.DeliveryMethod = deliveryMethod;
      }
      else if (document.DeliveryMethod == null)
        document.DeliveryMethod = deliveryMethod;
    }
    
    /// <summary>
    /// Проверять рег. номер на уникальность.
    /// </summary>
    /// <returns>True - проверять, False - не проверять.</returns>
    public virtual bool CheckRegistrationNumberUnique()
    {
      return true;
    }
    
    /// <summary>
    /// Получить описание для диалога отмены регистрации.
    /// </summary>
    /// <param name="settingType">Тип настройки.</param>
    /// <returns>Описание.</returns>
    public virtual string GetCancelRegistrationDialogDescription(Enumeration? settingType)
    {
      var description = Docflow.Resources.CancelRegistrationDescription;
      if (settingType == Docflow.RegistrationSetting.SettingType.Reservation)
        return Docflow.Resources.CancelReservationDescription;
      
      if (settingType == Docflow.RegistrationSetting.SettingType.Numeration)
        return Docflow.Resources.CancelNumberingDescription;
      
      return Docflow.Resources.CancelRegistrationDescription;
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      _obj.State.Properties.BusinessUnit.IsRequired = _obj.Info.Properties.BusinessUnit.IsRequired ||
        (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType != NumberingType.NotNumerable);

      _obj.State.Properties.Department.IsRequired = _obj.Info.Properties.Department.IsRequired ||
        (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType != NumberingType.NotNumerable);

      _obj.State.Properties.Subject.IsRequired = _obj.Info.Properties.Subject.IsRequired ||
        (_obj.DocumentKind != null &&
         (_obj.DocumentKind.NumberingType == NumberingType.Registrable ||
          _obj.DocumentKind.GenerateDocumentName == true));
      
      _obj.State.Properties.DocumentRegister.IsRequired = _obj.RegistrationState != RegistrationState.NotRegistered;
      
      _obj.State.Properties.RegistrationDate.IsRequired = _obj.RegistrationState != RegistrationState.NotRegistered;
    }
    
    #endregion
    
    #region История

    /// <summary>
    /// Получить операцию истории "Регистрация".
    /// </summary>
    /// <returns>Операция Регистрация.</returns>
    [PublicAttribute]
    public static string GetRegistrationOperation()
    {
      return Constants.OfficialDocument.Operation.Registration;
    }
    
    #endregion
    
    #region Работа с закладкой "Выдача"
    
    /// <summary>
    /// Получить удаленные строки выдачи.
    /// </summary>
    /// <param name="entity">Документ.</param>
    /// <returns>Удаленная выдача.</returns>
    public static System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> GetDeletedTrackingRecords(IOfficialDocument entity)
    {
      var deletedTrackingRecords = entity.State.Properties.Tracking.Deleted;
      
      var deletedRecords = deletedTrackingRecords
        .Where(l => l.ReturnTask != null &&
               l.ReturnTask.Status == Sungero.Workflow.Task.Status.InProcess &&
               l.ReturnTask.Info.Name == CheckReturnTasks.Info.Name);
      var records = entity.Tracking
        .Where(l => l.ReturnTask != null &&
               l.ReturnTask.Status == Sungero.Workflow.Task.Status.InProcess &&
               l.ReturnTask.Info.Name == CheckReturnTasks.Info.Name &&
               l.ReturnDeadline == null &&
               CheckReturnTasks.As(l.ReturnTask).Deadline != l.ReturnDeadline);
      return (IEnumerable<IOfficialDocumentTracking>)deletedRecords.Concat(records);
    }
    
    /// <summary>
    /// Получить строки выдачи с измененным сотрудником.
    /// </summary>
    /// <param name="entity">Документ.</param>
    /// <returns>Выдача с измененным сотрудником.</returns>
    public static System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> GetTrackingRecordsWithEmployeeChanged(IOfficialDocument entity)
    {
      var changedRecords = entity.State.Properties.Tracking.Changed;
      return (IEnumerable<IOfficialDocumentTracking>)changedRecords
        .Where(l => l.ReturnTask != null && l.ReturnTask.Info.Name == CheckReturnTasks.Info.Name &&
               !Equals(CheckReturnTasks.As(l.ReturnTask).Assignee, l.DeliveredTo));
    }
    
    /// <summary>
    /// Получить строки выдачи с задачами, которые необходимо выполнить.
    /// </summary>
    /// <param name="entity">Документ.</param>
    /// <returns>Возвращенная выдача.</returns>
    public static System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> GetChangedTrackingRecordsWithTasksInProcess(IOfficialDocument entity)
    {
      var changedRecords = entity.State.Properties.Tracking.Changed;
      return (IEnumerable<IOfficialDocumentTracking>)changedRecords
        .Where(l => l.ReturnTask != null &&
               l.ReturnTask.Status == Sungero.Workflow.Task.Status.InProcess &&
               l.ReturnDate != null && l.ExternalLinkId == null);
    }
    
    /// <summary>
    /// Получить строки выдачи с измененным сроком возврата.
    /// </summary>
    /// <param name="entity">Документ.</param>
    /// <returns>Выдача с измененным сроком.</returns>
    public static System.Collections.Generic.IEnumerable<IOfficialDocumentTracking> GetTrackingRecordsWithDeadlineChanged(IOfficialDocument entity)
    {
      return entity.Tracking.Where(l => l.ReturnTask != null &&
                                   l.ReturnTask.Status == Sungero.Workflow.Task.Status.InProcess &&
                                   l.ReturnTask.Info.Name == CheckReturnTasks.Info.Name &&
                                   l.ReturnDeadline != null &&
                                   CheckReturnTasks.As(l.ReturnTask).Deadline != l.ReturnDeadline);
    }

    #endregion

    #region Получение списка журналов регистрации по документу
    
    /// <summary>
    /// Получить отфильтрованные журналы регистрации по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Журналы по документу.</returns>
    public static List<int> GetDocumentRegistersByDocument(IOfficialDocument document)
    {
      var settingType = GetSettingType(document);
      return GetDocumentRegistersIdsByDocument(document, settingType);
    }
    
    /// <summary>
    ///  Получить отфильтрованные журналы регистрации по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="settingType">Тип регистрации.</param>
    /// <returns>Журналы.</returns>
    [Public, Obsolete("Используйте метод GetDocumentRegistersIdsByDocument.")]
    public static List<IDocumentRegister> GetDocumentRegistersByDocument(IOfficialDocument document, Enumeration? settingType)
    {
      var emptyList = new List<IDocumentRegister>();
      var documentKind = document.DocumentKind;
      if (documentKind == null)
        return emptyList;
      
      var isClerk = document.AccessRights.CanRegister();
      if (!isClerk || settingType == Docflow.RegistrationSetting.SettingType.Numeration)
      {
        var setting = PublicFunctions.Module.Remote.GetRegistrationSettings(settingType, document.BusinessUnit, documentKind, document.Department).FirstOrDefault();
        return setting != null ? new List<IDocumentRegister> { setting.DocumentRegister } : emptyList;
      }
      
      return Functions.DocumentRegister.Remote.GetDocumentRegistersByParams(document.DocumentKind, document.BusinessUnit, document.Department, settingType, true);
    }
    
    /// <summary>
    ///  Получить ИД отфильтрованных журналов регистрации по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="settingType">Тип регистрации.</param>
    /// <returns>Журналы.</returns>
    [Public]
    public static List<int> GetDocumentRegistersIdsByDocument(IOfficialDocument document, Enumeration? settingType)
    {
      var emptyList = new List<int>();
      var documentKind = document.DocumentKind;
      if (documentKind == null)
        return emptyList;
      
      var isClerk = document.AccessRights.CanRegister();
      if (!isClerk || settingType == Docflow.RegistrationSetting.SettingType.Numeration)
      {
        var setting = PublicFunctions.Module.Remote.GetRegistrationSettings(settingType, document.BusinessUnit, documentKind, document.Department).FirstOrDefault();
        return setting != null ? new List<int> { setting.DocumentRegister.Id } : emptyList;
      }
      
      return Functions.DocumentRegister.Remote.GetDocumentRegistersIdsByParams(document.DocumentKind, document.BusinessUnit, document.Department, settingType, true);
    }
    
    /// <summary>
    /// Имеются ли подходящие журналы регистрации по документу.
    /// </summary>
    /// <param name="settingType">Тип регистрации.</param>
    /// <returns>True - если есть подходящие журналы.</returns>
    [Public]
    public virtual bool HasDocumentRegistersByDocument(Enumeration? settingType)
    {
      if (_obj.DocumentKind == null)
        return false;
      
      var isClerk = _obj.AccessRights.CanRegister();
      if (!isClerk || settingType == Docflow.RegistrationSetting.SettingType.Numeration)
        return PublicFunctions.Module.Remote.GetRegistrationSettings(settingType, _obj.BusinessUnit, _obj.DocumentKind, _obj.Department).Any();
      
      return Functions.DocumentRegister.Remote.HasDocumentRegistersByParams(_obj.DocumentKind, _obj.BusinessUnit, _obj.Department, settingType, true);
    }
    
    /// <summary>
    /// Проверить возможность изменения реквизитов или отмены регистрации.
    /// </summary>
    /// <returns>True, если операции можно выполнить.</returns>
    /// <remarks>Только для регистрируемых журналов.</remarks>
    [Public]
    public virtual bool CanChangeRequisitesOrCancelRegistration()
    {
      // Разрешаем сначала изменить реквизиты с очисткой журнала, а потом отменить регистрацию.
      if (_obj.DocumentRegister == null)
        return true;
      
      // Разрешаем, если это резервирование.
      if (_obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Reserved)
        return true;
      
      // Только для регистрируемых журналов.
      if (_obj.DocumentRegister.RegisterType != Docflow.DocumentRegister.RegisterType.Registration)
        return true;
      
      return _obj.AccessRights.CanRegister() && Employees.AllRecipientIds.Contains(_obj.DocumentRegister.RegistrationGroup.Id);
    }
    
    /// <summary>
    /// Получить тип настроек.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Тип настроек.</returns>
    public static Enumeration? GetSettingType(IOfficialDocument document)
    {
      if (document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
        return Docflow.RegistrationSetting.SettingType.Numeration;
      else
        return document.RegistrationState == RegistrationState.Registered ?
          Docflow.RegistrationSetting.SettingType.Registration :
          Docflow.RegistrationSetting.SettingType.Reservation;
    }
    
    #endregion
    
    #region Отображение панели реквизитов
    
    /// <summary>
    /// Обновить карточку документа.
    /// </summary>
    public virtual void RefreshDocumentForm()
    {
      // Тип нумерации.
      var isNumerable = (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Numerable) || _obj.ExchangeState.HasValue;
      var isNotifiable = (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Registrable) || _obj.ExchangeState.HasValue;
      
      // Параметры формы.
      var formParams = ((IExtendedEntity)_obj).Params;
      var repeatRegister = formParams.ContainsKey(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister) &&
        (bool)formParams[Sungero.Docflow.Constants.OfficialDocument.RepeatRegister];
      
      // Показывать ли свойства.
      var needShow = Functions.OfficialDocument.NeedShowRegistrationPane(_obj, isNotifiable || isNumerable);
      Functions.OfficialDocument.ChangeRegistrationPaneVisibility(_obj, needShow, repeatRegister);
      
      var isNotRegistered = repeatRegister || _obj.RegistrationState == RegistrationState.NotRegistered;
      
      Functions.OfficialDocument.ChangeDocumentPropertiesAccess(_obj, isNotRegistered, repeatRegister);
      
      // Показывать ли основание подписания.
      Functions.OfficialDocument.ChangeOurSigningReasonVisibility(_obj);
    }
    
    /// <summary>
    /// Признак необходимости отображения панели регистрации.
    /// </summary>
    /// <param name="additionalCondition">Дополнительное условие при наследовании.</param>
    /// <returns>True, если надо показать панель.</returns>
    public virtual bool NeedShowRegistrationPane(bool additionalCondition)
    {
      // Параметры формы.
      var showParam = false;
      var formParams = ((IExtendedEntity)_obj).Params;
      if (formParams.ContainsKey(Sungero.Docflow.Constants.OfficialDocument.ShowParam))
        showParam = (bool)formParams[Sungero.Docflow.Constants.OfficialDocument.ShowParam];
      else
      {
        var showRegPane = Functions.PersonalSetting.Remote.GetShowRegistrationPaneParam(null);
        var onVerification = _obj.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess;
        showParam = showRegPane && additionalCondition ||
          onVerification ||
          Functions.OfficialDocument.DefaultRegistrationPaneVisibility(_obj);
        formParams[Sungero.Docflow.Constants.OfficialDocument.ShowParam] = showParam;
      }

      return showParam;
    }
    
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="repeatRegister">Перерегистрация.</param>
    public virtual void ChangeDocumentPropertiesAccess(bool isEnabled, bool repeatRegister)
    {
      if (_obj.VerificationState == VerificationState.InProcess && this.IsNumerationSucceed())
      {
        this.EnableRequisitesForVerification();
      }
      else
      {
        var isNotifiable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Registrable;
        var canRegister = _obj.AccessRights.CanRegister();
        var properties = _obj.State.Properties;
        var projectIsRequired = _obj.Info.Properties.Project.IsRequired || _obj.DocumentKind != null && _obj.DocumentKind.ProjectsAccounting == true;
        properties.Name.IsEnabled = _obj.DocumentKind == null || (!_obj.DocumentKind.GenerateDocumentName.Value && isEnabled);
        properties.DocumentKind.IsEnabled = isEnabled && !repeatRegister;
        properties.Subject.IsEnabled = isEnabled;
        properties.Project.IsVisible = projectIsRequired;
        
        // Наша организация должна переключаться во всех наследниках.
        properties.BusinessUnit.IsEnabled = isEnabled;
        
        // При перерегистрации НОР недоступна, если в журнале есть разрез по НОР или в формате номера журнала есть код НОР.
        var documentRegister = _obj.DocumentRegister;
        var businessUnitCodeIncludedInNumber = repeatRegister && documentRegister != null &&
          documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.BUCode);
        var businessUnitSectionIncludedInRegister = repeatRegister && documentRegister != null &&
          documentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.BusinessUnit;
        properties.BusinessUnit.IsEnabled = isEnabled && !businessUnitCodeIncludedInNumber && !businessUnitSectionIncludedInRegister;
        
        // При перерегистрации подразделение недоступно, если в журнале есть разрез по подразделению или в формате номера журнала есть код подразделения.
        var departmentCodeIncludedInNumber = repeatRegister && documentRegister != null &&
          documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode);
        var departmentSectionIncludedInRegister = repeatRegister && documentRegister != null &&
          documentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.Department;
        properties.Department.IsEnabled = isEnabled && !departmentCodeIncludedInNumber && !departmentSectionIncludedInRegister;
        
        // При перерегистрации контрагент недоступен, если в формате номера журнала есть код контрагента.
        var counterpartyCodeIncludedInNumber = repeatRegister && documentRegister != null &&
          documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.CPartyCode);
        this.ChangeCounterpartyPropertyAccess(isEnabled, counterpartyCodeIncludedInNumber);
        
        // "Подготовил" доступно только регистраторам указанного документопотока.
        properties.Assignee.IsEnabled = canRegister;
        
        // Проверить, что поле "Исполнитель" присутствует на карточке, т.к. код ниже содержит запрос на СП.
        if (properties.Assignee.IsVisible)
        {
          // Для зарегистрированных документов "Исполнитель" должно быть доступно только группе регистрации.
          if (canRegister && isNotifiable && _obj.AccessRights.CanUpdate() && _obj.RegistrationState == RegistrationState.Registered &&
              documentRegister != null && documentRegister.RegistrationGroup != null)
          {
            // Парамсы формы.
            var formParams = ((IExtendedEntity)_obj).Params;
            var canChange = formParams.ContainsKey(Constants.OfficialDocument.CanChangeAssignee);
            if (canChange)
              canChange = (bool)formParams[Constants.OfficialDocument.CanChangeAssignee];
            else
            {
              canChange = Functions.OfficialDocument.Remote.CanChangeAssignee(_obj);
              formParams[Constants.OfficialDocument.CanChangeAssignee] = canChange;
            }
            
            properties.Assignee.IsEnabled = canChange;
          }
        }
      }
      
      this.EnableRegistrationNumberAndDate();
    }
    
    /// <summary>
    /// Создать кеш параметров.
    /// </summary>
    [Public]
    public virtual void CreateParamsCache()
    {
      var parameters = Functions.OfficialDocument.Remote.GetOfficialDocumentParams(_obj);
      
      var formParams = ((IExtendedEntity)_obj).Params;
      
      if (parameters.HasReservationSetting.HasValue)
        formParams[Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting] = parameters.HasReservationSetting;
      
      if (parameters.HasNumerationSetting.HasValue)
        formParams[Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting] = parameters.HasNumerationSetting;
      
      if (parameters.CanChangeAssignee.HasValue)
        formParams[Constants.OfficialDocument.CanChangeAssignee] = parameters.CanChangeAssignee;
      
      if (parameters.NeedShowRegistrationPane.HasValue)
      {
        var isNumerable = (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Numerable) || _obj.ExchangeState.HasValue;
        var isNotifiable = (_obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Registrable) || _obj.ExchangeState.HasValue;
        var additionalCondition = isNotifiable || isNumerable;
        
        formParams[Sungero.Docflow.Constants.OfficialDocument.ShowParam] = (bool)parameters.NeedShowRegistrationPane && additionalCondition ||
          _obj.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess ||
          Functions.OfficialDocument.DefaultRegistrationPaneVisibility(_obj);
      }
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    public virtual void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber)
    {
      var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed);
      this.ChangeCounterpartyPropertyAccess(isEnabled, counterpartyCodeInNumber, enabledState);
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public virtual void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public virtual void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      // Документопоток.
      var direction = _obj.DocumentKind != null ? _obj.DocumentKind.DocumentFlow : null;
      var isIncomingDirection = direction == DocumentFlow.Incoming;
      var isOutgoingDirection = direction == DocumentFlow.Outgoing;
      var isInnerDirection = direction == DocumentFlow.Inner;
      var isContractDirection = direction == DocumentFlow.Contracts;
      
      // Тип нумерации.
      var isNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Numerable;
      var isNotifiable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Registrable;
      var isNotNumerable = !isNumerable && !isNotifiable;

      // Есть права на регистрацию.
      var canRegister = _obj.AccessRights.CanRegister();
      
      // Поля Дело и Дата помещения в дело д.б. недоступны,
      // если в формате номера журнала есть индекс дела и документ зарегистрирован(пронумерован).
      // Делопроизводитель должен иметь возможность сменить дело и дату помещения в дело при изменении реквизитов регистрации.
      var caseFileIncludedInNumber = _obj.DocumentRegister != null &&
        _obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.CaseFile);
      var alreadyRegistered = _obj.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered;
      var caseFileEnabled = canRegister && (!(caseFileIncludedInNumber && alreadyRegistered) || repeatRegister);
      
      var properties = _obj.State.Properties;
      
      properties.DeliveryMethod.IsEnabled = !isNotNumerable && canRegister;
      properties.DeliveryMethod.IsVisible = needShow && !isInnerDirection && !isContractDirection;
      
      properties.DocumentRegister.IsEnabled = repeatRegister;
      properties.DocumentRegister.IsVisible = needShow && !isNumerable;
      
      properties.CaseFile.IsEnabled = caseFileEnabled;
      properties.CaseFile.IsVisible = needShow;

      properties.PlacedToCaseFileDate.IsEnabled = caseFileEnabled;
      properties.PlacedToCaseFileDate.IsVisible = needShow;

      properties.RegistrationNumber.IsEnabled = repeatRegister;
      properties.RegistrationNumber.IsVisible = needShow;
      
      properties.RegistrationDate.IsEnabled = repeatRegister;
      properties.RegistrationDate.IsVisible = needShow;
      
      properties.LifeCycleState.IsEnabled = true;
      properties.LifeCycleState.IsVisible = needShow;

      properties.RegistrationState.IsVisible = needShow && isNotifiable;

      properties.InternalApprovalState.IsEnabled = true;
      properties.InternalApprovalState.IsVisible = (isOutgoingDirection || isInnerDirection || isContractDirection) && needShow;

      properties.ExternalApprovalState.IsEnabled = true;
      properties.ExternalApprovalState.IsVisible = isContractDirection && needShow;
      
      properties.ExecutionState.IsEnabled = true;
      properties.ExecutionState.IsVisible = (isIncomingDirection || isInnerDirection) && needShow;

      properties.ControlExecutionState.IsEnabled = true;
      properties.ControlExecutionState.IsVisible = (isIncomingDirection || isInnerDirection) && needShow;
      
      properties.LocationState.IsVisible = needShow && !string.IsNullOrWhiteSpace(_obj.LocationState);
      
      properties.Tracking.IsEnabled = isNumerable || (isNotifiable && canRegister);
      
      // Статус верификации.
      properties.VerificationState.IsVisible = needShow && Docflow.PublicFunctions.SmartProcessingSetting.SmartProcessingIsEnabled();
    }
    
    /// <summary>
    /// Поведение панели по умолчанию.
    /// </summary>
    /// <returns>True, если панель должна быть отображена при создании документа.</returns>
    public virtual bool DefaultRegistrationPaneVisibility()
    {
      // Тип нумерации.
      var isNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Numerable;
      var isNotifiable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == NumberingType.Registrable;
      
      // Есть права на регистрацию.
      var canRegister = _obj.AccessRights.CanRegister();
      
      return (canRegister && isNotifiable) || isNumerable;
    }
    
    /// <summary>
    /// Изменить отображение основания подписания.
    /// </summary>
    public virtual void ChangeOurSigningReasonVisibility()
    {
      var formParams = ((IExtendedEntity)_obj).Params;
      
      if (formParams.ContainsKey(Sungero.Docflow.Constants.OfficialDocument.ShowOurSigningReasonParam))
        _obj.State.Properties.OurSigningReason.IsVisible = this.GetShowOurSigningReasonParam();
    }
    
    /// <summary>
    /// Получить значение параметра, отвечающего за показ/скрытие основания подписания документа.
    /// </summary>
    /// <returns>True - если нужно показать основание подписания документа.</returns>
    public virtual bool GetShowOurSigningReasonParam()
    {
      var formParams = ((IExtendedEntity)_obj).Params;
      
      if (formParams.ContainsKey(Sungero.Docflow.Constants.OfficialDocument.ShowOurSigningReasonParam))
        return (bool)formParams[Sungero.Docflow.Constants.OfficialDocument.ShowOurSigningReasonParam];
      
      return false;
    }
    
    #endregion
    
    #region Жизненный цикл
    
    /// <summary>
    /// Обновить жизненный цикл документа.
    /// </summary>
    /// <param name="registrationState">Статус регистрации.</param>
    /// <param name="approvalState">Статус согласования.</param>
    /// <param name="counterpartyApprovalState">Статус согласования с контрагентом.</param>
    public virtual void UpdateLifeCycle(Enumeration? registrationState,
                                        Enumeration? approvalState,
                                        Enumeration? counterpartyApprovalState)
    {
      // Не проверять статусы для пустых параметров.
      if (_obj == null || _obj.DocumentKind == null)
        return;
      
      var direction = _obj.DocumentKind.DocumentFlow;
      var currentState = _obj.LifeCycleState;
      var lifeCycleMustByActive = IsLifeCycleMustBeActive(direction, approvalState, counterpartyApprovalState);
      
      // Если регистрация была отменена, а документ действующий согласно функции - ставим статус в разработке.
      if (currentState == LifeCycleState.Active &&
          registrationState == RegistrationState.NotRegistered &&
          _obj.State.Properties.RegistrationState.OriginalValue != registrationState &&
          _obj.State.Properties.RegistrationState.OriginalValue != null &&
          lifeCycleMustByActive)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;

      // Документ должен быть в разработке (или null) и зарегистрирован.
      if ((currentState != null && currentState != Docflow.OfficialDocument.LifeCycleState.Draft) ||
          registrationState != Docflow.OfficialDocument.RegistrationState.Registered)
        return;
      
      if (lifeCycleMustByActive)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
    }
    
    /// <summary>
    /// Проверка необходимости установки статуса Действующий для документопотока.
    /// </summary>
    /// <param name="direction">Документопоток.</param>
    /// <param name="approvalState">Статус согласования.</param>
    /// <param name="counterpartyApprovalState">Статус согласования с контрагентом.</param>
    /// <returns>Признак необходимости смены ЖЦ документа на действующий.</returns>
    public static bool IsLifeCycleMustBeActive(Enumeration? direction, Enumeration? approvalState, Enumeration? counterpartyApprovalState)
    {
      // Входящие и исходящие документы должны быть действующими.
      if (direction == Docflow.DocumentKind.DocumentFlow.Outgoing ||
          direction == Docflow.DocumentKind.DocumentFlow.Incoming)
        return true;

      // Внутренние документы необходимо подписать.
      if (direction == Docflow.DocumentKind.DocumentFlow.Inner &&
          (approvalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
           approvalState == Docflow.Memo.InternalApprovalState.Reviewed))
        return true;
      
      // Договорные документы необходимо подписать у нас и у контрагента.
      if (direction == Docflow.DocumentKind.DocumentFlow.Contracts &&
          counterpartyApprovalState == Docflow.OfficialDocument.ExternalApprovalState.Signed &&
          approvalState == Docflow.OfficialDocument.InternalApprovalState.Signed)
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Изменение состояния документа для ненумеруемых документов.
    /// </summary>
    public virtual void SetLifeCycleState()
    {
      var documentKind = _obj.DocumentKind;
      var isNotNumerable = documentKind != null &&
        documentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      var isAutoNumerable = documentKind != null &&
        documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable &&
        documentKind.AutoNumbering == true;
      var isDraft = _obj.LifeCycleState == null ||
        _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft;
      
      // Документ ненумеруемого или автонумеруемого вида сделать действующим, если раньше был черновиком.
      if ((isNotNumerable || isAutoNumerable) && isDraft)
        _obj.LifeCycleState = LifeCycleState.Active;
      
      // Для нумеруемого или регистрируемого сделать черновиком. Кроме автонумеруемых.
      if (!isNotNumerable && !isDraft && !isAutoNumerable)
        _obj.LifeCycleState = LifeCycleState.Draft;
    }
    
    /// <summary>
    /// Сменить тип документа на недействующий.
    /// </summary>
    /// <param name="isActive">True, если документ действующий.</param>
    [Public]
    public virtual void SetObsolete(bool isActive)
    {
      _obj.LifeCycleState = LifeCycleState.Obsolete;
    }
    
    /// <summary>
    /// Проверяет, является ли документ недействующим.
    /// </summary>
    /// <param name="lifeCycleState">Статус ЖЦ.</param>
    /// <returns>Признак того, является ли документ недействующим.</returns>
    public virtual bool IsObsolete(Enumeration? lifeCycleState)
    {
      return lifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete;
    }
    
    /// <summary>
    /// Проверяет, является ли документ недействующим.
    /// </summary>
    /// <returns>Признак того, является ли документ недействующим.</returns>
    [Public]
    public virtual bool IsObsolete()
    {
      return _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Obsolete;
    }
    
    #endregion
    
    #region Получение свойств документа
    
    /// <summary>
    /// Получение группы документа.
    /// </summary>
    /// <returns>Группа документа.</returns>
    [Public]
    public virtual IDocumentGroupBase GetDocumentGroup()
    {
      return _obj.DocumentGroup;
    }
    
    /// <summary>
    /// Получение контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    [Public]
    public virtual List<Parties.ICounterparty> GetCounterparties()
    {
      return null;
    }
    
    /// <summary>
    /// Получить код контрагента.
    /// </summary>
    /// <returns>Код контрагента либо пустая строка.</returns>
    [Public]
    public virtual string GetCounterpartyCode()
    {
      // Виртуальная функция. Переопределено в потомках.
      if (this.GetCounterparties() == null)
        return string.Empty;
      var counterparty = this.GetCounterparties().FirstOrDefault();
      var counterpartyCode = counterparty == null ? string.Empty : counterparty.Code;
      return counterpartyCode;
    }
    
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Пользователь, ответственный за документ.</returns>
    [Public]
    public virtual Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      return Employees.As(_obj.Author);
    }
    
    /// <summary>
    /// Получить подписывающего по умолчанию.
    /// </summary>
    /// <param name="signatories">Список подписывающих с приоритетом.</param>
    /// <returns>Подписывающий по умолчанию.</returns>
    [Obsolete("Используйте метод GetDefaultSignatory().")]
    public virtual Sungero.Company.IEmployee GetDefaultSignatory(List<Docflow.Structures.SignatureSetting.Signatory> signatories)
    {
      if (!signatories.Any())
        return null;
      
      var maxPriority = signatories.Max(sign => sign.Priority);
      var signatoriesMaxPriority = signatories.Where(s => s.Priority == maxPriority);
      var employeeMaxPriorityCount = signatoriesMaxPriority.Select(e => e.EmployeeId).Distinct().Count();
      if (employeeMaxPriorityCount == 1)
      {
        var defaultSignatoryId = signatoriesMaxPriority.Select(s => s.EmployeeId).FirstOrDefault();
        var defaultSignatory = Employees.Get(defaultSignatoryId);
        return defaultSignatory;
      }
      return null;
    }
    
    /// <summary>
    /// Получить группу регистрации.
    /// </summary>
    /// <returns>Список групп регистрации.</returns>
    [Public]
    public virtual Docflow.IRegistrationGroup GetRegistrationGroup()
    {
      if (_obj.DocumentRegister != null &&
          _obj.DocumentRegister.RegistrationGroup != null &&
          _obj.DocumentRegister.RegistrationGroup.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        return _obj.DocumentRegister.RegistrationGroup;
      
      return Docflow.RegistrationGroups.Null;
    }
    
    /// <summary>
    /// Получить адресатов.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public virtual List<Company.IEmployee> GetAddressees()
    {
      // Виртуальная функция. Переопределено в потомках.
      return new List<Company.IEmployee>();
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки письма.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public virtual List<Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      return new List<Structures.OfficialDocument.IEmailAddressee>();
    }
    
    /// <summary>
    /// Получить проект из документа.
    /// </summary>
    /// <returns>Проект, указанный в карточке документа.</returns>
    [Public]
    public virtual IProjectBase GetProject()
    {
      return _obj.Project;
    }
    
    /// <summary>
    /// Получить основание подписания со стороны контрагента.
    /// </summary>
    /// <returns>Основание подписания со стороны контрагента.</returns>
    [Public]
    public virtual string GetCounterpartySigningReason()
    {
      return string.Empty;
    }
    
    #endregion
    
    #region Заполнение свойств документа
    
    /// <summary>
    /// Заполнить подписывающего.
    /// </summary>
    /// <param name="signatory">Подписывающий со стороны контрагента.</param>
    [Public]
    public virtual void FillCounterpartySignatory(Parties.IContact signatory)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Заполнить основание со стороны контрагента.
    /// </summary>
    /// <param name="signingReason">Основание контрагента.</param>
    [Public]
    public virtual void FillCounterpartySigningReason(string signingReason)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Заполнить свойство "Ведущий документ" в зависимости от типа документа.
    /// </summary>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <remarks>Используется при смене типа.</remarks>
    [Public]
    public virtual void FillLeadingDocument(IOfficialDocument leadingDocument)
    {
      return;
    }
    
    /// <summary>
    /// Заполнить оргструктуру.
    /// </summary>
    public void FillOrganizationStructure()
    {
      // Заполнить нашу организацию.
      if (_obj.BusinessUnit == null && _obj.State.Properties.BusinessUnit.IsVisible)
        _obj.BusinessUnit = Functions.Module.GetDefaultBusinessUnit(Company.Employees.Current);

      // Заполнить подразделение.
      var employee = Company.Employees.Current;
      if (_obj.Department == null)
      {
        var department = Company.Departments.Null;
        var settings = Functions.PersonalSetting.GetPersonalSettings(employee);
        // Из настроек.
        if (settings != null)
          department = settings.Department;
        
        // По оргструктуре.
        if (department == null && employee != null)
          department = employee.Department;

        _obj.Department = department;
      }
      
      // Заполнить "Подготовил".
      if (_obj.PreparedBy == null)
        _obj.PreparedBy = employee;
    }
    
    /// <summary>
    /// Заполнить имя документа.
    /// </summary>
    [Public]
    public virtual void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        if (_obj.VerificationState == null)
          name = Docflow.Resources.DocumentNameAutotext;
        else
          name = _obj.DocumentKind.ShortName;
      }
      else if (documentKind != null)
      {
        name = documentKind.ShortName + name;
      }
      
      name = Functions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Functions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Добавить закрывающую кавычку для имени.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string AddClosingQuote(string name, IOfficialDocument document)
    {
      return name.Length > document.Info.Properties.Name.Length ?
        name.Substring(0, document.Info.Properties.Name.Length - 1) + "\"" :
        name;
    }

    /// <summary>
    /// Добавить закрывающую кавычку для содержания.
    /// </summary>
    /// <param name="subject">Содержание.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string AddClosingQuoteToSubject(string subject, IOfficialDocument document)
    {
      return subject.Length > document.Info.Properties.Subject.Length ?
        subject.Substring(0, document.Info.Properties.Subject.Length - 1) + "\"" :
        subject;
    }
    
    #endregion
    
    #region Генерация PDF с отметкой об ЭП
    
    /// <summary>
    /// Получить сообщение об ошибке для неподдерживаемых форматов.
    /// </summary>
    /// <param name="extension">Расширение.</param>
    /// <returns>Результат преобразования.</returns>
    public virtual Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult GetExtensionValidationError(string extension)
    {
      var result = Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult.Create();
      result.HasErrors = true;
      result.ErrorTitle = OfficialDocuments.Resources.ConvertionErrorTitleBase;
      result.ErrorMessage = OfficialDocuments.Resources.ExtensionNotSupportedFormat(extension);
      return result;
    }
    
    #endregion
    
    /// <summary>
    /// Обработать добавление документа как основного вложения в задачу.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <remarks>Только для задач, создаваемых пользователем вручную.</remarks>
    [Public]
    public virtual void DocumentAttachedInMainGroup(Sungero.Workflow.ITask task)
    {
      
    }
    
    /// <summary>
    /// Определить необходимость защиты от редактирования ведущего документа.
    /// </summary>
    /// <returns>True - нужно. False - иначе.</returns>
    [Public]
    public virtual bool NeedDisableLeadingDocument()
    {
      var needDisableByRegistration = this.NeedDisablePropertyByRegistration();
      if (needDisableByRegistration != null)
        return needDisableByRegistration == true;
      
      // При изменении рег.данных разрешено менять ведущий документ у журналов без разреза по ведущему.
      var leadingNumberIncludedInNumber = _obj.DocumentRegister != null &&
        (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber) ||
         _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.LeadingDocument);
      
      return leadingNumberIncludedInNumber;
    }
    
    /// <summary>
    /// Определить необходимость защиты от редактирования НОР.
    /// </summary>
    /// <returns>True - нужно. False - не нужно.</returns>
    [Public]
    public virtual bool NeedDisableBusinessUnit()
    {
      var needDisableByRegistration = this.NeedDisablePropertyByRegistration();
      if (needDisableByRegistration != null)
        return needDisableByRegistration == true;
      
      // При изменении рег.данных разрешено менять НОР у журналов без разреза по НОР.
      var businessUnitIncludedInNumber = _obj.DocumentRegister != null &&
        (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.BUCode) ||
         _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.BusinessUnit);
      
      return businessUnitIncludedInNumber;
    }
    
    /// <summary>
    /// Определить необходимость защиты от редактирования подразделения.
    /// </summary>
    /// <returns>True - нужно. False - не нужно.</returns>
    [Public]
    public virtual bool NeedDisableDepartment()
    {
      var needDisableByRegistration = this.NeedDisablePropertyByRegistration();
      if (needDisableByRegistration != null)
        return needDisableByRegistration == true;
      
      // При изменении рег.данных разрешено менять подразделение у журналов без разреза по подразделению.
      var departmentIncludedInNumber = _obj.DocumentRegister != null &&
        (_obj.DocumentRegister.NumberFormatItems.Any(n => n.Element == Docflow.DocumentRegisterNumberFormatItems.Element.DepartmentCode) ||
         _obj.DocumentRegister.NumberingSection == Docflow.DocumentRegister.NumberingSection.Department);
      
      return departmentIncludedInNumber;
    }
    
    /// <summary>
    /// Определить необходимость защиты от редактирования свойства в зависимости от регистрации документа.
    /// </summary>
    /// <returns>True - нужно. False - иначе.  Null - невозможно окончательно определить.</returns>
    [Public]
    public virtual bool? NeedDisablePropertyByRegistration()
    {
      // Запрещено менять, если нет прав.
      if (!_obj.AccessRights.CanUpdate())
        return true;
      
      // Разрешено менять для не зарегистрированных документов.
      var isNotRegistered = _obj.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.NotRegistered;
      if (isNotRegistered)
        return false;
      
      // Определить смену типа.
      var documentKindOriginalValue = _obj.State.Properties.DocumentKind.OriginalValue;
      var isDocumentTypeChange = documentKindOriginalValue != null &&
        !documentKindOriginalValue.DocumentType.Equals(_obj.DocumentKind.DocumentType);
      
      // Разрешено менять во время регистрации незарегистрированного документа, перерегистрации автонумеруемых или смены типа.
      // Также разрешено менять, когда верификация в процессе.
      var registrationStateOriginalValue = _obj.State.Properties.RegistrationState.OriginalValue;
      var verificationStateOriginalValue = _obj.State.Properties.VerificationState.OriginalValue;
      if (registrationStateOriginalValue == null || registrationStateOriginalValue == Sungero.Docflow.OfficialDocument.RegistrationState.NotRegistered ||
          _obj.DocumentKind.AutoNumbering == true || isDocumentTypeChange ||
          verificationStateOriginalValue == Docflow.OfficialDocument.VerificationState.InProcess)
        return false;
      
      // Запрещено менять значение свойства для зарегистрированных, кроме случаев изменения рег.данных и смены типа.
      var formParams = ((Sungero.Domain.Shared.IExtendedEntity)_obj).Params;
      var repeatRegister = formParams.ContainsKey(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister) &&
        (bool)formParams[Sungero.Docflow.Constants.OfficialDocument.RepeatRegister];
      if (!repeatRegister)
        return true;
      
      return null;
    }
    
    /// <summary>
    /// Получить документы, связанные типом связи "Приложение".
    /// </summary>
    /// <returns>Документы, связанные типом связи "Приложение".</returns>
    [Public]
    public virtual List<IOfficialDocument> GetAddenda()
    {
      return _obj.Relations.GetRelated(Docflow.Constants.Module.AddendumRelationName)
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .Where(x => !Docflow.PublicFunctions.OfficialDocument.IsObsolete(x))
        .ToList();
    }
    
    /// <summary>
    /// Добавить связанные документы в группу вложения.
    /// </summary>
    /// <param name="group">Группа вложения задачи.</param>
    [Public]
    public virtual void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Удалить связанные документы из группы вложения.
    /// </summary>
    /// <param name="group">Группа вложения задачи.</param>
    [Public]
    public virtual void RemoveRelatedDocumentsFromAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Копировать список проектов из ведущего документа.
    /// </summary>
    /// <param name="mainDocument">Ведущий документ.</param>
    /// <param name="document">Документ.</param>
    [Public]
    public static void CopyProjects(IOfficialDocument mainDocument, IOfficialDocument document)
    {
      if (document.DocumentKind != null &&
          document.DocumentKind.ProjectsAccounting == true &&
          mainDocument.DocumentKind != null &&
          mainDocument.DocumentKind.ProjectsAccounting == true)
        document.Project = mainDocument.Project;
    }
    
    /// <summary>
    /// Признак необходимости очистки поля Проект.
    /// </summary>
    /// <param name="e">Аргументы смены вида документа.</param>
    /// <returns>True - нужно очистить, false - не нужно.</returns>
    public virtual bool NeedClearProject(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      // Если в выбранном виде документа не установлен признак "Вести учет по проектам" - нужно очистить проект.
      return e.NewValue == null || e.NewValue.ProjectsAccounting != true;
    }
    
    /// <summary>
    /// Проверить право на удаление документа.
    /// </summary>
    /// <returns>True, если есть права, иначе - false.</returns>
    public bool CheckDeleteEntityAccessRights()
    {
      // Для автонумеруемых типов документов разрешить удаление документа согласно правам доступа.
      var isAutoNumerableDocument = _obj.DocumentKind != null &&
        (_obj.DocumentKind.AutoNumbering ?? false) &&
        _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable;
      
      // Для пронумерованных документов, находящихся в процессе верификации,
      // разрешить удаление документа согласно правам доступа.
      var isDocumentInProcessVerificationStateAndNumbered = _obj.RegistrationState == RegistrationState.Registered &&
        _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable &&
        _obj.VerificationState == Docflow.OfficialDocument.VerificationState.InProcess;
      
      return _obj.AccessRights.CanUpdate() && (isAutoNumerableDocument ||
                                               _obj.RegistrationState != RegistrationState.Reserved &&
                                               _obj.RegistrationState != RegistrationState.Registered ||
                                               isDocumentInProcessVerificationStateAndNumbered);
    }
    
    /// <summary>
    /// Очистка НОР для ненумеруемых документов.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    public void ClearBusinessUnit(IDocumentKind documentKind)
    {
      if (documentKind != null && documentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable &&
          _obj.BusinessUnit != null && !ExchangeDocuments.Is(_obj) && !_obj.State.Properties.BusinessUnit.IsVisible)
        _obj.BusinessUnit = null;
    }
    
    /// <summary>
    /// Проверка на то, что документ является проектным.
    /// </summary>
    /// <returns>True - если документ проектный, иначе - false.</returns>
    [Public, Obsolete("Используйте метод IsProjectDocument(List<int>)")]
    public virtual bool IsProjectDocument()
    {
      return this.IsProjectDocument(new List<int>());
    }
    
    /// <summary>
    /// Проверка на то, что документ является проектным.
    /// </summary>
    /// <param name="leadingDocumentIds">ИД ведущих документов.</param>
    /// <returns>True - если документ проектный, иначе - false.</returns>
    [Public]
    public virtual bool IsProjectDocument(List<int> leadingDocumentIds)
    {
      return _obj.Project != null && _obj.DocumentKind.ProjectsAccounting.Value;
    }
    
    /// <summary>
    /// Получить признак возможности подписания документа при заблокированной карточке.
    /// </summary>
    /// <returns>Признак возможности подписания документа при заблокированной карточке.</returns>
    public virtual bool CanSignLockedDocument()
    {
      var hasCallContext = CallContext.CalledFrom(ApprovalSigningAssignments.Info) || CallContext.CalledFrom(ApprovalReviewAssignments.Info);
      var hasParams = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Constants.OfficialDocument.CanSignLockedDocument);
      return hasCallContext || hasParams;
    }
    
    /// <summary>
    /// Получить признак возможности удаления версии документа.
    /// </summary>
    /// <param name="versionNumber">Номер версии.</param>
    /// <returns>Признак возможности удаления версии документа.</returns>
    [Public]
    public virtual bool CanDeleteVersion(int? versionNumber)
    {
      return Docflow.PublicFunctions.OfficialDocument.Remote.HasAcquaintanceTasks(_obj, versionNumber, true, true);
    }
    
    /// <summary>
    /// Получить признак возможности скрытия версии документа.
    /// </summary>
    /// <param name="versionNumber">Номер версии.</param>
    /// <returns>Признак возможности скрытия версии документа.</returns>
    [Public]
    public virtual bool CanHideVersion(int? versionNumber)
    {
      return Docflow.PublicFunctions.OfficialDocument.Remote.HasAcquaintanceTasks(_obj, versionNumber, false, false);
    }
    
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Проверка, поддерживается ли режим верификации для документа.
    /// </summary>
    /// <returns>True - если поддерживается, иначе - false.</returns>
    [Public]
    public virtual bool IsVerificationModeSupported()
    {
      return false;
    }
    
    /// <summary>
    /// Определить, пронумерован ли документ.
    /// </summary>
    /// <returns>True - документ успешно пронумерован, False - иначе.</returns>
    /// <remarks>Если документ зарегистрирован, а не пронумерован, то вернет false.</remarks>
    [Public]
    public virtual bool IsNumerationSucceed()
    {
      return _obj.RegistrationState == RegistrationState.Registered &&
        (_obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.Numerable) &&
        _obj.DocumentRegister != null;
    }
    
    /// <summary>
    /// Разблокировать реквизиты для верификации после нумерации.
    /// </summary>
    [Public]
    public virtual void EnableRequisitesForVerification()
    {
      if (_obj.VerificationState == VerificationState.InProcess &&
          this.IsNumerationSucceed() &&
          Functions.OfficialDocument.CanChangeRequisitesOrCancelRegistration(_obj) &&
          _obj.AccessRights.CanUpdate())
      {
        var properties = _obj.State.Properties;
        properties.Name.IsEnabled = _obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value;
        properties.DocumentKind.IsEnabled = true;
        properties.Subject.IsEnabled = true;
        properties.BusinessUnit.IsEnabled = true;
        properties.Department.IsEnabled = true;
        
        this.ChangeCounterpartyPropertyAccess(true);
        properties.Assignee.IsEnabled = true;
        
        properties.DeliveryMethod.IsEnabled = true;
        properties.CaseFile.IsEnabled = true;
        properties.PlacedToCaseFileDate.IsEnabled = true;
      }
    }
    
    /// <summary>
    /// Сделать доступными рег. номер и рег. дату незарегистрированного документа регистрируемого вида в процессе верификации.
    /// </summary>
    public virtual void EnableRegistrationNumberAndDate()
    {
      if (_obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.NotNumerable)
        return;
      
      var isRegistrable = _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.Registrable;
      var isNumerable = _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.Numerable;
      var properties = _obj.State.Properties;
      if (isNumerable ||
          isRegistrable && _obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.NotRegistered)
      {
        properties.RegistrationNumber.IsEnabled = true;
        properties.RegistrationDate.IsEnabled = true;
      }
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    public virtual void ChangeCounterpartyPropertyAccess(bool isEnabled)
    {
      // Виртуальная функция. Переопределено в потомках.
    }
    
    /// <summary>
    /// Проверка, заполнены ли обязательные и псевдообязательные свойства.
    /// </summary>
    /// <returns>True - если обязательные и псевдообязательные свойства не заполнены, иначе - false.</returns>
    [Public]
    public virtual bool HasEmptyRequiredProperties()
    {
      // Виртуальная функция. Переопределено в потомках.
      return false;
    }
    
    #endregion
  }
}
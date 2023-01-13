using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalTaskFunctions
  {
    
    /// <summary>
    /// Определить номер следующего этапа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Номер следующего этапа.</returns>
    public static int? GetNextStageNumber(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      return Functions.ApprovalRuleBase.GetNextStageNumber(task.ApprovalRule, document, task.StageNumber, task).Number;
    }
    
    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по базовым этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetRequiredProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      taskProperties.Addressee.IsRequired = refreshParameters.AddresseeIsRequired;
      taskProperties.Addressees.IsRequired = refreshParameters.AddresseesIsRequired;
      taskProperties.Signatory.IsRequired = refreshParameters.SignatoryIsRequired;
      taskProperties.ExchangeService.IsRequired = refreshParameters.ExchangeServiceIsRequired;
    }
    
    /// <summary>
    /// Установить видимость свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по базовым этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetVisibleProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      taskProperties.AddApprovers.IsVisible = refreshParameters.AddApproversIsVisible;
      taskProperties.Addressee.IsVisible = refreshParameters.AddresseeIsVisible;
      taskProperties.Addressees.IsVisible = refreshParameters.AddresseesIsVisible;
      taskProperties.Signatory.IsVisible = refreshParameters.SignatoryIsVisible;
      taskProperties.DeliveryMethod.IsVisible = refreshParameters.DeliveryMethodIsVisible;
      taskProperties.ExchangeService.IsVisible = refreshParameters.ExchangeServiceIsVisible;
    }
    
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по базовым этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetEnabledProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      
      taskProperties.Addressee.IsEnabled = false;
      taskProperties.Addressees.IsEnabled = false;
      taskProperties.ReqApprovers.IsEnabled = false;
      
      if (_obj.ApprovalRule != null)
      {
        taskProperties.Addressee.IsEnabled = refreshParameters.AddresseeIsEnabled;
        taskProperties.Addressees.IsEnabled = refreshParameters.AddresseesIsEnabled;
      }

      var isExchange = _obj.DeliveryMethod != null && _obj.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      taskProperties.ExchangeService.IsEnabled = refreshParameters.ExchangeServiceIsEnabled;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (isExchange && OfficialDocuments.Is(document))
      {
        if (document.Versions.Any())
        {
          taskProperties.DeliveryMethod.IsEnabled = refreshParameters.DeliveryMethodIsEnabled;
          taskProperties.ExchangeService.IsEnabled = refreshParameters.ExchangeServiceIsEnabled;
        }
      }
      
      // Не давать менять адресата в согласовании служебных записок.
      if (Memos.Is(document))
      {
        taskProperties.Addressee.IsEnabled = false;
        taskProperties.Addressees.IsEnabled = false;
      }
      
      // Не давать изменять способ доставки для исходящих писем на несколько адресатов.
      if (OutgoingDocumentBases.Is(document) && OutgoingDocumentBases.As(document).IsManyAddressees == true)
        taskProperties.DeliveryMethod.IsEnabled = false;
    }
    
    /// <summary>
    /// Получить описание способа доставки.
    /// </summary>
    /// <param name="method">Способ доставки.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="isManyAddressees">True, если отправка на несколько адресов, иначе - false.</param>
    /// <returns>Описание способа доставки.</returns>
    public static string GetDeliveryMethodDescription(IMailDeliveryMethod method, ExchangeCore.IExchangeService service, bool isManyAddressees)
    {
      if (isManyAddressees)
        return ApprovalTasks.Resources.DeliveryMethodToManyAddressees;
      if (method == null)
        return string.Empty;
      
      var exchangeServiceDelivery = method.Sid == Constants.MailDeliveryMethod.Exchange;
      
      if (exchangeServiceDelivery)
      {
        if (service == null)
          return string.Empty;
        
        return ApprovalTasks.Resources.DeliveryMethodByExchangeFormat(service.Name);
      }
      
      return ApprovalTasks.Resources.DeliveryMethodNotByExchangeFormat(method.Name);
    }
    
    /// <summary>
    ///  Обновить видимость, доступность и обязательность полей с учетом базовых этапов согласования в карточке задачи.
    /// </summary>
    /// <param name="stages">Этапы согласования.</param>
    /// <param name="updateCache">Обновить параметры обновления формы.</param>
    public void RefreshApprovalTaskForm(List<Structures.Module.DefinedApprovalBaseStageLite> stages, bool updateCache)
    {
      var refreshParameters = updateCache ? null : this.GetRefreshParams((Domain.Shared.IExtendedEntity)_obj);
      if (refreshParameters == null)
      {
        Logger.Debug("Start RefreshApprovalTaskForm(stages)");
        
        refreshParameters = stages == null ? Functions.ApprovalTask.Remote.GetFullStagesInfoForRefresh(_obj) :
          Functions.ApprovalTask.Remote.GetFullStagesInfoForRefresh(_obj, stages);
        
        this.SetRefreshParams((Domain.Shared.IExtendedEntity)_obj, refreshParameters);
      }
      else
        Logger.Debug("Start RefreshApprovalTaskForm(stages) with cache");
      
      this.RefreshProperties(refreshParameters);
    }
    
    /// <summary>
    /// Обновить видимость, доступность и обязательность полей с учетом базовых этапов согласования в карточке задачи.
    /// </summary>
    /// <param name="updateCache">Обновить параметры обновления формы.</param>
    public void RefreshApprovalTaskForm(bool updateCache)
    {
      this.RefreshApprovalTaskForm(null, updateCache);
    }
    
    /// <summary>
    /// Получить или обновить параметры обновления формы задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="updateCache">Обновить параметры обновления формы.</param>
    /// <returns>Параметры обновления формы задания.</returns>
    public virtual Structures.ApprovalTask.RefreshParameters GetOrUpdateAssignmentRefreshParams(Sungero.Workflow.IAssignment assignment, bool updateCache)
    {
      Structures.ApprovalTask.RefreshParameters refreshParameters = null;
      if (!updateCache)
        refreshParameters = this.GetRefreshParams((Domain.Shared.IExtendedEntity)assignment);
      if (refreshParameters == null ||
          refreshParameters.HasDocumentAndCanRead != Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
      {
        Logger.Debug("Start GetOrUpdateAssignmentRefreshParams");
        refreshParameters = Functions.ApprovalTask.Remote.GetFullStagesInfoForRefresh(_obj);
        this.SetRefreshParams((Domain.Shared.IExtendedEntity)assignment, refreshParameters);
      }
      else
        Logger.Debug("Start GetOrUpdateAssignmentRefreshParams with cache");
      
      return refreshParameters;
    }
    
    /// <summary>
    /// Обновить видимость, доступность и обязательность полей с учетом этапов согласования в карточке задачи.
    /// </summary>
    /// <param name="stages">Этапы согласования.</param>
    [Obsolete]
    public void RefreshApprovalTaskForm(List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var fullStages = Functions.ApprovalRuleBase.CastToBaseApprovalStageLite(stages);
      var refreshParameters = Functions.ApprovalTask.Remote.GetFullStagesInfoForRefresh(_obj, fullStages);
      this.RefreshProperties(refreshParameters);
    }
    
    /// <summary>
    /// Получить информацию по базовым этапам для обновления формы задачи из параметров обновления формы.
    /// </summary>
    /// <param name="entity">Задача.</param>
    /// <returns>Информация по базовым этапам для обновления формы задачи на согласование по регламенту.</returns>
    public virtual Structures.ApprovalTask.RefreshParameters GetRefreshParams(Domain.Shared.IExtendedEntity entity)
    {
      var entityParams = entity.Params;
      
      object hasDocumentAndCanRead = null;
      object forwardPerformerIsVisible = null;
      object signatoryIsVisible = null;
      object signatoryIsRequired = null;
      object addresseeIsEnabled = null;
      object addresseeIsVisible = null;
      object addresseeIsRequired = null;
      object addresseesIsEnabled = null;
      object addresseesIsVisible = null;
      object addresseesIsRequired = null;
      object deliveryMethodIsEnabled = null;
      object deliveryMethodIsVisible = null;
      object exchangeServiceIsEnabled = null;
      object exchangeServiceIsVisible = null;
      object exchangeServiceIsRequired = null;
      object approversActionIsEnabled = null;
      object approversIsVisible = null;
      object addApproversIsVisible = null;
      
      var formParamsExist =
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.HasDocumentAndCanReadParamName, out hasDocumentAndCanRead) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ForwardPerformerIsVisibleParamName, out forwardPerformerIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SignatoryIsVisibleParamName, out signatoryIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.SignatoryIsRequiredParamName, out signatoryIsRequired) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsEnabledParamName, out addresseeIsEnabled) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsVisibleParamName, out addresseeIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsRequiredParamName, out addresseeIsRequired) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsEnabledParamName, out addresseesIsEnabled) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsVisibleParamName, out addresseesIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsRequiredParamName, out addresseesIsRequired) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.DeliveryMethodIsEnabledParamName, out deliveryMethodIsEnabled) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.DeliveryMethodIsVisibleParamName, out deliveryMethodIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsEnabledParamName, out exchangeServiceIsEnabled) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsVisibleParamName, out exchangeServiceIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsRequiredParamName, out exchangeServiceIsRequired) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ApproversActionIsEnabledParamName, out approversActionIsEnabled) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.ApproversIsVisibleParamName, out approversIsVisible) &&
        entityParams.TryGetValue(Constants.ApprovalTask.RefreshApprovalTaskForm.AddApproversIsVisibleParamName, out addApproversIsVisible);
      
      if (formParamsExist)
      {
        try
        {
          var formParams = Structures.ApprovalTask.RefreshParameters.Create();
          formParams.HasDocumentAndCanRead = (bool)hasDocumentAndCanRead;
          formParams.ForwardPerformerIsVisible = (bool)forwardPerformerIsVisible;
          formParams.SignatoryIsVisible = (bool)signatoryIsVisible;
          formParams.SignatoryIsRequired = (bool)signatoryIsRequired;
          formParams.AddresseeIsEnabled = (bool)addresseeIsEnabled;
          formParams.AddresseeIsVisible = (bool)addresseeIsVisible;
          formParams.AddresseeIsRequired = (bool)addresseeIsRequired;
          formParams.AddresseesIsEnabled = (bool)addresseesIsEnabled;
          formParams.AddresseesIsVisible = (bool)addresseesIsVisible;
          formParams.AddresseesIsRequired = (bool)addresseesIsRequired;
          formParams.DeliveryMethodIsEnabled = (bool)deliveryMethodIsEnabled;
          formParams.DeliveryMethodIsVisible = (bool)deliveryMethodIsVisible;
          formParams.ExchangeServiceIsEnabled = (bool)exchangeServiceIsEnabled;
          formParams.ExchangeServiceIsVisible = (bool)exchangeServiceIsVisible;
          formParams.ExchangeServiceIsRequired = (bool)exchangeServiceIsRequired;
          formParams.ApproversActionIsEnabled = (bool)approversActionIsEnabled;
          formParams.ApproversIsVisible = (bool)approversIsVisible;
          formParams.AddApproversIsVisible = (bool)addApproversIsVisible;
          
          return formParams;
        } catch
        {
          return null;
        }
        
      }
      
      return null;
    }
    
    /// <summary>
    /// Установить параметры обновления формы.
    /// </summary>
    /// <param name="entity">Задача.</param>
    /// <param name="refreshParameters">Информация по базовым этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetRefreshParams(Domain.Shared.IExtendedEntity entity, Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var entityParams = entity.Params;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.HasDocumentAndCanReadParamName] = refreshParameters.HasDocumentAndCanRead;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ForwardPerformerIsVisibleParamName] = refreshParameters.ForwardPerformerIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.SignatoryIsVisibleParamName] = refreshParameters.SignatoryIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.SignatoryIsRequiredParamName] = refreshParameters.SignatoryIsRequired;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsEnabledParamName] = refreshParameters.AddresseeIsEnabled;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsVisibleParamName] = refreshParameters.AddresseeIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseeIsRequiredParamName] = refreshParameters.AddresseeIsRequired;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsEnabledParamName] = refreshParameters.AddresseesIsEnabled;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsVisibleParamName] = refreshParameters.AddresseesIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddresseesIsRequiredParamName] = refreshParameters.AddresseesIsRequired;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.DeliveryMethodIsEnabledParamName] = refreshParameters.DeliveryMethodIsEnabled;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.DeliveryMethodIsVisibleParamName] = refreshParameters.DeliveryMethodIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsEnabledParamName] = refreshParameters.ExchangeServiceIsEnabled;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsVisibleParamName] = refreshParameters.ExchangeServiceIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ExchangeServiceIsRequiredParamName] = refreshParameters.ExchangeServiceIsRequired;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ApproversActionIsEnabledParamName] = refreshParameters.ApproversActionIsEnabled;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.ApproversIsVisibleParamName] = refreshParameters.ApproversIsVisible;
      entityParams[Constants.ApprovalTask.RefreshApprovalTaskForm.AddApproversIsVisibleParamName] = refreshParameters.AddApproversIsVisible;
    }
    
    /// <summary>
    /// Обновить видимость, доступность и обязательность полей в карточке задачи.
    /// </summary>
    /// <param name="refreshParameters">Структура с данными по этапам согласования.</param>
    public void RefreshProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      Functions.ApprovalTask.SetEnabledProperties(_obj, refreshParameters);
      Functions.ApprovalTask.SetVisibleProperties(_obj, refreshParameters);
      Functions.ApprovalTask.SetRequiredProperties(_obj, refreshParameters);
    }
    
    /// <summary>
    /// Доступно ли указывать в качестве исполнителя задания на доработку не инициатора.
    /// </summary>
    /// <returns>True - если возможно, False - если нельзя.</returns>
    public virtual bool SchemeVersionSupportsRework()
    {
      return _obj.GetStartedSchemeVersion() >= LayerSchemeVersions.V3;
    }
    
    /// <summary>
    /// Доступность результата выполнения "Согласовать с замечаниями".
    /// </summary>
    /// <returns>True - если доступно, иначе - False.</returns>
    public virtual bool SchemeVersionSupportsApproveWithSuggestions()
    {
      return _obj.GetStartedSchemeVersion() >= LayerSchemeVersions.V5;
    }
    
    /// <summary>
    /// Быстрая валидация старта задачи на согласование по регламенту для вызовов из клиентского кода.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ClientValidateApprovalTaskStart(Sungero.Core.IValidationArgs e)
    {
      var haveError = false;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (!Sungero.Company.Employees.Is(_obj.Author))
      {
        e.AddError(_obj.Info.Properties.Author, Docflow.Resources.CantSendTaskByNonEmployee);
        haveError = true;
      }

      if (!document.AccessRights.CanUpdate())
      {
        e.AddError(ApprovalTasks.Resources.CantSendDocumentsWithoutUpdateRights);
        haveError = true;
      }
      
      // Проверить указанность регламента.
      if (_obj.ApprovalRule == null)
      {
        e.AddError(_obj.Info.Properties.ApprovalRule, ApprovalTasks.Resources.ToSendDocumentApprovalSpecifyRule);
        haveError = true;
      }
      
      return !haveError;
    }
    
    /// <summary>
    /// Валидация старта задачи на согласование по регламенту.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateApprovalTaskStart(Sungero.Core.IValidationArgs e)
    {
      var haveError = !this.ClientValidateApprovalTaskStart(e);
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      // Проверить указанность регламента.
      if (_obj.ApprovalRule == null)
        return false;
      
      // Если регламент указан, но есть ошибки в определении условий - значит, не все поля документа заполнены.
      var getStagesResult = Functions.ApprovalTask.Remote.GetStages(_obj);
      if (_obj.ApprovalRule != null && !getStagesResult.IsConditionsDefined)
      {
        e.AddError(getStagesResult.ErrorMessage);
        haveError = true;
      }
      
      // Если регламент указан, а срок не определен - значит, не хватает календаря рабочего времени.
      if (_obj.ApprovalRule != null && !_obj.MaxDeadline.HasValue && Functions.ApprovalTask.Remote.GetExpectedDate(_obj) == null)
      {
        e.AddError(ApprovalTasks.Resources.EmptyNextWorkingCalendar);
        haveError = true;
      }
      
      // Проверить актуальность регламента.
      var documentRules = Functions.OfficialDocument.Remote.GetApprovalRules(document).ToList();
      if (!documentRules.Contains(_obj.ApprovalRule))
      {
        e.AddError(_obj.Info.Properties.ApprovalRule, ApprovalTasks.Resources.RuleOrDocumentHasBeenChanged);
        haveError = true;
      }
      
      // Проверить, имеет ли сотрудник, указанный в поле "На подпись", право подписи документов.
      if (!Functions.ApprovalTask.Remote.CheckSignatory(_obj, _obj.Signatory, getStagesResult.Stages))
      {
        e.AddError(_obj.Info.Properties.Signatory, Docflow.Resources.TheSpecifiedEmployeeIsNotAuthorizedToSignDocuments);
        haveError = true;
      }

      // Проверить, определился ли для этапа регистрации исполнитель.
      var registerStage = getStagesResult.Stages.Where(s => s.StageType == Docflow.ApprovalStage.StageType.Register).FirstOrDefault();
      if (registerStage != null)
      {
        var registerStageAssignee = Functions.ApprovalRuleBase.Remote.GetEmployeeByAssignee(registerStage.Stage.Assignee);
        var clerk = registerStageAssignee ?? Docflow.PublicFunctions.ApprovalStage.Remote.GetRemoteStagePerformer(_obj, registerStage.Stage);
        if (clerk == null)
        {
          e.AddError(ApprovalTasks.Resources.DetermineRegistrarCurrentRuleError);
          haveError = true;
        }
      }
      
      // Проверить, определился ли для этапа создания поручений исполнитель.
      var stages = getStagesResult.Stages;
      var executionStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Execution).FirstOrDefault();
      var reviewStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Review).FirstOrDefault();
      var signStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Sign).FirstOrDefault();
      var reviewStageIndex = stages.IndexOf(reviewStage);
      var signStageIndex = stages.IndexOf(signStage);
      if (executionStage != null)
      {
        var executionStageAssignee = Functions.ApprovalRuleBase.Remote.GetEmployeeByAssignee(executionStage.Stage.Assignee);
        var performer = executionStageAssignee ?? Docflow.PublicFunctions.ApprovalStage.Remote.GetRemoteStagePerformer(_obj, executionStage.Stage);
        if (performer == null && signStageIndex > reviewStageIndex)
        {
          e.AddError(ApprovalTasks.Resources.NoExecutionAssignee);
          haveError = true;
        }
      }
      
      // Проверить, заполнен ли проект, когда его требует роль в правиле.
      if (document.Project == null && stages.Any(s => Functions.ApprovalStage.HasRole(s.Stage, Docflow.ApprovalRoleBase.Type.ProjectManager) ||
                                                 Functions.ApprovalStage.HasRole(s.Stage, Docflow.ApprovalRoleBase.Type.ProjectAdmin)))
      {
        e.AddError(ApprovalTasks.Resources.DocumentMustHaveProject);
        haveError = true;
      }
      
      return !haveError;
    }
    
    /// <summary>
    /// Проверка необходимости асинхронной выдачи прав на вложения.
    /// </summary>
    /// <returns>True - если нужно выдать права асинхронно.</returns>
    public virtual bool NeedGrantAccessRightsOnDocument()
    {
      var documents = new List<Sungero.Docflow.IOfficialDocument>();
      documents.AddRange(_obj.DocumentGroup.OfficialDocuments);
      documents.AddRange(_obj.AddendaGroup.OfficialDocuments);
      var needGrantAccessRightsOnDocument = false;
      foreach (var attachment in documents)
      {
        if (!attachment.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, _obj.Author) &&
            !attachment.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, _obj.Author) &&
            _obj.RevokedDocumentsRights.Any(r => r.DocumentId == attachment.Id && r.RightType != Docflow.ApprovalTaskRevokedDocumentsRights.RightType.Read))
          needGrantAccessRightsOnDocument = true;
      }
      return needGrantAccessRightsOnDocument;
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public virtual void ClearAddresseesAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Addressee == null)
        return;
      var newAddressee = _obj.Addressees.AddNew();
      newAddressee.Addressee = _obj.Addressee;
    }
    
    /// <summary>
    /// Заполнить адресата из коллекции адресатов.
    /// </summary>
    public virtual void FillAddresseeFromAddressees()
    {
      var addressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (addressee != null && !Equals(_obj.Addressee, addressee.Addressee))
        _obj.Addressee = addressee.Addressee;
      else if (addressee == null && _obj.Addressee != null)
        _obj.Addressee = null;
    }
    
    /// <summary>
    /// Синхронизировать адресатов из служебной записки в задачу.
    /// </summary>
    /// <param name="document">Служебная записка.</param>
    public virtual void SychronizeMemoAddressees(IMemo document)
    {
      if (document == null)
        return;
      
      _obj.IsManyAddressees = document.IsManyAddressees;
      
      var addressees = document.Addressees
        .OrderBy(x => x.Number)
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .Distinct()
        .ToList();
      this.SynchronizeAddresses(addressees, document.Addressee);
    }
    
    /// <summary>
    /// Синхронизировать адресатов из переданных значений.
    /// </summary>
    /// <param name="addressees">Список адресатов.</param>
    /// <param name="addressee">Адресат.</param>
    public virtual void SynchronizeAddresses(List<Company.IEmployee> addressees, IEmployee addressee)
    {
      this.SetAddressees(addressees);
      this.FillAddresseeFromAddressees();
      _obj.Addressee = addressee;
    }
    
    /// <summary>
    /// Задать адресатов в задаче.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<Company.IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).Distinct().ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
    
    /// <summary>
    /// Проверить, совпадают ли адресаты задачи со списком.
    /// </summary>
    /// <param name="addressees">Список адресатов для проверки.</param>
    /// <returns>True - адресаты задачи совпадают с переданными, False - иначе.</returns>
    public virtual bool AreAdresseesEquals(List<IEmployee> addressees)
    {
      var taskAddresses = _obj.Addressees
        .Where(x => x != null && x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      addressees = addressees
        .Where(x => x != null)
        .ToList();
      
      return !taskAddresses.Except(addressees).Any() &&
        !addressees.Except(taskAddresses).Any();
    }
    
    #region Синхронизация группы приложений
    
    /// <summary>
    /// Синхронизировать приложения документа и группы вложения.
    /// </summary>
    public virtual void SynchronizeAddendaAndAttachmentsGroup()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document == null)
      {
        _obj.AddendaGroup.All.Clear();
        _obj.AddedAddenda.Clear();
        _obj.RemovedAddenda.Clear();
        return;
      }

      var documentAddenda = Functions.OfficialDocument.GetAddenda(document);
      var taskAddenda = Functions.ApprovalTask.GetAddendaGroupAttachments(_obj);
      var taskAddedAddenda = Functions.ApprovalTask.GetAddedAddenda(_obj);
      var addendaToRemove = taskAddenda.Except(documentAddenda).Where(x => !taskAddedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToRemove)
      {
        _obj.AddendaGroup.All.Remove(addendum);
        this.RemovedAddendaRemove(addendum);
      }
      
      var taskRemovedAddenda = Functions.ApprovalTask.GetRemovedAddenda(_obj);
      var addendaToAdd = documentAddenda.Except(taskAddenda).Where(x => !taskRemovedAddenda.Contains(x.Id)).ToList();
      foreach (var addendum in addendaToAdd)
      {
        _obj.AddendaGroup.All.Add(addendum);
        this.AddedAddendaRemove(addendum);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void AddedAddendaAppend()
    {
      Logger.DebugFormat("ApprovalTask (ID={0}). AddedAddenda append from assignments.", _obj.Id);
      var addedAttachments = this.GetAddedAddendaFromAssignments();
      foreach (var attachment in addedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.AddedAddendaAppend(attachment);
        this.RemovedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче документами из заданий.
    /// </summary>
    public virtual void RemovedAddendaAppend()
    {
      Logger.DebugFormat("ApprovalTask (ID={0}). RemovedAddenda append from assignments.", _obj.Id);
      var removedAttachments = this.GetRemovedAddendaFromAssignments();
      foreach (var attachment in removedAttachments)
      {
        if (attachment == null)
          continue;
        
        this.RemovedAddendaAppend(attachment);
        this.AddedAddendaRemove(attachment);
      }
    }
    
    /// <summary>
    /// Дополнить коллекцию добавленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, добавленный вручную в группу "Приложения".</param>
    public virtual void AddedAddendaAppend(IOfficialDocument addendum)
    {
      if (addendum == null)
        return;
      
      if (_obj.AddedAddenda.Any(x => x.AddendumId == addendum.Id))
        return;
      
      _obj.AddedAddenda.AddNew().AddendumId = addendum.Id;
      Logger.DebugFormat("ApprovalTask (ID={0}). Append AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
    }
    
    /// <summary>
    /// Из коллекции добавленных вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void AddedAddendaRemove(IOfficialDocument addendum)
    {
      if (addendum == null)
        return;
      
      var addedAddendaItem = _obj.AddedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (addedAddendaItem == null)
        return;
      
      _obj.AddedAddenda.Remove(addedAddendaItem);
      Logger.DebugFormat("ApprovalTask (ID={0}). Remove AddedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      
    }
    
    /// <summary>
    /// Дополнить коллекцию удаленных вручную документов в задаче.
    /// </summary>
    /// <param name="addendum">Документ, удаленный вручную из группы "Приложения".</param>
    public virtual void RemovedAddendaAppend(IOfficialDocument addendum)
    {
      if (addendum == null)
        return;
      
      if (_obj.RemovedAddenda.Any(x => x.AddendumId == addendum.Id))
        return;
      
      _obj.RemovedAddenda.AddNew().AddendumId = addendum.Id;
      Logger.DebugFormat("ApprovalTask (ID={0}). Append RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
    }
    
    /// <summary>
    /// Из коллекции удаленных вручную документов удалить запись о приложении.
    /// </summary>
    /// <param name="addendum">Удаляемый документ.</param>
    public virtual void RemovedAddendaRemove(IOfficialDocument addendum)
    {
      if (addendum == null)
        return;
      
      var removedAddendaItem = _obj.RemovedAddenda.Where(x => x.AddendumId == addendum.Id).FirstOrDefault();
      if (removedAddendaItem == null)
        return;
      
      _obj.RemovedAddenda.Remove(removedAddendaItem);
      Logger.DebugFormat("ApprovalTask (ID={0}). Remove RemovedAddenda. Document (Id={1}).", _obj.Id, addendum.Id);
      
    }
    
    /// <summary>
    /// Получить вложения группы "Приложения".
    /// </summary>
    /// <returns>Вложения группы "Приложения".</returns>
    [Public]
    public virtual List<IOfficialDocument> GetAddendaGroupAttachments()
    {
      return _obj.AddendaGroup.All
        .Where(x => OfficialDocuments.Is(x))
        .Select(x => OfficialDocuments.As(x))
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, добавленных в группу "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    [Public]
    public virtual List<int> GetAddedAddenda()
    {
      return _obj.AddedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Получить список ИД документов, удаленных из группы "Приложения".
    /// </summary>
    /// <returns>Список ИД документов.</returns>
    [Public]
    public virtual List<int> GetRemovedAddenda()
    {
      return _obj.RemovedAddenda
        .Where(x => x.AddendumId.HasValue)
        .Select(x => x.AddendumId.Value)
        .ToList();
    }
    
    /// <summary>
    /// Получить список документов, добавленных в группу "Приложения" в заданиях.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IOfficialDocument> GetAddedAddendaFromAssignments()
    {
      var addedAddenda = new List<IOfficialDocument>();
      
      var addendaHistory = Functions.ApprovalTask.Remote.GetAttachmentHistoryEntriesByGroupId(_obj, Constants.ApprovalTask.AddendaGroupGuid);
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
          var attachment = Functions.Module.Remote.GetDocumentById(id);
          if (attachment == null)
            continue;
          addedAddenda.Add(attachment);
        }
      }
      
      return addedAddenda;
    }
    
    /// <summary>
    /// Получить список документов, удаленных из группы "Приложения" в заданиях.
    /// </summary>
    /// <returns>Список документов.</returns>
    public virtual List<IOfficialDocument> GetRemovedAddendaFromAssignments()
    {
      var removedAddenda = new List<IOfficialDocument>();
      
      var addendaHistory = Functions.ApprovalTask.Remote.GetAttachmentHistoryEntriesByGroupId(_obj, Constants.ApprovalTask.AddendaGroupGuid);
      var removedFromHistoryIds = addendaHistory.Removed
        .Select(x => x.DocumentId)
        .Distinct()
        .ToList();
      foreach (var id in removedFromHistoryIds)
      {
        var lastAddedDate = Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Added, id);
        var lastRemovedDate = Functions.Module.GetMaxHistoryOperationDateById(addendaHistory.Removed, id);
        
        if (lastRemovedDate.HasValue && (!lastAddedDate.HasValue || lastRemovedDate.Value > lastAddedDate.Value))
        {
          var attachment = Functions.Module.Remote.GetDocumentById(id);
          if (attachment == null)
            continue;
          removedAddenda.Add(attachment);
        }
      }
      
      return removedAddenda;
    }
    
    #endregion
    
    /// <summary>
    /// Получить параметры для отправки на доработку.
    /// </summary>
    /// <param name="stageNumber">Номер этапа.</param>
    /// <returns>Параметры доработки.</returns>
    public virtual Sungero.Docflow.Structures.ApprovalTask.ReworkParameters GetAssignmentReworkParameters(int stageNumber)
    {
      var reworkParameters = Structures.ApprovalTask.ReworkParameters.Create();
      reworkParameters.AllowChangeReworkPerformer = false;
      reworkParameters.AllowViewReworkPerformer = false;
      reworkParameters.AllowSendToRework = false;
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
        return reworkParameters;
      var item = _obj.ApprovalRule.Stages.Where(s => s.Number == stageNumber).FirstOrDefault();
      if (item == null)
        return reworkParameters;
      var stage = item.Stage;
      reworkParameters.AllowChangeReworkPerformer = stage.AllowChangeReworkPerformer ?? false;
      reworkParameters.AllowViewReworkPerformer = stage.AllowChangeReworkPerformer ?? false;
      reworkParameters.AllowSendToRework = stage.AllowSendToRework ?? false;
      return reworkParameters;
    }
    
    /// <summary>
    /// Проверить наличие согласуемого документа в задаче и наличие хоть каких-то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentGroup.OfficialDocuments.Any();
    }
  }
}
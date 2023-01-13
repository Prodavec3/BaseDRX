using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.Docflow
{
  partial class OfficialDocumentOurSigningReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OurSigningReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var now = Calendar.Now;
      var availableSettings = Functions.OfficialDocument.GetSignatureSettingsWithCertificateByEmployee(_obj, _obj.OurSignatory);
      query = query.Where(x => availableSettings.Contains(x));
      return query;
    }
  }

  partial class OfficialDocumentCaseFilePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CaseFileFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return PublicFunctions.Module.CaseFileFiltering(_obj, query).Cast<T>();
    }
  }

  partial class OfficialDocumentDocumentKindSearchPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindSearchDialogFiltering(IQueryable<T> query, Sungero.Domain.PropertySearchDialogFilteringEventArgs e)
    {
      if (e.EntityType != null)
        query = query.Where(k => k.DocumentType.DocumentTypeGuid == e.EntityType);

      return query;
    }
  }

  partial class OfficialDocumentConvertingFromServerHandler
  {

    public override void ConvertingFrom(Sungero.Domain.ConvertingFromEventArgs e)
    {
      base.ConvertingFrom(e);
      
      var sourceOfficialDocument = OfficialDocuments.As(_source);
      
      // Не копировать порядковый номер.
      e.Without(_info.Properties.Index);
      
      // Не копируем состояние ЖЦ для Вх. документа эл. обмена.
      if (ExchangeDocuments.Is(_source) &&
          sourceOfficialDocument.LifeCycleState != OfficialDocument.LifeCycleState.Obsolete)
        e.Without(_info.Properties.LifeCycleState);
      
      // Добавить параметр того, что документ меняет тип.
      var paramName = string.Format("doc{0}_ConvertingFrom", _source.Id);
      e.Params.AddOrUpdate(paramName, true);
      
      var sourceEntityGUID = _source.GetEntityMetadata().GetOriginal().NameGuid.ToString();
      var recognitionInfo = Commons.EntityRecognitionInfos.GetAll()
        .Where(r => r.EntityId == _source.Id && r.EntityType == sourceEntityGUID)
        .OrderByDescending(r => r.Id)
        .FirstOrDefault();
      
      if (recognitionInfo != null && sourceOfficialDocument.VerificationState == OfficialDocument.VerificationState.InProcess)
        Sungero.Commons.PublicFunctions.EntityRecognitionInfo.Remote.Clone(recognitionInfo, e.Entity);
      
      var businessUnit = Exchange.PublicFunctions.ExchangeDocumentInfo.GetDocumentBusinessUnit(_source, _source.LastVersion);
      if (businessUnit != null)
        e.Map(_info.Properties.BusinessUnit, businessUnit);
    }
  }

  partial class OfficialDocumentProjectPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ProjectFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed);
    }
  }

  partial class OfficialDocumentOurSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> OurSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;

      if (Functions.OfficialDocument.SignatorySettingWithAllUsersExist(_obj))
        return query;
      
      var signatories = Functions.OfficialDocument.GetSignatoriesIds(_obj);
      
      return query.Where(s => signatories.Contains(s.Id));
    }
  }

  partial class OfficialDocumentDocumentGroupPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return query;
      
      var availableGroups = Functions.DocumentGroupBase.GetAvailableDocumentGroup(documentKind).ToList();
      return query.Where(a => availableGroups.Contains(a));

    }
  }

  partial class OfficialDocumentDocumentRegisterPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentKind != null)
      {
        var availableDocumentRegistersIds = Functions.OfficialDocument.GetDocumentRegistersByDocument(_obj);
        query = query.Where(l => availableDocumentRegistersIds.Contains(l.Id));
      }
      return query;
    }
  }

  partial class OfficialDocumentDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.RegistrationState == RegistrationState.Registered)
        query = query.Where(k => k.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable);
      
      var availableDocumentKinds = Functions.DocumentKind.GetAvailableDocumentKinds(_obj);
      query = query.Where(k => availableDocumentKinds.Contains(k));
      
      query = PublicFunctions.Module.FilterDocumentKindsByAccessRights(query).Cast<T>();
      return query;
    }
  }

  partial class OfficialDocumentCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      // Область регистрации.
      e.Without(_info.Properties.RegistrationNumber);
      e.Without(_info.Properties.RegistrationDate);
      e.Without(_info.Properties.DocumentRegister);
      e.Without(_info.Properties.DeliveryMethod);
      e.Without(_info.Properties.CaseFile);
      e.Without(_info.Properties.PlacedToCaseFileDate);
      e.Without(_info.Properties.Tracking);

      // Статусы жизненного цикла.
      e.Without(_info.Properties.LifeCycleState);
      e.Without(_info.Properties.RegistrationState);
      e.Without(_info.Properties.VerificationState);
      e.Without(_info.Properties.InternalApprovalState);
      e.Without(_info.Properties.ExternalApprovalState);
      e.Without(_info.Properties.ExecutionState);
      e.Without(_info.Properties.ControlExecutionState);
      e.Without(_info.Properties.LocationState);
      e.Without(_info.Properties.ExchangeState);
      
      // Свойства "Подписал" и "Основание".
      e.Without(_info.Properties.OurSignatory);
      e.Without(_info.Properties.OurSigningReason);
      
      // Свойство "Исполнитель".
      e.Without(_info.Properties.Assignee);
      
      e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }
  }

  partial class OfficialDocumentServerHandlers
  {

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      // Удалить результаты распознавания документа после его удаления.
      var asyncDeleteRecognitionInfoHandler = SmartProcessing.AsyncHandlers.DeleteEntityRecognitionInfo.Create();
      asyncDeleteRecognitionInfoHandler.EntityId = _obj.Id;
      asyncDeleteRecognitionInfoHandler.ExecuteAsync();
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      // Сбрасывать значение параметра, так как после сохранения рег. данные должны быть валидны.
      bool paramValue;
      if (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, out paramValue) && paramValue)
        e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat);

      // TODO: удалить код после исправления бага платформы 100433.
      var isTransferring = _obj.Versions.Any(v => (v.Body != null && v.Body.Size > 0 && ((Sungero.Domain.BinaryData)v.Body).IsTransferring) ||
                                             (v.PublicBody != null && v.PublicBody.Size > 0 && ((Sungero.Domain.BinaryData)v.PublicBody).IsTransferring));

      if (e.Params.Contains(Docflow.PublicConstants.OfficialDocument.GrantAccessRightsToDocumentAsync) && !isTransferring)
      {
        if (e.Params.Contains(Constants.OfficialDocument.GrantAccessRightsToProjectDocument))
        {
          Sungero.Projects.Jobs.GrantAccessRightsToProjectDocuments.Enqueue();
          e.Params.Remove(Constants.OfficialDocument.GrantAccessRightsToProjectDocument);
        }
        PublicFunctions.Module.CreateGrantAccessRightsToDocumentAsyncHandler(_obj.Id, new List<int>(), true);
        e.Params.Remove(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync);
      }
      
      // Интеллектуальная обработка. Сохранить подтверждённые пользователем значения.
      Docflow.PublicFunctions.OfficialDocument.StoreVerifiedPropertiesValues(_obj);
    }

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      var ids = Functions.OfficialDocument.GetTaskIdsWhereDocumentInRequredGroup(_obj);
      if (ids.Any())
        throw AppliedCodeException.Create(OfficialDocuments.Resources.DocumentUseInTasksFormat(string.Join(", ", ids.ToArray())));
      
      var canDelete = Functions.OfficialDocument.CheckDeleteEntityAccessRights(_obj);
      if (!_obj.AccessRights.CanDelete() || !canDelete)
        throw AppliedCodeException.Create(OfficialDocuments.Resources.NoRightsToUpdateOrDelete);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Заполнить статус регистрации по умолчанию.
      _obj.RegistrationState = RegistrationState.NotRegistered;
      _obj.State.Properties.RegistrationState.IsEnabled = false;
      
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
      Functions.OfficialDocument.FillOrganizationStructure(_obj);
      
      // Переопределить вид документа при смене типа.
      var paramName = string.Format("doc{0}_ConvertingFrom", _obj.Id);
      if (e.Params.Contains(paramName))
      {
        // При смене вида происходит удаление параметра.
        _obj.DocumentKind = Functions.OfficialDocument.GetDefaultDocumentKind(_obj);
      }
      
      // Заполнить вид документа.
      if (_obj.DocumentKind == null)
      {
        // Заполнить вид документа, если подобрался вид по умолчанию.
        var defaultDocumentKind = Functions.OfficialDocument.GetDefaultDocumentKind(_obj);
        if (defaultDocumentKind != null)
          _obj.DocumentKind = defaultDocumentKind;
      }
      else if (_obj.State.IsCopied &&
               _obj.DocumentKind.Status == Sungero.Docflow.DocumentKind.Status.Closed)
      {
        // Если скопировали документ с закрытым видом - очищаем поле.
        _obj.DocumentKind = null;
      }

      // Заполнить статус жизненного цикла в зависимости от вида документа.
      Functions.OfficialDocument.SetLifeCycleState(_obj);
      if (_obj.LifeCycleState == null)
        _obj.LifeCycleState = OfficialDocument.LifeCycleState.Draft;
    }

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      var canSignLockedDocument = Functions.OfficialDocument.CanSignLockedDocument(_obj);
      var lockInfo = Locks.GetLockInfo(_obj);
      if (lockInfo == null || !lockInfo.IsLockedByOther || !canSignLockedDocument)
      {
        if (e.Signature.SignatureType == SignatureType.Approval && e.Signature.Signatory != null)
        {
          // Заполнить статус согласования "Подписан".
          Functions.OfficialDocument.SetInternalApprovalStateToSigned(_obj);
          
          var changedSignatory = !Equals(_obj.OurSignatory, Company.Employees.Current);
          
          // Заполнить подписывающего в карточке документа.
          Functions.OfficialDocument.SetDocumentSignatory(_obj, Company.Employees.Current);

          // Заполнить основание в карточке документа.
          Functions.OfficialDocument.SetOurSigningReason(_obj, Company.Employees.Current, e, changedSignatory);
          
          // Заполнить Единый рег. № из эл. доверенности в подпись.
          Functions.OfficialDocument.SetUnifiedRegistrationNumber(_obj, Company.Employees.Current, e.Signature, e.Certificate);
        }
      }
      
      // Если подписание выполняется в рамках агента - генерировать заглушку не надо.
      bool jobRan;
      if (e.Params.TryGetValue(ExchangeCore.PublicConstants.BoxBase.JobRunned, out jobRan) && jobRan)
        return;
      
      var versionId = (e.Signature as IInternalSignature).SignedEntityProperties
        .Select(p => p.ChildEntityId).Single();
      var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, versionId.Value);
      
      var version = _obj.Versions.Single(v => v.Id == versionId);
      if (e.Signature.SignatureType == SignatureType.Approval &&
          info != null &&
          !Signatures.Get(version).Any(s => s.SignatureType == SignatureType.Approval && s.Id != info.SenderSignId))
      {
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, version.Id);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, version.Id, _obj.ExchangeState);
      }
    }

    public override void BeforeSaveHistory(Sungero.Content.DocumentHistoryEventArgs e)
    {
      var isUpdateAction = e.Action == Sungero.CoreEntities.History.Action.Update;
      var isVersionCreateAction = e.Action == Sungero.CoreEntities.History.Action.Update &&
        e.Operation == new Enumeration(Constants.OfficialDocument.Operation.CreateVersion);
      var isCreateAction = e.Action == Sungero.CoreEntities.History.Action.Create;
      var isChangeTypeAction = e.Action == Sungero.CoreEntities.History.Action.ChangeType;
      var properties = _obj.State.Properties;
      
      var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      if (isVersionCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.FindByBarcodeParamName))
        e.Comment = Sungero.Docflow.OfficialDocuments.Resources.VersionCreatedByCaptureService;
      
      var isAddRegistrationStampAction = isVersionCreateAction && documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentAboutRegistrationStamp);
      if (isAddRegistrationStampAction)
        e.Comment = Sungero.Docflow.OfficialDocuments.Resources.VersionWithRegistrationStamp;
      
      // Изменять историю для изменения, создания и смены типа документа. Историю для создания версии не изменять,
      // кроме случая, когда версия создана с отметкой о регистрации.
      if ((!isUpdateAction || isVersionCreateAction && !isAddRegistrationStampAction) && !isCreateAction && !isChangeTypeAction)
        return;
      
      var historyRecordOverwritten = false;
      
      #region Очистка рег. данных при смене типа
      
      // Определить, что произошла смена типа документа.
      var documentKindOriginalValue = _obj.State.Properties.DocumentKind.OriginalValue;
      var documentTypeChange = documentKindOriginalValue != null &&
        !documentKindOriginalValue.DocumentType.Equals(_obj.DocumentKind.DocumentType);
      
      // При смене типа для нумеруемых документов автоматически очищаются рег.данные.
      // Записать в историю информацию об очистке полей регистрации.
      var changeTypeUnregistration = isChangeTypeAction && !string.IsNullOrWhiteSpace(properties.RegistrationNumber.OriginalValue);
      if (changeTypeUnregistration)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          /*
           * Только для нумеруемых. У ненумеруемого нечего очищать.
           * Для регистрируемого нельзя сменить тип, пока он зарегистрирован.
           */
          var numberingTypeOriginalValue = documentKindOriginalValue.NumberingType;
          var isSubstitute = !_obj.AccessRights.GetSubstitutedWhoCanRegister().Any(u => u.Id == Users.Current.Id);
          if (isSubstitute && _obj.RegistrationState != RegistrationState.Registered)
            isSubstitute = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) == null;
          
          if (numberingTypeOriginalValue.Equals(DocumentKind.NumberingType.Numerable))
            e.Write(new Enumeration(Constants.OfficialDocument.Operation.Unnumeration), null, OfficialDocuments.Resources.ChangeTypeUnnumerationComment, isSubstitute);
        }
      }
      
      #endregion
      
      #region История регистрации
      
      var registrationState = _obj.RegistrationState;
      var registrationStateIsChanged = registrationState != properties.RegistrationState.OriginalValue;
      var registrationDataChanged =
        (registrationStateIsChanged && (!isCreateAction || registrationState != RegistrationState.NotRegistered)) ||
        _obj.RegistrationNumber != properties.RegistrationNumber.OriginalValue ||
        _obj.RegistrationDate != properties.RegistrationDate.OriginalValue ||
        _obj.DocumentRegister != properties.DocumentRegister.OriginalValue;
      
      if (registrationDataChanged)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          var isDocumentNotifiable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
          var isDocumentReserved = registrationState == RegistrationState.Reserved;
          var wasDocumentReserved = properties.RegistrationState.OriginalValue == RegistrationState.Reserved;
          
          var unregistrationEventName = wasDocumentReserved ? Constants.OfficialDocument.Operation.Unreservation :
            (isDocumentNotifiable ? Constants.OfficialDocument.Operation.Unregistration : Constants.OfficialDocument.Operation.Unnumeration);
          
          var operation = e.Operation;
          var operationDetailed = e.OperationDetailed;
          var separator = "|";
          var registrationDate = _obj.RegistrationDate.HasValue ? _obj.RegistrationDate.Value.ToString("d") : string.Empty;
          var comment = !string.IsNullOrWhiteSpace(_obj.RegistrationNumber) ?
            string.Join(separator, _obj.RegistrationNumber, registrationDate, _obj.DocumentRegister) :
            e.Comment;
          
          // Изменение статуса регистрации. При смене типа у пронумерованного документа также идет изменение статуса.
          if (registrationStateIsChanged || changeTypeUnregistration)
          {
            // Регистрация.
            if (registrationState == RegistrationState.Registered)
            {
              if (isDocumentNotifiable)
              {
                operation = new Enumeration(Functions.OfficialDocument.GetRegistrationOperation());
                operationDetailed = operation;
              }
              else
              {
                operation = new Enumeration(Constants.OfficialDocument.Operation.Numeration);
                operationDetailed = operation;
              }
            }
            else if (isDocumentReserved)
            {
              // Резервирование.
              operation = new Enumeration(Constants.OfficialDocument.Operation.Reservation);
              operationDetailed = operation;
            }
            else if (registrationState == RegistrationState.NotRegistered && isUpdateAction && !changeTypeUnregistration)
            {
              // Отмена регистрации.
              operation = new Enumeration(unregistrationEventName);
            }
          }
          else
          {
            // Изменение только регистрационных данных.
            operation = new Enumeration(isDocumentNotifiable ? Constants.OfficialDocument.Operation.ChangeRegistration : Constants.OfficialDocument.Operation.ChangeNumeration);
            operationDetailed = operation;
          }
          
          var isSubstitute = !_obj.AccessRights.GetSubstitutedWhoCanRegister().Any(u => u.Id == Users.Current.Id);
          if (isSubstitute && registrationState != RegistrationState.Registered)
            isSubstitute = Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) == null;
          
          // Добавить отдельную запись истории, если регистрация/нумерация происходят при создании или смене типа.
          if (isCreateAction || isChangeTypeAction && registrationState != RegistrationState.NotRegistered)
            e.Write(operation, operationDetailed, comment, isSubstitute);
          else
          {
            e.Operation = operation;
            e.OperationDetailed = operationDetailed;
            e.Comment = comment;
            e.IsSubstitute = isSubstitute;
            historyRecordOverwritten = true;
          }
        }
      }
      
      #endregion
      
      #region История смены состояний
      
      /*
       * Для любой смены состояния:
       * - всегда писать отдельной строкой, если это смена типа документа;
       * - дописывать в историю, если это не смена типа.
       */
      
      // Статус "Согласование".
      if (_obj.InternalApprovalState != properties.InternalApprovalState.OriginalValue)
      {
        var operation = Functions.OfficialDocument.GetHistoryOperationByLifeCycleState(_obj.InternalApprovalState, Constants.OfficialDocument.Operation.Prefix.InternalApproval, isUpdateAction) ?? e.Operation;
        if (isCreateAction || historyRecordOverwritten || isChangeTypeAction)
          e.Write(operation, null, string.Empty);
        else if (!documentTypeChange)
        {
          e.Operation = operation;
          historyRecordOverwritten = true;
        }
      }

      // Статус "Согл. с контрагентом".
      if (_obj.ExternalApprovalState != properties.ExternalApprovalState.OriginalValue)
      {
        var operation = Functions.OfficialDocument.GetHistoryOperationByLifeCycleState(_obj.ExternalApprovalState, Constants.OfficialDocument.Operation.Prefix.ExternalApproval, isUpdateAction) ?? e.Operation;
        if (isCreateAction || historyRecordOverwritten || isChangeTypeAction)
          e.Write(operation, null, string.Empty);
        else if (!documentTypeChange)
        {
          e.Operation = operation;
          historyRecordOverwritten = true;
        }
      }

      // Статус "Исполнение".
      if (_obj.ExecutionState != properties.ExecutionState.OriginalValue)
      {
        var operation = Functions.OfficialDocument.GetHistoryOperationByLifeCycleState(_obj.ExecutionState, Constants.OfficialDocument.Operation.Prefix.Execution, isUpdateAction) ?? e.Operation;
        if (isCreateAction || historyRecordOverwritten || isChangeTypeAction)
          e.Write(operation, null, string.Empty);
        else if (!documentTypeChange)
        {
          e.Operation = operation;
          historyRecordOverwritten = true;
        }
      }

      // Статус "Контроль исполнения".
      if (_obj.ControlExecutionState != properties.ControlExecutionState.OriginalValue)
      {
        var operation = Functions.OfficialDocument.GetHistoryOperationByLifeCycleState(_obj.ControlExecutionState, Constants.OfficialDocument.Operation.Prefix.ControlExecution, isUpdateAction) ?? e.Operation;
        if (isCreateAction || historyRecordOverwritten  || isChangeTypeAction)
          e.Write(operation, null, string.Empty);
        else if (!documentTypeChange)
        {
          e.Operation = operation;
          historyRecordOverwritten = true;
        }
      }

      // Статус "Жизненный цикл".
      if (_obj.LifeCycleState != properties.LifeCycleState.OriginalValue)
      {
        var operation = Functions.OfficialDocument.GetHistoryOperationByLifeCycleState(_obj.LifeCycleState, Constants.OfficialDocument.Operation.Prefix.LifeCycle, isUpdateAction) ?? e.Operation;
        
        // Добавить отдельную запись изменения состояния, если также были изменены рег.данные - это создание документа или смена типа.
        if (isCreateAction || historyRecordOverwritten || isChangeTypeAction)
          e.Write(operation, null, string.Empty);
        else if (!documentTypeChange)
          e.Operation = operation;
      }

      #endregion
      
      var isConvertToPdfAction = e.Operation == Content.DocumentHistory.Operation.UpdateVerBody &&
        documentParams.ContainsKey(Docflow.PublicConstants.OfficialDocument.AddHistoryCommentAboutPDFConvert);
      if (isConvertToPdfAction)
      {
        var comment = Sungero.Docflow.OfficialDocuments.Resources.ConvertToPdfHistoryComment;
        if (string.IsNullOrEmpty(e.Comment))
        {
          e.Comment = comment;
        }
        else
        {
          var operation = new Enumeration(Constants.OfficialDocument.Operation.ContentChange);
          var version = e.VersionNumber;
          e.Write(operation, null, comment, version);
        }
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.DocumentRegister != null)
      {
        var registrationGroup = _obj.DocumentRegister.RegistrationGroup;
        
        // Выдать права группе регистрации. Заодно обновить кешированную проверку на доступность поля "Исполнитель".
        if (registrationGroup != null)
        {
          if (_obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
            _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
          e.Params.AddOrUpdate(Constants.OfficialDocument.CanChangeAssignee, Functions.OfficialDocument.CanChangeAssignee(_obj));
        }
      }
      
      // Проверить неизменность подразделения, если нет прав на его изменение.
      var departmentChanged = _obj.Department != _obj.State.Properties.Department.OriginalValue;
      if (departmentChanged)
      {
        var departmentDisabled = Functions.OfficialDocument.NeedDisableDepartment(_obj);
        if (departmentDisabled)
          e.AddError(Sungero.Docflow.OfficialDocuments.Resources.DepartmentPropertyDisabled);
      }
      
      // Проверить неизменность НОР, если нет прав на ее изменение.
      var businessUnitChanged = _obj.BusinessUnit != _obj.State.Properties.BusinessUnit.OriginalValue;
      if (businessUnitChanged)
      {
        var businessUnitDisabled = Functions.OfficialDocument.NeedDisableBusinessUnit(_obj);
        if (businessUnitDisabled)
          e.AddError(Sungero.Docflow.OfficialDocuments.Resources.BusinessUnitPropertyDisabled);
      }
      
      // Установить для документа хранилище из последней версии.
      if (_obj.Storage == null && _obj.LastVersion != null)
        _obj.Storage = _obj.LastVersion.Body.Storage;
      if (_obj.Storage != null && !_obj.HasVersions)
        _obj.Storage = null;
      
      var isTransferring = _obj.State.IsBinaryDataTransferring;
      
      // TODO: удалить код после исправления бага платформы 46179.
      if (!e.Params.Contains(Constants.OfficialDocument.DontUpdateModified) && !isTransferring)
        _obj.Modified = Calendar.Now;
      else if ((e.Params.Contains(Constants.OfficialDocument.DontUpdateModified) || isTransferring) && _obj.Modified != _obj.State.Properties.Modified.OriginalValue)
        _obj.Modified = _obj.State.Properties.Modified.OriginalValue;

      // Не вызывать сохранение, если изменилось только тело документа.
      if (Functions.OfficialDocument.IsOnlyVersionChanged(_obj))
        return;
      
      var documentKindOriginalValue = _obj.State.Properties.DocumentKind.OriginalValue;
      var isDocumentTypeChange = documentKindOriginalValue != null &&
        !documentKindOriginalValue.DocumentType.Equals(_obj.DocumentKind.DocumentType);
      var isOnVerification = _obj.VerificationState == VerificationState.InProcess ||
        _obj.State.Properties.VerificationState.IsChanged && _obj.VerificationState == VerificationState.Completed;
      
      #region Регистрация/нумерация
      
      var isRegistered = _obj.RegistrationState == RegistrationState.Registered;
      var isNotRegistered = _obj.RegistrationState == RegistrationState.NotRegistered;
      var documentKind = _obj.DocumentKind;
      
      var numberingType = documentKind != null ? documentKind.NumberingType : null;
      var isRegistrable = numberingType == Docflow.DocumentKind.NumberingType.Registrable;
      var isNumerable = numberingType == Docflow.DocumentKind.NumberingType.Numerable;
      var isNotNumerable = numberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      
      var originalNumberingType = documentKindOriginalValue != null ? documentKindOriginalValue.NumberingType : null;
      var isOriginalRegistered = _obj.State.Properties.RegistrationState.OriginalValue != RegistrationState.NotRegistered;
      
      // Данные для валидации рег. номера.
      var depCode = _obj.Department != null ? _obj.Department.Code : string.Empty;
      var bunitCode = _obj.BusinessUnit != null ? _obj.BusinessUnit.Code : string.Empty;
      var caseIndex = _obj.CaseFile != null ? _obj.CaseFile.Index : string.Empty;
      var kindCode = _obj.DocumentKind != null ? _obj.DocumentKind.Code : string.Empty;
      var counterpartyCode = Functions.OfficialDocument.GetCounterpartyCode(_obj);
      // Возможен корректировочный постфикс или нет (возможен, если необходимо проверять на уникальность).
      var correctingPostfixInNumberIsAvailable = Functions.OfficialDocument.CheckRegistrationNumberUnique(_obj);

      // Если изменен вид в зарегистрированном/пронумерованном документе и в виде и журнале отличается признак Регистрируемый/Нумеруемый, то выводим хинт.
      if (isOriginalRegistered)
      {
        if (isRegistered && !Equals(numberingType, originalNumberingType) && originalNumberingType == Docflow.DocumentKind.NumberingType.Registrable)
          e.AddError(OfficialDocuments.Resources.CannotChangeRegisteredDocumentToNumerable);
        
        if (isRegistered && !Equals(numberingType, originalNumberingType) && originalNumberingType == Docflow.DocumentKind.NumberingType.Numerable)
          e.AddError(OfficialDocuments.Resources.CannotChangeNumeratedDocumentToRegistrable);
      }
      
      // Автонумерация документа.
      var isAutoNumbering = documentKind != null && documentKind.AutoNumbering == true &&
        isNotRegistered &&
        e.IsValid &&
        !Functions.OfficialDocument.IsObsolete(_obj, _obj.LifeCycleState);

      if (isAutoNumbering && _obj.VerificationState != VerificationState.InProcess)
      {
        var documentRegistersIds = Functions.OfficialDocument.GetDocumentRegistersIdsByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
        if (!documentRegistersIds.Any())
        {
          e.AddError(Docflow.Resources.NumberingSettingsRequiredForSave);
          return;
        }
        
        var register = DocumentRegisters.Get(documentRegistersIds.First());
        
        // Заполнить дело, если оно будет заполнено после регистрации.
        if (_obj.CaseFile == null)
          Functions.OfficialDocument.FillCaseFileAndDeliveryMethod(_obj, register);
        caseIndex = _obj.CaseFile == null ? string.Empty : _obj.CaseFile.Index;
        
        Functions.OfficialDocument.RegisterDocument(_obj, register, Calendar.UserToday, null, false, false);
        
        // Добавить параметр о необходимости валидации.
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
      }
      
      // Игнорировать проверку прав на регистрацию и валидацию рег. номера при смене типа документа.
      if (!isDocumentTypeChange)
      {
        // Проверить наличие прав на журнал регистрации при регистрации системным пользователем.
        if (_obj.DocumentRegister != null &&
            _obj.State.Properties.DocumentRegister.OriginalValue != _obj.DocumentRegister &&
            documentKind != null &&
            Users.Current.IsSystem != true)
        {
          var documentRegisters = Functions.OfficialDocument.GetDocumentRegistersByDocument(_obj);
          if (!documentRegisters.Contains(_obj.DocumentRegister.Id))
          {
            e.AddError(Docflow.Resources.NoRightToRegistrationInDocumentRegister);
            return;
          }
        }
        
        // Убрать начальные и конечные пробелы в рег. номере.
        if (_obj.RegistrationNumber != null && System.Text.RegularExpressions.Regex.IsMatch(_obj.RegistrationNumber, @"\s"))
          _obj.RegistrationNumber = _obj.RegistrationNumber.Trim();
        
        // Проверить рег.номер на соответствие формату.
        if (_obj.DocumentRegister != null &&
            _obj.RegistrationDate.HasValue)
        {
          var leadDocNumber = _obj.LeadingDocument == null ? string.Empty : _obj.LeadingDocument.RegistrationNumber;
          
          // Для некоторых документов отключена проверка номера.
          var numberValidationDisabled = Functions.OfficialDocument.IsNumberValidationDisabled(_obj);
          
          // Если рег. номера нет, то нет смысла выключать проверку номера, т.к. он будет сгенерен автоматически.
          if (string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
            numberValidationDisabled = false;
          
          var numberSectionsError = Functions.DocumentRegister.CheckDocumentRegisterSections(_obj.DocumentRegister, _obj);
          var hasSectionsError = !string.IsNullOrWhiteSpace(numberSectionsError);
          var numberFormatError = hasSectionsError
            ? string.Empty
            : Functions.DocumentRegister.CheckRegistrationNumberFormat(_obj.DocumentRegister, _obj.RegistrationDate.Value,
                                                                       _obj.RegistrationNumber, depCode, bunitCode, caseIndex, kindCode, counterpartyCode, leadDocNumber,
                                                                       correctingPostfixInNumberIsAvailable);
          var hasNumberFormatError = !string.IsNullOrWhiteSpace(numberFormatError) &&
            !(isAutoNumbering && string.IsNullOrWhiteSpace(_obj.RegistrationNumber));
          
          var numberIsValid = numberValidationDisabled || !hasSectionsError && !hasNumberFormatError;
          if (!numberIsValid)
          {
            bool needValidate;
            e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, out needValidate);
            if (_obj.VerificationState == VerificationState.InProcess && documentKind != null && documentKind.AutoNumbering == true)
              needValidate = false;
            
            if (hasSectionsError && needValidate)
            {
              if (numberSectionsError == string.Format(Docflow.Resources.FillCaseFile, Docflow.Resources.numberWord))
                e.AddError(_obj.Info.Properties.CaseFile, numberSectionsError);
              else
                e.AddError(numberSectionsError);
            }
            
            if (hasNumberFormatError && needValidate)
              e.AddError(numberFormatError);
          }
          else if (!string.IsNullOrEmpty(_obj.RegistrationNumber) && (_obj.State.Properties.RegistrationNumber.IsChanged || _obj.Index == 0))
          {
            _obj.Index = Functions.DocumentRegister.GetIndexFromRegistrationNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, _obj.RegistrationNumber,
                                                                                   depCode, bunitCode, caseIndex, kindCode, counterpartyCode, leadDocNumber,
                                                                                   correctingPostfixInNumberIsAvailable);
          }
        }

        // Очистить индекс, если рег.номер пуст.
        if (string.IsNullOrEmpty(_obj.RegistrationNumber))
          _obj.Index = 0;

        // Проверить номер на уникальность.
        if (!string.IsNullOrEmpty(_obj.RegistrationNumber) &&
            _obj.RegistrationDate != null &&
            _obj.DocumentRegister != null &&
            (_obj.RegistrationNumber != _obj.State.Properties.RegistrationNumber.OriginalValue ||
             _obj.RegistrationDate != _obj.State.Properties.RegistrationDate.OriginalValue ||
             _obj.DocumentRegister != _obj.State.Properties.DocumentRegister.OriginalValue))
        {
          var leadingDocumentId = Functions.OfficialDocument.GetLeadDocumentId(_obj);
          if (!Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, _obj.Index ?? 0,
                                                                     _obj.RegistrationDate.Value, depCode, bunitCode,
                                                                     caseIndex, kindCode, counterpartyCode, leadingDocumentId))
            e.AddError(_obj.Info.Properties.RegistrationNumber,
                       isRegistrable ? Sungero.Docflow.Resources.RegistrationNumberIsNotUniqueFormat(_obj.RegistrationNumber) : Sungero.Docflow.Resources.RegistrationNumberIsNotUniqueForNumerable);
        }
      }
      
      // Получить и запомнить префикс и постфикс регистрационного номера.
      if (string.IsNullOrEmpty(_obj.RegistrationNumber) && _obj.RegistrationDate != null && _obj.DocumentRegister != null)
      {
        var leadingDocumentNumber = Functions.OfficialDocument.GetLeadDocumentNumber(_obj);
        var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfix(_obj.DocumentRegister, _obj.RegistrationDate.Value, leadingDocumentNumber,
                                                                                                     depCode, bunitCode, caseIndex, kindCode, counterpartyCode, false);
        // Проверить длину автогенерируемого номера.
        if (_obj.DocumentRegister.NumberOfDigitsInNumber + prefixAndPostfix.Prefix.Length + prefixAndPostfix.Postfix.Length > _obj.Info.Properties.RegistrationNumber.Length)
        {
          var errorMessage = string.Format(Docflow.Resources.PropertyLengthError, _obj.Info.Properties.RegistrationNumber.LocalizedName, _obj.Info.Properties.RegistrationNumber.Length);
          e.AddError(string.Format("{0} {1}", errorMessage, Parties.Resources.ContactAdministrator));
          return;
        }
        
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, prefixAndPostfix.Prefix);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, prefixAndPostfix.Postfix);
      }
      
      #endregion

      #region Дизейбл полей

      // Закрыть регистрационные данные от изменения.
      _obj.State.Properties.RegistrationNumber.IsEnabled = false;
      _obj.State.Properties.RegistrationDate.IsEnabled = false;
      _obj.State.Properties.DocumentRegister.IsEnabled = false;
      _obj.State.Properties.DocumentKind.IsEnabled = false;

      #endregion

      #region Выдача прав

      // Вывести предупреждение о том, что права для группы регистрации будут восстановлены, если они были удалены.
      if (_obj.DocumentRegister != null && _obj.AccessRights.StrictMode != AccessRightsStrictMode.Enhanced)
      {
        var registrationGroup = _obj.DocumentRegister.RegistrationGroup;
        
        // Выдать права группе регистрации.
        if (registrationGroup != null)
          _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }
      
      // Выдать права на документ при изменении проекта.
      if (_obj.Project != _obj.State.Properties.Project.OriginalValue && _obj.Project != null)
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToProjectDocument, true);
      
      #endregion
      
      #region Проверка неизменности данных

      var registrationRight = _obj.AccessRights.CanRegister();
      var canSend = _obj.AccessRights.CanSendByExchange();
      
      // Проверить неизменность регистрационных данных на сервере при отсутствии прав на изменение этих свойств.
      var canRegister = (isRegistrable && registrationRight) || !isRegistrable;
      var canReserve = isRegistrable && (registrationRight ||
                                         Docflow.PublicFunctions.RegistrationSetting.GetSettingByDocument(_obj, Docflow.RegistrationSetting.SettingType.Reservation) != null);
      
      // Игнорировать проверку изменения рег. данных при смене типа документа.
      if (!canRegister && !canReserve && !isDocumentTypeChange && !isOnVerification)
      {
        // Проверить неизменность даты регистрации.
        if ((_obj.RegistrationDate.HasValue &&
             _obj.RegistrationDate.Value != _obj.State.Properties.RegistrationDate.OriginalValue) ||
            (!_obj.RegistrationDate.HasValue &&
             _obj.State.Properties.RegistrationDate.OriginalValue.HasValue))
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationDate);

        // Проверить неизменность регистрационного номера.
        if (_obj.RegistrationNumber != _obj.State.Properties.RegistrationNumber.OriginalValue &&
            !(string.IsNullOrWhiteSpace(_obj.RegistrationNumber) &&
              string.IsNullOrWhiteSpace(_obj.State.Properties.RegistrationNumber.OriginalValue)))
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationNumber);

        // Проверить неизменность зарегистрированности.
        var registrationStateOriginalValue = _obj.State.Properties.RegistrationState.OriginalValue;
        if (_obj.RegistrationState != registrationStateOriginalValue &&
            !(registrationStateOriginalValue == null && isNotRegistered))
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyRegistrationInformation);

        // Проверить неизменность журнала регистрации.
        if (!Equals(_obj.DocumentRegister, _obj.State.Properties.DocumentRegister.OriginalValue))
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyDocumentRegister);

        // Проверить неизменность данных: "Дело" и "Направлено в дело" - на сервере при отсутствии прав на регистрацию.
        if (_obj.PlacedToCaseFileDate.HasValue &&
            _obj.PlacedToCaseFileDate.Value != _obj.State.Properties.PlacedToCaseFileDate.OriginalValue &&
            isRegistrable)
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyFAddedToFileListDate);

        // Проверить неизменность дела.
        if (!Equals(_obj.CaseFile, _obj.State.Properties.CaseFile.OriginalValue) &&
            isRegistrable)
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyFileList);

        // Проверить неизменность данных по местонахождению документа.
        if (_obj.State.Properties.Tracking.IsChanged && (!canRegister && isRegistrable && !canSend))
          e.AddError(Sungero.Docflow.Resources.NoRightsToModifyTracking);
      }

      #endregion

      #region Проверка корректности заполненности местонахождения

      // Проверить, что для возвращенных документов указан результат возврата.
      var completeTracking = _obj.Tracking
        .Where(l => l.ReturnDate.HasValue && l.DeliveredTo != null)
        .Where(l => l.ReturnResult == null);
      if (completeTracking.Any())
        e.AddError(Docflow.Resources.ToReturnDocumentYouMustSpecifiedReturnResult);
      
      #endregion

      #region Заполнение свойства "Местонахождение" на карточке

      var trackingState = Functions.OfficialDocument.GetLocationState(_obj);
      if (!string.IsNullOrEmpty(trackingState) || _obj.ExchangeState == null)
        _obj.LocationState = trackingState;
      
      #endregion

      #region Заполнение полей для вычислимых списков выданных и отправленных контрагенту

      var folderTracking = _obj.Tracking
        .Where(l => l.DeliveredTo != null && (!l.ReturnDate.HasValue || Equals(l.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.AtControl)))
        .OrderByDescending(l => l.DeliveryDate);

      #region Для отправки контрагенту

      var issueToContractor = folderTracking
        .Where(l => l.ReturnDeadline.HasValue && l.Action == OfficialDocumentTracking.Action.Endorsement)
        .OrderBy(l => l.ReturnDeadline).ThenBy(l => !(l.IsOriginal ?? false)).FirstOrDefault();

      _obj.ResponsibleForReturnEmployee = issueToContractor != null ? issueToContractor.DeliveredTo : null;
      _obj.IsHeldByCounterParty = issueToContractor != null;
      _obj.ScheduledReturnDateFromCounterparty = issueToContractor != null ? issueToContractor.ReturnDeadline : null;

      #endregion

      #region Для выдачи сотруднику
      
      var issueToEmployee = folderTracking
        .Where(l => l.Action != OfficialDocumentTracking.Action.Endorsement && !l.ReturnDate.HasValue)
        .OrderBy(l => !l.ReturnDeadline.HasValue)
        .ThenByDescending(l => l.DeliveryDate)
        .ThenBy(l => !(l.IsOriginal ?? false))
        .FirstOrDefault();
      
      if (issueToEmployee != null)
      {
        _obj.IsReturnRequired = issueToEmployee.ReturnDeadline.HasValue;
        _obj.ReturnDeadline = issueToEmployee.ReturnDeadline;
        _obj.ReturnDate = null;
        _obj.DeliveredTo = issueToEmployee.DeliveredTo;
      }
      else
      {
        _obj.IsReturnRequired = false;
        _obj.ReturnDeadline = null;
        _obj.ReturnDate = _obj.Tracking.Where(l => l.ReturnDate.HasValue)
          .OrderByDescending(l => l.ReturnDate)
          .Select(l => l.ReturnDate).FirstOrDefault();
        _obj.DeliveredTo = null;
      }

      #endregion

      // Проверка срока возврата.
      if (_obj.Tracking.Any(l => l.DeliveryDate > l.ReturnDeadline))
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndScheduledDate);

      #endregion

      #region Валидация возврата от контрагента

      var activeCounterpartyTask = _obj.State.Properties.Tracking.Changed
        .Where(l => l.ReturnResult != null && l.Action != Docflow.OfficialDocumentTracking.Action.Delivery && l.ReturnTask != null)
        .Select(l => l.ReturnTask.Id).Distinct();
      foreach (var task in activeCounterpartyTask)
      {
        var counterpartyTrackings = _obj.State.Properties.Tracking.Changed
          .Where(l => l.ReturnTask != null && l.ReturnTask.Id == task && l.ReturnResult != null);
        var defaultResult = counterpartyTrackings.FirstOrDefault().ReturnResult;
        foreach (var counterpartyTracking in counterpartyTrackings)
          if (counterpartyTracking.ReturnResult != defaultResult)
            e.AddError(Docflow.Resources.DifferentReturnResultForTask);
      }

      #endregion

      #region Заполнить имя

      Functions.OfficialDocument.FillName(_obj);

      #endregion

      #region Очистка НОР для ненумеруемых документов
      
      Functions.OfficialDocument.ClearBusinessUnit(_obj, _obj.DocumentKind);
      
      #endregion
      
      #region Работа с задачами контроля возврата

      // Прекратить задачи по удаленным из "Выдачи" строкам.
      var result = string.Empty;
      foreach (var documentTracking in Functions.OfficialDocument.GetDeletedTrackingRecords(_obj))
      {
        result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.AbortTask, CheckReturnTasks.Resources.TrackingHasBeenDeleted);
        if (!string.IsNullOrWhiteSpace(result))
        {
          e.AddError(result);
          return;
        }
        documentTracking.ReturnTask = null;
      }
      
      // Выполнить задания по тем измененным строкам, у которых указана дата возврата.
      var alreadyCompletedTask = new List<int>();
      foreach (var documentTracking in Functions.OfficialDocument.GetChangedTrackingRecordsWithTasksInProcess(_obj))
      {
        if (!alreadyCompletedTask.Contains(documentTracking.ReturnTask.Id))
        {
          var isReturnControlTask = documentTracking.ReturnTask.Info.Name == CheckReturnTasks.Info.Name;
          if (isReturnControlTask)
          {
            result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.CompleteAssignment);
          }
          else
          {
            var operation = documentTracking.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.Signed ?
              Constants.Module.ReturnControl.SignAssignment : Constants.Module.ReturnControl.NotSignAssignment;
            var hasNotReturnedDocument = _obj.Tracking
              .Where(l => l.ReturnTask != null && l.ReturnTask.Id == documentTracking.ReturnTask.Id)
              .Any(l => l.ReturnResult == null);
            if (!hasNotReturnedDocument)
              result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, operation);
          }
          if (!string.IsNullOrWhiteSpace(result))
          {
            e.AddError(result);
            return;
          }
        }

        if (documentTracking.Note == ApprovalTasks.Resources.CommentOnEndorsement)
          documentTracking.Note = null;

        alreadyCompletedTask.Add(documentTracking.ReturnTask.Id);
      }

      // Синхронизировать сроки заданий между "Выдачей" и задачами на контроль возврата.
      bool isDeadlineExtensionTaskCallContext;
      isDeadlineExtensionTaskCallContext = e.Params.TryGetValue(Docflow.Constants.Module.DeadlineExtentsionTaskCallContext, out isDeadlineExtensionTaskCallContext) ?
        isDeadlineExtensionTaskCallContext : false;
      if (!isDeadlineExtensionTaskCallContext)
      {
        foreach (var documentTracking in Functions.OfficialDocument.GetTrackingRecordsWithDeadlineChanged(_obj))
        {
          result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.DeadlineChange, null, documentTracking.ReturnDeadline);
          if (!string.IsNullOrWhiteSpace(result))
          {
            e.AddError(result);
            return;
          }
        }
      }
      // Для всех измененных исполнителей прекращать старую и стартовать новую задачу.
      foreach (var documentTracking in Functions.OfficialDocument.GetTrackingRecordsWithEmployeeChanged(_obj))
      {
        result = Functions.Module.CompleteCheckReturnTask(documentTracking.ReturnTask.Id, Constants.Module.ReturnControl.AbortTask, CheckReturnTasks.Resources.TrackingHasBeenDeleted);
        if (!string.IsNullOrWhiteSpace(result))
        {
          e.AddError(result);
          return;
        }

        var task = CheckReturnTasks.Create();
        task.Assignee = documentTracking.DeliveredTo;
        task.MaxDeadline = documentTracking.ReturnDeadline;
        task.DocumentGroup.OfficialDocuments.Add(_obj);
        task.Start();
        documentTracking.ReturnTask = task;
      }

      // Отправить новые задачи.
      foreach (var documentTracking in _obj.Tracking.Where(l => l.ReturnDeadline.HasValue && !l.ReturnDate.HasValue && l.ReturnTask == null && l.ExternalLinkId == null))
      {
        var task = CheckReturnTasks.Create();
        task.Assignee = documentTracking.DeliveredTo;
        task.MaxDeadline = documentTracking.ReturnDeadline;
        task.DocumentGroup.OfficialDocuments.Add(_obj);
        task.Start();
        documentTracking.ReturnTask = task;
      }

      #endregion
      
      #region Проверить право подписи у подписанта с нашей стороны при верификации
      
      if (isOnVerification)
      {
        // Проводить проверку только в UI, если это необходимо для документа и статус верификации "В процессе" или его значение изменено.
        var needValidateOurSignatory = Functions.OfficialDocument.NeedValidateOurSignatorySignatureSetting(_obj);
        var isVisualMode = ((Domain.Shared.IExtendedEntity)_obj).Params.ContainsKey(Constants.OfficialDocument.IsVisualModeParamName);
        
        if (needValidateOurSignatory && isVisualMode && _obj.OurSignatory != null
            && !Functions.OfficialDocument.CanSignByEmployee(_obj, _obj.OurSignatory))
        {
          var message = Sungero.Docflow.OfficialDocuments.Resources.IncorrectOurSignatoryFormat(_obj.OurSignatory.Name);
          e.AddError(_obj.Info.Properties.OurSignatory, message);
        }
      }
      
      #endregion
      
      _obj.DocumentDate = _obj.RegistrationDate.HasValue ? _obj.RegistrationDate : _obj.Created;
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      // Заполнить регистрационный номер.
      if (_obj.RegistrationDate != null && _obj.DocumentRegister != null)
      {
        // Получить код подразделения.
        var departmentCode = string.Empty;
        var departmentId = 0;
        if (_obj.Department != null)
        {
          departmentId = _obj.Department.Id;
          departmentCode = _obj.Department.Code;
        }
        
        // Получить ID и код НОР.
        var businessUnitCode = string.Empty;
        var businessUnitId = 0;
        if (_obj.BusinessUnit != null)
        {
          businessUnitId = _obj.BusinessUnit.Id;
          businessUnitCode = _obj.BusinessUnit.Code;
        }
        
        var leadDocumentId = Functions.OfficialDocument.GetLeadDocumentId(_obj);
        
        if (string.IsNullOrEmpty(_obj.RegistrationNumber))
        {
          var registrationIndex = 0;
          var caseFileIndex = _obj.CaseFile != null ? _obj.CaseFile.Index : string.Empty;
          var docKindCode = _obj.DocumentKind != null ? _obj.DocumentKind.Code : string.Empty;
          var counterpartyCode = Functions.OfficialDocument.GetCounterpartyCode(_obj);
          do
          {
            // Для доп.соглашений и актов номер устанавливать в разрезе ведущего документа.
            registrationIndex = Functions.DocumentRegister.GetNextRegistrationNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, leadDocumentId, departmentId, businessUnitId);
            var registrationIndexWithLeadZero = registrationIndex.ToString();
            if (registrationIndexWithLeadZero.Length < _obj.DocumentRegister.NumberOfDigitsInNumber)
              registrationIndexWithLeadZero = string.Concat(Enumerable.Repeat("0", (_obj.DocumentRegister.NumberOfDigitsInNumber - registrationIndexWithLeadZero.Length) ?? 0)) +
                registrationIndexWithLeadZero;

            string registrationNumberPrefixValue;
            e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, out registrationNumberPrefixValue);
            string registrationNumberPostfixValue;
            e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, out registrationNumberPostfixValue);
            _obj.RegistrationNumber = registrationNumberPrefixValue + registrationIndexWithLeadZero +
              registrationNumberPostfixValue;
          } while (!Functions.DocumentRegister.IsRegistrationNumberUnique(_obj.DocumentRegister, _obj, _obj.RegistrationNumber, registrationIndex,
                                                                          _obj.RegistrationDate.Value, departmentCode, businessUnitCode,
                                                                          caseFileIndex, docKindCode, counterpartyCode, leadDocumentId));

          _obj.Index = registrationIndex;
        }
        else if (!string.IsNullOrEmpty(_obj.RegistrationNumber) && _obj.Index.HasValue && _obj.Index.Value > 0 &&
                 _obj.RegistrationNumber != _obj.State.Properties.RegistrationNumber.OriginalValue)
        {
          var currentCode = Functions.DocumentRegister.GetCurrentNumber(_obj.DocumentRegister, _obj.RegistrationDate.Value, leadDocumentId, departmentId, businessUnitId);
          if (_obj.Index == (currentCode + 1))
          {
            Functions.DocumentRegister.SetCurrentNumber(_obj.DocumentRegister, _obj.Index.Value, leadDocumentId, departmentId, businessUnitId, _obj.RegistrationDate.Value);
          }
        }
        
      }
      
      if (e.Params.Contains(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister))
        e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister);
    }
  }
  
}
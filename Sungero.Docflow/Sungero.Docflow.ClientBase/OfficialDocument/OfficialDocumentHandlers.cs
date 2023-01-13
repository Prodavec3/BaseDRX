using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.Docflow
{
  partial class OfficialDocumentTrackingClientHandlers
  {

    public virtual IEnumerable<Enumeration> TrackingReturnResultFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.Action == OfficialDocumentTracking.Action.Endorsement)
      {
        query = query.Where(r => Equals(r, OfficialDocumentTracking.ReturnResult.Signed) ||
                            Equals(r, OfficialDocumentTracking.ReturnResult.NotSigned));
      }
      else
      {
        query = query.Where(r => Equals(r, OfficialDocumentTracking.ReturnResult.AtControl) ||
                            Equals(r, OfficialDocumentTracking.ReturnResult.Returned));
      }
      return query;
    }

    public virtual IEnumerable<Enumeration> TrackingActionFiltering(IEnumerable<Enumeration> query)
    {
      query = query.Where(ta => Equals(ta, OfficialDocumentTracking.Action.Delivery));
      
      return query;
    }

    public virtual void TrackingReturnDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (_obj.DeliveryDate > e.NewValue)
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndScheduledDate);

      if (_obj.Action == Docflow.OfficialDocumentTracking.Action.Endorsement)
      {
        e.AddError(Docflow.Resources.CantChangeReturnRowForEndorsementTask);
        return;
      }

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
    }

    public virtual void TrackingReturnResultValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue &&
          _obj.State.Properties.ReturnResult.OriginalValue != null &&
          !Equals(_obj.State.Properties.ReturnResult.OriginalValue, Docflow.OfficialDocumentTracking.ReturnResult.AtControl))
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
      
      // Запретить изменение, если документ был отправлен через сервис обмена.
      if (_obj.ExternalLinkId != null)
        e.AddError(OfficialDocuments.Resources.CannotChangeTrackingSentByExchange);
    }

    public virtual void TrackingReturnDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (_obj.DeliveryDate > e.NewValue)
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndReturnDate);

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
      
      // Запретить изменение, если документ был отправлен через сервис обмена.
      if (_obj.ExternalLinkId != null)
        e.AddError(OfficialDocuments.Resources.CannotChangeTrackingSentByExchange);
    }

    public virtual void TrackingIsOriginalValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
    }

    public virtual void TrackingDeliveredToValueInput(Sungero.Docflow.Client.OfficialDocumentTrackingDeliveredToValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (_obj.Action == Docflow.OfficialDocumentTracking.Action.Endorsement)
      {
        e.AddError(Docflow.Resources.CantChangeReturnRowForEndorsementTask);
        return;
      }

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
    }

    public virtual void TrackingDeliveryDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var deliveryDate = e.NewValue;
      if (Equals(deliveryDate, e.OldValue))
        return;

      if (_obj.ReturnDeadline < deliveryDate)
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndScheduledDate);
      if (deliveryDate > _obj.ReturnDate)
        e.AddError(Docflow.Resources.ReturnDocumentDeliveryAndReturnDate);

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
    }

    public virtual void TrackingNoteValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      // Запретить изменение, если документ уже возвращен.
      if (_obj.State.Properties.ReturnDate.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordDocumentReturnIsInadmissible);
      
      // Запретить изменение, если документ был отправлен через сервис обмена.
      if (_obj.ExternalLinkId != null)
        e.AddError(OfficialDocuments.Resources.CannotChangeTrackingSentByExchange);
    }

    public virtual void TrackingActionValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      // Запретить изменение действия, если документ уже сохранен.
      if (_obj.State.Properties.Action.OriginalValue.HasValue)
        e.AddError(Docflow.Resources.ChangingRecordActionTypeReturnedDocumentIsInadmissible);
    }
  }

  partial class OfficialDocumentClientHandlers
  {

    public virtual void PlacedToCaseFileDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
        e.AddError(_obj.Info.Properties.PlacedToCaseFileDate, Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate);
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      ((Domain.Shared.IExtendedEntity)_obj).Params.Remove(Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName);
    }

    public virtual void VerificationStateValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      if (e.NewValue == Docflow.OfficialDocument.VerificationState.Completed)
      {
        var args = new Sungero.Domain.Client.ExecuteActionArgs(Sungero.Domain.Shared.FormType.Card, _obj);
        if (!args.Validate())
          // Bug 96585. Прервать смену значения в контроле невозможно.
          e.NewValue = e.OldValue;
      }
    }

    public virtual void ProjectValueInput(Sungero.Docflow.Client.OfficialDocumentProjectValueInputEventArgs e)
    {
      // Отобразить однократно нотифайку о выдаче прав на проектные документы.
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && Projects.Projects.Is(e.NewValue))
        Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Projects.Resources.ProjectDocumentRightsNotifyMessage);
    }

    public virtual void DepartmentValueInput(Sungero.Docflow.Client.OfficialDocumentDepartmentValueInputEventArgs e)
    {
      if (_obj.AccessRights.CanUpdate() && !Functions.Module.IsLockedByOther(_obj))
      {
        var hasReservationSetting = PublicFunctions.Module.Remote.GetRegistrationSettings(Docflow.RegistrationSetting.SettingType.Reservation, _obj.BusinessUnit, _obj.DocumentKind, e.NewValue).Any();
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, hasReservationSetting);
      }
    }

    public virtual void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      if (_obj.AccessRights.CanUpdate() && !Functions.Module.IsLockedByOther(_obj))
      {
        var hasReservationSetting = PublicFunctions.Module.Remote.GetRegistrationSettings(Docflow.RegistrationSetting.SettingType.Reservation, e.NewValue, _obj.DocumentKind, _obj.Department).Any();
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, hasReservationSetting);
      }
    }

    public virtual void LifeCycleStateValueInput(Sungero.Presentation.EnumerationValueInputEventArgs e)
    {
      // Проверка доступности нумерации.
      if (_obj.DocumentKind != null &&
          _obj.DocumentKind.AutoNumbering == true &&
          _obj.RegistrationState == RegistrationState.NotRegistered &&
          !Functions.OfficialDocument.IsObsolete(_obj, e.NewValue))
      {
        var hasNumerationSetting = Functions.OfficialDocument.HasDocumentRegistersByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration); 
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting, hasNumerationSetting);
        if (!hasNumerationSetting)
          e.AddWarning(_obj.Info.Properties.LifeCycleState, Sungero.Docflow.Resources.NumberingSettingsRequiredForSave, _obj.Info.Properties.DocumentKind);
        else
          e.AddInformation(_obj.Info.Properties.LifeCycleState, Sungero.Docflow.Resources.DocumentNumberAutomaticallyGeneratedOnSave, _obj.Info.Properties.DocumentKind);
      }
    }

    public virtual void RegistrationDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
      {
        e.AddError(_obj.Info.Properties.RegistrationDate, Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate);
        return;
      }
      
      if (e.NewValue != _obj.State.Properties.RegistrationDate.OriginalValue)
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
    }

    public virtual void RegistrationNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != _obj.State.Properties.RegistrationNumber.OriginalValue)
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      PublicFunctions.OfficialDocument.CreateParamsCache(_obj);
      PublicFunctions.OfficialDocument.SwitchVerificationMode(_obj);
    }

    public virtual void SubjectValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
    }

    public override void ShowingSignDialog(Sungero.Domain.Client.ShowingSignDialogEventArgs e)
    {
      var errors = Functions.OfficialDocument.Remote.GetApprovalValidationErrors(_obj, true);
      foreach (var error in errors)
        e.Hint.Add(error);
      e.CanApprove = !errors.Any();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Чтобы в зависимости от режима изменять обязательность для возможности сохранять документ с незаполненными полями,
      // используется этот параметр. Добавляется на Refresh до отрабатывания базового события,
      // чтобы выполнились вычисления обязательности свойств, т.к. при отмене изменений параметры откатываются.
      ((Domain.Shared.IExtendedEntity)_obj).Params[Sungero.Docflow.PublicConstants.OfficialDocument.IsVisualModeParamName] = true;
      
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
      Functions.OfficialDocument.SetRequiredProperties(_obj);
      
      bool numberRequiredValue;
      var numberRequired = (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.NumberRequired, out numberRequiredValue) && numberRequiredValue) || !e.Params.Contains(Sungero.Docflow.Constants.OfficialDocument.NumberRequired);
      _obj.State.Properties.RegistrationNumber.IsRequired = _obj.RegistrationState != RegistrationState.NotRegistered && numberRequired;
      
      // Проверка доступности нумерации.
      if (_obj.DocumentKind != null &&
          _obj.DocumentKind.AutoNumbering == true &&
          _obj.RegistrationState == RegistrationState.NotRegistered &&
          !Functions.OfficialDocument.IsObsolete(_obj, _obj.LifeCycleState))
      {
        bool hasNumerationSetting;
        if (!e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting, out hasNumerationSetting))
        {
          hasNumerationSetting = Functions.OfficialDocument.HasDocumentRegistersByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
          e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting, hasNumerationSetting);
        }
        
        if (!hasNumerationSetting)
          e.AddWarning(_obj.Info.Properties.DocumentKind, Sungero.Docflow.Resources.NumberingSettingsRequiredForSave, _obj.Info.Properties.LifeCycleState);
        else if (_obj.VerificationState != VerificationState.InProcess)
          e.AddInformation(_obj.Info.Properties.DocumentKind, Sungero.Docflow.Resources.DocumentNumberAutomaticallyGeneratedOnSave, _obj.Info.Properties.LifeCycleState);
      }
      
      // Добавить хинт о том, как зарегистрировать зарезервированный документ.
      if (_obj.RegistrationState == RegistrationState.Reserved)
      {
        // Отображение только для делопроизводителя, который может регистрировать данный документ.
        var documentKind = _obj.DocumentKind;
        if (documentKind != null)
        {
          var accessRights = _obj.AccessRights;
          var isNotifiable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
          
          if (accessRights.CanUpdate() &&
              _obj.RegistrationState != RegistrationState.Registered &&
              (isNotifiable && accessRights.CanRegister()) &&
              !Functions.Module.IsLockedByOther(_obj))
          {
            e.AddInformation(Docflow.Resources.DocumentIsReservedToRegisterClickRegisterFormat(_obj.RegistrationNumber), _obj.Info.Actions.Register);
          }
        }
      }
      
      // Для документа на верификации показать хинт, если нужно зарегистрировать вручную.
      var regNumber = _obj.RegistrationNumber != null ? _obj.RegistrationNumber.Trim() : _obj.RegistrationNumber;
      if (_obj.VerificationState == VerificationState.InProcess &&
          _obj.DocumentKind != null &&
          _obj.DocumentKind.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable &&
          _obj.RegistrationState == RegistrationState.NotRegistered &&
          (!string.IsNullOrEmpty(regNumber) ||
           _obj.RegistrationDate.HasValue))
      {
        e.AddInformation(OfficialDocuments.Resources.CheckRegDataAndFinishRegistration);
      }
      
      // Изменить заголовок окна карточки в соответствии с именем документа.
      e.Title = _obj.Name == Docflow.Resources.DocumentNameAutotext ? null : _obj.Name;
      
      #region Очистка НОР для ненумеруемых документов
      
      if (_obj.State.IsInserted)
        Functions.OfficialDocument.ClearBusinessUnit(_obj, _obj.DocumentKind);
      
      #endregion
      
      // Отобразить однократно нотифайку о выдаче прав на проектные документы.
      if (_obj.State.IsInserted && _obj.Project != null && Projects.Projects.Is(_obj.Project))
        Projects.PublicFunctions.Module.ShowProjectRightsNotifyOnce(e, Projects.Projects.Resources.ProjectDocumentRightsNotifyMessage);
      
      PublicFunctions.OfficialDocument.SwitchVerificationMode(_obj);
      
      // Поле Основание для нашей стороны недоступно, если не указан подписывающий.
      _obj.State.Properties.OurSigningReason.IsEnabled = _obj.OurSignatory != null;
    }

    public virtual void DocumentRegisterValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentRegisterValueInputEventArgs e)
    {
      // Добавить предупреждение, если изменился журнал при перерегистрации.
      if (e.NewValue == null || (!Equals(_obj.State.Properties.DocumentRegister.OriginalValue, e.NewValue) && _obj.RegistrationState == RegistrationState.Registered))
      {
        e.AddWarning(Docflow.Resources.DocumentRegisterWasChanged);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
      }
    }

    public virtual void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      if (e.NewValue != e.OldValue && _obj.RegistrationState == RegistrationState.Registered)
        e.AddWarning(Docflow.Resources.DocumentKindWasChanged);

      // Проверка доступности нумерации.
      if (e.NewValue != null &&
          e.NewValue.AutoNumbering == true &&
          !Functions.OfficialDocument.IsObsolete(_obj, _obj.LifeCycleState))
      {
        var setting = Docflow.PublicFunctions.RegistrationSetting.GetSettingForKind(_obj, Docflow.RegistrationSetting.SettingType.Numeration, e.NewValue);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting, setting != null);
        if (setting == null)
          e.AddWarning(_obj.Info.Properties.DocumentKind, Sungero.Docflow.Resources.NumberingSettingsRequiredForSave, _obj.Info.Properties.LifeCycleState);
        else
          e.AddInformation(_obj.Info.Properties.DocumentKind, Sungero.Docflow.Resources.DocumentNumberAutomaticallyGeneratedOnSave, _obj.Info.Properties.LifeCycleState);
      }
      
      if (_obj.AccessRights.CanUpdate() && !Functions.Module.IsLockedByOther(_obj))
      {
        var hasReservationSetting = PublicFunctions.Module.Remote.GetRegistrationSettings(Docflow.RegistrationSetting.SettingType.Reservation, _obj.BusinessUnit, e.NewValue, _obj.Department).Any();
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, hasReservationSetting);
      }
    }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.Docflow
{

  partial class OfficialDocumentVersionsSharedCollectionHandlers
  {

    public override void VersionsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      // Если удалили последнюю версию.
      if (_obj.LastVersion != null && _deleted.Number > _obj.LastVersion.Number)
      {
        var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(_obj, _obj.LastVersion.Id);
        if (info != null)
          _obj.ExchangeState = info.ExchangeState;
      }
    }

    public override void VersionsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      // Формирование имени документа при добавлении версии.
      if (_obj.State.IsInserted && !_obj.State.IsCopied &&
          _obj.DocumentKind != null && _obj.DocumentKind.GenerateDocumentName == true &&
          !_obj.Name.Equals(Resources.DocumentNameAutotext))
      {
        // Если версия документа создается из шаблона, то не заполнять Содержание.
        if (string.IsNullOrEmpty(_obj.Subject) && !e.Params.Contains(Constants.Module.CreateFromTemplate))
          _obj.Subject = _obj.Name;
        else
          Functions.OfficialDocument.FillName(_obj);
      }
      
      // Сбрасываем статус согл. с КА при создании новой версии документа из МКДО.
      if (_obj.ExternalApprovalState != null && _obj.ExchangeState != null)
        _obj.ExternalApprovalState = null;
      
      // Сбрасываем статус эл. обмена при создании новой версии.
      if (_obj.ExchangeState != null)
      {
        // Версии можно создавать и при заблокированной карточке, а в таком случае сохранить эту карточку нельзя.
        var lockInfo = Locks.GetLockInfo(_obj);
        if (lockInfo != null && lockInfo.IsLockedByOther)
          throw AppliedCodeException.Create(lockInfo.LockedMessage);
        
        _obj.ExchangeState = null;
      }
      
      var storage = _obj.Storage ?? Functions.Module.GetStorageByPolicies(_obj);
      if (storage != null)
      {
        if (!Equals(_added.Body.Storage, storage))
          _added.Body.SetStorage(storage);

        if (!Equals(_added.PublicBody.Storage, storage))
          _added.PublicBody.SetStorage(storage);
      }
    }
  }

  partial class OfficialDocumentTrackingSharedCollectionHandlers
  {

    public virtual void TrackingDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_deleted.State.Properties.ReturnDate.OriginalValue.HasValue)
        throw AppliedCodeException.Create(Docflow.Resources.CantDeleteReturnedTracking);

      if (_deleted.Action == Docflow.OfficialDocumentTracking.Action.Endorsement)
        throw AppliedCodeException.Create(Docflow.Resources.CantDeleteAutomaticallyTracking);
    }

    public virtual void TrackingAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.IsOriginal = !_obj.Tracking.Any(l => (l.IsOriginal ?? false) && l.ReturnDate == null);
      _added.DeliveryDate = Calendar.UserToday;
      _added.Action = Docflow.OfficialDocumentTracking.Action.Delivery;
      _added.ReturnTask = null;
      _added.ReturnResult = null;
      _added.ReturnDate = null;
    }
  }

  partial class OfficialDocumentTrackingSharedHandlers
  {

    public virtual void TrackingDeliveredToChanged(Sungero.Docflow.Shared.OfficialDocumentTrackingDeliveredToChangedEventArgs e)
    {

    }

    public virtual void TrackingReturnTaskChanged(Sungero.Docflow.Shared.OfficialDocumentTrackingReturnTaskChangedEventArgs e)
    {
      var approvalTask = ApprovalTasks.As(e.NewValue);
      _obj.Iteration = approvalTask != null ? approvalTask.Iteration : null;
    }
    
    public virtual void TrackingReturnResultChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;

      if (!_obj.ReturnDate.HasValue)
        _obj.ReturnDate = Calendar.UserToday;
    }

    public virtual void TrackingActionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      // Установить срок возврата: 10 раб.дней для внутренней выдачи и 20 раб.дней для Отправки КА.
      if (!_obj.State.Properties.ReturnDeadline.OriginalValue.HasValue)
      {
        var user = _obj.DeliveredTo ?? Users.Current;
        var issuingScheduledDate = Calendar.Today.AddWorkingDays(user, 10).Date;
        var sendingScheduledDate = Calendar.Today.AddWorkingDays(user, 20).Date;

        if (e.NewValue == Docflow.OfficialDocumentTracking.Action.Delivery &&
            (!_obj.ReturnDeadline.HasValue || (_obj.ReturnDeadline == sendingScheduledDate &&
                                               e.OldValue == Docflow.OfficialDocumentTracking.Action.Sending)))
          _obj.ReturnDeadline = issuingScheduledDate;
        
        if (e.NewValue == Docflow.OfficialDocumentTracking.Action.Sending &&
            (!_obj.ReturnDeadline.HasValue || (_obj.ReturnDeadline == issuingScheduledDate &&
                                               e.OldValue == Docflow.OfficialDocumentTracking.Action.Delivery)))
          _obj.ReturnDeadline = sendingScheduledDate;
      }
    }
  }

  partial class OfficialDocumentSharedHandlers
  {

    public virtual void OurSignatoryChanged(Sungero.Docflow.Shared.OfficialDocumentOurSignatoryChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
        _obj.OurSigningReason = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, _obj.OurSignatory);
      
      if (e.NewValue == null)
        _obj.OurSigningReason = null;
    }

    public virtual void DocumentGroupChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentGroupChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OriginalValue))
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }

    public virtual void LeadingDocumentChanged(Sungero.Docflow.Shared.OfficialDocumentLeadingDocumentChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OriginalValue))
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }

    public virtual void BusinessUnitChanged(Sungero.Docflow.Shared.OfficialDocumentBusinessUnitChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OriginalValue))
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }

    public virtual void VerificationStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == Docflow.OfficialDocument.VerificationState.Completed)
        ((Domain.Shared.IExtendedEntity)_obj).Params[Constants.OfficialDocument.NeedStoreVerifiedPropertiesValuesParamName] = true;
      else
        ((Domain.Shared.IExtendedEntity)_obj).Params.Remove(Constants.OfficialDocument.NeedStoreVerifiedPropertiesValuesParamName);
      
      // Вызвать переформирование имени документа, т.к. статус верификации влияет на формирование имени в случае,
      // когда для вида документа отключено формирование имени документа автоматически.
      this.FillName();
    }

    public virtual void CaseFileChanged(Sungero.Docflow.Shared.OfficialDocumentCaseFileChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.PlacedToCaseFileDate = null;
      }
      else if (e.NewValue != e.OldValue && _obj.PlacedToCaseFileDate == null)
      {
        _obj.PlacedToCaseFileDate = Calendar.UserNow;
      }
    }

    public virtual void DepartmentChanged(Sungero.Docflow.Shared.OfficialDocumentDepartmentChangedEventArgs e)
    {
      var department = e.NewValue;
      if (department != null && _obj.BusinessUnit == null && department.BusinessUnit != null)
        _obj.BusinessUnit = department.BusinessUnit;
      
      if (e.NewValue != null && !Equals(e.NewValue, e.OriginalValue))
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }
    
    protected void FillName()
    {
      Functions.OfficialDocument.FillName(_obj);
    }
    
    public virtual void LifeCycleStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {

    }

    public virtual void TrackingChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      
    }
    
    public virtual void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      this.FillName();
    }

    public virtual void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      this.FillName();
    }

    public virtual void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      this.FillName();
    }

    public override void NameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: пересмотреть код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      var nameLength = _obj.Info.Properties.Name.Length;
      if (e.NewValue != null && e.NewValue.Length > nameLength)
        _obj.Name = e.NewValue.Substring(0, nameLength);
    }

    public virtual void RegistrationStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.OfficialDocument.SetRequiredProperties(_obj);
      Functions.OfficialDocument.UpdateLifeCycle(_obj, e.NewValue, _obj.InternalApprovalState, _obj.ExternalApprovalState);
      
      // Обновить параметр доступности нумерации.
      if (_obj.DocumentKind != null &&
          _obj.DocumentKind.AutoNumbering == true &&
          _obj.RegistrationState == RegistrationState.NotRegistered &&
          !Functions.OfficialDocument.IsObsolete(_obj, e.NewValue))
      {
        var hasNumerationSetting = Functions.OfficialDocument.HasDocumentRegistersByDocument(_obj, Docflow.RegistrationSetting.SettingType.Numeration);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.HasNumerationSetting, hasNumerationSetting);
      }
    }

    public virtual void InternalApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.OfficialDocument.UpdateLifeCycle(_obj, _obj.RegistrationState, e.NewValue, _obj.ExternalApprovalState);
    }

    public virtual void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      var paramName = string.Format("doc{0}_ConvertingFrom", _obj.Id);
      var isTypeChanging = e.Params.Contains(paramName);
      if (isTypeChanging)
      {
        // Записать имя документа до смены типа в примечание, если:
        //   - имя уже заполнено;
        //   - новый вид документа с автоформированием имени;
        //   - предыдущий вид документа без автоформирования имени;
        //   - документ не находится на верификации.
        if (!string.IsNullOrWhiteSpace(_obj.Name) &&
            e.NewValue != null && e.NewValue.GenerateDocumentName == true &&
            (e.OriginalValue == null || e.OriginalValue.GenerateDocumentName != true) &&
            _obj.VerificationState != OfficialDocument.VerificationState.InProcess)
        {
          var nameBeforeChangingType = OfficialDocuments.Resources.DocumentNameBeforeChangingTypeNoteFormat(_obj.Name);
          if (string.IsNullOrWhiteSpace(_obj.Subject))
            _obj.Subject = _obj.Name;
          else
          {
            _obj.Note = string.IsNullOrWhiteSpace(_obj.Note) ?
              nameBeforeChangingType :
              string.Format("{0}{1}{1}{2}", nameBeforeChangingType, Environment.NewLine, _obj.Note);
          }
        }
        
        // Очистить имя документа, если:
        //   - новый вид документа без автоформирования имени;
        //   - предыдущий вид документа с автоформированием имени.
        if (e.NewValue != null && e.NewValue.GenerateDocumentName != true &&
            e.OriginalValue != null && e.OriginalValue.GenerateDocumentName == true)
        {
          _obj.Name = _obj.Subject;
          _obj.Subject = string.Empty;
        }
        
        // Сбросить жизненный цикл для действующего не нумеруемого документа.
        if (e.OriginalValue != null && e.OriginalValue.NumberingType == DocumentKind.NumberingType.NotNumerable &&
            _obj.LifeCycleState == OfficialDocument.LifeCycleState.Active)
          _obj.LifeCycleState = LifeCycleState.Draft;
      }
      
      // Если при смене типа документ находится на верификации, то номер и дату регистрации очищать не нужно.
      if (isTypeChanging && _obj.VerificationState != VerificationState.InProcess ||
          e.NewValue != null && e.NewValue.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable)
      {
        // Очистить свойства.
        _obj.RegistrationNumber = null;
        _obj.RegistrationDate = null;
      }
      
      if (isTypeChanging || (e.NewValue != null && e.NewValue.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable))
      {
        _obj.DocumentRegister = null;
        _obj.RegistrationState = RegistrationState.NotRegistered;
        
        // При смене типа документа сохранить значения полей Дело, Дата помещения в дело, Способ доставки.
        if (!isTypeChanging)
        {
          _obj.DeliveryMethod = null;
          _obj.CaseFile = null;
          _obj.PlacedToCaseFileDate = null;
        }
        
        // Отменить необходимость, т.к. рег.данные очищены.
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedRegistration, false);
      }
      
      // См.55345. Смена типа.
      // Obsolete остается Obsolete.
      // Для ненумеруемого вида устанавливается в Active.
      // Для регистрируемого или нумеруемого устанавливается Draft.
      // Ф-я SetLifeCycleState() может быть переопределена в наследниках.
      if (!isTypeChanging && _obj.State.IsInserted == true && e.NewValue != null && _obj.LifeCycleState != LifeCycleState.Obsolete)
        Functions.OfficialDocument.SetLifeCycleState(_obj);
      
      // Очистить поле Проект, если нужно.
      if (Functions.OfficialDocument.NeedClearProject(_obj, e))
        _obj.Project = null;
      
      if (e.NewValue != null && (e.OldValue == null || e.OldValue != null && e.NewValue.NumberingType != e.OldValue.NumberingType))
        e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.ShowParam);
      
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
      Functions.OfficialDocument.SetRequiredProperties(_obj);
      this.FillName();
      
      Functions.OfficialDocument.FillOrganizationStructure(_obj);
      Functions.OfficialDocument.ClearBusinessUnit(_obj, e.NewValue);
      
      e.Params.Remove(paramName);
      
      if (e.NewValue != null && !Equals(e.NewValue, e.OriginalValue))
        e.Params.AddOrUpdate(Constants.OfficialDocument.GrantAccessRightsToDocumentAsync, true);
    }

    public virtual void ExternalApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.OfficialDocument.UpdateLifeCycle(_obj, _obj.RegistrationState, _obj.InternalApprovalState, e.NewValue);
    }

  }
}
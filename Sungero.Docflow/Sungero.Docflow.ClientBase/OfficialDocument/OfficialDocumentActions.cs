using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.Reporting;

namespace Sungero.Docflow.Client
{
  partial class OfficialDocumentVersionsActions
  {
    public override void HideVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var document = OfficialDocuments.As(_obj.RootEntity);
      if (PublicFunctions.OfficialDocument.CanHideVersion(document, _obj.Number))
      {
        Dialogs.ShowMessage(OfficialDocuments.Resources.HideVersionOnAcquaintanceError, MessageType.Error);
        return;
      }
      
      base.HideVersion(e);
    }

    public override bool CanHideVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanHideVersion(e);
    }

    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var document = OfficialDocuments.As(_obj.RootEntity);
      if (PublicFunctions.OfficialDocument.CanDeleteVersion(document, _obj.Number))
      {
        Dialogs.ShowMessage(OfficialDocuments.Resources.DeleteVersionOnAcquaintanceError, MessageType.Error);
        return;
      }
      
      base.DeleteVersion(e);
    }

    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanDeleteVersion(e);
    }

    public virtual bool CanOpenOriginal(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.PublicBody.Size != 0;
    }

    public virtual void OpenOriginal(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      _obj.Open(DocumentBodySource.Original);
    }
  }

  internal static class OfficialDocumentStaticActions
  {
    public static bool CanShowDocumentReturn(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return RecordManagement.PublicFunctions.Module.GetDocumentReturnReport().CanExecute();
    }

    public static void ShowDocumentReturn(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      RecordManagement.PublicFunctions.Module.GetDocumentReturnReport().Open();
    }
  }

  partial class OfficialDocumentCollectionActions
  {
    public override void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (this.Entities.Count() != 1)
      {
        base.SendByMail(e);
        return;
      }
      
      var document = OfficialDocuments.As(_objs.FirstOrDefault());
      var relations = Functions.OfficialDocument.GetRelatedDocumentsWithVersions(document)
        .Where(x => x.AccessRights.CanRead(Users.Current)).ToList();
      Functions.OfficialDocument.SelectRelatedDocumentsAndCreateEmail(document, relations);
    }

    public override bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendByMail(e);
    }

    public override void Sign(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Sign(e);
    }

    public override bool CanSign(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSign(e);
    }

  }

  partial class OfficialDocumentTrackingActions
  {
    public virtual void ShowReturnAssignments(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var assignments = new List<Workflow.IAssignment>();
      try
      {
        assignments = Functions.OfficialDocument.Remote.GetReturnAssignments(_obj.ReturnTask);
      }
      catch (Sungero.Domain.Shared.Exceptions.SecuritySystemException)
      {
      }

      if (assignments.Any())
      {
        if (assignments.Count == 1)
          assignments.First().Show();
        else
        {
          var assignment = assignments.FirstOrDefault(a => Equals(a.Performer, Company.Employees.Current));
          if (assignment != null)
            assignment.Show();
          else
            assignments.Show();
        }
      }
      else
        Dialogs.NotifyMessage(Docflow.Resources.JobToReturnNotFound);
    }

    public virtual bool CanShowReturnAssignments(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.ReturnTask != null;
    }
  }

  partial class OfficialDocumentActions
  {
    public virtual void ShowOurSigningReason(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      bool showOurSigningReasonParamValue;
      
      if (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.ShowOurSigningReasonParam, out showOurSigningReasonParamValue))
        showOurSigningReasonParamValue = !showOurSigningReasonParamValue;
      else
        showOurSigningReasonParamValue = true;
      
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.ShowOurSigningReasonParam, showOurSigningReasonParamValue);
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
    }

    public virtual bool CanShowOurSigningReason(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.HasVersions && !_obj.State.IsInserted;
    }

    public virtual void ExportDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = new List<IOfficialDocument>() { _obj };
      Functions.Module.ExportDocumentDialog(documents);
    }

    public virtual bool CanExportDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged;
    }

    public virtual void ChangeDocumentType(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Запретить смену типа, если включен строгий доступ к документу.
      if (_obj.AccessRights.StrictMode != AccessRightsStrictMode.None)
      {
        // Используем диалоги, чтобы хинт не пробрасывался в задачу, в которую он вложен.
        Dialogs.ShowMessage(OfficialDocuments.Resources.DisableStrictModeToChangeType, MessageType.Error);
        return;
      }
      
      // Запретить смену типа, если документ зарегистрирован или зарезервирован.
      if (_obj.RegistrationState == OfficialDocument.RegistrationState.Registered &&
          _obj.DocumentKind.NumberingType != DocumentKind.NumberingType.Numerable ||
          _obj.RegistrationState == OfficialDocument.RegistrationState.Reserved)
      {
        Dialogs.ShowMessage(OfficialDocuments.Resources.NeedCancelRegistration, MessageType.Error);
        return;
      }
      
      // Запретить смену типа, если по документу есть активные задачи согласования по регламенту.
      if (Functions.OfficialDocument.Remote.HasApprovalTasksWithCurrentDocument(_obj))
      {
        Dialogs.ShowMessage(SimpleDocuments.Resources.NeedAbortApproval, MessageType.Error);
        return;
      }
      
      if (Functions.OfficialDocument.Remote.HasSpecifiedTypeRelations(_obj))
      {
        var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.NeedDeleteLink, MessageType.Error);
        var relatedDocumentsHyperlink = dialog.AddHyperlink(OfficialDocuments.Resources.RelatedDocumentsHyperlinkDisplayName);
        
        Action relatedDocumentsHyperlinkAction = () =>
        {
          var relatedDocuments = _obj.Relations.GetRelated().Union(_obj.Relations.GetRelatedFrom());
          relatedDocuments.ShowModal();
        };
        
        relatedDocumentsHyperlink.SetOnExecute(relatedDocumentsHyperlinkAction);
        dialog.Buttons.AddOk();
        dialog.Show();
        return;
      }
      
      var types = Functions.OfficialDocument.GetTypesAvailableForChange(_obj);
      var convertedDocument = Functions.OfficialDocument.ChangeDocumentType(_obj, types);
      if (convertedDocument != null)
      {
        // Dmitriev_IA: Критически важно для корректного открытия в десктоп клиенте карточки сконвертированного документа.
        e.CloseFormAfterAction = true;
        convertedDocument.Show();
      }
    }

    public virtual bool CanChangeDocumentType(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged && _obj.AccessRights.CanUpdate() &&
        Functions.OfficialDocument.CanChangeDocumentType(_obj);
    }
    
    public virtual void CreateManyAddendum(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.AddManyAddendumDialog(_obj);
    }

    public virtual bool CanCreateManyAddendum(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void ConvertToPdf(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // При асинхронном преобразовании не отправлять на обработку повторно.
      int convertingVersionIdParamValue = -1;
      bool needConversion = !e.Params.TryGetValue(Constants.OfficialDocument.ConvertingVersionId, out convertingVersionIdParamValue) ||
        convertingVersionIdParamValue != _obj.LastVersion.Id;
      
      // Преобразование в PDF.
      Structures.OfficialDocument.СonversionToPdfResult result = null;
      if (needConversion)
      {
        result = Sungero.Docflow.Functions.OfficialDocument.Remote.ConvertToPdfWithSignatureMark(_obj);
        if (result.IsOnConvertion)
          e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.ConvertingVersionId, _obj.LastVersion.Id);
        
        // Успешная интерактивная конвертация.
        if (!result.HasErrors && result.IsFastConvertion)
        {
          Dialogs.NotifyMessage(OfficialDocuments.Resources.ConvertionDone);
          return;
        }
      }
      
      // Сообщение об ошибке при асинхронном преобразовании.
      if (needConversion && result.HasErrors)
      {
        Dialogs.ShowMessage(result.ErrorTitle, result.ErrorMessage, MessageType.Information);
        return;
      }
      
      if (needConversion && Sungero.Docflow.Functions.OfficialDocument.Remote.IsExchangeDocument(_obj, _obj.LastVersion.Id))
      {
        Dialogs.ShowMessage(OfficialDocuments.Resources.ConvertionInProgress, OfficialDocuments.Resources.CloseDocumentAndOpenLater, MessageType.Information);
        return;
      }
    }

    public virtual bool CanConvertToPdf(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var isDesktop = ClientApplication.ApplicationType == ApplicationType.Desktop;
      return !isDesktop &&
        !_obj.State.IsInserted &&
        _obj.HasVersions &&
        !_obj.State.IsChanged &&
        _obj.AccessRights.CanUpdate() &&
        Locks.GetLockInfo(_obj).IsLockedByMe;
    }

    public virtual void ShowRelatedDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var related = _obj.Relations.GetRelated().Union(_obj.Relations.GetRelatedFrom());
      related.Show();
    }

    public virtual bool CanShowRelatedDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.HasRelations;
    }

    public virtual void BarcodeReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetBarcodePageReport();
      var tenantId = Functions.Module.Remote.GetCurrentTenantId();
      var formattedTenantId = PublicFunctions.Module.FormatTenantIdForBarcode(tenantId);
      report.barcode = string.Format("{0} - {1}", formattedTenantId, _obj.Id);
      report.barcodeName = string.Format("{0} - {1}", SystemInfo.GetBrandName(), _obj.Id);
      report.Open();
    }

    public virtual bool CanBarcodeReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowAcquaintanceReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.OfficialDocument.Remote.GetAcquaintanceTasks(_obj).Any())
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.NoAcquaintanceTasks);
        return;
      }
      
      RecordManagement.PublicFunctions.Module.GetAcquaintanceReport(_obj).Open();
    }

    public virtual bool CanShowAcquaintanceReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && !_obj.State.IsInserted;
    }

    public virtual void SendForAcquaintance(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      
      var acqTask = RecordManagement.PublicFunctions.Module.Remote.CreateAcquaintanceTask(_obj);
      if (acqTask != null)
      {
        acqTask.Show();
        e.CloseFormAfterAction = true;
      }
    }

    public virtual bool CanSendForAcquaintance(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.OfficialDocument.CanExecuteSendAction(_obj, _obj.Info.Actions.SendForAcquaintance);
    }

    public virtual void OpenExchangeOrderReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var info = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(_obj);
      if (info == null)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.DocumentNotFromService);
        return;
      }
      else if (info.SenderSignId == null && info.ReceiverSignId == null)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.ExchangeReportOnlyInService);
        return;
      }
      Functions.Module.RunExchangeOrderReport(_obj);
    }

    public virtual bool CanOpenExchangeOrderReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.HasVersions && !_obj.State.IsInserted;
    }

    public virtual void OpenInExchangeService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      try
      {
        var hyperlink = Sungero.Exchange.PublicFunctions.Module.Remote.GetDocumentHyperlink(_obj);
        if (string.IsNullOrWhiteSpace(hyperlink))
          e.AddInformation(OfficialDocuments.Resources.DocumentNotInService);
        else
          Hyperlinks.Open(hyperlink);
      }
      catch (AppliedCodeException ex)
      {
        e.AddError(ex.Message);
      }
    }

    public virtual bool CanOpenInExchangeService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ExchangeState.HasValue;
    }

    public virtual void SendToCounterparty(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Exchange.PublicFunctions.Module.SendResultToCounterparty(_obj);
    }

    public virtual bool CanSendToCounterparty(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged && _obj.AccessRights.CanUpdate() &&
        _obj.AccessRights.CanSendByExchange() && _obj.HasVersions;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Добавить признак того, что версия создается из шаблона.
      e.Params.Add(Constants.Module.CreateFromTemplate, true);
      
      base.CreateFromTemplate(e);
      
      e.Params.Remove(Constants.Module.CreateFromTemplate);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromTemplate(e);
    }

    public virtual void CreateAddendum(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var addendum = Functions.Addendum.Remote.Create();
      addendum.LeadingDocument = _obj;
      addendum.Show();
    }

    public virtual bool CanCreateAddendum(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Если по главному документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateReviewTask(_obj))
        return;
      
      // Принудительно сохранить документ, чтобы сохранились связи. Иначе они не попадут в задачу.
      _obj.Save();
      
      var task = Functions.Module.CreateDocumentReview(_obj);
      task.Show();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanSendForReview(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ExecutionState != Docflow.OfficialDocument.ExecutionState.OnExecution &&
        Functions.OfficialDocument.CanExecuteSendAction(_obj, _obj.Info.Actions.SendForReview);
    }

    public virtual void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Принудительно сохранить документ, чтобы сохранились связи. Иначе они не попадут в задачу.
      _obj.Save();
      
      Functions.Module.Remote.CreateFreeApprovalTask(_obj).Show();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanSendForFreeApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.OfficialDocument.CanExecuteSendAction(_obj, _obj.Info.Actions.SendForFreeApproval);
    }

    public virtual void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.RunApprovalSheetReport(_obj);
    }

    public virtual bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.HasVersions && !_obj.State.IsInserted;
    }

    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e) && Functions.OfficialDocument.CheckDeleteEntityAccessRights(_obj);
    }

    public virtual void ReturnFromCounterparty(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // TODO: продумать ситуацию когда на часть задач все-таки есть права.
      if (Functions.Module.IsLockedByOther(_obj, e))
        return;
      
      var tracking = _obj.Tracking.Where(l => (!l.ReturnDate.HasValue || Equals(l.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.AtControl)) &&
                                         l.ReturnDeadline.HasValue &&
                                         l.Action == Docflow.OfficialDocumentTracking.Action.Endorsement).ToList();
      
      if (tracking.Any() && tracking.All(t => t.ExternalLinkId != null))
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.TrackingNoneReturnFromCounterparty);
        return;
      }
      
      tracking = tracking.Where(t => t.ReturnTask != null).ToList();

      if (!tracking.Any())
      {
        Dialogs.NotifyMessage(Docflow.Resources.ReturnDocumentActiveTrackingNotFound);
        return;
      }

      var employees = new List<Company.IEmployee>();
      foreach (var emp in tracking.Select(l => l.DeliveredTo))
        if (!employees.Contains(emp))
          employees.Add(emp);
      var employee = employees.FirstOrDefault();

      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.ReturnDocumentDialog, _obj.Name);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.ReturnFromCounterparty;
      INavigationDialogValue<Company.IEmployee> employeeDialog = null;
      if (employees.Count > 1)
        employeeDialog = dialog.AddSelect(Docflow.Resources.ReturnDocumentEmployee, true, employee).From(employees);
      var returnDate = dialog.AddDate(Docflow.Resources.ReturnDocumentDate, true, Calendar.UserToday);
      
      // Принудительно увеличиваем ширину диалога в Web для корректного отображения кнопок.
      if (ClientApplication.ApplicationType == ApplicationType.Web)
      {
        var fakeControl = dialog.AddString("12345678901234", false);
        fakeControl.IsVisible = false;
      }

      var signed = dialog.Buttons.AddCustom(Docflow.Resources.ReturnDocumentSigned);
      dialog.Buttons.Default = signed;
      var notSigned = dialog.Buttons.AddCustom(Docflow.Resources.ReturnDocumentNotSigned);

      var hasAvailableTasks = false;
      foreach (var row in tracking)
      {
        if (row.ReturnTask.AccessRights.CanRead())
          hasAvailableTasks = hasAvailableTasks || Functions.OfficialDocument.Remote.GetReturnAssignments(row.ReturnTask).Any();
      }
      CommonLibrary.DialogButton openAssignments = null;
      if (hasAvailableTasks)
        openAssignments = dialog.Buttons.AddCustom(Docflow.Resources.ReturnDocumentOpenAssignment);

      dialog.Buttons.AddCancel();

      var result = dialog.Show();
      if (result != DialogButtons.Cancel)
      {
        if (employeeDialog != null)
          employee = employeeDialog.Value;
        var employeeTracking = tracking.Where(l => Equals(l.DeliveredTo, employee));
        
        if (result == signed || result == notSigned)
        {
          var returnResult = result == signed ?
            Docflow.OfficialDocumentTracking.ReturnResult.Signed :
            Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
          foreach (var row in employeeTracking)
          {
            row.ReturnDate = returnDate.Value;
            row.ReturnResult = returnResult;
          }

          _obj.Save();
          Dialogs.NotifyMessage(Docflow.Resources.ReturnDocumentNotify);
        }
        
        if (result == openAssignments)
        {
          try
          {
            var returnTasks = employeeTracking.Select(i => i.ReturnTask).ToList();
            var assignments = Functions.OfficialDocument.Remote.GetReturnAssignments(returnTasks);

            if (assignments.Count == 1)
              assignments.Single().Show();
            else if (assignments.Count > 1)
              assignments.Show();
            else if (!assignments.Any())
              Dialogs.NotifyMessage(Docflow.Resources.JobToReturnNotFound);
          }
          catch (Exception)
          {
            Dialogs.NotifyMessage(Docflow.Resources.JobToReturnNotFound);
          }
        }
      }
    }

    public virtual bool CanReturnFromCounterparty(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() &&
        _obj.AccessRights.CanRegister() &&
        (_obj.IsHeldByCounterParty == true);
    }

    public virtual void ReturnDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Functions.Module.IsLockedByOther(_obj, e))
        return;
      
      var tracking = _obj.Tracking.Where(l => (!l.ReturnDate.HasValue || Equals(l.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.AtControl)) &&
                                         l.ReturnDeadline.HasValue &&
                                         (l.Action == Docflow.OfficialDocumentTracking.Action.Delivery ||
                                          l.Action == Docflow.OfficialDocumentTracking.Action.Sending)).ToList();

      if (!tracking.Any())
      {
        Dialogs.NotifyMessage(Docflow.Resources.ReturnDocumentActiveTrackingNotFound);
        return;
      }

      var employees = tracking.Select(l => l.DeliveredTo).Distinct().ToList();
      var employee = employees.FirstOrDefault();

      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.ReturnDocumentDialog, _obj.Name);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.Return;
      INavigationDialogValue<Company.IEmployee> employeeDialog = null;
      if (employees.Count > 1)
        employeeDialog = dialog.AddSelect(Docflow.Resources.ReturnDocumentEmployee, true, employee).From(employees);
      var returnDate = dialog.AddDate(Docflow.Resources.ReturnDocumentDate, true, Calendar.UserToday);
      
      var openAssignments = dialog.AddHyperlink(Docflow.Resources.ReturnDocumentOpenAssignment);
      var returnTasks = tracking.Where(x => x.ReturnTask.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, Users.Current)).Select(x => x.ReturnTask).ToList();
      var hasAvailableTasks = Functions.OfficialDocument.Remote.GetReturnAssignments(returnTasks).Any();
      openAssignments.IsVisible = hasAvailableTasks;

      var returnDocument = dialog.Buttons.AddCustom(Docflow.Resources.ReturnDocument);
      dialog.Buttons.Default = returnDocument;
      
      dialog.Buttons.AddCancel();

      var employeeTracking = tracking.Where(l => Equals(l.DeliveredTo, employee));
      dialog.SetOnRefresh(d =>
                          {
                            if (employeeDialog != null)
                              employee = employeeDialog.Value;
                            openAssignments.IsEnabled = employee != null;
                            if (employee == null)
                              return;
                            
                            employeeTracking = tracking.Where(l => Equals(l.DeliveredTo, employee));
                            if (returnDate.Value.HasValue && employeeTracking.Any(x => returnDate.Value.Value < x.DeliveryDate.Value))
                              d.AddError(Docflow.Resources.ReturnDocumentDeliveryAndReturnDate, returnDate);
                          });
      
      openAssignments.SetOnExecute(
        () =>
        {
          try
          {
            var employeeReturnTasks = employeeTracking.Select(i => i.ReturnTask).ToList();
            var assignments = Functions.OfficialDocument.Remote.GetReturnAssignments(employeeReturnTasks);

            if (assignments.Count == 1)
              assignments.Single().ShowModal();
            else if (assignments.Count > 1)
              assignments.ShowModal();
            else if (!assignments.Any())
              Dialogs.NotifyMessage(Docflow.Resources.JobToReturnNotFound);
          }
          catch (Exception)
          {
            // TODO: продумать ситуацию когда на часть задач все-таки есть права.
            Dialogs.NotifyMessage(Docflow.Resources.JobToReturnNotFound);
          }
        });
      
      var result = dialog.Show();
      if (result == returnDocument)
      {
        var returnResult = Docflow.OfficialDocumentTracking.ReturnResult.Returned;
        foreach (var row in employeeTracking)
        {
          row.ReturnDate = returnDate.Value;
          row.ReturnResult = returnResult;
        }

        _obj.Save();
        Dialogs.NotifyMessage(Docflow.Resources.ReturnDocumentNotify);
      }
    }

    public virtual bool CanReturnDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() &&
        _obj.AccessRights.CanRegister() &&
        (_obj.IsReturnRequired == true);
    }

    public virtual void DeliverDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Functions.Module.IsLockedByOther(_obj, e))
        return;
      
      // Срок внутренней выдачи по умолчанию - 10 рабочих дней.
      var additionalDays = 10;
      
      var dialog = Dialogs.CreateInputDialog(Docflow.Resources.DeliverDocumentDialog, _obj.Name);
      dialog.HelpCode = Constants.OfficialDocument.HelpCode.Deliver;
      var employee = dialog.AddSelect(Docflow.Resources.DeliverDocumentEmployee, true, Company.Employees.Null)
        .Where(x => Equals(x.Status, CoreEntities.DatabookEntry.Status.Active));
      var deliveryDate = dialog.AddDate(Docflow.Resources.DeliverDocumentDeliveryDate, true, Calendar.UserToday);
      var returnDate = dialog.AddDate(Docflow.Resources.DeliverDocumentSheduledReturnDate, false, Calendar.UserToday.AddWorkingDays(additionalDays));
      var comment = dialog.AddMultilineString(Docflow.Resources.DeliverDocumentComment, false);
      var canIssueOriginal = dialog.AddBoolean(Docflow.Resources.DeliverDocumentIsOriginal, true);
      dialog.SetOnRefresh(d =>
                          {
                            if (returnDate.Value.HasValue && deliveryDate.Value.HasValue && returnDate.Value.Value < deliveryDate.Value.Value)
                              d.AddError(Docflow.Resources.ReturnDocumentDeliveryAndReturnDate, returnDate);
                          });
      if (dialog.Show() == DialogButtons.Ok)
      {
        var issued = _obj.Tracking.AddNew();
        issued.DeliveryDate = deliveryDate.Value;
        issued.ReturnDeadline = returnDate.Value;
        issued.Note = comment.Value;
        issued.DeliveredTo = employee.Value;
        issued.IsOriginal = canIssueOriginal.Value;
        _obj.Save();
        Dialogs.NotifyMessage(Docflow.Resources.DeliverDocumentNotify);
      }
    }

    public virtual bool CanDeliverDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.DocumentKind != null &&
        ((_obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable && _obj.AccessRights.CanRegister()) ||
         _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable);
    }

    public virtual void ShowRegistrationPane(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      bool showParamValue;
      var showParam = e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.ShowParam, out showParamValue);
      if (showParam)
        showParam = !showParamValue;
      else
      {
        var setting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
        var settingShow = setting != null && setting.ShowRegPane == true;
        var notNumerableKind = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType != Docflow.DocumentKind.NumberingType.NotNumerable;
        showParam = !(settingShow || (_obj.AccessRights.CanRegister() && notNumerableKind));
      }
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.ShowParam, showParam);
      Functions.OfficialDocument.RefreshDocumentForm(_obj);
    }

    public virtual bool CanShowRegistrationPane(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CancelRegistration(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var settingType = Functions.OfficialDocument.GetSettingType(_obj);
      
      // Можно отменять регистрацию только журналов своей группы регистрации.
      if (!Functions.OfficialDocument.CanChangeRequisitesOrCancelRegistration(_obj))
      {
        e.AddError(settingType == Docflow.RegistrationSetting.SettingType.Registration ?
                   Docflow.Resources.NeedRightOnDocumentRegisterToUnregister :
                   Docflow.Resources.NeedRightOnDocumentRegisterToUnreserve);
        return;
      }

      // Поля будут очищены после отмены, проверять обязательность нет смысла.
      // Обязательность полей будет восстановлена на рефреше.
      _obj.State.Properties.RegistrationNumber.IsRequired = false;
      _obj.State.Properties.RegistrationDate.IsRequired = false;
      _obj.State.Properties.DocumentRegister.IsRequired = false;
      if (!e.Validate())
        return;
      
      var text = Docflow.Resources.CancelRegistration;
      var description = Functions.OfficialDocument.GetCancelRegistrationDialogDescription(_obj, settingType);
      if (settingType == Docflow.RegistrationSetting.SettingType.Reservation)
        text = Docflow.Resources.CancelReservation;
      if (settingType == Docflow.RegistrationSetting.SettingType.Numeration)
        text = Docflow.Resources.CancelNumbering;
      
      var dialog = Dialogs.CreateTaskDialog(text, description, MessageType.Information);
      dialog.Buttons.AddYesNo();
      dialog.Buttons.Default = DialogButtons.Yes;
      if (dialog.Show() == DialogButtons.Yes)
      {
        var needSaveDocument = _obj.DocumentKind.NumberingType != Docflow.DocumentKind.NumberingType.Numerable ||
          !_obj.DocumentKind.AutoNumbering.Value;
        
        Functions.OfficialDocument.RegisterDocument(_obj, DocumentRegisters.Null, null, null, false, needSaveDocument);

        // Задизейблить свойства.
        _obj.State.Properties.DocumentRegister.IsEnabled = false;
        _obj.State.Properties.RegistrationNumber.IsEnabled = false;
        _obj.State.Properties.RegistrationDate.IsEnabled = false;
        
        // Не показывать хинт о перерегистрации.
        bool repeatRegister;
        if (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, out repeatRegister) && repeatRegister)
          e.Params.Remove(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister);
      }
      
      // Отменить подсветку рег.номера и даты.
      this._obj.State.Properties.RegistrationNumber.HighlightColor = Sungero.Core.Colors.Empty;
      this._obj.State.Properties.RegistrationDate.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual bool CanCancelRegistration(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return false;

      var accessRights = _obj.AccessRights;
      var isNotifiable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
      var isNumerable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable;
      var isRegistered = _obj.RegistrationState == RegistrationState.Registered;
      var isReserved = _obj.RegistrationState == RegistrationState.Reserved;

      var canUnregister = isRegistered &&
        ((isNotifiable && accessRights.CanRegister()) || isNumerable);

      bool hasReservationSetting;
      var canUnreserve = isReserved &&
        (accessRights.CanRegister() ||
         (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, out hasReservationSetting) && hasReservationSetting));
      
      // Это действие используется как для отмены регистрации / нумерации, так и для отмены резервирования номера.
      // В режиме изменения реквизитов отменить регистрацию нельзя.
      var isRequisiteChangeModeOn = e.Params.Contains(Constants.OfficialDocument.NeedValidateRegisterFormat);
      return accessRights.CanUpdate() && (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted) && (canUnregister || canUnreserve) && !isRequisiteChangeModeOn;
    }

    public virtual void AssignNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Bug 96201
      // Сброс подсветки полей происходит в событии контрола InputValue.
      // Действия регистрации изменяют значения полей программно, событие ValueInput не вызывается.
      var originalNumber = _obj.State.Properties.RegistrationNumber.OriginalValue;
      var originalDate = _obj.State.Properties.RegistrationDate.OriginalValue;
      
      Functions.Module.RemoveNeedValidateRegisterFormatParameter(_obj, e);
      
      var isNumerable = _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable;
      if (isNumerable)
        Functions.OfficialDocument.AssignNumber(_obj, e);
      else
        Functions.OfficialDocument.ReserveNumber(_obj, e);

      if (_obj.RegistrationNumber != originalNumber)
        _obj.State.Properties.RegistrationNumber.HighlightColor = Colors.Empty;
      if (_obj.RegistrationDate != originalDate)
        _obj.State.Properties.RegistrationDate.HighlightColor = Colors.Empty;
    }

    public virtual bool CanAssignNumber(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return false;

      var accessRights = _obj.AccessRights;
      var isNotifiable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
      var isRegistered = _obj.RegistrationState == RegistrationState.Registered;
      var isReserved = _obj.RegistrationState == RegistrationState.Reserved;
      var isNumerable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable;
      bool hasReservationSetting;
      var canReserveNumber = accessRights.CanRegister() ||
        (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, out hasReservationSetting) && hasReservationSetting);
      
      return accessRights.CanUpdate() &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted) &&
        ((isNumerable && !isRegistered) ||
         (documentKind.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Incoming &&
          !isRegistered &&
          !isReserved &&
          isNotifiable &&
          canReserveNumber));
    }

    public virtual void ChangeRequisites(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Можно изменять регистрационные данные только журналов своей группы регистрации.
      if (!Functions.OfficialDocument.CanChangeRequisitesOrCancelRegistration(_obj))
      {
        e.AddError(Docflow.Resources.NeedRightOnDocumentRegisterToChangeDocument);
        return;
      }

      if (!_obj.State.Properties.RegistrationDate.IsVisible)
        this.ShowRegistrationPane(e);

      if (_obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable)
        e.AddInformation(Docflow.Resources.SaveDocumentToCompleteNumbering);
      else
        e.AddInformation(Docflow.Resources.SaveDocumentToCompleteRegistration);

      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, true);
      e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
    }

    public virtual bool CanChangeRequisites(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return false;

      var accessRights = _obj.AccessRights;
      var isNotifiable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;
      var isNumerable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Numerable;
      var isRegistered = _obj.RegistrationState == RegistrationState.Registered;
      var isReserved = _obj.RegistrationState == RegistrationState.Reserved;

      var canUnregister = isRegistered &&
        ((isNotifiable && accessRights.CanRegister()) || isNumerable);
      
      bool hasReservationSetting;
      var canUnreserve = isReserved &&
        (accessRights.CanRegister() ||
         (e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.HasReservationSetting, out hasReservationSetting) && hasReservationSetting));
      
      return accessRights.CanUpdate() && (canUnregister || canUnreserve) && (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted);
    }
    
    public virtual void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Bug 96201
      // Сброс подсветки полей происходит в событии контрола InputValue.
      // Действия регистрации изменяют значения полей программно, событие ValueInput не вызывается.
      var originalNumber = _obj.State.Properties.RegistrationNumber.OriginalValue;
      var originalDate = _obj.State.Properties.RegistrationDate.OriginalValue;
      
      Functions.Module.RemoveNeedValidateRegisterFormatParameter(_obj, e);
      
      Functions.OfficialDocument.Register(_obj, e);
      
      if (_obj.RegistrationNumber != originalNumber)
        _obj.State.Properties.RegistrationNumber.HighlightColor = Colors.Empty;
      if (_obj.RegistrationDate != originalDate)
        _obj.State.Properties.RegistrationDate.HighlightColor = Colors.Empty;
    }

    public virtual bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind == null)
        return false;

      var accessRights = _obj.AccessRights;
      var isNotifiable = documentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable;

      return
        accessRights.CanUpdate() &&
        _obj.RegistrationState != RegistrationState.Registered &&
        (isNotifiable && accessRights.CanRegister()) &&
        (Functions.Module.IsLockedByMe(_obj) || _obj.State.IsInserted);
    }

    public virtual void SendActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Принудительно сохранить документ, чтобы сохранились связи. Иначе они не попадут в задачу.
      _obj.Save();
      
      var hackTask = Functions.Module.CreateActionItemExecution(_obj);
      if (hackTask != null)
      {
        hackTask.Show();
        e.CloseFormAfterAction = true;
      }
    }

    public virtual bool CanSendActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.OfficialDocument.CanExecuteSendAction(_obj, _obj.Info.Actions.SendActionItem);
    }

    public virtual void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Если по документу ранее были запущены задачи, то вывести соответствующий диалог.
      if (!Docflow.PublicFunctions.OfficialDocument.NeedCreateApprovalTask(_obj))
        return;
      
      // Принудительно сохранить документ, чтобы сохранились связи. Иначе они не попадут в задачу.
      _obj.Save();
      
      this.CreateApprovalTask(e);
    }

    private void CreateApprovalTask(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var availableApprovalRules = Functions.OfficialDocument.Remote.GetApprovalRules(_obj).ToList();

      if (availableApprovalRules.Any())
      {
        var task = Functions.Module.Remote.CreateApprovalTask(_obj);
        task.Show();
        e.CloseFormAfterAction = true;
      }
      else
      {
        // Если по документу нет регламента, вывести сообщение.
        Dialogs.ShowMessage(OfficialDocuments.Resources.NoApprovalRuleWarning, MessageType.Warning);
        throw new OperationCanceledException();
      }
    }
    
    public virtual bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.OfficialDocument.CanExecuteSendAction(_obj, _obj.Info.Actions.SendForApproval);
    }

  }

}
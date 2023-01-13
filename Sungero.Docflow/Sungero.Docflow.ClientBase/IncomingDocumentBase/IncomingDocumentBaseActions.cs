using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;
using Sungero.Reporting;

namespace Sungero.Docflow.Client
{
  partial class IncomingDocumentBaseActions
  {
    public override void ConvertToPdf(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ConvertToPdf(e);
    }

    public override bool CanConvertToPdf(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.ExchangeState.HasValue;
    }

    public virtual void ChangeManyAddressees(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyAddressees == false)
      {
        Dialogs.NotifyMessage(OfficialDocuments.Resources.FillAddresseesOnAddresseesTab);
        _obj.IsManyAddressees = true;
      }
      else if (_obj.IsManyAddressees == true)
      {
        if (_obj.Addressees.Count(a => a.Addressee != null) > 1)
        {
          var addresseeRaw = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Addressee != null);
          var addresseeName = addresseeRaw.Addressee.Person.ShortName;
          var dialog = Dialogs.CreateTaskDialog(OfficialDocuments.Resources.ChangeManyAddresseesQuestion,
                                                OfficialDocuments.Resources.ChangeManyAddresseesDescriptionFormat(addresseeName), MessageType.Question);
          dialog.Buttons.AddYesNo();
          if (dialog.Show() == DialogButtons.Yes)
            _obj.IsManyAddressees = false;
        }
        else
        {
          _obj.IsManyAddressees = false;
        }
      }
    }

    public virtual bool CanChangeManyAddressees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      // Смена режима многоадресности доступна только пользователям с правами на изменение документа.
      return _obj.AccessRights.CanUpdate() ? _obj.State.Properties.IsManyAddressees.IsEnabled : false;
    }

    public virtual void OpenActionItems(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.GetActionItemsByDocument(_obj).Show();
    }

    public virtual bool CanOpenActionItems(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void OpenActionItemExecutionReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var actionItemsArePresent = RecordManagement.PublicFunctions.Module.Remote.ActionItemCompletionDataIsPresent(null, _obj);
      
      if (!actionItemsArePresent)
      {
        Dialogs.NotifyMessage(RecordManagement.Reports.Resources.ActionItemsExecutionReport.NoAnyActionItemsForDocument);
        return;
      }
      else
      {
        var actionItemExecutionReport = RecordManagement.Reports.GetActionItemsExecutionReport();
        actionItemExecutionReport.Document = _obj;
        actionItemExecutionReport.Open();
      }
    }

    public virtual bool CanOpenActionItemExecutionReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void CreateOutgoingLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var letter = Docflow.PublicFunctions.OfficialDocument.Remote.CreateReplyDocument(_obj);
      letter.Show();
    }

    public virtual bool CanCreateOutgoingLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }
    
    public virtual void AddRegistrationStamp(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить, что преобразование уже запущено, чтобы не запускать еще раз при повторном нажатии.
      // Вместо этого будет показан диалог о том, что преобразование в процессе.
      int convertingVersionIdParamValue = -1;
      bool addingRegistrationStampIsInProcess = e.Params.TryGetValue(Constants.OfficialDocument.ConvertingVersionId, out convertingVersionIdParamValue) &&
        convertingVersionIdParamValue == _obj.LastVersion.Id;
      
      // Преобразование в PDF.
      Structures.OfficialDocument.СonversionToPdfResult result = null;
      if (!addingRegistrationStampIsInProcess)
      {
        // Проверки возможности преобразования и наложения отметки.
        result = Functions.IncomingDocumentBase.ValidatePdfConvertibilityByExtension(_obj);
        if (!result.HasErrors)
        {
          var position = Functions.IncomingDocumentBase.ShowAddRegistrationStampDialog(_obj);
          if (position == null)
            return;
          
          result = Functions.IncomingDocumentBase.Remote.AddRegistrationStamp(_obj, position.RightIndent, position.BottomIndent);
          
          if (result.IsOnConvertion)
            e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.ConvertingVersionId, _obj.LastVersion.Id);
          
          // Успешная интерактивная конвертация.
          if (!result.HasErrors && result.IsFastConvertion)
          {
            Dialogs.NotifyMessage(OfficialDocuments.Resources.ConvertionDone);
            return;
          }
        }
      }
      
      // Сообщение об ошибке при асинхронном преобразовании.
      if (!addingRegistrationStampIsInProcess && result.HasErrors)
      {
        Dialogs.ShowMessage(result.ErrorTitle, result.ErrorMessage, MessageType.Information);
        return;
      }
    }

    public virtual bool CanAddRegistrationStamp(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var isRegistered = _obj.RegistrationState == RegistrationState.Registered;
      var isDesktop = ClientApplication.ApplicationType == ApplicationType.Desktop;
      return !isDesktop &&
        !_obj.State.IsInserted &&
        !_obj.State.IsChanged &&
        _obj.HasVersions &&
        _obj.AccessRights.CanUpdate() &&
        Locks.GetLockInfo(_obj).IsLockedByMe &&
        isRegistered &&
        _obj.ExchangeState == null;
    }

  }

  internal static class IncomingDocumentBaseStaticActions
  {
    public static void DocumentProcessingReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      RecordManagement.PublicFunctions.Module.GetIncomingDocumentsProcessingReport().Open();
    }

    public static bool CanDocumentProcessingReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return RecordManagement.PublicFunctions.Module.GetIncomingDocumentsProcessingReport().CanExecute();
    }

    public static bool CanDocumentRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return RecordManagement.PublicFunctions.Module.GetIncomingDocumentsReport().CanExecute();
    }

    public static void DocumentRegister(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      RecordManagement.PublicFunctions.Module.GetIncomingDocumentsReport().Open();
    }
  }

}
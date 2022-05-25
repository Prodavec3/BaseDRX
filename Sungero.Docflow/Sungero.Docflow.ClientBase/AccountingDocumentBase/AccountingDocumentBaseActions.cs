using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;

namespace Sungero.Docflow.Client
{

  partial class AccountingDocumentBaseVersionsActions
  {

    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var accountDocument = AccountingDocumentBases.As(_obj.ElectronicDocument);
      return base.CanEditVersion(e) && accountDocument.IsFormalized != true;
    }

    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteVersion(e);
    }

    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var accountDocument = AccountingDocumentBases.As(_obj.ElectronicDocument);
      return base.CanDeleteVersion(e) && accountDocument.IsFormalized != true;
    }

    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var accountDocument = AccountingDocumentBases.As(_obj.ElectronicDocument);
      return base.CanImportVersion(e) && accountDocument.IsFormalized != true;
    }

    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var accountDocument = AccountingDocumentBases.As(_obj.ElectronicDocument);
      return base.CanCreateVersion(e) && accountDocument.IsFormalized != true;
    }

  }

  partial class AccountingDocumentBaseCollectionActions
  {
    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanOpenDocumentEdit(e) && _objs.Any(x => x.IsFormalized != true);
    }

    /// <summary>
    /// Открытие документа на чтение.
    /// Действие может вызываться как для одного документа, так и для нескольких.
    /// Если действие вызывается для нескольких документов, то откроется на чтение только один.
    /// </summary>
    /// <param name="e">Объект события.</param>
    public override void OpenDocumentRead(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentRead(e);
    }

    /// <summary>
    /// Проверка возможности открыть документ на чтение.
    /// </summary>
    /// <param name="e">Объект события.</param>
    /// <returns>Возможность выполнения действия.</returns>
    public override bool CanOpenDocumentRead(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanOpenDocumentRead(e);
    }

    /// <summary>
    /// Проверка возможности экспортировать документы.
    /// </summary>
    /// <param name="e">Объект события.</param>
    /// <returns>Возможность выполнить экспорт.</returns>
    public virtual bool CanExportFinancialDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (e.FormType == Domain.Shared.FormType.Collection)
        return true;
      
      return _objs.Any() && _objs.All(d => !d.State.IsChanged);
    }

    /// <summary>
    /// Открытие диалога для экспорта документов из фин. архива.
    /// </summary>
    /// <param name="e">Объект события.</param>
    public virtual void ExportFinancialDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (Functions.Module.CheckFinancialArchiveLicense())
        Functions.Module.ExportDocumentDialog(_objs.ToList<IOfficialDocument>());
    }

    public virtual bool CanPrintEnvelope(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_objs.Any(t => t.State.IsInserted || t.State.IsChanged);
    }

    public virtual void PrintEnvelope(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(null, null, _objs.ToList());
    }
  }

  partial class AccountingDocumentBaseActions
  {

    public virtual void FillSellerInfo(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsFormalized == true && _obj.SellerTitleId != null && FinancialArchive.PublicFunctions.Module.Remote.HasSellerSignatoryInfo(_obj))
      {
        var dialogDescription = string.Empty;
        if (Sungero.FinancialArchive.PublicFunctions.Module.Remote.HasUnsignedSellerTitle(_obj))
          dialogDescription = string.Format(AccountingDocumentBases.Resources.LastVersionSellerTitleWillBeChanged, _obj.LastVersion.Number);
        else
          dialogDescription = AccountingDocumentBases.Resources.NewVersionWillBeCreated;
        
        var confirmDialog = Dialogs.CreateTaskDialog(AccountingDocumentBases.Resources.RefillSellerTitleQuestion, dialogDescription, MessageType.Warning);
        var customButton = confirmDialog.Buttons.AddCustom(AccountingDocumentBases.Resources.FillingButtonText);
        confirmDialog.Buttons.Default = customButton;
        confirmDialog.Buttons.AddCancel();
        
        if (confirmDialog.Show() == DialogButtons.Cancel)
          return;
      }

      Functions.AccountingDocumentBase.SellerTitlePropertiesFillingDialog(_obj);
    }

    public virtual bool CanFillSellerInfo(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsFormalized == true && _obj.AccessRights.CanUpdate() &&
        _obj.SellerSignatureId == null && !_obj.State.IsInserted;
    }

    public virtual void PrintEnvelopeCard(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.OutgoingDocumentBase.ShowSelectEnvelopeFormatDialog(null, null, new List<IAccountingDocumentBase>() { _obj });
    }

    public virtual bool CanPrintEnvelopeCard(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void FillBuyerInfo(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.ExchangeState != OfficialDocument.ExchangeState.SignRequired)
      {
        if (Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(_obj) != null)
          Dialogs.NotifyMessage(Exchange.Resources.AnswerIsAlreadySent);
        else
          Dialogs.NotifyMessage(AccountingDocumentBases.Resources.FillBuyerInfoOutgoingDocument);
      }
      else if (_obj.BusinessUnitBox != null && Equals(_obj.BusinessUnitBox.ExchangeService.ExchangeProvider, ExchangeCore.ExchangeService.ExchangeProvider.Sbis) &&
               !Exchange.PublicFunctions.Module.Remote.CanSendSign(_obj))
      {
        Dialogs.NotifyMessage(Exchange.Resources.AnswerIsAlreadySent);
      }
      else
      {
        if (_obj.BuyerTitleId != null)
        {
          var dialogDescription = string.Empty;
          if (Sungero.Exchange.PublicFunctions.Module.Remote.HasUnsignedBuyerTitle(_obj))
            dialogDescription = string.Format(AccountingDocumentBases.Resources.LastVersionWillBeChanged, _obj.LastVersion.Number);
          else
            dialogDescription = AccountingDocumentBases.Resources.NewVersionWillBeCreated;
          
          var confirmDialog = Dialogs.CreateTaskDialog(AccountingDocumentBases.Resources.RefillBuyerTitleQuestion, dialogDescription, MessageType.Warning);
          var customButton = confirmDialog.Buttons.AddCustom(AccountingDocumentBases.Resources.FillingButtonText);
          confirmDialog.Buttons.Default = customButton;
          confirmDialog.Buttons.AddCancel();
          
          if (confirmDialog.Show() == DialogButtons.Cancel)
            return;
        }

        Functions.AccountingDocumentBase.BuyerTitlePropertiesFillingDialog(_obj);
      }
    }

    public virtual bool CanFillBuyerInfo(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsFormalized == true && _obj.AccessRights.CanUpdate() && !_obj.State.IsInserted && _obj.SellerSignatureId != null;
    }
    
    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromTemplate(e) && _obj.IsFormalized != true;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromScanner(e) && _obj.IsFormalized != true;
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromFile(e) && _obj.IsFormalized != true;
    }

    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanScanInNewVersion(e) && _obj.IsFormalized != true;
    }

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanImportInLastVersion(e) && _obj.IsFormalized != true;
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanImportInNewVersion(e) && _obj.IsFormalized != true;
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateVersionFromLastVersion(e) && _obj.IsFormalized != true;
    }

    public override void ShowRegistrationPane(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowRegistrationPane(e);
    }

    public override bool CanShowRegistrationPane(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }

}
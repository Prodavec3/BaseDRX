using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FormalizedPowerOfAttorney;

namespace Sungero.Docflow.Client
{
  partial class FormalizedPowerOfAttorneyCollectionActions
  {
    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

  }

  partial class FormalizedPowerOfAttorneyVersionsActions
  {
    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override void CreateDocumentFromVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateDocumentFromVersion(e);
    }

    public override bool CanCreateDocumentFromVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void CreateVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CreateVersion(e);
    }

    public override bool CanCreateVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

  }

  partial class FormalizedPowerOfAttorneyActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.FormalizedPowerOfAttorney.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(FormalizedPowerOfAttorneys.Resources.DuplicatesNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public override void DeliverDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeliverDocument(e);
    }

    public override bool CanDeliverDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override void CreateVersionFromLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateVersionFromLastVersion(e);
    }

    public override bool CanCreateVersionFromLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ScanInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ScanInNewVersion(e);
    }

    public override bool CanScanInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public virtual bool CanImportVersionWithSignature(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (e.Params.Contains(Constants.FormalizedPowerOfAttorney.HideImportParamName))
        return false;
      
      return FormalizedPowerOfAttorneys.AccessRights.CanCreate() &&
        FormalizedPowerOfAttorneys.AccessRights.CanApprove();
    }

    public virtual void ImportVersionWithSignature(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Чтобы при добавлении версии пустое Содержание не заполнялось, добавить признак того, что версия создается из шаблона.
      e.Params.Add(Constants.Module.CreateFromTemplate, true);
      
      if (Functions.FormalizedPowerOfAttorney.ShowImportVersionWithSignatureDialog(_obj) == true &&
          _obj.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered)
      {
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, true);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.NeedValidateRegisterFormat, true);
        e.Params.AddOrUpdate(Constants.FormalizedPowerOfAttorney.HideImportParamName, true);
      }
      
      e.Params.Remove(Constants.Module.CreateFromTemplate);
    }
  }

}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ExchangeDocument;

namespace Sungero.Docflow.Client
{
  internal static class ExchangeDocumentStaticActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public static void ShowDocumentReturn(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      
    }

    public static bool CanShowDocumentReturn(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentTrackingActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void ShowReturnAssignments(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ShowReturnAssignments(e);
    }

    public override bool CanShowReturnAssignments(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentAnyChildEntityCollectionActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentAnyChildEntityActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentCollectionActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void Sign(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Sign(e);
    }

    public override bool CanSign(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void SendByMail(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendByMail(e);
    }

    public override bool CanSendByMail(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void OpenDocumentEdit(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.OpenDocumentEdit(e);
    }

    public override bool CanOpenDocumentEdit(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void AddToMyFolder(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.AddToMyFolder(e);
    }

    public override bool CanAddToMyFolder(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void AddToFavoritesFolder(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.AddToFavoritesFolder(e);
    }

    public override bool CanAddToFavoritesFolder(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentVersionsActions
  {
    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void DeleteVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteVersion(e);
    }

    public override bool CanDeleteVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
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

    public override void SignVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.SignVersion(e);
    }

    public override bool CanSignVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void ImportVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.ImportVersion(e);
    }

    public override bool CanImportVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }

    public override void EditVersion(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.EditVersion(e);
    }

    public override bool CanEditVersion(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return false;
    }
    #endregion
  }

  partial class ExchangeDocumentActions
  {

    public override void ApprovalForm(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ApprovalForm(e);
    }

    public override bool CanApprovalForm(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    // Dmitriev_IA: Закрытие доступности действий нужно для корректного отображения риббона в списках/папках разнотипного содержимого.
    #region Закрытие доступности действий
    public override void CreateAddendum(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateAddendum(e);
    }

    public override bool CanCreateAddendum(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void SendForReview(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendForReview(e);
    }

    public override bool CanSendForReview(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void SendForFreeApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendForFreeApproval(e);
    }

    public override bool CanSendForFreeApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ReturnFromCounterparty(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ReturnFromCounterparty(e);
    }

    public override bool CanReturnFromCounterparty(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ReturnDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ReturnDocument(e);
    }

    public override bool CanReturnDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void DeliverDocument(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeliverDocument(e);
    }

    public override bool CanDeliverDocument(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ShowRegistrationPane(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowRegistrationPane(e);
    }

    public override bool CanShowRegistrationPane(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CancelRegistration(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CancelRegistration(e);
    }

    public override bool CanCancelRegistration(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void AssignNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.AssignNumber(e);
    }

    public override bool CanAssignNumber(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ChangeRequisites(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeRequisites(e);
    }

    public override bool CanChangeRequisites(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Register(e);
    }

    public override bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void SendActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendActionItem(e);
    }

    public override bool CanSendActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendForApproval(e);
    }

    public override bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
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

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
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

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return false;
    }
    #endregion
  }

}
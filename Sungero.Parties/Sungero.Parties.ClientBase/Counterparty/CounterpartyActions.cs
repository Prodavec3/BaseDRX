using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties.Client
{
  partial class CounterpartyAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      // Дизейбл грида абонентских ящиков.
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes) 
        ? false 
        : base.CanDeleteChildEntity(e);
    }

  }

  partial class CounterpartyAnyChildEntityActions
  {
    public override void CopyChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.CopyChildEntity(e);
    }

    public override bool CanCopyChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes) 
        ? false 
        : base.CanCopyChildEntity(e);      
    }

    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      var root = Counterparties.As(e.RootEntity);
      return (root != null && _all == root.ExchangeBoxes) 
        ? false 
        : base.CanAddChildEntity(e);
    }

  }

  partial class CounterpartyExchangeBoxesActions
  {

    public virtual bool CanAcceptInvitation(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return (_obj.Status == CounterpartyExchangeBoxes.Status.ApprovingByUs || _obj.Status == CounterpartyExchangeBoxes.Status.Closed) &&
        _obj.Counterparty.AccessRights.CanUpdate() && _obj.Counterparty.AccessRights.CanSetExchange();
    }

    public virtual void AcceptInvitation(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.Counterparty.ShowInvitationDialog(_obj, true);
    }

    public virtual bool CanRejectInvitation(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.Status != CounterpartyExchangeBoxes.Status.Closed && 
        _obj.Counterparty.AccessRights.CanUpdate() && _obj.Counterparty.AccessRights.CanSetExchange();
    }

    public virtual void RejectInvitation(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.Counterparty.ShowInvitationDialog(_obj, false);
    }
  }

  partial class CounterpartyActions
  {
    public virtual void ForceDuplicateSave(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate(Counterparties.Resources.ParameterIsForceDuplicateSaveFormat(_obj.Id), true);
      _obj.Save();
    }

    public virtual bool CanForceDuplicateSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowCounterpartyDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documents = Docflow.PublicFunctions.Module.Remote.GetCounterpartyDocuments(_obj);
      documents.Show(_obj.Name);
    }

    public virtual bool CanShowCounterpartyDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.Counterparty.GetDuplicates(_obj, true);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Parties.Counterparties.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CanExchange(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var info = Functions.Counterparty.ValidateExchangeAction(_obj, e);
      if (!info.CanDoAction)
        return;
      
      var names = string.Empty;
      if (info.Services.Count < 3)
        names = string.Join(Counterparties.Resources.ExchangeServicesSeparator, info.Services.Select(x => x.Info.Properties.ExchangeProvider.GetLocalizedValue(x.ExchangeProvider)));
      else
      {
        var exchangeServices = info.Services.GetRange(0, 2).Select(x => x.Info.Properties.ExchangeProvider.GetLocalizedValue(x.ExchangeProvider));
        var exchangeServicesNames = string.Join(Counterparties.Resources.ExchangeServicesSeparator, exchangeServices);       
        names = Counterparties.Resources.ToManyExchangeServicesFormat(exchangeServices);
      }
      
      var dialog = Dialogs.CreateTaskDialog(Counterparties.Resources.FoundInExchangeServices,
                                            Counterparties.Resources.FoundInExchangeServicesDescriptionFormat(names),
                                            MessageType.Information, _obj.Info.Actions.CanExchange.LocalizedName);
      var send = dialog.Buttons.AddCustom(_obj.Info.Actions.SendInvitation.LocalizedName);
      dialog.Buttons.Default = send;
      dialog.Buttons.AddCancel();
      if (dialog.Show() == send)
      {
        this.SendInvitation(e);
      }
    }

    public virtual bool CanCanExchange(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return this.CanSendInvitation(e);
    }

    public virtual void SendInvitation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var info = Functions.Counterparty.ValidateExchangeAction(_obj, e);
      if (!info.CanDoAction)
        return;
      
      var dialog = Dialogs.CreateInputDialog(Counterparties.Resources.InvitationTitle);
      var box = dialog.AddSelect(_obj.Info.Properties.ExchangeBoxes.Properties.Box.LocalizedName, true, info.DefaultBox).From(info.Boxes);
      var comment = dialog.AddMultilineString(Counterparties.Resources.InvitationMessageHeader, false,
                                              Counterparties.Resources.InvitationMessageDefault);

      dialog.HelpCode = Constants.Counterparty.HelpCodes.SendInvitation;
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      dialog.SetOnButtonClick(x =>
                              {
                                if (x.Button == DialogButtons.Ok && x.IsValid && e.Validate())
                                {
                                  var result = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.SendInvitation(box.Value, _obj, comment.Value);
                                  if (!string.IsNullOrWhiteSpace(result))
                                    x.AddError(result);
                                  else
                                  {
                                    var counterpartyExchangeBox = _obj.ExchangeBoxes.FirstOrDefault(b => Equals(b.Box, box.Value));
                                    Dialogs.NotifyMessage(counterpartyExchangeBox.Info.Properties.Status.GetLocalizedValue(counterpartyExchangeBox.Status));
                                  }
                                }
                              });
      dialog.Show();
    }

    public virtual bool CanSendInvitation(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.AccessRights.CanSetExchange();
    }

    public virtual void SearchDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Sungero.Shell.PublicFunctions.Module.SearchDocumentsWithCounterparties(_obj);
    }

    public virtual bool CanSearchDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void WriteLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.WriteLetter(_obj.Email);
    }

    public virtual bool CanWriteLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual bool CanGoToWebsite(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.Module.CanGoToWebsite(_obj.Homepage);
    }

    public virtual void GoToWebsite(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.GoToWebsite(_obj.Homepage, e);
    }

  }

}
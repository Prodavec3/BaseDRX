using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Counterparty;

namespace Sungero.Parties.Client
{
  partial class CounterpartyFunctions
  {
    /// <summary>
    /// Показать диалог для обработки приглашения из сервиса обмена.
    /// </summary>
    /// <param name="obj">Элемент коллекции ящиков в КА.</param>
    /// <param name="accept">True, если надо принять инвайт. False, если отказать.</param>
    public static void ShowInvitationDialog(ICounterpartyExchangeBoxes obj, bool accept)
    {
      var dialogHeader = string.Empty;
      var helpCode = Constants.Counterparty.HelpCodes.WhenApprovingByUs;
      if (accept)
      {
        if (obj.Status == CounterpartyExchangeBoxes.Status.Closed)
        {
          dialogHeader = Counterparties.Resources.InvitationHeaderRestore;
          helpCode = Constants.Counterparty.HelpCodes.RestoreExchange;
        }
        else
          dialogHeader = Counterparties.Resources.InvitationHeaderAccept;
      }
      else
      {
        if (obj.Status == CounterpartyExchangeBoxes.Status.ApprovingByUs)
          dialogHeader = Counterparties.Resources.InvitationHeaderReject;
        else if (obj.Status == CounterpartyExchangeBoxes.Status.ApprovingByCA)
        {
          dialogHeader = Counterparties.Resources.InvitationHeaderRevoke;
          helpCode = Constants.Counterparty.HelpCodes.RejectExchange;
        }
        else
        {
          dialogHeader = Counterparties.Resources.InvitationHeaderTermination;
          helpCode = Constants.Counterparty.HelpCodes.RejectExchange;
        }
      }
      
      var dialog = Dialogs.CreateInputDialog(dialogHeader);
      dialog.HelpCode = helpCode;
      dialog.AddSelect(obj.Info.Properties.Box.LocalizedName, true, obj.Box).IsEnabled = false;
      var comment = dialog.AddMultilineString(Counterparties.Resources.InvitationMessageHeader, false);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
      {
        var result = accept ?
          ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.AcceptInvitation(obj.Box, obj.Counterparty, obj.OrganizationId, comment.Value) :
          ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.RejectInvitation(obj.Box, obj.Counterparty, obj.OrganizationId, comment.Value);
        
        if (string.IsNullOrWhiteSpace(result))
        {
          var counterpartyExchangeBox = obj.Counterparty.ExchangeBoxes.Single(b => b.Id == obj.Id);
          Dialogs.NotifyMessage(obj.Info.Properties.Status.GetLocalizedValue(counterpartyExchangeBox.Status));
        }
        else
          Dialogs.NotifyMessage(result);
      }
    }
    
    /// <summary>
    /// Провалидировать доступность действия отправки приглашения \ проверки доступности в сервисе.
    /// </summary>
    /// <param name="args">Аргументы действия, из которого идёт валидация.</param>
    /// <returns>True, если действие можно выполнять.</returns>
    public virtual bool ValidateTinTrrcBeforeExchange(Sungero.Domain.Client.ExecuteActionArgs args)
    {
      var based = args.Validate();
      if (string.IsNullOrWhiteSpace(_obj.TIN))
      {
        if (args.Action.Name == _obj.Info.Actions.SendInvitation.Name)
        {
          args.AddError(Counterparties.Resources.NeedFillTinForSendInvitation);
          based = false;
        }
        if (args.Action.Name == _obj.Info.Actions.CanExchange.Name)
        {
          args.AddError(Counterparties.Resources.NeedFillTinForCanExchange);
          based = false;
        }
      }
      return based;
    }
    
    /// <summary>
    /// Проверки перед отправкой приглашения и проверкой доступности КА в сервисах.
    /// </summary>
    /// <param name="args">Аргументы действия.</param>
    /// <returns>Возвращает всю полученную информацию.</returns>
    /// <remarks>Только когда свойство CanDoAction = true, можно выполнять действие.</remarks>
    public virtual Structures.Counterparty.SendInvitation ValidateExchangeAction(Sungero.Domain.Client.ExecuteActionArgs args)
    {
      if (!Functions.Counterparty.ValidateTinTrrcBeforeExchange(_obj, args))
      {
        var empty = Structures.Counterparty.SendInvitation.Create();
        empty.CanDoAction = false;
        return empty;
      }
      
      var info = Structures.Counterparty.SendInvitation.Create();
      info.CanDoAction = false;
      
      try
      {
        info = Functions.Counterparty.Remote.InvitationBoxes(_obj);
      }
      catch (Exception ex)
      {
        Dialogs.NotifyMessage(ex.Message);
        return info;
      }
      
      // Нет валидных ящиков.
      if (!info.HaveAnyBoxes)
      {
        Dialogs.NotifyMessage(Counterparties.Resources.InvitationBoxesNotFound);
        return info;
      }
      
      // Контрагент не найден в сервисах или уже установлен обмен с дублем.
      if (!info.CanSendInivtationFromAnyService)
      {
        if (info.HaveDoubleCounterparty)
          Dialogs.NotifyMessage(Counterparties.Resources.HaveDoubleCounterparties);
        else
        {
          // Предложить пригласить контрагента по email.
          var dialog = Dialogs.CreateInputDialog(Resources.WizardTitle);
          dialog.Text = Counterparties.Resources.NotFoundInExchangeServices;
          
          var boxParameter = dialog.AddSelect(Resources.WizardFrom, true, ExchangeCore.BusinessUnitBoxes.Null);
          var availableBoxes = Functions.Module.GetAvailableBoxes(null, ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToArray());
          boxParameter.Value = availableBoxes.FirstOrDefault();
          boxParameter.From(availableBoxes);
          
          // Кнопки.
          var inviteByEmailButton = dialog.Buttons.AddCustom(Resources.WizardInviteByEmail);
          dialog.Buttons.Default = inviteByEmailButton;
          var cancelButton = dialog.Buttons.AddCustom(Resources.WizardCancel);
          
          dialog.SetOnButtonClick(
            (dialogArgs) =>
            {
              if (Equals(dialogArgs.Button, inviteByEmailButton))
              {
                Functions.Module.CreateInvitationEmail(boxParameter.Value, _obj.Email);
              }
              dialogArgs.CloseAfterExecute = true;
            });
          dialog.Show();
        }
        
        return info;
      }
      
      // Соединения установлены со всеми рабочими ящиками, в т.ч. и с дублями.
      if (!info.HaveAllowedBoxes)
      {
        if (info.HaveDoubleCounterparty)
          Dialogs.NotifyMessage(Counterparties.Resources.InvitationBoxesAndDoubleNotAllowed);
        else
          Dialogs.NotifyMessage(Counterparties.Resources.InvitationBoxesNotAllowed);
        return info;
      }
      
      return info;
    }
  }
}
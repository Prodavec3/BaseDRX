using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Client
{
  public class ModuleFunctions
  {   
    #region Сайт
    
    /// <summary>
    /// Перейти на сайт.
    /// </summary>
    /// <param name="website">Адрес сайта.</param>
    /// <param name="e">Аргумент события.</param>
    public static void GoToWebsite(string website, Sungero.Domain.Client.ExecuteActionArgs e)
    {
      website = website.ToLower();
      if (!(website.StartsWith("http://") || website.StartsWith("https://")))
      {
        website = "http://" + website;
      }
      
      try
      {
        Hyperlinks.Open(website);
      }
      catch
      {
        e.AddError(Resources.WrongWebsite);
      }
    }

    /// <summary>
    /// Проверить возможность перейти на сайт.
    /// </summary>
    /// <param name="website">Адрес сайта.</param>
    /// <returns>Возможность перейти на сайт.</returns>
    public static bool CanGoToWebsite(string website)
    {
      return !string.IsNullOrWhiteSpace(website);
    }
    
    #endregion

    #region Email

    /// <summary>
    /// Написать письмо.
    /// </summary>
    /// <param name="email">Email.</param>
    public virtual void WriteLetter(string email)
    {
      Hyperlinks.Open("mailto:" + email);
    }

    #endregion
    
    #region Обложка
    
    /// <summary>
    /// Создать новую организацию.
    /// </summary>
    public virtual void CreateCompany()
    {
      Functions.Module.Remote.CreateCompany().Show();
    }

    /// <summary>
    /// Создать новое контактное лицо.
    /// </summary>
    public virtual void CreateContact()
    {
      Functions.Module.Remote.CreateContact().Show();
    }

    /// <summary>
    /// Создать новую персону.
    /// </summary>
    public virtual void CreatePerson()
    {
      Functions.Module.Remote.CreatePerson().Show();
    }
    
    /// <summary>
    /// Показать контрагентов, с которыми возможен обмен документами через сервис обмена.
    /// </summary>
    /// <returns>Список контрагентов.</returns>
    public virtual IQueryable<ICounterparty> ShowCounterpartiesAvailableForExchange()
    {
      var counterpaties = Functions.Module.Remote.CounterpartiesAvailableForExchange();
      return counterpaties;
    }
    
    #endregion
    
    #region Мастер действий "Поиск по ИНН"
    
    /// <summary>
    /// Пригласить контрагента к обмену.
    /// </summary>
    public virtual void InviteCounterpartyToExchange()
    {
      #region Проверка возможности запуска
      
      var connectedBoxes = GetAvailableBoxes(null, ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToArray());
      if (!connectedBoxes.Any())
      {
        Dialogs.ShowMessage(Counterparties.Resources.InvitationBoxesNotFound,
                            Parties.Resources.ContactAdministrator,
                            MessageType.Error);
        return;
      }
      
      if (!Counterparties.AccessRights.CanUpdate() || !Counterparties.AccessRights.CanSetExchange())
      {
        Dialogs.ShowMessage(Parties.Resources.NeedToBeIncludedInRole,
                            Parties.Resources.ContactAdministrator,
                            MessageType.Error);
        return;
      }
      
      #endregion
      
      Logger.DebugFormat("Start invitation counterparty by TIN.");
      
      var tin = string.Empty;
      var trrc = string.Empty;
      var filteredByExchangeServiceBoxes = connectedBoxes.ToArray();
      var foundFromExchangeServiceCompanies = new List<Structures.Module.CounterpartyFromExchangeService>();
      Structures.Module.CounterpartyFromExchangeService selectedCompany = null;
      
      // Диалог.
      var dialog = Dialogs.CreateInputDialog(Resources.WizardTitle);
      dialog.HelpCode = Constants.Module.HelpCodes.CounterpartyInvitation;
      dialog.Text = Resources.WizardTinInputStepDescription;
      dialog.Height = ClientApplication.ApplicationType == ApplicationType.Desktop ? 180 : 140;
      
      // Параметры.
      var tinParameter = dialog.AddString(Resources.TIN, true);
      var trrcParameter = dialog.AddString(Resources.TRRC, false);
      var exchangeServices = connectedBoxes.Select(b => b.ExchangeService).Distinct().ToArray();
      var exchangeServiceParameter = dialog.AddSelectMany(Resources.WizardExchangeServices, true, exchangeServices)
        .From(exchangeServices);
      var counterpartyParameter = dialog.AddSelect(Resources.Counterparty, true);
      var boxParameter = dialog.AddSelect(Resources.WizardFrom, true, ExchangeCore.BusinessUnitBoxes.Null);
      
      // Кнопки.
      var backButton = dialog.Buttons.AddCustom(Resources.WizardBack);
      var nextButton = dialog.Buttons.AddCustom(Resources.WizardNext);
      var openCardButton = dialog.Buttons.AddCustom(Resources.OpenCard);
      var inviteByEmailButton = dialog.Buttons.AddCustom(Resources.WizardInviteByEmail);
      var inviteButton = dialog.Buttons.AddCustom(Resources.WizardInvite);
      var completedButton = dialog.Buttons.AddCustom(Resources.WizardCancel);
      
      // Отключить валидацию заполненности параметров для "Назад" и "Готово/Отмена".
      backButton.ValidateOnClick = false;
      completedButton.ValidateOnClick = false;
      
      #region Этап, видимость полей и кнопок при открытии
      
      // Определить этап при открытии.
      var stepNumber = 1;
      var counterpartyFoundByTin = false;
      var counterpartyHasExchange = false;
      var isTinInputStep = true;
      var isCounterpartySelectStep = false;
      var isMailInviteStep = false;
      var isStatusStep = false;
      var isInviteResultStep = false;
      
      // Поля.
      tinParameter.IsVisible = true;
      trrcParameter.IsVisible = true;
      exchangeServiceParameter.IsVisible = true;
      counterpartyParameter.IsVisible = false;
      boxParameter.IsVisible = false;
      
      // Кнопки.
      backButton.IsVisible = false;
      nextButton.IsVisible = true;
      openCardButton.IsVisible = false;
      inviteByEmailButton.IsVisible = false;
      inviteButton.IsVisible = false;
      completedButton.IsVisible = true;
      
      // Определить кнопку по умолчанию.
      if (isMailInviteStep)
        dialog.Buttons.Default = inviteByEmailButton;
      else if (isCounterpartySelectStep)
        dialog.Buttons.Default = inviteButton;
      else if (isStatusStep || isInviteResultStep)
        dialog.Buttons.Default = completedButton;
      else
        dialog.Buttons.Default = nextButton;
      
      #endregion
      
      dialog.SetOnButtonClick(
        (args) =>
        {
          #region Далее
          
          if (Equals(args.Button, nextButton))
          {
            // Проверить заполненность обязательных параметров.
            if (string.IsNullOrWhiteSpace(tinParameter.Value))
              return;
            if (!exchangeServiceParameter.Value.Any())
              return;
            
            // Проверить валидность ИНН.
            var result = Functions.Counterparty.CheckTin(tin, true);
            if (!string.IsNullOrEmpty(result))
            {
              args.AddError(result, tinParameter);
              return;
            }
            
            if (!string.IsNullOrEmpty(result))
            {
              args.AddError(result, tinParameter);
              return;
            }
            
            try
            {
              // Поиск контрагентов.
              foundFromExchangeServiceCompanies = Functions.Module.Remote.FindCompanyInExchangeServices(tin, trrc, filteredByExchangeServiceBoxes.ToList());
            }
            catch (Exception ex)
            {
              Dialogs.ShowMessage(ex.Message,
                                  Parties.Resources.ContactAdministrator,
                                  MessageType.Error);
              Logger.ErrorFormat("Error on find company in exchange services.", ex);
              return;
            }
            
            Logger.DebugFormat("Found from exchange service companies: {0}.", foundFromExchangeServiceCompanies.Count());
            
            // Задать список выбора контрагента.
            var counterpartiesFromString = foundFromExchangeServiceCompanies
              .Select(a => ConvertCounterpartyToString(a)).Distinct().ToArray();
            counterpartyParameter.From(counterpartiesFromString);
            
            // Очистить текущее значение компании, если её нет в списке.
            if (!counterpartiesFromString.Contains(counterpartyParameter.Value))
              counterpartyParameter.Value = string.Empty;
            
            // Если есть подходящие, то шаг отправки приглашения через СО, иначе шаг приглашения по почте.
            if (foundFromExchangeServiceCompanies.Any())
            {
              counterpartyFoundByTin = true;
              
              // Если компании нашлись, установить первую из найденных в поле "Контрагент".
              selectedCompany = foundFromExchangeServiceCompanies.FirstOrDefault();
              counterpartyParameter.Value = ConvertCounterpartyToString(selectedCompany);
              
              // Если компания одна, проверить, установлен ли с ней обмен.
              if (foundFromExchangeServiceCompanies.Count() == 1)
              {
                counterpartyHasExchange = selectedCompany.ExchangeStatus != null;
                
                // Принудительно запустить синхронизацию контрагентов, если контрагента ещё не найдено.
                if (selectedCompany.Counterparty == null)
                  ExchangeCore.PublicFunctions.Module.Remote.RequeueCounterpartySync();
              }
            }
            else
            {
              // Переход к приглашению по почте.
              counterpartyFoundByTin = false;
              
              // Очистить компанию.
              selectedCompany = null;
              counterpartyParameter.Value = string.Empty;
            }
            
            stepNumber++;
          }
          
          #endregion
          
          #region Приглашение через сервис обмена
          
          if (Equals(args.Button, inviteButton))
          {
            // Проверить заполненность обязательных параметров.
            if (string.IsNullOrWhiteSpace(counterpartyParameter.Value) || boxParameter.Value == null)
              return;
            
            selectedCompany = this.ConvertStringToCounterparty(foundFromExchangeServiceCompanies, counterpartyParameter.Value, boxParameter.Value);
            counterpartyHasExchange = selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Active ||
              selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA;
            if (!counterpartyHasExchange)
            {
              var counterpartyName = string.Empty;
              if (string.IsNullOrEmpty(selectedCompany.TRRC))
                counterpartyName = Resources.WizardCounterpartyFullNameShortFormat(selectedCompany.Name, selectedCompany.TIN);
              else
                counterpartyName = Resources.WizardCounterpartyFullNameFormat(selectedCompany.Name, selectedCompany.TIN, selectedCompany.TRRC);
              
              // Пригласить контрагента.
              var result = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.SendInvitation(boxParameter.Value, selectedCompany.OrganizationId, counterpartyName, string.Empty);
              if (!string.IsNullOrWhiteSpace(result))
              {
                Dialogs.ShowMessage(result, MessageType.Error);
                return;
              }
            }
            
            // Попробовать найти контрагента в RX, вдруг синхронизация прошла.
            selectedCompany.Counterparty = Functions.Module.Remote.FindCounterpartyByOrganizationId(selectedCompany.Box, selectedCompany.OrganizationId);
            
            ExchangeCore.PublicFunctions.Module.Remote.RequeueCounterpartySync();
            
            stepNumber++;
          }
          
          #endregion
          
          #region Приглашение по почте
          
          if (Equals(args.Button, inviteByEmailButton))
          {
            var email = Functions.Module.Remote.GetEmailByTinTrrc(tin, trrc);
            CreateInvitationEmail(boxParameter.Value, email);
            Logger.DebugFormat("Invitation by email.");
          }
          
          #endregion
          
          var haveCounterpartyInRX = selectedCompany != null && selectedCompany.Counterparty != null;
          if (Equals(args.Button, openCardButton))
          {
            // Попробовать найти контрагента в RX, вдруг синхронизация прошла.
            selectedCompany.Counterparty = Functions.Module.Remote.FindCounterpartyByOrganizationId(selectedCompany.Box, selectedCompany.OrganizationId);
            haveCounterpartyInRX = selectedCompany.Counterparty != null;
            
            // Показать карточку контрагента.
            if (haveCounterpartyInRX)
              ShowCounterpartyCard(selectedCompany);
            else
              // Вывести сообщение о необходимости ожидания создания контрагента.
              Dialogs.ShowMessage(Resources.WizardSyncronizationInProcess);
          }
          
          if (Equals(args.Button, backButton))
          {
            stepNumber--;
            if (isStatusStep)
            {
              selectedCompany = null;
              counterpartyHasExchange = false;
            }
          }
          
          #region Этап, видимость полей и кнопок
          
          // Определить этап.
          isTinInputStep = stepNumber == 1;
          isCounterpartySelectStep = stepNumber == 2 && counterpartyFoundByTin && !counterpartyHasExchange;
          isMailInviteStep = stepNumber == 2 && !counterpartyFoundByTin;
          isStatusStep = (stepNumber == 2 || stepNumber == 3) && counterpartyHasExchange;
          isInviteResultStep = stepNumber == 3 && !counterpartyHasExchange;
          
          // Поля.
          tinParameter.IsVisible = isTinInputStep;
          trrcParameter.IsVisible = isTinInputStep;
          exchangeServiceParameter.IsVisible = isTinInputStep;
          counterpartyParameter.IsVisible = isCounterpartySelectStep;
          boxParameter.IsVisible = isMailInviteStep || isCounterpartySelectStep;
          
          // Кнопки.
          backButton.IsVisible = isMailInviteStep || isCounterpartySelectStep || isStatusStep;
          nextButton.IsVisible = isTinInputStep;
          openCardButton.IsVisible = (isStatusStep || isInviteResultStep) || isMailInviteStep &&
            selectedCompany != null && selectedCompany.Counterparty != null;
          inviteByEmailButton.IsVisible = isMailInviteStep;
          inviteButton.IsVisible = isCounterpartySelectStep;
          
          // Определить кнопку по умолчанию.
          if (isMailInviteStep)
            dialog.Buttons.Default = inviteByEmailButton;
          else if (isCounterpartySelectStep)
            dialog.Buttons.Default = inviteButton;
          else if (isStatusStep || isInviteResultStep)
            dialog.Buttons.Default = completedButton;
          else
            dialog.Buttons.Default = nextButton;
          
          #endregion
          
          #region Инструкция
          
          var dialogText = string.Empty;
          if (isTinInputStep)
            dialogText = Resources.WizardTinInputStepDescription;
          
          if (isCounterpartySelectStep)
          {
            var counterpartyCount = foundFromExchangeServiceCompanies
              .Select(a => ConvertCounterpartyToString(a)).Distinct().Count();
            
            // Если контрагент один, добавить отступ.
            dialogText = counterpartyCount == 1 ?
              dialogText = Resources.WizardCounterpartySelectedStepOneDescription :
              dialogText = Resources.WizardCounterpartySelectedStepDescriptionFormat(counterpartyCount);
          }
          
          if (isMailInviteStep)
            dialogText = string.IsNullOrWhiteSpace(trrc) ?
              Resources.WizardMailInviteStepDescriptionShortFormat(tin) :
              Resources.WizardMailInviteStepDescriptionFormat(tin, trrc);
          
          if (isStatusStep)
          {
            // Добавить 2 пустых строки для "центрирования" текста.
            dialogText = "\n\n";
            
            if (string.IsNullOrWhiteSpace(counterpartyParameter.Value))
              dialogText += Resources.WizardStatusStepDescriptionShort;
            else if (selectedCompany.ExchangeStatus != null)
            {
              // Заголовок.
              if (selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Active)
                dialogText += Resources.WizardStatusStepActiveDescription;
              if (selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.Closed)
                dialogText += Resources.WizardStatusStepBlockedDescription;
              if (selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByCA)
                dialogText += Resources.WizardStatusStepApprovingByCADescription;
              if (selectedCompany.ExchangeStatus == Parties.CounterpartyExchangeBoxes.Status.ApprovingByUs)
                dialogText += Resources.WizardStatusStepApprovingByUsDescription;
              
              // Информация по контрагенту.
              if (string.IsNullOrWhiteSpace(selectedCompany.TRRC))
                dialogText += Resources.WizardCounterpartyInfoShortFormat(selectedCompany.Name, selectedCompany.TIN);
              else
                dialogText += Resources.WizardCounterpartyInfoFormat(selectedCompany.Name, selectedCompany.TIN, selectedCompany.TRRC);
              
              // Информация по нашим НОР и сервисам.
              var boxesInfo = string.Empty;
              var boxesInfos = foundFromExchangeServiceCompanies
                .Where(c => c.TIN == selectedCompany.TIN && c.TRRC == selectedCompany.TRRC && c.ExchangeStatus != null && c.OrganizationId == selectedCompany.OrganizationId)
                .Select(c => Resources.WizardBusinessUnitInfoFormat(c.Box.BusinessUnit.Name, c.Box.ExchangeService.Name,
                                                                    Parties.Counterparties.Info.Properties.ExchangeBoxes.Properties.Status.GetLocalizedValue(c.ExchangeStatus)));
              boxesInfo = string.Join(";\n", boxesInfos);
              
              dialogText += "\n" + Resources.WizardFromMany + "\n" + boxesInfo + ".";
            }
          }
          
          if (isInviteResultStep)
          {
            // Добавить 2 пустых строки для "центрирования" текста.
            dialogText = "\n\n";
            
            if (string.IsNullOrWhiteSpace(counterpartyParameter.Value))
              dialogText += Resources.WizardInviteResultStepDecriptionShort;
            else
            {
              dialogText += Resources.WizardInviteResultStepDecriptionFormat(selectedCompany.Name, selectedCompany.TIN, selectedCompany.TRRC,
                                                                             selectedCompany.Box.BusinessUnit.Name, selectedCompany.Box.ExchangeService.Name);
            }
          }
          
          dialog.Text = dialogText;
          
          #endregion
          
          // Название кнопки "Готово/Отменить".
          completedButton.Name = isStatusStep || isInviteResultStep ? Resources.WizardCompleted : Resources.WizardCancel;
          
          // Не закрывать, если нажали "Назад" "Далее" "Пригласить".
          args.CloseAfterExecute = haveCounterpartyInRX && Equals(args.Button, openCardButton) ||
            Equals(args.Button, inviteByEmailButton) || Equals(args.Button, completedButton);
        });
      
      #region События
      
      // Прокинуть параметры для сохранения значений при перестроении диалогов.
      tinParameter.SetOnValueChanged(
        (args) =>
        {
          tin = tinParameter.Value;
        });
      
      trrcParameter.SetOnValueChanged(
        (args) =>
        {
          trrc = trrcParameter.Value;
        });
      
      exchangeServiceParameter.SetOnValueChanged(
        (args) =>
        {
          // Отфильтровать список наших а/я по выбранным сервисам обмена.
          var selectedServices = exchangeServiceParameter.Value;
          filteredByExchangeServiceBoxes = connectedBoxes.Where(b => selectedServices.Contains(b.ExchangeService)).ToArray();
        });
      
      counterpartyParameter.SetOnValueChanged(
        (args) =>
        {
          if (string.IsNullOrWhiteSpace(args.NewValue))
            selectedCompany = null;
          else
            selectedCompany = this.ConvertStringToCounterparty(foundFromExchangeServiceCompanies, args.NewValue);
          
          // Обновить список доступных ящиков по выбранному контрагенту.
          var availableBoxes = GetAvailableBoxes(selectedCompany, filteredByExchangeServiceBoxes);
          boxParameter.Value = availableBoxes.FirstOrDefault();
          boxParameter.From(availableBoxes);
        });

      #endregion
      
      var dialogResult = dialog.Show();
    }
    
    /// <summary>
    /// Сформировать текстовое представление информации о контрагенте системы обмена,
    /// полученной из сервиса обмена.
    /// </summary>
    /// <param name="counterparty">Информация о контрагенте системы обмена.</param>
    /// <returns>Текстовая информация о контрагенте системы обмена.</returns>
    public static string ConvertCounterpartyToString(Structures.Module.CounterpartyFromExchangeService counterparty)
    {
      if (string.IsNullOrWhiteSpace(counterparty.TRRC))
        return Resources.WizardCounterpartyViewShortFormat(counterparty.Name, counterparty.TIN, counterparty.Box.ExchangeService.Name);
      
      return Resources.WizardCounterpartyViewFormat(counterparty.Name, counterparty.TIN, counterparty.TRRC,
                                                    counterparty.Box.ExchangeService.Name);
    }

    /// <summary>
    /// Выбрать ту информацию о контрагенте системы обмена,
    /// полученную ранее из сервиса обмена,
    /// которая соответствует заданному описанию контрагента.
    /// </summary>
    /// <param name="counterparties">Информация о контрагентах систем обмена.</param>
    /// <param name="counterpartyString">Описание контрагента системы обмена.</param>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <returns>Информация о контрагенте, полученная из сервиса обмена.</returns>    
    public virtual Structures.Module.CounterpartyFromExchangeService ConvertStringToCounterparty(List<Structures.Module.CounterpartyFromExchangeService> counterparties,
                                                                                                 string counterpartyString,
                                                                                                 ExchangeCore.IBusinessUnitBox box = null)
    {
      if (box == null)
        return counterparties.FirstOrDefault(a => ConvertCounterpartyToString(a) == counterpartyString);
      else
        return counterparties.FirstOrDefault(a => ConvertCounterpartyToString(a) == counterpartyString && Equals(a.Box, box));
    }
    
    /// <summary>
    /// Показать карточку контрагента.
    /// </summary>
    /// <param name="counterparty">Информация о контрагенте, полученная из сервиса обмена.</param>
    public static void ShowCounterpartyCard(Structures.Module.CounterpartyFromExchangeService counterparty)
    {
      if (counterparty == null || counterparty.Counterparty == null)
        return;
      
      counterparty.Counterparty.Show();
    }
    
    /// <summary>
    /// Получить подходящие для приглашения контрагента ящики.
    /// </summary>
    /// <param name="foundCompany">Найденный контрагент.</param>
    /// <param name="connectedBoxes">Доступные ящики для сервисов обмена.</param>
    /// <returns>Отсортированный массив подходящих ящиков из всех доступных.</returns>
    public static Sungero.ExchangeCore.IBusinessUnitBox[] GetAvailableBoxes(Structures.Module.CounterpartyFromExchangeService foundCompany,
                                                                            Sungero.ExchangeCore.IBusinessUnitBox[] connectedBoxes)
    {
      // Сортировка сервисов по степени проплаченности.
      var usersBusinessUnit = Sungero.Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(Sungero.Company.Employees.As(Users.Current));
      if (usersBusinessUnit != null)
        connectedBoxes = connectedBoxes
          .OrderBy(b => !Equals(b.BusinessUnit, usersBusinessUnit))
          .ThenBy(b => b.Name)
          .ToArray();
      else
        connectedBoxes = connectedBoxes
          .OrderBy(b => b.Name)
          .ToArray();
      
      if (foundCompany == null)
        return connectedBoxes.ToArray();
      
      var availableBoxes = connectedBoxes
        .Where(b => Equals(b.ExchangeService.ExchangeProvider, foundCompany.Box.ExchangeService.ExchangeProvider))
        .Where(b => foundCompany.TIN != b.BusinessUnit.TIN || (foundCompany.TRRC != b.BusinessUnit.TRRC && !string.IsNullOrWhiteSpace(b.BusinessUnit.TRRC)))
        .ToArray();
      
      return availableBoxes;
    }
    
    /// <summary>
    /// Отправить по почте приглашение к обмену.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    /// <param name="recipientEMail">Электронный адрес получателя.</param>
    public static void CreateInvitationEmail(ExchangeCore.IBusinessUnitBox box, string recipientEMail)
    {
      // Проверить заполненность обязательных параметров.
      if (box == null)
        return;
      
      var businessUnit = box.BusinessUnit;
      var businessUnitLegalName = businessUnit.LegalName;
      
      // Получить ссылку на регистрацию в сервисе.
      var registrationUrl = string.Empty;

      if (box.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
        registrationUrl = Resources.WizardMailRegisterDiadoc;
      else
        registrationUrl = box.ExchangeService.LogonUrl;
      
      var mailSubject = Resources.WizardMailSubjectFormat(businessUnit);
      var mailBody = Resources.WizardMailBodyFormat(businessUnitLegalName, Users.Current, box.ExchangeService.Name, registrationUrl);
      string mailto = Resources.WizardMailFormat(recipientEMail, mailSubject, mailBody);
      
      mailto = mailto.Replace("\"", "%22");
      Hyperlinks.Open(mailto);
    }
    
    #endregion
    
    /// <summary>
    /// Вызвать поиск контрагента по ссылке.
    /// </summary>
    /// <param name="cuuid">Uuid контрагента в 1С.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindCounterparty(string cuuid, string ctin, string ctrrc, string sysid)
    {
      var result = Functions.Module.Remote.FindCounterparty(cuuid, ctin, ctrrc, sysid);
      if (!result.Any())
        Dialogs.ShowMessage("Контрагент не найден.");
      else if (result.Count == 1)
        result.First().Show();
      else
        result.Show();
    }
  }
}
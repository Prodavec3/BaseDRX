using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties.Client
{
  public partial class CompanyBaseActions
  {

    public virtual void OpenOnDueDiligenceWebsite(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dueDiligenceWebsite = Parties.PublicFunctions.DueDiligenceWebsite.Remote.GetDefaultDueDiligenceWebsite();
      if (dueDiligenceWebsite == null)
      {
        e.AddError(Parties.DueDiligenceWebsites.Resources.ErrorDefaultDueDiligenceWebsiteNotFound);
        return;
      }

      // Проверка и выбор сайта для ИП.
      var dueDiligenceWebsiteUrl = dueDiligenceWebsite.Url;
      if (Functions.CompanyBase.IsSelfEmployed(_obj) && !string.IsNullOrWhiteSpace(dueDiligenceWebsite.UrlForSelfEmployed))
        dueDiligenceWebsiteUrl = dueDiligenceWebsite.UrlForSelfEmployed;
      dueDiligenceWebsiteUrl = dueDiligenceWebsiteUrl.ToUpper();
      
      // Проверка ОГРН.
      var psrnNeeded = dueDiligenceWebsiteUrl.Contains(Constants.DueDiligenceWebsite.Websites.OgrnMask);
      if (psrnNeeded && string.IsNullOrWhiteSpace(_obj.PSRN))
      {
        e.AddError(Parties.CompanyBases.Resources.NeedFillPsrnForDueDiligenceWebsite);
        return;
      }
      
      // Проверка ИНН.
      var tinNeeded = dueDiligenceWebsiteUrl.Contains(Constants.DueDiligenceWebsite.Websites.InnMask);
      if (tinNeeded && string.IsNullOrWhiteSpace(_obj.TIN))
      {
        e.AddError(Parties.CompanyBases.Resources.NeedFillTinForDueDiligenceWebsite);
        return;
      }
      
      // Формирование URL.
      var url = dueDiligenceWebsiteUrl
        .Replace(Constants.DueDiligenceWebsite.Websites.OgrnMask, _obj.PSRN)
        .Replace(Constants.DueDiligenceWebsite.Websites.InnMask, _obj.TIN);
      Functions.Module.GoToWebsite(url, e);
    }

    public virtual bool CanOpenOnDueDiligenceWebsite(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void FillFromService(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (string.IsNullOrWhiteSpace(_obj.PSRN) && string.IsNullOrWhiteSpace(_obj.TIN) && string.IsNullOrWhiteSpace(_obj.Name))
      {
        e.AddError(CompanyBases.Resources.ErrorNeedFillTinPsrnNameForService);
        return;
      }
      
      if (!string.IsNullOrEmpty(_obj.PSRN))
        _obj.PSRN = _obj.PSRN.Trim();
      
      if (!string.IsNullOrEmpty(_obj.TIN))
        _obj.TIN = _obj.TIN.Trim();
      
      var response = Functions.CompanyBase.Remote.FillFromService(_obj, string.Empty);
      var error = response.Message;
      
      var companyDisplayValues = response.CompanyDisplayValues;
      if (companyDisplayValues != null && companyDisplayValues.Count < 1)
      {
        Dialogs.ShowMessage(CompanyBases.Resources.ErrorCompanyNotFoundInService, MessageType.Information);
        return;
      }
      
      if (!string.IsNullOrWhiteSpace(error))
      {
        Dialogs.ShowMessage(error, CompanyBases.Resources.ContactAdministrator, MessageType.Error);
        return;
      }
      
      if (response.Amount > 1)
      {
        const int MaxCompaniesCount = 25;
        var dialogText = response.Amount > MaxCompaniesCount
          ? CompanyBases.Resources.FoundMoreThanNCompaniesInServiceFormat(MaxCompaniesCount, response.Amount)
          : CompanyBases.Resources.FoundSeveralCompaniesInServiceFormat(response.Amount);
        var dialog = Dialogs.CreateInputDialog(CompanyBases.Resources.ChoseCompanyDialogTitle, dialogText);
        dialog.Buttons.AddOkCancel();
        var companyShortLabels = companyDisplayValues.Select(x => x.DisplayValue).Take(MaxCompaniesCount);
        var companyShortLabel = dialog.AddSelect(CompanyBases.Resources.FillFrom, true).From(companyShortLabels.ToArray());
        
        var result = dialog.Show();
        if (result == DialogButtons.Ok)
        {
          var companyDisplayValue = companyDisplayValues.Where(x => x.DisplayValue == companyShortLabel.Value).First();
          response = Functions.CompanyBase.Remote.FillFromService(_obj, companyDisplayValue.PSRN);
          error = response.Message;
          if (!string.IsNullOrWhiteSpace(error))
          {
            Dialogs.ShowMessage(error, MessageType.Error);
            return;
          }
        }
        else
          return;
      }
      
      if (string.IsNullOrWhiteSpace(error) &&
          response.CompanyDisplayValues != null)
      {
        Dialogs.NotifyMessage(CompanyBases.Resources.FillFromServiceSuccess);
        if (response.FoundContacts != null)
        {
          var contacts = string.Join(";", response.FoundContacts.Select(contact => string.Format("{0}|{1}|{2}", contact.FullName, contact.JobTitle, contact.Phone)));
          e.Params.AddOrUpdate(Constants.CompanyBase.FindedContactsInServiceParamName, contacts);
        }
      }
    }

    public virtual bool CanFillFromService(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.IsCardReadOnly != true;
    }

    public override void SendInvitation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendInvitation(e);
    }

    public override bool CanSendInvitation(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendInvitation(e);
    }

    public virtual void OpenContacts(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.GetContactsFromCompany(_obj).Show();
    }

    public virtual bool CanOpenContacts(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

  }
}
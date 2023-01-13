using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;
using Sungero.Domain.Shared.Validation;

namespace Sungero.Docflow.Client
{
  partial class AccountingDocumentBaseFunctions
  {
    /// <summary>
    /// Диалог заполнения информации о продавце.
    /// </summary>
    public virtual void SellerTitlePropertiesFillingDialog()
    {
      var isDpt = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer;
      var isDprr = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer;
      var isUtdAny = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      var isUtdCorrection = isUtdAny && _obj.IsAdjustment == true;
      var isUtdNotCorrection = isUtdAny && _obj.IsAdjustment != true;
      
      if (!isDpt && !isDprr && !isUtdAny)
        return;
      
      var dialog = Dialogs.CreateInputDialog(AccountingDocumentBases.Resources.PropertiesFillingDialog_SellerTitle);

      if (isDpt)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerGoodsTransfer;
      else if (isDprr)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerWorksTransfer;
      else if (isUtdNotCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerUniversalTransfer;
      else if (isUtdCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.SellerUniversalCorrectionTransfer;

      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = null;
      
      dialog.Text = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_SellerTitle;
      
      // Поле Подписал.
      var showSaveAndSignButton = false;
      var defaultSignatory = Company.Employees.Null;
      var signedBy = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, Company.Employees.Null);
      
      if (Functions.OfficialDocument.Remote.SignatorySettingWithAllUsersExist(_obj))
      {
        if (_obj.OurSignatory != null)
          defaultSignatory = _obj.OurSignatory;
        else if (Company.Employees.Current != null)
          defaultSignatory = Company.Employees.Current;
        
        showSaveAndSignButton = Users.Current != null;
      }
      else
      {
        var signatoriesIds = Functions.OfficialDocument.Remote.GetSignatoriesIds(_obj);
        
        if (signatoriesIds.Any(s => _obj.OurSignatory != null && Equals(s, _obj.OurSignatory.Id)))
          defaultSignatory = _obj.OurSignatory;
        else if (signatoriesIds.Any(s => Company.Employees.Current != null && Equals(s, Company.Employees.Current.Id)))
          defaultSignatory = Company.Employees.Current;
        else if (signatoriesIds.Count() == 1)
          defaultSignatory = Company.PublicFunctions.Module.Remote.GetEmployeeById(signatoriesIds.First());
        
        var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatoriesIds);
        
        signedBy.From(defaultEmployees);
        
        showSaveAndSignButton = signatoriesIds.Any(s => Users.Current != null && Equals(s, Users.Current.Id));
      }
      
      // Поле Полномочия.
      CommonLibrary.IDropDownDialogValue hasAuthority = null;
      if (isDpt || isDprr)
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister,
                AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register);
      else if (isUtdAny && _obj.IsAdjustment != true)
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegisterAndInvoiceSignatory,
                AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory);
      else if (isUtdAny && _obj.IsAdjustment == true)
      {
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_RegisterAndInvoiceSignatory);
        hasAuthority.IsEnabled = false;
      }

      // Поле Основание.
      INavigationDialogValue<ISignatureSetting> basis = null;
      basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, SignatureSettings.Null);

      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      if (showSaveAndSignButton)
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      IQueryable<ISignatureSetting> basisValues = null;
      List<ISignatureSetting> settings = null;
      
      signedBy.SetOnValueChanged(
        (sc) =>
        {
          settings = Functions.OfficialDocument.Remote.GetSignatureSettings(_obj, sc.NewValue);
          if (basis != null)
          {
            basisValues = Functions.OfficialDocument.Remote.GetSignatureSettingsWithCertificateByEmployee(_obj, sc.NewValue);
            basis.From(basisValues);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
            basis.Value = _obj.OurSigningReason != null && basisValues.Contains(_obj.OurSigningReason)
              ? _obj.OurSigningReason
              : Functions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, sc.NewValue);
          }
        });
      signedBy.Value = defaultSignatory;
      
      dialog.SetOnRefresh(refresh);
      dialog.SetOnButtonClick(
        (b) =>
        {
          if (b.Button == saveAndSignButton || b.Button == saveButton)
          {
            if (!b.IsValid)
              return;
          }
          
          var errorList = Functions.AccountingDocumentBase.Remote
            .TitleDialogValidationErrors(_obj, signedBy.Value, null, null, null, basis != null ? basis.Value : null);
          foreach (var errors in errorList.GroupBy(e => e.Text))
          {
            var controls = new List<CommonLibrary.IDialogControl>();
            foreach (var error in errors)
            {
              if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory)
                controls.Add(basis);
            }
            b.AddError(errors.Key, controls.ToArray());
          }
          
          if (b.IsValid)
          {
            var basisValue = basis != null ? SignatureSettings.Info.Properties.Reason.GetLocalizedValue(basis.Value.Reason) : string.Empty;
            var hasAuthorityValue = hasAuthority != null ? hasAuthority.Value : string.Empty;
            var signatureSetting = basis != null ? basis.Value : null;
            var title = Structures.AccountingDocumentBase.SellerTitle.Create(signedBy.Value, basisValue, hasAuthorityValue, signatureSetting);
            
            try
            {
              Functions.AccountingDocumentBase.Remote.GenerateSellerTitle(_obj, title);
            }
            catch (AppliedCodeException ex)
            {
              b.AddError(ex.Message);
              return;
            }
            catch (ValidationException ex)
            {
              b.AddError(ex.Message);
              return;
            }
            catch (Exception ex)
            {
              Logger.ErrorFormat("Error generation title: ", ex);
              b.AddError(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorSellerTitlePropertiesFilling);
              return;
            }

            if (b.Button == saveAndSignButton)
            {
              try
              {
                Functions.Module.ApproveWithAddenda(_obj, null, null, null, false, true, string.Empty);
              }
              catch (Exception ex)
              {
                b.AddError(ex.Message);
              }
            }
          }
        });
      
      dialog.Show();
    }

    /// <summary>
    /// Диалог заполнения информации о покупателе.
    /// </summary>
    public virtual void BuyerTitlePropertiesFillingDialog()
    {
      var taxDocumentClassifier = Functions.AccountingDocumentBase.Remote.GetTaxDocumentClassifier(_obj);
      var isAct = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Act;
      var isTorg12 = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.Waybill;
      var isDpt = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GoodsTransfer &&
        taxDocumentClassifier == Exchange.PublicConstants.Module.TaxDocumentClassifier.GoodsTransferSeller;
      var isDprr = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.WorksTransfer &&
        taxDocumentClassifier == Exchange.PublicConstants.Module.TaxDocumentClassifier.WorksTransferSeller;
      var isUtdAny = _obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer;
      var isUtdCorrection = isUtdAny && _obj.IsAdjustment == true;
      var isUtdNotCorrection = isUtdAny && _obj.IsAdjustment != true;
      var isOldUtdCorrection = isUtdCorrection && (taxDocumentClassifier == Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalCorrectionDocumentSeller);
      var isWaybill = isTorg12 || isDpt;
      var isContractStatement = isAct || isDprr;
      
      if (!isUtdAny && !isWaybill && !isContractStatement)
        return;
      
      var dialog = Dialogs.CreateInputDialog(AccountingDocumentBases.Resources.PropertiesFillingDialog_Title);

      if (isTorg12)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.Waybill;
      else if (isAct)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.ContractStatement;
      else if (isDpt)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.GoodsTransfer;
      else if (isDprr)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.WorksTransfer;
      else if (isUtdNotCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.UniversalTransfer;
      else if (isUtdCorrection)
        dialog.HelpCode = Constants.AccountingDocumentBase.HelpCodes.UniversalCorrectionTransfer;

      Action<CommonLibrary.InputDialogRefreshEventArgs> refresh = null;

      var dialogText = string.Empty;
      
      if (isUtdNotCorrection)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Universal;

      if (isUtdCorrection)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_UniversalCorrection;

      if (isWaybill)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Waybill;

      if (isContractStatement)
        dialogText = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_Act;
      
      dialog.Text = dialogText;
      
      // Поле Подписал.
      var showSaveAndSignButton = false;
      var defaultSignatory = Company.Employees.Null;
      var signatory = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, defaultSignatory);

      if (Functions.OfficialDocument.Remote.SignatorySettingWithAllUsersExist(_obj))
      {
        if (_obj.OurSignatory != null)
          defaultSignatory = _obj.OurSignatory;
        else if (Company.Employees.Current != null)
          defaultSignatory = Company.Employees.Current;
        
        showSaveAndSignButton = Users.Current != null;
      }
      else
      {
        var signatoriesIds = Functions.OfficialDocument.Remote.GetSignatoriesIds(_obj);
        
        if (signatoriesIds.Any(s => _obj.OurSignatory != null && Equals(s, _obj.OurSignatory.Id)))
          defaultSignatory = _obj.OurSignatory;
        else if (signatoriesIds.Any(s => Company.Employees.Current != null && Equals(s, Company.Employees.Current.Id)))
          defaultSignatory = Company.Employees.Current;
        else if (signatoriesIds.Count() == 1)
          defaultSignatory = Company.PublicFunctions.Module.Remote.GetEmployeeById(signatoriesIds.First());
        
        var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatoriesIds);
        
        signatory.From(defaultEmployees);
        
        showSaveAndSignButton = signatoriesIds.Any(s => Users.Current != null && Equals(s, Users.Current.Id));
      }
      
      // Поле Полномочия.
      CommonLibrary.IDropDownDialogValue hasAuthority = null;
      if (!isAct && !isTorg12)
      {
        hasAuthority = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority, true, 0);
        if (isOldUtdCorrection)
        {
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register);
          hasAuthority.IsEnabled = false;
        }
        else if (isUtdCorrection)
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchf,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_SignSchfAndRegister,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Other);
        else
          hasAuthority.From(AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Deal,
                            AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister);
      }

      // Поле Основание.
      INavigationDialogValue<ISignatureSetting> basis = null;
      if (!isTorg12)
        basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, SignatureSettings.Null);

      // Дата подписания (Дата согласования, если УКД).
      var signingLabel = isUtdCorrection ?
        AccountingDocumentBases.Resources.PropertiesFillingDialog_DateApproving :
        AccountingDocumentBases.Resources.PropertiesFillingDialog_AcceptanceDate;
      var signingDate = dialog.AddDate(signingLabel, true, Calendar.UserToday);
      
      // Результат и Разногласия.
      CommonLibrary.IDropDownDialogValue result = null;
      CommonLibrary.IMultilineStringDialogValue disagreement = null;
      if (!isUtdCorrection && !isContractStatement)
      {
        result = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted,
                AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement,
                isUtdNotCorrection ? AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted : null);
        disagreement = dialog.AddMultilineString(AccountingDocumentBases.Resources.PropertiesFillingDialog_Disagreement, false);
      }
      
      // Поле Результат для УКД.
      CommonLibrary.IDropDownDialogValue adjustmentResult = null;
      if (isUtdCorrection)
      {
        adjustmentResult = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Result, true, 0)
          .From(AccountingDocumentBases.Resources.PropertiesFillingDialog_AdjustmentResult_AgreedChanges);
        adjustmentResult.IsEnabled = false;
      }
      
      // Груз принял получатель груза.
      CommonLibrary.IBooleanDialogValue isSameConsignee = null;
      INavigationDialogValue<Company.IEmployee> consignee = null;
      CommonLibrary.IDropDownDialogValue consigneeBasis = null;
      INavigationDialogValue<IPowerOfAttorney> consigneeAttorney = null;
      CommonLibrary.IStringDialogValue consigneeDocument = null;
      if (isWaybill || isUtdNotCorrection)
      {
        isSameConsignee = dialog.AddBoolean(AccountingDocumentBases.Resources.PropertiesFillingDialog_SameConsignee, true);
        consignee = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Consignee, false, Company.Employees.Null)
          .Where(x => Equals(x.Status, CoreEntities.DatabookEntry.Status.Active));
        consigneeBasis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_ConsigneeBasis, false, 0);
        consigneeAttorney = dialog.AddSelect(PowerOfAttorneys.Info.LocalizedName, false, PowerOfAttorneys.Null);
        consigneeDocument = dialog.AddString(AccountingDocumentBases.Resources.PropertiesFillingDialog_Document, false);
        consigneeDocument.MaxLength(Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength);
      }

      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      
      if (showSaveAndSignButton)
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      IQueryable<ISignatureSetting> settings = null;
      IPowerOfAttorney[] consigneePowerOfAttorneyValues = null;
      
      signatory.SetOnValueChanged(
        (sc) =>
        {
          if (basis != null)
          {
            settings = Functions.OfficialDocument.Remote.GetSignatureSettingsWithCertificateByEmployee(_obj, sc.NewValue);
            basis.From(settings);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
            basis.Value = _obj.OurSigningReason != null && settings.Contains(_obj.OurSigningReason)
              ? _obj.OurSigningReason
              : Functions.OfficialDocument.Remote.GetDefaultSignatureSetting(_obj, sc.NewValue);
          }
        });
      signatory.Value = defaultSignatory;
      
      Action<CommonLibrary.InputDialogValueChangedEventArgs<string>> consigneeBasisChanged =
        cb =>
      {
        var basisIsDuties = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Duties);
        var basisIsAttorney = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney);
        var basisIsOther = cb.NewValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other);
        
        if (consigneeAttorney != null)
        {
          consigneeAttorney.IsVisible = !basisIsOther;
          consigneeAttorney.IsRequired = basisIsAttorney;
          consigneeAttorney.IsEnabled = basisIsAttorney;
          if (!consigneeAttorney.IsEnabled)
            consigneeAttorney.Value = null;
          else
            consigneeAttorney.Value = consigneePowerOfAttorneyValues.Length == 1 ? consigneePowerOfAttorneyValues.SingleOrDefault() : null;
        }
        
        if (consigneeDocument != null)
        {
          consigneeDocument.IsVisible = basisIsOther;
          consigneeDocument.IsRequired = basisIsOther;
          if (!consigneeDocument.IsVisible)
            consigneeDocument.Value = null;
        }
      };
      
      if (consignee != null)
        consignee.SetOnValueChanged(
          ce =>
          {
            var cbValues = new List<string>();
            if (ce.NewValue != null)
            {
              cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Duties));
              cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney));
              if (!isTorg12)
                cbValues.Add(SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other));

              consigneePowerOfAttorneyValues = Functions.PowerOfAttorney.Remote.GetActivePowerOfAttorneys(ce.NewValue, signingDate.Value).ToArray();
            }
            else
              consigneePowerOfAttorneyValues = new IPowerOfAttorney[0];
            
            consigneeBasis.From(cbValues.ToArray());
            consigneeAttorney.From(consigneePowerOfAttorneyValues);

            consigneeBasis.Value = cbValues.OrderBy(v => v != consigneeBasis.Value).FirstOrDefault();
            consigneeBasisChanged.Invoke(new CommonLibrary.InputDialogValueChangedEventArgs<string>(null, consigneeBasis.Value));
          });
      
      if (consigneeBasis != null)
        consigneeBasis.SetOnValueChanged(consigneeBasisChanged);
      
      signingDate.SetOnValueChanged(
        sd =>
        {
          if (consigneeAttorney != null)
          {
            if (sd.NewValue.HasValue && consignee.Value != null)
              consigneePowerOfAttorneyValues = Functions.PowerOfAttorney.Remote.GetActivePowerOfAttorneys(consignee.Value, signingDate.Value).ToArray();
            else
              consigneePowerOfAttorneyValues = new IPowerOfAttorney[0];

            consigneeAttorney.From(consigneePowerOfAttorneyValues);
            consigneeBasisChanged.Invoke(new CommonLibrary.InputDialogValueChangedEventArgs<string>(null, consigneeBasis.Value));
          }
        });
      
      if (result != null)
        result.SetOnValueChanged(
          r =>
          {
            if (string.Equals(r.NewValue, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted) && disagreement != null)
              disagreement.Value = string.Empty;
          });

      refresh = (r) =>
      {
        if (disagreement != null)
          disagreement.IsEnabled = string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement) ||
            string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted);
        
        if (isSameConsignee != null)
        {
          var needConsignee = !isSameConsignee.Value.Value;
          consignee.IsEnabled = needConsignee;
          consignee.IsRequired = needConsignee;
          consigneeBasis.IsEnabled = needConsignee && consignee.Value != null;
          consigneeBasis.IsRequired = needConsignee && consignee.Value != null;

          if (!needConsignee)
          {
            consignee.Value = Company.Employees.Null;
            consigneeBasis.Value = string.Empty;
          }
        }
      };
      
      dialog.SetOnRefresh(refresh);
      dialog.SetOnButtonClick(
        (b) =>
        {
          if (b.Button == saveAndSignButton || b.Button == saveButton)
          {
            if (!b.IsValid)
              return;

            var consigneeValue = isSameConsignee != null ? (isSameConsignee.Value == true ? signatory.Value : consignee.Value) : null;
            IPowerOfAttorneyBase consigneePowerOfAttorneyValue = null;
            var consigneeOtherReasonValue = string.Empty;
            if (isSameConsignee != null && isSameConsignee.Value != true)
            {
              consigneePowerOfAttorneyValue = consigneeAttorney != null ? consigneeAttorney.Value : null;
              consigneeOtherReasonValue = consigneeDocument != null ? consigneeDocument.Value : null;
            }

            var errorList = Functions.AccountingDocumentBase.Remote
              .TitleDialogValidationErrors(_obj, signatory.Value, consignee != null ? consignee.Value : null,
                                           consigneePowerOfAttorneyValue, consigneeOtherReasonValue, basis != null ? basis.Value : null);
            
            if (isSameConsignee != null && isSameConsignee.Value != null && isSameConsignee.Value == true && basis != null && basis.Value != null)
            {
              var powersBase = Functions.Module.GetSigningReason(basis.Value);
              
              if (!string.IsNullOrEmpty(powersBase) && powersBase.Length > Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength)
              {
                var error = string.Format(Sungero.Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_ConsigneePowersBaseGreaterMaxLength, 
                                          Constants.AccountingDocumentBase.PowersBaseConsigneeMaxLength);
                errorList.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowersBase, error));
              }
            }
            
            foreach (var errors in errorList.GroupBy(e => e.Text))
            {
              var controls = new List<CommonLibrary.IDialogControl>();
              foreach (var error in errors)
              {
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory)
                  controls.Add(signatory);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Consignee)
                  controls.Add(consignee);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.ConsigneePowerOfAttorney)
                  controls.Add(consigneeAttorney);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowersBase)
                  controls.Add(basis);
              }
              b.AddError(errors.Key, controls.ToArray());
            }
            
            if (b.IsValid)
            {
              var basisValue = basis != null ? Functions.Module.GetSigningReason(basis.Value) : string.Empty;
              var consigneeBasisValue = isSameConsignee != null ? (isSameConsignee.Value == true ? basisValue : consigneeBasis.Value) : string.Empty;
              var disagreementValue = disagreement != null ? disagreement.Value : string.Empty;
              var hasAuthorityValue = hasAuthority != null ? hasAuthority.Value : string.Empty;
              
              var title = Structures.AccountingDocumentBase.BuyerTitle.Create();
              title.ActOfDisagreement = disagreementValue;
              title.Signatory = signatory.Value;
              title.SignatoryPowersBase = basisValue;
              title.SignatureSetting = basis != null ? basis.Value : null;
              title.Consignee = consigneeValue;
              title.ConsigneePowersBase = consigneeBasisValue;
              
              if (result == null || !result.IsVisible || string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_Accepted))
                title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
              else if (string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_AcceptedWithDisagreement))
                title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.PartiallyAccepted;
              else if (string.Equals(result.Value, AccountingDocumentBases.Resources.PropertiesFillingDialog_Result_NotAccepted))
                title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Rejected;
              else
                title.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
              
              title.SignatoryPowers = hasAuthorityValue;
              title.AcceptanceDate = signingDate.Value;
              title.ConsigneePowerOfAttorney = consigneePowerOfAttorneyValue;
              title.ConsigneeOtherReason = consigneeOtherReasonValue;
              
              try
              {
                Functions.AccountingDocumentBase.Remote.GenerateAnswer(_obj, title, false);
              }
              catch (AppliedCodeException ex)
              {
                b.AddError(ex.Message);
                return;
              }
              catch (ValidationException ex)
              {
                b.AddError(ex.Message);
                return;
              }
              catch (Exception ex)
              {
                Logger.ErrorFormat("Error generation title: ", ex);
                b.AddError(Sungero.Docflow.AccountingDocumentBases.Resources.ErrorBuyerTitlePropertiesFilling);
                return;
              }

              if (b.Button == saveAndSignButton)
              {
                try
                {
                  Functions.Module.ApproveWithAddenda(_obj, null, null, null, false, true, string.Empty);
                }
                catch (Exception ex)
                {
                  b.AddError(ex.Message);
                }
              }
            }
          }
        });
      dialog.Show();
    }
    
    /// <summary>
    /// Заполнение значений доверенности и документа с основанием "Другой документ".
    /// </summary>
    /// <param name="newValue">Основание.</param>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <param name="basisDocument">Документ основания.</param>
    /// <param name="powerOfAttorneyValues">Список доверенностей.</param>
    /// <param name="basisDocumentValues">Список документов основания.</param>
    private static void FillBasisDocuments(string newValue,
                                           INavigationDialogValue<IPowerOfAttorney> powerOfAttorney,
                                           CommonLibrary.IDropDownDialogValue basisDocument,
                                           IPowerOfAttorney[] powerOfAttorneyValues,
                                           string[] basisDocumentValues)
    {
      var basisIsAttorney = newValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.PowerOfAttorney);
      var basisIsOther = newValue == SignatureSettings.Info.Properties.Reason.GetLocalizedValue(SignatureSetting.Reason.Other);
      
      if (powerOfAttorney != null)
      {
        powerOfAttorney.IsVisible = !basisIsOther;
        powerOfAttorney.IsRequired = basisIsAttorney;
        powerOfAttorney.IsEnabled = basisIsAttorney;
        if (!powerOfAttorney.IsEnabled)
          powerOfAttorney.Value = null;
        else
          powerOfAttorney.Value = powerOfAttorneyValues.Length == 1 ? powerOfAttorneyValues.Single() : null;
      }
      if (basisDocument != null)
      {
        basisDocument.IsVisible = basisIsOther;
        basisDocument.IsRequired = basisIsOther;
        if (!basisDocument.IsVisible)
          basisDocument.Value = null;
        else
          basisDocument.Value = basisDocumentValues.Length == 1 ? basisDocumentValues.Single() : null;
      }
    }
    
    /// <summary>
    /// Генерировать титул покупателя в автоматическом режиме.
    /// </summary>
    public virtual void GenerateDefaultBuyerTitle()
    {
      if (_obj.ExchangeState == OfficialDocument.ExchangeState.SignRequired && _obj.BuyerTitleId == null)
      {
        Docflow.PublicFunctions.AccountingDocumentBase.Remote.GenerateDefaultAnswer(_obj, Company.Employees.Current, false);
      }
    }
    
    /// <summary>
    /// Генерировать титул продавца в автоматическом режиме.
    /// </summary>
    public virtual void GenerateDefaultSellerTitle()
    {
      if (_obj.IsFormalized == true && _obj.SellerTitleId != null && !FinancialArchive.PublicFunctions.Module.Remote.HasSellerSignatoryInfo(_obj))
      {
        Docflow.PublicFunctions.AccountingDocumentBase.Remote.GenerateDefaultSellerTitle(_obj, Sungero.Company.Employees.Current);
      }
    }
    
    /// <summary>
    /// Дополнительное условие доступности действия "Сменить тип".
    /// </summary>
    /// <returns>True - если действие "Сменить тип" доступно, иначе - false.</returns>
    public override bool CanChangeDocumentType()
    {
      return _obj.IsFormalized != true && base.CanChangeDocumentType();
    }
  }
}
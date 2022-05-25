using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.ClientExtensions;
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
      var signatories = Functions.OfficialDocument.Remote.GetSignatories(_obj);
      
      dialog.Text = AccountingDocumentBases.Resources.PropertiesFillingDialog_Text_SellerTitle;
      
      var defaultSignatory = Company.Employees.Null;
      if (signatories.Any(s => _obj.OurSignatory != null && Equals(s.EmployeeId, _obj.OurSignatory.Id)))
        defaultSignatory = _obj.OurSignatory;
      else if (signatories.Any(s => Company.Employees.Current != null && Equals(s.EmployeeId, Company.Employees.Current.Id)))
        defaultSignatory = Company.Employees.Current;
      else if (signatories.Select(s => s.EmployeeId).Distinct().Count() == 1)
        defaultSignatory = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatories.Select(s => s.EmployeeId).ToList()).FirstOrDefault();
      
      // Поле Подписал.
      var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatories.Select(s => s.EmployeeId).ToList());
      var signedBy = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, Company.Employees.Null)
        .From(defaultEmployees.Distinct());
      
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

      // Поле Основание и связанные с ним Доверенность/Документ.
      CommonLibrary.IDropDownDialogValue basis = null;
      INavigationDialogValue<IPowerOfAttorney> attorney = null;
      CommonLibrary.IDropDownDialogValue basisDocument = null;
      basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, 0);
      attorney = dialog.AddSelect(PowerOfAttorneys.Info.LocalizedName, false, PowerOfAttorneys.Null);
      basisDocument = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Document, false, null);

      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      if (signatories.Any(s => Users.Current != null && Equals(s.EmployeeId, Users.Current.Id)))
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      string[] basisValues = null;
      IPowerOfAttorney[] attorneyValues = null;
      string[] basisDocumentValues = null;
      List<ISignatureSetting> settings = null;

      if (basis != null)
        basis.SetOnValueChanged(bv => FillBasisDocuments(bv.NewValue, attorney, basisDocument, attorneyValues, basisDocumentValues));
      
      signedBy.SetOnValueChanged(
        (sc) =>
        {
          settings = Functions.OfficialDocument.Remote.GetSignatureSettings(_obj, sc.NewValue);
          if (basis != null)
          {
            basisValues = settings.Select(s => s.Reason).Distinct()
              .OrderBy(r => r != SignatureSetting.Reason.Duties)
              .ThenBy(r => r != SignatureSetting.Reason.PowerOfAttorney)
              .Select(r => SignatureSettings.Info.Properties.Reason.GetLocalizedValue(r)).ToArray();
            basis.From(basisValues);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
          }
          if (attorney != null)
          {
            attorneyValues = settings.Where(s => s.Reason == SignatureSetting.Reason.PowerOfAttorney)
              .Select(s => s.Document).OfType<IPowerOfAttorney>().Where(d => d.AccessRights.CanRead(Users.Current)).ToArray();
            attorney.From(attorneyValues);
          }
          if (basisDocument != null)
          {
            basisDocumentValues = settings.Where(s => s.Reason == SignatureSetting.Reason.Other).Select(s => s.DocumentInfo).ToArray();
            basisDocument.From(basisDocumentValues);
          }
          if (basis != null)
          {
            basis.Value = basisValues.FirstOrDefault();
            FillBasisDocuments(basis.Value, attorney, basisDocument, attorneyValues, basisDocumentValues);
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
          var signatoryAttorneyValue = attorney != null ? attorney.Value : null;
          var signatoryOtherReasonValue = basisDocument != null ? basisDocument.Value : null;
          
          var errorList = Functions.AccountingDocumentBase.Remote
            .TitleDialogValidationErrors(_obj, signedBy.Value, null,
                                         signatoryAttorneyValue, null,
                                         signatoryOtherReasonValue, null);
          foreach (var errors in errorList.GroupBy(e => e.Text))
          {
            var controls = new List<CommonLibrary.IDialogControl>();
            foreach (var error in errors)
            {
              if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory)
                controls.Add(signedBy);
              if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowerOfAttorney)
                controls.Add(attorney);
            }
            b.AddError(errors.Key, controls.ToArray());
          }
          
          if (b.IsValid)
          {
            var basisValue = basis != null ? basis.Value : string.Empty;
            var hasAuthorityValue = hasAuthority != null ? hasAuthority.Value : string.Empty;
            var signatoryAttorney = Structures.AccountingDocumentBase.Attorney.Create(signatoryAttorneyValue, signatoryOtherReasonValue);
            var title = Structures.AccountingDocumentBase.SellerTitle.Create(signedBy.Value, basisValue, hasAuthorityValue,
                                                                             signatoryAttorney.Document, signatoryAttorney.OtherReason);
            
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
      var exchangeProvider = _obj.BusinessUnitBox.ExchangeService.ExchangeProvider;
      
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
      var signatories = Functions.OfficialDocument.Remote.GetSignatories(_obj);
      
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
      
      var defaultSignatory = Company.Employees.Null;
      if (signatories.Any(s => _obj.OurSignatory != null && Equals(s.EmployeeId, _obj.OurSignatory.Id)))
        defaultSignatory = _obj.OurSignatory;
      else if (signatories.Any(s => Company.Employees.Current != null && Equals(s.EmployeeId, Company.Employees.Current.Id)))
        defaultSignatory = Company.Employees.Current;
      else if (signatories.Select(s => s.EmployeeId).Distinct().Count() == 1)
        defaultSignatory = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatories.Select(s => s.EmployeeId).ToList()).FirstOrDefault();
      
      // Поле Подписал.
      var defaultEmployees = Functions.AccountingDocumentBase.Remote.GetEmployeesByIds(signatories.Select(s => s.EmployeeId).ToList());
      var signatory = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_SignedBy, true, Company.Employees.Null)
        .From(defaultEmployees.Distinct());
      
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
        else if (isUtdCorrection && exchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Synerdocs)
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

      // Поле Основание и связанные с ним Доверенность/Документ.
      CommonLibrary.IDropDownDialogValue basis = null;
      INavigationDialogValue<IPowerOfAttorney> powerOfAttorney = null;
      CommonLibrary.IDropDownDialogValue basisDocument = null;
      if (!isTorg12)
      {
        basis = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Basis, true, 0);
        powerOfAttorney = dialog.AddSelect(PowerOfAttorneys.Info.LocalizedName, false, PowerOfAttorneys.Null);

        if (!isAct)
          basisDocument = dialog.AddSelect(AccountingDocumentBases.Resources.PropertiesFillingDialog_Document, false, null);
      }

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
      }

      CommonLibrary.CustomDialogButton saveAndSignButton = null;
      if (signatories.Any(s => Users.Current != null && Equals(s.EmployeeId, Users.Current.Id)))
        saveAndSignButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_SaveAndSign);
      
      var saveButton = dialog.Buttons.AddCustom(AccountingDocumentBases.Resources.PropertiesFillingDialog_Save);
      dialog.Buttons.Default = saveAndSignButton ?? saveButton;
      var cancelButton = dialog.Buttons.AddCancel();
      
      string[] basisValues = null;
      IPowerOfAttorney[] powerOfAttorneyValues = null;
      string[] basisDocumentValues = null;
      List<ISignatureSetting> settings = null;
      IPowerOfAttorney[] consigneePowerOfAttorneyValues = null;

      if (basis != null)
        basis.SetOnValueChanged(bv => FillBasisDocuments(bv.NewValue, powerOfAttorney, basisDocument, powerOfAttorneyValues, basisDocumentValues));
      
      signatory.SetOnValueChanged(
        (sc) =>
        {
          settings = Functions.OfficialDocument.Remote.GetSignatureSettings(_obj, sc.NewValue);
          if (basis != null)
          {
            basisValues = settings.Select(s => s.Reason).Distinct()
              .Where(r => !isAct || r != SignatureSetting.Reason.Other)
              .OrderBy(r => r != SignatureSetting.Reason.Duties)
              .ThenBy(r => r != SignatureSetting.Reason.PowerOfAttorney)
              .Select(r => SignatureSettings.Info.Properties.Reason.GetLocalizedValue(r)).ToArray();
            basis.From(basisValues);
            basis.IsEnabled = sc.NewValue != null;
            basis.IsRequired = sc.NewValue != null;
          }
          if (powerOfAttorney != null)
          {
            powerOfAttorneyValues = settings.Where(s => s.Reason == SignatureSetting.Reason.PowerOfAttorney)
              .Select(s => s.Document).OfType<IPowerOfAttorney>().Where(d => d.AccessRights.CanRead(Users.Current)).ToArray();
            powerOfAttorney.From(powerOfAttorneyValues);
          }
          if (basisDocument != null)
          {
            basisDocumentValues = settings.Where(s => s.Reason == SignatureSetting.Reason.Other).Select(s => s.DocumentInfo).ToArray();
            basisDocument.From(basisDocumentValues);
          }
          if (basis != null)
          {
            basis.Value = basisValues.FirstOrDefault();
            FillBasisDocuments(basis.Value, powerOfAttorney, basisDocument, powerOfAttorneyValues, basisDocumentValues);
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
            var signatoryPowerOfAttorneyValue = powerOfAttorney != null ? powerOfAttorney.Value : null;
            var signatoryOtherReasonValue = basisDocument != null ? basisDocument.Value : null;
            var consigneePowerOfAttorneyValue = consigneeAttorney != null ? consigneeAttorney.Value : null;
            var consigneeOtherReasonValue = consigneeDocument != null ? consigneeDocument.Value : null;
            var errorList = Functions.AccountingDocumentBase.Remote
              .TitleDialogValidationErrors(_obj, signatory.Value, consignee != null ? consignee.Value : null,
                                           signatoryPowerOfAttorneyValue, consigneePowerOfAttorneyValue,
                                           signatoryOtherReasonValue, consigneeOtherReasonValue);
            foreach (var errors in errorList.GroupBy(e => e.Text))
            {
              var controls = new List<CommonLibrary.IDialogControl>();
              foreach (var error in errors)
              {
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory)
                  controls.Add(signatory);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.Consignee)
                  controls.Add(consignee);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.SignatoryPowerOfAttorney)
                  controls.Add(powerOfAttorney);
                if (error.Type == Constants.AccountingDocumentBase.GenerateTitleTypes.ConsigneePowerOfAttorney)
                  controls.Add(consigneeAttorney);
              }
              b.AddError(errors.Key, controls.ToArray());
            }
            
            if (b.IsValid)
            {
              var consigneeBasisValue = isSameConsignee != null && basis != null ? (isSameConsignee.Value == true ? basis.Value : consigneeBasis.Value) : string.Empty;
              var disagreementValue = disagreement != null ? disagreement.Value : string.Empty;
              var basisValue = basis != null ? basis.Value : string.Empty;
              var hasAuthorityValue = hasAuthority != null ? hasAuthority.Value : string.Empty;
              var signatoryPowerOfAttorney = Structures.AccountingDocumentBase.Attorney.Create(signatoryPowerOfAttorneyValue, signatoryOtherReasonValue);
              var consigneePowerOfAttorney = Structures.AccountingDocumentBase.Attorney.Create(consigneePowerOfAttorneyValue, consigneeOtherReasonValue);
              var title = Structures.AccountingDocumentBase.BuyerTitle.Create();
              title.ActOfDisagreement = disagreementValue;
              title.Signatory = signatory.Value;
              title.SignatoryPowersBase = basisValue;
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
              title.SignatoryPowerOfAttorney = signatoryPowerOfAttorney.Document;
              title.SignatoryOtherReason = signatoryPowerOfAttorney.OtherReason;
              title.ConsigneePowerOfAttorney = consigneePowerOfAttorney.Document;
              title.ConsigneeOtherReason = consigneePowerOfAttorney.OtherReason;
              
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
  }
}
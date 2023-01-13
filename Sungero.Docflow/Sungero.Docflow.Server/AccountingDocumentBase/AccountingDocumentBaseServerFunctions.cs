using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.AccountingDocumentBase;
using Sungero.Domain;

namespace Sungero.Docflow.Server
{
  partial class AccountingDocumentBaseFunctions
  {
    
    /// <summary>
    /// Получить права подписания финансовых документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    [Obsolete("Используйте метод GetSignatureSettingsQuery")]
    public override List<ISignatureSetting> GetSignatureSettings()
    {
      var basedSettings = base.GetSignatureSettings()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || (s.Limit == Docflow.SignatureSetting.Limit.Amount &&
                                                                          s.Amount >= _obj.TotalAmount && Equals(s.Currency, _obj.Currency)))
        .ToList();
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)))
          .ToList();
      }
      return basedSettings;
    }
    
    /// <summary>
    /// Получить права подписания финансовых документов.
    /// </summary>
    /// <returns>Список подходящих правил.</returns>
    public override IQueryable<ISignatureSetting> GetSignatureSettingsQuery()
    {
      var basedSettings = base.GetSignatureSettingsQuery()
        .Where(s => s.Limit == Docflow.SignatureSetting.Limit.NoLimit || (s.Limit == Docflow.SignatureSetting.Limit.Amount &&
                                                                          s.Amount >= _obj.TotalAmount && Equals(s.Currency, _obj.Currency)));
      
      if (_obj.DocumentKind != null && _obj.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
      {
        var category = Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(_obj);
        basedSettings = basedSettings
          .Where(s => !s.Categories.Any() || s.Categories.Any(c => Equals(c.Category, category)));
      }
      return basedSettings;
    }
    
    /// <summary>
    /// Получить дату начала квартала.
    /// </summary>
    /// <param name="currentDate">Дата.</param>
    /// <returns>Дата начала квартала.</returns>
    [Public]
    public static DateTime BeginningOfQuarter(DateTime currentDate)
    {
      if (currentDate.Month < 4)
        return currentDate.BeginningOfYear();
      if (currentDate.Month > 3 && currentDate.Month < 7)
        return new DateTime(currentDate.Year, 4, 1);
      if (currentDate.Month > 6 && currentDate.Month < 10)
        return new DateTime(currentDate.Year, 7, 1);
      return new DateTime(currentDate.Year, 10, 1);
    }
    
    /// <summary>
    /// Получить дату окончания квартала.
    /// </summary>
    /// <param name="currentDate">Дата.</param>
    /// <returns>Дата окончания квартала.</returns>
    [Public]
    public static DateTime EndOfQuarter(DateTime currentDate)
    {
      if (currentDate.Month < 4)
        return new DateTime(currentDate.Year, 3, 31);
      if (currentDate.Month > 3 && currentDate.Month < 7)
        return new DateTime(currentDate.Year, 6, 30);
      if (currentDate.Month > 6 && currentDate.Month < 10)
        return new DateTime(currentDate.Year, 9, 30);
      return currentDate.EndOfYear();
    }
    
    /// <summary>
    /// Сгенерировать титул покупателя.
    /// </summary>
    /// <param name="sellerTitle">Параметры титула для генерации.</param>
    [Remote, Public]
    public virtual void GenerateSellerTitle(Structures.AccountingDocumentBase.ISellerTitle sellerTitle)
    {
      FinancialArchive.PublicFunctions.Module.Remote.GenerateSellerTitle(_obj, sellerTitle);

      // Поставить документ в очередь на генерацию pdf.
      if (_obj.SellerTitleId.HasValue)
      {
        var sellerVersion = _obj.Versions.Single(v => v.Id == _obj.SellerTitleId);

        if (sellerVersion.PublicBody.Size == 0)
        {
          var previousVersion = _obj.Versions.Where(v => v.Id != _obj.SellerTitleId).OrderBy(v => v.Number).LastOrDefault();
          if (previousVersion != null)
          {
            sellerVersion.PublicBody.Write(previousVersion.PublicBody.Read());
            sellerVersion.AssociatedApplication = previousVersion.AssociatedApplication;
          }
        }
        
        Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.SellerTitleId.Value);
        Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.SellerTitleId.Value, _obj.ExchangeState);
      }
    }
    
    /// <summary>
    /// Сгенерировать титул покупателя.
    /// </summary>
    /// <param name="buyerTitle">Параметры титула для генерации.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса.</param>
    [Remote, Public]
    public virtual void GenerateAnswer(Structures.AccountingDocumentBase.IBuyerTitle buyerTitle, bool isAgent)
    {
      Exchange.PublicFunctions.Module.GenerateBuyerTitle(_obj, buyerTitle);

      // Поставить документ в очередь на генерацию pdf.
      if (_obj.BuyerTitleId.HasValue)
      {
        var version = _obj.Versions.Single(v => v.Id == _obj.BuyerTitleId);
        var sellerVersion = _obj.Versions.Single(v => v.Id == _obj.SellerTitleId);
        version.PublicBody.Write(sellerVersion.PublicBody.Read());
        version.AssociatedApplication = sellerVersion.AssociatedApplication;
        if (isAgent)
        {
          Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
        }
        else
        {
          Docflow.PublicFunctions.Module.GenerateTempPublicBodyForExchangeDocument(_obj, _obj.BuyerTitleId.Value);
          Exchange.PublicFunctions.Module.EnqueueXmlToPdfBodyConverter(_obj, _obj.BuyerTitleId.Value, _obj.ExchangeState);
        }
      }
    }
    
    /// <summary>
    /// Сгенерировать титул покупателя в автоматическом режиме.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    /// <param name="isAgent">Признак вызова из фонового процесса.</param>
    [Remote, Public]
    public virtual void GenerateDefaultAnswer(Company.IEmployee signatory, bool isAgent)
    {
      var signatureSetting = Functions.OfficialDocument.GetDefaultSignatureSetting(_obj, signatory);
      var errorlist = Functions.AccountingDocumentBase.TitleDialogValidationErrors(_obj, signatory, null, null, null, signatureSetting);
      var validationText = string.Join(Environment.NewLine, errorlist.Select(l => l.Text));
      if (errorlist.Any())
        throw AppliedCodeException.Create(validationText);
      
      // У УКД доступен только вариант "ответственный за оформление".
      var authority = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister;
      if (_obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer && _obj.IsAdjustment == true)
        authority = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register;
      
      var basis = Functions.Module.GetSigningReason(signatureSetting);
      var buyerTitle = Docflow.Structures.AccountingDocumentBase.BuyerTitle.Create();
      buyerTitle.ActOfDisagreement = string.Empty;
      buyerTitle.Signatory = signatory;
      buyerTitle.SignatoryPowersBase = basis;
      buyerTitle.Consignee = null;
      buyerTitle.ConsigneePowersBase = string.Empty;
      buyerTitle.BuyerAcceptanceStatus = Exchange.ExchangeDocumentInfo.BuyerAcceptanceStatus.Accepted;
      buyerTitle.SignatoryPowers = authority;
      buyerTitle.AcceptanceDate = Calendar.Now;
      buyerTitle.ConsigneePowerOfAttorney = null;
      buyerTitle.ConsigneeOtherReason = null;
      buyerTitle.SignatureSetting = signatureSetting;

      this.GenerateAnswer(buyerTitle, isAgent);
    }

    /// <summary>
    /// Валидация диалога заполнения титула.
    /// </summary>
    /// <param name="signatory">Подписал.</param>
    /// <param name="consignee">Груз получил.</param>
    /// <param name="signatoryPowerOfAttorney">Доверенность подписывающего.</param>
    /// <param name="consigneePowerOfAttorney">Доверенность груз принявшего.</param>
    /// <param name="signatoryOtherReason">Документ подписывающего.</param>
    /// <param name="consigneeOtherReason">Документ груз принявшего.</param>
    /// <returns>Список ошибок.</returns>
    [Remote, Obsolete("Используйте TitleDialogValidationErrors без параметров доверенность подписывающего и документ подписывающего.")]
    public virtual List<Structures.AccountingDocumentBase.GenerateTitleError> TitleDialogValidationErrors(Company.IEmployee signatory,
                                                                                                          Company.IEmployee consignee,
                                                                                                          IPowerOfAttorneyBase signatoryPowerOfAttorney,
                                                                                                          IPowerOfAttorney consigneePowerOfAttorney,
                                                                                                          string signatoryOtherReason,
                                                                                                          string consigneeOtherReason)
    {
      return this.TitleDialogValidationErrors(signatory, consignee, consigneePowerOfAttorney, consigneeOtherReason, null);
    }
    
    /// <summary>
    /// Валидация диалога заполнения титула.
    /// </summary>
    /// <param name="signatory">Подписал.</param>
    /// <param name="consignee">Груз получил.</param>
    /// <param name="consigneePowerOfAttorney">Доверенность груз принявшего.</param>
    /// <param name="consigneeOtherReason">Документ груз принявшего.</param>
    /// <param name="signatorySetting">Право подписи подписавшего.</param>
    /// <returns>Список ошибок.</returns>
    [Remote]
    public virtual List<Structures.AccountingDocumentBase.GenerateTitleError> TitleDialogValidationErrors(Company.IEmployee signatory,
                                                                                                          Company.IEmployee consignee,
                                                                                                          IPowerOfAttorneyBase consigneePowerOfAttorney,
                                                                                                          string consigneeOtherReason,
                                                                                                          ISignatureSetting signatorySetting)
    {
      var errorlist = new List<Structures.AccountingDocumentBase.GenerateTitleError>();
      var signatoryType = Constants.AccountingDocumentBase.GenerateTitleTypes.Signatory;
      var consigneeType = Constants.AccountingDocumentBase.GenerateTitleTypes.Consignee;
      var consigneePoAType = Constants.AccountingDocumentBase.GenerateTitleTypes.ConsigneePowerOfAttorney;
      
      if (string.IsNullOrEmpty(_obj.BusinessUnit.TIN))
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_TIN));
      
      if (signatorySetting != null && signatorySetting.JobTitle == null && signatory != null && signatory.JobTitle == null)
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(signatoryType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_SignatoryJobTitle));
      
      if (consignee != null && consignee != signatory && consignee.JobTitle == null)
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneeType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_ConsigneeJobTitle));
      
      if (consigneePowerOfAttorney != null)
      {
        var number = string.Empty;
        if (Docflow.FormalizedPowerOfAttorneys.Is(consigneePowerOfAttorney))
          number = Docflow.FormalizedPowerOfAttorneys.As(consigneePowerOfAttorney).UnifiedRegistrationNumber;
        else
          number = consigneePowerOfAttorney.RegistrationNumber;
        
        if (consigneePowerOfAttorney.RegistrationDate == null || string.IsNullOrWhiteSpace(number))
          errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneePoAType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_AttorneyRegistration));
        
        if (consigneePowerOfAttorney.OurSignatory != null && consigneePowerOfAttorney.OurSignatory.JobTitle == null)
          errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(consigneePoAType, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_AttorneyJobTitle));
      }
      
      if (_obj.FormalizedServiceType == FormalizedServiceType.Waybill && !string.IsNullOrWhiteSpace(consigneeOtherReason))
        errorlist.Add(Structures.AccountingDocumentBase.GenerateTitleError.Create(null, AccountingDocumentBases.Resources.PropertiesFillingDialog_Error_OtherDocument));
      
      return errorlist;
    }
    
    /// <summary>
    /// Сгенерировать титул продавца в автоматическом режиме.
    /// </summary>
    /// <param name="signatory">Подписывающий.</param>
    [Remote, Public]
    public virtual void GenerateDefaultSellerTitle(Sungero.Company.IEmployee signatory)
    {
      var signatureSetting = Functions.OfficialDocument.GetDefaultSignatureSetting(_obj, signatory);
      var errorList = Functions.AccountingDocumentBase.TitleDialogValidationErrors(_obj, signatory, null, null, null, signatureSetting);
      var validationText = string.Join(Environment.NewLine, errorList.Select(l => l.Text));
      if (errorList.Any())
        throw AppliedCodeException.Create(validationText);
      
      // Полномочия: Лицо, совершившее сделку и отв. за оформление.
      var power = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_DealAndRegister;
      // Для УКД: Лицо, ответственное за оформление свершившегося события.
      if (_obj.FormalizedServiceType == Docflow.AccountingDocumentBase.FormalizedServiceType.GeneralTransfer && _obj.IsAdjustment == true)
        power = Docflow.AccountingDocumentBases.Resources.PropertiesFillingDialog_HasAuthority_Register;
      
      var sellerTitle = Docflow.Structures.AccountingDocumentBase.SellerTitle.Create();
      sellerTitle.Signatory = signatory;
      sellerTitle.SignatoryPowersBase = SignatureSettings.Info.Properties.Reason.GetLocalizedValue(signatureSetting.Reason);
      sellerTitle.SignatoryPowers = power;
      sellerTitle.SignatureSetting = signatureSetting;

      Functions.AccountingDocumentBase.GenerateSellerTitle(_obj, sellerTitle);
    }
    
    /// <summary>
    /// Получить список сотрудников по id.
    /// </summary>
    /// <param name="ids">Список Id.</param>
    /// <returns>Список сотрудников.</returns>
    [Remote]
    public static List<Company.IEmployee> GetEmployeesByIds(List<int> ids)
    {
      return Company.Employees.GetAll(x => ids.Contains(x.Id)).ToList();
    }
    
    /// <summary>
    /// Получить КНД.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>КНД.</returns>
    [Remote]
    public static string GetTaxDocumentClassifier(IAccountingDocumentBase document)
    {
      if (document.SellerTitleId.HasValue)
      {
        var sellerVersion = document.Versions.Single(v => v.Id == document.SellerTitleId);
        return Exchange.PublicFunctions.Module.GetTaxDocumentClassifierByContent(sellerVersion.Body.Read()).TaxDocumentClassifierCode;
      }
      return string.Empty;
    }

    /// <summary>
    /// Проверить, связан ли документ специализированной связью.
    /// </summary>
    /// <returns>True - если связан, иначе - false.</returns>
    [Remote(IsPure = true)]
    public override bool HasSpecifiedTypeRelations()
    {
      var hasSpecifiedTypeRelations = false;
      AccessRights.AllowRead(
        () =>
        {
          hasSpecifiedTypeRelations = AccountingDocumentBases.GetAll().Any(x => Equals(x.Corrected, _obj));
        });
      return base.HasSpecifiedTypeRelations() || hasSpecifiedTypeRelations;
    }
  }
}
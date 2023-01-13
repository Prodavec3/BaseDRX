using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.FinancialArchive.IncomingTaxInvoice;

namespace Sungero.FinancialArchive.Shared
{
  partial class IncomingTaxInvoiceFunctions
  {
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == Docflow.OfficialDocuments.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName;
      }
      
      if (_obj.DocumentKind == null || (!_obj.DocumentKind.GenerateDocumentName.Value && _obj.IsFormalized != true))
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> <контрагент> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += IncomingTaxInvoices.Resources.NamePartForContractor + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment != true)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment == true)
      {
        using (TenantInfo.Culture.SwitchTo())
        {
          name = Docflow.AccountingDocumentBases.Resources.Adjustment + _obj.DocumentKind.ShortName.ToLower() + name;
        }
      }
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }

    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки вложением в письмо.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      return Functions.Module.GetEmailAddressees(_obj);
    }
    
    #region Интеллектуальная обработка
    
    [Public]
    public override bool IsVerificationModeSupported()
    {
      return true;
    }
    
    #endregion
    
  }
}
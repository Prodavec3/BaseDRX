using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.FinancialArchive.ContractStatement;

namespace Sungero.FinancialArchive.Shared
{
  partial class ContractStatementFunctions
  {
    /// <summary>
    /// Проверить акт на дубли.
    /// </summary>
    /// <param name="contractStatement">Акт.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="registrationNumber">Рег. номер.</param>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="leadingDocument">Ведущий документ.</param>
    /// <returns>Признак дублей.</returns>
    public static bool HaveDuplicates(IContractStatement contractStatement,
                                      Sungero.Company.IBusinessUnit businessUnit,
                                      string registrationNumber,
                                      DateTime? registrationDate,
                                      Sungero.Parties.ICounterparty counterparty,
                                      Docflow.IOfficialDocument leadingDocument)
    {
      if (contractStatement == null ||
          businessUnit == null ||
          string.IsNullOrWhiteSpace(registrationNumber) ||
          registrationDate == null ||
          counterparty == null ||
          leadingDocument == null)
        return false;
      
      return Functions.ContractStatement.Remote.GetDuplicates(contractStatement,
                                                              businessUnit,
                                                              registrationNumber,
                                                              registrationDate,
                                                              counterparty,
                                                              leadingDocument)
        .Any();
    }
    
    #region Регистрация
    
    /// <summary>
    /// Получить номер ведущего документа.
    /// </summary>
    /// <returns>Номер документа либо пустая строка.</returns>
    public override string GetLeadDocumentNumber()
    {
      if (_obj.LeadingDocument != null)
      {
        return _obj.LeadingDocument.AccessRights.CanRead() ?
          _obj.LeadingDocument.RegistrationNumber :
          Contracts.PublicFunctions.ContractualDocument.Remote.GetRegistrationNumberIgnoreAccessRights(_obj.LeadingDocument.Id);
      }
      return string.Empty;
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      // Не автоформируемое имя.
      if (_obj != null && _obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value)
      {
        if (_obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
          _obj.Name = string.Empty;
        
        if (_obj.VerificationState != null && string.IsNullOrWhiteSpace(_obj.Name))
          _obj.Name = _obj.DocumentKind.ShortName; 
      }      
      
      if (_obj.DocumentKind == null || (!_obj.DocumentKind.GenerateDocumentName.Value && _obj.IsFormalized != true))
        return;
      
      // Автоформируемое имя.
      var name = string.Empty;
      
      /* Имя в форматах:
        <Вид документа> №<номер> от <дата> к договору № <номер_договора> с <наименование контрагента> "<содержание>"
        <Вид документа> №<номер> от <дата> к доп. соглашению № <номер_доп.соглашения> к договору № <номер_договора> с <наименование контрагента> "<содержание>"
        <Вид документа> №<номер> от <дата> с <наименование контрагента> "<содержание>"
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.LeadingDocument != null)
        {
          if (Contracts.ContractBases.Is(_obj.LeadingDocument))
            name += Contracts.PublicFunctions.ContractBase.GetContractNamePart(Contracts.ContractBases.As(_obj.LeadingDocument));
          else
            name += Contracts.PublicFunctions.SupAgreement.GetSupAgreementNamePart(Contracts.SupAgreements.As(_obj.LeadingDocument));
        }
        else
        {
          if (_obj.Counterparty != null)
            name += Docflow.AccountingDocumentBases.Resources.NamePartForContractor + _obj.Counterparty.DisplayValue;
        }
        
        if (!string.IsNullOrEmpty(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Определить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.ResponsibleEmployee != null)
        return _obj.ResponsibleEmployee;
      
      return base.GetDocumentResponsibleEmployee();
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
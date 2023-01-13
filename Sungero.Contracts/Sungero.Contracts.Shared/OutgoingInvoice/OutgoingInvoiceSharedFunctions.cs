using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Contracts.OutgoingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Shared
{
  partial class OutgoingInvoiceFunctions
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
        <Вид документа> №<номер> от <дата> в <контрагент> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += OutgoingInvoices.Resources.NamePartForContractor + _obj.Counterparty.DisplayValue;
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
      {
        name = _obj.VerificationState == null ? Docflow.OfficialDocuments.Resources.DocumentNameAutotext : _obj.DocumentKind.ShortName;
      }
      else if (_obj.DocumentKind != null && _obj.IsAdjustment != true)
      {
        name = _obj.DocumentKind.ShortName + name;
      }
      
      name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Обновить жизненный цикл документа.
    /// </summary>
    /// <param name="registrationState">Статус регистрации.</param>
    /// <param name="approvalState">Статус согласования.</param>
    /// <param name="counterpartyApprovalState">Статус согласования с контрагентом.</param>
    public override void UpdateLifeCycle(Enumeration? registrationState,
                                         Enumeration? approvalState,
                                         Enumeration? counterpartyApprovalState)
    {
      // Не проверять статусы для пустых параметров.
      if (_obj == null || _obj.DocumentKind == null)
        return;
      
      var lifeCycleMustBeActive = _obj.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft &&
        approvalState == Docflow.OfficialDocument.InternalApprovalState.Signed;
      
      if (lifeCycleMustBeActive)
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Active;
    }
    
    /// <summary>
    /// Получить список адресатов с электронной почтой для отправки вложением в письмо.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee> GetEmailAddressees()
    {
      var result = new List<Sungero.Docflow.Structures.OfficialDocument.IEmailAddressee>();
      
      // Получить контрагента.
      if (_obj.Counterparty != null && !string.IsNullOrWhiteSpace(_obj.Counterparty.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Counterparty.Name, _obj.Counterparty.Email),
                  _obj.Counterparty.Email);
        result.Add(emailAddressee);
      }
      
      // Получить контакт.
      if (_obj.Contact != null && !string.IsNullOrWhiteSpace(_obj.Contact.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.Contact.Name, _obj.Contact.Email),
                  _obj.Contact.Email);
        result.Add(emailAddressee);
      }
      
      // Получить подписывающего.
      if (_obj.CounterpartySignatory != null && !string.IsNullOrWhiteSpace(_obj.CounterpartySignatory.Email))
      {
        var emailAddressee = Sungero.Docflow.Structures.OfficialDocument.EmailAddressee
          .Create(Sungero.Docflow.OfficialDocuments.Resources.AddresseeLabelFormat(_obj.CounterpartySignatory.Name, _obj.CounterpartySignatory.Email),
                  _obj.CounterpartySignatory.Email);
        result.Add(emailAddressee);
      }
      
      return result;
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class OutgoingDocumentBaseFunctions
  {
    /// <summary>
    /// Добавить в группу вложений входящее письмо, в ответ на которое было создано исходящее.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      if (_obj.InResponseTo != null && !group.All.Contains(_obj.InResponseTo))
        group.All.Add(_obj.InResponseTo);
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Addressees == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>(_obj.Addressees.OrderBy(a => a.Number).Select(a => a.Correspondent));
    }
    
    /// <summary>
    /// Получить ответственного за документ.
    /// </summary>
    /// <returns>Ответственный за документ.</returns>
    public override Sungero.Company.IEmployee GetDocumentResponsibleEmployee()
    {
      if (_obj.PreparedBy != null)
        return _obj.PreparedBy;
      
      return base.GetDocumentResponsibleEmployee();
    }
    
    /// <summary>
    /// Очистить лист рассылки и заполнить первого адресата из карточки.
    /// </summary>
    public void ClearAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Correspondent != null)
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Correspondent = _obj.Correspondent;
        newAddressee.Addressee = _obj.Addressee;
        newAddressee.DeliveryMethod = _obj.DeliveryMethod;
        newAddressee.Number = 1;
      }
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступности поля в зависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      var properties = _obj.State.Properties;
      
      if (_obj.IsManyAddressees == false)
      {
        if (_obj.Correspondent != null)
          properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
        else
          properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber;
      }
      if (_obj.IsManyAddressees == true)
        properties.Addressees.Properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
      
      _obj.State.Properties.IsManyAddressees.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
    }
    
    /// <summary>
    /// Получить контактную информацию для отчета Лист рассылки.
    /// </summary>
    /// <param name="addresseesItem">Элемент коллекции адресатов.</param>
    /// <param name="document">Документ.</param>
    /// <returns>Контактная информация.</returns>
    [Public]
    public static string GetContactsInformation(Docflow.IOutgoingDocumentBaseAddressees addresseesItem, IOutgoingDocumentBase document)
    {
      if (addresseesItem.DeliveryMethod != null && addresseesItem.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange)
      {
        var boxes = addresseesItem.Correspondent.ExchangeBoxes
          .Where(b => b.Status == Sungero.Parties.CounterpartyExchangeBoxes.Status.Active)
          .Where(b => Equals(b.Box.BusinessUnit, document.BusinessUnit))
          .Select(b => b.Box.ExchangeService.Name).Distinct();
        return boxes.Any() ? string.Join(", ", boxes) : string.Empty;
      }
      
      var result = new List<string>();
      
      var postalAddress = string.IsNullOrEmpty(addresseesItem.Correspondent.PostalAddress)
        ? addresseesItem.Correspondent.LegalAddress
        : addresseesItem.Correspondent.PostalAddress;
      if (!string.IsNullOrEmpty(postalAddress))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationPostalAddressTemplate, postalAddress));
      
      var fax = addresseesItem.Addressee != null
        ? addresseesItem.Addressee.Fax
        : string.Empty;
      if (!string.IsNullOrEmpty(fax))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationFaxTemplate, fax));
      
      var email = addresseesItem.Addressee != null && !string.IsNullOrEmpty(addresseesItem.Addressee.Email)
        ? addresseesItem.Addressee.Email
        : addresseesItem.Correspondent.Email;
      if (!string.IsNullOrEmpty(email))
        result.Add(string.Format(Docflow.Reports.Resources.DistributionSheetReport.ContactsInformationEmailTemplate, email));
      
      return result.Any() ? string.Join(Environment.NewLine, result) : string.Empty;
    }
    
    /// <summary>
    /// Отключение родительской функции, т.к. здесь не нужна доступность рег.номера и даты.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      
    }
    
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;
using Sungero.Domain.Shared;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalManagerAssignmentFunctions
  {
    /// <summary>
    /// Обновить отображение доставки.
    /// </summary>
    public virtual void UpdateDeliveryMethod()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var deliveryMethodIsExchange = _obj.DeliveryMethod != null && _obj.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      
      // Не давать изменять способ доставки для исходящих писем на несколько адресатов
      if (OutgoingDocumentBases.Is(document) && OutgoingDocumentBases.As(document).IsManyAddressees == true)
      {
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = deliveryMethodIsExchange;
        _obj.State.Properties.ExchangeService.IsRequired = deliveryMethodIsExchange;
        
        if (deliveryMethodIsExchange && document != null)
        {
          var formParams = ((IExtendedEntity)_obj).Params;
          bool isIncomingDocument = false;
          if (formParams.ContainsKey(Constants.ApprovalManagerAssignment.IsIncomingDocument))
            isIncomingDocument = (bool)formParams[Constants.ApprovalManagerAssignment.IsIncomingDocument];
          else
          {
            isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
            formParams[Constants.ApprovalManagerAssignment.IsIncomingDocument] = isIncomingDocument;
          }
          var isFormalizedDocument = Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true;
          _obj.State.Properties.DeliveryMethod.IsEnabled = !isIncomingDocument;
          _obj.State.Properties.ExchangeService.IsEnabled = !(isIncomingDocument || isFormalizedDocument);
        }
      }
    }
    
    /// <summary>
    /// Синхронизировать адресатов из переданных значений.
    /// </summary>
    /// <param name="addressees">Список адресатов.</param>
    /// <param name="addressee">Адресат.</param>
    public virtual void SynchronizeAddresses(List<Company.IEmployee> addressees, IEmployee addressee)
    {
      this.SetAddressees(addressees);
      this.FillAddresseeFromAddressees();
      _obj.Addressee = addressee;
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public virtual void ClearAddresseesAndFillFirstAddressee()
    {
      this.SetAddressees(new List<IEmployee>() { _obj.Addressee });
    }
    
    /// <summary>
    /// Заполнить адресата из коллекции адресатов.
    /// </summary>
    public virtual void FillAddresseeFromAddressees()
    {
      var addressee = _obj.Addressees.OrderBy(a => a.Id).FirstOrDefault(a => a.Addressee != null);
      if (addressee != null)
        _obj.Addressee = addressee.Addressee;
      else
        _obj.Addressee = null;
    }
    
    /// <summary>
    /// Задать адресатов в задании на согласование руководителем.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).Distinct().ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
  }
}
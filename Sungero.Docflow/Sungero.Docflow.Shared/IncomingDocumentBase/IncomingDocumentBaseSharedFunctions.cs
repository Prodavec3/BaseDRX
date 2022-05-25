using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow.Shared
{
  partial class IncomingDocumentBaseFunctions
  {
    /// <summary>
    /// Добавить в группу вложений исходящее письмо, в ответ на которое было создано входящее.
    /// </summary>
    /// <param name="group">Группа вложений.</param>
    public override void AddRelatedDocumentsToAttachmentGroup(Sungero.Workflow.Interfaces.IWorkflowEntityAttachmentGroup group)
    {
      if (_obj.InResponseTo != null && !group.All.Contains(_obj.InResponseTo) && _obj.InResponseTo.AccessRights.CanRead())
        group.All.Add(_obj.InResponseTo);
    }
    
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Correspondent == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>() { _obj.Correspondent };
    }
    
    /// <summary>
    /// Получить адресатов.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    [Public]
    public override List<Company.IEmployee> GetAddressees()
    {
      return _obj.Addressees.Select(x => x.Addressee).Distinct().ToList();
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber)
    {
      _obj.State.Properties.Correspondent.IsEnabled = isEnabled && !counterpartyCodeInNumber;
    }

    /// <summary>
    /// Отключение родительской функции, т.к. здесь не нужна доступность рег.номера и даты.
    /// </summary>
    public override void EnableRegistrationNumberAndDate()
    {
      
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public void ClearAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Addressee != null)
      {
        var newAddressee = _obj.Addressees.AddNew();
        newAddressee.Addressee = _obj.Addressee;
        newAddressee.Number = 1;
      }
    }
    
    /// <summary>
    /// Заполнить адресата из коллекции адресатов.
    /// </summary>
    public virtual void FillAddresseeFromAddressees()
    {
      var addressee = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Addressee != null);
      if (addressee != null)
        _obj.Addressee = addressee.Addressee;
      else
        _obj.Addressee = null;
    }
    
    /// <summary>
    /// Установить метку "Несколько адресатов".
    /// </summary>
    public virtual void SetManyAddresseesPlaceholder()
    {
      // Заполнить метку в локали тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.ManyAddresseesPlaceholder = OfficialDocuments.Resources.ManyAddresseesPlaceholder;
    }
  }
}
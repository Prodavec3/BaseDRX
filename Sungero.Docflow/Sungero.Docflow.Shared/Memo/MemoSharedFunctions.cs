using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow.Shared
{
  partial class MemoFunctions
  {
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
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      this.ChangeAddresseePropertiesAccess();
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      var manyAddresseesMode = _obj.IsManyAddressees == true;
      _obj.State.Properties.Addressee.IsRequired = !manyAddresseesMode;
      _obj.State.Properties.Addressees.IsRequired = manyAddresseesMode;
    }
    
    /// <summary>
    /// Очистить адресатов и заполнить первого адресата из карточки.
    /// </summary>
    public virtual void ClearAndFillFirstAddressee()
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
      {
        if (!Equals(_obj.Addressee, addressee.Addressee))
          _obj.Addressee = addressee.Addressee;
      }
      else
      {
        if (_obj.Addressee != null)
          _obj.Addressee = null;
      }
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
    
    /// <summary>
    /// Установить доступность всех контролов адресатов (единичный адресат, грид, плейсхолдер).
    /// </summary>
    public virtual void ChangeAddresseePropertiesAccess()
    {
      var readOnlyState = _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
        _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
        _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
        _obj.InternalApprovalState == Docflow.Memo.InternalApprovalState.PendingReview ||
        _obj.InternalApprovalState == Docflow.Memo.InternalApprovalState.Reviewed;
      
      var manyAddressees = _obj.IsManyAddressees.HasValue && _obj.IsManyAddressees.Value;
      
      _obj.State.Properties.Addressee.IsVisible = !manyAddressees;
      _obj.State.Properties.Addressee.IsEnabled = !manyAddressees &&
        (!readOnlyState ||
         readOnlyState && _obj.Addressee == null);
      _obj.State.Properties.ManyAddresseesPlaceholder.IsVisible = manyAddressees;
      _obj.State.Properties.Addressees.IsEnabled = manyAddressees &&
        (!readOnlyState ||
         readOnlyState && _obj.Addressees != null && (_obj.Addressees.Count == 0 || _obj.Addressees.Any(x => x.Addressee == null)));
      _obj.State.Properties.IsManyAddressees.IsEnabled = !readOnlyState;
    }
    
    /// <summary>
    /// Заполнить адресатов из списка ознакомления/рассмотрения.
    /// </summary>
    /// <param name="acquaintanceList">Список ознакомления/рассмотрения.</param>
    /// <returns>Сообщение об ошибке или пустая строка.</returns>
    public virtual string TryFillFromAcquaintanceList(RecordManagement.IAcquaintanceList acquaintanceList)
    {
      if (acquaintanceList == null)
        return string.Empty;
      
      var participants = RecordManagement.PublicFunctions.AcquaintanceList.GetParticipants(acquaintanceList);
      var addresseesLimit = this.GetAddresseesLimit();
      if (participants.Count > addresseesLimit)
        return Memos.Resources.TooManyParticipantsFormat(addresseesLimit);
      
      foreach (var participant in participants)
      {
        var newParticipantRow = _obj.Addressees.AddNew();
        newParticipantRow.Addressee = participant;
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить максимальное количество адресатов.
    /// </summary>
    /// <returns>Максимальное количество адресатов.</returns>
    public virtual int GetAddresseesLimit()
    {
      return Constants.Memo.AddresseesLimit;
    }
  }
}
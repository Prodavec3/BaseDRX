using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ExchangeDocument;

namespace Sungero.Docflow.Shared
{
  partial class ExchangeDocumentFunctions
  {
    /// <summary>
    /// Получить контрагентов по документу.
    /// </summary>
    /// <returns>Контрагенты.</returns>
    public override List<Sungero.Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Counterparty == null)
        return null;
      
      return new List<Sungero.Parties.ICounterparty>() { _obj.Counterparty };
    }
    
    /// <summary>
    /// Получить основание подписания со стороны контрагента.
    /// </summary>
    /// <returns>Основание подписания со стороны контрагента.</returns>
    [Public]
    public override string GetCounterpartySigningReason()
    {
      return _obj.CounterpartySigningReason;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> "<содержание>" от <имя контрагента>.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
        
        if (_obj.Counterparty != null)
          name += ExchangeDocuments.Resources.From + _obj.Counterparty.Name;
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Functions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Functions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Заполнить подписывающего.
    /// </summary>
    /// <param name="signatory">Подписывающий со стороны контрагента.</param>
    public override void FillCounterpartySignatory(Parties.IContact signatory)
    {
      _obj.CounterpartySignatory = signatory;
    }
    
    /// <summary>
    /// Заполнить основание со стороны контрагента.
    /// </summary>
    /// <param name="signingReason">Основание контрагента.</param>
    public override void FillCounterpartySigningReason(string signingReason)
    {
      if (!string.IsNullOrEmpty(signingReason) && signingReason.Length > _obj.Info.Properties.CounterpartySigningReason.Length)
        signingReason = signingReason.Substring(0, _obj.Info.Properties.CounterpartySigningReason.Length);
      _obj.CounterpartySigningReason = signingReason;
    }
    
    // Перекрываем ф-ю OfficialDocument, т.к. входящий документ эл. системы обмена не нумеруемый, но д.б. черновиком, а не действующим.
    public override void SetLifeCycleState()
    {
      return;
    }
    
    /// <summary>
    /// Сменить доступность поля Контрагент.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber)
    {
      _obj.State.Properties.Counterparty.IsEnabled = isEnabled && !counterpartyCodeInNumber;
    }
    
  }
}
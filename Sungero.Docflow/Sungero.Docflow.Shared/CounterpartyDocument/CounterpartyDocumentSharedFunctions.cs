using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.CounterpartyDocument;

namespace Sungero.Docflow.Shared
{
  partial class CounterpartyDocumentFunctions
  {
    public override List<Parties.ICounterparty> GetCounterparties()
    {
      if (_obj.Counterparty == null)
        return null;
      
      return new List<Parties.ICounterparty> { _obj.Counterparty };
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      // Имя в формате: <Вид документа> <Имя контрагента> №<номер> от <дата> <содержание>.
      var name = string.Empty;
      using (TenantInfo.Culture.SwitchTo())
      {
        if (documentKind != null)
          name += documentKind.ShortName + " ";
        
        var counterparty = _obj.Counterparty;
        if (counterparty != null)
          name += counterparty.Name + " ";
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " " + _obj.Subject;
        
        name = Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
        _obj.Name = Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
      }
    }
    
    /// <summary>
    /// Изменение состояния документа для ненумеруемых документов.
    /// </summary>
    public override void SetLifeCycleState()
    {
      // Не изменять статус при сохранении.
    }
    
    public override void RefreshDocumentForm()
    {
      base.RefreshDocumentForm();
      
      var isNotNumerableType = _obj.DocumentKind == null || _obj.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.NotNumerable;
      _obj.State.Properties.BusinessUnit.IsVisible = !isNotNumerableType;
      _obj.State.Properties.Department.IsVisible = !isNotNumerableType;
      _obj.State.Properties.Assignee.IsVisible = false;
      _obj.State.Properties.PreparedBy.IsVisible = false;
      _obj.State.Properties.OurSignatory.IsVisible = false;
    }
    
  }
}
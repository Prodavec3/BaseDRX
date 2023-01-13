using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorneyBase;

namespace Sungero.Docflow.Shared
{
  partial class PowerOfAttorneyBaseFunctions
  {
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      // Поле "Действует по" доступно для редактирования при изменении реквизитов и для доверенностей в разработке.
      var isDraft = _obj.LifeCycleState == Docflow.PowerOfAttorney.LifeCycleState.Draft;
      _obj.State.Properties.ValidTill.IsEnabled = isDraft || isEnabled;
      _obj.State.Properties.ValidFrom.IsEnabled = isDraft || isEnabled;

      // При перерегистрации "Кому выдана" недоступно, если в формате номера журнала есть код подразделения.
      var documentRegister = _obj.DocumentRegister;
      var departmentCodeIncludedInNumber = isRepeatRegister && documentRegister != null &&
        documentRegister.NumberFormatItems.Any(n => n.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode);
      _obj.State.Properties.IssuedTo.IsEnabled = isEnabled && !departmentCodeIncludedInNumber;
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> для <Кому выдана> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (_obj.IssuedTo != null)
          name += PowerOfAttorneyBases.Resources.DocumentnameFor + Company.PublicFunctions.Employee.GetShortName(_obj.IssuedTo, DeclensionCase.Genitive, true);
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " \"" + _obj.Subject + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Functions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Functions.OfficialDocument.AddClosingQuote(name, _obj);
      
    }
    
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      
    }
  }
}
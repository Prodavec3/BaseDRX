using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow
{
  partial class RegistrationSettingClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var databooksWithNullCode = Functions.Module.Remote.HasDatabooksWithNullCode();
      if (databooksWithNullCode.HasDepartmentWithNullCode)
        e.Params.AddOrUpdate(Constants.RegistrationSetting.HasDepartmentWithNullCode, true);
      
      if (databooksWithNullCode.HasBusinessUnitWithNullCode)
        e.Params.AddOrUpdate(Constants.RegistrationSetting.HasBusinessUnitWithNullCode, true);
      
      if (databooksWithNullCode.HasDocumentKindWithNullCode)
        e.Params.AddOrUpdate(Constants.RegistrationSetting.HasDocumentKindWithNullCode, true);
    }
    
    public virtual IEnumerable<Enumeration> DocumentFlowFiltering(IEnumerable<Enumeration> query)
    {
      // Для входящих документов резервирование не имеет смысла.
      // Исключить возможность выбора входящих документов для резервирования.
      if (_obj.SettingType == SettingType.Reservation)
        query = query.Where(df => !Equals(df, DocumentFlow.Incoming));
      
      return query;
    }
    
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (!e.IsValid)
        return;
      
      _obj.State.Properties.DocumentKinds.IsRequired = true;
      _obj.State.Properties.DocumentRegister.IsEnabled = _obj.DocumentFlow != null;
      _obj.State.Properties.DocumentKinds.IsEnabled = _obj.DocumentFlow != null;
      
      if (_obj.AccessRights.CanUpdate() && !_obj.State.IsInserted &&
          _obj.DocumentRegister != null && _obj.DocumentRegister.RegistrationGroup != null &&
          !Functions.Module.CalculateParams(e, _obj.DocumentRegister.RegistrationGroup, true, true, false, false, null))
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
      
      _obj.State.Properties.DocumentRegister.IsRequired = _obj.State.Properties.DocumentRegister.IsEnabled;
      
      var register = _obj.DocumentRegister;
      
      // Если в формате номера в журнале есть код подразделения, то проверять, что у всех указанных в настройке подразделений заполнены коды.
      if (register != null)
      {
        if (register.NumberFormatItems.Any(f => f.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode))
        {
          var hasDepartmentsWithoutCode = _obj.Departments.Any() ?
            _obj.Departments.Select(x => x.Department).Any(x => x.Code == null) :
            e.Params.Contains(Constants.DocumentRegister.HasDepartmentWithNullCode);
          
          if (hasDepartmentsWithoutCode)
            e.AddWarning(DocumentRegisters.Resources.NeedFillDepartmentCodes, _obj.Info.Actions.ShowDepartments);
        }
        
        // Если в формате номера в журнале есть код НОР, то проверять, что у всех указанных в настройке НОР заполнены коды.
        if (register.NumberFormatItems.Any(f => f.Element == DocumentRegisterNumberFormatItems.Element.BUCode))
        {
          var hasBusinessUnitsWithoutCode = _obj.BusinessUnits.Any() ?
            _obj.BusinessUnits.Select(x => x.BusinessUnit).Any(x => x.Code == null) :
            e.Params.Contains(Constants.DocumentRegister.HasBusinessUnitWithNullCode);
          
          if (hasBusinessUnitsWithoutCode)
            e.AddWarning(DocumentRegisters.Resources.NeedFillBusinessUnitCodes, _obj.Info.Actions.ShowBusinessUnits);
        }
        
        // Если в формате номера в журнале есть код вида документа, то проверять, что у всех указанных в настройке видов документа заполнены коды.
        if (register.NumberFormatItems.Any(f => f.Element == DocumentRegisterNumberFormatItems.Element.DocKindCode))
        {
          var hasDocumentKindWithNullCode = false;
          if (_obj.DocumentKinds.Any())
            hasDocumentKindWithNullCode = _obj.DocumentKinds.Select(x => x.DocumentKind).Where(x => x.Code == null).Any();
          else
            hasDocumentKindWithNullCode = e.Params.Contains(Constants.DocumentRegister.HasDocumentKindWithNullCode);

          if (hasDocumentKindWithNullCode)
            e.AddWarning(DocumentRegisters.Resources.NeedFillDocumentKindCodes, _obj.Info.Actions.ShowDocumentKinds);
        }
        
      }
    }
  }
}
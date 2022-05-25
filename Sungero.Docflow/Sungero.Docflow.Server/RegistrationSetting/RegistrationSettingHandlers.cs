using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow
{
  partial class RegistrationSettingDocumentKindsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindsDocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(k => k.DocumentFlow == _root.DocumentFlow &&
                         ((k.NumberingType == DocumentKind.NumberingType.Numerable && _root.SettingType == RegistrationSetting.SettingType.Numeration) ||
                          (k.NumberingType == DocumentKind.NumberingType.Registrable && (_root.SettingType == RegistrationSetting.SettingType.Registration ||
                                                                                         _root.SettingType == RegistrationSetting.SettingType.Reservation))));
    }
  }

  partial class RegistrationSettingDocumentRegisterPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> DocumentRegisterFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var registerType = (_obj.SettingType == SettingType.Numeration) ? DocumentRegister.RegisterType.Numbering : DocumentRegister.RegisterType.Registration;
      return query.Where(l => l.DocumentFlow == _obj.DocumentFlow && l.RegisterType == registerType);
    }
  }

  partial class RegistrationSettingServerHandlers
  {
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (_obj.DocumentFlow == null)
        _obj.DocumentFlow = Docflow.RegistrationSetting.DocumentFlow.Incoming;
      if (_obj.SettingType == null)
        _obj.SettingType = SettingType.Registration;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Priority = 0;
      if (_obj.BusinessUnits.Any())
        _obj.Priority += 8;
      if (_obj.DocumentKinds.Any())
        _obj.Priority += 4;
      if (_obj.Departments.Any())
        _obj.Priority += 2;

      var conflictedSettings = Functions.RegistrationSetting.GetDoubleSettings(_obj);
      if (conflictedSettings.Any())
        e.AddError(RegistrationSettings.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicate);
      
      // Проверка допустимости журнала.
      if (_obj.DocumentRegister != null)
      {
        var isTypeValid = _obj.SettingType == (_obj.DocumentRegister.RegisterType == DocumentRegister.RegisterType.Numbering ? SettingType.Numeration : _obj.SettingType);
        var isDirectionValid = _obj.DocumentFlow == _obj.DocumentRegister.DocumentFlow;
        if (!isTypeValid || !isDirectionValid)
          e.AddError(RegistrationSettings.Resources.InvalidDocumentRegister);
      }
    }
  }
}
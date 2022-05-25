using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;

namespace Sungero.Docflow
{
  partial class DocumentRegisterNumberFormatItemsSharedCollectionHandlers
  {
    public virtual void NumberFormatItemsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      // Установить номер на 1 больше максимального.
      var maxNumber = _obj.NumberFormatItems.Select(element => element.Number).OrderByDescending(element => element).FirstOrDefault();
      if (maxNumber != null)
        _added.Number = maxNumber + 1;
      else
        _added.Number = 1;
    }
  }

  partial class DocumentRegisterSharedHandlers
  {
    public virtual void RegisterTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      var numerable = e.NewValue == RegisterType.Numbering;
      if (numerable && e.NewValue != e.OldValue)
        _obj.RegistrationGroup = null;
      var isUsed = Constants.Module.IsUsedParamName;
      bool isUsedValue;
      _obj.State.Properties.RegistrationGroup.IsEnabled = (!e.Params.TryGetValue(isUsed, out isUsedValue) || !isUsedValue) && e.NewValue == RegisterType.Registration;
      Functions.DocumentRegister.SetRequiredProperties(_obj);
    }

    public virtual void IndexChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Заполнить пример значения в соответствии с форматом.
      Functions.DocumentRegister.FillValueExample(_obj);
    }

    public virtual void RegistrationGroupChanged(Sungero.Docflow.Shared.DocumentRegisterRegistrationGroupChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      var isSubstituteParamName = Constants.Module.IsSubstituteResponsibleEmployeeParamName;
      if (e.Params.Contains(isSubstituteParamName))
        e.Params.Remove(isSubstituteParamName);
      
      var isAdministratorParamName = Constants.Module.IsAdministratorParamName;
      if (e.Params.Contains(isAdministratorParamName))
        e.Params.Remove(isAdministratorParamName);
      
      var allowedDocumentFlows = Functions.DocumentRegister.GetFilteredDocumentFlows(_obj, _obj.DocumentFlowAllowedItems.AsQueryable());
      if (allowedDocumentFlows.Count == 1)
        _obj.DocumentFlow = allowedDocumentFlows.Single();
      
      // Заполнить пример значения в соответствии с форматом.
      Functions.DocumentRegister.FillValueExample(_obj);
    }

    public virtual void NumberFormatItemsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      // Заполнить пример значения в соответствии с форматом.
      Functions.DocumentRegister.FillValueExample(_obj);
    }

    public virtual void NumberOfDigitsInNumberChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      // Исправить количество цифр в номере при неверном вводе.
      if (e.NewValue > 9)
        _obj.NumberOfDigitsInNumber = 9;
      else if (e.NewValue < 1)
        _obj.NumberOfDigitsInNumber = 1;
      
      // Заполнить пример значения в соответствии с форматом.
      Functions.DocumentRegister.FillValueExample(_obj);
    }
  }
}
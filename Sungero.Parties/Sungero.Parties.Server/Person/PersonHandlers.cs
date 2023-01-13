using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Person;

namespace Sungero.Parties
{
  partial class PersonServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);
      
      Sungero.Company.PublicFunctions.Employee.CreateUpdateEmployeeNameAsyncHandler(_obj.Id);
      Sungero.Parties.PublicFunctions.Contact.CreateUpdateContactNameAsyncHandler(_obj.Id);
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Если персона создается из выпадающего списка (см. 56654),
      // то введенные в контрол ФИО будут доступны в e.Filter.
      if (string.IsNullOrWhiteSpace(_obj.FirstName) &&
          string.IsNullOrWhiteSpace(_obj.MiddleName) &&
          string.IsNullOrWhiteSpace(_obj.LastName) &&
          !string.IsNullOrWhiteSpace(e.Filter))
        _obj.LastName = e.Filter;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!string.IsNullOrEmpty(_obj.FirstName))
      {
        _obj.FirstName = _obj.FirstName.Trim();
        if (string.IsNullOrWhiteSpace(_obj.FirstName))
          e.AddError(_obj.Info.Properties.FirstName, Commons.Resources.RequiredPropertiesNotFilledIn);
      }
      if (!string.IsNullOrEmpty(_obj.LastName))
      {
        _obj.LastName = _obj.LastName.Trim();
        if (string.IsNullOrWhiteSpace(_obj.LastName))
          e.AddError(_obj.Info.Properties.LastName, Commons.Resources.RequiredPropertiesNotFilledIn);
      }
      if (!string.IsNullOrEmpty(_obj.MiddleName))
        _obj.MiddleName = _obj.MiddleName.Trim();
      
      Functions.Person.FillName(_obj);
      
      base.BeforeSave(e);
    }
  }
}
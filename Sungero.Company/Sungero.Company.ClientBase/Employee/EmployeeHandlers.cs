using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{

  partial class EmployeeClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Sungero.Company.PublicFunctions.Employee.SetRequiredProperties(_obj);
      
      if (!Functions.Employee.IsValidEmail(_obj.Email))
        e.AddWarning(_obj.Info.Properties.Email, Parties.Resources.WrongEmailFormat);
    }

    public virtual void EmailValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (!Functions.Employee.IsValidEmail(e.NewValue))
        e.AddError(Parties.Resources.WrongEmailFormat);
      else if (!Docflow.PublicFunctions.Module.IsASCII(e.NewValue))
        e.AddWarning(Docflow.Resources.ASCIIWarning);
    }

    public override void SignatoryInfoObtaining(Sungero.Domain.Client.SignatoryInfoObtainingEventArgs e)
    {
      var result = _obj.JobTitle != null ? _obj.JobTitle.Name.Trim() : string.Empty;
      
      if (_obj.Department != null)
      {
        var department = string.IsNullOrEmpty(_obj.Department.ShortName) ? _obj.Department.Name : _obj.Department.ShortName;
        
        if (string.IsNullOrEmpty(result))
          result = department;
        else
          result = string.Format("{0} ({1})", result, department);
      }
      
      e.AdditionalInfo = result + "|";
    }
  }
}
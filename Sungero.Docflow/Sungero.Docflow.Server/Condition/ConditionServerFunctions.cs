using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Condition;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow.Server
{
  partial class ConditionFunctions
  {
    /// <summary>
    /// Создание условия.
    /// </summary>
    /// <returns>Условие.</returns>
    [Remote]
    public static ICondition CreateCondition()
    {
      return Conditions.Create();
    }
    
    public override string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == Sungero.Docflow.Condition.ConditionType.Addressee)
        {
          var addressees = _obj.Addressees.Select(a => a.Addressee.Person.ShortName).ToList();
          var conditionName = Functions.ConditionBase.ConditionMultiSelectNameBuilder(addressees);
          return Conditions.Resources.TitleAddresseeFormat(conditionName);
        }
        
        if (_obj.ConditionType == Sungero.Docflow.Condition.ConditionType.ManyAddressees)
          return Conditions.Resources.ManyAddressees;
      }
      return base.GetConditionName();
    }
    
    public override List<Sungero.Company.IEmployee> GetEmployeesFromProperties()
    {
      var employees = base.GetEmployeesFromProperties();
      employees.AddRange(_obj.Addressees.Select(a => a.Addressee));
      return employees;
    }
  }
}
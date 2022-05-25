using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.SignatureSetting
{
  partial class Signatory
  {
    public int EmployeeId { get; set; }
    
    public int Priority { get; set; }
  }
  
  partial class SignatoryByDepartment
  {
    public Sungero.Company.IEmployee Employee { get; set; }
    
    public Sungero.Company.IDepartment Department { get; set; }
    
    public string Conditions { get; set; }
    
    public int Priority { get; set; }
    
  }
  
  partial class SignatoriesList
  {
    public string Employees { get; set; }
    
    public Sungero.Company.IDepartment Department { get; set; }
  }
}